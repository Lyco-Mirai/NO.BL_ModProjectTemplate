using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class SliderText : MonoBehaviour
	{
		[SerializeField]
		private Slider slider;

		[SerializeField]
		private TextMeshProUGUI text;

		[Tooltip("use {0} to insert value")]
		[SerializeField]
		private string format;

		private void Awake()
		{
			slider.onValueChanged.AddListener(SetText);
			SetText(slider.value);
		}

		private void OnValidate()
		{
			if (slider != null && text != null)
			{
				SetText(slider.value);
			}
		}

		private void SetText(float value)
		{
			string text = ((!string.IsNullOrEmpty(format)) ? string.Format(format, value) : value.ToString("0.0"));
			this.text.text = text;
		}
	}
}
