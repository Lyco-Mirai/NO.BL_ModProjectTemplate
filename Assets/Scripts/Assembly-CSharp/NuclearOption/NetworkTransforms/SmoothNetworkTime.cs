using System;
using Mirage;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class SmoothNetworkTime
	{
		private class Timer
		{
			public readonly ExponentialMovingAverage AvgDiff = new ExponentialMovingAverage(20);

			public double Time;

			public float Timescale = 1f;
		}

		private readonly Timer extrapolation = new Timer();

		private readonly Timer interpolation = new Timer();

		public double ExtrapolationOffsetUnclamped => extrapolation.Time - interpolation.Time;

		public double InterpolationTime => interpolation.Time;

		public double GetExtrapolationOffset(float max = float.MaxValue)
		{
			return Math.Clamp(extrapolation.Time - interpolation.Time, 0.0, max);
		}

		public SmoothNetworkTime()
		{
			extrapolation.Time = 0.0;
			interpolation.Time = 0.0;
		}

		public void Update(float deltaTime)
		{
			interpolation.Time += deltaTime * interpolation.Timescale;
			extrapolation.Time += deltaTime * extrapolation.Timescale;
		}

		public void ResetValues(double targetTime, double extrapolationOffset)
		{
			interpolation.Time = targetTime;
			interpolation.Timescale = 1f;
			interpolation.AvgDiff.Reset();
			extrapolation.Time = targetTime + extrapolationOffset;
			extrapolation.Timescale = 1f;
			extrapolation.AvgDiff.Reset();
		}

		public void OnMessage(double targetTime, double extrapolationOffset, float snapThreshold, out bool snap)
		{
			AdjustTimeScale(targetTime, ref interpolation.Time, interpolation.AvgDiff, snapThreshold, out interpolation.Timescale, out snap);
			double num = interpolation.Time + extrapolationOffset;
			if (snap)
			{
				extrapolation.Timescale = 1f;
				extrapolation.Time = num;
				extrapolation.AvgDiff.Reset();
			}
			else
			{
				AdjustTimeScale(num, ref extrapolation.Time, extrapolation.AvgDiff, snapThreshold, out extrapolation.Timescale, out var _);
			}
		}

		public static void AdjustTimeScale(double target, ref double current, ExponentialMovingAverage diffAvg, float snapThreshold, out float timescale, out bool snap)
		{
			if (current == 0.0)
			{
				snap = true;
				timescale = 1f;
				current = target;
				return;
			}
			float num = (float)(target - current);
			diffAvg.Add(num);
			float num2 = (float)diffAvg.Value;
			if (Mathf.Abs(num2) > snapThreshold)
			{
				Debug.LogWarning($"{Time.unscaledTime:0.000}: Smooth Time Snap: diff={num2}");
				snap = true;
				timescale = 1f;
				current = target;
				diffAvg.Reset();
			}
			else
			{
				timescale = CalculateTimeScale(num2);
				snap = false;
			}
		}

		private static float CalculateTimeScale(double diff)
		{
			if (diff > 0.25)
			{
				return 1.08f;
			}
			if (diff > 0.02500000037252903)
			{
				return 1.01f;
			}
			if (diff < -0.02499999850988388)
			{
				return 0.8f;
			}
			if (diff < -0.0024999999441206455)
			{
				return 0.96f;
			}
			if (diff > 0.0024999999441206455)
			{
				return 1.0025f;
			}
			if (diff < -0.00024999998277053237)
			{
				return 0.9975f;
			}
			return 1f;
		}
	}
}
