using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphPin : GraphNodeElement, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
	{
		private GraphPinData pinData;

		[Header("Visual References")]
		[SerializeField]
		private Image dotImage;

		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private Color labelColor = Color.white;

		[SerializeField]
		private Color labelColorSelected = Color.yellow;

		[SerializeField]
		private Color inputDotColor = Color.green;

		[SerializeField]
		private Color outputDotColor = Color.red;

		public bool IsSelected { get; private set; }

		public PinId PinId => pinData.PinId;

		public PinType PinType => pinData.PinType;

		public List<PinType> AllowedConnectionTypes => pinData.AllowedConnectionTypes;

		public bool AllowMultipleConnections => pinData.AllowMultipleConnections;

		public RectTransform RectTransform => base.transform.AsRectTransform();

		public Vector3 ConnectionAnchorPosition
		{
			get
			{
				if (!(dotImage != null))
				{
					return base.transform.position;
				}
				return dotImage.rectTransform.position;
			}
		}

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			GraphPinData graphPinData = (GraphPinData)data;
			Init(parentNode, data, editor, direction);
			pinData = graphPinData;
			labelText.text = (string.IsNullOrEmpty(graphPinData.DisplayName) ? graphPinData.PinId.ToString() : graphPinData.DisplayName);
			labelText.color = labelColor;
			bool flag = base.Direction == PinDirection.Input;
			dotImage.color = (flag ? inputDotColor : outputDotColor);
			if (flag)
			{
				dotImage.transform.SetAsFirstSibling();
			}
			else
			{
				dotImage.transform.SetAsLastSibling();
			}
		}

		public bool IsCompatibleWith(GraphPin other)
		{
			if (other == null)
			{
				return false;
			}
			if (base.Node == other.Node)
			{
				return false;
			}
			return IsCompatibleWith(other.Direction, other.PinType, other.AllowedConnectionTypes);
		}

		public bool IsCompatibleWith(PinDirection otherDirection, PinType otherPinType, List<PinType> otherAllowedTypes)
		{
			if (base.Direction == otherDirection)
			{
				return false;
			}
			if (PinType == otherPinType)
			{
				return true;
			}
			if (AllowedConnectionTypes != null && AllowedConnectionTypes.Contains(otherPinType))
			{
				return true;
			}
			if (otherAllowedTypes != null && otherAllowedTypes.Contains(PinType))
			{
				return true;
			}
			return false;
		}

		void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
		{
			base.Editor.DraggedPin = this;
			base.Editor.TempDragTarget = eventData.position;
		}

		void IDragHandler.OnDrag(PointerEventData eventData)
		{
			base.Editor.TempDragTarget = eventData.position;
		}

		void IEndDragHandler.OnEndDrag(PointerEventData eventData)
		{
			base.Editor.DraggedPin = null;
			GameObject gameObject = eventData.pointerCurrentRaycast.gameObject;
			GraphPin graphPin = null;
			if (gameObject != null)
			{
				graphPin = gameObject.GetComponentInParent<GraphPin>();
			}
			if (graphPin != null)
			{
				if (IsCompatibleWith(graphPin))
				{
					base.Editor.Connect(this, graphPin);
				}
			}
			else
			{
				base.Editor.OpenContextMenuAtMouse(new ContextMenuOpenSource(this));
			}
		}

		public void SetSelected(bool selected)
		{
			IsSelected = selected;
			labelText.color = (selected ? labelColorSelected : labelColor);
		}

		void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				base.Editor.SelectPin(this);
			}
			else if (eventData.button == PointerEventData.InputButton.Right)
			{
				base.Editor.OpenContextMenuForPin(this, eventData.position);
			}
		}

		public override string ToString()
		{
			if (base.Node != null && base.Node.Data != null)
			{
				return $"{base.Node.Data.TitleText}.{PinId}";
			}
			return PinId.ToString();
		}
	}
}
