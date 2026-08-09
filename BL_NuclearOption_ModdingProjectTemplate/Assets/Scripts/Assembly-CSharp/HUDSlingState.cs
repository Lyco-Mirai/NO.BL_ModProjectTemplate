using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDSlingState : HUDWeaponState
{
	private Image targetDesignator;

	private WeaponStation weaponStation;

	private SlingloadHook hook;

	[SerializeField]
	private Sprite defaultImg;

	[SerializeField]
	private Image reticleImg;

	[SerializeField]
	private Image centerImg;

	[SerializeField]
	private Image unitImg;

	[SerializeField]
	private Image unitFwdImg;

	[SerializeField]
	private TextMeshProUGUI hookState;

	[SerializeField]
	private TextMeshProUGUI vehicleInfo;

	[SerializeField]
	private TextMeshProUGUI ropeInfo;

	[SerializeField]
	private GameObject rangeObject;

	private Vector3 relPos;

	private float relAngle;

	private float UI_scale = 5f;

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		weaponInfo = weaponStation.WeaponInfo;
		this.targetDesignator = targetDesignator;
		targetDesignator.transform.localScale = Vector3.one;
		SceneSingleton<CombatHUD>.i.targetDesignator.enabled = true;
		SceneSingleton<FlightHud>.i.waterline.enabled = false;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
		hook = weaponStation.Weapons[0] as SlingloadHook;
		reticleImg.enabled = false;
		centerImg.enabled = false;
		centerImg.rectTransform.sizeDelta = Vector2.zero;
		unitImg.enabled = false;
		unitFwdImg.enabled = false;
		vehicleInfo.enabled = false;
		ropeInfo.enabled = false;
		rangeObject.SetActive(value: false);
	}

	public override void HUDFixedUpdate(Aircraft aircraft, List<Unit> targetList)
	{
	}

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		Unit unit = null;
		Unit suspendedUnit = hook.GetSuspendedUnit();
		if (aircraft.weaponManager.GetTargetList().Count > 0)
		{
			unit = aircraft.weaponManager.GetTargetList()[0];
		}
		bool flag = unit != null && !unit.disabled && unit.definition.CanSlingLoad;
		targetDesignator.enabled = !flag;
		targetDesignator.color = ThemeManager.Active.ColorTheme.AllClear;
		switch (hook.deployState)
		{
		case SlingloadHook.DeployState.Deployed:
		{
			float lineLength = hook.GetLineLength();
			float lineMaxLength = hook.GetLineMaxLength();
			if (lineLength < lineMaxLength)
			{
				hookState.text = $"EXTENDING v {lineLength:F1}m";
			}
			else if (!flag)
			{
				hookState.text = "NO TARGET";
			}
			else if (aircraft.radarAlt > lineLength)
			{
				hookState.text = "TOO HIGH";
			}
			else
			{
				hookState.text = "READY";
			}
			if (!reticleImg.enabled)
			{
				reticleImg.enabled = true;
			}
			if (!centerImg.enabled)
			{
				centerImg.enabled = true;
			}
			unitImg.enabled = unit != null;
			unitFwdImg.enabled = unit != null;
			if (unit != null)
			{
				if (!rangeObject.activeSelf)
				{
					rangeObject.SetActive(value: true);
				}
				float num3 = Mathf.Clamp(lineLength - aircraft.radarAlt + unit.definition.height, 0f, lineLength) * UI_scale;
				centerImg.rectTransform.sizeDelta = num3 * Vector2.one;
				relPos = hook.winch.InverseTransformPoint(unit.transform.position);
				relAngle = Vector3.SignedAngle(unit.transform.forward, aircraft.transform.forward, Vector3.up);
				relPos.x = Mathf.Clamp(relPos.x, -40f, 40f);
				relPos.y = 0f;
				relPos.z = Mathf.Clamp(relPos.z, -40f, 40f);
				unitImg.rectTransform.sizeDelta = UI_scale * new Vector2(unit.definition.width, unit.definition.length);
				unitImg.transform.localPosition = UI_scale * new Vector3(relPos.x, relPos.z, 0f);
				unitImg.transform.localEulerAngles = new Vector3(0f, 0f, relAngle);
				if (unitImg.transform.localPosition.magnitude < 1f * num3)
				{
					centerImg.color = ThemeManager.Active.ColorTheme.AllClear;
					unitImg.color = ThemeManager.Active.ColorTheme.AllClear;
					unitFwdImg.color = ThemeManager.Active.ColorTheme.AllClear;
					unitImg.pixelsPerUnitMultiplier = 2f;
				}
				else if (unitImg.transform.localPosition.magnitude < 2f * num3)
				{
					centerImg.color = ThemeManager.Active.ColorTheme.Warning;
					unitImg.color = ThemeManager.Active.ColorTheme.Warning;
					unitFwdImg.color = ThemeManager.Active.ColorTheme.Warning;
					unitImg.pixelsPerUnitMultiplier = 3f;
				}
				else
				{
					centerImg.color = Color.grey;
					unitImg.color = Color.white;
					unitFwdImg.color = Color.white;
					unitImg.pixelsPerUnitMultiplier = 4f;
				}
			}
			break;
		}
		case SlingloadHook.DeployState.Retracted:
			if (!flag)
			{
				hookState.text = "NO TARGET";
			}
			else
			{
				hookState.text = "RETRACTED";
			}
			if (reticleImg.enabled)
			{
				reticleImg.enabled = false;
			}
			if (centerImg.enabled)
			{
				centerImg.enabled = false;
				centerImg.rectTransform.sizeDelta = Vector2.zero;
			}
			if (unitImg.enabled)
			{
				unitImg.enabled = false;
			}
			if (unitFwdImg.enabled)
			{
				unitFwdImg.enabled = false;
			}
			if (vehicleInfo.enabled)
			{
				vehicleInfo.enabled = false;
			}
			if (ropeInfo.enabled)
			{
				ropeInfo.enabled = false;
			}
			if (rangeObject.activeSelf)
			{
				rangeObject.SetActive(value: false);
			}
			break;
		case SlingloadHook.DeployState.Retracting:
			hookState.text = $"RETRACTING ^ {hook.GetLineLength():F1}m";
			if (reticleImg.enabled)
			{
				reticleImg.enabled = false;
			}
			if (centerImg.enabled)
			{
				centerImg.enabled = false;
				centerImg.rectTransform.sizeDelta = Vector2.zero;
			}
			if (unitImg.enabled)
			{
				unitImg.enabled = false;
			}
			if (unitFwdImg.enabled)
			{
				unitFwdImg.enabled = false;
			}
			if (vehicleInfo.enabled)
			{
				vehicleInfo.enabled = false;
			}
			if (ropeInfo.enabled)
			{
				ropeInfo.enabled = false;
			}
			if (rangeObject.activeSelf)
			{
				rangeObject.SetActive(value: false);
			}
			break;
		case SlingloadHook.DeployState.Connected:
			if (!(suspendedUnit == null))
			{
				double num2 = (double)hook.loadForce * 0.000101971621;
				if (num2 < 1.0)
				{
					num2 = suspendedUnit.definition.mass * 0.001f;
				}
				hookState.text = suspendedUnit.definition.code + " CONNECTED";
				suspendedUnit.CheckRadarAlt();
				if (!vehicleInfo.enabled)
				{
					vehicleInfo.enabled = true;
				}
				vehicleInfo.text = $"{num2:F1}T - {UnitConverter.AltitudeReading(suspendedUnit.radarAlt)}";
				if (!ropeInfo.enabled)
				{
					ropeInfo.enabled = true;
				}
				ropeInfo.text = $"{hook.GetRopeAngle():F1}°\n{0.001f * hook.loadForce:F1}kN";
				if (hook.GetRopeAngle() > 45f || hook.GetRopeFactor() > 0.5f)
				{
					ropeInfo.color = ThemeManager.Active.ColorTheme.Warning;
				}
				else if (hook.GetRopeAngle() > 80f || hook.GetRopeFactor() > 0.8f)
				{
					ropeInfo.color = ThemeManager.Active.ColorTheme.Alert;
				}
				else
				{
					ropeInfo.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				if (!reticleImg.enabled)
				{
					reticleImg.enabled = true;
				}
				if (centerImg.enabled)
				{
					centerImg.enabled = false;
				}
				if (!unitImg.enabled)
				{
					unitImg.enabled = true;
					unitImg.pixelsPerUnitMultiplier = 2f;
					unitImg.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				if (!unitFwdImg.enabled)
				{
					unitFwdImg.enabled = true;
					unitFwdImg.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				relPos = hook.transform.InverseTransformPoint(suspendedUnit.transform.position);
				relAngle = Vector3.SignedAngle(aircraft.transform.forward, suspendedUnit.transform.forward, Vector3.up);
				relPos.x = Mathf.Clamp(relPos.x, -40f, 40f);
				relPos.z = Mathf.Clamp(relPos.z, -40f, 40f);
				unitImg.rectTransform.sizeDelta = UI_scale * new Vector2(suspendedUnit.definition.width, suspendedUnit.definition.length);
				unitImg.transform.localPosition = UI_scale * new Vector3(relPos.x, relPos.z, 0f);
				unitImg.transform.localEulerAngles = new Vector3(0f, 0f, 0f - relAngle);
				ropeInfo.transform.eulerAngles = Vector3.zero;
			}
			break;
		case SlingloadHook.DeployState.RescuePilot:
			if (!(suspendedUnit == null) && suspendedUnit is PilotDismounted)
			{
				hookState.text = $"RECOVERING PILOT ^ {hook.GetLineLength():F1}m";
				double num = (double)hook.loadForce * 0.000101971621;
				if (num < 1.0)
				{
					num = suspendedUnit.definition.mass * 0.001f;
				}
				suspendedUnit.CheckRadarAlt();
				if (!vehicleInfo.enabled)
				{
					vehicleInfo.enabled = true;
				}
				vehicleInfo.text = $"{num:F1}T - {UnitConverter.AltitudeReading(suspendedUnit.radarAlt)}";
				if (!ropeInfo.enabled)
				{
					ropeInfo.enabled = true;
				}
				ropeInfo.text = $"{hook.GetRopeAngle():F1}°\n{0.001f * hook.loadForce:F1}kN";
				if (hook.GetRopeAngle() > 45f || hook.GetRopeFactor() > 0.5f)
				{
					ropeInfo.color = ThemeManager.Active.ColorTheme.Warning;
				}
				else if (hook.GetRopeAngle() > 80f || hook.GetRopeFactor() > 0.8f)
				{
					ropeInfo.color = ThemeManager.Active.ColorTheme.Alert;
				}
				else
				{
					ropeInfo.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				if (!reticleImg.enabled)
				{
					reticleImg.enabled = true;
				}
				if (centerImg.enabled)
				{
					centerImg.enabled = false;
					centerImg.rectTransform.sizeDelta = Vector2.zero;
				}
				if (!unitImg.enabled)
				{
					unitImg.enabled = true;
					unitImg.pixelsPerUnitMultiplier = 2f;
					unitImg.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				if (!unitFwdImg.enabled)
				{
					unitFwdImg.enabled = true;
					unitFwdImg.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				relPos = hook.transform.InverseTransformPoint(suspendedUnit.transform.position);
				relAngle = Vector3.SignedAngle(aircraft.transform.forward, suspendedUnit.transform.forward, Vector3.up);
				relPos.x = Mathf.Clamp(relPos.x, -40f, 40f);
				relPos.z = Mathf.Clamp(relPos.z, -40f, 40f);
				unitImg.rectTransform.sizeDelta = UI_scale * new Vector2(suspendedUnit.definition.width, suspendedUnit.definition.length);
				unitImg.transform.localPosition = UI_scale * new Vector3(relPos.x, relPos.z, 0f);
				unitImg.transform.localEulerAngles = new Vector3(0f, 0f, 0f - relAngle);
				ropeInfo.transform.eulerAngles = Vector3.zero;
			}
			break;
		}
	}
}
