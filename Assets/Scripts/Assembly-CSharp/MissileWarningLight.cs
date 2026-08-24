using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class MissileWarningLight : HUDApp
{
	[SerializeField]
	private Image[] images;

	private List<Missile> knownMissiles;

	private MissileWarning missileWarning;

	public override void Initialize(Aircraft aircraft)
	{
		base.enabled = false;
		missileWarning = aircraft.GetMissileWarningSystem();
		knownMissiles = missileWarning.knownMissiles;
		missileWarning.onMissileWarning += MissileWarningLights_OnMissileWarning;
		aircraft.onDisableUnit += MissileWarningLights_OnDisable;
		ThemeManager.ThemeGroupChanged += MissileWarningLights_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= MissileWarningLights_OnThemeGroupChanged;
	}

	private void MissileWarningLights_OnThemeGroupChanged()
	{
		Image[] array = images;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].color = ThemeManager.Active.ColorTheme.Alert;
		}
	}

	private void MissileWarningLights_OnMissileWarning(MissileWarning.OnMissileWarning obj)
	{
		base.enabled = true;
	}

	private void MissileWarningLights_OnDisable(Unit unit)
	{
		missileWarning.onMissileWarning -= MissileWarningLights_OnMissileWarning;
		base.enabled = false;
	}

	public override void Refresh()
	{
		if (knownMissiles.Count > 0)
		{
			bool flag = Mathf.Sin(Time.timeSinceLevelLoad * 20f) > 0f;
			Image[] array = images;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = flag;
			}
		}
		else
		{
			base.enabled = false;
			Image[] array = images;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
	}
}
