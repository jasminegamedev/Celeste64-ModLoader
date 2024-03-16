using System.Reflection;

namespace Celeste64.Mod.Editor;

public interface ICustomProperty<T>
{
	public static abstract void Serialize(T value, BinaryWriter writer);
	public static abstract T Deserialize(BinaryReader reader);
	public static abstract bool RenderGui(ref T value);
}

[AttributeUsage(AttributeTargets.Property)]
public class CustomPropertyAttribute(Type type) : Attribute
{
	private readonly MethodInfo m_Serialize = type.GetMethod(nameof(ICustomProperty<object>.Serialize), BindingFlags.Public | BindingFlags.Static)
										   ?? throw new Exception($"Custom property definition {type} does not inherit from ICustomProperty");

	private readonly MethodInfo m_Deserialize = type.GetMethod(nameof(ICustomProperty<object>.Deserialize), BindingFlags.Public | BindingFlags.Static)
											 ?? throw new Exception($"Custom property definition {type} does not inherit from ICustomProperty");

	private readonly MethodInfo m_RenderGui = type.GetMethod(nameof(ICustomProperty<object>.RenderGui), BindingFlags.Public | BindingFlags.Static)
										   ?? throw new Exception($"Custom property definition {type} does not inherit from ICustomProperty");

	internal void Serialize(object value, BinaryWriter writer) => m_Serialize.Invoke(null, [value, writer]);
	internal object Deserialize(BinaryReader reader) => m_Deserialize.Invoke(null, [reader])!;
	internal bool RenderGui(ref object value) => (bool)m_RenderGui.Invoke(null, [value])!;
}

