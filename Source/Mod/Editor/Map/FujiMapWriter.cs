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
		
		#region Header
		
		writer.Write(FormatMagic);
		writer.Write(FormatVersion);
		
		#endregion

		#region Metadata
		
		// Skybox
		writer.WriteNullTerminatedString("city");
		// Snow amount
		writer.Write(1.0f);
		// Snow direction
		writer.Write(new Vec3(0.0f, 0.0f, -1.0f));
		// Ambience
		writer.Write("mountain");
		// Music
		writer.Write("mus_lvl1");
		
		#endregion
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
	
	public static Vec2 ReadVec2(this BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle());
	public static Vec3 ReadVec3(this BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
}