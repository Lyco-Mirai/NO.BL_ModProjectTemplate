using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDTurretState : HUDWeaponState
{
	private enum HintState
	{
		Shoot = 0,
		Neutral = 1,
		OutOfRange = 2
	}

	private HintState hintState = HintState.Neutral;

	[SerializeField]
	private TextMeshProUGUI hint;

	[SerializeField]
	private Transform boresight;

	[SerializeField]
	private GameObject turretCrosshairPrefab;

	private HUDTurretCrosshair[] crosshairs;

	private Image targetDesignator;

	private WeaponStation weaponStation;

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		SceneSingleton<FlightHud>.i.velocityVector.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, boresight.position) * 0.015f - 0.15f));
		float num = float.MaxValue;
		HUDTurretCrosshair[] array = crosshairs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Refresh(Camera.main, out var crosshairPosition);
			num = Mathf.Min(num, Vector3.Distance(crosshairPosition, targetDesignator.transform.position));
		}
		HintState hintState = this.hintState;
		if (targetList.Count > 0 && !targetList[0].disabled && aircraft.NetworkHQ.TryGetKnownPosition(targetList[0], out var knownPosition))
		{
			float num2 = FastMath.Distance(knownPosition, aircraft.GlobalPosition());
			if (num2 > weaponInfo.targetRequirements.maxRange)
			{
				hintState = HintState.OutOfRange;
			}
			if (num2 < weaponInfo.targetRequirements.maxRange * 0.5f)
			{
				hintState = HintState.Shoot;
			}
		}
		else
		{
			hintState = HintState.Neutral;
		}
		if (hintState != this.hintState)
		{
			this.hintState = hintState;
			UpdateHint();
		}
		targetDesignator.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(num * 0.01f - 0.3f));
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		weaponInfo = weaponStation.WeaponInfo;
		this.targetDesignator = targetDesignator;
		targetDesignator.transform.localScale = Vector3.one;
		SceneSingleton<FlightHud>.i.waterline.enabled = true;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
		crosshairs = new HUDTurretCrosshair[weaponStation.Turrets.Count];
		for (int i = 0; i < crosshairs.Length; i++)
		{
			crosshairs[i] = Object.Instantiate(turretCrosshairPrefab, base.transform).GetComponent<HUDTurretCrosshair>();
			crosshairs[i].Initialize(weaponStation.Turrets[i]);
		}
	}

	private void UpdateHint()
	{
		switch (hintState)
		{
		case HintState.Shoot:
			hint.text = "SHOOT";
			break;
		case HintState.Neutral:
			hint.text = "";
			break;
		case HintState.OutOfRange:
			hint.text = "OUT OF RANGE";
			break;
		}
	}
}
