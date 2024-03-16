namespace Celeste64.Mod.Editor;

[AttributeUsage(AttributeTargets.Property)]
public class SpecialPropertyAttribute(SpecialPropertyType value) : Attribute
{
	public readonly SpecialPropertyType Value = value;
}

public enum SpecialPropertyType
{
	PositionXYZ,
}
