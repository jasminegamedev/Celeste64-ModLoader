using Celeste64.Mod;

namespace Celeste64;

public class ModControlsMenu : Menu
{
	private Target GameTarget;

	//Items list always includes the Back item and reset to default, so check if it's more than 2 to know if we should display it.
	internal bool ShouldDisplay => items.Count > 2;

	public override void Closed()
	{
		base.Closed();
		Controls.SaveToFile();
	}

	public ModControlsMenu(Menu? rootMenu)
	{
		RootMenu = rootMenu;
		GameTarget = new Target(Game.Width, Game.Height);
		Game.OnResolutionChanged += () => GameTarget = new Target(Game.Width, Game.Height);
	}

	public void AddItems(GameMod mod, bool isForController)
	{
		items.Clear();
		if (mod.SettingsType != null)
		{
			foreach (var prop in mod.SettingsType.GetProperties())
			{
				if (prop.PropertyType == typeof(VirtualButton))
				{
					VirtualButton? vb = prop.GetValue(mod.Settings) as VirtualButton;
					if (vb != null)
					{
						Add(new InputBind(vb.Name, vb, RootMenu, isForController, mod));
					}
				}
				if (prop.PropertyType == typeof(VirtualStick))
				{
					VirtualStick? vs = prop.GetValue(mod.Settings) as VirtualStick;
					if (vs != null)
					{
						Add(new InputBind(vs.Name + "Up", vs.Vertical.Negative, RootMenu, isForController));
						Add(new InputBind(vs.Name + "Down", vs.Vertical.Positive, RootMenu, isForController));
						Add(new InputBind(vs.Name + "Left", vs.Horizontal.Negative, RootMenu, isForController));
						Add(new InputBind(vs.Name + "Right", vs.Horizontal.Positive, RootMenu, isForController));
					}
				}
			}
		}

		// Reset all bindings for this control type to their default value.
		Add(new Option("ControlsResetToDefault", () =>
		{
			Controls.ResetAllBindings(isForController, mod);
		}));

		Add(new Option("Exit", () =>
		{
			PopRootSubMenu();
		}));
	}

	/// <summary>
	/// Render bindings and input legend
	/// </summary>
	/// <param name="batch"></param>
	protected override void RenderItems(Batcher batch)
	{
		batch.PushMatrix(new Vec2(GameTarget.Bounds.Center.X, GameTarget.Bounds.Center.Y - 16f), false);
		base.RenderItems(batch);
		batch.PopMatrix();

		batch.PushMatrix(Vec2.Zero, false);
		var at = GameTarget.Bounds.BottomRight + new Vec2(-32, -4) * Game.RelativeScale + new Vec2(0, -UI.PromptSize);
		UI.Prompt(batch, Controls.Cancel, Loc.Str("Back"), at, out var width, 1.0f);
		at.X -= width + 8 * Game.RelativeScale;

		UI.Prompt(batch, Controls.Confirm, Loc.Str("Bind"), at, out width, 1.0f);
		at.X -= width + 8 * Game.RelativeScale;

		UI.Prompt(batch, Controls.CreateFile, Loc.Str("Clear"), at, out width, 1.0f);
		at.X -= width + 8 * Game.RelativeScale;

		UI.Prompt(batch, Controls.CopyFile, Loc.Str("Reset"), at, out width, 1.0f);
		batch.PopMatrix();
	}
}
