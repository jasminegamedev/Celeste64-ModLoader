namespace Celeste64.Mod.Editor;

public class PositionGizmo : Gizmo
{
	private GizmoTarget target;
	
	private const float CubeSize = 0.15f;
	private const float PlaneSize = 0.6f;
	private const float Padding = 0.15f;
	private const float AxisLen = 1.5f;
	private const float AxisRadius = AxisLen / 35.0f;
	private const float ConeLen = AxisLen / 2.5f;
	private const float ConeRadius = ConeLen / 3.0f;
	
	private const float BoundsPadding = 0.1f;

	// Axis
	private const float AxisBoundsLengthMin = CubeSize + Padding;
	private const float AxisBoundsLengthMax = AxisLen + ConeLen * 0.9f;
	private const float AxisBoundsRadiusMin = -AxisRadius - BoundsPadding;
	private const float AxisBoundsRadiusMax = AxisRadius + BoundsPadding;
	
	private static readonly BoundingBox XAxisBounds = new(
		new Vec3(AxisBoundsLengthMin, AxisBoundsRadiusMin, AxisBoundsRadiusMin),
		new Vec3(AxisBoundsLengthMax, AxisBoundsRadiusMax, AxisBoundsRadiusMax));
	
	private static readonly BoundingBox YAxisBounds = new(
		new Vec3(AxisBoundsRadiusMin, AxisBoundsLengthMin, AxisBoundsRadiusMin),
		new Vec3(AxisBoundsRadiusMax, AxisBoundsLengthMax, AxisBoundsRadiusMax));
	
	private static readonly BoundingBox ZAxisBounds = new(
		new Vec3(AxisBoundsRadiusMin, AxisBoundsRadiusMin, AxisBoundsLengthMin),
		new Vec3(AxisBoundsRadiusMax, AxisBoundsRadiusMax, AxisBoundsLengthMax));
	
	// Planes
	private const float PlaneBoundsMin = CubeSize + AxisLen / 2.0f - PlaneSize / 2.0f - BoundsPadding;
	private const float PlaneBoundsMax = CubeSize + AxisLen / 2.0f + PlaneSize / 2.0f + BoundsPadding;
	
	private static readonly BoundingBox XZPlaneBounds = new(
		new Vec3(PlaneBoundsMin, 0.0f, PlaneBoundsMin),
		new Vec3(PlaneBoundsMax, 0.0f, PlaneBoundsMax));
	
	private static readonly BoundingBox YZPlaneBounds = new(
		new Vec3(0.0f, PlaneBoundsMin, PlaneBoundsMin),
		new Vec3(0.0f, PlaneBoundsMax, PlaneBoundsMax));
	
	private static readonly BoundingBox XYPlaneBounds = new(
		new Vec3(PlaneBoundsMin, PlaneBoundsMin, 0.0f),
		new Vec3(PlaneBoundsMax, PlaneBoundsMax, 0.0f));
	
	// Cube
	private static readonly BoundingBox XYZCubeBounds = new(
		-new Vec3(CubeSize + BoundsPadding),
		 new Vec3(CubeSize + BoundsPadding));
	
	public static Matrix Transform
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
	
	public override void Render(Batcher3D batch3D)
	{
		if (EditorWorld.Current.Selected is not SpikeBlock.Definition selected)
			return;
		
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
		
		var xAxisColor = target == GizmoTarget.AxisX ? xColorSelected : xColorDeselected;
		var yAxisColor = target == GizmoTarget.AxisY ? yColorSelected : yColorDeselected;
		var zAxisColor = target == GizmoTarget.AxisZ ? zColorSelected : zColorDeselected;
		
		var xzPlaneColor = target == GizmoTarget.PlaneXZ ? yColorSelected : yColorDeselected;
		var yzPlaneColor = target == GizmoTarget.PlaneYZ ? xColorSelected : xColorDeselected;
		var xyPlaneColor = target == GizmoTarget.PlaneXY ? zColorSelected : zColorDeselected;
		
		var xyzCubeColor = target == GizmoTarget.CubeXYZ ? cubeColorSelected : cubeColorDeselected; 
		
		// X
		batch3D.Line(Vec3.UnitX * (CubeSize + Padding), Vec3.UnitX * AxisLen, xAxisColor, Transform, AxisRadius);
		batch3D.Cone(Vec3.UnitX * AxisLen, Batcher3D.Direction.X, ConeLen, ConeRadius, 12, xAxisColor, Transform);
		// Y
		batch3D.Line(Vec3.UnitY * (CubeSize + Padding), Vec3.UnitY * AxisLen, yAxisColor, Transform, AxisRadius);
		batch3D.Cone(Vec3.UnitY * AxisLen, Batcher3D.Direction.Y, ConeLen, ConeRadius, 12, yAxisColor, Transform);
		// Z
		batch3D.Line(Vec3.UnitZ * (CubeSize + Padding), Vec3.UnitZ * AxisLen, zAxisColor, Transform, AxisRadius);
		batch3D.Cone(Vec3.UnitZ * AxisLen, Batcher3D.Direction.Z, ConeLen, ConeRadius, 12, zAxisColor, Transform);
		
		// XZ
		batch3D.Square(Vec3.UnitX * (CubeSize + AxisLen / 2.0f) + Vec3.UnitZ * (CubeSize + AxisLen / 2.0f), 
					   Vec3.UnitY, xzPlaneColor, Transform, PlaneSize / 2.0f);
		// YZ
		batch3D.Square(Vec3.UnitY * (CubeSize + AxisLen / 2.0f) + Vec3.UnitZ * (CubeSize + AxisLen / 2.0f), 
			           Vec3.UnitX, yzPlaneColor, Transform, PlaneSize / 2.0f);
		// XY
		batch3D.Square(Vec3.UnitX * (CubeSize + AxisLen / 2.0f) + Vec3.UnitY * (CubeSize + AxisLen / 2.0f), 
			           Vec3.UnitZ, xyPlaneColor, Transform, PlaneSize / 2.0f);

		// XYZ
		batch3D.Cube(Vec3.Zero, xyzCubeColor, Transform, CubeSize);
	}
	
