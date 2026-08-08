using System;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusGauges : HUDApp
{
	[Serializable]
	private class Gauge
	{
		[SerializeField]
		private TextMeshProUGUI title;

		[SerializeField]
		private Image image;

		[SerializeField]
		private Image circle;

		[SerializeField]
		private TextMeshProUGUI reading;

		[SerializeField]
		private Transform needle;

		[SerializeField]
		private float maxValue;

		public void Update(float value)
		{
			if (!(reading == null))
			{
				reading.text = value.ToString("F1");
				needle.transform.localEulerAngles = Vector3.forward * (value / maxValue) * -300f;
				circle.fillAmount = 0.84f * value / maxValue;
				float num = Mathf.Clamp01(value / maxValue);
				circle.color = redToGreenGradient.Evaluate(1f - num);
			}
		}
	}

	private Aircraft aircraft;

	[SerializeField]
	private Image fuelLevelDisplay;

	[SerializeField]
	private Image throttleLevelDisplay;

	[SerializeField]
	private Gauge irLevelGauge;

	[SerializeField]
	private TextMeshProUGUI massValue;

	[SerializeField]
	private TextMeshProUGUI twrValue;

	private IRSource irSource;

	private ControlInputs inputs;

	private float lastRefresh;

	private float refreshDelay = 10f;

	private float gaugeThickness = 25f;

	private static Gradient redToGreenGradient;

	public override void Initialize(Aircraft aircraft)
	{
		if (!(aircraft == null))
		{
			this.aircraft = aircraft;
			irSource = aircraft.GetIRSource();
			inputs = aircraft.GetInputs();
			StatusGauges_OnThemeGroupChanged();
			float fuelLevel = aircraft.GetFuelLevel();
			fuelLevelDisplay.rectTransform.sizeDelta = new Vector2(gaugeThickness, 200f * fuelLevel);
			fuelLevelDisplay.color = redToGreenGradient.Evaluate(fuelLevel);
			float mass = aircraft.GetMass();
			massValue.text = UnitConverter.WeightReading(mass);
		}
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		float value = Mathf.Clamp(irSource.intensity, 0f, 12f);
		irLevelGauge.Update(value);
		throttleLevelDisplay.rectTransform.sizeDelta = new Vector2(gaugeThickness, 200f * inputs.throttle);
		if (Time.timeSinceLevelLoad > lastRefresh + refreshDelay)
		{
			float fuelLevel = aircraft.GetFuelLevel();
			fuelLevelDisplay.rectTransform.sizeDelta = new Vector2(gaugeThickness, 200f * fuelLevel);
			fuelLevelDisplay.color = redToGreenGradient.Evaluate(fuelLevel);
			float mass = aircraft.GetMass();
			massValue.text = UnitConverter.WeightReading(mass);
			float maxThrust;
			if (aircraft.GetMaxPower(out var maxPower))
			{
				twrValue.text = UnitConverter.PowerToWeightReading(maxPower * 0.001f / mass);
			}
			else if (aircraft.GetMaxThrust(out maxThrust))
			{
				twrValue.text = $"{maxThrust / (mass * 9.81f):F2}";
			}
			lastRefresh = Time.timeSinceLevelLoad;
		}
	}

	private void StatusGauges_OnThemeGroupChanged()
	{
		redToGreenGradient = ThemeManager.Active.ColorTheme.Gradient();
	}

	private void OnEnable()
	{
		ThemeManager.ThemeGroupChanged += StatusGauges_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= StatusGauges_OnThemeGroupChanged;
	}
}
