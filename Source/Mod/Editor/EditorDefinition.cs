using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class EditorDefinition
{
	// TODO: Figure out how to let definitions mark support for certain special properties like position / rotation / scale
	public virtual Vec3 Position { get; set; } = Vector3.Zero;
	public virtual Vec3 Rotation { get; set; } = Vector3.Zero;
	public virtual Vec3 Scale { get; set; } = Vector3.One;
	
	public virtual void Render(ref EditorRenderState state) { }

	public virtual void RenderGUI(EditorScene editor)
	{
		var pos = Position;
		ImGui.DragFloat3("Position", ref pos, 0.1f);
		Position = pos;
			
		var rot = Rotation;
		ImGui.DragFloat3("Rotation", ref rot, 0.1f);
		Rotation = rot;
			
		var scale = Scale;
		ImGui.DragFloat3("Scale", ref scale, 0.1f);
		Scale = scale;
	}
}