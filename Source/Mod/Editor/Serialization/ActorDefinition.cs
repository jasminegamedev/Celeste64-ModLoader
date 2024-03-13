namespace Celeste64.Mod.Editor;

public abstract class ActorDefinition
{
	public bool Dirty = true;

	public abstract Actor[] Load(World.WorldType type);
}
