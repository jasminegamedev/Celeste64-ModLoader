namespace Celeste64.Mod.Editor;

public abstract class GeometryDefinition : ActorDefinition
{
	[IgnoreProperty]
	protected abstract Matrix Transform { get; }
	
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
    
    public GeometryDefinition()
    {
    	SelectionTypes = [
    		new VertexSelectionType(this),
    	];
    }
 
    public class VertexSelectionType(GeometryDefinition def) : SelectionType
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
    				
    				var transform = def.Transform;
				    if (!Matrix.Invert(transform, out var inverseTransform))
					    return;
					
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
    									() => Vec3.Transform(def.Vertices[idx], transform),
    									v =>
    									{
    										def.Vertices[idx] = Vec3.Transform(v, inverseTransform);
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
}
