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
		var tSlabExit = Vec3.Max(t0, t1);

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

	// "Box Intersection Generic" from https://iquilezles.org/articles/boxfunctions
	public static bool RayIntersectOBB(Vec3 origin, Vec3 direction, BoundingBox box, Matrix transform, out float t)
	{
		t = 0.0f;
		if (!Matrix.Invert(transform, out var inverse))
			return false;
		
		// The center of the bounding box needs to be at <0,0,0>
		inverse *= Matrix.CreateTranslation(-box.Center);
		
		// convert from world to box space
		var ro = Vec3.Transform(origin, inverse);
		var rd = Vec3.TransformNormal(direction, inverse);

		var rad = box.Size / 2.0f;
			
		// ray-box intersection in box space
		var m = Vec3.One / rd;
		var s = new Vec3(
			(rd.X<0.0f)?1.0f:-1.0f,
			(rd.Y<0.0f)?1.0f:-1.0f,
			(rd.Z<0.0f)?1.0f:-1.0f);
		var t1 = m*(-ro + s*rad);
		var t2 = m*(-ro - s*rad);

		float tN = Math.Max( Math.Max( t1.X, t1.Y ), t1.Z );
		float tF = Math.Min( Math.Min( t2.X, t2.Y ), t2.Z );
	
		if( tN>tF || tF<0.0) 
			return false;

		// compute normal (in world space), face and UV
		// currently not required
		// if( t1.x>t1.y && t1.x>t1.z ) { oN=txi[0].xyz*s.x; oU=ro.yz+rd.yz*t1.x; oF=([1+int(s.x))/[2];
		// else if( t1.y>t1.z   )       { oN=txi[1].xyz*s.y; oU=ro.zx+rd.zx*t1.y; oF=([5+int(s.y))/[2];
		// else                         { oN=txi[2].xyz*s.z; oU=ro.xy+rd.xy*t1.z; oF=([9+int(s.z))/[2];

		// exit point currently not required
		// oT = vec2(tN,tF);
		t = tN;

		return true;
	}
}
