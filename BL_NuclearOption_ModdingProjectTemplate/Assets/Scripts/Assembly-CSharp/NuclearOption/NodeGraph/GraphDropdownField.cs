using System.Collections.Generic;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphDropdownField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private TMP_Dropdown dropdownField;

		private IValueWrapper<int> wrapper;

		private bool suppressCallbacks;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			GraphDropdownFieldData graphDropdownFieldData = (GraphDropdownFieldData)data;
			Init(parentNode, data, editor, direction);
			wrapper = graphDropdownFieldData.ValueWrapper;
			labelText.text = (string.IsNullOrEmpty(graphDropdownFieldData.DisplayName) ? graphDropdownFieldData.PinId.ToString() : graphDropdownFieldData.DisplayName);
			suppressCallbacks = true;
			dropdownField.ClearOptions();
			dropdownField.AddOptions(graphDropdownFieldData.Options ?? new List<string>());
			dropdownField.value = wrapper.Value;
			suppressCallbacks = false;
			dropdownField.onValueChanged.AddListener(OnDropdownChanged);
			wrapper.RegisterOnChange(this, OnWrapperChanged);
		}

		private void OnDropdownChanged(int val)
		{
			if (!suppressCallbacks)
			{
				wrapper.SetValue(val, this);
			}
		}

		private void OnWrapperChanged(int newValue)
		{
			suppressCallbacks = true;
			dropdownField.value = newValue;
			suppressCallbacks = false;
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}
	}
}
