using Celeste64.Mod;
using Celeste64.Mod.Patches;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Celeste64;

#region Transition Struct
/// <summary>
/// Represents a game transition. Transitions are used to smoothly go from one game scene to the next.
/// </summary>
public struct Transition
{
	/// <summary>
	/// Transition modes
	/// </summary>
	public enum Modes
	{
		/// <summary>
		/// Replace the current scene
		/// </summary>
		Replace,
		/// <summary>
		/// Push a new scene to the stack
		/// </summary>
		Push,
		/// <summary>
		/// Remove the current scene from the stack
		/// </summary>
		Pop
	}

	/// <summary>
	/// The mode of this transition - the action it will take upon being executed
	/// </summary>
	public Modes Mode;
	/// <summary>
	/// The scene this transition will go to next - nullable
	/// </summary>
	public Func<Scene>? Scene;
	/// <summary>
	/// The screen wipe used when entering the transition
	/// </summary>
	public ScreenWipe? ToBlack;
	/// <summary>
	/// The screen wipe used when exiting the transition
	/// </summary>
	public ScreenWipe? FromBlack;
	public bool ToPause;
	public bool FromPause;
	/// <summary>
	/// Whether to save player stats when doing the transition
	/// </summary>
	public bool Saving;
	/// <summary>
	/// Whether to stop the music when transitioning
	/// </summary>
	public bool StopMusic;
	/// <summary>
	/// Whether to perform an asset reload when transitioning
	/// </summary>
	public bool PerformAssetReload;
	/// <summary>
	/// Whether to reload all assets
	/// </summary>
	public bool ReloadAll;
	/// <summary>
	/// How long to hold on black when executing the transition (e.g. 1.0f -> one second)
	/// </summary>
	public float HoldOnBlackFor;
}
#endregion

#region Saving State Enum
/// <summary>
/// Represents the current saving state of the game.
/// </summary>
public enum SavingState
{
	/// <summary>
	/// The game is ready to save.
	/// </summary>
	Ready,
	/// <summary>
	/// The game is currently saving.
	/// </summary>
	Saving,
	/// <summary>
	/// The game is currently saving and an additional save is queued (will re-save after the current task)
	/// </summary>
	SaveQueued
}
#endregion

public class Game : Module
{
	#region Properties
	private enum TransitionStep
	{
		None,
		FadeOut,
		Hold,
		Perform,
		FadeIn
	}

	public const string GamePath = "Celeste64";
	// ModloaderCustom
	public const string GameTitle = "Celeste 64: Fragments of the Mountain + Fuji Mod Loader";
	public static readonly Version GameVersion = typeof(Game).Assembly.GetName().Version!;
	public static readonly string VersionString = $"Celeste 64: v.{GameVersion.Major}.{GameVersion.Minor}.{GameVersion.Build}";
	public static string LoaderVersion { get; set; } = "";

	public const int DefaultWidth = 640;
	public const int DefaultHeight = 360;

	public static event Action OnResolutionChanged = () => { };

	private static float _resolutionScale = 1.0f;
	/// <summary>
	/// The current render resolution multiplier of the game
	/// </summary>
	public static float ResolutionScale
	{
		get => _resolutionScale;
		set
		{
			if (_resolutionScale == value)
				return;

			_resolutionScale = value;
			OnResolutionChanged.Invoke();
		}
	}

	public static bool IsDynamicRes;

	public static int Width => IsDynamicRes ? App.WidthInPixels : (int)(DefaultWidth * _resolutionScale);
	public static int Height => IsDynamicRes ? App.HeightInPixels : (int)(DefaultHeight * _resolutionScale);
	private int Height_old = (int)(DefaultHeight * _resolutionScale);
	private int Width_old = (int)(DefaultWidth * _resolutionScale);

	/// <summary>
	/// Used by various rendering elements to proportionally scale if you change the default game resolution
	/// </summary>
	public static float RelativeScale => _resolutionScale;

	private static Game? instance;
	/// <summary>
	/// The current instance of the game. Use this for any non-static methods.
	/// </summary>
	public static Game Instance => instance ?? throw new Exception("Game isn't running");
	public static CommandParser? AppArgs;

