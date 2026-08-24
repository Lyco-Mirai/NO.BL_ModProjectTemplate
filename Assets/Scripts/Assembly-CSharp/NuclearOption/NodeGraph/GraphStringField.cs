using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphStringField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private TMP_InputField inputField;

		private IValueWrapper<string> wrapper;

		private bool suppressCallbacks;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			GraphStringFieldData graphStringFieldData = (GraphStringFieldData)data;
			Init(parentNode, data, editor, direction);
			wrapper = graphStringFieldData.ValueWrapper;
			labelText.text = (string.IsNullOrEmpty(graphStringFieldData.DisplayName) ? graphStringFieldData.PinId.ToString() : graphStringFieldData.DisplayName);
			suppressCallbacks = true;
			inputField.contentType = TMP_InputField.ContentType.Standard;
			inputField.text = wrapper.Value ?? string.Empty;
			suppressCallbacks = false;
			inputField.onEndEdit.AddListener(OnInputEndEdit);
			wrapper.RegisterOnChange(this, OnWrapperChanged);
		}

		private void OnInputEndEdit(string val)
		{
			if (!suppressCallbacks)
			{
				wrapper.SetValue(val, this);
			}
		}

		private void OnWrapperChanged(string newValue)
		{
			inputField.text = newValue ?? string.Empty;
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}
	}
}