	private static readonly (BoundingBox Bounds, GizmoTarget Target)[] GizmoTargets = [
		(XAxisBounds, GizmoTarget.AxisX),
		(YAxisBounds, GizmoTarget.AxisY),
		(ZAxisBounds, GizmoTarget.AxisZ),
					
		(XZPlaneBounds, GizmoTarget.PlaneXZ),
		(YZPlaneBounds, GizmoTarget.PlaneYZ),
		(XYPlaneBounds, GizmoTarget.PlaneXY),
					
		(XYZCubeBounds, GizmoTarget.CubeXYZ),
	];
	public bool RaycastCheck(Vec3 origin, Vec3 direction)
	{
		float closestGizmo = float.PositiveInfinity;
		
		target = GizmoTarget.None;
		foreach (var (checkBounds, checkTarget) in GizmoTargets)
		{
			if (!ModUtils.RayIntersectOBB(origin, direction, checkBounds, Transform, out float dist) || dist >= closestGizmo) 
				continue;

			target = checkTarget;
			closestGizmo = dist;
		}
		
		return target != GizmoTarget.None;
	}
	
	public void Drag(EditorWorld editor, Vec2 mouseDelta, Vec3 mouseRay, Vec3 objectStartingPosition)
	{
		var axisMatrix = Transform * editor.Camera.ViewProjection;
		var screenXAxis = Vec3.TransformNormal(Vec3.UnitX, axisMatrix).XY();
		var screenYAxis = Vec3.TransformNormal(Vec3.UnitY, axisMatrix).XY();
		var screenZAxis = Vec3.TransformNormal(Vec3.UnitZ, axisMatrix).XY();
		// Flip Y, since down is positive in screen coords
		screenXAxis.Y *= -1.0f;
		screenYAxis.Y *= -1.0f;
		screenZAxis.Y *= -1.0f;
		
		// Linear scalar for the movement. Chosen on what felt best.
		const float dotScale = 1.0f / 50.0f;
		float dotX = Vec2.Dot(mouseDelta, screenXAxis) * dotScale;
		float dotY = Vec2.Dot(mouseDelta, screenYAxis) * dotScale;
		float dotZ = Vec2.Dot(mouseDelta, screenZAxis) * dotScale;
		
		Vec3 newPosition = Vec3.Zero;
		if (editor.Selected is SpikeBlock.Definition def)
			newPosition = def.Position;
		
		var xzPlaneDelta = Vec3.Transform(XZPlaneBounds.Center, Transform) - newPosition;
		var yzPlaneDelta = Vec3.Transform(YZPlaneBounds.Center, Transform) - newPosition;
		var xyPlaneDelta = Vec3.Transform(XYPlaneBounds.Center, Transform) - newPosition;
		
		var cameraPlaneNormal = (editor.Camera.Position - objectStartingPosition).Normalized();
		var cameraPlane = new Plane(cameraPlaneNormal, Vec3.Dot(cameraPlaneNormal, objectStartingPosition));
		
		switch (target)
		{
			case GizmoTarget.AxisX:
				newPosition = objectStartingPosition + Vec3.UnitX * dotX;
				break;
			case GizmoTarget.AxisY:
				newPosition = objectStartingPosition + Vec3.UnitY * dotY;
				break;
			case GizmoTarget.AxisZ:
				newPosition = objectStartingPosition + Vec3.UnitZ * dotZ;
				break;
			
			case GizmoTarget.PlaneXZ:
				float tY = (objectStartingPosition.Y - editor.Camera.Position.Y) / mouseRay.Y;
				newPosition = editor.Camera.Position + mouseRay * tY - xzPlaneDelta;
				break;
			case GizmoTarget.PlaneYZ:
				float tX = (objectStartingPosition.X - editor.Camera.Position.X) / mouseRay.X;
				newPosition = editor.Camera.Position + mouseRay * tX - yzPlaneDelta;
				break;
			case GizmoTarget.PlaneXY:
				float tZ = (objectStartingPosition.Z - editor.Camera.Position.Z) / mouseRay.Z;
				newPosition = editor.Camera.Position + mouseRay * tZ - xyPlaneDelta;
				break;

			case GizmoTarget.CubeXYZ:
				if (ModUtils.RayIntersectsPlane(editor.Camera.Position, mouseRay, cameraPlane, out var hit))
				{
					newPosition = hit;
				}
				break;

			case GizmoTarget.None:
			default:
				break;
		}
		
		if (editor.Selected is SpikeBlock.Definition def2)
		{
			def2.Position = newPosition;
			def2.Dirty = true;
		}
	}
}