	private readonly Stack<Scene> scenes = new();
	/// <summary>
	/// The render target of this game instance
	/// </summary>
	public Target target { get; internal set; } = new(Width, Height, [TextureFormat.Color, TextureFormat.Depth24Stencil8]);
	/// <summary>
	/// The render batcher of this game instance
	/// </summary>
	public Batcher batcher { get; internal set; } = new();
	private Transition transition;
	private TransitionStep transitionStep = TransitionStep.None;
	private readonly FMOD.Studio.EVENT_CALLBACK audioEventCallback;
	private int audioBeatCounter;
	private bool audioBeatCounterEvent;

	private ImGuiManager imGuiManager;

	public AudioHandle Ambience;
	public AudioHandle Music;

	public SoundHandle? AmbienceWav;
	public SoundHandle? MusicWav;

	private Task? SaveTask;
	private SavingState _SaveSt = SavingState.Ready;
	/// <summary>
	/// The current saving state of the game.
	/// </summary>
	public SavingState SavingState
	{
		get => _SaveSt;
		internal set
		{
			_SaveSt = value;
			if (!Settings.EnableAdditionalLogging) return;

			LogHelper.Info($"Saving state changed to {_SaveSt}");
		}
	}

	/// <summary>
	/// Returns the topmost (i.e. currently active) scene of this game instance.
	/// </summary>
	public Scene? Scene => scenes.TryPeek(out var scene) ? scene : null;
	/// <summary>
	/// Returns the World scene this game instance is running, or null if the active scene is not a World scene.
	/// </summary>
	public World? World => Scene as World;
	#endregion

	#region Constructor
	public Game()
	{
		if (IsDynamicRes)
		{
			Log.Warning("Dynamic resolution is an experimental feature. Certain UI elements may not be adjusted correctly.");
		}

		OnResolutionChanged += () =>
		{
			target.Dispose();
			target = new(Width, Height, [TextureFormat.Color, TextureFormat.Depth24Stencil8]);
		};

		// If this isn't stored, the delegate will get GC'd and everything will crash :)
		audioEventCallback = MusicTimelineCallback;
		imGuiManager = new ImGuiManager();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Request a full save of all persistence files.
	/// </summary>
	public static void RequestSave()
	{
		/* An additional save task is already queued. No need to save more */
		if (Instance.SavingState == SavingState.SaveQueued) return;

		/* Ready -> Saving -> SaveQueued */
		Instance.SavingState = Instance.SavingState != SavingState.Saving
		? SavingState.Saving
		: SavingState.SaveQueued;

		Task.Run(() =>
		{
			if (Instance.SavingState == SavingState.SaveQueued)
			{
				if (Instance.SaveTask is not null && !Instance.SaveTask.IsCompleted) Instance.SaveTask.Wait();
				Instance.SavingState = SavingState.Ready;
				RequestSave();
			}
			else
			{
				Instance.SaveTask = Task.Run(() =>
				{
					Save.SaveToFile();
					Settings.SaveToFile();
					Controls.SaveToFile();
					ModSettings.SaveToFile();
				});

				Instance.SaveTask.Wait();

				Instance.SavingState = SavingState.Ready;
			}
		});
	}

	/// <summary>
	/// Gets the full version string of the instance
	/// </summary>
	public string GetFullVersionString()
	{
		return $"{VersionString}\n{LoaderVersion}";
	}

	/// <summary>
	/// Sets the resolution scale of the game and saves it to settings.
	/// </summary>
	/// <param name="scale">The scale</param>
	public void SetResolutionScale(int scale)
	{
		ResolutionScale = scale;
		Settings.SetResolutionScale(scale);
	}

	public override void Startup()
	{
		instance = this;

		// Fuji: apply patches
		Patches.Load();

		Time.FixedStep = true;
		App.VSync = true;
		App.Title = GameTitle;
		Audio.Init();

		scenes.Push(new Startup());
		ModManager.Instance.OnGameLoaded(this);
	}

	public override void Shutdown()
	{
		if (scenes.TryPeek(out var topScene))
			topScene.Exited();

		while (scenes.Count > 0)
		{
			var it = scenes.Pop();
			it.Disposed();
		}

		// Fuji: remove patches
		Patches.Unload();

		scenes.Clear();
		instance = null;

		Log.Info("Shutting down...");
	}

	public bool IsMidTransition => transitionStep != TransitionStep.None;

	/// <summary>
	/// Request a transition for this game instance.
	/// 
	/// Your request will be silently rejected if Game.IsMidTransition is already true.
	/// </summary>
	/// <param name="next">The transition to perform</param>
	public void Goto(Transition next)
	{
		if (IsMidTransition) return;
		Debug.Assert(
			transitionStep == TransitionStep.None ||
			transitionStep == TransitionStep.FadeIn);
		transition = next;
		transitionStep = scenes.Count > 0 ? TransitionStep.FadeOut : TransitionStep.Perform;
		transition.ToBlack?.Restart(transitionStep != TransitionStep.FadeOut);

		if (transition.StopMusic)
			Music.Stop();
	}

	/// <summary>
	/// Set the scene of this instance directly.
	/// 
	/// DON'T USE unless you have a very good reason to. This clears the scene stack and replaces it
	/// uncleanly with no regard for edge cases, meaning it may lead to crashes and loss of progress.
	/// Game.Goto is strongly preferred over this method.
	/// </summary>
	/// <param name="next"></param>
	public void UnsafelySetScene(Scene next)
	{
		scenes.Clear();
		scenes.Push(next);
	}

	private void HandleError(Exception e)
	{
		if (scenes.Peek() is GameErrorMessage)
		{
			throw e; // If we're already on the error message screen, accept our fate: it's a fatal crash!
		}

		scenes.Clear();
		LogHelper.Error("An Unhandled Exception occurred: ", e);
		UnsafelySetScene(new GameErrorMessage(e));
	}

	internal void ReloadAssets(bool reloadAll)
	{
		if (!scenes.TryPeek(out var scene))
			return;

		if (IsMidTransition)
			return;

		if (scene is World world)
		{
			Goto(new Transition()
			{
				Mode = Transition.Modes.Replace,
				Scene = () => new World(world.Entry),
				ToPause = true,
				ToBlack = new AngledWipe(),
				PerformAssetReload = true,
				ReloadAll = reloadAll
			});
		}
		else
		{
			Goto(new Transition()
			{
				Mode = Transition.Modes.Replace,
				Scene = () => new Titlescreen(),
				ToPause = true,
				ToBlack = new AngledWipe(),
				PerformAssetReload = true,
				ReloadAll = reloadAll
			});
		}
	}

	private FMOD.RESULT MusicTimelineCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameters)
	{
		// notify that an audio event happened (but handle it on the main thread)
		if (transitionStep == TransitionStep.None)
			audioBeatCounterEvent = true;
		return FMOD.RESULT.OK;
	}
	#endregion

