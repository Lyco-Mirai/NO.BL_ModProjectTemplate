using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphIntField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private TMP_InputField inputField;

		private IValueWrapper<int> wrapper;

		private bool suppressCallbacks;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			GraphIntFieldData graphIntFieldData = (GraphIntFieldData)data;
			Init(parentNode, data, editor, direction);
			wrapper = graphIntFieldData.ValueWrapper;
			labelText.text = (string.IsNullOrEmpty(graphIntFieldData.DisplayName) ? graphIntFieldData.PinId.ToString() : graphIntFieldData.DisplayName);
			suppressCallbacks = true;
			inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
			inputField.text = wrapper.Value.ToString();
			suppressCallbacks = false;
			inputField.onEndEdit.AddListener(OnInputEndEdit);
			wrapper.RegisterOnChange(this, OnWrapperChanged);
		}

		private void OnInputEndEdit(string val)
		{
			if (!suppressCallbacks && int.TryParse(val, out var result))
			{
				wrapper.SetValue(result, this);
			}
		}

		private void OnWrapperChanged(int newValue)
		{
			inputField.text = newValue.ToString();
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}
	}
}
