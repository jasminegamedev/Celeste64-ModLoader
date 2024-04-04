using ImGuiNET;

namespace Celeste64.Mod.Editor;

public abstract class EditorWindow(string id) : ImGuiHandler
{
	protected virtual string Title => id;

	protected abstract void RenderWindow(EditorWorld editor);
	public sealed override void Render()
	{
		ImGui.Begin($"{Title}###{id}");
		RenderWindow(EditorWorld.Current);
		ImGui.End();
	}
}
