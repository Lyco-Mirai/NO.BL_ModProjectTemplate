using System;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class FloatDataField : DataField, IDataField<float>
	{
		[Serializable]
		public struct IntSlider
		{
			public int Min;

			public int Max;

			public IntSlider(int min, int max)
			{
				Min = min;
				Max = max;
			}

			public static explicit operator FloatSlider?(IntSlider? v)
			{
				if (!v.HasValue)
				{
					return null;
				}
				return v.Value;
			}

			public static implicit operator FloatSlider(IntSlider v)
			{
				return new FloatSlider
				{
					Min = v.Min,
					Max = v.Max
				};
			}
		}

		[Serializable]
		public struct IntSettings
		{
			public IntSlider? Slider;

			public int? Steps;

			public string TextFormat;

			public static explicit operator DataSettings?(IntSettings? v)
			{
				if (!v.HasValue)
				{
					return null;
				}
				return v.Value;
			}

			public static implicit operator DataSettings(IntSettings v)
			{
				return new DataSettings
				{
					Slider = (FloatSlider?)v.Slider,
					WholeNumber = true,
					Steps = (v.Steps ?? 1),
					TextFormat = v.TextFormat
				};
			}
		}

		[Serializable]
		public struct FloatSlider
		{
			public float Min;

			public float Max;

			public FloatSlider(float min, float max)
			{
				Min = min;
				Max = max;
			}
		}

		[Serializable]
		public struct FloatSettings
		{
			public FloatSlider? Slider;

			public float? Steps;

			public string TextFormat;

			public static explicit operator DataSettings?(FloatSettings? v)
			{
				if (!v.HasValue)
				{
					return null;
				}
				return v.Value;
			}

			public static implicit operator DataSettings(FloatSettings v)
			{
				return new DataSettings
				{
					Slider = v.Slider,
					WholeNumber = false,
					Steps = v.Steps,
					TextFormat = v.TextFormat
				};
			}
		}

		[Serializable]
		public struct DataSettings
		{
			public FloatSlider? Slider;

			public float? Steps;

			public bool WholeNumber;

			public string TextFormat;
		}

		[SerializeField]
		private TMP_InputField text;

		[SerializeField]
		private Slider slider;

		[SerializeField]
		private float textWidthNoSlider = 240f;

		[SerializeField]
		private float textWidthWithSlider = 80f;

		private IValueWrapper<float> wrapper;

		private bool useSlider;

		private float? valueSteps;

		public string TextFormat;

		private bool suppressUiCallbacks;

		protected override void SetFieldInteractable(bool value)
		{
			text.interactable = value;
			slider.interactable = value;
		}

		protected override void AwakeSetup()
		{
			text.onValueChanged.AddListener(Text_OnValueChanged);
			slider.onValueChanged.AddListener(Slider_OnValueChanged);
		}

		public void Setup(string label, ValueWrapperInt wrapper, IntSettings? settings = null)
		{
			Setup(label, wrapper, (DataSettings?)settings);
		}

		void IDataField<float>.Setup(string label, IValueWrapper<float> wrapper)
		{
			Setup(label, wrapper);
		}

		public void Setup(string label, IValueWrapper<float> wrapper, FloatSettings? settings = null)
		{
			Setup(label, wrapper, (DataSettings?)settings);
		}

		public void Setup(string label, IValueWrapper<float> wrapper, DataSettings? settings)
		{
			this.wrapper?.UnregisterOnChange(this);
			this.wrapper = wrapper;
			wrapper.RegisterOnChange(this, Wrapper_OnChange);
			SetupFields(label, settings);
			UpdateFields(wrapper.Value);
			base.Interactable = true;
		}

		private void OnDestroy()
		{
			wrapper?.UnregisterOnChange(this);
		}

		public void SetupReadOnly(string label, int value, IntSettings? settings = null)
		{
			SetupReadOnly(label, value, (DataSettings?)settings);
		}

		public void SetupReadOnly(string label, float value, FloatSettings? settings = null)
		{
			SetupReadOnly(label, value, (DataSettings?)settings);
		}

		public void SetupReadOnly(string label, float value, DataSettings? settings)
		{
			wrapper?.UnregisterOnChange(this);
			wrapper = null;
			SetupFields(label, settings);
			UpdateFields(value);
			slider.interactable = false;
		}

		private void SetupFields(string label, DataSettings? settingsNullable)
		{
			base.label.text = label;
			DataSettings valueOrDefault = settingsNullable.GetValueOrDefault();
			SetContentType(valueOrDefault.WholeNumber);
			valueSteps = valueOrDefault.Steps;
			TextFormat = valueOrDefault.TextFormat;
			if (valueOrDefault.Slider.HasValue)
			{
				SetSliderSettingsInternal(valueOrDefault.WholeNumber, valueOrDefault.Slider.Value);
				return;
			}
			useSlider = false;
			RectTransform obj = (RectTransform)text.transform;
			Vector2 sizeDelta = obj.sizeDelta;
			slider.gameObject.SetActive(value: false);
			sizeDelta.x = textWidthNoSlider;
			obj.sizeDelta = sizeDelta;
		}

		public void SetContentType(bool wholeNumbers)
		{
			text.contentType = (wholeNumbers ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber);
		}

		public void SetSteps(float? steps)
		{
			valueSteps = steps;
		}

		public void SetSliderSettings(IntSlider sliderSettings)
		{
			SetSliderSettingsInternal(wholeNumbers: true, sliderSettings);
		}

		public void SetSliderSettings(FloatSlider sliderSettings)
		{
			SetSliderSettingsInternal(wholeNumbers: false, sliderSettings);
		}

		private void SetSliderSettingsInternal(bool wholeNumbers, FloatSlider sliderSettings)
		{
			suppressUiCallbacks = true;
			useSlider = true;
			slider.gameObject.SetActive(value: true);
			slider.wholeNumbers = wholeNumbers;
			slider.minValue = sliderSettings.Min;
			slider.maxValue = sliderSettings.Max;
			RectTransform obj = (RectTransform)text.transform;
			Vector2 sizeDelta = obj.sizeDelta;
			sizeDelta.x = textWidthWithSlider;
			obj.sizeDelta = sizeDelta;
			if (float.TryParse(text.text, out var result))
			{
				slider.SetValueWithoutNotify(result);
			}
			suppressUiCallbacks = false;
		}

		private void Wrapper_OnChange(float newValue)
		{
			UpdateFields(newValue);
		}

		private void UpdateFields(float value)
		{
			if (useSlider)
			{
				slider.SetValueWithoutNotify(value);
			}
			string textWithoutNotify = ((TextFormat != null) ? value.ToString(TextFormat) : value.ToString());
			text.SetTextWithoutNotify(textWithoutNotify);
		}

		private void Slider_OnValueChanged(float value)
		{
			if (!suppressUiCallbacks)
			{
				value = RoundToStep(value);
				UpdateFields(value);
				wrapper.SetValue(value, this);
			}
		}

		private float RoundToStep(float value)
		{
			if (valueSteps.HasValue)
			{
				float value2 = valueSteps.Value;
				value /= value2;
				value = Mathf.Round(value);
				value *= value2;
			}
			return value;
		}

		private void Text_OnValueChanged(string stringValue)
		{
			if (float.TryParse(text.text.Replace("%", "").Trim(), out var result))
			{
				result *= ((TextFormat == "P1") ? 0.01f : 1f);
				result = RoundToStep(result);
				if (useSlider)
				{
					result = Mathf.Clamp(result, slider.minValue, slider.maxValue);
				}
				UpdateFields(result);
				wrapper.SetValue(result, this);
			}
		}
	}
}
