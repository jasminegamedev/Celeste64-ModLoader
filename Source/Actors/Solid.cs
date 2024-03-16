using Celeste64.Mod;
using Celeste64.Mod.Editor;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace Celeste64;

public class Solid : Actor, IHaveModels
{
	public class Definition : ActorDefinition
	{
		[SpecialProperty(SpecialPropertyType.PositionXYZ)]
		public Vec3 Position { get; set; }
		
		static float size => 10.0f;
		
		[CustomProperty(typeof(VerticesProperty))]
		public List<List<Vec3>> Faces { get; set; } = [
			[new Vec3(-size, 0.0f, size), new Vec3(size, 0.0f, size), new Vec3(size, 0.0f, -size), new Vec3(-size, 0.0f, -size)]
		];
		
		public override Actor[] Load(World.WorldType type)
		{
			// Calculate bounds
			var bounds = new BoundingBox();
			foreach (var face in Faces)
			{
				var faceMin = face.Aggregate(Vec3.Min);
				var faceMax = face.Aggregate(Vec3.Max);
				bounds = new BoundingBox(Vec3.Min(bounds.Min, faceMin), Vec3.Max(bounds.Max, faceMax)); 
			}
			
			// Generate visual / collision mesh
			var colliderVertices = new List<Vec3>();
			var colliderFaces = new List<Face>();
			
			var meshVertices = new List<Vertex>();
			var meshIndices = new List<int>();
			
			foreach (var face in Faces)
			{
				int vertexIndex = colliderVertices.Count;
				var plane = Plane.CreateFromVertices(face[0], face[1], face[2]);
				
				colliderFaces.Add(new Face
				{
					Plane = plane,
					VertexStart = vertexIndex,
					VertexCount = face.Count
				});
				
				// Triangulate the mesh
				for (int i = 0; i < face.Count - 2; i++)
				{
					meshIndices.Add(vertexIndex + 0);
					meshIndices.Add(vertexIndex + i + 1);
					meshIndices.Add(vertexIndex + i + 2);
				}
				
				// The center of the bounding box should always be <0, 0, 0>
				colliderVertices.AddRange(face.Select(vertex => vertex - bounds.Center));
				meshVertices.AddRange(face.Select(vertex => new Vertex(
					position: vertex - bounds.Center,
					texcoord: Vec2.Zero,
					color: Vec3.One,
					normal: plane.Normal)));
			}
			
			var solid = new Solid();
			solid.Model.Mesh.SetVertices<Vertex>(CollectionsMarshal.AsSpan(meshVertices));
			solid.Model.Mesh.SetIndices<int>(CollectionsMarshal.AsSpan(meshIndices));
			solid.Model.Materials.Add(new DefaultMaterial(Assets.Textures["wall"]));
			solid.Model.Parts.Add(new SimpleModel.Part(0, 0, meshIndices.Count));
			
			solid.LocalBounds = new BoundingBox(
            	colliderVertices.Aggregate(Vec3.Min),
            	colliderVertices.Aggregate(Vec3.Max)
            );
			solid.LocalVertices = colliderVertices.ToArray();
			solid.LocalFaces =  colliderFaces.ToArray();
			solid.Position = Position + bounds.Center;
			
			return [solid];
		}
		
		public class VerticesProperty : ICustomProperty<List<List<Vec3>>>
		{
			public static void Serialize(List<List<Vec3>> value, BinaryWriter writer)
			{
				writer.Write(value.Count);
				foreach (var vertices in value)
				{
					writer.Write(vertices.Count);
					foreach (var vertex in vertices)
					{
						writer.Write(vertex);
					}
				}
			}

			public static List<List<Vec3>> Deserialize(BinaryReader reader)
			{
				var value = new List<List<Vec3>>(capacity: reader.ReadInt32());
				for (int i = 0; i < value.Capacity; i++)
				{
					var vertices = new List<Vec3>(capacity: reader.ReadInt32());
					for (int j = 0; j < vertices.Capacity; j++)
					{
						vertices[j] = reader.ReadVec3();
					}
					value[i] = vertices;
				}
				
				return value;
			}

			public static bool RenderGui(ref List<List<Vec3>> value)
			{
				bool changed = false;
				
				ImGui.Text("Geometry:");
				for (int i = 0; i < value.Count; i++)
				{
					ImGui.SeparatorText($"Face {i + 1}");
					for (int j = 0; j < value[i].Count; j++)
					{
						var v = value[i][j];
						changed |= ImGui.DragFloat3($"{j + 1}##{i}-{j}", ref v);
						value[i][j] = v;
						
						if (value[i].Count > 3 && ImGui.Button($"Remove Vertex##{i}-{j}"))
						{
							value[i].RemoveAt(j);
							changed = true;
						}
					}
					
					ImGui.Separator();
					
					if (ImGui.Button($"Add Vertex##{i}"))
					{
						value[i].Add(Vec3.Zero);
						changed = true;
					}
					
					if (ImGui.Button($"Remove Face##{i}"))
					{
						value.RemoveAt(i);
						changed = true;
					}
				}
				
				ImGui.Separator();
				
				if (ImGui.Button("Add Face"))
				{
					value.Add([Vec3.Zero, Vec3.Zero, Vec3.Zero]);
					changed = true;
				}
				
				return changed;
			}
		}
	}
	
