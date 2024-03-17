using Celeste64.Mod.Helpers;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Celeste64.Mod.Editor;

public class EditorWorld : World
{
	internal readonly ImGuiHandler[] Handlers = [
		new EditorMenuBar(),

		new EditActorWindow(),
		new EnvironmentSettingsWindow(),
	];

	public static EditorWorld Current => (Game.Scene as EditorWorld)!;

	public List<ActorDefinition> Definitions => Map is FujiMap fujiMap ? fujiMap.Definitions : [];
	public ReadOnlyDictionary<ActorDefinition, Actor[]> ActorsFromDefinition => actorsFromDefinition.AsReadOnly();
	public ReadOnlyDictionary<Actor, ActorDefinition> DefinitionFromActors => definitionFromActors.AsReadOnly();

	public event Action<ActorDefinition?> OnSelectionChanged = def => {};
	
	private ActorDefinition? selectedDefinition = null;
	public ActorDefinition? Selected
	{
		private set
		{
			selectedDefinition = value;
			gizmo = null;
			
			OnSelectionChanged(value);

			if (selectedDefinition is null)
				return;
			
			var positionProp = selectedDefinition
				.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(prop =>
					!prop.HasAttr<IgnorePropertyAttribute>() &&
					prop.GetCustomAttribute<SpecialPropertyAttribute>() is { Value: SpecialPropertyType.PositionXYZ });

			if (positionProp is null || positionProp.GetGetMethod() is not { } getMethod || positionProp.GetSetMethod() is not { } setMethod)
				return;

			gizmo = new PositionGizmo(
				() => (Vec3)getMethod.Invoke(selectedDefinition, [])!,
				newValue =>
				{
					setMethod.Invoke(selectedDefinition, [newValue]);
					selectedDefinition.Dirty = true;
				});
		}
		get => selectedDefinition;
	}

	public Actor[] SelectedActors => Selected is not null && ActorsFromDefinition.TryGetValue(Selected, out var actors) ? actors : [];

	private readonly Dictionary<ActorDefinition, Actor[]> actorsFromDefinition = new();
	private readonly Dictionary<Actor, ActorDefinition> definitionFromActors = new();

	private Vec3 cameraPos = new(0, -10, 0);
	private Vec2 cameraRot = new(0, 0);

	private readonly Batcher3D batch3D = new();

	// TODO: Temporary!
	private Gizmo? gizmo;
	private SelectionTarget? dragTarget = null;
	private Vec2 dragMouseStart = Vec2.Zero;

	internal EditorWorld(EntryInfo entry) : base(entry)
	{
		Camera.NearPlane = 0.1f; // Allow getting closer to objects
		Camera.FOVMultiplier = 1.25f; // Higher FOV feels better in the editor

		// Store previous game resolution to restore it when exiting
		previousScale = Game.ResolutionScale;

		// Load environment
		RefreshEnvironment();

		// Map gets implicitly loaded, since our Definitions are taken directly from it
		// However mark all definitions as dirty to ensure they will get added
		foreach (var def in Definitions)
		{
			def.Dirty = true;
		}
	}

