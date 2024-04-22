namespace Celeste64;

public class ControlsMenu : Menu
{
	private Target GameTarget;

	private bool isForController;


	public override void Closed()
	{
		base.Closed();
		Controls.SaveToFile();
	}

	public override void Initialized()
	{
		Index = 1;
	}

	public ControlsMenu(Menu? rootMenu, bool isForController)
	{
		RootMenu = rootMenu;
		this.isForController = isForController;

		GameTarget = new Target(Game.Width, Game.Height);
		Game.OnResolutionChanged += () => GameTarget = new Target(Game.Width, Game.Height);

		Add(new SubHeader("ControlsHeaderGame"));
		Add(new InputBind(Controls.Jump.Name, Controls.Jump, rootMenu, isForController));
		Add(new InputBind(Controls.Dash.Name, Controls.Dash, rootMenu, isForController));
		Add(new InputBind(Controls.Climb.Name, Controls.Climb, rootMenu, isForController));
		Add(new InputBind(Controls.Move.Name + "Up", Controls.Move.Vertical.Negative, rootMenu, isForController));
		Add(new InputBind(Controls.Move.Name + "Down", Controls.Move.Vertical.Positive, rootMenu, isForController));
		Add(new InputBind(Controls.Move.Name + "Left", Controls.Move.Horizontal.Negative, rootMenu, isForController));
		Add(new InputBind(Controls.Move.Name + "Right", Controls.Move.Horizontal.Positive, rootMenu, isForController));
		Add(new InputBind(Controls.Camera.Name + "Up", Controls.Camera.Vertical.Negative, rootMenu, isForController));
		Add(new InputBind(Controls.Camera.Name + "Down", Controls.Camera.Vertical.Positive, rootMenu, isForController));
		Add(new InputBind(Controls.Camera.Name + "Left", Controls.Camera.Horizontal.Negative, rootMenu, isForController));
		Add(new InputBind(Controls.Camera.Name + "Right", Controls.Camera.Horizontal.Positive, rootMenu, isForController));

		Add(new SubHeader("ControlsHeaderMenu"));
		Add(new InputBind(Controls.Pause.Name, Controls.Pause, rootMenu, isForController) { RequiresBinding = true });
		Add(new InputBind(Controls.Confirm.Name, Controls.Confirm, rootMenu, isForController) { RequiresBinding = true });
		Add(new InputBind(Controls.Cancel.Name, Controls.Cancel, rootMenu, isForController) { RequiresBinding = true });
		Add(new InputBind(Controls.CopyFile.Name, Controls.CopyFile, rootMenu, isForController) { RequiresBinding = true });
		Add(new InputBind(Controls.CreateFile.Name, Controls.CreateFile, rootMenu, isForController) { RequiresBinding = true });
		Add(new InputBind(Controls.DeleteFile.Name, Controls.DeleteFile, rootMenu, isForController) { RequiresBinding = true });
		Add(new InputBind(Controls.Menu.Name + "Up", Controls.Menu.Vertical.Negative, rootMenu, isForController) { DeadZone = 0.5f, RequiresBinding = true });
		Add(new InputBind(Controls.Menu.Name + "Down", Controls.Menu.Vertical.Positive, rootMenu, isForController) { DeadZone = 0.5f, RequiresBinding = true });
		Add(new InputBind(Controls.Menu.Name + "Left", Controls.Menu.Horizontal.Negative, rootMenu, isForController) { DeadZone = 0.5f, RequiresBinding = true });
		Add(new InputBind(Controls.Menu.Name + "Right", Controls.Menu.Horizontal.Positive, rootMenu, isForController) { DeadZone = 0.5f, RequiresBinding = true });
		Add(new SubHeader("ControlsHeaderAdvanced"));
		Add(new InputBind(Controls.DebugMenu.Name, Controls.DebugMenu, rootMenu, isForController));
		Add(new InputBind(Controls.Restart.Name, Controls.Restart, rootMenu, isForController));

		Add(new Spacer());

		Add(new Option("ControlsResetToDefault", () =>
		{
			Controls.ResetAllBindings(isForController);
		}));

		Add(new Option("Exit", () =>
		{
			PopRootSubMenu();
		}));
	}

	protected override void HandleInput()
	{
		base.HandleInput();

		if (Controls.CopyFile.ConsumePress())
		{
			if (items[Index] is InputBind bind)
			{
				Controls.ResetBinding(bind.GetButton(), isForController);
			}
		}

		if (Controls.CreateFile.ConsumePress())
		{
			if (items[Index] is InputBind bind)
			{
				Controls.ClearBinding(bind.GetButton(), isForController, bind.RequiresBinding);
			}
		}
	}

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
