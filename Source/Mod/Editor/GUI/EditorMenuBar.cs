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
			
			bool skybox = Save.Instance.Editor.RenderSkybox;
			changed |= ImGui.Checkbox("Show Skybox", ref skybox);
			Save.Instance.Editor.RenderSkybox = skybox;
			
			float renderDistance = Save.Instance.Editor.RenderDistance;
			changed |= ImGui.DragFloat("Render Distance", ref renderDistance, v_speed: 10.0f, v_min: Save.EditorSettings.MinRenderDistance, v_max: Save.EditorSettings.MaxRenderDistance);
			Save.Instance.Editor.RenderDistance = renderDistance;

			string[] displayStrings = [
				"Game (640x360)",
				"HD (1920x1080)",
				$"Native ({App.Width}x{App.Height})",
			];
			
			var resolutionType = Save.Instance.Editor.ResolutionType;
			if (ImGui.BeginCombo("Resolution", displayStrings[(int)resolutionType]))
			{
				if (ImGui.Selectable(displayStrings[(int)Save.EditorSettings.Resolution.Game], resolutionType == Save.EditorSettings.Resolution.Game))
				{
					Save.Instance.Editor.ResolutionType = Save.EditorSettings.Resolution.Game;
					changed = true;
				}
				if (ImGui.Selectable(displayStrings[(int)Save.EditorSettings.Resolution.HD], resolutionType == Save.EditorSettings.Resolution.HD))
				{
					Save.Instance.Editor.ResolutionType = Save.EditorSettings.Resolution.HD;
					changed = true;
				}
				if (ImGui.Selectable(displayStrings[(int)Save.EditorSettings.Resolution.Native], resolutionType == Save.EditorSettings.Resolution.Native))
				{
					Save.Instance.Editor.ResolutionType = Save.EditorSettings.Resolution.Native;
					changed = true;
				}
				
				ImGui.EndCombo();
			}
			
			ImGui.EndMenu();
		}
		
		ImGui.EndMainMenuBar();
		
		if (changed)
			(Game.Scene as EditorWorld)!.RefreshEnvironment();
	}
}
