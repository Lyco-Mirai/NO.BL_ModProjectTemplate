using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDLaserGuidedState : HUDWeaponState
{
	private bool hidden;

	private bool allRequirementsMet;

	private float targetDist;

	private float minRange;

	private float maxRange;

	private float rangeRatio;

	private float maxRangeSmoothed;

	private float maxRangeSmoothingVel;

	private float minAlignment;

	private float currentMaxArc;

	private float lastWeaponRangeCalc;

	private float lastTextDisplay;

	private float fovPrev;

	private float currentMaxArcPrev;

	private GlobalPosition knownPos;

	[SerializeField]
	private Image outerCircle;

	[SerializeField]
	private Image innerCircle;

	[SerializeField]
	private Image noShoot;

	[SerializeField]
	private TextMeshProUGUI maxRangeText;

	[SerializeField]
	private TextMeshProUGUI hint;

	[SerializeField]
	private Transform textAnchor;

	private Camera cam;

	private Aircraft aircraft;

	private WeaponStation weaponStation;

	private Missile prefabMissile;

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		SceneSingleton<FlightHud>.i.velocityVector.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.color = Color.Lerp(Color.black, ThemeManager.Active.ColorTheme.AllClear, Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.GetHUDCenter().position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.enabled = !aircraft.gearDeployed;
		maxRangeSmoothed = Mathf.SmoothDamp(maxRangeSmoothed, maxRange, ref maxRangeSmoothingVel, 1f, 1000f);
		rangeRatio = maxRangeSmoothed / targetDist;
		targetDist = FastMath.Distance(knownPos, aircraft.GlobalPosition());
		innerCircle.fillAmount = rangeRatio;
		currentMaxArc = Mathf.Min(minAlignment, Mathf.Max(targetDist, minRange) * 0.002f);
		if (cam.fieldOfView != fovPrev || currentMaxArcPrev != currentMaxArc)
		{
			fovPrev = cam.fieldOfView;
			currentMaxArcPrev = currentMaxArc;
			outerCircle.transform.localScale = 50f / fovPrev * (currentMaxArc / 8f) * Vector3.one;
			hint.transform.position = textAnchor.position;
			maxRangeText.transform.position = textAnchor.position;
		}
		CalcWeaponRange();
		DisplayText();
	}

	private void Awake()
	{
		innerCircle.color = ThemeManager.Active.ColorTheme.AllClear;
		outerCircle.color = ThemeManager.Active.ColorTheme.AllClear;
	}

	private void CalcWeaponRange()
	{
		if (!(Time.timeSinceLevelLoad - lastWeaponRangeCalc < 1f) && targetList.Count != 0)
		{
			lastWeaponRangeCalc = Time.timeSinceLevelLoad;
			aircraft.NetworkHQ.TryGetKnownPosition(targetList[0], out knownPos);
			maxRange = prefabMissile.CalcRange(aircraft.speed, aircraft.GlobalPosition().y, knownPos.y, targetDist, 0f, out var _);
		}
	}

	private void DisplayText()
	{
		if (Time.timeSinceLevelLoad - lastTextDisplay < 0.1f || (hidden && targetList.Count == 0))
		{
			return;
		}
		lastTextDisplay = Time.timeSinceLevelLoad;
		if (targetList.Count == 0 || weaponStation.Ammo == 0)
		{
			HideAll();
			return;
		}
		ShowAll();
		maxRangeText.text = "MAX " + UnitConverter.DistanceReading(maxRangeSmoothed);
		maxRangeText.enabled = rangeRatio < 2f;
		Vector3 to = knownPos - aircraft.GlobalPosition();
		float num = Vector3.Angle(aircraft.transform.forward, to);
		bool flag = aircraft.NetworkHQ.IsTargetLased(targetList[0]);
		bool num2 = allRequirementsMet;
		allRequirementsMet = false;
		if (targetDist > maxRangeSmoothed)
		{
			hint.text = "OUT OF RANGE";
		}
		else if (num > currentMaxArc)
		{
			hint.text = "OUT OF ARC";
		}
		else if (!flag)
		{
			hint.text = "NOT LASED";
		}
		else if (targetDist < minRange)
		{
			hint.text = "TOO CLOSE";
		}
		else
		{
			allRequirementsMet = true;
			hint.text = "SHOOT";
		}
		if (num2 != allRequirementsMet)
		{
			noShoot.enabled = !allRequirementsMet;
			outerCircle.color = (allRequirementsMet ? Color.black : (Color.white * 0.5f));
			innerCircle.color = (allRequirementsMet ? ThemeManager.Active.ColorTheme.AllClear : (ThemeManager.Active.ColorTheme.AllClear * 0.5f + ThemeManager.Active.ColorTheme.Alert * 0.5f));
		}
	}

	private void HideAll()
	{
		if (!hidden)
		{
			hidden = true;
			outerCircle.enabled = false;
			innerCircle.enabled = false;
			hint.enabled = false;
			maxRangeText.enabled = false;
			noShoot.enabled = false;
		}
	}

	private void ShowAll()
	{
		if (hidden)
		{
			hidden = false;
			outerCircle.enabled = true;
			innerCircle.enabled = true;
			hint.enabled = true;
			maxRangeText.enabled = true;
			ResetRange();
		}
	}

	private void ResetRange()
	{
		if (targetList.Count > 0)
		{
			aircraft.NetworkHQ.TryGetKnownPosition(targetList[0], out knownPos);
			CalcWeaponRange();
			maxRangeSmoothed = maxRange;
		}
		else
		{
			maxRangeSmoothed = weaponInfo.targetRequirements.maxRange;
		}
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		weaponInfo = weaponStation.WeaponInfo;
		this.aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		targetDesignator.transform.localScale = Vector3.one;
		targetList = aircraft.weaponManager.GetTargetList();
		prefabMissile = weaponStation.WeaponInfo.weaponPrefab.GetComponent<Missile>();
		minRange = weaponInfo.targetRequirements.minRange;
		minAlignment = weaponInfo.targetRequirements.minAlignment;
		cam = SceneSingleton<CameraStateManager>.i.mainCamera;
		SceneSingleton<FlightHud>.i.waterline.enabled = false;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
		ResetRange();
		HideAll();
	}
}
