using UnityEngine;

namespace NuclearOption
{
	public static class RectTransformExtensions
	{
		public static void SetRectWidth(this Component comp, float width)
		{
			((RectTransform)comp.transform).SetRectWidth(width);
		}

		public static void SetRectWidth(this RectTransform transform, float width)
		{
			Vector2 sizeDelta = transform.sizeDelta;
			sizeDelta.x = width;
			transform.sizeDelta = sizeDelta;
		}

		public static void SetRectHeight(this Component comp, float height)
		{
			((RectTransform)comp.transform).SetRectHeight(height);
		}

		public static void SetRectHeight(this RectTransform transform, float height)
		{
			Vector2 sizeDelta = transform.sizeDelta;
			sizeDelta.y = height;
			transform.sizeDelta = sizeDelta;
		}

		public static void SetRectSize(this Component comp, Vector2 size)
		{
			((RectTransform)comp.transform).sizeDelta = size;
		}

		public static RectTransform AsRectTransform(this Transform transform)
		{
			return (RectTransform)transform;
		}
	}
}
