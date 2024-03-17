namespace Celeste64.Mod.Editor;

public abstract class SelectionType
{
	public abstract IEnumerable<SelectionTarget> Targets { get; }
	public abstract IEnumerable<Gizmo> Gizmos { get; }
}