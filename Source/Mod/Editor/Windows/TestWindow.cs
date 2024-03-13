using System.Reflection;
using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class TestWindow : EditorWindow
{
	protected override string Title => "Test";

	protected override void RenderWindow(EditorWorld editor)
	{
		ImGui.Text("Testing");
		ImGui.Text($"Selected: {editor.Selected}");

		if (editor.Selected is { DefinitionType: { } defType, _Data: { } data })
		{
			var props = defType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(prop => !prop.HasAttr<PropertyIgnoreAttribute>());

			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<PropertyCustomAttribute>() is { } custom)
				{
					var obj = prop.GetValue(data)!;
					custom.RenderGui(ref obj);
					prop.SetValue(data, obj);

					continue;
				}

				switch (prop.GetValue(data))
				{
					case Vec3 v:
						if (ImGui.DragFloat3(prop.Name, ref v))
						{
							prop.SetValue(data, v);
							data.Dirty = true;
						}
						break;

					default:
						ImGui.Text($" - {prop.Name}: {prop.GetValue(data)}");
						break;
				}
			}
		}
	}
}
