using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphFloatField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private TMP_InputField inputField;

		private IValueWrapper<float> wrapper;

		private bool suppressCallbacks;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			GraphFloatFieldData graphFloatFieldData = (GraphFloatFieldData)data;
			Init(parentNode, data, editor, direction);
			wrapper = graphFloatFieldData.ValueWrapper;
			labelText.text = (string.IsNullOrEmpty(graphFloatFieldData.DisplayName) ? graphFloatFieldData.PinId.ToString() : graphFloatFieldData.DisplayName);
			suppressCallbacks = true;
			inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
			inputField.text = wrapper.Value.ToString();
			suppressCallbacks = false;
			inputField.onEndEdit.AddListener(OnInputEndEdit);
			wrapper.RegisterOnChange(this, OnWrapperChanged);
		}

		private void OnInputEndEdit(string val)
		{
			if (!suppressCallbacks && float.TryParse(val, out var result))
			{
				wrapper.SetValue(result, this);
			}
		}

		private void OnWrapperChanged(float newValue)
		{
			inputField.text = newValue.ToString();
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}
	}
}