	internal void RefreshEnvironment()
	{
		Camera.FarPlane = Save.Instance.Editor.RenderDistance;
		Game.ResolutionScale = Save.Instance.Editor.ResolutionType switch
		{
			Save.EditorSettings.Resolution.Game => 1.0f,
			Save.EditorSettings.Resolution.Double => 2.0f,
			Save.EditorSettings.Resolution.HD => 3.0f,
			Save.EditorSettings.Resolution.Native => Math.Max(App.Width / (float)Game.DefaultWidth, App.Height / (float)Game.DefaultHeight),
			_ => throw new ArgumentOutOfRangeException(),
		};

		if (Map == null)
			return;

		// Taken from World constructor with added cleanup of previously created stuff

		if (Get<Snow>() is { } snow)
			Destroy(snow);
		if (Map.SnowAmount > 0 && Save.Instance.Editor.RenderSnow)
		{
			Add(new Snow(Map.SnowAmount, Map.SnowWind));
		}

		Game.Instance.Music.Stop();
		Game.Instance.MusicWav?.Stop();
		if (Save.Instance.Editor.PlayMusic)
		{
			if (Map.Music != null && Assets.Music.ContainsKey(Map.Music))
			{
				MusicWav = Map.Music;
				Music = $"event:/music/";
			}
			else
			{
				MusicWav = "";
				Music = $"event:/music/{Map.Music}";
			}
		}
		else
		{
			MusicWav = string.Empty;
			Music = string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(Music))
			Game.Instance.Music = Audio.Play(Music);
		if (!string.IsNullOrWhiteSpace(MusicWav))
			Game.Instance.MusicWav = Audio.PlayMusic(MusicWav);

		Game.Instance.Ambience.Stop();
		Game.Instance.AmbienceWav?.Stop();
		if (Save.Instance.Editor.PlayAmbience)
		{
			if (Map.Ambience != null && Assets.Music.ContainsKey(Map.Ambience))
			{
				AmbienceWav = Map.Ambience;
				Ambience = $"event:/sfx/ambience/";
			}
			else
			{
				AmbienceWav = "";
				Ambience = $"event:/sfx/ambience/{Map.Ambience}";
			}
		}
		else
		{
			AmbienceWav = string.Empty;
			Ambience = string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(Ambience))
			Game.Instance.Ambience = Audio.Play(Ambience);
		if (!string.IsNullOrWhiteSpace(AmbienceWav))
			Game.Instance.AmbienceWav = Audio.PlayMusic(AmbienceWav);

		skyboxes.Clear();
		if (!string.IsNullOrEmpty(Map.Skybox) && Save.Instance.Editor.RenderSkybox)
		{
			// single skybox
			if (Assets.Textures.TryGetValue($"skyboxes/{Map.Skybox}", out var skybox))
			{
				skyboxes.Add(new(skybox));
			}
			// group
			else
			{
				while (Assets.Textures.TryGetValue($"skyboxes/{Map.Skybox}_{skyboxes.Count}", out var nextSkybox))
					skyboxes.Add(new(nextSkybox));
			}
		}
	}

	private float previousScale = 1.0f;

	public override void Entered()
	{
		// Game.ResolutionScale = Save.;
	}
	public override void Exited()
	{
		Game.ResolutionScale = previousScale;
	}

	public void RemoveDefinition(ActorDefinition definition)
	{
		Definitions.Remove(definition);
		if (actorsFromDefinition.Remove(definition, out var actors))
		{
			foreach (var actor in actors)
			{
				definitionFromActors.Remove(actor);
				Destroy(actor);
			}
		}
	}

