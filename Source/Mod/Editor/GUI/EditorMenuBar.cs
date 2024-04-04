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
			bool music = Settings.Editor.PlayMusic;
			changed |= ImGui.Checkbox("Player Music", ref music);
			Settings.Editor.PlayMusic = music;

			bool ambience = Settings.Editor.PlayAmbience;
			changed |= ImGui.Checkbox("Play Ambience", ref ambience);
			Settings.Editor.PlayAmbience = ambience;

			ImGui.EndMenu();
		}

		if (ImGui.BeginMenu("View"))
		{
			bool snow = Settings.Editor.RenderSnow;
			changed |= ImGui.Checkbox("Show Snow", ref snow);
			Settings.Editor.RenderSnow = snow;
			
			bool skybox = Settings.Editor.RenderSkybox;
			changed |= ImGui.Checkbox("Show Skybox", ref skybox);
			Settings.Editor.RenderSkybox = skybox;
			
			bool anim = Settings.Editor.PlayAnimations;
			changed |= ImGui.Checkbox("Play Animations", ref anim);
			Settings.Editor.PlayAnimations = anim;

			float renderDistance = Settings.Editor.RenderDistance;
			changed |= ImGui.DragFloat("Render Distance", ref renderDistance, v_speed: 10.0f, v_min: EditorSettings_V01.MinRenderDistance, v_max: EditorSettings_V01.MaxRenderDistance);
			Settings.Editor.RenderDistance = renderDistance;

			string[] displayStrings = [
				"Game (640x360)",
				"720p (1280x720)",
				"HD (1920x1080)",
				$"Native ({App.Width}x{App.Height})",
			];

			var resolutionType = Settings.Editor.ResolutionType;
			if (ImGui.BeginCombo("Resolution", displayStrings[(int)resolutionType]))
			{
				if (ImGui.Selectable(displayStrings[(int)EditorSettings_V01.Resolution.Game], resolutionType == EditorSettings_V01.Resolution.Game))
				{
					Settings.Editor.ResolutionType = EditorSettings_V01.Resolution.Game;
					changed = true;
				}
				if (ImGui.Selectable(displayStrings[(int)EditorSettings_V01.Resolution.Double], resolutionType == EditorSettings_V01.Resolution.Double))
				{
					Settings.Editor.ResolutionType = EditorSettings_V01.Resolution.Double;
					changed = true;
				}
				if (ImGui.Selectable(displayStrings[(int)EditorSettings_V01.Resolution.HD], resolutionType == EditorSettings_V01.Resolution.HD))
				{
					Settings.Editor.ResolutionType = EditorSettings_V01.Resolution.HD;
					changed = true;
				}
				if (ImGui.Selectable(displayStrings[(int)EditorSettings_V01.Resolution.Native], resolutionType == EditorSettings_V01.Resolution.Native))
				{
					Settings.Editor.ResolutionType = EditorSettings_V01.Resolution.Native;
					changed = true;
				}

				ImGui.EndCombo();
			}

			ImGui.EndMenu();
		}

		ImGui.EndMainMenuBar();

		if (changed)
			EditorWorld.Current.RefreshEnvironment();
	}
}
