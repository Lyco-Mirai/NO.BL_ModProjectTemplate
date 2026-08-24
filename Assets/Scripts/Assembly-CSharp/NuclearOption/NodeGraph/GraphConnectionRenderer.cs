using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class GraphConnectionRenderer : MaskableGraphic
	{
		[Header("Graph Connection")]
		[SerializeField]
		private GraphEditor graphEditor;

		[SerializeField]
		private float thickness = 3f;

		[SerializeField]
		private Color connectionColor = Color.white;

		[SerializeField]
		private Color tempConnectionColor = Color.yellow;

		[SerializeField]
		private int curveSegments = 30;

		[SerializeField]
		private float handleDistance = 100f;

		public float HandleDistance => handleDistance;

		private void Update()
		{
			SetVerticesDirty();
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			foreach (GraphConnection connection in graphEditor.Connections)
			{
				Vector2 start = WorldToLocal(connection.OutputPin.ConnectionAnchorPosition);
				Vector2 end = WorldToLocal(connection.InputPin.ConnectionAnchorPosition);
				if (graphEditor.SelectedConnection == connection)
				{
					DrawBezier(vh, start, end, startIsInput: false, endIsInput: true, tempConnectionColor, thickness * 1.5f);
				}
				else
				{
					DrawBezier(vh, start, end, startIsInput: false, endIsInput: true, connectionColor);
				}
			}
			if (graphEditor.DraggedPin != null)
			{
				GraphPin draggedPin = graphEditor.DraggedPin;
				if (draggedPin.Direction == PinDirection.Input)
				{
					Vector2 start2 = ScreenToLocal(graphEditor.TempDragTarget);
					Vector2 end2 = WorldToLocal(draggedPin.ConnectionAnchorPosition);
					DrawBezier(vh, start2, end2, startIsInput: false, endIsInput: true, tempConnectionColor);
				}
				else
				{
					Vector2 start3 = WorldToLocal(draggedPin.ConnectionAnchorPosition);
					Vector2 end3 = ScreenToLocal(graphEditor.TempDragTarget);
					DrawBezier(vh, start3, end3, startIsInput: false, endIsInput: true, tempConnectionColor);
				}
			}
		}

		private Vector2 WorldToLocal(Vector3 worldPos)
		{
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(base.rectTransform, screenPoint, null, out var localPoint);
			return localPoint;
		}

		private Vector2 ScreenToLocal(Vector2 screenPoint)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(base.rectTransform, screenPoint, null, out var localPoint);
			return localPoint;
		}

		private void DrawBezier(VertexHelper vh, Vector2 start, Vector2 end, bool startIsInput, bool endIsInput, Color color, float drawThickness = -1f)
		{
			float num = ((drawThickness > 0f) ? drawThickness : thickness);
			float num2 = Mathf.Abs(start.x - end.x);
			float num3 = Mathf.Max(handleDistance, num2 * 0.5f);
			Vector2 p = start + (startIsInput ? Vector2.left : Vector2.right) * num3;
			float num4 = Mathf.Max(handleDistance, num2 * 0.5f);
			Vector2 p2 = end + (endIsInput ? Vector2.left : Vector2.right) * num4;
			Vector2[] array = new Vector2[curveSegments + 1];
			for (int i = 0; i <= curveSegments; i++)
			{
				float t = (float)i / (float)curveSegments;
				array[i] = EvaluateCubicBezier(start, p, p2, end, t);
			}
			Vector2[] array2 = new Vector2[curveSegments + 1];
			for (int j = 0; j <= curveSegments; j++)
			{
				Vector2 vector = ((j != 0) ? ((j != curveSegments) ? (array[j + 1] - array[j - 1]) : (array[curveSegments] - array[curveSegments - 1])) : (array[1] - array[0]));
				vector.Normalize();
				array2[j] = new Vector2(0f - vector.y, vector.x);
			}
			int currentVertCount = vh.currentVertCount;
			for (int k = 0; k <= curveSegments; k++)
			{
				float y = (float)k / (float)curveSegments;
				UIVertex simpleVert = UIVertex.simpleVert;
				simpleVert.color = color;
				simpleVert.position = array[k] - array2[k] * (num * 0.5f);
				simpleVert.uv0 = new Vector2(0f, y);
				vh.AddVert(simpleVert);
				simpleVert.position = array[k] + array2[k] * (num * 0.5f);
				simpleVert.uv0 = new Vector2(1f, y);
				vh.AddVert(simpleVert);
			}
			for (int l = 0; l < curveSegments; l++)
			{
				int num5 = currentVertCount + l * 2;
				int idx = num5 + 1;
				int idx2 = num5 + 2;
				int num6 = num5 + 3;
				vh.AddTriangle(num5, idx, num6);
				vh.AddTriangle(num5, num6, idx2);
			}
		}

		public static Vector2 EvaluateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			float num = 1f - t;
			float num2 = num * num;
			float num3 = num2 * num;
			float num4 = t * t;
			float num5 = num4 * t;
			return num3 * p0 + 3f * num2 * t * p1 + 3f * num * num4 * p2 + num5 * p3;
		}
	}
}
