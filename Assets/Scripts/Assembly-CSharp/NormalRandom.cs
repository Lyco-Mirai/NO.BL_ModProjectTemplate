using UnityEngine;

public static class NormalRandom
{
	public static float GetNormalizedRandom(float minValue = 0.01f, float maxValue = 1f)
	{
		float num;
		float num3;
		do
		{
			num = 2f * Random.value - 1f;
			float num2 = 2f * Random.value - 1f;
			num3 = num * num + num2 * num2;
		}
		while (num3 >= 1f);
		float num4 = num * Mathf.Sqrt(-2f * Mathf.Log(num3) / num3);
		float num5 = (minValue + maxValue) / 2f;
		float num6 = (maxValue - num5) / 3f;
		return Mathf.Clamp(num4 * num6 + num5, minValue, maxValue);
	}
}