	#region Update
	public override void Update()
	{
		if (IsDynamicRes)
		{
			if (Height_old != Height || Width_old != Width)
			{
				OnResolutionChanged.Invoke();
			}

			Height_old = Height;
			Width_old = Width;
		}

		scenes.TryPeek(out var scene); // gets the top scene

		// update top scene
		try
		{
			if (!(scene is GameErrorMessage))
			{
				ModManager.Instance.PreUpdate(Time.Delta);
			}

			if (scene != null)
			{
				var pausing =
					transitionStep == TransitionStep.FadeIn && transition.FromPause ||
					transitionStep == TransitionStep.FadeOut && transition.ToPause;

				if (!pausing)
					scene.Update();
			}

			if (!(scene is GameErrorMessage))
			{
				ModManager.Instance.Update(Time.Delta);
			}
		}
		catch (Exception e)
		{
			HandleError(e);
		}

		imGuiManager.UpdateHandlers();

		// handle transitions
		if (transitionStep == TransitionStep.FadeOut)
		{
			if (transition.ToBlack == null || transition.ToBlack.IsFinished)
			{
				transitionStep = TransitionStep.Hold;
			}
			else
			{
				transition.ToBlack.Update();
			}
		}
		else if (transitionStep == TransitionStep.Hold)
		{
			transition.HoldOnBlackFor -= Time.Delta;
			if (transition.HoldOnBlackFor <= 0)
			{
				if (transition.FromBlack != null)
					transition.ToBlack = transition.FromBlack;
				transition.ToBlack?.Restart(true);
				transitionStep = TransitionStep.Perform;
			}
		}
		else if (transitionStep == TransitionStep.Perform)
		{
			Audio.StopBus(Sfx.bus_gameplay_world, false);

			// exit last scene
			if (scenes.TryPeek(out var lastScene))
			{
				try
				{
					lastScene?.Exited();
					if (transition.Mode != Transition.Modes.Push)
						lastScene?.Disposed();
				}
				catch (Exception e)
				{
					transitionStep = TransitionStep.None;
					HandleError(e);
				}
			}

			// perform game save between transitions
			if (transition.Saving)
			{
				RequestSave();
			}

			// reload assets if requested
			if (transition.PerformAssetReload)
			{
				if (transition.ReloadAll)
				{
					Assets.Load();
				}
				else
				{
					List<GameMod> modsToReload = ModManager.Instance.Mods
						.Where(mod => mod.NeedsReload)
						.OrderBy(mod => mod.ModInfo.Id)
						.ToList();

					if (Settings.EnableAdditionalLogging)
					{
						StringBuilder reloadList = new();

						reloadList.Append($"Reloading {modsToReload.Count} mods: ");
						foreach (GameMod mod in modsToReload)
						{
							reloadList.Append($"\n- {mod.ModInfo.Id}");
						}

						Log.Info(reloadList);
					}

					while (modsToReload.Count > 0)
					{
						bool loadedModThisIteration = false;
						for (int i = modsToReload.Count - 1; i >= 0; i--)
						{
							// Only Reload this mod when all of it's dependencies are already loaded
							if (!modsToReload[i].GetDependencies().Any(mod => mod.NeedsReload))
							{
								ModLoader.ReloadChangedMod(modsToReload[i]);
								loadedModThisIteration = true;
								modsToReload.Remove(modsToReload[i]);
							}
						}

						if (!loadedModThisIteration)
						{
							throw new Exception($"Could not reload {modsToReload.Count} mods due to dependencies not reloading properly.");
						}
					}

					// Re-sort mods after loading.
					ModManager.Instance.Mods = ModManager.Instance.Mods
					.OrderBy(mod => mod.ModInfo.Id) // Alphabetical
					.OrderBy(mod => !(mod is VanillaGameMod)) // Put the vanilla mod first
					.ToList();

					Language.Current.Use();
				}
			}

			// perform transition
			switch (transition.Mode)
			{
				case Transition.Modes.Replace:
					Debug.Assert(transition.Scene != null);
					if (scenes.Count > 0)
						scenes.Pop();
					scenes.Push(transition.Scene());
					break;
				case Transition.Modes.Push:
					Debug.Assert(transition.Scene != null);
					scenes.Push(transition.Scene());
					audioBeatCounter = 0;
					break;
				case Transition.Modes.Pop:
					scenes.Pop();
					break;
			}

			// run a single update when transition happens so stuff gets established
			if (scenes.TryPeek(out var nextScene))
			{
				if (Settings.EnableAdditionalLogging) Log.Info("Switching scene: " + nextScene.GetType());

				try
				{
					nextScene.Entered();
					ModManager.Instance.OnSceneEntered(nextScene);
					nextScene.Update();
				}
				catch (Exception e)
				{
					transitionStep = TransitionStep.None;
					HandleError(e);
				}
			}

			// don't let the game sit in a sceneless place
			if (scenes.Count <= 0)
				scenes.Push(new Overworld(false));

			// switch music
			{
				var last = Music.IsPlaying && lastScene != null ? lastScene.Music : string.Empty;
				var next = nextScene?.Music ?? string.Empty;
				if (next != last)
				{
					Music.Stop();
					Music = Audio.Play(next);
					if (Music)
						Music.SetCallback(audioEventCallback);
				}

				string lastWav = MusicWav is { IsPlaying: true } && lastScene != null ? lastScene.MusicWav : string.Empty;
				string nextWav = nextScene?.MusicWav ?? string.Empty;
				if (lastWav != nextWav)
				{
					MusicWav?.Stop();
					if (!string.IsNullOrEmpty(nextWav))
					{
						MusicWav = Audio.PlayMusic(nextWav);
					}
				}
			}

			// switch ambience
			{
				var last = Ambience.IsPlaying && lastScene != null ? lastScene.Ambience : string.Empty;
				var next = nextScene?.Ambience ?? string.Empty;
				if (next != last)
				{
					Ambience.Stop();
					if (!string.IsNullOrEmpty(next))
					{
						Ambience = Audio.Play(next);
					}
				}

				string lastWav = AmbienceWav is { IsPlaying: true } && lastScene != null ? lastScene.AmbienceWav : string.Empty;
				string nextWav = nextScene?.AmbienceWav ?? string.Empty;
				if (lastWav != nextWav)
				{
					AmbienceWav?.Stop();
					if (string.IsNullOrEmpty(nextWav))
					{
						AmbienceWav = Audio.PlayMusic(nextWav);
					}
				}
			}

			// in case new music was played
			Settings.SyncSettings();
			transitionStep = TransitionStep.FadeIn;
		}
		else if (transitionStep == TransitionStep.FadeIn)
		{
			if (transition.ToBlack == null || transition.ToBlack.IsFinished)
			{
				transitionStep = TransitionStep.None;
				transition = new();
			}
			else
			{
				transition.ToBlack.Update();
			}
		}
		else if (transitionStep == TransitionStep.None)
		{
			// handle audio beat events on main thread
			if (audioBeatCounterEvent)
			{
				audioBeatCounterEvent = false;
				audioBeatCounter++;

				if (scene is World world)
				{
					foreach (var listener in world.All<IListenToAudioCallback>())
						(listener as IListenToAudioCallback)?.AudioCallbackEvent(audioBeatCounter);
				}
			}
		}


		if (scene is not Celeste64.Startup && Scene is not GameErrorMessage)
		{
			// toggle fullscreen
			if (Controls.FullScreen.ConsumePress())
				Settings.ToggleFullscreen();

			if (Controls.ReloadAssets.ConsumePress() && !IsMidTransition)
			{
				Log.Info($"--- User has initiated a{(Input.Keyboard.CtrlOrCommand ? " full" : string.Empty)} manual reload. ---");
				ReloadAssets(Input.Keyboard.CtrlOrCommand); // F5 - Reload changed; Ctrl + F5 - Reload all
			}
		}
	}
	#endregion

