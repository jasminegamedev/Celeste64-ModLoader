using Celeste64.Mod;
using System.Text.Json;

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
			controls = ControlsConfig_V01.Defaults;
			using var stream = File.Create(controlsFile);
			JsonSerializer.Serialize(stream, ControlsConfig_V01.Defaults, ControlsConfig_V01Context.Default.ControlsConfig_V01);
			stream.Flush();
		}
		else
		{
			// Add any default actions and sticks that are missing.
			foreach (var action in ControlsConfig_V01.Defaults.Actions)
			{
				if (!controls.Actions.ContainsKey(action.Key))
				{
					controls.Actions.Add(action.Key, action.Value);
				}
			}
			foreach (var stick in ControlsConfig_V01.Defaults.Sticks)
			{
				if (!controls.Sticks.ContainsKey(stick.Key))
				{
					controls.Sticks.Add(stick.Key, stick.Value);
				}
			}
		}

		Instance = controls;
		LoadConfig(Instance);
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, Keys key)
	{
		var config = GetButtonBindings(Instance, virtualButton);
		if (config != null && !config.Any(a => a.Key == key))
		{
			config.Add(new(key));
			config.Last().BindTo(virtualButton);
		}
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, Buttons button)
	{
		var config = GetButtonBindings(Instance, virtualButton);
		if (config != null && !config.Any(a => a.Button == button))
		{
			config.Add(new(button));
			config.Last().BindTo(virtualButton);
		}
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, MouseButtons mouseButton)
	{
		var config = GetButtonBindings(Instance, virtualButton);
		if (config != null && !config.Any(a => a.MouseButton == mouseButton))
		{
			config.Add(new(mouseButton));
			config.Last().BindTo(virtualButton);
		}
	}

	[DisallowHooks]
	internal static void AddBinding(VirtualButton virtualButton, Axes axis, bool inverted)
	{
		var config = GetButtonBindings(Instance, virtualButton);
		if (config != null && !config.Any(a => a.Axis == axis))
		{
			config.Add(new(axis, 0.5f, inverted));
			config.Last().BindTo(virtualButton);
		}
	}

	[DisallowHooks]
	internal static void ClearBinding(VirtualButton virtualButton, bool forController)
	{
		var config = GetButtonBindings(Instance, virtualButton);
		if (config != null)
		{
			virtualButton.Clear();
			// Remove all bindings for this control type
			config.RemoveAll(x => x.IsForController() == forController);

			// Rebind remaining bindings.
			// Needed to rebind other control type, i.e. to not lose controller binds when clearing for keyboard.
			foreach (var binding in config)
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

	public static void LoadConfig(ControlsConfig_V01? config = null)
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
	}

	private static readonly Dictionary<string, Dictionary<string, string>> prompts = [];

	private static string GetControllerName(Gamepads pad) => pad switch
	{
		Gamepads.DualShock4 => "PlayStation",
		Gamepads.DualSense => "PlayStation",
		Gamepads.Nintendo => "Nintendo Switch",
		Gamepads.Xbox => "Xbox Series",
		_ => "Xbox Series",
	};

	public static Subtexture GetPrompt(VirtualButton button)
	{
		return Assets.Subtextures.GetValueOrDefault(GetPromptLocation(button));
	}

	public static string GetPromptLocation(VirtualButton button)
	{
		var gamepad = Input.Controllers[0];

		var deviceTypeName =
					gamepad.Connected ? GetControllerName(gamepad.Gamepad) : "PC";
		if (!prompts.TryGetValue(deviceTypeName, out var list))
			prompts[deviceTypeName] = list = [];

		var action = Instance.Actions.ContainsKey(button.Name) ? Instance.Actions[button.Name] : ControlsConfig_V01.Defaults.Actions[button.Name];

		var binding = action.FirstOrDefault(b => b.IsForController() == gamepad.Connected);

		string buttonName = binding.GetBindingName();

		buttonName = GetButtonOverrides(buttonName, gamepad.Gamepad);

		if (!list.TryGetValue(binding.GetBindingName(), out var lookup))
			list[binding.GetBindingName()] = lookup = $"Controls/{deviceTypeName}/{binding.GetBindingName()}";

		return lookup;
	}

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

				buttonName = GetButtonOverrides(buttonName, gamepad.Gamepad);

				if (!list.TryGetValue(buttonName, out var lookup))
					list[buttonName] = lookup = $"Controls/{promptDeviceTypeName}/{buttonName}";

				if (Gamepads.Nintendo.Equals(binding.NotFor) || !Gamepads.Nintendo.Equals(binding.OnlyFor) || (binding.NotFor == null && binding.OnlyFor == null)) //only non switch prompts atm
					locations.Add(lookup);

			}
		}

		return locations;
	}

	/// <summary>
	/// Used for special cases where there may be multiple options for a button, like with different control types.
	/// </summary>
	/// <param name="buttonName"></param>
	/// <param name="gamepadType"></param>
	/// <returns></returns>
	private static string GetButtonOverrides(string buttonName, Gamepads gamepadType)
	{
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

		return buttonName;
	}
}
