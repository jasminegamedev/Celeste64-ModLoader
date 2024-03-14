namespace Celeste64;

/// <summary>
/// Common base calls for all map types.
/// The vanilla map parser was renamed to SledgeMap.
/// </summary>
public abstract class Map
{
	public bool isMalformed { get; init; } = false;
	public string? readExceptionMessage { get; init; } = null;

	public string Name { get; init; }
	public string Filename { get; init; }
	public string Folder { get; init; }

	public string? Skybox { get; internal set; }
	public float SnowAmount { get; internal set; }
	public Vec3 SnowWind { get; internal set; }
	public string? Music { get; internal set; }
	public string? Ambience { get; internal set; }

	public abstract void Load(World world);
}
