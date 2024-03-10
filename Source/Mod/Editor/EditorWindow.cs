using ImGuiNET;

namespace Celeste64.Mod.Editor;

public abstract class EditorWindow : ImGuiHandler
{
	protected abstract string Title { get; }

	protected abstract void RenderWindow();
	public sealed override void Render()
	{
		ImGui.Begin(Title);
		RenderWindow();
		ImGui.End();
	}
}