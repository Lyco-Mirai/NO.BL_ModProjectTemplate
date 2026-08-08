using System.Runtime.CompilerServices;
using UnityEngine;

public static class NetworkFloatHelper
{
	public static bool Validate(float value, bool logErrors, string variableName)
	{
		if (!float.IsFinite(value))
		{
			if (logErrors)
			{
				LogNaN(value, variableName);
			}
			return false;
		}
		return true;
	}

	public static bool Validate(double value, bool logErrors, string variableName)
	{
		if (!double.IsFinite(value))
		{
			if (logErrors)
			{
				LogNaN(value, variableName);
			}
			return false;
		}
		return true;
	}

	public static bool Validate(Vector3 value, bool logErrors, string variableName)
	{
		bool result = true;
		if (!float.IsFinite(value.x))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.x, variableName + ".x");
			}
		}
		if (!float.IsFinite(value.y))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.y, variableName + ".y");
			}
		}
		if (!float.IsFinite(value.z))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.z, variableName + ".z");
			}
		}
		return result;
	}

	public static bool Validate(Quaternion value, bool logErrors, string variableName)
	{
		bool result = true;
		if (!float.IsFinite(value.x))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.x, variableName + ".x");
			}
		}
		if (!float.IsFinite(value.y))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.y, variableName + ".y");
			}
		}
		if (!float.IsFinite(value.z))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.z, variableName + ".z");
			}
		}
		if (!float.IsFinite(value.w))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(value.w, variableName + ".w");
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Validate(GlobalPosition value, bool logErrors, string variableName)
	{
		return Validate(value.AsVector3(), logErrors, variableName);
	}

	public static bool Validate(Vector3Compressed value, bool logErrors, string variableName)
	{
		float num = Mathf.HalfToFloat(value.x.Value);
		float num2 = Mathf.HalfToFloat(value.y.Value);
		float num3 = Mathf.HalfToFloat(value.z.Value);
		bool result = true;
		if (!float.IsFinite(num))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(num, variableName + ".x");
			}
		}
		if (!float.IsFinite(num2))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(num2, variableName + ".y");
			}
		}
		if (!float.IsFinite(num3))
		{
			result = false;
			if (logErrors)
			{
				LogNaN(num3, variableName + ".z");
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Validate(CompressedFloat compressed, bool logErrors, string variableName)
	{
		return Validate(Mathf.HalfToFloat(compressed.Value), logErrors, variableName);
	}

	public static void LogNaN(double value, string variableName)
	{
		if (double.IsNaN(value))
		{
			Debug.LogError(variableName + " is NaN");
		}
		if (double.IsInfinity(value))
		{
			Debug.LogError(variableName + " is infinity");
		}
	}

	public static void LogNaN(float value, string variableName)
	{
		if (float.IsNaN(value))
		{
			Debug.LogError(variableName + " is NaN");
		}
		if (float.IsInfinity(value))
		{
			Debug.LogError(variableName + " is infinity");
		}
	}

	public static void LogNaN(Vector3 value, string variableName)
	{
		if (float.IsNaN(value.x))
		{
			Debug.LogError(variableName + ".x is NaN");
		}
		if (float.IsInfinity(value.x))
		{
			Debug.LogError(variableName + ".x is infinity");
		}
		if (float.IsNaN(value.y))
		{
			Debug.LogError(variableName + ".y is NaN");
		}
		if (float.IsInfinity(value.y))
		{
			Debug.LogError(variableName + ".y is infinity");
		}
		if (float.IsNaN(value.z))
		{
			Debug.LogError(variableName + ".z is NaN");
		}
		if (float.IsInfinity(value.z))
		{
			Debug.LogError(variableName + ".z is infinity");
		}
	}

	public static Vector3Compressed CompressIfValid(Vector3 value, bool logErrors, string variableName, Vector3 defaultValue = default(Vector3))
	{
		if (!Validate(value, logErrors, variableName))
		{
			value = defaultValue;
		}
		return value.Compress();
	}

	public static Vector3 DecompressIfValid(Vector3Compressed value, bool logErrors, string variableName, Vector3 defaultValue = default(Vector3))
	{
		if (!Validate(value, logErrors, variableName))
		{
			return defaultValue;
		}
		return value.Decompress();
	}

	public static bool TryDecompress(Vector3Compressed value, out Vector3 outValue, bool logErrors, string variableName)
	{
		outValue = value.Decompress();
		return Validate(outValue, logErrors, variableName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Compressed Compress(this Vector3 value)
	{
		Vector3Compressed result = default(Vector3Compressed);
		result.x = value.x.Compress();
		result.y = value.y.Compress();
		result.z = value.z.Compress();
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Decompress(this Vector3Compressed value)
	{
		Vector3 result = default(Vector3);
		result.x = value.x.Decompress();
		result.y = value.y.Decompress();
		result.z = value.z.Decompress();
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CompressedFloat Compress(this float value)
	{
		ushort value2 = Mathf.FloatToHalf(Mathf.Clamp(value, -65504f, 65504f));
		return new CompressedFloat
		{
			Value = value2
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Decompress(this CompressedFloat value)
	{
		float num = Mathf.HalfToFloat(value.Value);
		if (!float.IsFinite(num))
		{
			if (float.IsPositiveInfinity(num))
			{
				return 65504f;
			}
			if (float.IsNegativeInfinity(num))
			{
				return -65504f;
			}
		}
		return num;
	}
}
