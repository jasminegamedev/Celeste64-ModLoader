using System.Reflection;

namespace Celeste64.Mod.Editor;

public class MoveTool : Tool
{
	public override string Name => "Move";
	public override Gizmo[] Gizmos => gizmo == null ? [] : [gizmo];

	private Gizmo? gizmo;
	
	public override void Awake(EditorWorld editor)
	{
		editor.OnDefinitionSelected += def =>
		{
			if (def is null)
			{
				gizmo = null;
				return;
			}
		
			var positionProp = def
				.GetType()
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(prop =>
					!prop.HasAttr<IgnorePropertyAttribute>() &&
					prop.GetCustomAttribute<SpecialPropertyAttribute>() is { Value: SpecialPropertyType.PositionXYZ });

			if (positionProp is null || positionProp.GetGetMethod() is not { } getMethod || positionProp.GetSetMethod() is not { } setMethod)
				return;

			gizmo = new PositionGizmo(
				() => (Vec3)getMethod.Invoke(def, [])!,
				newValue =>
				{
					setMethod.Invoke(def, [newValue]);
					def.Dirty = true;
				},
				scale: 1.0f);
		};
	}
}
