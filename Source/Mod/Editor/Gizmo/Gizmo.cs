namespace Celeste64.Mod.Editor;

public abstract class Gizmo
{
	public abstract Matrix Transform { get; }

	public abstract void Render(Batcher3D batch3D);
	public abstract bool RaycastCheck(Vec3 origin, Vec3 direction);
	
	public abstract void DragStart();
	public abstract void Drag(Vec2 mouseDelta, Vec3 mouseRay);
}

public enum GizmoTarget
{
	None,
	AxisX, AxisY, AxisZ,
	PlaneXZ, PlaneYZ, PlaneXY,
	CubeXYZ,
}
