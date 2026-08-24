using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class FastMath
{
	public static double Lerp(double a, double b, float t)
	{
		t = Mathf.Clamp01(t);
		return a + (b - a) * (double)t;
	}

	public static double LerpUnclamped(double a, double b, float t)
	{
		return a + (b - a) * (double)t;
	}

	public static float InverseLerp(double a, double b, double v)
	{
		return Mathf.Clamp01((float)((v - a) / (b - a)));
	}

	public static float InverseLerpUnclamped(double a, double b, double v)
	{
		return (float)((v - a) / (b - a));
	}

	public static float Map(float value, float aIn, float bIn, float aOut, float bOut)
	{
		if (bIn == aIn)
		{
			return bOut;
		}
		float value2 = (value - aIn) / (bIn - aIn);
		value2 = Mathf.Clamp01(value2);
		return aOut + (bOut - aOut) * value2;
	}

	public static float MapUnclamped(float value, float aIn, float bIn, float aOut, float bOut)
	{
		if (bIn == aIn)
		{
			return bOut;
		}
		float num = (value - aIn) / (bIn - aIn);
		return aOut + (bOut - aOut) * num;
	}

	public static GlobalPosition Lerp(GlobalPosition a, GlobalPosition b, float t)
	{
		t = Mathf.Clamp01(t);
		return new GlobalPosition(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
	}

	public static GlobalPosition LerpUnclamped(GlobalPosition a, GlobalPosition b, float t)
	{
		return new GlobalPosition(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 LerpXZ(Vector3 a, Vector3 b, float t)
	{
		t = Mathf.Clamp01(t);
		return new Vector3(a.x + (b.x - a.x) * t, 0f, a.z + (b.z - a.z) * t);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 NormalizedDirection(GlobalPosition from, GlobalPosition to)
	{
		return Direction(from, to).normalized;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Direction(GlobalPosition from, GlobalPosition to)
	{
		return new Vector3(to.x - from.x, to.y - from.y, to.z - from.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Distance(GlobalPosition a, GlobalPosition b)
	{
		return (float)Math.Sqrt(SquareDistance(a, b));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SquareDistance(GlobalPosition a, GlobalPosition b)
	{
		float num = a.x - b.x;
		float num2 = a.y - b.y;
		float num3 = a.z - b.z;
		return num * num + num2 * num2 + num3 * num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool OutOfRange(GlobalPosition a, GlobalPosition b, float range)
	{
		return !InRange(a, b, range);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InRange(GlobalPosition a, GlobalPosition b, float range)
	{
		float num = a.x - b.x;
		float num2 = a.z - b.z;
		float num3 = a.y - b.y;
		return num * num + num3 * num3 + num2 * num2 < range * range;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 NormalizedDirection(Vector3 from, Vector3 to)
	{
		return Direction(from, to).normalized;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Direction(Vector3 from, Vector3 to)
	{
		return new Vector3(to.x - from.x, to.y - from.y, to.z - from.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Distance(Vector3 a, Vector3 b)
	{
		return (float)Math.Sqrt(SquareDistance(a, b));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SquareDistance(Vector3 a, Vector3 b)
	{
		float num = a.x - b.x;
		float num2 = a.y - b.y;
		float num3 = a.z - b.z;
		return num * num + num2 * num2 + num3 * num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool OutOfRange(Vector3 a, Vector3 b, float range)
	{
		return !InRange(a, b, range);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InRange(Vector3 a, Vector3 b, float range)
	{
		float num = a.x - b.x;
		float num2 = a.z - b.z;
		float num3 = a.y - b.y;
		return num * num + num3 * num3 + num2 * num2 < range * range;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, bool useUnscaledTime = false)
	{
		float num = (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
		if (num == 0f)
		{
			return current;
		}
		return Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, float.PositiveInfinity, num);
	}

	public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime == 0f)
		{
			return current;
		}
		Vector3 eulerAngles = current.eulerAngles;
		Vector3 eulerAngles2 = target.eulerAngles;
		return Quaternion.Euler(Mathf.SmoothDampAngle(eulerAngles.x, eulerAngles2.x, ref currentVelocity.x, smoothTime, float.PositiveInfinity, deltaTime), Mathf.SmoothDampAngle(eulerAngles.y, eulerAngles2.y, ref currentVelocity.y, smoothTime, float.PositiveInfinity, deltaTime), Mathf.SmoothDampAngle(eulerAngles.z, eulerAngles2.z, ref currentVelocity.z, smoothTime, float.PositiveInfinity, deltaTime));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion LookRotation(Vector3 directionOrZero)
	{
		if (directionOrZero.x == 0f && directionOrZero.y == 0f && directionOrZero.z == 0f)
		{
			return Quaternion.identity;
		}
		return Quaternion.LookRotation(directionOrZero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion LookRotation(Vector3 directionOrZero, Vector3 upDirection)
	{
		if (directionOrZero.x == 0f && directionOrZero.y == 0f && directionOrZero.z == 0f)
		{
			return Quaternion.identity;
		}
		return Quaternion.LookRotation(directionOrZero, upDirection);
	}
}
