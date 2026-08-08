using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class ColorPicker : MonoBehaviour
	{
		[Header("Color Name")]
		[SerializeField]
		private TextMeshProUGUI colorName;

		[Header("Image")]
		[SerializeField]
		private Image colorPreviewImage;

		[Header("Sliders")]
		[SerializeField]
		private Slider hueSlider;

		[SerializeField]
		private Image hueSliderBackground;

		[SerializeField]
		private TMP_InputField hueValue;

		[SerializeField]
		private Slider saturationSlider;

		[SerializeField]
		private Image saturationSliderBackground;

		[SerializeField]
		private TMP_InputField saturationValue;

		[SerializeField]
		private Slider valueSlider;

		[SerializeField]
		private Image valueSliderBackground;

		[SerializeField]
		private TMP_InputField valueValue;

		[Header("Hex Input")]
		[SerializeField]
		private TMP_InputField hexInputField;

		private (Slider slider, TMP_InputField inputField, Image background)[] sliders;

		private Texture2D hueGradientTexture;

		private Texture2D saturationGradientTexture;

		private Texture2D valueGradientTexture;

		private string tooltipText;

		public Color Color { get; private set; }

		public event Action<Color> OnColorChanged;

		public event Action<string> OnColorNameHoverIn;

		public event Action<string> OnColorNameHoverOut;

		private void Awake()
		{
			sliders = new(Slider, TMP_InputField, Image)[3]
			{
				(hueSlider, hueValue, hueSliderBackground),
				(saturationSlider, saturationValue, saturationSliderBackground),
				(valueSlider, valueValue, valueSliderBackground)
			};
			(Slider, TMP_InputField, Image)[] array = sliders;
			for (int i = 0; i < array.Length; i++)
			{
				var (slider, tMP_InputField, _) = array[i];
				slider.onValueChanged.AddListener(UpdateFromSliderValue);
				tMP_InputField.onEndEdit.AddListener(UpdateFromSliderInputField);
			}
			hexInputField.onEndEdit.AddListener(UpdateFromHexInputField);
			InitializeHueGradient();
			InitializeSaturationGradient();
			InitializeValueGradient();
		}

		public void OnPointerEnter()
		{
			this.OnColorNameHoverIn?.Invoke(tooltipText);
		}

		public void OnPointerExit()
		{
			this.OnColorNameHoverOut?.Invoke(tooltipText);
		}

		public void SetValues(string text, Color color, string tooltip)
		{
			colorName.text = text;
			tooltipText = tooltip;
			Color = new Color(color.r, color.g, color.b, 1f);
			Color.RGBToHSV(color, out var H, out var S, out var V);
			hueSlider.SetValueWithoutNotify(H * 255f);
			saturationSlider.SetValueWithoutNotify(S * 255f);
			valueSlider.SetValueWithoutNotify(V * 255f);
			hueValue.text = (H * 255f).ToString("N0");
			saturationValue.text = (S * 255f).ToString("N0");
			valueValue.text = (V * 255f).ToString("N0");
			hexInputField.text = ColorUtility.ToHtmlStringRGB(color);
			UpdateColorPreview(invoke: false);
		}

		private void UpdateFromSliderValue(float value)
		{
			Color = Color.HSVToRGB(hueSlider.value / 255f, saturationSlider.value / 255f, valueSlider.value / 255f);
			hueValue.text = hueSlider.value.ToString("N0");
			saturationValue.text = saturationSlider.value.ToString("N0");
			valueValue.text = valueSlider.value.ToString("N0");
			hexInputField.text = ColorUtility.ToHtmlStringRGB(Color);
			UpdateColorPreview();
		}

		private void UpdateFromSliderInputField(string input)
		{
			if (!string.IsNullOrWhiteSpace(input))
			{
				if (!float.TryParse(input, out var result) || result < 0f || result > 255f)
				{
					hueValue.text = hueSlider.value.ToString("N0");
					saturationValue.text = saturationSlider.value.ToString("N0");
					valueValue.text = valueSlider.value.ToString("N0");
					return;
				}
				Color = Color.HSVToRGB(float.Parse(hueValue.text) / 255f, float.Parse(saturationValue.text) / 255f, float.Parse(valueValue.text) / 255f);
				hueSlider.SetValueWithoutNotify(float.Parse(hueValue.text));
				saturationSlider.SetValueWithoutNotify(float.Parse(saturationValue.text));
				valueSlider.SetValueWithoutNotify(float.Parse(valueValue.text));
				hexInputField.text = ColorUtility.ToHtmlStringRGB(Color);
				UpdateColorPreview();
			}
		}

		private void UpdateFromHexInputField(string hexInput)
		{
			if (hexInput.StartsWith("#"))
			{
				string text = hexInput;
				hexInput = text.Substring(1, text.Length - 1);
			}
			if (ColorUtility.TryParseHtmlString("#" + hexInput, out var color))
			{
				Color = color;
				Color.RGBToHSV(Color, out var H, out var S, out var V);
				hueSlider.SetValueWithoutNotify(H * 255f);
				saturationSlider.SetValueWithoutNotify(S * 255f);
				valueSlider.SetValueWithoutNotify(V * 255f);
				hueValue.text = (H * 255f).ToString("N0");
				saturationValue.text = (S * 255f).ToString("N0");
				valueValue.text = (V * 255f).ToString("N0");
				UpdateColorPreview();
			}
		}

		private void UpdateColorPreview(bool invoke = true)
		{
			colorPreviewImage.color = Color;
			UpdateHueGradient();
			UpdateSaturationGradient();
			UpdateValueGradient();
			if (invoke)
			{
				this.OnColorChanged?.Invoke(Color);
			}
		}

		private void InitializeHueGradient()
		{
			hueGradientTexture = new Texture2D(256, 1, TextureFormat.RGB24, mipChain: false);
			UpdateHueGradient();
		}

		private void InitializeSaturationGradient()
		{
			saturationGradientTexture = new Texture2D(256, 1, TextureFormat.RGB24, mipChain: false);
			UpdateSaturationGradient();
		}

		private void InitializeValueGradient()
		{
			valueGradientTexture = new Texture2D(256, 1, TextureFormat.RGB24, mipChain: false);
			UpdateValueGradient();
		}

		private void UpdateHueGradient()
		{
			float s = saturationSlider.value / 255f;
			float v = valueSlider.value / 255f;
			for (int i = 0; i < 256; i++)
			{
				hueGradientTexture.SetPixel(i, 0, Color.HSVToRGB((float)i / 255f, s, v));
			}
			hueGradientTexture.Apply();
			hueSliderBackground.sprite = Sprite.Create(hueGradientTexture, new Rect(0f, 0f, 256f, 1f), new Vector2(0.5f, 0.5f));
		}

		private void UpdateSaturationGradient()
		{
			float h = hueSlider.value / 255f;
			float v = valueSlider.value / 255f;
			for (int i = 0; i < 256; i++)
			{
				saturationGradientTexture.SetPixel(i, 0, Color.HSVToRGB(h, (float)i / 255f, v));
			}
			saturationGradientTexture.Apply();
			saturationSliderBackground.sprite = Sprite.Create(saturationGradientTexture, new Rect(0f, 0f, 256f, 1f), new Vector2(0.5f, 0.5f));
		}

		private void UpdateValueGradient()
		{
			float h = hueSlider.value / 255f;
			float s = saturationSlider.value / 255f;
			for (int i = 0; i < 256; i++)
			{
				valueGradientTexture.SetPixel(i, 0, Color.HSVToRGB(h, s, (float)i / 255f));
			}
			valueGradientTexture.Apply();
			valueSliderBackground.sprite = Sprite.Create(valueGradientTexture, new Rect(0f, 0f, 256f, 1f), new Vector2(0.5f, 0.5f));
		}
	}
}
