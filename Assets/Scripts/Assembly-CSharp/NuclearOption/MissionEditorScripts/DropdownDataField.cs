using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class DropdownDataField : DataField
	{
		[SerializeField]
		private TMP_Dropdown dropdown;

		private Action<int> setValue;

		private List<string> options;

		private Color? startingTextColor;

		protected override void SetFieldInteractable(bool value)
		{
			if (!startingTextColor.HasValue)
			{
				startingTextColor = dropdown.itemText.color;
			}
			dropdown.interactable = value;
			dropdown.itemText.color = startingTextColor.Value * (value ? 1f : 0.7f);
		}

		protected override void AwakeSetup()
		{
			dropdown.onValueChanged.AddListener(OnValueChanged);
		}

		public void Setup(string label, List<string> options, string current, Action<string> setValue)
		{
			int num = options.IndexOf(current);
			if (num == -1)
			{
				num = 0;
			}
			Setup(label, options, num, delegate(int i)
			{
				setValue(options[i]);
			});
		}

		public void Setup(string label, List<string> options, int current, Action<int> setValue)
		{
			if (!string.IsNullOrEmpty(label))
			{
				base.label.text = label;
			}
			dropdown.ClearOptions();
			this.options = options;
			dropdown.AddOptions(options);
			dropdown.SetValueWithoutNotify(current);
			this.setValue = setValue;
			base.Interactable = true;
		}

		private void OnValueChanged(int arg0)
		{
			setValue?.Invoke(arg0);
		}

		public void LabelWidth(int width)
		{
			RectTransform obj = (RectTransform)label.transform;
			Vector2 sizeDelta = obj.sizeDelta;
			sizeDelta.x = width;
			obj.sizeDelta = sizeDelta;
		}

		internal void SetValue(string faction)
		{
			int num = options.IndexOf(faction);
			if (num == -1)
			{
				num = 0;
			}
			dropdown.SetValueWithoutNotify(num);
			setValue?.Invoke(num);
		}
	}
}
