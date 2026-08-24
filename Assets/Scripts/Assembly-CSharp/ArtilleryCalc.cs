using System;
using UnityEngine;

public static class ArtilleryCalc
{
	public static float GetElevation(float displacementH, float displacementV, float muzzleVelocity, int maxIterations)
	{
		float num = MathF.PI / 8f;
		Vector2 zero = Vector2.zero;
		float num2 = 0f;
		float num3 = 0.0001f;
		float num4 = 0f;
		int num5 = 0;
		while (Mathf.Abs(num2) > 10f && num5 <= maxIterations)
		{
			zero.x = muzzleVelocity * Mathf.Cos(num);
			zero.y = muzzleVelocity * Mathf.Sin(num);
			float num6 = zero.y / 9.81f;
			float num7 = zero.y / 19.62f - displacementV;
			float num8 = Mathf.Sqrt(2f * num7 / 9.81f);
			num4 = num6 + num8;
			num2 = zero.x * num4 - displacementH;
			num -= num3 * num2;
		}
		Debug.Log($"[TargetCacl] Calculated artillery firing solution in {num5} iterations");
		return num * (MathF.PI / 180f);
	}
}
