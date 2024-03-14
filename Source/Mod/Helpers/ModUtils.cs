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

	// Intersection method from "Real-Time Rendering and Essential Mathematics for Games"
	// Reference Implementation: https://github.com/opengl-tutorials/ogl/blob/15e57f6cccef388915e565d8322b8442049e1bd8/misc05_picking/misc05_picking_custom.cpp#L83-L197 
	public static bool RayIntersectOBB(Vec3 origin, Vec3 direction, BoundingBox box, Matrix transform, out float t)
	{
		t = 0.0f;
		float tMin = 0.0f, tMax = 100000.0f;

		var oobWorldPos = new Vec3(transform.M41, transform.M42, transform.M43);
		var delta = oobWorldPos - origin;

		// Test intersection with the 2 planes perpendicular to the OBB's X axis
		{
			var xAxis = new Vec3(transform.M11, transform.M12, transform.M13);
			float e = Vec3.Dot(xAxis, delta);
			float f = Vec3.Dot(direction, xAxis);

			if (Math.Abs(f) > 0.001f) // Standard case
			{
				float t1 = (e + box.Min.X) / f; // Intersection with the "left" plane
				float t2 = (e + box.Max.X) / f; // Intersection with the "right" plane
												// t1 and t2 now contain distances between ray origin and ray-plane intersections

				// We want t1 to represent the nearest intersection, 
				// so if it's not the case, invert t1 and t2
				if (t1 > t2)
					(t1, t2) = (t2, t1);

				// tMax is the nearest "far" intersection (amongst the X,Y and Z planes pairs)
				if (t2 < tMax)
					tMax = t2;
				// tMin is the farthest "near" intersection (amongst the X,Y and Z planes pairs)
				if (t1 > tMin)
					tMin = t1;

				// And here's the trick :
				// If "far" is closer than "near", then there is NO intersection.
				// See the images in the tutorials for the visual explanation.
				if (tMax < tMin)
					return false;
			}
			else // Rare case: The ray is almost parallel to the planes, so they don't have any "intersection"
			{
				if (-e + box.Min.X > 0.0f || -e + box.Max.X < 0.0f)
					return false;
			}
		}

		// Test intersection with the 2 planes perpendicular to the OBB's Y axis
		// Exactly the same thing than above.
		{
			var yAxis = new Vec3(transform.M21, transform.M22, transform.M23);
			float e = Vec3.Dot(yAxis, delta);
			float f = Vec3.Dot(direction, yAxis);

			if (Math.Abs(f) > 0.001f)
			{
				float t1 = (e + box.Min.Y) / f;
				float t2 = (e + box.Max.Y) / f;

				if (t1 > t2)
					(t1, t2) = (t2, t1);

				if (t2 < tMax)
					tMax = t2;
				if (t1 > tMin)
					tMin = t1;

				if (tMax < tMin)
					return false;
			}
			else
			{
				if (-e + box.Min.Y > 0.0f || -e + box.Max.Y < 0.0f)
					return false;
			}
		}


		// Test intersection with the 2 planes perpendicular to the OBB's Z axis
		// Exactly the same thing than above.
		{
			var zAxis = new Vec3(transform.M31, transform.M32, transform.M33);
			float e = Vec3.Dot(zAxis, delta);
			float f = Vec3.Dot(direction, zAxis);

			if (Math.Abs(f) > 0.001f)
			{
				float t1 = (e + box.Min.Z) / f;
				float t2 = (e + box.Max.Z) / f;

				if (t1 > t2)
					(t1, t2) = (t2, t1);

				if (t2 < tMax)
					tMax = t2;
				if (t1 > tMin)
					tMin = t1;

				if (tMax < tMin)
					return false;
			}
			else
			{
				if (-e + box.Min.Z > 0.0f || -e + box.Max.Z < 0.0f)
					return false;
			}
		}

		t = tMin;
		return true;
	}
}
