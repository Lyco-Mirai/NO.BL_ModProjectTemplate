using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorSettingsMenu : MonoBehaviour
	{
		[SerializeField]
		private Slider intervalSlider;

		[SerializeField]
		private TextMeshProUGUI intervalSliderText;

		[SerializeField]
		private Slider maxSavesSlider;

		[SerializeField]
		private TextMeshProUGUI maxSavesSliderText;

		[SerializeField]
		private Slider retentionSlider;

		[SerializeField]
		private TextMeshProUGUI retentionSliderText;

		private MissionAutoSaveSettings settings;

		private void Start()
		{
			settings = MissionAutoSaveSettings.GetOrLoad();
			intervalSlider.minValue = 1f;
			intervalSlider.maxValue = 60f;
			intervalSlider.wholeNumbers = true;
			intervalSlider.SetValueWithoutNotify(settings.IntervalMinutes);
			intervalSliderText.text = $"{settings.IntervalMinutes:0} minutes";
			intervalSlider.onValueChanged.AddListener(OnIntervalChanged);
			maxSavesSlider.minValue = 1f;
			maxSavesSlider.maxValue = 20f;
			maxSavesSlider.wholeNumbers = true;
			maxSavesSlider.SetValueWithoutNotify(settings.MaxAutoSaves);
			maxSavesSliderText.text = $"{settings.MaxAutoSaves} saves";
			maxSavesSlider.onValueChanged.AddListener(OnMaxSavesChanged);
			retentionSlider.minValue = 1f;
			retentionSlider.maxValue = 365f;
			retentionSlider.wholeNumbers = true;
			retentionSlider.SetValueWithoutNotify(settings.RetentionDays);
			retentionSliderText.text = $"{settings.RetentionDays} days";
			retentionSlider.onValueChanged.AddListener(OnRetentionChanged);
		}

		private void OnIntervalChanged(float value)
		{
			settings.IntervalMinutes = value;
			intervalSliderText.text = $"{settings.IntervalMinutes:0} minutes";
			settings.Save();
		}

		private void OnMaxSavesChanged(float value)
		{
			settings.MaxAutoSaves = (int)value;
			maxSavesSliderText.text = $"{settings.MaxAutoSaves} saves";
			settings.Save();
		}

		private void OnRetentionChanged(float value)
		{
			settings.RetentionDays = (int)value;
			retentionSliderText.text = $"{settings.RetentionDays} days";
			settings.Save();
		}
	}
}
