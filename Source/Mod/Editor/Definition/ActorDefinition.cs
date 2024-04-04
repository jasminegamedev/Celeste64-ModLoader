namespace Celeste64.Mod.Editor;

public abstract class ActorDefinition
{
	public event Action OnUpdated = () => {};
	internal void Updated() => OnUpdated();
	
	public bool Dirty = true;
	public SelectionType[] SelectionTypes { get; init; } = [];
	
	public abstract Actor[] Load(World.WorldType type);
}
