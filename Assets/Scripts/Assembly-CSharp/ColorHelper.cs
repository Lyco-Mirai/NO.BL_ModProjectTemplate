using System.Runtime.CompilerServices;
using UnityEngine;

public static class ColorHelper
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color WithAlpha(this Color color, float alpha)
	{
		return new Color(color.r, color.g, color.b, alpha);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color MultiplyRGB(this Color color, float factor)
	{
		return color.MultiplyRGB(factor, color.a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color MultiplyRGB(this Color color, float factor, float alpha)
	{
		return new Color(color.r * factor, color.g * factor, color.b * factor, alpha);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetSat(this Color color)
	{
		Color.RGBToHSV(color, out var _, out var S, out var _);
		return S;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color WithSat(this Color color, float sat)
	{
		Color.RGBToHSV(color, out var H, out var _, out var V);
		Color color2 = Color.HSVToRGB(H, sat, V);
		return new Color(color2.r, color2.g, color2.b, color.a);
	}
}
