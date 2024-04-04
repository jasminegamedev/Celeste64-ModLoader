using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class ActorSelectionWindow() : EditorWindow("ActorSelect")
{
	protected override string Title => "Select Actor";

	protected override void RenderWindow(EditorWorld editor)
	{
		if (editor.CurrentTool is not PlaceActorTool placeActorTool)
			return;
		
		foreach (var def in placeActorTool.Definitions)
		{
			bool isSelected = placeActorTool.CurrentDefinition == def;
			
			if (ImGui.Selectable(def.FullName, isSelected))
				placeActorTool.CurrentDefinition = def;
			
			if (isSelected)
				ImGui.SetItemDefaultFocus();
		}
	}
}
