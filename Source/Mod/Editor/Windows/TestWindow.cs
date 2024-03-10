using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class TestWindow : EditorWindow
{
	protected override string Title => "Test";

	protected override void RenderWindow(EditorScene editor)
	{
		ImGui.Text("Testing");
		ImGui.Text($"Selected: {editor.Selected}");
		
		if (editor.Selected is { } selected)
		{
			var pos = selected.Position;
			ImGui.DragFloat3("Position", ref pos, 0.1f);
			selected.Position = pos;
			
			var rot = selected.Rotation;
			ImGui.DragFloat3("Rotation", ref rot, 0.1f);
			selected.Rotation = rot;
			
			var scale = selected.Scale;
			ImGui.DragFloat3("Scale", ref scale, 0.1f);
			selected.Scale = scale;
		}
	}
}