using System.Reflection;
using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class TestWindow : EditorWindow
{
	protected override string Title => "Test";

	protected override void RenderWindow(EditorScene editor)
	{
		ImGui.Text("Testing");
		ImGui.Text($"Selected: {editor.Selected}");
		
		if (editor.Selected is { DefinitionType: { } defType, _Data: { } data })
		{
			var props = defType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(prop => !prop.HasAttr<SerializeIgnoreAttribute>());
			
			foreach (var prop in props)
			{
				ImGui.Text($" - {prop.Name}: {prop.GetValue(data)}");
			}
		}
	}
}