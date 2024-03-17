namespace Celeste64.Mod.Editor;

public class SelectionTarget
{
	public required Matrix Transform;
	public required BoundingBox Bounds;
	
	public required Action OnSelected;
}
