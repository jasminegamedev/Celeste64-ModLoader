using Celeste64.Mod;

namespace Celeste64;

public static class Controls
{
	public static readonly VirtualStick Move = new("Move", VirtualAxis.Overlaps.TakeNewer, 0.35f);
	public static readonly VirtualStick Menu = new("Menu", VirtualAxis.Overlaps.TakeNewer, 0.35f);
	public static readonly VirtualStick Camera = new("Camera", VirtualAxis.Overlaps.TakeNewer, 0.35f);
	public static readonly VirtualButton Jump = new("Jump", .1f);
	public static readonly VirtualButton Dash = new("Dash", .1f);
	public static readonly VirtualButton Climb = new("Climb");
	public static readonly VirtualButton Confirm = new("Confirm");
	public static readonly VirtualButton Cancel = new("Cancel");
	public static readonly VirtualButton Pause = new("Pause");
	public static readonly VirtualButton CopyFile = new("CopyFile");
	public static readonly VirtualButton DeleteFile = new("DeleteFile");
	public static readonly VirtualButton CreateFile = new("CreateFile");
	public static readonly VirtualButton ResetBindings = new("ResetBindings");
	public static readonly VirtualButton ClearBindings = new("ClearBindings");
	public static readonly VirtualButton DebugMenu = new("DebugMenu");
	public static readonly VirtualButton Restart = new("Restart");
	public static readonly VirtualButton FullScreen = new("FullScreen");
	public static readonly VirtualButton ReloadAssets = new("ReloadAssets");

	public static ControlsConfig_V01 Instance = new();

	public const string DefaultFileName = "controls.json";

