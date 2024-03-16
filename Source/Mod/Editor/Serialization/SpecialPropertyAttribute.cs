namespace Celeste64.Mod.Editor;

public enum SpecialPropertyType
{
	PositionXYZ,
}

[AttributeUsage(AttributeTargets.Property)]
public class SpecialPropertyAttribute(SpecialPropertyType value) : Attribute
{
	public readonly SpecialPropertyType Value = value;
}
