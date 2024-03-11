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
		
		using var reader = new BinaryReader(stream);
		
		try
		{
			#region Metadata
		
			var magic = reader.ReadBytes(4);
			if (!magic.SequenceEqual(FujiMapWriter.FormatMagic))
			{
				isMalformed = true;
				readExceptionMessage = $"Invalid magic bytes! Found '{(char)magic[0]}{(char)magic[1]}{(char)magic[2]}{(char)magic[3]}'";
				return;
			}
		
			var version = reader.ReadByte(); // Not currently used
		
			#endregion

			#region Metadata

			Skybox = reader.ReadString();
			SnowAmount = reader.ReadSingle();
			SnowWind = reader.ReadVec3();
			Ambience = reader.ReadString();
			Music = reader.ReadString();

			#endregion
		}
		catch (Exception ex)
		{
			isMalformed = true;
			readExceptionMessage = ex.Message;
			
			Log.Error($"Failed to load map {name}, more details below.");
			Log.Error(ex.ToString());
		}
	}
	
	public override void Load(World world)
	{
		world.Add(new Player { Position = new Vec3(0, 0, 1000) });
	}
}