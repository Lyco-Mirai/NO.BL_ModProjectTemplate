using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeedGauge : HUDApp
{
	private Aircraft aircraft;

	private AircraftParameters aircraftParameters;

	[SerializeField]
	private AudioClip overspeedVoice;

	[SerializeField]
	private TextMeshProUGUI airspeedDisplay;

	[SerializeField]
	private TextMeshProUGUI overspeedDisplay;

	[SerializeField]
	private Image border;

	[SerializeField]
	private float overspeedThreshold = float.MaxValue;

	private Gradient speedGradient;

	private float lastOverspeed = -100f;

	private void Awake()
	{
		overspeedDisplay.enabled = false;
	}

	public override void Initialize(Aircraft aircraft)
	{
		if (!(aircraft == null))
		{
			this.aircraft = aircraft;
			aircraftParameters = aircraft.definition.aircraftParameters;
			SpeedGauge_OnThemeGroupChanged();
			ThemeManager.ThemeGroupChanged += SpeedGauge_OnThemeGroupChanged;
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		airspeedDisplay.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		float speed = aircraft.speed;
		airspeedDisplay.text = UnitConverter.SpeedReading(speed);
		if (!(speed > overspeedThreshold))
		{
			if (overspeedDisplay.enabled)
			{
				overspeedDisplay.enabled = false;
			}
			airspeedDisplay.color = ((aircraft.gearState == LandingGear.GearState.LockedExtended) ? ThemeManager.Active.ColorTheme.AllClear : speedGradient.Evaluate(speed * 0.5f / Mathf.Max(aircraftParameters.takeoffSpeed, 1f)));
			return;
		}
		overspeedDisplay.enabled = Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f;
		if (Time.timeSinceLevelLoad - lastOverspeed > 20f)
		{
			SoundManager.PlayInterfaceOneShot(overspeedVoice);
		}
		lastOverspeed = Time.timeSinceLevelLoad;
		airspeedDisplay.color = ThemeManager.Active.ColorTheme.Alert;
	}

	private void SpeedGauge_OnThemeGroupChanged()
	{
		speedGradient = ThemeManager.Active.ColorTheme.Gradient(new List<float> { 0.25f, 0.5f, 1f }, new List<float> { 0.05f, 0.525f, 1f });
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= SpeedGauge_OnThemeGroupChanged;
	}
}
