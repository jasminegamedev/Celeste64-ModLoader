using Celeste64.Mod.Helpers;

namespace Celeste64.Mod.Editor;

public class WorldRenderer
{
	private Camera camera = new();
	private Vec3 cameraPos = new(0, -10, 0);
	private Vec2 cameraRot = new(0, 0);
	
	private readonly List<Definition> definitions = [];
	
	public WorldRenderer()
	{
		camera.NearPlane = 5;
		camera.FarPlane = 800;
		camera.Position = new Vec3(0, -10, 0);
		camera.FOVMultiplier = 1;
		
		definitions.Add(new TestDefinition());
	}
	
	public void Update()
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
	
	public void Render(Target target)
	{
		camera.Target = target;
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
		
		foreach (var definition in definitions)
		{
			definition.Render(ref state);
		}
		
		Log.Info($"Editor: {state.Calls} draw calls with {state.Triangles} triangles");
	}
}