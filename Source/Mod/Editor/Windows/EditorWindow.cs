using ImGuiNET;

namespace Celeste64.Mod.Editor;

public abstract class EditorWindow : ImGuiHandler
{
	protected abstract string Title { get; }

	protected abstract void RenderWindow(EditorWorld editor);
	public sealed override void Render()
	{
		ImGui.Begin(Title);
		RenderWindow((Game.Scene as EditorWorld)!);
		ImGui.End();
	}
}
