using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphBackground : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		[SerializeField]
		private GraphEditor graphEditor;

		[SerializeField]
		private RectTransform contentTransform;

		[SerializeField]
		private RawImage backgroundImage;

		[SerializeField]
		private RectTransform selectionBox;

		[SerializeField]
		private Canvas canvas;

		[SerializeField]
		private float zoomSpeed = 0.15f;

		[SerializeField]
		private float panSpeed = 1f;

		[SerializeField]
		private float gridSpacing = 64f;

		private Vector2 dragStartPos;

		private bool isPanning;

		private bool isSelecting;

		private void OnValidate()
		{
		}

		private void Awake()
		{
			selectionBox.gameObject.SetActive(value: false);
			RectTransform rectTransform = selectionBox.parent.AsRectTransform();
			selectionBox.anchorMin = rectTransform.pivot;
			selectionBox.anchorMax = rectTransform.pivot;
			selectionBox.pivot = Vector2.zero;
			if (backgroundImage.texture != null)
			{
				backgroundImage.texture.wrapMode = TextureWrapMode.Repeat;
			}
		}

		private void Update()
		{
			UpdateBackgroundUV();
			if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)base.transform, Input.mousePosition, null))
			{
				return;
			}
			if (EventSystem.current != null)
			{
				PointerEventData eventData = new PointerEventData(EventSystem.current)
				{
					position = Input.mousePosition
				};
				List<RaycastResult> list = new List<RaycastResult>();
				EventSystem.current.RaycastAll(eventData, list);
				if (list.Count > 0)
				{
					Transform transform = list[0].gameObject.transform;
					if (!transform.IsChildOf(base.transform) && (contentTransform == null || !transform.IsChildOf(contentTransform)))
					{
						return;
					}
				}
			}
			float axis = Input.GetAxis("Mouse ScrollWheel");
			if (Mathf.Abs(axis) > 0.001f)
			{
				HandleZoom(axis);
			}
		}

		private void HandleZoom(float scrollDelta)
		{
			Vector3 localScale = contentTransform.localScale;
			float value = localScale.x + scrollDelta * zoomSpeed;
			value = Mathf.Clamp(value, 0.2f, 2f);
			if (!Mathf.Approximately(value, localScale.x))
			{
				Vector3 vector = new Vector3(value, value, 1f);
				Vector2 screenPoint = Input.mousePosition;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(contentTransform, screenPoint, null, out var localPoint);
				Vector2 vector2 = vector - localScale;
				Vector2 vector3 = new Vector2(localPoint.x * vector2.x, localPoint.y * vector2.y);
				contentTransform.anchoredPosition -= vector3;
				contentTransform.localScale = vector;
			}
		}

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Middle)
			{
				isPanning = true;
			}
			else if (eventData.button == PointerEventData.InputButton.Left)
			{
				if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
				{
					graphEditor.ClearSelection();
				}
				isSelecting = true;
				dragStartPos = eventData.position;
				selectionBox.gameObject.SetActive(value: true);
				UpdateSelectionBox(dragStartPos, eventData.position);
				UpdatePendingSelection(dragStartPos, eventData.position);
			}
		}

		void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Middle)
			{
				isPanning = false;
			}
			else if (eventData.button == PointerEventData.InputButton.Left && isSelecting)
			{
				isSelecting = false;
				selectionBox.gameObject.SetActive(value: false);
				graphEditor.ApplyPendingSelection();
			}
		}

		void IDragHandler.OnDrag(PointerEventData eventData)
		{
			if (isPanning && eventData.button == PointerEventData.InputButton.Middle)
			{
				contentTransform.anchoredPosition += eventData.delta * panSpeed;
			}
			else if (isSelecting && eventData.button == PointerEventData.InputButton.Left)
			{
				UpdateSelectionBox(dragStartPos, eventData.position);
				UpdatePendingSelection(dragStartPos, eventData.position);
			}
		}

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
			{
				if (graphEditor.TryGetConnectionAtPosition(eventData.position, out var connection))
				{
					graphEditor.SelectConnection(connection);
				}
				else
				{
					graphEditor.ClearSelection();
				}
			}
			else if (eventData.button == PointerEventData.InputButton.Right && !eventData.dragging)
			{
				if (graphEditor.TryGetConnectionAtPosition(eventData.position, out var connection2))
				{
					graphEditor.SelectConnection(connection2);
					graphEditor.OpenContextMenuForConnection(connection2, eventData.position);
				}
				else
				{
					graphEditor.ClearSelection();
					graphEditor.OpenContextMenuAtMouse(new ContextMenuOpenSource(null));
				}
			}
		}

		private void UpdateSelectionBox(Vector2 startPos, Vector2 currentPos)
		{
			RectTransform rect = selectionBox.parent as RectTransform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, startPos, null, out var localPoint);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, currentPos, null, out var localPoint2);
			Vector2 vector = Vector2.Min(localPoint, localPoint2);
			Vector2 vector2 = Vector2.Max(localPoint, localPoint2);
			selectionBox.anchoredPosition = vector;
			selectionBox.sizeDelta = vector2 - vector;
		}

		private void UpdatePendingSelection(Vector2 startPos, Vector2 currentPos)
		{
			Vector2 vector = Vector2.Min(startPos, currentPos);
			Vector2 vector2 = Vector2.Max(startPos, currentPos);
			Rect rect = new Rect(vector, vector2 - vector);
			foreach (GraphNode allNode in graphEditor.AllNodes)
			{
				Rect screenRect = GetScreenRect(allNode.rectTransform);
				if (rect.Overlaps(screenRect))
				{
					if (allNode.SelectionState == NodeSelectionState.None)
					{
						allNode.SetSelected(NodeSelectionState.PendingDragSelect, updateEditor: false);
					}
				}
				else if (allNode.SelectionState == NodeSelectionState.PendingDragSelect)
				{
					allNode.SetSelected(NodeSelectionState.None, updateEditor: false);
				}
			}
		}

		private Rect GetScreenRect(RectTransform rectTransform)
		{
			Vector3[] array = new Vector3[4];
			rectTransform.GetWorldCorners(array);
			Vector2 vector = RectTransformUtility.WorldToScreenPoint(null, array[0]);
			Vector2 vector2 = vector;
			for (int i = 1; i < 4; i++)
			{
				Vector2 rhs = RectTransformUtility.WorldToScreenPoint(null, array[i]);
				vector = Vector2.Min(vector, rhs);
				vector2 = Vector2.Max(vector2, rhs);
			}
			return new Rect(vector, vector2 - vector);
		}

		private void UpdateBackgroundUV()
		{
			Rect uvRect = backgroundImage.uvRect;
			float x = contentTransform.localScale.x;
			float y = contentTransform.localScale.y;
			Rect rect = backgroundImage.transform.AsRectTransform().rect;
			uvRect.width = rect.width / (gridSpacing * x);
			uvRect.height = rect.height / (gridSpacing * y);
			uvRect.x = (rect.x - contentTransform.anchoredPosition.x) / (gridSpacing * x);
			uvRect.y = (rect.y - contentTransform.anchoredPosition.y) / (gridSpacing * y);
			backgroundImage.uvRect = uvRect;
		}
	}
}
