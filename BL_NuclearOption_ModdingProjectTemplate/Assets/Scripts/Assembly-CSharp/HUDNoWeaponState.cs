using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using UnityEngine;
using UnityEngine.UI;

public class HUDNoWeaponState : HUDWeaponState
{
	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		SceneSingleton<FlightHud>.i.velocityVector.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.color = Color.Lerp(Color.black, ThemeManager.Active.ColorTheme.AllClear, Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.GetHUDCenter().position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.enabled = !aircraft.gearDeployed;
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		SceneSingleton<FlightHud>.i.waterline.enabled = true;
		targetDesignator.transform.localScale = Vector3.one;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
	}
}
