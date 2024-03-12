namespace Celeste64.Mod.Editor;

public abstract class ActorDefinition
{
	public bool Dirty = false;
	
	public abstract void Load(World world);
}