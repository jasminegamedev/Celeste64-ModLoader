using System.Reflection;
using System.Reflection.Emit;

namespace Celeste64.Mod.Editor;

/// <summary>
/// Map parser for the custom Fuji map format.
/// </summary>
public class FujiMap : Map
{
	/// <summary>
	/// Magic 4 bytes at the start of the file, to indicate the format.
	/// </summary>
	private static readonly byte[] FormatMagic = [(byte)'F', (byte)'U', (byte)'J', (byte)'I'];

	/// <summary>
	/// Current version of the map format. Needs to be incremented with every change to it.
	/// </summary>
	private const byte FormatVersion = 1;

	public readonly string? FullPath;
	public readonly List<ActorDefinition> Definitions = [];

	public FujiMap(string name, string virtPath, Stream stream, string? fullPath)
	{
		Name = name;
		Filename = virtPath;
		Folder = Path.GetDirectoryName(virtPath) ?? string.Empty;
		FullPath = fullPath;

		using var reader = new BinaryReader(stream);

		try
		{
			// Header
			var magic = reader.ReadBytes(4);
			if (!magic.SequenceEqual(FormatMagic))
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
				// Get the definition data type, by the full name
				var fullName = reader.ReadString();
				var defType = Assembly.GetExecutingAssembly().GetType(fullName)!;
				var def = Activator.CreateInstance(defType);

				Log.Info($"Reading def: {def}");

				var props = defType
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(prop => !prop.HasAttr<PropertyIgnoreAttribute>());

				foreach (var prop in props)
				{
					if (prop.GetCustomAttribute<PropertyCustomAttribute>() is { } custom)
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
					else if (prop.PropertyType == typeof(string))
						prop.SetValue(def, reader.ReadString());
					// Special support
					else if (prop.PropertyType == typeof(Vec2))
						prop.SetValue(def, reader.ReadVec2());
					else if (prop.PropertyType == typeof(Vec3))
						prop.SetValue(def, reader.ReadVec3());
					else if (prop.PropertyType == typeof(Color))
						prop.SetValue(def, reader.ReadColor());

					Log.Info($" - {prop.Name}: {prop.GetValue(def)}");
				}

				Definitions.Add((ActorDefinition)def!);
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

	public void SaveToFile()
	{
		// Only allow saving when the mod is a folder
		if (FullPath == null)
		{
			Log.Warning("Tried to save zipped map file");
			return;
		}

		using var fs = File.Open(FullPath, FileMode.Create);
		using var writer = new BinaryWriter(fs);

		// Header
		writer.Write(FormatMagic);
		writer.Write(FormatVersion);

		// Metadata
		// Skybox
		writer.Write("city");
		// Snow amount
		writer.Write(1.0f);
		// Snow direction
		writer.Write(new Vec3(0.0f, 0.0f, -1.0f));
		// Ambience
		writer.Write("mountain");
		// Music
		writer.Write("mus_lvl1");

		// Definitions
		writer.Write(Definitions.Count);
		foreach (var def in Definitions)
		{
			Log.Info($"Writing def: {def}");
			writer.Write(def.GetType().FullName!);

			var props = def.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(prop => !prop.HasAttr<PropertyIgnoreAttribute>());

			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<PropertyCustomAttribute>() is { } custom)
				{
					custom.Serialize(prop.GetValue(def)!, writer);
					continue;
				}

				switch (prop.GetValue(def))
				{
					// Primitives
					case bool v:
						writer.Write(v);
						break;
					case byte v:
						writer.Write(v);
						break;
					case byte[] v:
						writer.Write7BitEncodedInt(v.Length);
						writer.Write(v);
						break;
					case char v:
						writer.Write(v);
						break;
					case char[] v:
						writer.Write7BitEncodedInt(v.Length);
						writer.Write(v);
						break;
					case decimal v:
						writer.Write(v);
						break;
					case double v:
						writer.Write(v);
						break;
					case float v:
						writer.Write(v);
						break;
					case int v:
						writer.Write(v);
						break;
					case long v:
						writer.Write(v);
						break;
					case sbyte v:
						writer.Write(v);
						break;
					case short v:
						writer.Write(v);
						break;
					case Half v:
						writer.Write(v);
						break;
					case string v:
						writer.Write(v);
						break;

					// Special support
					case Vec2 v:
						writer.Write(v);
						break;
					case Vec3 v:
						writer.Write(v);
						break;
					case Color v:
						writer.Write(v);
						break;

					default:
						throw new Exception($"Property '{prop.Name}' of type {prop.PropertyType} from definition '{def}' cannot be serialized");
				}

				Log.Info($" * {prop.Name}: {prop.GetValue(def)}");
			}
		}
	}

	public override void Load(World world)
	{
		foreach (var def in Definitions)
		{
			var newActors = def.Load(world.Type);
			foreach (var actor in newActors)
			{
				world.Add(actor);
			}
		}
		world.Add(new Player { Position = new Vec3(0, 0, 100) });
	}
}
