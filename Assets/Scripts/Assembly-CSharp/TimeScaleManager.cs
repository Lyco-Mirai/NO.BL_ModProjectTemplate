using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct TimeScaleManager
{
	public static float Scale
	{
		get
		{
			return Time.timeScale;
		}
		set
		{
			ColorLog<TimeScaleManager>.Info($"Setting time scale from {Time.timeScale} to {value}");
			Time.timeScale = value;
		}
	}
}
