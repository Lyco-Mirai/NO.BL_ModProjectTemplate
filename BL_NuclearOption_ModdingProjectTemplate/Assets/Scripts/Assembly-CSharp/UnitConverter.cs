using UnityEngine;

public static class UnitConverter
{
	public static string AltitudeReading(float altitude)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			if (!(Mathf.Abs(altitude) < 10f))
			{
				return $"{altitude:F0}m";
			}
			return $"{altitude:F1}m";
		}
		return $"{altitude * 3.28084f:F0}ft";
	}

	public static string DistanceReading(float distance)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			if (distance > 10000f)
			{
				return $"{distance * 0.001f:F0}km";
			}
			if (distance > 1000f)
			{
				return $"{distance * 0.001f:F1}km";
			}
			return $"{distance:F0}m";
		}
		float num = distance * 1.09361f;
		if (num < 1000f)
		{
			return $"{num:F0}yd";
		}
		return $"{distance * 0.000539957f:F1}nm";
	}

	public static string SpeedReading(float speed)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			return $"{(double)speed * 3.6:F0}km/h";
		}
		return $"{speed * 1.94384f:F0}kt";
	}

	public static string SpeedReadingGround(float speed)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			return $"{(double)speed * 3.6:F0}km/h";
		}
		return $"{(double)speed * 2.23694:F0}mph";
	}

	public static string ClimbRateReading(float speed)
	{
		string arg = "";
		if (speed > 0.5f)
		{
			arg = "+";
		}
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			if (!(Mathf.Abs(speed) < 10f))
			{
				return $"{arg}{speed:F0}m/s";
			}
			return $"{arg}{speed:F1}m/s";
		}
		return $"{arg}{speed * 60f * 3.28084f:F0}fpm";
	}

	public static string DimensionReading(float length)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			return $"{length:F1}m";
		}
		return $"{(double)length * 3.28084:F1}ft";
	}

	public static string WeightReading(float weight)
	{
		if (weight >= 899999f)
		{
			return $"{weight * 1E-06f:F2}kt";
		}
		if (weight >= 2999f)
		{
			return $"{weight * 0.001f:F1}t";
		}
		if (weight > 9999f)
		{
			return $"{weight * 0.001f:F2}t";
		}
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			return $"{weight:F0}kg";
		}
		return $"{(double)weight * 2.20462:F0}lb";
	}

	public static string YieldReading(float yield)
	{
		if ((double)yield > 10000000000.0)
		{
			return $"{yield * 1E-09f:F0}Mt";
		}
		if ((double)yield > 100000000.0)
		{
			return $"{yield * 1E-06f:F0}kt";
		}
		if ((double)yield > 10000000.0)
		{
			return $"{yield * 1E-06f:F1}kt";
		}
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			return $"{yield:F0}kg";
		}
		return $"{(double)yield * 2.20462:F0}lb";
	}

	public static string PowerReading(float kW)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			if (kW < 1f)
			{
				return $"{kW * 1000f:F0}W";
			}
			if (kW < 1000f)
			{
				return $"{kW:F1}kW";
			}
			return $"{kW * 0.001f:F2}MW";
		}
		float num = kW * 1.34102f;
		return $"{num:F1}hp";
	}

	public static string PowerToWeightReading(float kWPerKg)
	{
		if (PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Metric)
		{
			return $"{kWPerKg:F2}kW/kg";
		}
		kWPerKg *= 1.341f;
		kWPerKg *= 0.4536f;
		return $"{kWPerKg:F2}hp/lb";
	}

	public static string ValueReading(float valueInMillions)
	{
		float num = valueInMillions * 1000000f;
		if (num * num < 100000000f)
		{
			return $"${num:F0}";
		}
		if (valueInMillions * valueInMillions < 1f)
		{
			return $"${valueInMillions * 1000f:F1}k";
		}
		if (valueInMillions * valueInMillions < 100f)
		{
			return $"${valueInMillions * 1f:F2}m";
		}
		if (valueInMillions * valueInMillions < 1000000f)
		{
			return $"${valueInMillions:F1}m";
		}
		if (valueInMillions * valueInMillions < 1E+12f)
		{
			return $"${valueInMillions * 0.001f:F2}b";
		}
		return $"${valueInMillions * 1E-06f:F3}t";
	}

	public static string TimeOfDay(float totalHours, bool includeSeconds)
	{
		int num = (int)totalHours;
		float num2 = totalHours % 1f * 3600f;
		int num3 = (int)(num2 / 60f);
		if (includeSeconds)
		{
			int num4 = (int)(num2 % 60f);
			return $"{num:D2}:{num3:D2}:{num4:D2}";
		}
		return $"{num:D2}:{num3:D2}";
	}

	public static bool TimeOfDay(float totalHours, bool includeSeconds, ref float lastTimeHours, out string timeString)
	{
		if (lastTimeHours > 0f)
		{
			int num = (int)(totalHours * (float)(includeSeconds ? 3600 : 60));
			int num2 = (int)(lastTimeHours * (float)(includeSeconds ? 3600 : 60));
			if (num == num2)
			{
				timeString = null;
				return false;
			}
		}
		lastTimeHours = totalHours;
		timeString = TimeOfDay(totalHours, includeSeconds);
		return true;
	}
}
