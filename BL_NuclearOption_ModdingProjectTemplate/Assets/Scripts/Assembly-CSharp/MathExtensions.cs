using UnityEngine;

public static class MathExtensions
{
	public static void ClampPos(this Rect rect, ref Vector2 pos, float factor = 1f)
	{
		if (pos.x > factor * rect.xMax)
		{
			pos.x = factor * rect.xMax;
		}
		if (pos.x < factor * rect.xMin)
		{
			pos.x = factor * rect.xMin;
		}
		if (pos.y > factor * rect.yMax)
		{
			pos.y = factor * rect.yMax;
		}
		if (pos.y < factor * rect.yMin)
		{
			pos.y = factor * rect.yMin;
		}
	}
}
