using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDMissileState : HUDWeaponState
{
	private bool hidden;

	private bool allRequirementsMet;

	private float minTargetDist;

	private float maxTargetDist;

	private float minRange;

	private float maxRange;

	private float noEscapeRange;

	private float maxTargetAngle;

	private float minAlignment;

	private float minPlatformSpeed;

	private float maxTargetSpeed;

	private float lastWeaponRangeCalc;

	private float lastTextDisplay;

	private GlobalPosition knownPos;

	[SerializeField]
	private Image noShoot;

	[SerializeField]
	private Image noEscapeMark;

	[SerializeField]
	private Image maxDistImage;

	[SerializeField]
	private Image minDistImage;

	[SerializeField]
	private Image distSpanImage;

	[SerializeField]
	private Image[] ladderImages;

	[SerializeField]
	private TextMeshProUGUI maxRangeText;

	[SerializeField]
	private TextMeshProUGUI minRangeText;

	[SerializeField]
	private TextMeshProUGUI noEscapeRangeText;

	[SerializeField]
	private TextMeshProUGUI targetText;

	[SerializeField]
	private TextMeshProUGUI hint;

	[SerializeField]
	private Transform rMaxTransform;

	[SerializeField]
	private Transform rMinTransform;

	[SerializeField]
	private Transform rNETransform;

	[SerializeField]
	private Transform maxTargetDistTransform;

	[SerializeField]
	private Transform minTargetDistTransform;

	[SerializeField]
	private Transform targetDistSpan;

	[SerializeField]
	private Transform avgDistTransform;

	[SerializeField]
	private Transform outRangeTransform;

	private Aircraft aircraft;

	private Unit farTarget;

	private Unit closeTarget;

	private WeaponStation weaponStation;

	private Missile prefabMissile;

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		SceneSingleton<FlightHud>.i.velocityVector.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.color = Color.Lerp(Color.black, ThemeManager.Active.ColorTheme.AllClear, Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.GetHUDCenter().position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.enabled = !aircraft.gearDeployed;
		GlobalPosition b = aircraft.GlobalPosition();
		if (targetList.Count > 0 && farTarget != null)
		{
			if (aircraft.NetworkHQ.TryGetKnownPosition(farTarget, out var knownPosition))
			{
				maxTargetDist = FastMath.Distance(knownPosition, b);
			}
			if (targetList.Count > 1)
			{
				if (aircraft.NetworkHQ.TryGetKnownPosition(closeTarget, out var knownPosition2))
				{
					minTargetDist = FastMath.Distance(knownPosition2, b);
				}
			}
			else
			{
				minTargetDist = maxTargetDist;
			}
		}
		rNETransform.position = Vector3.Lerp(rMinTransform.position, rMaxTransform.position, Mathf.Max((noEscapeRange - minRange) / (maxRange - minRange), 0.1f));
		if (maxTargetDist < maxRange)
		{
			maxTargetDistTransform.position = Vector3.Lerp(rMinTransform.position, rMaxTransform.position, (maxTargetDist - minRange) / (maxRange - minRange));
		}
		else
		{
			maxTargetDistTransform.position = Vector3.Lerp(rMaxTransform.position, outRangeTransform.position, (maxTargetDist - maxRange) / maxRange);
		}
		if (minTargetDist < maxRange)
		{
			minTargetDistTransform.position = Vector3.Lerp(rMinTransform.position, rMaxTransform.position, (minTargetDist - minRange) / (maxRange - minRange));
		}
		else
		{
			minTargetDistTransform.position = Vector3.Lerp(rMaxTransform.position, outRangeTransform.position, (minTargetDist - maxRange) / maxRange);
		}
		float num = 1080f / (float)Screen.height;
		targetDistSpan.localScale = new Vector3(targetDistSpan.localScale.x, num * (maxTargetDistTransform.position.y - minTargetDistTransform.position.y), 1f);
		avgDistTransform.position = Vector3.Lerp(minTargetDistTransform.position, maxTargetDistTransform.position, 0.5f);
		CalcWeaponRange();
		DisplayText();
	}

	private void CalcWeaponRange()
	{
		if (Time.timeSinceLevelLoad - lastWeaponRangeCalc < 1f || targetList.Count == 0 || weaponStation.Ammo == 0)
		{
			return;
		}
		lastWeaponRangeCalc = Time.timeSinceLevelLoad;
		GlobalPosition globalPosition = aircraft.GlobalPosition();
		float num = 0f;
		float num2 = float.MaxValue;
		maxTargetAngle = 0f;
		maxTargetSpeed = 0f;
		foreach (Unit target in targetList)
		{
			if (aircraft.NetworkHQ.TryGetKnownPosition(target, out var knownPosition))
			{
				float num3 = FastMath.SquareDistance(knownPosition, globalPosition);
				if (num3 > num)
				{
					num = num3;
					farTarget = target;
				}
				if (num3 < num2)
				{
					num2 = num3;
					closeTarget = target;
				}
				maxTargetAngle = Mathf.Max(maxTargetAngle, Vector3.Angle(knownPosition - globalPosition, aircraft.transform.forward));
				maxTargetSpeed = Mathf.Max(target.speed, maxTargetSpeed);
			}
		}
		if (prefabMissile != null)
		{
			maxRange = prefabMissile.CalcRange(aircraft.speed, aircraft.GlobalPosition().y, knownPos.y, maxTargetDist, maxTargetSpeed, out noEscapeRange);
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
		maxRangeText.text = "MAX \n" + UnitConverter.DistanceReading(maxRange);
		minRangeText.text = "MIN \n" + UnitConverter.DistanceReading(minRange);
		if (noEscapeRange < maxRange * 0.9f)
		{
			noEscapeMark.enabled = true;
			noEscapeRangeText.enabled = true;
			noEscapeRangeText.text = "NEZ \n" + UnitConverter.DistanceReading(noEscapeRange);
		}
		else
		{
			noEscapeMark.enabled = false;
			noEscapeRangeText.enabled = false;
		}
		allRequirementsMet = false;
		if (maxTargetDist > maxRange)
		{
			hint.enabled = true;
			hint.text = "OUT OF RANGE";
		}
		else if (minTargetDist < minRange)
		{
			hint.enabled = true;
			hint.text = "TOO CLOSE";
		}
		else if (maxTargetAngle > minAlignment)
		{
			hint.text = "OUT OF ARC";
		}
		else if (aircraft.speed < minPlatformSpeed)
		{
			hint.text = "TOO SLOW";
		}
		else
		{
			allRequirementsMet = true;
			hint.text = "SHOOT";
			hint.enabled = maxTargetDist < noEscapeRange;
		}
		noShoot.enabled = !allRequirementsMet;
	}

	private void HideAll()
	{
		if (!hidden)
		{
			hidden = true;
			hint.enabled = false;
			maxRangeText.enabled = false;
			minRangeText.enabled = false;
			targetText.enabled = false;
			noEscapeMark.enabled = false;
			noEscapeRangeText.enabled = false;
			noShoot.enabled = false;
			maxDistImage.enabled = false;
			minDistImage.enabled = false;
			distSpanImage.enabled = false;
			Image[] array = ladderImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
	}

	private void ShowAll()
	{
		if (hidden)
		{
			hidden = false;
			maxRangeText.enabled = true;
			minRangeText.enabled = true;
			targetText.enabled = true;
			maxDistImage.enabled = true;
			minDistImage.enabled = true;
			distSpanImage.enabled = true;
			Image[] array = ladderImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			ResetRange();
		}
	}

	private void ResetRange()
	{
		if (targetList.Count > 0)
		{
			CalcWeaponRange();
		}
		else
		{
			maxRange = weaponInfo.targetRequirements.maxRange;
		}
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		targetDesignator.transform.localScale = Vector3.one;
		weaponInfo = weaponStation.WeaponInfo;
		this.aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		targetList = aircraft.weaponManager.GetTargetList();
		if (weaponStation.WeaponInfo.weaponPrefab != null)
		{
			prefabMissile = weaponStation.WeaponInfo.weaponPrefab.GetComponent<Missile>();
		}
		minRange = weaponInfo.targetRequirements.minRange;
		maxRange = weaponInfo.targetRequirements.maxRange;
		noEscapeRange = maxRange;
		minAlignment = weaponInfo.targetRequirements.minAlignment;
		minPlatformSpeed = weaponInfo.targetRequirements.minOwnerSpeed;
		SceneSingleton<FlightHud>.i.waterline.enabled = true;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
		ResetRange();
		HideAll();
	}
}
