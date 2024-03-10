namespace Celeste64.Mod.Editor;

public class EditorDefinition
{
	// TODO: Figure out how to let definitions mark support for certain special properties like position / rotation / scale
	public virtual Vec3 Position { get; set; } = Vector3.Zero;
	public virtual Vec3 Rotation { get; set; } = Vector3.Zero;
	public virtual Vec3 Scale { get; set; } = Vector3.One;
	
	public virtual void Render(ref EditorRenderState state) { }
}