using System.Reflection;
using Celeste64.Mod.Helpers;

namespace Celeste64.Mod.Editor;

public class EditorScene : World
{
	private const float EditorResolutionScale = 3.0f; 
	
	internal readonly ImGuiHandler[] Handlers = [
		new TestWindow(),
	];
	
	public Actor? Selected { internal set; get; } = null;
	
	private Vec3 cameraPos = new(0, -10, 0);
	private Vec2 cameraRot = new(0, 0);
	
	// private readonly WorldRenderer worldRenderer = new();
	private readonly Batcher3D batch3D = new();
	
	internal EditorScene(EntryInfo entry) : base(entry)
	{
		Camera.NearPlane = 0.1f;
		Camera.FarPlane = 4000; // Increase render distance
		Camera.FOVMultiplier = 1.25f;
		
		//Definitions.Add(new TestEditorDefinition());
		
		// Load the map
		// if (Assets.Maps[entry.Map] is not FujiMap map)
		// {
		// 	// Not a Fuji map, return to level
		// 	Game.Instance.scenes.Pop();
		// 	Game.Instance.scenes.Push(new World(Entry));
		// 	return;
		// }
		//
		// foreach (var defData in map.DefinitionData)
		// {
		// 	var defType = Assembly.GetExecutingAssembly().GetType(defData.DefinitionFullName)!;
		// 	var def = (EditorDefinition)Activator.CreateInstance(defType)!;
		// 	def._Data = defData;
		// 	Definitions.Add(def);
		// }
	}

	private float previousScale = 1.0f;
	public override void Entered()
	{
		previousScale = Game.ResolutionScale;
		Game.ResolutionScale = EditorResolutionScale;
	}
	public override void Exited()
	{
		Game.ResolutionScale = previousScale;
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
		
		if (Input.Keyboard.Ctrl && Input.Keyboard.Pressed(Keys.S))
		{
			// TODO: Dont actually hardcode this lol
			var path = "/media/Storage/Code/C#/Fuji/Mods/Template-BasicCassetteLevel/Maps/test.bin";
			Log.Info($"Saving map to '{path}'");

			using var fs = File.Open(path, FileMode.Create);
			FujiMapWriter.WriteTo(this, fs);
			
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
			cameraPos += cameraForward * moveSpeed * Time.Delta;
		if (Input.Keyboard.Down(Keys.S))
			cameraPos -= cameraForward * moveSpeed * Time.Delta;
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
		if (Input.Mouse.Down(MouseButtons.Right))
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
		if (Input.Mouse.LeftPressed)
		{
			if (Camera.Target != null &&
				Matrix.Invert(Camera.Projection, out var inverseProj) && 
			    Matrix.Invert(Camera.View, out var inverseView))
			{
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
				
				if (ActorRayCast(Camera.Position, direction, 10000.0f, out var hit, ignoreBackfaces: false))
					Selected = hit.Actor;
				else
					Selected = null;
			}
		}
		
		// Don't call base.Update, since we don't want the actors to update
		// Instead we manually call only the things which we want for the editor

		// toggle debug draw
		if (Input.Keyboard.Pressed(Keys.F1))
			DebugDraw = !DebugDraw;
		
		// add / remove actors
		ResolveChanges();
	}
	
	public override void Render(Target target)
	{
		// We copy and modify World.Render, since thats easier
		
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
		
		// Render selected actor bounding box on-top of everything else
		if (Selected is { } selected)
		{
			var lineColor = Color.Green;
			var innerColor = Color.Green * 0.4f;
			var lineThickness = 0.1f;
			var inflate = 0.25f;
			var matrix = Matrix.CreateTranslation(selected.Position);
			
			var bounds = selected.LocalBounds.Inflate(inflate);
			var v000 = bounds.Min;
			var v100 = bounds.Min with { X = bounds.Max.X };
			var v010 = bounds.Min with { Y = bounds.Max.Y };
			var v001 = bounds.Min with { Z = bounds.Max.Z };
			var v011 = bounds.Max with { X = bounds.Min.X };
			var v101 = bounds.Max with { Y = bounds.Min.Y };
			var v110 = bounds.Max with { Z = bounds.Min.Z };
			var v111 = bounds.Max;
			
			batch3D.Box(v000, v111, innerColor, matrix);
			batch3D.Render(ref state);
			batch3D.Clear();
			
			// Ignore depth for outline
			state.Camera.Target.Clear(Color.Black, 1.0f, 0, ClearMask.Depth);
			
			batch3D.Line(v000, v100, lineColor, matrix, lineThickness);
			batch3D.Line(v000, v010, lineColor, matrix, lineThickness);
			batch3D.Line(v000, v001, lineColor, matrix, lineThickness);

			batch3D.Line(v111, v011, lineColor, matrix, lineThickness);
			batch3D.Line(v111, v101, lineColor, matrix, lineThickness);
			batch3D.Line(v111, v110, lineColor, matrix, lineThickness);

			batch3D.Line(v010, v011, lineColor, matrix, lineThickness);
			batch3D.Line(v010, v110, lineColor, matrix, lineThickness);

			batch3D.Line(v101, v100, lineColor, matrix, lineThickness);
			batch3D.Line(v101, v001, lineColor, matrix, lineThickness);

			batch3D.Line(v100, v110, lineColor, matrix, lineThickness);
			batch3D.Line(v001, v011, lineColor, matrix, lineThickness);
			
			batch3D.Render(ref state);
			batch3D.Clear();
		}

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
				var fps = (int)(1000/frameMs);
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
			
			// TODO: Allow selecting decorations, since they're currently one giant object
			if (actor is Decoration or FloatingDecoration)
				continue;
			
			if (actor is not Solid solid)
			{
				if (ModUtils.RayIntersectsBox(point, direction, actor.WorldBounds, out float distEnter, out float distExit))
				{
					// too far away
					if (distEnter > distance)
						continue;
					
					hit.Intersections++;

					// we have a closer value
					if (closest.HasValue && distEnter > closest.Value)
						continue;
					
					// store as closest
					hit.Point = point + direction * distEnter;
					hit.Distance = distEnter;
					hit.Actor = actor;
					closest = distEnter;
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
				for (int i = 0; i < face.VertexCount - 2; i ++)
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

internal class EditorHandler : ImGuiHandler
{
	
}