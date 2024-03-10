namespace Celeste64.Mod.Editor;

public class WorldRenderer
{
	private Camera camera = new();
	private readonly List<Definition> definitions = [];
	
	public WorldRenderer()
	{
		camera.NearPlane = 20;
		camera.FarPlane = 800;
		camera.Position = new Vec3(0, -10, 0);
		camera.LookAt = Vec3.Zero;
		camera.FOVMultiplier = 1;
		
		definitions.Add(new TestDefinition());
	}
	
	public void Update()
	{
		
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