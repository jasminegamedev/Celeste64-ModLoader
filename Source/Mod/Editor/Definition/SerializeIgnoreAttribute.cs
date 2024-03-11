using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Celeste64.Mod.Editor;

[AttributeUsage(AttributeTargets.Property)]
public class SerializeIgnoreAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class SerializeCustomAttribute(Type type) : Attribute
{
	private readonly MethodInfo m_Serialize = type.GetMethod(nameof(CustomPropertySerializer<object>.Serialize), BindingFlags.Public | BindingFlags.Static)
	                                          ?? throw new Exception($"Custom property serializer {type} does not inherit from CustomPropertySerializer");
	private readonly MethodInfo m_Deserialize = type.GetMethod(nameof(CustomPropertySerializer<object>.Deserialize), BindingFlags.Public | BindingFlags.Static) 
	                                           ?? throw new Exception($"Custom property serializer {type} does not inherit from CustomPropertySerializer");
	
	internal void Serialize(object value, BinaryWriter writer) => m_Serialize.Invoke(null, [value, writer]);
	internal object Deserialize(BinaryReader reader) => m_Deserialize.Invoke(null, [reader])!;
}

public interface CustomPropertySerializer<T>
{
	public static abstract void Serialize(T value, BinaryWriter writer);
	public static abstract T Deserialize(BinaryReader reader);
}