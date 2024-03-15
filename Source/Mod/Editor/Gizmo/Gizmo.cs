namespace Celeste64.Mod.Editor;

public abstract class Gizmo
{
	public abstract void Render(ref RenderState state);
}

public enum GizmoTarget
{
	None,
	AxisX, AxisY, AxisZ,
	PlaneXZ, PlaneYZ, PlaneXY,
	CubeXYZ,
}
