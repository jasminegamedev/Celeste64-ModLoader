namespace Celeste64.Mod;

public static class BinaryExtensions
{
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