	#region Render
	public override void Render()
	{
		Graphics.Clear(Color.Black);

		imGuiManager.RenderHandlers();

		if (transitionStep != TransitionStep.Perform && transitionStep != TransitionStep.Hold)
		{
			// draw the world to the target
			if (scenes.TryPeek(out var scene))
				try
				{
					scene.Render(target);

					ModManager.Instance.AfterSceneRender(batcher);
				}
				catch (Exception e)
				{
					HandleError(e);
				}

			// draw screen wipe over top
			if (transitionStep != TransitionStep.None && transition.ToBlack != null)
			{
				transition.ToBlack.Render(batcher, new Rect(0, 0, target.Width, target.Height));
				batcher.Render(target);
				batcher.Clear();
			}

			// Draw saving toast if currently saving
			if (SavingState != SavingState.Ready)
			{
				Vec2 ToastSize = Language.Current.SpriteFont.SizeOf(Loc.Str("FujiSaving"));
				int Pad = 4;

				batcher.PushMatrix(Matrix3x2.CreateScale(0.75f) * Matrix3x2.CreateTranslation(target.Bounds.BottomRight + new Vec2(-Pad, -Pad)));
				UI.Text(batcher, Loc.Str("FujiSaving"), target.Bounds.TopLeft + new Vec2(-ToastSize.X, -ToastSize.Y), Vec2.Zero, Time.BetweenInterval(0.25f) ? Color.White : Color.Gray);
				batcher.Render(target);
				batcher.Clear();
			}

			// draw the target to the window
			{
				var scale = Math.Min(App.WidthInPixels / (float)target.Width, App.HeightInPixels / (float)target.Height);
				batcher.SetSampler(new(TextureFilter.Nearest, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));
				batcher.Image(target, App.SizeInPixels / 2, target.Bounds.Size / 2, Vec2.One * scale, 0, Color.White);
				imGuiManager.RenderTexture(batcher);
				batcher.Render();
				batcher.Clear();
			}
		}
	}
	#endregion
}
