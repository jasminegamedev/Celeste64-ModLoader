using System.Reflection;

namespace Celeste64.Mod.Editor;

public class PlaceActorTool : Tool
{
	public override string Name => "Place Actor";
	public override Gizmo[] Gizmos => [];
	public override bool EnableSelection => false;
	
	// TODO: Detect these definitions somehow
	internal readonly List<Type> Definitions = [typeof(SpikeBlock.Definition), typeof(Solid.Definition)];

	private Type currentDefinition = null!; // Indirectly initialized in constructor
	internal Type CurrentDefinition
	{
		get => currentDefinition;
		set
		{
			if (currentDefinition == value)
				return;
			
			currentDefinition = value;
			definitionToPlace = (ActorDefinition)Activator.CreateInstance(value)!;
			
			var prop = value
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(prop =>
					!prop.HasAttr<IgnorePropertyAttribute>() &&
					prop.GetCustomAttribute<SpecialPropertyAttribute>() is { Value: SpecialPropertyType.PositionXYZ });

			if (prop is null || prop.GetGetMethod() is not { } getMethod || prop.GetSetMethod() is not { } setMethod)
				positionProp = null;
			else
				positionProp = prop;
		}
	}

	private Actor[] actorsToPlace = [];
	private ActorDefinition? definitionToPlace = null;
	private PropertyInfo? positionProp;

	public PlaceActorTool()
	{
		CurrentDefinition = Definitions[0];
	}

	public override void OnDeselectTool(EditorWorld editor)
	{
		foreach (var actor in actorsToPlace)
		{
			editor.Destroy(actor);
		}
		actorsToPlace = [];
	}
	
	public override void Update(EditorWorld editor)
	{
		if (positionProp is null || definitionToPlace is null)
			return;
		
		// TODO: Choose the placement position more intelligently: configurable distance, place along geometry, etc.
		positionProp.SetValue(definitionToPlace, editor.Camera.Position + editor.MouseRay * 250.0f);

		foreach (var actor in actorsToPlace)
		{
			editor.Destroy(actor);
		}
		
		if (!ImGuiManager.WantCaptureMouse && Input.Mouse.LeftPressed)
		{
			// Place the definition.
			editor.AddDefinition(definitionToPlace);
			// Generate a new definition + actors
			definitionToPlace = (ActorDefinition)Activator.CreateInstance(currentDefinition)!;
			actorsToPlace = [];
			return;
		}
		
		actorsToPlace = definitionToPlace.Load(World.WorldType.Editor);
		foreach (var actor in actorsToPlace)
		{
			editor.Add(actor);
		}
	}
}
