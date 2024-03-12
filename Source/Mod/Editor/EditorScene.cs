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
			if (Matrix.Invert(Camera.ViewProjection, out var inverse) && Camera.Target != null)
			{
				// The top-left of the image might not be the top-left of the window, when using non 16:9 aspect ratios
				var scale = Math.Min(App.WidthInPixels / (float)Camera.Target.Width, App.HeightInPixels / (float)Camera.Target.Height);
				var imageRelativePos = Input.Mouse.Position - (App.SizeInPixels / 2 - Camera.Target.Bounds.Size / 2 * scale);
				// Convert into normalized-device-coordinates
				var ncdPos = imageRelativePos / (Camera.Target.Bounds.Size / 2 * scale) - Vec2.One;
				// Turn it back into a world position (with distance 0 from the camera)
				var worldPos = Vec4.Transform(new Vec4(ncdPos, 0.0f, 1.0f), inverse);
				worldPos.X /= worldPos.W;
				worldPos.Y /= worldPos.W;
				worldPos.Z /= worldPos.W;
				var direction = new Vec3(worldPos.X, worldPos.Y, worldPos.Z) - Camera.Position;
				// direction *= 10.0f;
				direction = direction.Normalized();
				
				Log.Info($"Casting at {Camera.Position} into {direction} (vs {Camera.Forward}");
				if (ActorRayCast(Camera.Position, direction, 10000.0f, out var hit, ignoreBackfaces: false))
				{
					Log.Info($"hit Point: {hit.Point} Normal: {hit.Normal} Distance: {hit.Distance} Actor: {hit.Actor} Intersections: {hit.Intersections}");
					Selected = hit.Actor;
				}
			}
		}
		
		// Don't call base.Update, since we don't want the actors to update
		// Instead we manually call only the things which we want for the editor
		ResolveChanges();
		
		// worldRenderer.Update(this);
	}
	
	public override void Render(Target target)
	{
		// target.Clear(Color.Black, 1.0f, 0, ClearMask.All);
		// worldRenderer.Render(this, target);
		base.Render(target);
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
					
					Log.Info($"intersected non-solid {actor} @ {distEnter} / {distExit}");
					
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
				if (Utils.DistanceToPlane(point, face.Plane) > distance)
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

						Log.Info($"intersected solid {actor} @ {dist}");
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