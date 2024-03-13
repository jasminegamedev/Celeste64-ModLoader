using System.Reflection;
using System.Reflection.Emit;

namespace Celeste64.Mod.Editor;

/// <summary>
/// Map parser for the custom Fuji map format.
/// </summary>
public class FujiMap : Map
{
	internal readonly List<ActorDefinition> Definitions = [];

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
				// Get the definition data type, by the full name
				var fullName = reader.ReadString();
				var defDataType = Assembly.GetExecutingAssembly().GetType(fullName)!;
				var defData = Activator.CreateInstance(defDataType);

				Log.Info($"Def: {defData}");

                var props = defDataType
                	.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                	.Where(prop => !prop.HasAttr<PropertyIgnoreAttribute>());

                foreach (var prop in props)
                {
                	if (prop.GetCustomAttribute<PropertyCustomAttribute>() is { } custom)
                	{
                		prop.SetValue(defData, custom.Deserialize(reader));
                		continue;
                	}

	                // Primitives
	                if (prop.PropertyType == typeof(bool))
		                prop.SetValue(defData, reader.ReadBoolean());
					else if (prop.PropertyType == typeof(byte))
						prop.SetValue(defData, reader.ReadByte());
					else if (prop.PropertyType == typeof(byte[]))
						prop.SetValue(defData, reader.ReadBytes(reader.Read7BitEncodedInt()));
					else if (prop.PropertyType == typeof(char))
						prop.SetValue(defData, reader.ReadChar());
					else if (prop.PropertyType == typeof(char[]))
						prop.SetValue(defData, reader.ReadChars(reader.Read7BitEncodedInt()));
					else if (prop.PropertyType == typeof(decimal))
						prop.SetValue(defData, reader.ReadDecimal());
					else if (prop.PropertyType == typeof(double))
		                prop.SetValue(defData, reader.ReadDouble());
					else if (prop.PropertyType == typeof(float))
		                prop.SetValue(defData, reader.ReadSingle());
					else if (prop.PropertyType == typeof(int))
		                prop.SetValue(defData, reader.ReadInt32());
					else if (prop.PropertyType == typeof(long))
		                prop.SetValue(defData, reader.ReadInt64());
					else if (prop.PropertyType == typeof(sbyte))
		                prop.SetValue(defData, reader.ReadSByte());
					else if (prop.PropertyType == typeof(short))
		                prop.SetValue(defData, reader.ReadInt16());
					else if (prop.PropertyType == typeof(Half))
		                prop.SetValue(defData, reader.ReadHalf());
	                else if (prop.PropertyType == typeof(string))
		                prop.SetValue(defData, reader.ReadString());
	                // Special support
	                else if (prop.PropertyType == typeof(Vec2))
		                prop.SetValue(defData, reader.ReadVec2());
	                else if (prop.PropertyType == typeof(Vec3))
		                prop.SetValue(defData, reader.ReadVec3());
	                else if (prop.PropertyType == typeof(Color))
		                prop.SetValue(defData, reader.ReadColor());

                	Log.Info($" - {prop.Name}: {prop.GetValue(defData)}");
                }

                Definitions.Add((ActorDefinition)defData!);
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
		foreach (var def in Definitions)
		{
			// TODO: Probably move this into the definition data itself?
			// if (def is TestEditorDefinition.DefinitionData test)
			// {
			// 	var matrix = Matrix.Identity;
			// 	Vec3[] verts = [
			// 		Vec3.Zero,
			// 		Vec3.UnitX * test.Scale.X,
			// 		Vec3.UnitX * test.Scale.X + Vec3.UnitY * test.Scale.Y,
			// 		Vec3.UnitY * test.Scale.Y,
			// 	];
			//
			// 	Log.Info(test.Color);
			//
			// 	var solid = new Solid
			// 	{
			// 		LocalBounds = new BoundingBox(
			// 			verts.Aggregate(Vec3.Min),
			// 			verts.Aggregate(Vec3.Max)
			// 		),
			// 		LocalVertices = verts,
			// 		LocalFaces =
			// 		[
			// 			new Solid.Face
			// 			{
			// 				Plane = new Plane(Vec3.UnitZ, 0.001f),
			// 				VertexStart = 0,
			// 				VertexCount = 4,
			// 			},
			// 		],
			// 		Position = test.Position + test.Scale / 2.0f,
			// 	};
			//
			// 	solid.Model.Materials.Add(new DefaultMaterial(Assets.Textures["white"]));
			// 	solid.Model.Parts.Add(new SimpleModel.Part(0, 0, 6));
			// 	solid.Model.Mesh.SetVertices<Vertex>([
			// 		new Vertex(verts[0], Vec2.Zero, test.Color.ToVector3(), Vec3.UnitZ),
			// 		new Vertex(verts[1], Vec2.UnitX, test.Color.ToVector3(), Vec3.UnitZ),
			// 		new Vertex(verts[2], Vec2.One, test.Color.ToVector3(), Vec3.UnitZ),
			// 		new Vertex(verts[3], Vec2.UnitY, test.Color.ToVector3(), Vec3.UnitZ),
			// 	]);
			// 	solid.Model.Mesh.SetIndices<int>([0, 1, 2, 0, 2, 3]);
			//
			// 	world.Add(solid);
			// }
		}
		world.Add(new Player { Position = new Vec3(0, 0, 100) });
	}
}
