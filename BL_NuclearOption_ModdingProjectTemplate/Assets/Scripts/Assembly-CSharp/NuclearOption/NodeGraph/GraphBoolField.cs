using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphBoolField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private Toggle toggleField;

		private IValueWrapper<bool> wrapper;

		private bool suppressCallbacks;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			GraphBoolFieldData graphBoolFieldData = (GraphBoolFieldData)data;
			Init(parentNode, data, editor, direction);
			wrapper = graphBoolFieldData.ValueWrapper;
			labelText.text = (string.IsNullOrEmpty(graphBoolFieldData.DisplayName) ? graphBoolFieldData.PinId.ToString() : graphBoolFieldData.DisplayName);
			suppressCallbacks = true;
			toggleField.isOn = wrapper.Value;
			suppressCallbacks = false;
			toggleField.onValueChanged.AddListener(OnToggleChanged);
			wrapper.RegisterOnChange(this, OnWrapperChanged);
		}

		private void OnToggleChanged(bool val)
		{
			if (!suppressCallbacks)
			{
				wrapper.SetValue(val, this);
			}
		}

		private void OnWrapperChanged(bool newValue)
		{
			suppressCallbacks = true;
			toggleField.isOn = newValue;
			suppressCallbacks = false;
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}
	}
}
