namespace Celeste64.Mod;

public static class ModUtils
{
	// Based on Unity's implementation
	// See: https://github.com/Unity-Technologies/Graphics/blob/17c1d4655ea4685128801d617bdc8d624e586bc6/Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl#L48-L75
	public static bool RayIntersectsBox(Vec3 origin, Vec3 direction, BoundingBox box, out float tEnter, out float tExit)
	{
		// Could be precomputed. Clamp to avoid INF. clamp() is a single ALU on GCN.
		// rcp(FLT_EPS) = 16,777,216, which is large enough for our purposes,
		// yet doesn't cause a lot of numerical issues associated with FLT_MAX.
		// float3 rayDirInv = clamp(rcp(rayDirection), -rcp(FLT_EPS), rcp(FLT_EPS));
		var dirInv = Vec3.Clamp(Vec3.One / direction, new Vec3(-1.0f / float.Epsilon), new Vec3(1.0f / float.Epsilon));

		// Perform ray-slab intersection (component-wise).
		// float3 t0 = boxMin * rayDirInv - (rayOrigin * rayDirInv);
		// float3 t1 = boxMax * rayDirInv - (rayOrigin * rayDirInv);
		var t0 = box.Min * dirInv - (origin * dirInv);
		var t1 = box.Max * dirInv - (origin * dirInv);

		// Find the closest/farthest distance (component-wise).
		// float3 tSlabEntr = min(t0, t1);
		// float3 tSlabExit = max(t0, t1);
		var tSlabEnter = Vec3.Min(t0, t1);
		var tSlabExit  = Vec3.Max(t0, t1);

		// Find the farthest entry and the nearest exit.
		// tEntr = Max3(tSlabEntr.x, tSlabEntr.y, tSlabEntr.z);
		// tExit = Min3(tSlabExit.x, tSlabExit.y, tSlabExit.z);
		tEnter = Math.Max(tSlabEnter.X, Math.Max(tSlabEnter.Y, tSlabEnter.Z));
		tExit = Math.Min(tSlabExit.X, Math.Min(tSlabExit.Y, tSlabExit.Z));

		// Clamp to the range.
		// tEntr = max(tEntr, tMin);
		// tExit = min(tExit, tMax);
		
		return tEnter < tExit;
	}
}