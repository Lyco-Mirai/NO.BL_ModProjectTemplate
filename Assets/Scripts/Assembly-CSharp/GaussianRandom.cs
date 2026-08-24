using System;
using UnityEngine;

public static class GaussianRandom
{
	public static float NormalRandom(float sigma)
	{
		float f = UnityEngine.Random.Range(0f, 1f);
		float num = UnityEngine.Random.Range(0f, 1f);
		float num2 = Mathf.Sqrt(-2f * Mathf.Log(f)) * Mathf.Cos(MathF.PI * 2f * num);
		return sigma * num2;
	}

	public static Vector3 NormalRandomSpherized(float sigma)
	{
		Vector3 normalized = UnityEngine.Random.insideUnitSphere.normalized;
		return NormalRandom(sigma) * normalized;
	}
}
