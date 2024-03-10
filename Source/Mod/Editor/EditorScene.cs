namespace Celeste64.Mod.Editor;

public class EditorScene : Scene
{
	private World.EntryInfo Entry;
	
	internal readonly ImGuiHandler[] Handlers = [
		new TestWindow(),
	];
	
	internal EditorScene(World.EntryInfo entry)
	{
		Entry = entry;
	}
	
	public override void Update()
	{
		// Toggle to in-game
		if (Input.Keyboard.Pressed(Keys.F3))
		{
			Game.Instance.scenes.Pop();
			Game.Instance.scenes.Push(new World(Entry));       
		}
	}

	public override void Render(Target target)
	{
		target.Clear(Color.Black);	
	}
}

internal class EditorHandler : ImGuiHandler
{
	
}