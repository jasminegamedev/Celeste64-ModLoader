using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celeste64;

public sealed class ControlsConfigBinding
{
	public Keys? Key { get; set; }
	public MouseButtons? MouseButton { get; set; }
	public Buttons? Button { get; set; }
	public Axes? Axis { get; set; }
	public float AxisDeadzone { get; set; }
	public bool AxisInverted { get; set; }
	public Gamepads[]? ForGamepads { get; set; }

	public ControlsConfigBinding() { }
	public ControlsConfigBinding(Keys input) => Key = input;
	public ControlsConfigBinding(MouseButtons input) => MouseButton = input;
	public ControlsConfigBinding(Buttons input) => Button = input;
	public ControlsConfigBinding(Axes input, float deadzone, bool inverted)
	{
		Axis = input;
		AxisDeadzone = deadzone;
		AxisInverted = inverted;
	}

	private bool Condition(VirtualButton vb, VirtualButton.IBinding binding)
	{
		if (ForGamepads == null || !ForGamepads.Any())
			return true;

		int index;
		if (binding is VirtualButton.ButtonBinding btn)
			index = btn.Controller;
		else if (binding is VirtualButton.AxisBinding axs)
			index = axs.Controller;
		else
			return true;

		if (!ForGamepads.Contains(Input.Controllers[index].Gamepad))
			return false;

		return true;
	}

	public void BindTo(VirtualButton button)
	{
		if (Key.HasValue)
			button.Add(Key.Value);

		if (Button.HasValue)
			button.Add(Condition, 0, Button.Value);

		if (MouseButton.HasValue)
			button.Add(MouseButton.Value);

		if (Axis.HasValue)
			button.Add(Condition, 0, Axis.Value, AxisInverted ? -1 : 1, AxisDeadzone);
	}

	public string GetBindingName()
	{
		if (Key != null)
			return Key.ToString() ?? "";
		if (Button != null)
			return Button.ToString() ?? "";
		if (Axis != null)
			return Axis.ToString() + (AxisInverted ? "Negative" : "Positive");
		if (MouseButton != null)
			return MouseButton.ToString() ?? "";
		return "";
	}

	public bool IsForController()
	{
		if (Key != null)
			return false;
		if (Button != null)
			return true;
		if (Axis != null)
			return true;
		if (MouseButton != null)
			return false;
		return false;
	}
}

public class ControlsConfigBinding_Converter : JsonConverter<ControlsConfigBinding>
{
	public override ControlsConfigBinding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (var jsonDoc = JsonDocument.ParseValue(ref reader))
		{
			return JsonSerializer.Deserialize(jsonDoc.RootElement.GetRawText(), ControlsConfigBindingContext.Default.ControlsConfigBinding);
		}
	}

	public override void Write(Utf8JsonWriter writer, ControlsConfigBinding value, JsonSerializerOptions options)
	{
		// All of this is just so the Binding values are on a single line to increase readability
		var data =
			"\n" +
			new string(' ', writer.CurrentDepth * 2) +
			JsonSerializer.Serialize(value, ControlsConfigBindingContext.Default.ControlsConfigBinding);
		writer.WriteRawValue(data);
	}
}

[JsonSourceGenerationOptions(
	UseStringEnumConverter = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
	AllowTrailingCommas = true
)]
[JsonSerializable(typeof(ControlsConfigBinding))]
internal partial class ControlsConfigBindingContext : JsonSerializerContext { }
