namespace Celeste64.Mod.Editor;

public abstract class Gizmo
{
	public abstract IEnumerable<SelectionTarget> SelectionTargets { get; }

	public abstract void Render(Batcher3D batch3D);
}
