using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class ToolSelectionWindow() : EditorWindow("ToolSelect")
{
	protected override string Title => "Select Tool";

	protected override void RenderWindow(EditorWorld editor)
	{
		foreach (var tool in editor.Tools)
		{
			bool isSelected = editor.CurrentTool == tool;
			
			if (ImGui.Selectable(tool.Name, isSelected))
				editor.CurrentTool = tool;
			
			if (isSelected)
				ImGui.SetItemDefaultFocus();
		}
	}
}
