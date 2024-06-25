using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Celeste64;

public class ControlsConfig_V01 : PersistedData
{
	public override int Version => 1;

	public Dictionary<string, List<ControlsConfigBinding>> Actions { get; set; } = [];
	public Dictionary<string, ControlsConfigStick> Sticks { get; set; } = [];

	public override JsonTypeInfo GetTypeInfo()
	{
		return ControlsConfig_V01Context.Default.ControlsConfig_V01;
	}
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	UseStringEnumConverter = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
	AllowTrailingCommas = true,
	Converters = [typeof(ControlsConfigBinding_Converter)]
)]
[JsonSerializable(typeof(ControlsConfig_V01))]
internal partial class ControlsConfig_V01Context : JsonSerializerContext { }
