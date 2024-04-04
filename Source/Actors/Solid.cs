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

		public List<Vec3> Vertices { get; set; } = [
			new Vec3(-size, size, size), new Vec3(size, size, size), new Vec3(size, size, -size), new Vec3(-size, size, -size),
			new Vec3(-size, -size, size), new Vec3(size, -size, size), new Vec3(size, -size, -size), new Vec3(-size, -size, -size)
		];
		
		public List<List<int>> Faces { get; set; } = [
			[0, 1, 2, 3], // Front
			[7, 6, 5, 4], // Back
			[4, 5, 1, 0], // Top
			[3, 2, 6, 7], // Bottom
			[1, 5, 6, 2], // Left
			[4, 0, 3, 7], // Right
		];
		
		public Definition()
		{
			SelectionTypes = [
				new VertexSelectionType(this),
			];
		}
		
		public override Actor[] Load(World.WorldType type)
		{
			// Calculate bounds
			var bounds = new BoundingBox();
			foreach (var face in Faces)
			{
				var faceMin = face.Select(idx => Vertices[idx]).Aggregate(Vec3.Min);
				var faceMax = face.Select(idx => Vertices[idx]).Aggregate(Vec3.Max);
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
				var plane = Plane.CreateFromVertices(Vertices[face[0]], Vertices[face[1]], Vertices[face[2]]);
				
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
				colliderVertices.AddRange(face.Select(idx => Vertices[idx] - bounds.Center));
				meshVertices.AddRange(face.Select(idx => new Vertex(
					position: Vertices[idx] - bounds.Center,
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
	}
	
	public class VertexSelectionType(Solid.Definition def) : SelectionType
	{
		private readonly List<SelectionTarget> targets = [];
		// TODO: Support other gizmos depending on current tool. Maybe with some restriction on which tools are allowed tho
		private PositionGizmo? vertexGizmo = null;
		
		public override IEnumerable<SelectionTarget> Targets => targets;
		public override IEnumerable<Gizmo> Gizmos => vertexGizmo is null ? [] : [vertexGizmo];

		public override void Awake()
		{
			EditorWorld.Current.OnTargetSelected += target =>
			{
				// Deselect if something else was selected
				if (target is null || !targets.Contains(target) && !(vertexGizmo?.SelectionTargets.Contains(target) ?? false))
					vertexGizmo = null;
			};
			
			def.OnUpdated += () =>
			{
				targets.Clear();
				// TODO: Detect when geometry changed?
				// vertexGizmo = null;
				
				var transform = Matrix.CreateTranslation(def.Position);
				const float selectionRadius = 1.0f;

				foreach (var face in def.Faces)
				{
					foreach (int idx in face)
					{
						targets.Add(new SimpleSelectionTarget(transform, new BoundingBox(def.Vertices[idx], selectionRadius * 2.0f))
						{
							// OnHovered = () => Log.Info($"Hovered vertex {vertex}"),
							OnSelected = () =>
							{
								Log.Info($"Selected vertex {idx} {def.Vertices[idx]}");
								vertexGizmo = new PositionGizmo(
									() => def.Vertices[idx] + def.Position,
									v =>
									{
										def.Vertices[idx] = v - def.Position;
										def.Dirty = true;
									},
									scale: 0.5f);
							},
							// OnDragged = (mouseDelta, mouseRay) => Log.Info($"Dragged vertex {vertex} ({mouseDelta}, {mouseRay})"),
						});
					}
				}
			};
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
	/// If we're currently climbable
	/// </summary>
	public bool AllowWallJumps = true;

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

	public virtual bool CanWallJump
	{
		get
		{
			return AllowWallJumps;
		}
	}

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
