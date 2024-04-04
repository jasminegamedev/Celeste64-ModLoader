namespace Celeste64.Mod.Editor;

public abstract class Tool
{
	public abstract string Name { get; }
	public abstract Gizmo[] Gizmos { get; }
	public abstract bool EnableSelection { get; }
	
	public virtual void Awake(EditorWorld editor) { }
}
