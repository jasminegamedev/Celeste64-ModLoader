using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class TestWindow : EditorWindow
{
	protected override string Title => "Test";

	protected override void RenderWindow(EditorScene editor)
	{
		ImGui.Text("Testing");
		ImGui.Text($"Selected: {editor.Selected}");
		
		// if (editor.Selected is { } selected)
		// {
		// 	selected.RenderGUI(editor);
		// }
	}
}