namespace Celeste64;

public class BindControlMenu : Menu
{
	private float inMenuTime = 0;

	private const float waitBeforeClosingTime = 5;
	private VirtualButton button;
	private bool isForController;
	private float deadZone;

	public BindControlMenu(Menu? rootMenu, VirtualButton button, bool isForController, float deadZone = 0)
	{
		RootMenu = rootMenu;

		this.button = button;
		this.deadZone = deadZone;
		this.isForController = isForController;
		Title = Loc.Str($"Press Button to bind {button.Name}");
		inMenuTime = 0;
	}

	protected override void HandleInput()
	{
		if (!isForController)
		{
			Keys? pressed = Input.Keyboard.FirstPressed();
			if (pressed != null)
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

		inMenuTime += Time.Delta;
		if (inMenuTime > waitBeforeClosingTime)
		{
			RootMenu?.PopSubMenu();
		}
	}
}