	public override void Update()
	{
		// Toggle to in-game
		if (Input.Keyboard.Pressed(Keys.F3))
		{
			Game.Scene!.Exited();
			Game.Instance.scenes.Pop();
			Game.Instance.scenes.Push(new World(Entry));
			Game.Scene.Entered();
			return;
		}

		if (Input.Keyboard.Ctrl && Input.Keyboard.Pressed(Keys.S) && Map is FujiMap { FullPath: { } fullPath } fujiMap)
		{
			Log.Info($"Saving map to '{fullPath}'");
			fujiMap.SaveToFile();

			return;
		}

		// Camera movement
		var cameraForward = new Vec3(
			MathF.Sin(cameraRot.X),
			MathF.Cos(cameraRot.X),
			0.0f);
		var cameraRight = new Vec3(
			MathF.Sin(cameraRot.X - Calc.HalfPI),
			MathF.Cos(cameraRot.X - Calc.HalfPI),
			0.0f);

		float moveSpeed = 250.0f;

		if (Input.Keyboard.Down(Keys.W))
			// cameraPos += cameraForward * moveSpeed * Time.Delta;
			cameraPos += Camera.Forward * moveSpeed * Time.Delta;
		if (Input.Keyboard.Down(Keys.S))
			// cameraPos -= cameraForward * moveSpeed * Time.Delta;
			cameraPos -= Camera.Forward * moveSpeed * Time.Delta;
		if (Input.Keyboard.Down(Keys.A))
			cameraPos += cameraRight * moveSpeed * Time.Delta;
		if (Input.Keyboard.Down(Keys.D))
			cameraPos -= cameraRight * moveSpeed * Time.Delta;
		if (Input.Keyboard.Down(Keys.Space))
			cameraPos.Z += moveSpeed * Time.Delta;
		if (Input.Keyboard.Down(Keys.LeftShift))
			cameraPos.Z -= moveSpeed * Time.Delta;

		// Camera rotation
		float rotateSpeed = 16.5f * Calc.DegToRad;
		if (Input.Mouse.Down(MouseButtons.Right) && !ImGuiManager.WantCaptureMouse)
		{
			cameraRot.X += InputHelper.MouseDelta.X * rotateSpeed * Time.Delta;
			cameraRot.Y += InputHelper.MouseDelta.Y * rotateSpeed * Time.Delta;
			cameraRot.X %= 360.0f * Calc.DegToRad;
			cameraRot.Y = Math.Clamp(cameraRot.Y, -89.9f * Calc.DegToRad, 89.9f * Calc.DegToRad);
		}

		// Update camera
		var forward = new Vec3(
			MathF.Sin(cameraRot.X) * MathF.Cos(cameraRot.Y),
			MathF.Cos(cameraRot.X) * MathF.Cos(cameraRot.Y),
			MathF.Sin(-cameraRot.Y));
		Camera.Position = cameraPos;
		Camera.LookAt = cameraPos + forward;

		// Shoot ray cast for selection
		SelectionRaycast();

		// Update actors of definitions
		foreach (var def in Definitions.Where(def => def.Dirty))
		{
			if (actorsFromDefinition.Remove(def, out var actors))
			{
				foreach (var actor in actors)
				{
					definitionFromActors.Remove(actor);
					Destroy(actor);
				}
			}

			var newActors = def.Load(WorldType.Editor);
			actorsFromDefinition[def] = newActors;

			foreach (var actor in newActors)
			{
				definitionFromActors.Add(actor, def);
				Add(actor);
			}

			def.Dirty = false;
			def.Updated();
		}

		// Don't call base.Update, since we don't want the actors to update
		// Instead we manually call only the things which we want for the editor

		// toggle debug draw
		if (Input.Keyboard.Pressed(Keys.F1))
			DebugDraw = !DebugDraw;

		// add / remove actors
		ResolveChanges();
	}

	private void SelectionRaycast()
	{
		if (ImGuiManager.WantCaptureMouse ||
		    Camera.Target is null ||
		    !Matrix.Invert(Camera.Projection, out var inverseProj) ||
		    !Matrix.Invert(Camera.View, out var inverseView))
		{
			return;
		}
		
		// The top-left of the image might not be the top-left of the window, when using non 16:9 aspect ratios
		var scale = Math.Min(App.WidthInPixels / (float)Camera.Target.Width, App.HeightInPixels / (float)Camera.Target.Height);
		var imageRelativePos = Input.Mouse.Position - (App.SizeInPixels / 2 - Camera.Target.Bounds.Size / 2 * scale);
		// Convert into normalized-device-coordinates
		var ndcPos = imageRelativePos / (Camera.Target.Bounds.Size / 2 * scale) - Vec2.One;
		// Flip Y, since up is negative in NDC coords
		ndcPos.Y *= -1.0f;
		var clipPos = new Vec4(ndcPos, -1.0f, 1.0f);
		var eyePos = Vec4.Transform(clipPos, inverseProj);
		// We only care about XY, so we set ZW to "forward"
		eyePos.Z = -1.0f;
		eyePos.W = 0.0f;
		var worldPos = Vec4.Transform(eyePos, inverseView);
		var direction = new Vec3(worldPos.X, worldPos.Y, worldPos.Z).Normalized();

		// Continue/Stop dragging
		if (Input.Mouse.LeftDown && dragTarget is not null)
		{
			dragTarget.OnDragged?.Invoke(Input.Mouse.Position - dragMouseStart, direction);
			return;
		}
		dragTarget = null;

		if (Selected is not null && Selected.SelectionTypes.Length > 0)
		{
			// TODO: Allow for selection different types
			var selType = Selected.SelectionTypes[0];
			var targets = selType.Targets.Concat((gizmo as PositionGizmo)?.Targets ?? []);
			
			SelectionTarget? closest = null;
			float closestDist = float.PositiveInfinity;
			
			foreach (var target in targets)
			{
				if (!ModUtils.RayIntersectOBB(Camera.Position, direction, target.Bounds, target.Transform, out float dist) || dist >= closestDist)
					continue;
				
				closest = target;
				closestDist = dist;
			}
			
			if (closest is not null)
			{
				closest.OnHovered?.Invoke();
				if (Input.Mouse.LeftPressed)
				{
					closest.OnSelected?.Invoke();
				
					dragTarget = closest;
					dragMouseStart = Input.Mouse.Position;
				}
				return;
			}
		}
		
		// Notice when mouse is hovering over.
		// While dragging, don't update the gizmo since we might go out of the gizmo's bounds.
		bool isDragging = Input.Mouse.LeftDown && !Input.Mouse.LeftPressed;
		// bool hitGizmo = isDragging || (gizmo?.RaycastCheck(Camera.Position, direction) ?? false);

		if (Input.Mouse.LeftPressed)
		{
			// First check if we hit a gizmo
			// if (hitGizmo)
			// {
				// Start dragging
				
				// gizmo?.DragStart();
			// }
			// Then check for actors
			// else
			// {
				if (ActorRayCast(Camera.Position, direction, 10000.0f, out var hit, ignoreBackfaces: false))
					Selected = hit.Actor is not null && definitionFromActors.TryGetValue(hit.Actor, out var def) ? def : null;
				else
					Selected = null;
			// }
		}
		// Continue dragging
		else if (Input.Mouse.LeftDown)
		{
			// gizmo?.Drag(this, Input.Mouse.Position - dragMouseStart, direction);
		}
	}

