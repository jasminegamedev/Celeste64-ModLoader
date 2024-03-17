namespace Celeste64.Mod.Editor;

public class SelectionTarget
{
	public required Matrix Transform;
	public required BoundingBox Bounds;
	
	public Action? OnHovered = null;
	public Action? OnSelected = null;
	public Action<Vec2, Vec3>? OnDragged = null;
}
