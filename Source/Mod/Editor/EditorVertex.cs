namespace Celeste64.Mod.Editor;

public readonly struct EditorVertex(Vec3 position, Vec2 texcoord, Vec3 color, Vec3 normal) : IVertex
{
	public readonly Vec3 Pos = position;
	public readonly Vec2 Tex = texcoord;
	public readonly Vec3 Col = color;
	public readonly Vec3 Normal = normal;

	public VertexFormat Format => VertexFormat;

	private static readonly VertexFormat VertexFormat = VertexFormat.Create<EditorVertex>(
	[
		new (0, VertexType.Float3, normalized: false),
		new (1, VertexType.Float2, normalized: false),
		new (2, VertexType.Float3, normalized: true),
		new (3, VertexType.Float3, normalized: false),
	]);
}