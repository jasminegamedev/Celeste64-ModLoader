using ImGuiNET;

namespace Celeste64.Mod.Editor;

public class EnvironmentSettingsWindow() : EditorWindow("EnvironmentSettings")
{
	protected override string Title => "Environment Settings";
	
	protected override void RenderWindow(EditorWorld editor)
	{
		if (editor.Map == null)
			return;
		
		bool changed = false;
		
		// AFAIK just passing a large value works fine for C# strings
		const int bufferSize = 32767;
		
		// TODO: Add an asset picker??
		string skybox = editor.Map.Skybox ?? string.Empty;
		changed |= ImGui.InputText("Skybox", ref skybox, bufferSize);
		editor.Map.Skybox = skybox;
		
		float snowAmount = editor.Map.SnowAmount;
		changed |= ImGui.DragFloat("Snow Amount", ref snowAmount, v_speed: 0.1f, v_min: 0.0f);
		editor.Map.SnowAmount = snowAmount;
		
		var snowWind = editor.Map.SnowWind;
		changed |= ImGui.DragFloat3("Snow Wind", ref snowWind, v_speed: 0.1f);
		editor.Map.SnowWind = snowWind;
		
		string music = editor.Map.Music ?? string.Empty;
		changed |= ImGui.InputText("Music", ref music, bufferSize);
		editor.Map.Music = music;
		
		string ambience = editor.Map.Ambience ?? string.Empty;
		changed |= ImGui.InputText("Ambience", ref ambience, bufferSize);
		editor.Map.Ambience = ambience;
		
		if (changed)
			editor.RefreshEnvironment();
	}
}
