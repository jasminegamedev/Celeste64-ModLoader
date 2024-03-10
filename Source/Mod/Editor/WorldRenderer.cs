using Celeste64.Mod.Helpers;

namespace Celeste64.Mod.Editor;

public class WorldRenderer
{
	private Camera camera = new();
	private Vec3 cameraPos = new(0, -10, 0);
	private Vec2 cameraRot = new(0, 0);
	
	private Target? worldTarget = null;
	private readonly Batcher batch = new();
	
	public WorldRenderer()
	{
		camera.NearPlane = 5;
		camera.FarPlane = 800;
		camera.Position = new Vec3(0, -10, 0);
		camera.FOVMultiplier = 1;
	}
	
	public void Update(EditorScene editor)
	{
		// Camera movement
		var cameraForward = new Vec3(
			MathF.Sin(cameraRot.X),
			MathF.Cos(cameraRot.X),
			0.0f);
		var cameraRight = new Vec3(
			MathF.Sin(cameraRot.X - Calc.HalfPI),
			MathF.Cos(cameraRot.X - Calc.HalfPI),
			0.0f);
		
		float moveSpeed = 30.0f;
		
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
		float rotateSpeed = 15.0f * Calc.DegToRad;
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
		camera.Position = cameraPos;
		camera.LookAt = cameraPos + forward;
	}	
	
	public void Render(EditorScene editor, Target target)
	{
		// TODO: Maybe render at a higher resolution in the editor?
		if (worldTarget == null || worldTarget.Width != target.Width || worldTarget.Height != target.Height)
		{
			worldTarget?.Dispose();
			worldTarget = new Target(target.Width, target.Height, [TextureFormat.Color, TextureFormat.R8, TextureFormat.Depth24Stencil8]);
		}
		worldTarget.Clear(Color.Black, 1.0f, 0, ClearMask.All);
		
		camera.Target = worldTarget;
		EditorRenderState state = new();
		{
			state.Camera = camera;
			state.ModelMatrix = Matrix.Identity;
			state.SunDirection = new Vec3(0, -.7f, -1).Normalized();
			// state.Silhouette = false;
			state.DepthCompare = DepthCompare.Less;
			state.DepthMask = true;
			// state.VerticalFogColor = 0xdceaf0;
		}
		
		for (int i = 0; i < editor.Definitions.Count; i++)
		{
			state.ObjectID = i + 1; // Use 0 as "nothing selected"
			editor.Definitions[i].Render(ref state);	
		}
		
		// Try to select the object under the cursor
		if (Input.Mouse.LeftPressed)
		{
			// The top-left of the image might not be the top-left of the window, when using non 16:9 aspect ratios
			var scale = Math.Min(App.WidthInPixels / (float)target.Width, App.HeightInPixels / (float)target.Height);
			var imageRelativePos = Input.Mouse.Position - (App.SizeInPixels / 2 - target.Bounds.Size / 2 * scale);
			// Convert it into a pixel position inside the target
			var pixelPos = imageRelativePos / scale;
			// Round to integer values
			pixelPos = new Vec2(MathF.Round(pixelPos.X), MathF.Round(pixelPos.Y));
			
			if (pixelPos.X >= 0 && pixelPos.Y >= 0 && pixelPos.X < worldTarget.Width && pixelPos.Y < worldTarget.Height)
			{
				var data = new byte[worldTarget.Width * worldTarget.Height];
				worldTarget.Attachments[1].GetData<byte>(data);
				
				// NOTE: OpenGL flips the image vertically
				byte objectID = data[worldTarget.Width * (target.Height - (int)pixelPos.Y - 1) + (int)pixelPos.X];
				editor.Selected = objectID == 0 
					? null // Nothing selected 
					: editor.Definitions[objectID - 1];
			}
		}
		
		// Render to the main target
		batch.Image(worldTarget.Attachments[0], Color.White);
		batch.Render(target);
		batch.Clear();
	}
}