	/// <summary>
	/// If we're currently solid
	/// </summary>
	public bool Collidable = true;

	/// <summary>
	/// If the Camera should care about it
	/// </summary>
	public bool Transparent = false;

	/// <summary>
	/// If we're currently climbable
	/// </summary>
	public bool Climbable = true;

	/// <summary>
	/// Visual Model to Draw
	/// </summary>
	public readonly SimpleModel Model = new() { Flags = ModelFlags.Terrain };

	public readonly List<Attacher> Attachers = [];

	/// <summary>
	/// Collision Face
	/// </summary>
	public struct Face
	{
		public Plane Plane;
		public int VertexStart;
		public int VertexCount;
	}

	public Vec3[] LocalVertices = [];
	public Face[] LocalFaces = [];

	public virtual Vec3[] WorldVertices
	{
		get
		{
			ValidateTransformations();
			return WorldVerticesLocal;
		}
	}
	public virtual Face[] WorldFaces
	{
		get
		{
			ValidateTransformations();
			return WorldFacesLocal;
		}
	}

	public virtual bool IsClimbable => Climbable;

	public Vec3 Velocity = Vec3.Zero;

	public float TShake;

	public bool Initialized = false;
	public Vec3[] WorldVerticesLocal = [];
	public Face[] WorldFacesLocal = [];
	public BoundingBox LastWorldBounds;

	public override void Created()
	{
		WorldVerticesLocal = new Vec3[LocalVertices.Length];
		WorldFacesLocal = new Face[LocalFaces.Length];
		LastWorldBounds = new();
		Initialized = true;
		Transformed();
	}

	public override void Destroyed()
	{
		World.SolidGrid.Remove(this, new Rect(LastWorldBounds.Min.XY(), LastWorldBounds.Max.XY()));
	}

	public override void Update()
	{
		if (Velocity.LengthSquared() > .001f)
			MoveTo(Position + Velocity * Time.Delta);

		// virtual shaking
		if (TShake > 0)
		{
			TShake -= Time.Delta;
			if (TShake <= 0)
				Model.Transform = Matrix.Identity;
			else if (Time.OnInterval(.02f))
				Model.Transform = Matrix.CreateTranslation(World.Rng.Float(-1, 1), World.Rng.Float(-1, 1), 0);
		}
	}

	public override void Transformed()
	{
		// realistically instead of transforming all the points, we could
		// inverse the matrix and test against that instead ... but *shrug*
		if (Initialized)
		{
			var mat = Matrix;
			for (int i = 0; i < LocalVertices.Length; i++)
				WorldVerticesLocal[i] = Vec3.Transform(LocalVertices[i], mat);

			for (int i = 0; i < LocalFaces.Length; i++)
			{
				WorldFacesLocal[i] = LocalFaces[i];
				WorldFacesLocal[i].Plane = Plane.Transform(LocalFaces[i].Plane, mat);
			}

			if (Alive)
			{
				World.SolidGrid.Remove(this, new Rect(LastWorldBounds.Min.XY(), LastWorldBounds.Max.XY()));
				World.SolidGrid.Insert(this, new Rect(WorldBounds.Min.XY(), WorldBounds.Max.XY()));
				LastWorldBounds = WorldBounds;
			}
		}
	}

	public virtual bool HasPlayerRider()
	{
		return World.Get<Player>()?.RidingPlatformCheck(this) ?? false;
	}

	public virtual void MoveTo(Vec3 target)
	{
		var delta = (target - Position);

		if (Collidable)
		{
			if (delta.LengthSquared() > 0.001f)
			{
				foreach (var actor in World.All<IRidePlatforms>())
				{
					if (actor == this || actor is not IRidePlatforms rider)
						continue;

					if (rider.RidingPlatformCheck(this))
					{
						Collidable = false;
						rider.RidingPlatformSetVelocity(Velocity);
						rider.RidingPlatformMoved(delta);
						Collidable = true;
					}
				}

				Position += delta;
			}
		}
		else
		{
			Position += delta;
		}
	}

	public virtual void CollectModels(List<(Actor Actor, Model Model)> populate)
	{
		populate.Add((this, Model));
	}
}
