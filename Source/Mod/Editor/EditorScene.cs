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
			Game.Instance.scenes.Pop();
			Game.Instance.scenes.Push(new World(Entry));
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
}

internal class EditorHandler : ImGuiHandler
{
	
}