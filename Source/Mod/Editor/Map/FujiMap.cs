namespace Celeste64.Mod.Editor;

/// <summary>
/// Map parser for the custom Fuji map format.
/// </summary>
public class FujiMap : Map
{
	public FujiMap(string name, string virtPath, Stream stream)
	{
		Name = name;
		Filename = virtPath;
		Folder = Path.GetDirectoryName(virtPath) ?? string.Empty;
	}
	
	public override void Load(World world)
	{
		world.Add(new Player { Position = new Vec3(0, 0, 1000) });
	}
}