using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class EnvironmentTab : MonoBehaviour, IMissionTab
	{
		[SerializeField]
		private Slider timeSlider;

		[SerializeField]
		private Slider timeFactorSlider;

		[SerializeField]
		private Slider conditionsSlider;

		[SerializeField]
		private Slider cloudHeightSlider;

		[SerializeField]
		private Slider windSpeedSlider;

		[SerializeField]
		private Slider windTurbulenceSlider;

		[SerializeField]
		private Slider windRandomDirectionSlider;

		[SerializeField]
		private Slider moonPhaseSlider;

		[SerializeField]
		private RadialSlider windDirectionRadialSlider;

		[SerializeField]
		private TextMeshProUGUI timeLabel;

		[SerializeField]
		private TextMeshProUGUI timeFactorLabel;

		[SerializeField]
		private TextMeshProUGUI conditionsLabel;

		[SerializeField]
		private TextMeshProUGUI cloudHeightLabel;

		[SerializeField]
		private TextMeshProUGUI windSpeedLabel;

		[SerializeField]
		private TextMeshProUGUI windTurbulenceLabel;

		[SerializeField]
		private TextMeshProUGUI windHeadingLabel;

		[SerializeField]
		private TextMeshProUGUI windRandomHeadingLabel;

		[SerializeField]
		private TextMeshProUGUI moonPhaseLabel;

		[SerializeField]
		private WeatherSet[] weatherSets;

		[SerializeField]
		private Transform windArrow;

		[SerializeField]
		private Image windRandomArc;

		[SerializeField]
		private Image moonPhaseImage;

		private MissionEnvironment environment;

		public void SetMission(Mission mission)
		{
			environment = mission.environment;
			timeSlider.SetValueWithoutNotify(environment.timeOfDay / 24f);
			timeFactorSlider.SetValueWithoutNotify(environment.timeFactor);
			conditionsSlider.SetValueWithoutNotify(environment.weatherIntensity);
			cloudHeightSlider.SetValueWithoutNotify(Mathf.Max(environment.cloudAltitude, 500f));
			windSpeedSlider.SetValueWithoutNotify(environment.windSpeed);
			windTurbulenceSlider.SetValueWithoutNotify(environment.windTurbulence);
			windDirectionRadialSlider.SetValue(environment.windHeading);
			windRandomDirectionSlider.SetValueWithoutNotify(environment.windRandomHeading);
			moonPhaseSlider.SetValueWithoutNotify(environment.moonPhase);
			UpdateLabels();
		}

		public void ValuesChanged()
		{
			environment.timeOfDay = timeSlider.value * 24f;
			environment.timeFactor = timeFactorSlider.value;
			environment.weatherIntensity = conditionsSlider.value;
			environment.cloudAltitude = cloudHeightSlider.value;
			environment.windSpeed = windSpeedSlider.value;
			environment.windTurbulence = windTurbulenceSlider.value;
			environment.windHeading = windDirectionRadialSlider.value;
			environment.windRandomHeading = windRandomDirectionSlider.value;
			environment.moonPhase = moonPhaseSlider.value;
			if (NetworkSceneSingleton<LevelInfo>.i != null)
			{
				NetworkSceneSingleton<LevelInfo>.i.LoadEnvironment(environment);
			}
			UpdateLabels();
		}

		public void UpdateLabels()
		{
			timeLabel.text = UnitConverter.TimeOfDay(timeSlider.value * 24f, includeSeconds: false);
			float num = 1f;
			float value = timeFactorSlider.value;
			if (value <= 0f)
			{
				if (value != -2f)
				{
					if (value != -1f)
					{
						if (value == 0f)
						{
							num = 1f;
						}
					}
					else
					{
						num = 0.5f;
					}
				}
				else
				{
					num = 0f;
				}
			}
			else if (value != 1f)
			{
				if (value != 2f)
				{
					if (value == 3f)
					{
						num = 60f;
					}
				}
				else
				{
					num = 30f;
				}
			}
			else
			{
				num = 10f;
			}
			timeFactorLabel.text = ((timeFactorSlider.value == -1f) ? $"{num:F1}x" : $"{num:F0}x");
			int value2 = Mathf.FloorToInt(environment.weatherIntensity * (float)weatherSets.Length);
			value2 = Mathf.Clamp(value2, 0, weatherSets.Length - 1);
			conditionsLabel.text = weatherSets[value2].displayName;
			cloudHeightLabel.text = UnitConverter.AltitudeReading(environment.cloudAltitude);
			windSpeedLabel.text = UnitConverter.SpeedReading(environment.windSpeed);
			windTurbulenceLabel.text = $"{environment.windTurbulence * 100f:F0}%";
			windHeadingLabel.text = $"{environment.windHeading:F0}";
			windRandomHeadingLabel.text = $"{environment.windRandomHeading:F0}°";
			windRandomArc.fillAmount = 2f * environment.windRandomHeading / 360f;
			windRandomArc.transform.localEulerAngles = new Vector3(0f, 0f, environment.windRandomHeading);
			moonPhaseImage.enabled = true;
			if (moonPhaseSlider.value < 0f)
			{
				moonPhaseLabel.text = "No Moon";
				moonPhaseImage.enabled = false;
			}
			else if ((moonPhaseSlider.value >= 0f && moonPhaseSlider.value <= 2f) || (moonPhaseSlider.value > 26f && moonPhaseSlider.value <= 28f))
			{
				moonPhaseLabel.text = "New Moon";
			}
			else if (moonPhaseSlider.value > 2f && moonPhaseSlider.value <= 6f)
			{
				moonPhaseLabel.text = "First Crescent";
			}
			else if (moonPhaseSlider.value > 6f && moonPhaseSlider.value <= 11f)
			{
				moonPhaseLabel.text = "First Quarter";
			}
			else if (moonPhaseSlider.value > 11f && moonPhaseSlider.value <= 17f)
			{
				moonPhaseLabel.text = "Full Moon";
			}
			else if (moonPhaseSlider.value > 17f && moonPhaseSlider.value <= 22f)
			{
				moonPhaseLabel.text = "Last Quarter";
			}
			else if (moonPhaseSlider.value > 22f && moonPhaseSlider.value <= 26f)
			{
				moonPhaseLabel.text = "Last Crescent";
			}
		}
	}
}
