using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class BoolDataField : DataField, IDataField<bool>
	{
		[SerializeField]
		private Toggle toggle;

		private IValueWrapper<bool> wrapper;

		protected override void SetFieldInteractable(bool value)
		{
			toggle.interactable = value;
		}

		protected override void AwakeSetup()
		{
			toggle.onValueChanged.AddListener(OnValueChanged);
		}

		public void Setup(string label, IValueWrapper<bool> wrapper)
		{
			this.wrapper?.UnregisterOnChange(this);
			this.wrapper = wrapper;
			wrapper.RegisterOnChange(this, Wrapper_OnChange);
			base.label.text = label;
			toggle.SetIsOnWithoutNotify(wrapper.Value);
			base.Interactable = true;
		}

		private void Wrapper_OnChange(bool newValue)
		{
			toggle.SetIsOnWithoutNotify(newValue);
		}

		private void OnValueChanged(bool value)
		{
			wrapper.SetValue(value, this);
		}
	}
}
