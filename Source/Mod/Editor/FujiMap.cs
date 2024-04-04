using System.Collections;
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
				var defType = Assembly.GetExecutingAssembly().GetType(fullName);
				if (defType is null || !defType.IsAssignableTo(typeof(ActorDefinition)))
				{
					isMalformed = true;
					readExceptionMessage = $"The definition type {fullName} is invalid";
					return;
				}
				
				var def = Activator.CreateInstance(defType);

				Log.Info($"Reading def: {def}");

				var props = defType
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(prop => !prop.HasAttr<IgnorePropertyAttribute>() && prop.Name != nameof(ActorDefinition.SelectionTypes));

				foreach (var prop in props)
				{
					if (prop.GetCustomAttribute<CustomPropertyAttribute>() is { } custom)
					{
						prop.SetValue(def, custom.Deserialize(reader));
						continue;
					}
					
					if (DeserializeObject(prop.PropertyType, reader) is not { } obj)
						throw new Exception($"Property '{prop.Name}' of type {prop.PropertyType} from definition '{def}' cannot be deserialized");
					
					prop.SetValue(def, obj);

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
				.Where(prop => !prop.HasAttr<IgnorePropertyAttribute>() && prop.Name != nameof(ActorDefinition.SelectionTypes));

			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<CustomPropertyAttribute>() is { } custom)
				{
					custom.Serialize(prop.GetValue(def)!, writer);
					continue;
				}

				if (!SerializeObject(prop.GetValue(def), writer))
					throw new Exception($"Property '{prop.Name}' of type {prop.PropertyType} from definition '{def}' cannot be serialized");

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
	
	private bool SerializeObject(object? obj, BinaryWriter writer)
	{
		switch (obj)
		{
			// Primitives
			case bool v:
				writer.Write(v);
				break;
			case byte v:
				writer.Write(v);
				break;
			case char v:
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

			// Collections
			case IList v:
				writer.Write7BitEncodedInt(v.Count);
				foreach (var item in v)
					SerializeObject(item, writer);
				break;
				
			default:
				return false;
		}
		
		return true;
	}
	
	private object? DeserializeObject(Type type, BinaryReader reader)
	{
		// Primitives
		if (type == typeof(bool))
			return reader.ReadBoolean();
		if (type == typeof(byte))
			return reader.ReadByte();
		if (type == typeof(byte[]))
			return reader.ReadBytes(reader.Read7BitEncodedInt());
		if (type == typeof(char))
			return reader.ReadChar();
		if (type == typeof(char[]))
			return reader.ReadChars(reader.Read7BitEncodedInt());
		if (type == typeof(decimal))
			return reader.ReadDecimal();
		if (type == typeof(double))
			return reader.ReadDouble();
		if (type == typeof(float))
			return reader.ReadSingle();
		if (type == typeof(int))
			return reader.ReadInt32();
		if (type == typeof(long))
			return reader.ReadInt64();
		if (type == typeof(sbyte))
			return reader.ReadSByte();
		if (type == typeof(short))
			return reader.ReadInt16();
		if (type == typeof(Half))
			return reader.ReadHalf();
		if (type == typeof(string))
			return reader.ReadString();
		// Special support
		if (type == typeof(Vec2))
			return reader.ReadVec2();
		if (type == typeof(Vec3))
			return reader.ReadVec3();
		if (type == typeof(Color))
			return reader.ReadColor();
		// Collections
		if (type.IsAssignableTo(typeof(IList)) && type.IsGenericType)
		{
			var itemType = type.GenericTypeArguments[0];
			var list = (IList)Activator.CreateInstance(type)!;
			int count = reader.Read7BitEncodedInt();
			for (int i = 0; i < count; i++)
				list.Add(DeserializeObject(itemType, reader));
			return list;
		}
		
		return null;
	}
}
