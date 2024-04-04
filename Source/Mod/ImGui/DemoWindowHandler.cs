using ImGuiNET;

namespace Celeste64.Mod;

public class DemoWindowHandler : ImGuiHandler
{
	public override void Render()
	{
		ImGui.ShowDemoWindow();
	}
}
