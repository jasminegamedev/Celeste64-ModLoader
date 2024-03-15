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
		float scale = Math.Max(minScale, Vec3.Distance(EditorWorld.Current.Camera.Position, selected.Position) / 20.0f);
		
		float cubeSize = 0.15f * scale;
		float planeSize = 0.6f * scale;
		float padding = 0.15f * scale;
		float axisLen = 1.5f * scale;
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
		
		var xAxisColor = false ? xColorSelected : xColorDeselected;
		var yAxisColor = false ? yColorSelected : yColorDeselected;
		var zAxisColor = false ? zColorSelected : zColorDeselected;
		
		var xzPlaneColor = false ? yColorSelected : yColorDeselected;
		var yzPlaneColor = false ? xColorSelected : xColorDeselected;
		var xyPlaneColor = false ? zColorSelected : zColorDeselected;
		
		// X
		batch3D.Line(selected.Position + Vec3.UnitX * (cubeSize + padding), selected.Position + Vec3.UnitX * axisLen, xAxisColor, axisRadius);
		batch3D.Cone(selected.Position + Vec3.UnitX * axisLen, Batcher3D.Direction.X, coneLen, coneRadius, 12, xAxisColor);
		// Y
		batch3D.Line(selected.Position + Vec3.UnitY * (cubeSize + padding), selected.Position + Vec3.UnitY * axisLen, yAxisColor, axisRadius);
		batch3D.Cone(selected.Position + Vec3.UnitY * axisLen, Batcher3D.Direction.Y, coneLen, coneRadius, 12, yAxisColor);
		// Z
		batch3D.Line(selected.Position + Vec3.UnitZ * (cubeSize + padding), selected.Position + Vec3.UnitZ * axisLen, zAxisColor, axisRadius);
		batch3D.Cone(selected.Position + Vec3.UnitZ * axisLen, Batcher3D.Direction.Z, coneLen, coneRadius, 12, zAxisColor);
		
		// XZ
		batch3D.Square(selected.Position + Vec3.UnitX * (cubeSize + axisLen / 2.0f) + Vec3.UnitZ * (cubeSize + axisLen / 2.0f), 
					   Vec3.UnitY, xzPlaneColor, planeSize / 2.0f);
		// YZ
		batch3D.Square(selected.Position + Vec3.UnitY * (cubeSize + axisLen / 2.0f) + Vec3.UnitZ * (cubeSize + axisLen / 2.0f), 
			           Vec3.UnitX, yzPlaneColor, planeSize / 2.0f);
		// XY
		batch3D.Square(selected.Position + Vec3.UnitX * (cubeSize + axisLen / 2.0f) + Vec3.UnitY * (cubeSize + axisLen / 2.0f), 
			           Vec3.UnitZ, xyPlaneColor, planeSize / 2.0f);

		// XYZ
		batch3D.Cube(selected.Position, Color.White, cubeSize);
	}
}

