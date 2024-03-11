using System.Reflection;
using System.Reflection.Emit;

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
			// Header
			var magic = reader.ReadBytes(4);
			if (!magic.SequenceEqual(FujiMapWriter.FormatMagic))
			{
				isMalformed = true;
				readExceptionMessage = $"Invalid magic bytes! Found '{(char)magic[0]}{(char)magic[1]}{(char)magic[2]}{(char)magic[3]}'";
				return;
			}
			var version = reader.ReadByte(); // Not currently used

			// Metadata
			Skybox = reader.ReadString();
			SnowAmount = reader.ReadSingle();
			SnowWind = reader.ReadVec3();
			Ambience = reader.ReadString();
			Music = reader.ReadString();
			
			// Definitions
			var defCount = reader.ReadInt32();
			for (int i = 0; i < defCount; i++)
			{
				var fullName = reader.ReadString();
				var defType = Assembly.GetExecutingAssembly().GetType(fullName)!;
				var def = Activator.CreateInstance(defType);
				
				Log.Info($"Def: {def}");
                			
                var props = defType
                	.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                	.Where(prop => !prop.HasAttr<SerializeIgnoreAttribute>());
                
                foreach (var prop in props)
                {
                	if (prop.GetCustomAttribute<SerializeCustomAttribute>() is { } custom)
                	{
                		prop.SetValue(def, custom.Deserialize(reader));
                		continue;
                	}
                	
	                // Primitives
	                if (prop.PropertyType == typeof(bool)) 
		                prop.SetValue(def, reader.ReadBoolean());
					else if (prop.PropertyType == typeof(byte)) 
						prop.SetValue(def, reader.ReadByte());
					else if (prop.PropertyType == typeof(byte[]))
						prop.SetValue(def, reader.ReadBytes(reader.Read7BitEncodedInt()));
					else if (prop.PropertyType == typeof(char)) 
						prop.SetValue(def, reader.ReadChar());
					else if (prop.PropertyType == typeof(char[])) 
						prop.SetValue(def, reader.ReadChars(reader.Read7BitEncodedInt()));
					else if (prop.PropertyType == typeof(decimal))
						prop.SetValue(def, reader.ReadDecimal());
					else if (prop.PropertyType == typeof(double))
		                prop.SetValue(def, reader.ReadDouble());
					else if (prop.PropertyType == typeof(float)) 
		                prop.SetValue(def, reader.ReadSingle());
					else if (prop.PropertyType == typeof(int))
		                prop.SetValue(def, reader.ReadInt32());
					else if (prop.PropertyType == typeof(long)) 
		                prop.SetValue(def, reader.ReadInt64());
					else if (prop.PropertyType == typeof(sbyte)) 
		                prop.SetValue(def, reader.ReadSByte());
					else if (prop.PropertyType == typeof(short))
		                prop.SetValue(def, reader.ReadInt16());
					else if (prop.PropertyType == typeof(Half))
		                prop.SetValue(def, reader.ReadHalf());
	                // Special support
	                else if (prop.PropertyType == typeof(Vec2))
		                prop.SetValue(def, reader.ReadVec2());
	                else if (prop.PropertyType == typeof(Vec3))
		                prop.SetValue(def, reader.ReadVec3());
	                else if (prop.PropertyType == typeof(Color))
		                prop.SetValue(def, reader.ReadColor());
                	
                	Log.Info($" - {prop.Name}: {prop.GetValue(def)}");
                }
			}
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