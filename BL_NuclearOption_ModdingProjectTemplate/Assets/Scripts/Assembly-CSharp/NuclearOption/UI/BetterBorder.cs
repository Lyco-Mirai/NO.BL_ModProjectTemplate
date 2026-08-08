using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[AddComponentMenu("UI/Better Border", 532)]
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(CanvasRenderer))]
	public class BetterBorder : MaskableGraphic
	{
		[Tooltip("Thickness of the border in Unity units.")]
		[SerializeField]
		private float borderThickness = 1f;

		[Tooltip("Color of the central fill area.")]
		[SerializeField]
		private Color fillColor = Color.clear;

		[Header("Sides")]
		[SerializeField]
		private bool left = true;

		[SerializeField]
		private bool right = true;

		[SerializeField]
		private bool top = true;

		[SerializeField]
		private bool bottom = true;

		public float BorderThickness
		{
			get
			{
				return borderThickness;
			}
			set
			{
				if (!Mathf.Approximately(borderThickness, value))
				{
					borderThickness = value;
					SetVerticesDirty();
				}
			}
		}

		public override Color color
		{
			get
			{
				return base.color;
			}
			set
			{
				if (!(base.color == value))
				{
					base.color = value;
					SetVerticesDirty();
				}
			}
		}

		public Color FillColor
		{
			get
			{
				return fillColor;
			}
			set
			{
				if (!(fillColor == value))
				{
					fillColor = value;
					SetVerticesDirty();
				}
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			Rect pixelAdjustedRect = GetPixelAdjustedRect();
			float xMin = pixelAdjustedRect.xMin;
			float yMin = pixelAdjustedRect.yMin;
			float xMax = pixelAdjustedRect.xMax;
			float yMax = pixelAdjustedRect.yMax;
			float num = borderThickness;
			if (num > 0f)
			{
				float b = ((base.canvas != null) ? (1f / base.canvas.scaleFactor) : 1f);
				num = Mathf.Max(num, b);
			}
			num = Mathf.Min(num, pixelAdjustedRect.width / 2f, pixelAdjustedRect.height / 2f);
			float num2 = xMin + (left ? num : 0f);
			float num3 = xMax - (right ? num : 0f);
			float num4 = yMin + (bottom ? num : 0f);
			float num5 = yMax - (top ? num : 0f);
			if (fillColor.a > 0f && num2 < num3 && num4 < num5)
			{
				AddQuad(vh, new Vector2(num2, num4), new Vector2(num3, num5), fillColor);
			}
			if (color.a > 0f && num > 0f)
			{
				if (top)
				{
					AddQuad(vh, new Vector2(xMin, yMax - num), new Vector2(xMax, yMax), color);
				}
				if (bottom)
				{
					AddQuad(vh, new Vector2(xMin, yMin), new Vector2(xMax, yMin + num), color);
				}
				float y = (bottom ? (yMin + num) : yMin);
				float y2 = (top ? (yMax - num) : yMax);
				if (left)
				{
					AddQuad(vh, new Vector2(xMin, y), new Vector2(xMin + num, y2), color);
				}
				if (right)
				{
					AddQuad(vh, new Vector2(xMax - num, y), new Vector2(xMax, y2), color);
				}
			}
		}

		private void AddQuad(VertexHelper vh, Vector2 bottomLeft, Vector2 topRight, Color quadColor)
		{
			int currentVertCount = vh.currentVertCount;
			UIVertex simpleVert = UIVertex.simpleVert;
			simpleVert.color = quadColor;
			simpleVert.uv0 = Vector2.zero;
			simpleVert.position = new Vector3(bottomLeft.x, bottomLeft.y, 0f);
			vh.AddVert(simpleVert);
			simpleVert.position = new Vector3(bottomLeft.x, topRight.y, 0f);
			vh.AddVert(simpleVert);
			simpleVert.position = new Vector3(topRight.x, topRight.y, 0f);
			vh.AddVert(simpleVert);
			simpleVert.position = new Vector3(topRight.x, bottomLeft.y, 0f);
			vh.AddVert(simpleVert);
			vh.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 2);
			vh.AddTriangle(currentVertCount + 2, currentVertCount + 3, currentVertCount);
		}
	}
}
