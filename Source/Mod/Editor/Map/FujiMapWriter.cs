using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using MonoMod.Utils;

namespace Celeste64.Mod.Editor;

public static class FujiMapWriter
{
	/// <summary>
	/// Magic 4 bytes at the start of the file, to indicate the format.
	/// </summary>
	public static readonly byte[] FormatMagic = [(byte)'F', (byte)'U', (byte)'J', (byte)'I'];

	/// <summary>
	/// Current version of the map format. Needs to be incremented with every change to it.
	/// </summary>
	public const byte FormatVersion = 1;
	
	public static void WriteTo(EditorScene editor, Stream stream)
	{
		using var writer = new BinaryWriter(stream);
		
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
		writer.Write(editor.Definitions.Count);
		foreach (var def in editor.Definitions)
		{
			Log.Info($"Def: {def}");
			writer.Write(def.DataType.FullName!);
			
			var props = def.DataType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(prop => !prop.HasAttr<SerializeIgnoreAttribute>());
			
			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<SerializeCustomAttribute>() is { } custom)
				{
					custom.Serialize(prop.GetValue(def._Data)!, writer);
					continue;
				}
				
				switch (prop.GetValue(def._Data))
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
						throw new Exception($"Property '{prop.Name}' of type {prop.PropertyType} from definition '{def._Data}' cannot be serialized");
				}
				
				Log.Info($" - {prop.Name}: {prop.GetValue(def._Data)}");
			}
		}
	}
	
	public static void Write(this BinaryWriter writer, Vec2 value)
	{
		writer.Write(value.X);
		writer.Write(value.Y);
	}
	public static void Write(this BinaryWriter writer, Vec3 value)
	{
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Z);
	}
	public static void Write(this BinaryWriter writer, Color value)
	{
		writer.Write(value.R);
		writer.Write(value.G);
		writer.Write(value.B);
		writer.Write(value.A);
	}
	
	public static Vec2 ReadVec2(this BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle());
	public static Vec3 ReadVec3(this BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	public static Color ReadColor(this BinaryReader reader) => new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
}