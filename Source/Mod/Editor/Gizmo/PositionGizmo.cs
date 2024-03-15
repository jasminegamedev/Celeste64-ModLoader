namespace Celeste64.Mod.Editor;

public class PositionGizmo : Gizmo
{
	public override void Render(ref RenderState state)
	{
		if (EditorWorld.Current.Selected is not { } selected)
			return;
		
		
	}
	
	public void Render2(ref RenderState state, Batcher3D batch3D)
	{
		if (EditorWorld.Current.Selected is not SpikeBlock.Definition selected)
			return;
		
		const float minScale = 10.0f;
		float scale = Math.Max(minScale, Vec3.Distance(EditorWorld.Current.Camera.Position, selected.Position) / 50.0f);
		
		float axisLen = 2.5f * scale;
		float axisRadius = axisLen / 35.0f;
		float coneLen = axisLen / 2.5f;
		float coneRadius = coneLen / 3.0f;
		
		// const byte selectedAlpha = 0x7f;
		// const byte deselectedAlpha = 0x2f;
		const byte selectedAlpha = 0xff;
		const byte deselectedAlpha = 0xff;
		
		var xColorSelected = new Color(0xff9999, selectedAlpha);
		var yColorSelected = new Color(0xbfffbf, selectedAlpha);
		var zColorSelected = new Color(0x8080ff, selectedAlpha);
		var xColorDeselected = new Color(0xbf0000, deselectedAlpha);
		var yColorDeselected = new Color(0x00bf00, deselectedAlpha);
		var zColorDeselected = new Color(0x0000bf, deselectedAlpha);
		
		var xColor = false ? xColorSelected : xColorDeselected;
		var yColor = false ? yColorSelected : yColorDeselected;
		var zColor = false ? zColorSelected : zColorDeselected;
		
		// X
		batch3D.Line(selected.Position, selected.Position + Vec3.UnitX * axisLen, xColor, axisRadius);
		batch3D.Cone(selected.Position + Vec3.UnitX * axisLen, Batcher3D.Direction.X, coneLen, coneRadius, 12, xColor);
		// Y
		batch3D.Line(selected.Position, selected.Position + Vec3.UnitY * axisLen, yColor, axisRadius);
		batch3D.Cone(selected.Position + Vec3.UnitY * axisLen, Batcher3D.Direction.Y, coneLen, coneRadius, 12, yColor);
		// Z
		batch3D.Line(selected.Position, selected.Position + Vec3.UnitZ * axisLen, zColor, axisRadius);
		batch3D.Cone(selected.Position + Vec3.UnitZ * axisLen, Batcher3D.Direction.Z, coneLen, coneRadius, 12, zColor);
	}
}

