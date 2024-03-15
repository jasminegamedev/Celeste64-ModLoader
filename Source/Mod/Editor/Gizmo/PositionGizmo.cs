namespace Celeste64.Mod.Editor;

public class PositionGizmo : Gizmo
{
	public override void Render(ref RenderState state)
	{
		if (EditorWorld.Current.Selected is not { } selected)
			return;
		
		
	}
	
	public GizmoTarget Target;
	
	static float cubeSize = 0.15f;
	static float planeSize = 0.6f;
	static float padding = 0.15f;
	static float axisLen = 1.5f;
	static float axisRadius => axisLen / 35.0f;
	static float coneLen => axisLen / 2.5f;
	static float coneRadius => coneLen / 3.0f;
	
	static float boundsPadding => 0.1f;

	// Axis
	static float axisBoundsLengthMin => cubeSize + padding;
	static float axisBoundsLengthMax => axisLen + coneLen * 0.9f;
	static float axisBoundsRadiusMin => -axisRadius - boundsPadding;
	static float axisBoundsRadiusMax => axisRadius + boundsPadding;
	
	public BoundingBox xAxisBounds => new(
		new Vec3(axisBoundsLengthMin, axisBoundsRadiusMin, axisBoundsRadiusMin),
		new Vec3(axisBoundsLengthMax, axisBoundsRadiusMax, axisBoundsRadiusMax));
	
	public BoundingBox yAxisBounds => new(
		new Vec3(axisBoundsRadiusMin, axisBoundsLengthMin, axisBoundsRadiusMin),
		new Vec3(axisBoundsRadiusMax, axisBoundsLengthMax, axisBoundsRadiusMax));
	
	public BoundingBox zAxisBounds => new(
		new Vec3(axisBoundsRadiusMin, axisBoundsRadiusMin, axisBoundsLengthMin),
		new Vec3(axisBoundsRadiusMax, axisBoundsRadiusMax, axisBoundsLengthMax));
	
	// Planes
	static float planeBoundsMin => cubeSize + axisLen / 2.0f - planeSize / 2.0f - boundsPadding;
	static float planeBoundsMax => cubeSize + axisLen / 2.0f + planeSize / 2.0f + boundsPadding;
	
	public BoundingBox xzPlaneBounds => new(
		new Vec3(planeBoundsMin, 0.0f, planeBoundsMin),
		new Vec3(planeBoundsMax, 0.0f, planeBoundsMax));
	
	public BoundingBox yzPlaneBounds => new(
		new Vec3(0.0f, planeBoundsMin, planeBoundsMin),
		new Vec3(0.0f, planeBoundsMax, planeBoundsMax));
	
	public BoundingBox xyPlaneBounds => new(
		new Vec3(planeBoundsMin, planeBoundsMin, 0.0f),
		new Vec3(planeBoundsMax, planeBoundsMax, 0.0f));
	
	// Cube
	public BoundingBox xyzCubeBounds => new(
		-new Vec3(cubeSize + boundsPadding),
		 new Vec3(cubeSize + boundsPadding));
	
	public Matrix Transform
	{
		get
		{
			if (EditorWorld.Current.Selected is not SpikeBlock.Definition selected)
				return Matrix.Identity;
			
			const float minScale = 10.0f;
			float scale = Math.Max(minScale, Vec3.Distance(EditorWorld.Current.Camera.Position, selected.Position) / 20.0f);
			
			return Matrix.CreateScale(scale) * 
				   Matrix.CreateTranslation(selected.Position);
		}
	}

	public void Render2(ref RenderState state, Batcher3D batch3D)
	{
		if (EditorWorld.Current.Selected is not SpikeBlock.Definition selected)
			return;
		
		// const byte selectedAlpha = 0x7f;
		// const byte deselectedAlpha = 0x2f;
		const byte selectedAlpha = 0xff;
		const byte deselectedAlpha = 0xff;
		
		var xColorSelected = new Color(0xff9999, selectedAlpha);
		var yColorSelected = new Color(0xbfffbf, selectedAlpha);
		var zColorSelected = new Color(0x8080ff, selectedAlpha);
		var cubeColorSelected = new Color(0xffffff, selectedAlpha);
		var xColorDeselected = new Color(0xbf0000, deselectedAlpha);
		var yColorDeselected = new Color(0x00bf00, deselectedAlpha);
		var zColorDeselected = new Color(0x0000bf, deselectedAlpha);
		var cubeColorDeselected = new Color(0xbfbfbf, deselectedAlpha);
		
		var xAxisColor = Target == GizmoTarget.AxisX ? xColorSelected : xColorDeselected;
		var yAxisColor = Target == GizmoTarget.AxisY ? yColorSelected : yColorDeselected;
		var zAxisColor = Target == GizmoTarget.AxisZ ? zColorSelected : zColorDeselected;
		
		var xzPlaneColor = Target == GizmoTarget.PlaneXZ ? yColorSelected : yColorDeselected;
		var yzPlaneColor = Target == GizmoTarget.PlaneYZ ? xColorSelected : xColorDeselected;
		var xyPlaneColor = Target == GizmoTarget.PlaneXY ? zColorSelected : zColorDeselected;
		
		var xyzCubeColor = Target == GizmoTarget.CubeXYZ ? cubeColorSelected : cubeColorDeselected; 
		
		// X
		batch3D.Line(Vec3.UnitX * (cubeSize + padding), Vec3.UnitX * axisLen, xAxisColor, Transform, axisRadius);
		batch3D.Cone(Vec3.UnitX * axisLen, Batcher3D.Direction.X, coneLen, coneRadius, 12, xAxisColor, Transform);
		// Y
		batch3D.Line(Vec3.UnitY * (cubeSize + padding), Vec3.UnitY * axisLen, yAxisColor, Transform, axisRadius);
		batch3D.Cone(Vec3.UnitY * axisLen, Batcher3D.Direction.Y, coneLen, coneRadius, 12, yAxisColor, Transform);
		// Z
		batch3D.Line(Vec3.UnitZ * (cubeSize + padding), Vec3.UnitZ * axisLen, zAxisColor, Transform, axisRadius);
		batch3D.Cone(Vec3.UnitZ * axisLen, Batcher3D.Direction.Z, coneLen, coneRadius, 12, zAxisColor, Transform);
		
		// XZ
		batch3D.Square(Vec3.UnitX * (cubeSize + axisLen / 2.0f) + Vec3.UnitZ * (cubeSize + axisLen / 2.0f), 
					   Vec3.UnitY, xzPlaneColor, Transform, planeSize / 2.0f);
		// YZ
		batch3D.Square(Vec3.UnitY * (cubeSize + axisLen / 2.0f) + Vec3.UnitZ * (cubeSize + axisLen / 2.0f), 
			           Vec3.UnitX, yzPlaneColor, Transform, planeSize / 2.0f);
		// XY
		batch3D.Square(Vec3.UnitX * (cubeSize + axisLen / 2.0f) + Vec3.UnitY * (cubeSize + axisLen / 2.0f), 
			           Vec3.UnitZ, xyPlaneColor, Transform, planeSize / 2.0f);

		// XYZ
		batch3D.Cube(Vec3.Zero, xyzCubeColor, Transform, cubeSize);
	}
}

