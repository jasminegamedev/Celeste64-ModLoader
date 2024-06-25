using Celeste64.Mod;
using System.Reflection;

namespace Celeste64;

public class ModControlsMenu : Menu
{
	private Target GameTarget;

	//Items list always includes the Back item and reset to default, so check if it's more than 2 to know if we should display it.
	internal bool ShouldDisplay => items.Count > 2;

	private GameMod? Mod;

	public override void Closed()
	{
		base.Closed();
		if (Mod != null)
		{
			Mod.SaveSettings();
		}
	}

	public ModControlsMenu(Menu? rootMenu)
	{
		RootMenu = rootMenu;
		GameTarget = new Target(Game.Width, Game.Height);
		Game.OnResolutionChanged += () => GameTarget = new Target(Game.Width, Game.Height);
	}

	public void AddItems(GameMod mod, bool isForController)
	{
		Mod = mod;
		items.Clear();
		if (mod.SettingsType != null)
		{
			foreach (var prop in mod.SettingsType.GetProperties())
			{
				var propType = prop.PropertyType;

				if (prop.GetCustomAttribute<SettingIgnoreAttribute>() != null)
					continue;

				string propName = prop.Name;
				string? nameAttibute = prop.GetCustomAttribute<SettingNameAttribute>()?.Name;
				if (!string.IsNullOrEmpty(nameAttibute))
				{
					propName = Loc.TryGetModString(mod, nameAttibute, out string localizedName) ?
						localizedName :
						nameAttibute;
				}

				Menu.Item? newItem = null;

				if (prop.GetCustomAttribute<SettingSpacerAttribute>() != null)
				{
					Add(new Spacer());
				}

				bool changingNeedsReload = prop.GetCustomAttribute<SettingNeedsReloadAttribute>() != null;

				string? subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
				if (!string.IsNullOrEmpty(subheader))
				{
					string subHeader = Loc.TryGetModString(mod, subheader, out string localizedSubHeader) ?
						localizedSubHeader :
						subheader;
					Add(new SubHeader((Loc.Unlocalized)subHeader));
				}

				if (prop.PropertyType == typeof(VirtualButton))
				{
					VirtualButton? vb = prop.GetValue(mod.Settings) as VirtualButton;
					if (vb != null)
					{
						Add(new InputBind((Loc.Unlocalized)(propName), vb, RootMenu, isForController, mod));
					}
				}
				if (prop.PropertyType == typeof(VirtualStick))
				{
					VirtualStick? vs = prop.GetValue(mod.Settings) as VirtualStick;
					if (vs != null)
					{
						Add(new InputBind((Loc.Unlocalized)(propName + " Up"), vs.Vertical.Negative, RootMenu, isForController, mod));
						Add(new InputBind((Loc.Unlocalized)(propName + " Down"), vs.Vertical.Positive, RootMenu, isForController, mod));
						Add(new InputBind((Loc.Unlocalized)(propName + " Left"), vs.Horizontal.Negative, RootMenu, isForController, mod));
						Add(new InputBind((Loc.Unlocalized)(propName + " Right"), vs.Horizontal.Positive, RootMenu, isForController, mod));
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

		UI.Prompt(batch, Controls.ClearBindings, Loc.Str("Clear"), at, out width, 1.0f);
		at.X -= width + 8 * Game.RelativeScale;

		UI.Prompt(batch, Controls.ResetBindings, Loc.Str("Reset"), at, out width, 1.0f);
		batch.PopMatrix();
	}
}
