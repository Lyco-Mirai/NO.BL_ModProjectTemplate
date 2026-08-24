using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemStatusDisplay : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private Image systemEngineImg;

	[SerializeField]
	private Image systemFlightControlImg;

	[SerializeField]
	private Image systemRadarImg;

	[SerializeField]
	private Image systemGearImg;

	[SerializeField]
	private Image systemModeImg;

	[SerializeField]
	private Image systemFuelImg;

	[SerializeField]
	private TextMeshProUGUI systemEngineTxt;

	[SerializeField]
	private TextMeshProUGUI systemFlightControlTxt;

	[SerializeField]
	private TextMeshProUGUI systemRadarTxt;

	[SerializeField]
	private TextMeshProUGUI systemGearTxt;

	[SerializeField]
	private TextMeshProUGUI systemModeTxt;

	[SerializeField]
	private TextMeshProUGUI systemFuelTxt;

	public override void Initialize(Aircraft aircraft)
	{
		if (aircraft == null)
		{
			return;
		}
		this.aircraft = aircraft;
		ThemeManager.ThemeGroupChanged += SystemStatusDisplay_OnThemeGroupChanged;
		aircraft.onSetGear += OnSetGear;
		if (aircraft.gearDeployed)
		{
			systemGearImg.color = ThemeManager.Active.ColorTheme.AllClear;
			systemGearTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		}
		else
		{
			systemGearImg.color = Color.grey;
			systemGearTxt.color = Color.grey;
		}
		aircraft.onSetFlightAssist += OnSetFlightAssist;
		OnSetFlightAssist(new Aircraft.OnFlightAssistToggle
		{
			enabled = aircraft.flightAssist
		});
		systemEngineImg.color = ThemeManager.Active.ColorTheme.AllClear;
		systemEngineTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		if (aircraft.radar != null)
		{
			systemRadarTxt.text = "RADAR";
			OnSetRadar();
		}
		else
		{
			systemRadarTxt.text = "OPTICAL";
		}
		OnSetFuel();
		SceneSingleton<HUDOptions>.i.OnApplyOptions += OnHUDMode;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item.TryGetComponent<IEngine>(out var component))
			{
				component.OnEngineDisable += OnEngineDisable;
				component.OnEngineDamage += OnEngineDamage;
			}
		}
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			if (aircraft.radar != null)
			{
				OnSetRadar();
			}
			OnSetFuel();
			OnHUDMode();
		}
	}

	private void SystemStatusDisplay_OnThemeGroupChanged()
	{
		Refresh();
		OnSetFlightAssist(new Aircraft.OnFlightAssistToggle
		{
			enabled = aircraft.flightAssist
		});
		OnSetGear(new Aircraft.OnSetGear
		{
			gearState = aircraft.gearState
		});
		systemEngineImg.color = ThemeManager.Active.ColorTheme.AllClear;
		systemEngineTxt.color = ThemeManager.Active.ColorTheme.AllClear;
	}

	public void OnSetGear(Aircraft.OnSetGear onSetGear)
	{
		if (onSetGear.gearState == LandingGear.GearState.LockedRetracted)
		{
			systemGearImg.color = Color.grey;
			systemGearTxt.color = Color.grey;
		}
		else if (onSetGear.gearState == LandingGear.GearState.Extending || onSetGear.gearState == LandingGear.GearState.Retracting)
		{
			systemGearImg.color = ThemeManager.Active.ColorTheme.Warning;
			systemGearTxt.color = ThemeManager.Active.ColorTheme.Warning;
		}
		else
		{
			systemGearImg.color = ThemeManager.Active.ColorTheme.AllClear;
			systemGearTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		}
	}

	public void OnSetFlightAssist(Aircraft.OnFlightAssistToggle onFlightAssistToggle)
	{
		if (onFlightAssistToggle.enabled)
		{
			systemFlightControlImg.color = ThemeManager.Active.ColorTheme.AllClear;
			systemFlightControlTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		}
		else
		{
			systemFlightControlImg.color = Color.grey;
			systemFlightControlTxt.color = Color.grey;
		}
	}

	public void OnSetRadar()
	{
		if (aircraft.radar.activated)
		{
			systemRadarImg.color = ThemeManager.Active.ColorTheme.AllClear;
			systemRadarTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		}
		else
		{
			systemRadarImg.color = Color.grey;
			systemRadarTxt.color = Color.grey;
		}
	}

	public void OnSetFuel()
	{
		if (aircraft.GetFuelLevel() < 0.1f)
		{
			systemFuelImg.color = ThemeManager.Active.ColorTheme.Alert;
			systemFuelTxt.color = ThemeManager.Active.ColorTheme.Alert;
		}
		else if (aircraft.GetFuelLevel() < 0.2f)
		{
			systemFuelImg.color = Color.Lerp(ThemeManager.Active.ColorTheme.Warning, ThemeManager.Active.ColorTheme.Alert, 0.5f);
			systemFuelTxt.color = Color.Lerp(ThemeManager.Active.ColorTheme.Warning, ThemeManager.Active.ColorTheme.Alert, 0.5f);
		}
		else if (aircraft.GetFuelLevel() < 0.5f)
		{
			systemFuelImg.color = ThemeManager.Active.ColorTheme.Warning;
			systemFuelTxt.color = ThemeManager.Active.ColorTheme.Warning;
		}
		else
		{
			systemFuelImg.color = ThemeManager.Active.ColorTheme.AllClear;
			systemFuelTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		}
	}

	public void OnHUDMode()
	{
		systemModeTxt.text = $"MODE : {SceneSingleton<HUDOptions>.i.currentMode}";
		if (SceneSingleton<HUDOptions>.i.currentMode == HUDOptions.HUDMode.NAV)
		{
			systemModeImg.color = Color.white;
			systemModeTxt.color = Color.white;
		}
		else
		{
			systemModeImg.color = ThemeManager.Active.ColorTheme.AllClear;
			systemModeTxt.color = ThemeManager.Active.ColorTheme.AllClear;
		}
	}

	public void OnEngineDamage()
	{
		systemEngineImg.color = ThemeManager.Active.ColorTheme.Warning;
		systemEngineTxt.color = ThemeManager.Active.ColorTheme.Warning;
	}

	public void OnEngineDisable()
	{
		systemEngineImg.color = ThemeManager.Active.ColorTheme.Alert;
		systemEngineTxt.color = ThemeManager.Active.ColorTheme.Alert;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.TryGetComponent<IEngine>(out var component))
			{
				component.OnEngineDisable -= OnEngineDisable;
				component.OnEngineDamage -= OnEngineDamage;
			}
		}
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= SystemStatusDisplay_OnThemeGroupChanged;
		if (SceneSingleton<HUDOptions>.i != null)
		{
			SceneSingleton<HUDOptions>.i.OnApplyOptions -= OnHUDMode;
		}
		if (!(aircraft != null))
		{
			return;
		}
		aircraft.onSetGear -= OnSetGear;
		aircraft.onSetFlightAssist -= OnSetFlightAssist;
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.TryGetComponent<IEngine>(out var component))
			{
				component.OnEngineDisable -= OnEngineDisable;
				component.OnEngineDamage -= OnEngineDamage;
			}
		}
	}
}
