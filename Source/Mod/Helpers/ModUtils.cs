namespace Celeste64.Mod;

public static class ModUtils
{
	public static bool RayIntersectsBox(Vec3 origin, Vec3 direction, BoundingBox box, out float t)
	{
		t = 0.0f;
		
		// X
		float tMin = (box.Min.X - origin.X) / direction.X;
		float tMax = (box.Max.X - origin.X) / direction.X;

		if (tMin > tMax)
			(tMin, tMax) = (tMax, tMin);

		// Y
		float tyMin = (box.Min.Y - origin.Y) / direction.Y;
		float tyMax = (box.Max.Y - origin.Y) / direction.Y;

		if (tyMin > tyMax)
			(tyMin, tyMax) = (tyMax, tyMin);

		if (tMin > tyMax || tyMin > tMax)
			return false;

		if (tyMin > tMin)
			tMin = tyMin;

		if (tyMax < tMax)
			tMax = tyMax;

		// Z
		float tzMin = (box.Min.Z - origin.Z) / direction.Z;
		float tzMax = (box.Max.Z - origin.Z) / direction.Z;

		if (tzMin > tzMax)
			(tzMin, tzMax) = (tzMax, tzMin);

		if (tMin > tzMax || tzMin > tMax)
			return false;

		if (tzMin > tMin)
			tMin = tzMin;

		if (tzMax < tMax)
			tMax = tzMax;

		t = tMin;
		return t > 0.0f;
	}
}