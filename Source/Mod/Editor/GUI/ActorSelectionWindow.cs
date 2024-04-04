using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class ActorSelectionWindow() : EditorWindow("ActorSelect")
{
	protected override string Title => "Select Actor";

	// TODO: Detect these definitions somehow
	private List<Type> definitionTypes = [typeof(SpikeBlock.Definition), typeof(Solid.Definition)];
	
	private int currentDefinition = 0;
	
	protected override void RenderWindow(EditorWorld editor)
	{
		for (int i = 0; i < definitionTypes.Count; i++)
		{
			bool isSelected = currentDefinition == i;
			
			// TODO: 1) Cache this? 2) Somehow get a good human-readable name
			if (ImGui.Selectable(definitionTypes[i].FullName, isSelected))
				currentDefinition = i;
			
			if (isSelected)
				ImGui.SetItemDefaultFocus();
		}
	}
}
