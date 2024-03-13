using System.Reflection;
using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class TestWindow : EditorWindow
{
	protected override string Title => "Test";

	protected override void RenderWindow(EditorWorld editor)
	{
		ImGui.Text("Testing");

		if (ImGui.Button("Add Spikes"))
		{
			editor.Definitions.Add(new SpikeBlock.Definition());
		}

		ImGui.Text($"Selected: {editor.Selected}");

		if (editor.Selected is { } selected)
		{
			var props = selected.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(prop => !prop.HasAttr<PropertyIgnoreAttribute>());

			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<PropertyCustomAttribute>() is { } custom)
				{
					var obj = prop.GetValue(selected)!;
					custom.RenderGui(ref obj);
					prop.SetValue(selected, obj);

					continue;
				}

				switch (prop.GetValue(selected))
				{
					case Vec3 v:
						if (ImGui.DragFloat3(prop.Name, ref v))
						{
							prop.SetValue(selected, v);
							selected.Dirty = true;
						}
						break;

					default:
						ImGui.Text($" - {prop.Name}: {prop.GetValue(selected)}");
						break;
				}
			}
		}
	}
}
