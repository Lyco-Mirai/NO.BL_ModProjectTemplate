using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[AddComponentMenu("UI/Effects/UI Gradient")]
	[RequireComponent(typeof(Graphic))]
	public class UIGradient : BaseMeshEffect
	{
		[SerializeField]
		private Color startColor = new Color(0f, 0.6f, 1f, 0.25f);

		[SerializeField]
		private Color endColor = new Color(0.11f, 0.122f, 0.188f, 0f);

		public Color StartColor
		{
			get
			{
				return startColor;
			}
			set
			{
				startColor = value;
				base.graphic.SetVerticesDirty();
			}
		}

		public Color EndColor
		{
			get
			{
				return endColor;
			}
			set
			{
				endColor = value;
				base.graphic.SetVerticesDirty();
			}
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (!IsActive())
			{
				return;
			}
			int currentVertCount = vh.currentVertCount;
			if (currentVertCount == 0)
			{
				return;
			}
			UIVertex vertex = default(UIVertex);
			float num = float.MaxValue;
			float num2 = float.MinValue;
			for (int i = 0; i < currentVertCount; i++)
			{
				vh.PopulateUIVertex(ref vertex, i);
				num = Mathf.Min(num, vertex.position.x);
				num2 = Mathf.Max(num2, vertex.position.x);
			}
			float num3 = num2 - num;
			if (!Mathf.Approximately(num3, 0f))
			{
				for (int j = 0; j < currentVertCount; j++)
				{
					vh.PopulateUIVertex(ref vertex, j);
					float t = (vertex.position.x - num) / num3;
					Color color = Color.Lerp(startColor, endColor, t);
					vertex.color *= color;
					vh.SetUIVertex(vertex, j);
				}
			}
		}
	}
}
