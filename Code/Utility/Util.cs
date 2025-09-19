namespace ResourceFarmer.Utility;
using System;
using Sandbox;


public static class Util
{
	public static float InverseLerp(float a, float b, float value)
	{
		if (a == b) return 0f;
		return Math.Clamp((value - a) / (b - a), 0f, 1f);
	}

	public static float Lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	public static float Clamp(float value, float min, float max)
	{
		return Math.Clamp(value, min, max);
	}

	public static double GetDistance(GameObject a, GameObject b)
	{
		if (a == null || b == null) return 0.0;
		return Vector3.DistanceBetween(a.Transform.World.Position, b.Transform.World.Position);

	}
}
