using System;
using Unity.Burst;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public static class ChartHelper
	{
		[BurstDiscard]
		public static void LogErrorDiscard(string msg)
		{
			Debug.LogError(msg);
		}

		public static float SafeRead(float index, ReadOnlySpan<float> chart)
		{
			if (float.IsNaN(index))
			{
				LogErrorDiscard("Index was IsNaN");
				return chart[chart.Length - 1];
			}
			if (float.IsInfinity(index))
			{
				LogErrorDiscard("Index was Infinity");
				return chart[chart.Length - 1];
			}
			if (index <= 0f)
			{
				return chart[0];
			}
			if (index >= (float)(chart.Length - 1))
			{
				return chart[chart.Length - 1];
			}
			int index2 = Mathf.FloorToInt(index);
			int index3 = Mathf.CeilToInt(index);
			float a = chart[index2];
			float b = chart[index3];
			float t = index % 1f;
			return Mathf.Lerp(a, b, t);
		}
	}
}
