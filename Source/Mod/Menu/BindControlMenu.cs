namespace Celeste64;

public class BindControlMenu : Menu
{
	private float inMenuTime = 0;

	private const float waitBeforeClosingTime = 5;
	private VirtualButton button;
	private bool isForController;
	private float deadZone;

	public BindControlMenu(Menu? rootMenu, VirtualButton button, string buttonName, bool isForController, float deadZone = 0)
	{
		RootMenu = rootMenu;

		this.button = button;
		this.deadZone = deadZone;
		this.isForController = isForController;
		Title = string.Format(Loc.Str("PressToBind"), buttonName);
		TitleScale = 1;

		inMenuTime = 0;
	}

	protected override void HandleInput()
	{
		// Only accept keys and mouse buttons if this is not for controller. Otherwise, only accept controller buttons and sticks
		if (!isForController)
		{
			Keys? pressed = Input.Keyboard.FirstPressed();
			if (pressed != null && pressed != Keys.Unknown && pressed != Keys.Application && pressed != Keys.ScrollLock) // We don't have icons for these currently, so don't allow them
			{
				Controls.AddBinding(button, (Keys)pressed);
				RootMenu?.PopSubMenu();
				return;
			}

			// TODO: Maybe find a better way to do this? Foster doesn't expose a FirstPressed for mouse buttons.
			foreach (var button in Enum.GetValues(typeof(MouseButtons)))
			{
				if (Input.Mouse.Pressed((MouseButtons)button))
				{
					Controls.AddBinding(this.button, (MouseButtons)button);
					RootMenu?.PopSubMenu();
					return;
				}
			}
		}
		else
		{
			// TODO: Maybe find a better way to do this? Foster doesn't expose a FirstPressed for buttons.
			Controller? controller = Input.Controllers.FirstOrDefault();
			if (controller != null)
			{
				foreach (var button in Enum.GetValues(typeof(Buttons)))
				{
					if (controller.Pressed((Buttons)button))
					{
						Controls.AddBinding(this.button, (Buttons)button);
						RootMenu?.PopSubMenu();
						return;
					}
				}

				foreach (var axis in Enum.GetValues(typeof(Axes)))
				{
					if (Math.Abs(controller.Axis((Axes)axis)) > 0.5f)
					{
						Controls.AddBinding(this.button, (Axes)axis, controller.Axis((Axes)axis) < -0.5f, deadZone);
						RootMenu?.PopSubMenu();
						return;
					}
				}
			}
		}

		// Closes automatically after <waitBeforeClosingTime> seconds
		// This is to prevent users from being forced to bind if they click by mistake.
		inMenuTime += Time.Delta;
		if (inMenuTime > waitBeforeClosingTime)
		{
			RootMenu?.PopSubMenu();
		}
	}

	protected override void RenderItems(Batcher batch)
	{
		batch.PushMatrix(Matrix3x2.CreateScale(TitleScale));
		UI.Text(batch, Title, Vec2.Zero, new Vec2(0.5f, 0), Color.White);
		batch.PopMatrix();

		batch.PushMatrix(Matrix3x2.CreateScale(0.75f));
		UI.Text(batch, $"Menu will close automatically in {(int)Math.Ceiling(waitBeforeClosingTime - inMenuTime)}", new(0, Game.Height * 0.15f), new Vec2(0.5f, 0), Color.White);
		batch.PopMatrix();
	}
}
