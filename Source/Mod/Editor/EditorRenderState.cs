namespace Celeste64.Mod.Editor;

public class EditorRenderState
{
	public Camera Camera;
	public Matrix ModelMatrix;
	
	// public bool Silhouette;
	public Vec3 SunDirection;
	// public Color VerticalFogColor;

	public DepthCompare DepthCompare;
	public bool DepthMask;
	public bool CutoutMode;
	
	public int ObjectID;
	
	// Stats
	public int Calls;
	public int Triangles;

	public void ApplyToMaterial(EditorMaterial mat, in Matrix localTransformation)
	{
		if (mat.Shader == null)
			return;

		mat.Model = localTransformation * ModelMatrix;
		mat.MVP = mat.Model * Camera.ViewProjection;
		
		mat.NearPlane = Camera.NearPlane;
		mat.FarPlane = Camera.FarPlane;
		// mat.Silhouette = Silhouette;
		// mat.Time = (float)Time.Duration.TotalSeconds;
		mat.SunDirection = SunDirection;
		// mat.VerticalFogColor = VerticalFogColor;
		// mat.Cutout = CutoutMode;
		mat.ObjectID = ObjectID;
	}
}