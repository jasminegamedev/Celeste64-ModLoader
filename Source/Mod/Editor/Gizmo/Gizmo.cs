namespace Celeste64.Mod.Editor;

public abstract class Gizmo
{
	public abstract void Render(Batcher3D batch3D);
}

public enum GizmoTarget
{
	None,
	AxisX, AxisY, AxisZ,
	PlaneXZ, PlaneYZ, PlaneXY,
	CubeXYZ,
}
