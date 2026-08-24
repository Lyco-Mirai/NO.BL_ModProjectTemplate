using System;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class OverrideDataField : MonoBehaviour
	{
		public struct InnerField
		{
			public readonly DataField DataField;

			public readonly Button Button;

			private Color defaultColor;

			public InnerField(DataField dataField)
			{
				this = default(InnerField);
				DataField = dataField;
			}

			public InnerField(Button button)
			{
				this = default(InnerField);
				Button = button;
			}

			public void Setup()
			{
				if (DataField != null)
				{
					defaultColor = DataField.LabelColor;
				}
				if (Button != null)
				{
					TextMeshProUGUI componentInChildren = Button.GetComponentInChildren<TextMeshProUGUI>();
					if (componentInChildren != null)
					{
						defaultColor = componentInChildren.color;
					}
				}
			}

			public readonly void SetInteractable(bool interactable, Color disabledColor)
			{
				if (DataField != null)
				{
					DataField.Interactable = interactable;
					DataField.LabelColor = (interactable ? defaultColor : disabledColor);
				}
				if (Button != null)
				{
					Button.interactable = interactable;
					TextMeshProUGUI componentInChildren = Button.GetComponentInChildren<TextMeshProUGUI>();
					if (componentInChildren != null)
					{
						componentInChildren.color = (interactable ? defaultColor : disabledColor);
					}
				}
			}

			public static implicit operator InnerField(DataField dataField)
			{
				return new InnerField(dataField);
			}

			public static implicit operator InnerField(Button button)
			{
				return new InnerField(button);
			}
		}

		[SerializeField]
		private Toggle overrideToggle;

		[SerializeField]
		public RectTransform innerHolder;

		[SerializeField]
		private Color labelDisabledColor = new Color(0.8f, 0.8f, 0.8f);

		private InnerField[] innerFields;

		private Action<bool> setOverride;

		private IValueWrapper wrapper;

		private void Awake()
		{
			overrideToggle.onValueChanged.AddListener(OverrideToggled);
			if (setOverride == null)
			{
				overrideToggle.interactable = false;
			}
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}

		public void Setup<T>(ValueWrapper<Override<T>> wrapper, params InnerField[] innerFields) where T : IEquatable<T>
		{
			this.wrapper?.UnregisterOnChange(this);
			this.wrapper = wrapper;
			setOverride = delegate(bool isOverride)
			{
				T value = wrapper.Value.Value;
				Override<T> value2 = new Override<T>(isOverride, value);
				wrapper.SetValue(value2, this);
			};
			wrapper.RegisterOnChange(this, delegate(Override<T> value)
			{
				if (overrideToggle.isOn != value.IsOverride)
				{
					UpdateFields(value.IsOverride);
				}
			});
			SetupInternal(wrapper.Value.IsOverride, innerFields);
		}

		public void SetupInternal(bool isOverride, InnerField[] innerFields)
		{
			if (innerFields == null)
			{
				innerFields = Array.Empty<InnerField>();
			}
			this.innerFields = innerFields;
			for (int i = 0; i < this.innerFields.Length; i++)
			{
				this.innerFields[i].Setup();
			}
			overrideToggle.interactable = true;
			UpdateFields(isOverride);
		}

		private void UpdateFields(bool isOverride)
		{
			overrideToggle.SetIsOnWithoutNotify(isOverride);
			for (int i = 0; i < innerFields.Length; i++)
			{
				innerFields[i].SetInteractable(isOverride, labelDisabledColor);
			}
		}

		public void SetupReadOnly(params InnerField[] innerFields)
		{
			wrapper?.UnregisterOnChange(this);
			wrapper = null;
			overrideToggle.SetIsOnWithoutNotify(value: false);
			overrideToggle.interactable = false;
			if (innerFields != null)
			{
				foreach (InnerField innerField in innerFields)
				{
					innerField.SetInteractable(interactable: false, labelDisabledColor);
				}
			}
		}

		private void OverrideToggled(bool isOn)
		{
			UpdateFields(isOn);
			setOverride(isOn);
		}
	}
}
