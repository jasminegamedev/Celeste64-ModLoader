using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class EditorMenuBar : ImGuiHandler
{
	public override void Render()
	{
		bool changed = false;

		ImGui.BeginMainMenuBar();
		
		if (ImGui.BeginMenu("Settings"))
		{
			bool music = Save.Instance.Editor.PlayMusic;
			changed |= ImGui.Checkbox("Player Music", ref music);
			Save.Instance.Editor.PlayMusic = music;
			
			bool ambience = Save.Instance.Editor.PlayAmbience;
			changed |= ImGui.Checkbox("Play Ambience", ref ambience);
			Save.Instance.Editor.PlayAmbience = ambience;
			
			ImGui.EndMenu();
		}
		
		if (ImGui.BeginMenu("View"))
		{
			bool snow = Save.Instance.Editor.RenderSnow;
			changed |= ImGui.Checkbox("Show Snow", ref snow);
			Save.Instance.Editor.RenderSnow = snow;
			
			ImGui.EndMenu();
		}
		
		ImGui.EndMainMenuBar();
		
		if (changed)
			(Game.Scene as EditorWorld)!.RefreshEnvironment();
	}
}
