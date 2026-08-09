using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.NodeGraph
{
	public class GraphReferenceField : GraphNodeElement
	{
		[SerializeField]
		private TextMeshProUGUI labelText;

		[SerializeField]
		private Button selectButton;

		[SerializeField]
		private TextMeshProUGUI buttonText;

		public ReferencePopup Popup;

		private GraphReferenceFieldData fieldData;

		private void OnValidate()
		{
		}

		public override void Setup(GraphNode parentNode, GraphElementData data, GraphEditor editor, PinDirection direction)
		{
			fieldData = (GraphReferenceFieldData)data;
			Init(parentNode, data, editor, direction);
			Popup.Hide();
			labelText.text = (string.IsNullOrEmpty(fieldData.DisplayName) ? fieldData.PinId.ToString() : fieldData.DisplayName);
			UpdateButtonText();
			selectButton.onClick.AddListener(OnClickButton);
		}

		private void UpdateButtonText()
		{
			ISaveableReference saveableReference = fieldData.GetValue?.Invoke();
			buttonText.text = ((saveableReference != null) ? saveableReference.ToUIString(oneLine: true) : "<none>");
		}

		private void OnClickButton()
		{
			ISaveableReference startingOption = fieldData.GetValue?.Invoke();
			Popup.ShowPickOption(startingOption, allowNone: true, () => fieldData.GetOptions?.Invoke() ?? new List<ISaveableReference>(), (ISaveableReference o) => o.ToUIString(), delegate(bool pick, ISaveableReference obj)
			{
				if (pick)
				{
					fieldData.SetValue?.Invoke(obj);
					UpdateButtonText();
				}
			});
		}
	}
}
