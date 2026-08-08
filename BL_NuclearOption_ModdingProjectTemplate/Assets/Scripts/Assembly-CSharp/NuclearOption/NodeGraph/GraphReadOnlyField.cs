using TMPro;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	public class GraphReadOnlyField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private TextMeshProUGUI valueText;

		private GraphReadOnlyFieldData fieldData;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			fieldData = (GraphReadOnlyFieldData)data;
			Init(parentNode, data, editor, direction);
			labelText.text = (string.IsNullOrEmpty(fieldData.DisplayName) ? fieldData.PinId.ToString() : fieldData.DisplayName);
			UpdateText();
		}

		private void UpdateText()
		{
			valueText.text = fieldData.GetText?.Invoke() ?? string.Empty;
		}

		private void Update()
		{
			UpdateText();
		}
	}
}
