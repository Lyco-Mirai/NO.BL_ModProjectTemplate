using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FuelGauge : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private Transform fuelReadingPivot;

	[SerializeField]
	private TextMeshProUGUI fuelReading;

	[SerializeField]
	private TextMeshProUGUI fuelLabel;

	[SerializeField]
	private Image fuelBar;

	[SerializeField]
	private Image fuelPointer;

	[SerializeField]
	private Image fuelArc;

	private float lastReading;

	private Gradient redYellowGreenGradient;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		UpdateGradient();
		Show(PlayerSettings.gauges);
		Refresh();
		ThemeManager.ThemeGroupChanged += FuelGauge_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= FuelGauge_OnThemeGroupChanged;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		fuelReading.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		fuelLabel.fontSize = (int)((float)(fontSize - 4) * fontSizeMultiplier);
		Show(PlayerSettings.gauges);
	}

	public override void Refresh()
	{
		if (!(aircraft == null) && !(Time.timeSinceLevelLoad - lastReading < 1f))
		{
			UpdateFuelGauge();
		}
	}

	private void UpdateFuelGauge()
	{
		lastReading = Time.timeSinceLevelLoad;
		float fuelLevel = aircraft.GetFuelLevel();
		fuelReadingPivot.localEulerAngles = new Vector3(0f, 0f, 0f - (fuelLevel * 28f - 14f));
		fuelReading.transform.eulerAngles = new Vector3(0f, 0f, 0f - (SceneSingleton<CameraStateManager>.i.mainCamera.transform.eulerAngles.z - aircraft.cockpit.transform.eulerAngles.z));
		fuelReading.text = (fuelLevel * 100f).ToString("F0") + "%";
		fuelBar.fillAmount = fuelLevel;
		fuelBar.color = redYellowGreenGradient.Evaluate(fuelLevel);
		fuelPointer.color = fuelBar.color;
		fuelLabel.color = fuelBar.color;
		fuelReading.color = fuelBar.color;
	}

	private void UpdateGradient()
	{
		redYellowGreenGradient = ThemeManager.Active.ColorTheme.Gradient(new List<float> { 0f, 0f, 0.5f, 0.5f, 1f, 1f }, new List<float> { 0f, 0.099f, 0.1f, 0.339f, 0.34f, 1f });
	}

	public void Show(bool arg)
	{
		fuelArc.enabled = arg;
		fuelPointer.enabled = arg;
		fuelBar.enabled = arg;
		fuelReading.enabled = arg;
		fuelLabel.enabled = arg;
	}

	private void FuelGauge_OnThemeGroupChanged()
	{
		UpdateGradient();
		UpdateFuelGauge();
	}
}
