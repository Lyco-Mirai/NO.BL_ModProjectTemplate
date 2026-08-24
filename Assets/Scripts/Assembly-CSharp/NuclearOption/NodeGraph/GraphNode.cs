using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphNode : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IBeginDragHandler, IDragHandler
	{
		[SerializeField]
		private Image headerImage;

		[SerializeField]
		private Image horizontalLineImage;

		[SerializeField]
		private Image bodyImage;

		[SerializeField]
		private Image tagBackgroundImage;

		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI tagText;

		[Header("Default Theme Colors")]
		[SerializeField]
		private Color defaultHeaderColor = Color.grey;

		[SerializeField]
		private Color defaultLineColor = Color.white;

		[SerializeField]
		private Color defaultBodyColor = new Color(0.2f, 0.2f, 0.2f, 1f);

		[SerializeField]
		private Color defaultTagTextColor = Color.white;

		[SerializeField]
		private Color defaultTagBgColor = new Color(0.4f, 0.4f, 0.4f, 1f);

		[Header("Pins")]
		[SerializeField]
		private RectTransform leftPinContainer;

		[SerializeField]
		private RectTransform rightPinContainer;

		[SerializeField]
		private GraphPin pinPrefab;

		[SerializeField]
		private GraphFloatField floatFieldPrefab;

		[SerializeField]
		private GraphIntField intFieldPrefab;

		[SerializeField]
		private GraphBoolField boolFieldPrefab;

		[SerializeField]
		private GraphDropdownField dropdownFieldPrefab;

		[SerializeField]
		private GraphStringField stringFieldPrefab;

		[SerializeField]
		private GraphReferenceField referenceFieldPrefab;

		[SerializeField]
		private GraphReadOnlyField readOnlyFieldPrefab;

		[SerializeField]
		private Image selectionOutline;

		[Header("Selection Colors")]
		[SerializeField]
		private Color selectedColor = Color.white;

		[SerializeField]
		private Color pendingSelectedColor = new Color(1f, 1f, 1f, 0.5f);

		private Canvas canvas;

		private GraphEditor editor;

		public RectTransform rectTransform => base.transform.AsRectTransform();

		public GraphNodeData Data { get; private set; }

		public NodeSelectionState SelectionState { get; private set; }

		public bool IsSelected => SelectionState == NodeSelectionState.Selected;

		public List<GraphPin> Pins { get; } = new List<GraphPin>();

		private void OnValidate()
		{
			if (headerImage != null)
			{
				headerImage.color = defaultHeaderColor;
			}
			if (horizontalLineImage != null)
			{
				horizontalLineImage.color = defaultLineColor;
			}
			if (bodyImage != null)
			{
				bodyImage.color = defaultBodyColor;
			}
			if (tagBackgroundImage != null)
			{
				tagBackgroundImage.color = defaultTagBgColor;
			}
			if (tagText != null)
			{
				tagText.color = defaultTagTextColor;
			}
		}

		private void OnEnable()
		{
			if (editor != null && !editor.AllNodes.Contains(this))
			{
				editor.AllNodes.Add(this);
			}
		}

		private void OnDisable()
		{
			if (editor != null)
			{
				editor.AllNodes.Remove(this);
				SetSelected(NodeSelectionState.None);
			}
		}

		public void Setup(GraphNodeData data, GraphEditor editor)
		{
			Data = data;
			this.editor = editor;
			canvas = editor.Canvas;
			if (!editor.AllNodes.Contains(this))
			{
				editor.AllNodes.Add(this);
			}
			titleText.text = data.TitleText;
			tagText.text = data.TagText;
			Color.RGBToHSV(data.NodeColor, out var H, out var S, out var V);
			headerImage.color = ShiftHue(defaultHeaderColor, H);
			horizontalLineImage.color = ShiftHue(defaultLineColor, H);
			bodyImage.color = ShiftHue(defaultBodyColor, H);
			Color.RGBToHSV(data.TagColor, out var H2, out V, out S);
			tagBackgroundImage.color = ShiftHue(defaultTagBgColor, H2);
			tagText.color = ShiftHue(defaultTagTextColor, H2);
			rectTransform.anchoredPosition = data.Position;
			UpdateOutline(SelectionState);
			foreach (GraphElementData inputElement in Data.InputElements)
			{
				SpawnElement(inputElement, leftPinContainer, PinDirection.Input);
			}
			foreach (GraphElementData outputElement in Data.OutputElements)
			{
				SpawnElement(outputElement, rightPinContainer, PinDirection.Output);
			}
		}

		public void UpdateTitle(string newTitle)
		{
			Data.TitleText = newTitle;
			titleText.text = newTitle;
		}

		private void SpawnElement(GraphElementData data, RectTransform container, PinDirection direction)
		{
			GraphNodeElement graphNodeElement = InstantiateElement(data, container);
			graphNodeElement.Setup(this, data, editor, direction);
			if (graphNodeElement is GraphPin item)
			{
				Pins.Add(item);
			}
		}

		private GraphNodeElement InstantiateElement(GraphElementData data, RectTransform container)
		{
			if (!(data is GraphPinData))
			{
				if (!(data is GraphFloatFieldData))
				{
					if (!(data is GraphIntFieldData))
					{
						if (!(data is GraphBoolFieldData))
						{
							if (!(data is GraphDropdownFieldData))
							{
								if (!(data is GraphStringFieldData))
								{
									if (!(data is GraphReferenceFieldData))
									{
										if (data is GraphReadOnlyFieldData)
										{
											return UnityEngine.Object.Instantiate(readOnlyFieldPrefab, container, worldPositionStays: false);
										}
										throw new ArgumentOutOfRangeException("data", "Unhandled element data type: " + data.GetType().Name);
									}
									return UnityEngine.Object.Instantiate(referenceFieldPrefab, container, worldPositionStays: false);
								}
								return UnityEngine.Object.Instantiate(stringFieldPrefab, container, worldPositionStays: false);
							}
							return UnityEngine.Object.Instantiate(dropdownFieldPrefab, container, worldPositionStays: false);
						}
						return UnityEngine.Object.Instantiate(boolFieldPrefab, container, worldPositionStays: false);
					}
					return UnityEngine.Object.Instantiate(intFieldPrefab, container, worldPositionStays: false);
				}
				return UnityEngine.Object.Instantiate(floatFieldPrefab, container, worldPositionStays: false);
			}
			return UnityEngine.Object.Instantiate(pinPrefab, container, worldPositionStays: false);
		}

		private Color ShiftHue(Color color, float hue)
		{
			Color.RGBToHSV(color, out var _, out var S, out var V);
			return Color.HSVToRGB(hue, S, V);
		}

		public bool TryGetPin(PinId pinId, PinDirection direction, out GraphPin pin)
		{
			foreach (GraphPin pin2 in Pins)
			{
				if (pin2.PinId == pinId && pin2.Direction == direction)
				{
					pin = pin2;
					return true;
				}
			}
			pin = null;
			return false;
		}

		public void SetSelected(NodeSelectionState state, bool updateEditor = true, bool notify = true)
		{
			if (SelectionState != state)
			{
				SelectionState = state;
				UpdateOutline(state);
				if (updateEditor && editor != null)
				{
					editor.OnNodeSelectedInternal(this, state == NodeSelectionState.Selected, notify);
				}
			}
		}

		private void UpdateOutline(NodeSelectionState state)
		{
			switch (state)
			{
			case NodeSelectionState.None:
				selectionOutline.enabled = false;
				break;
			case NodeSelectionState.PendingDragSelect:
				selectionOutline.enabled = true;
				selectionOutline.color = pendingSelectedColor;
				break;
			case NodeSelectionState.Selected:
				selectionOutline.enabled = true;
				selectionOutline.color = selectedColor;
				break;
			}
		}

		public void SelectIndividually()
		{
			editor.ClearSelection(notify: false);
			SetSelected(NodeSelectionState.Selected);
		}

		void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
		{
			if (!IsSelected)
			{
				SelectIndividually();
			}
		}

		void IDragHandler.OnDrag(PointerEventData eventData)
		{
			Vector2 delta = eventData.delta;
			delta /= canvas.scaleFactor;
			if (base.transform.parent != null)
			{
				delta /= base.transform.parent.localScale.x;
			}
			foreach (GraphNode allNode in editor.AllNodes)
			{
				if (allNode.IsSelected)
				{
					allNode.rectTransform.anchoredPosition += delta;
					if (allNode.Data != null)
					{
						allNode.Data.Position = allNode.rectTransform.anchoredPosition;
					}
				}
			}
		}

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				SelectIndividually();
			}
			else if (eventData.button == PointerEventData.InputButton.Right)
			{
				editor.OpenContextMenuForNode(this, eventData.position);
			}
		}
	}
}
