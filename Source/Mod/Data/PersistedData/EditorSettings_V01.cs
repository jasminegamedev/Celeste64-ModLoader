using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Celeste64;

public sealed class EditorSettings_V01 : PersistedData
{
	public override int Version => 1;
	
	// Settings
	public bool PlayMusic { get; set; } = false;
	public bool PlayAmbience { get; set; } = false;

	// View
	public bool RenderSnow { get; set; } = false;
	public bool RenderSkybox { get; set; } = true;

	public const float MinRenderDistance = 500.0f;
	public const float MaxRenderDistance = 5000.0f;
	private float renderDistance = 4000.0f;
	public float RenderDistance
	{
		get => renderDistance;
		set => renderDistance = Math.Clamp(value, MinRenderDistance, MaxRenderDistance);
	}

	public enum Resolution { Game = 0, Double = 1, HD = 2, Native = 3 }
	public Resolution ResolutionType { get; set; } = Resolution.Double;

	public override JsonTypeInfo GetTypeInfo()
	{
		return EditorSettings_V01Context.Default.EditorSettings_V01;
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(EditorSettings_V01))]
internal partial class EditorSettings_V01Context : JsonSerializerContext { }