	public override void Render(Target target)
	{
		// We copy and modify World.Render, since that's easier

		debugRndTimer.Restart();
		Camera.Target = target;
		target.Clear(0x444c83, 1, 0, ClearMask.All);

		// create render state
		RenderState state = new();
		{
			state.Camera = Camera;
			state.ModelMatrix = Matrix.Identity;
			state.SunDirection = new Vec3(0, -.7f, -1).Normalized();
			state.Silhouette = false;
			state.DepthCompare = DepthCompare.Less;
			state.DepthMask = true;
			state.VerticalFogColor = 0xdceaf0;
		}

		// collect renderable objects
		{
			sprites.Clear();
			models.Clear();

			// collect point shadows
			foreach (var actor in All<ICastPointShadow>())
			{
				var alpha = (actor as ICastPointShadow)!.PointShadowAlpha;
				if (alpha > 0 &&
					Camera.Frustum.Contains(actor.WorldBounds.Conflate(actor.WorldBounds - Vec3.UnitZ * 1000)))
					sprites.Add(Sprite.CreateShadowSprite(this, actor.Position + Vec3.UnitZ, alpha));
			}

			// collect models & sprites
			foreach (var actor in Actors)
			{
				if (!Camera.Frustum.Contains(actor.WorldBounds.Inflate(1)))
					continue;

				(actor as IHaveSprites)?.CollectSprites(sprites);
				(actor as IHaveModels)?.CollectModels(models);
			}

			// sort models by distance (for transparency)
			models.Sort((a, b) =>
				(int)((b.Actor.Position - Camera.Position).LengthSquared() -
				 (a.Actor.Position - Camera.Position).LengthSquared()));

			// perp all models
			foreach (var it in models)
				it.Model.Prepare();
		}

		// draw the skybox first
		{
			var shift = new Vec3(Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
			for (int i = 0; i < skyboxes.Count; i++)
			{
				skyboxes[i].Render(Camera,
				Matrix.CreateRotationZ(i * GeneralTimer * 0.01f) *
				Matrix.CreateScale(1, 1, 0.5f) *
				Matrix.CreateTranslation(shift), 300);
			}
		}

		// render solids
		RenderModels(ref state, models, ModelFlags.Terrain);

		// render silhouettes
		{
			var it = state;
			it.DepthCompare = DepthCompare.Greater;
			it.DepthMask = false;
			it.Silhouette = true;
			RenderModels(ref it, models, ModelFlags.Silhouette);
			state.Triangles = it.Triangles;
			state.Calls = it.Calls;
		}

		// render main models
		RenderModels(ref state, models, ModelFlags.Default);

		// perform post processing effects
		ApplyPostEffects();

		// render alpha threshold transparent stuff
		{
			state.CutoutMode = true;
			RenderModels(ref state, models, ModelFlags.Cutout);
			state.CutoutMode = false;
		}

		// render 2d sprites
		{
			spriteRenderer.Render(ref state, sprites, false);
			spriteRenderer.Render(ref state, sprites, true);
		}

		// render partially transparent models... must be sorted etc
		{
			state.DepthMask = false;
			RenderModels(ref state, models, ModelFlags.Transparent);
			state.DepthMask = true;
		}

		var selectedLocalBoundsFillColor = Color.Green * 0.4f;
		var selectedLocalBoundsOutlineColor = Color.Green;
		var selectedWorldBoundsOutlineColor = Color.Blue;
		const float selectedBoundsInflate = 0.25f;

		// Render selected actors bounding box
		foreach (var selected in SelectedActors)
		{
			var matrix = selected.Matrix;
			var bounds = selected.LocalBounds.Inflate(selectedBoundsInflate);

			batch3D.Box(bounds.Min, bounds.Max, selectedLocalBoundsFillColor, matrix);
		}
		batch3D.Render(ref state);
		batch3D.Clear();

		// Render outline on-top of everything else
		target.Clear(Color.Black, 1.0f, 0, ClearMask.Depth);
		foreach (var selected in SelectedActors)
		{
			// Scale thickness based on distance
			var lineThickness = Vec3.Distance(Camera.Position, selected.WorldBounds.Center) * 0.001f;

			// Transformed local bounds
			var matrix = selected.Matrix;
			var bounds = selected.LocalBounds.Inflate(selectedBoundsInflate);
			var v000 = bounds.Min;
			var v100 = bounds.Min with { X = bounds.Max.X };
			var v010 = bounds.Min with { Y = bounds.Max.Y };
			var v001 = bounds.Min with { Z = bounds.Max.Z };
			var v011 = bounds.Max with { X = bounds.Min.X };
			var v101 = bounds.Max with { Y = bounds.Min.Y };
			var v110 = bounds.Max with { Z = bounds.Min.Z };
			var v111 = bounds.Max;

			batch3D.Line(v000, v100, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v000, v010, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v000, v001, selectedLocalBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v111, v011, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v111, v101, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v111, v110, selectedLocalBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v010, v011, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v010, v110, selectedLocalBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v101, v100, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v101, v001, selectedLocalBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v100, v110, selectedLocalBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v001, v011, selectedLocalBoundsOutlineColor, matrix, lineThickness);

			// World bounds
			matrix = Matrix.Identity;
			bounds = selected.WorldBounds.Inflate(selectedBoundsInflate);
			v000 = bounds.Min;
			v100 = bounds.Min with { X = bounds.Max.X };
			v010 = bounds.Min with { Y = bounds.Max.Y };
			v001 = bounds.Min with { Z = bounds.Max.Z };
			v011 = bounds.Max with { X = bounds.Min.X };
			v101 = bounds.Max with { Y = bounds.Min.Y };
			v110 = bounds.Max with { Z = bounds.Min.Z };
			v111 = bounds.Max;

			batch3D.Line(v000, v100, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v000, v010, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v000, v001, selectedWorldBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v111, v011, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v111, v101, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v111, v110, selectedWorldBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v010, v011, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v010, v110, selectedWorldBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v101, v100, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v101, v001, selectedWorldBoundsOutlineColor, matrix, lineThickness);

			batch3D.Line(v100, v110, selectedWorldBoundsOutlineColor, matrix, lineThickness);
			batch3D.Line(v001, v011, selectedWorldBoundsOutlineColor, matrix, lineThickness);
		}
		batch3D.Render(ref state);
		batch3D.Clear();

		// Render gizmos on-top
		target.Clear(Color.Black, 1.0f, 0, ClearMask.Depth);
		{
			gizmo?.Render(batch3D);
		}
		batch3D.Render(ref state);
		batch3D.Clear();
		ApplyPostEffects();

		// ui
		{
			batch.SetSampler(new TextureSampler(TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));
			var bounds = new Rect(0, 0, target.Width, target.Height);
			var font = Language.Current.SpriteFont;

			// debug
			if (DebugDraw)
			{
				var updateMs = debugUpdTimer.Elapsed.TotalMilliseconds;
				var renderMs = lastDebugRndTime.TotalMilliseconds;
				var frameMs = debugFpsTimer.Elapsed.TotalMilliseconds;
				var fps = (int)(1000 / frameMs);
				debugFpsTimer.Restart();

				batch.Text(font, $"Draws: {state.Calls}, Tris: {state.Triangles}, Upd: {debugUpdateCount}", bounds.BottomLeft, new Vec2(0, 1), Color.Red);
				batch.Text(font, $"u:{updateMs:0.00}ms | r:{renderMs:0.00}ms | f:{frameMs:0.00}ms / {fps}fps", bounds.BottomLeft - new Vec2(0, font.LineHeight), new Vec2(0, 1), Color.Red);
				batch.Text(font, $"m: {Entry.Map}, c: {Entry.CheckPoint}, s: {Entry.Submap}", bounds.BottomLeft - new Vec2(0, font.LineHeight * 2), new Vec2(0, 1), Color.Red);
			}

			batch.Render(Camera.Target);
			batch.Clear();
		}

		lastDebugRndTime = debugRndTimer.Elapsed;
		debugRndTimer.Stop();
	}

	public bool ActorRayCast(in Vec3 point, in Vec3 direction, float distance, out RayHit hit, bool ignoreBackfaces = true, bool ignoreTransparent = false)
	{
		hit = default;
		float? closest = null;

		var p0 = point;
		var p1 = point + direction * distance;
		var box = new BoundingBox(Vec3.Min(p0, p1), Vec3.Max(p0, p1)).Inflate(1);

		foreach (var actor in Actors)
		{
			if (!actor.WorldBounds.Intersects(box))
				continue;

			// Don't re-select an actor
			if (SelectedActors.Contains(actor))
				continue;

			// TODO: Allow selecting decorations, since they're currently one giant object
			if (actor is Decoration or FloatingDecoration)
				continue;
			// Snow is not edited as an actor, but rather through the environment settings
			if (actor is Snow)
				continue;

			if (actor is not Solid solid)
			{
				if (ModUtils.RayIntersectOBB(point, direction, actor.LocalBounds, actor.Matrix, out float dist))
				{
					// too far away
					if (dist > distance)
						continue;

					hit.Intersections++;

					// we have a closer value
					if (closest.HasValue && dist > closest.Value)
						continue;

					// store as closest
					hit.Point = point + direction * dist;
					hit.Distance = dist;
					hit.Actor = actor;
					closest = dist;
				}

				continue;
			}

			// Special handling for solid to properly check against mesh
			if (!solid.Collidable || solid.Destroying)
				continue;

			if (solid.Transparent && ignoreTransparent)
				continue;

			var verts = solid.WorldVertices;
			var faces = solid.WorldFaces;

			foreach (var face in faces)
			{
				// only do planes that are facing against us
				if (ignoreBackfaces && Vec3.Dot(face.Plane.Normal, direction) >= 0)
					continue;

				// ignore faces that are definitely too far away
				if (point.DistanceToPlane(face.Plane) > distance)
					continue;

				// check against each triangle in the face
				for (int i = 0; i < face.VertexCount - 2; i++)
				{
					if (Utils.RayIntersectsTriangle(point, direction,
							verts[face.VertexStart + 0],
							verts[face.VertexStart + i + 1],
							verts[face.VertexStart + i + 2], out float dist))
					{
						// too far away
						if (dist > distance)
							continue;

						hit.Intersections++;

						// we have a closer value
						if (closest.HasValue && dist > closest.Value)
							continue;

						// store as closest
						hit.Point = point + direction * dist;
						hit.Normal = face.Plane.Normal;
						hit.Distance = dist;
						hit.Actor = solid;
						closest = dist;
						break;
					}
				}
			}
		}

		return closest.HasValue;
	}
}
