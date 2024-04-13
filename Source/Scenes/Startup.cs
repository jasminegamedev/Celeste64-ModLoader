using Celeste64.Mod;
using Celeste64.Mod.Data;
using System.Text;

namespace Celeste64;

/// <summary>
/// Creates a slight delay so the window looks OK before we load Assets
/// TODO: Would be nice if Foster could hide the Window till assets are ready.
/// </summary>
public class Startup : Scene
{
	private int assetQueueSize;
	private int queueIndex = 0;
	private bool areModsRegistered = false;
	private string lastLoadedId = string.Empty;
	private int delay = 5;

	public Startup()
	{
		// Register vanilla mod so that it can load its assets
		// Assume this will be overridden later
		ModManager.Instance.VanillaGameMod = new VanillaGameMod
		{
			ModInfo = new ModInfo
			{
				Id = "Celeste64Vanilla",
				Name = "Celeste 64: Fragments of the Mountain",
				VersionString = "1.1.1",
			},
			Filesystem = new FolderModFilesystem(Assets.ContentPath)
		};
		ModManager.Instance.RegisterMod(ModManager.Instance.VanillaGameMod);

		// load save file
		{
			SaveManager.Instance.LoadSaveByFileName(SaveManager.Instance.GetLastLoadedSave());
		}

		// load settings file
		{
			Settings.LoadSettingsByFileName(Settings.DefaultFileName);
		}

		// load mod settings file
		{
			ModSettings.LoadModSettingsByFileName(ModSettings.DefaultFileName);
		}

		// load vanilla assets
		Assets.StageVanilla();

		// make sure the active language is ready for use,
		// since the save file may have loaded a different language than default.
		Language.Current.Use();

		// try to load controls, or overwrite with defaults if they don't exist
		{
			Controls.LoadControlsByFileName(Controls.DefaultFileName);
		}
	}

	public override void Update()
	{
		if (delay > 0)
		{
			delay--;
			return;
		}

		if (!areModsRegistered)
		{
			// this also loads mods, which get their saved settings from the save file.
			ModLoader.RegisterAllMods();
			assetQueueSize = Assets.FillLoadQueue();
			areModsRegistered = true;
			return;
		}

		// load assets
		lastLoadedId = Assets.loadQueue.Count > 0 ? Assets.loadQueue[0].ModInfo.Id : string.Empty;
		bool shouldGo = !Assets.MoveLoadQueue();
		queueIndex++;

		if (shouldGo && !Game.Instance.IsMidTransition)
		{
			// enter game
			if (Input.Keyboard.CtrlOrCommand && !Game.Instance.IsMidTransition && Settings.EnableQuickStart)
			{
				var entry = new Overworld.Entry(Assets.Levels[0], null);
				entry.Level.Enter();
			}
			else
			{
				Game.Instance.Goto(new Transition()
				{
					Mode = Transition.Modes.Replace,
					Scene = () => new Titlescreen(),
					ToBlack = null,
					FromBlack = new AngledWipe(),
				});
			}
		}
	}

	public override void Render(Target target)
	{
		target.Clear(Color.Black);

		Batcher batcher = new();
		Rect bounds = new(0, 0, target.Width, target.Height);

		string loadInfo;

		if (!areModsRegistered)
		{
			loadInfo = Loc.Str("FujiLoaderStatusRegistering");
		}
		else
		{
			loadInfo = String.Format(Loc.Str("FujiLoaderStatusNormal"), lastLoadedId, queueIndex, assetQueueSize);
		}

		UI.Text(batcher, loadInfo, bounds.BottomLeft + new Vec2(4 * Game.RelativeScale, -28 * Game.RelativeScale), Vec2.Zero, Color.White);

		batcher.Render(target);
		batcher.Clear();
	}
}
