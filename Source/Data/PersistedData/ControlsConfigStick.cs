using System.Text.Json.Serialization;

namespace Celeste64;

public sealed class ControlsConfigStick
{
	public float Deadzone { get; set; } = 0;
	public List<ControlsConfigBinding> Up { get; set; } = [];
	public List<ControlsConfigBinding> Down { get; set; } = [];
	public List<ControlsConfigBinding> Left { get; set; } = [];
	public List<ControlsConfigBinding> Right { get; set; } = [];

	public void BindTo(VirtualStick stick)
	{
		stick.CircularDeadzone = Deadzone;
		stick.Horizontal.OverlapBehaviour = VirtualAxis.Overlaps.TakeNewer;
		stick.Vertical.OverlapBehaviour = VirtualAxis.Overlaps.TakeNewer;
		foreach (var it in Up)
			it.BindTo(stick.Vertical.Negative);
		foreach (var it in Down)
			it.BindTo(stick.Vertical.Positive);
		foreach (var it in Left)
			it.BindTo(stick.Horizontal.Negative);
		foreach (var it in Right)
			it.BindTo(stick.Horizontal.Positive);
	}
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	UseStringEnumConverter = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
	Converters = [typeof(ControlsConfigBinding_Converter)],
	AllowTrailingCommas = true
)]
[JsonSerializable(typeof(ControlsConfigStick))]
internal partial class ControlsConfigStickContext : JsonSerializerContext { }
