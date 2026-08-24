using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class Vector3DataField : DataField, IDataField<Vector3>
	{
		[SerializeField]
		private TMP_InputField xText;

		[SerializeField]
		private TMP_InputField yText;

		[SerializeField]
		private TMP_InputField zText;

		public string FormatString;

		private IValueWrapper<Vector3> wrapper;

		protected override void SetFieldInteractable(bool value)
		{
			xText.interactable = value;
			yText.interactable = value;
			zText.interactable = value;
		}

		protected override void AwakeSetup()
		{
			xText.contentType = TMP_InputField.ContentType.DecimalNumber;
			yText.contentType = TMP_InputField.ContentType.DecimalNumber;
			zText.contentType = TMP_InputField.ContentType.DecimalNumber;
			xText.onEndEdit.AddListener(Text_OnValueChanged);
			yText.onEndEdit.AddListener(Text_OnValueChanged);
			zText.onEndEdit.AddListener(Text_OnValueChanged);
		}

		public void Setup(string label, IValueWrapper<Vector3> wrapper)
		{
			this.wrapper?.UnregisterOnChange(this);
			this.wrapper = wrapper;
			this.wrapper.RegisterOnChange(this, Wrapper_OnChange);
			base.label.text = label;
			UpdateFields(wrapper.Value);
			base.Interactable = true;
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}

		public void SetupReadOnly(string label, GlobalPosition value)
		{
			SetupReadOnly(label, value.AsVector3());
		}

		public void SetupReadOnly(string label, Vector3 value)
		{
			wrapper?.UnregisterOnChange(this);
			wrapper = null;
			base.label.text = label;
			UpdateFields(value);
			base.Interactable = false;
		}

		public void SetupNonValue(string label)
		{
			wrapper?.UnregisterOnChange(this);
			wrapper = null;
			base.label.text = label;
			xText.SetIfNotFocus("-");
			yText.SetIfNotFocus("-");
			zText.SetIfNotFocus("-");
			base.Interactable = false;
		}

		private void Wrapper_OnChange(Vector3 newValue)
		{
			UpdateFields(newValue);
		}

		private void UpdateFields(Vector3 current)
		{
			if (string.IsNullOrEmpty(FormatString))
			{
				xText.SetIfNotFocus(current.x.ToString());
				yText.SetIfNotFocus(current.y.ToString());
				zText.SetIfNotFocus(current.z.ToString());
			}
			else
			{
				xText.SetIfNotFocus(current.x.ToString(FormatString));
				yText.SetIfNotFocus(current.y.ToString(FormatString));
				zText.SetIfNotFocus(current.z.ToString(FormatString));
			}
		}

		private void Text_OnValueChanged(string arg0)
		{
			if ((0u | (float.TryParse(xText.text, out var result) ? 1u : 0u) | (float.TryParse(yText.text, out var result2) ? 1u : 0u) | (float.TryParse(zText.text, out var result3) ? 1u : 0u)) != 0)
			{
				Vector3 value = new Vector3(result, result2, result3);
				wrapper?.SetValue(value, this);
			}
		}
	}
}
