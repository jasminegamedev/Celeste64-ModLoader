namespace Celeste64;

public class ControlsMenu : Menu
{
	private Target GameTarget;


	public override void Closed()
	{
		base.Closed();
		Controls.SaveToFile();
	}

	public override void Initialized()
	{
		Index = 1;
	}

	public ControlsMenu(Menu? rootMenu)
	{
		RootMenu = rootMenu;

		GameTarget = new Target(Game.Width, Game.Height);
		Game.OnResolutionChanged += () => GameTarget = new Target(Game.Width, Game.Height);

		Title = Loc.Str("ControlsTitle");

		Add(new SubHeader("Game"));
		Add(new InputBind(Controls.Jump.Name, Controls.Jump, rootMenu));
		Add(new InputBind(Controls.Dash.Name, Controls.Dash, rootMenu));
		Add(new InputBind(Controls.Climb.Name, Controls.Climb, rootMenu));
		Add(new InputBind(Controls.Pause.Name, Controls.Pause, rootMenu));

		//Add(new SubHeader("Menu"));
		//Add(new InputBind(Controls.Confirm.Name, Controls.Confirm, rootMenu));
		//Add(new InputBind(Controls.Cancel.Name, Controls.Cancel, rootMenu));
		//Add(new InputBind(Controls.CopyFile.Name, Controls.CopyFile, rootMenu));
		//Add(new InputBind(Controls.CreateFile.Name, Controls.CreateFile, rootMenu));
		//Add(new InputBind(Controls.DeleteFile.Name, Controls.DeleteFile, rootMenu));

		Add(new Spacer());

		Add(new Option("ResetToDefault", () =>
		{
			Controls.ResetBindings();
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
				Controls.ResetBinding(bind.GetButton());
			}
		}

		if (Controls.CreateFile.ConsumePress())
		{
			if (items[Index] is InputBind bind)
			{
				Controls.ClearBinding(bind.GetButton());
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
