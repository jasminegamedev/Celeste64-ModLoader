namespace Celeste64.Mod.Editor;

public class EditorScene : Scene
{
	public World.EntryInfo Entry;
	
	internal readonly ImGuiHandler[] Handlers = [
		new TestWindow(),
	];
	
	public readonly List<EditorDefinition> Definitions = [];
	public EditorDefinition? Selected { internal set; get; } = null;
	
	private readonly WorldRenderer worldRenderer = new();
	
	internal EditorScene(World.EntryInfo entry)
	{
		Entry = entry;
		Definitions.Add(new TestEditorDefinition());
	}
	
	public override void Update()
	{
		// Toggle to in-game
		if (Input.Keyboard.Pressed(Keys.F3))
		{
			Game.Instance.scenes.Pop();
			Game.Instance.scenes.Push(new World(Entry));
			return;
		}
		
		if (Input.Keyboard.Ctrl && Input.Keyboard.Pressed(Keys.S))
		{
			// TODO: Dont actually hardcode this lol
			var path = "/media/Storage/Code/C#/Fuji/Content/Maps/test.bin";
			Log.Info($"Saving map to '{path}'");

			using var fs = File.Open(path, FileMode.Create);
			FujiMapWriter.WriteTo(this, fs);
		}
		
		worldRenderer.Update(this);
	}

	public override void Render(Target target)
	{
		target.Clear(Color.Black, 1.0f, 0, ClearMask.All);
		worldRenderer.Render(this, target);
	}
}

internal class EditorHandler : ImGuiHandler
{
	
}