	[DisallowHooks]
	internal static void LoadControlsByFileName(string file_name)
	{
		if (file_name == string.Empty) file_name = DefaultFileName;
		var controlsFile = Path.Join(App.UserPath, file_name);

		ControlsConfig_V01? controls = null;
		if (File.Exists(controlsFile))
		{
			try
			{
				controls = Instance.Deserialize<ControlsConfig_V01>(File.ReadAllText(controlsFile)) ?? null;
			}
			catch
			{
				controls = null;
			}
		}

		if (controls == null)
		{
			controls = new ControlsConfig_V01();
		}

		// Add any default actions and sticks that are missing.
		foreach (var defaultAction in ControlsConfig_V01.Defaults.Actions)
		{
			if (!controls.Actions.ContainsKey(defaultAction.Key))
			{
				controls.Actions[defaultAction.Key] = [.. defaultAction.Value];
			}
		}
		foreach (var defaultStick in ControlsConfig_V01.Defaults.Sticks)
		{
			if (!controls.Sticks.ContainsKey(defaultStick.Key))
			{
				controls.Sticks[defaultStick.Key] = new ControlsConfigStick()
				{
					Up = [.. defaultStick.Value.Up],
					Down = [.. defaultStick.Value.Down],
					Left = [.. defaultStick.Value.Left],
					Right = [.. defaultStick.Value.Right],
				};
			}
		}

		Instance = controls;
		LoadConfig(Instance);
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, Keys key)
	{
		var bindings = GetButtonBindings(Instance, virtualButton);
		if (bindings != null && !bindings.Any(a => a.Key == key))
		{
			bindings.Add(new(key));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, Buttons button)
	{
		var bindings = GetButtonBindings(Instance, virtualButton);
		if (bindings != null && !bindings.Any(a => a.Button == button))
		{
			bindings.Add(new(button));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, MouseButtons mouseButton)
	{
		var bindings = GetButtonBindings(Instance, virtualButton);
		if (bindings != null && !bindings.Any(a => a.MouseButton == mouseButton))
		{
			bindings.Add(new(mouseButton));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, Axes axis, bool inverted, float deadzone = 0.0f)
	{
		var bindings = GetButtonBindings(Instance, virtualButton);
		if (bindings != null && !bindings.Any(a => a.Axis == axis))
		{
			bindings.Add(new(axis, deadzone, inverted));
			bindings.Last().BindTo(virtualButton);
		}
		Consume();
	}

	[DisallowHooks]
	internal static void ClearBinding(VirtualButton virtualButton, bool forController, bool requiresBinding = false)
	{
		var bindings = GetButtonBindings(Instance, virtualButton);
		if (bindings != null)
		{
			virtualButton.Clear();
			// Remove all bindings for this control type
			if (requiresBinding)
			{
				var toRemove = bindings.Where(x => x.IsForController() == forController).SkipLast(1).ToList();
				foreach (var removeItem in toRemove)
				{
					bindings.Remove(removeItem);
				}
			}
			else
			{
				bindings.RemoveAll(x => x.IsForController() == forController);
			}

			// Rebind remaining bindings.
			// Needed to rebind other control type, i.e. to not lose controller binds when clearing for keyboard.
			foreach (var binding in bindings)
			{
				binding.BindTo(virtualButton);
			}
		}
	}

	[DisallowHooks]
	internal static void ResetBinding(VirtualButton virtualButton, bool forController)
	{
		var bindings = GetButtonBindings(Instance, virtualButton);
		var defaultBindings = GetButtonBindings(ControlsConfig_V01.Defaults, virtualButton);
		if (bindings != null && defaultBindings != null)
		{
			virtualButton.Clear();

			// Remove all bindings for this control type
			bindings.RemoveAll(x => x.IsForController() == forController);

			// Readd all default bindings for this control type
			foreach (var binding in defaultBindings)
			{
				if (binding.IsForController() == forController)
				{
					bindings.Add(binding);
				}
			}

			// Rebind bindings to the virtual button.
			foreach (var binding in bindings)
			{
				binding.BindTo(virtualButton);
			}
		}
	}

	[DisallowHooks]
	internal static void ResetAllBindings(bool forController)
	{
		// Remove all bindings for this control type
		foreach (var action in Instance.Actions)
		{
			Instance.Actions[action.Key].RemoveAll(x => x.IsForController() == forController);
		}
		foreach (var action in Instance.Sticks)
		{
			Instance.Sticks[action.Key].Up.RemoveAll(x => x.IsForController() == forController);
			Instance.Sticks[action.Key].Down.RemoveAll(x => x.IsForController() == forController);
			Instance.Sticks[action.Key].Left.RemoveAll(x => x.IsForController() == forController);
			Instance.Sticks[action.Key].Right.RemoveAll(x => x.IsForController() == forController);
		}

		// Readd default bindings for this control type.
		foreach (var action in ControlsConfig_V01.Defaults.Actions)
		{
			foreach (var binding in action.Value)
			{
				if (binding.IsForController() == forController)
				{
					Instance.Actions[action.Key].Add(binding);
				}
			}
		}
		foreach (var stick in ControlsConfig_V01.Defaults.Sticks)
		{
			Instance.Sticks[stick.Key].Up.AddRange(stick.Value.Up.Where(b => b.IsForController() == forController));
			Instance.Sticks[stick.Key].Down.AddRange(stick.Value.Down.Where(b => b.IsForController() == forController));
			Instance.Sticks[stick.Key].Left.AddRange(stick.Value.Left.Where(b => b.IsForController() == forController));
			Instance.Sticks[stick.Key].Right.AddRange(stick.Value.Right.Where(b => b.IsForController() == forController));
		}

		// Reload bindings
		LoadConfig(Instance);
	}

	[DisallowHooks]
	public static void SaveToFile()
	{
		var savePath = Path.Join(App.UserPath, DefaultFileName);
		var tempPath = Path.Join(App.UserPath, DefaultFileName + ".backup");

		// first save to a temporary file
		{
			using var stream = File.Create(tempPath);
			Instance.Serialize(stream, Instance);
			stream.Flush();
		}

		// validate that the temp path worked, and overwrite existing if it did.
		if (File.Exists(tempPath) &&
			Instance.Deserialize<ControlsConfig_V01>(File.ReadAllText(tempPath)) != null)
		{
			File.Copy(tempPath, savePath, true);
		}
	}

	[DisallowHooks]
	internal static void LoadConfig(ControlsConfig_V01? config = null)
	{
		static ControlsConfigStick FindStick(ControlsConfig_V01? config, string name)
		{
			if (config != null && config.Sticks.TryGetValue(name, out var stick))
				return stick;
			if (ControlsConfig_V01.Defaults.Sticks.TryGetValue(name, out stick))
				return stick;
			throw new Exception($"Missing Stick Binding for '{name}'");
		}

		static List<ControlsConfigBinding> FindAction(ControlsConfig_V01? config, string name)
		{
			if (config != null && config.Actions.TryGetValue(name, out var action))
				return action;
			if (ControlsConfig_V01.Defaults.Actions.TryGetValue(name, out action))
				return action;
			throw new Exception($"Missing Action Binding for '{name}'");
		}

		Clear();

		FindStick(config, "Move").BindTo(Move);
		FindStick(config, "Camera").BindTo(Camera);
		FindStick(config, "Menu").BindTo(Menu);

		foreach (var it in FindAction(config, "Jump"))
			it.BindTo(Jump);
		foreach (var it in FindAction(config, "Dash"))
			it.BindTo(Dash);
		foreach (var it in FindAction(config, "Climb"))
			it.BindTo(Climb);
		foreach (var it in FindAction(config, "Confirm"))
			it.BindTo(Confirm);
		foreach (var it in FindAction(config, "Cancel"))
			it.BindTo(Cancel);
		foreach (var it in FindAction(config, "Pause"))
			it.BindTo(Pause);
		foreach (var it in FindAction(config, "CopyFile"))
			it.BindTo(CopyFile);
		foreach (var it in FindAction(config, "DeleteFile"))
			it.BindTo(DeleteFile);
		foreach (var it in FindAction(config, "CreateFile"))
			it.BindTo(CreateFile);
		foreach (var it in FindAction(config, "ResetBindings"))
			it.BindTo(ResetBindings);
		foreach (var it in FindAction(config, "ClearBindings"))
			it.BindTo(ClearBindings);
		foreach (var it in FindAction(config, "DebugMenu"))
			it.BindTo(DebugMenu);
		foreach (var it in FindAction(config, "Restart"))
			it.BindTo(Restart);
		foreach (var it in FindAction(config, "FullScreen"))
			it.BindTo(FullScreen);
		foreach (var it in FindAction(config, "ReloadAssets"))
			it.BindTo(ReloadAssets);
	}

	public static void Clear()
	{
		Move.Clear();
		Camera.Clear();
		Jump.Clear();
		Dash.Clear();
		Climb.Clear();
		Menu.Clear();
		Confirm.Clear();
		Cancel.Clear();
		Pause.Clear();
		CopyFile.Clear();
		DeleteFile.Clear();
		CreateFile.Clear();
		ResetBindings.Clear();
		ClearBindings.Clear();
		DebugMenu.Clear();
		Restart.Clear();
		FullScreen.Clear();
		ReloadAssets.Clear();
	}

	public static void Consume()
	{
		Move.Consume();
		Menu.Consume();
		Camera.Consume();
		Jump.Consume();
		Dash.Consume();
		Climb.Consume();
		Confirm.Consume();
		Cancel.Consume();
		Pause.Consume();
		CopyFile.Consume();
		DeleteFile.Consume();
		CreateFile.Consume();
		ResetBindings.Consume();
		ClearBindings.Consume();
		DebugMenu.Consume();
		Restart.Consume();
		FullScreen.Consume();
		ReloadAssets.Consume();
	}

	private static readonly Dictionary<string, Dictionary<string, string>> prompts = [];

	/// <summary>
	/// Get the icon texture for the first binding of a virtual button.
	/// </summary>
	/// <param name="button">Virtual button to get an icon for.</param>
	/// <returns></returns>
	public static Subtexture GetPrompt(VirtualButton button)
	{
		return Assets.Subtextures.GetValueOrDefault(GetPromptLocations(button, Input.Controllers.Any() && Input.Controllers[0].Connected).FirstOrDefault(""));
	}

	/// <summary>
	/// Get icon textures for all bindings for a virtual button.
	/// </summary>
	/// <param name="button">Virtual button to get icons for.</param>
	/// <param name="isForController">True if we should show controller icons. False to show keyboard icons.</param>
	/// <returns></returns>
	public static List<Subtexture> GetPrompts(VirtualButton button, bool isForController)
	{
		List<Subtexture> subtextures = [];
		foreach (var location in GetPromptLocations(button, isForController))
			subtextures.Add(Assets.Subtextures.GetValueOrDefault(location));
		return subtextures;
	}

	private static List<string> GetPromptLocations(VirtualButton button, bool isForController)
	{
		List<string> locations = [];
		var gamepad = Input.Controllers[0];
		var deviceTypeName =
			gamepad.Connected ? GetControllerName(gamepad.Gamepad) : "PC";

		var config = GetButtonBindings(Instance, button);

		if (config != null)
		{
			foreach (var binding in config)
			{
				if (binding.IsForController() != isForController)
				{
					continue;
				}

				string promptDeviceTypeName = "PC";
				var buttonName = binding.GetBindingName();
				if (binding.IsForController())
					promptDeviceTypeName = GetControllerName(gamepad.Gamepad);

				if (!prompts.TryGetValue(deviceTypeName, out var list))
					prompts[deviceTypeName] = list = [];

				buttonName = GetButtonOverrides(binding, buttonName, gamepad.Gamepad);

				if (!list.TryGetValue(buttonName, out var lookup))
					list[buttonName] = lookup = $"Controls/{promptDeviceTypeName}/{buttonName}";

				if (Gamepads.Nintendo.Equals(binding.NotFor) || !Gamepads.Nintendo.Equals(binding.OnlyFor) || (binding.NotFor == null && binding.OnlyFor == null)) //only non switch prompts atm
					locations.Add(lookup);
			}
		}

		return locations;
	}

	private static List<ControlsConfigBinding>? GetButtonBindings(ControlsConfig_V01 config, VirtualButton virtualButton)
	{
		string[] nameParts = virtualButton.Name.Split("/");

		if (nameParts.Length == 3)
		{
			var stickConfig = config.Sticks[nameParts[0]];

			if (stickConfig != null)
			{
				if (nameParts[1] == "Horizontal" && nameParts[2] == "Positive")
				{
					return stickConfig.Right;
				}
				else if (nameParts[1] == "Horizontal" && nameParts[2] == "Negative")
				{
					return stickConfig.Left;
				}
				else if (nameParts[1] == "Vertical" && nameParts[2] == "Negative")
				{
					return stickConfig.Up;
				}
				else if (nameParts[1] == "Vertical" && nameParts[2] == "Positive")
				{
					return stickConfig.Down;
				}
			}
		}
		else if (config.Actions.ContainsKey(virtualButton.Name))
		{
			return config.Actions[virtualButton.Name];
		}

		return null;
	}

	/// <summary>
	/// Gets a controller name for a gamepad. This is used to determine which folder we pull icons from.
	/// PS4 and PS5 are shared, since most of their icons are the same. This is different from how this was handled in vanilla Celeste 64, which had them split.
	/// </summary>
	/// <param name="pad"></param>
	/// <returns></returns>
	private static string GetControllerName(Gamepads pad) => pad switch
	{
		Gamepads.DualShock4 => "PlayStation",
		Gamepads.DualSense => "PlayStation",
		Gamepads.Nintendo => "Nintendo Switch",
		Gamepads.Xbox => "Xbox Series",
		_ => "Xbox Series",
	};

	/// <summary>
	/// Used for special cases where there may be multiple options for a button, like with different control types.
	/// </summary>
	/// <param name="buttonName"></param>
	/// <param name="gamepadType"></param>
	/// <returns></returns>
	private static string GetButtonOverrides(ControlsConfigBinding binding, string buttonName, Gamepads gamepadType)
	{
		// Mouse buttons need special names, since Left and Right could refer to Keyboard Left or Mouse Left.
		switch (binding.MouseButton)
		{
			case MouseButtons.Left:
				buttonName = "MouseLeft";
				break;
			case MouseButtons.Right:
				buttonName = "MouseRight";
				break;
			case MouseButtons.Middle:
				buttonName = "MouseMiddle";
				break;
			default:
				break;
		}

		// This is needed to workaround 2 problems with how foster's buttons are set up.
		// Normally, these can get their name from the enum name directly, but these have some special cases.
		// The first issue is that the face buttons have multiple enum entries for the same value, with some being obsolete. We need to force it to get the correct name.
		// The second issue is that Select and Back seem to currently be swapped. So we account for this here. If this gets fixed in foster, we should remove the last 2 cases.
		switch (binding.Button)
		{
			case Buttons.South:
				buttonName = "South";
				break;
			case Buttons.East:
				buttonName = "East";
				break;
			case Buttons.West:
				buttonName = "West";
				break;
			case Buttons.North:
				buttonName = "North";
				break;
			case Buttons.Select:
				buttonName = "Back";
				break;
			case Buttons.Back:
				buttonName = "Select";
				break;
			default:
				break;
		}

		// PS4 has unique icons separate from PS5. We account for that here.
		if (gamepadType == Gamepads.DualShock4)
		{
			if (buttonName == "Start")
			{
				buttonName = "PS4_Start";
			}
			else if (buttonName == "Select")
			{
				buttonName = "PS4_Select";
			}
		}

		// Separate MacOS special names from Windows.
		if (OperatingSystem.IsMacOS())
		{
			if (buttonName == "LeftAlt" || buttonName == "RightAlt")
			{
				buttonName = "Option";
			}
			else if (buttonName == "LeftOS" || buttonName == "RightOS")
			{
				buttonName = "Command";
			}
		}

		return buttonName;
	}
}
