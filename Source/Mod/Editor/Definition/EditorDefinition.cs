using ImGuiNET;

namespace Celeste64.Mod.Editor;

public abstract class EditorDefinitionData
{
	// Used to link back to the EditorDefinition
	// TODO: Maybe remove this??
	public string DefinitionFullName { get; set; }
	
	// TODO: Figure out how to let definitions mark support for certain special properties like position / rotation / scale
	public virtual Vec3 Position { get; set; } = Vector3.Zero;
	public virtual Vec3 Rotation { get; set; } = Vector3.Zero;
	public virtual Vec3 Scale { get; set; } = Vector3.One;
}

public abstract class EditorDefinition
{
	/// <summary>
	/// Type of the associated <see cref="EditorDefinitionData"/>.
	/// </summary>
	public readonly Type DataType;
	
	/// <summary>
	/// Instance of the data type associated with this definition.
	/// Needs to be casted to the appropriate type inside the sub class.
	/// </summary>
	public EditorDefinitionData _Data { get; internal set; }

	protected EditorDefinition(Type dataType)
	{
		DataType = dataType;
		_Data = (EditorDefinitionData)Activator.CreateInstance(DataType)!;
		_Data.DefinitionFullName = GetType().FullName!;
	}
	
	public virtual void Render(ref EditorRenderState state) { }

	public virtual void RenderGUI(EditorScene editor)
	{
		var pos = _Data.Position;
		ImGui.DragFloat3("Position", ref pos, 0.1f);
		_Data.Position = pos;
			
		var rot = _Data.Rotation;
		ImGui.DragFloat3("Rotation", ref rot, 0.1f);
		_Data.Rotation = rot;
			
		var scale = _Data.Scale;
		ImGui.DragFloat3("Scale", ref scale, 0.1f);
		_Data.Scale = scale;
	}
}