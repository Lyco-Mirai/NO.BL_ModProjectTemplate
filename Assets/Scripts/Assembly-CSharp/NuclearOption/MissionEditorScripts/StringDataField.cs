using System;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class StringDataField : DataField, IDataField<string>
	{
		[SerializeField]
		private TMP_InputField text;

		[SerializeField]
		private float heightPerLine = 30f;

		private Action<string> setValue;

		private IValueWrapper wrapper;

		protected override void SetFieldInteractable(bool value)
		{
			text.interactable = value;
		}

		protected override void AwakeSetup()
		{
			text.onValueChanged.AddListener(OnValueChanged);
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}

		void IDataField<string>.Setup(string label, IValueWrapper<string> wrapper)
		{
			Setup(label, wrapper);
		}

		public void Setup(string label, IValueWrapper<string> wrapper, int? multiLineCount = null)
		{
			this.wrapper?.UnregisterOnChange(this);
			this.wrapper = wrapper;
			wrapper.RegisterOnChange(this, delegate
			{
				text.SetTextWithoutNotify(wrapper.Value ?? "");
			});
			Setup(label, wrapper.Value, delegate(string v)
			{
				wrapper.SetValue(v, this);
			}, multiLineCount);
		}

		public void Setup(string label, string current, Action<string> setValue, int? multiLineCount = null)
		{
			text.lineType = (multiLineCount.HasValue ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine);
			RectTransform obj = (RectTransform)text.transform;
			Vector2 sizeDelta = obj.sizeDelta;
			if (multiLineCount.HasValue)
			{
				sizeDelta.y = heightPerLine * (float)multiLineCount.Value;
			}
			obj.sizeDelta = sizeDelta;
			base.label.text = label;
			text.SetTextWithoutNotify(current ?? "");
			this.setValue = setValue;
			base.Interactable = true;
		}

		private void OnValueChanged(string value)
		{
			setValue?.Invoke(value);
		}
	}
}
