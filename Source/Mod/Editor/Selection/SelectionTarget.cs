namespace Celeste64.Mod.Editor;

public abstract class SelectionTarget
{
	public abstract Matrix Transform { get; }
	public abstract BoundingBox Bounds { get; }
	
	public bool IsHovered { get; internal set; } = false;
	public bool IsDragged { get; internal set; } = false;
	
	public virtual void Update() { }
	public virtual void Render(ref RenderState state, Batcher3D batch3D) { }
	
	public virtual void Hovered() { }
	public virtual void Selected() { }
	public virtual void Dragged(Vec2 mouseDelta, Vec3 mouseRay) { }
}

// TODO: Is this a good name? It provides Actions so you don't need to create a subclass for every type 
public class SimpleSelectionTarget(Matrix transform, BoundingBox bounds) : SelectionTarget
{
	public override Matrix Transform { get; } = transform;
	public override BoundingBox Bounds { get; } = bounds;
	
	public Action? OnHovered = null;
    public Action? OnSelected = null;
    public Action<Vec2, Vec3>? OnDragged = null;

    public override void Hovered() => OnHovered?.Invoke();
    public override void Selected() => OnSelected?.Invoke();
    public override void Dragged(Vec2 mouseDelta, Vec3 mouseRay) => OnDragged?.Invoke(mouseDelta, mouseRay);
}

