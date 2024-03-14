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

	public string? Skybox { get; init; }
	public float SnowAmount { get; init; }
	public Vec3 SnowWind { get; init; }
	public string? Music { get; init; }
	public string? Ambience { get; init; }

	public abstract void Load(World world);
}
