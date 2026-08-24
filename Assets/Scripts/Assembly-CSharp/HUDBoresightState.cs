using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDBoresightState : HUDWeaponState
{
	private enum DisplayState
	{
		Disabled = 0,
		OutOfRange = 1,
		ShowingLead = 2,
		Shoot = 3
	}

	private DisplayState displayState;

	[SerializeField]
	private Image projectedPosition;

	[SerializeField]
	private Image targetPosition;

	[SerializeField]
	private Image boresight;

	[SerializeField]
	private Image line;

	[SerializeField]
	private TextMeshProUGUI hint;

	private Vector3 gunDirectionRelative;

	private Image targetDesignator;

	private WeaponStation weaponStation;

	private Vector3 targetVel;

	private Vector3 targetVelPrev;

	private Vector3 targetAccel;

	private Vector3 targetAccelSmoothed;

	private Vector3 targetAccelSmoothingVel;

	private ControlsFilter controlsFilter;

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		DisplayState num = displayState;
		base.transform.localPosition = Vector3.zero;
		SceneSingleton<FlightHud>.i.velocityVector.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(FastMath.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, boresight.transform.position) * 0.05f - 0.1f));
		Vector3 vector = aircraft.transform.TransformDirection(gunDirectionRelative);
		Vector3 position = aircraft.transform.position + vector * 3000f;
		Vector3 vector2 = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(position);
		vector2.z = 0f;
		boresight.transform.position = vector2;
		targetDesignator.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(Vector3.Distance(targetDesignator.transform.position, vector2) * 0.05f - 0.1f));
		if (aircraft.gearDeployed)
		{
			boresight.enabled = false;
			SceneSingleton<FlightHud>.i.waterline.enabled = true;
		}
		else
		{
			boresight.enabled = Vector3.Dot(SceneSingleton<CameraStateManager>.i.transform.forward, vector) > 0f;
			SceneSingleton<FlightHud>.i.waterline.enabled = false;
		}
		displayState = DisplayState.Disabled;
		bool lookingAtTarget = false;
		boresight.color = Color.gray;
		if (weaponStation.Ammo > 0 && targetList.Count > 0)
		{
			Unit unit = targetList[0];
			float num2 = FastMath.Distance(unit.GlobalPosition(), aircraft.GlobalPosition()) / weaponInfo.targetRequirements.maxRange;
			displayState = DisplayState.OutOfRange;
			if (DisplayLead(aircraft, unit, out lookingAtTarget))
			{
				if (num2 < 0.75f && FastMath.InRange(projectedPosition.transform.position, boresight.transform.position, 20f))
				{
					displayState = DisplayState.Shoot;
				}
				else
				{
					displayState = DisplayState.ShowingLead;
				}
			}
		}
		if (PlayerSettings.zoomOnBoresight)
		{
			SceneSingleton<CameraStateManager>.i.SetDesiredFoV(lookingAtTarget ? (PlayerSettings.defaultFoV * 0.7f) : PlayerSettings.defaultFoV, 0f);
		}
		if (num != displayState)
		{
			UpdateDisplayState();
		}
	}

	private bool DisplayLead(Aircraft aircraft, Unit firstTarget, out bool lookingAtTarget)
	{
		lookingAtTarget = false;
		if (!aircraft.NetworkHQ.IsTargetPositionAccurate(firstTarget, 10f))
		{
			return false;
		}
		controlsFilter.GetAim(firstTarget, out var aimPoint, out var _);
		if (!aimPoint.HasValue)
		{
			return false;
		}
		Vector3 a = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(firstTarget.transform.position);
		a = Vector3.Scale(a, new Vector3(1f, 1f, 0f));
		targetPosition.transform.position = a;
		Vector3 a2 = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(aimPoint.Value.ToLocalPosition());
		a2 = Vector3.Scale(a2, new Vector3(1f, 1f, 0f));
		Vector3 a3 = boresight.transform.position - a2 + a;
		a3 = Vector3.Scale(a3, new Vector3(1f, 1f, 0f));
		projectedPosition.transform.position = (PlayerSettings.lagPip ? a3 : a2);
		Vector3 obj = (PlayerSettings.lagPip ? Vector3.zero : targetPosition.transform.localPosition);
		Vector3 localPosition = projectedPosition.transform.localPosition;
		Vector3 localPosition2 = (obj + localPosition) / 2f;
		line.transform.localPosition = localPosition2;
		Vector3 vector = obj - localPosition;
		line.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vector.y, vector.x) * 57.29578f);
		float num = vector.magnitude;
		boresight.color = (FastMath.InRange(projectedPosition.transform.position, boresight.transform.position, 15f) ? ThemeManager.Active.ColorTheme.AllClear : ThemeManager.Active.ColorTheme.Warning);
		if (num > 14f)
		{
			if (!line.enabled)
			{
				line.enabled = true;
			}
			num -= 14f;
		}
		else if (line.enabled)
		{
			line.enabled = false;
		}
		line.transform.localScale = new Vector3(num, 1f, 1f);
		lookingAtTarget = Vector3.Angle(aimPoint.Value - aircraft.GlobalPosition(), SceneSingleton<CameraStateManager>.i.transform.forward) < 10f;
		return true;
	}

	private void UpdateDisplayState()
	{
		switch (displayState)
		{
		case DisplayState.Disabled:
			projectedPosition.enabled = false;
			line.enabled = false;
			hint.text = "";
			break;
		case DisplayState.OutOfRange:
			projectedPosition.enabled = false;
			line.enabled = false;
			hint.text = "OUT OF RANGE";
			break;
		case DisplayState.ShowingLead:
			projectedPosition.enabled = true;
			hint.text = "";
			break;
		case DisplayState.Shoot:
			hint.text = "SHOOT";
			projectedPosition.enabled = true;
			break;
		}
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		weaponInfo = weaponStation.WeaponInfo;
		this.targetDesignator = targetDesignator;
		targetDesignator.transform.localScale = Vector3.one * 0.6f;
		SceneSingleton<FlightHud>.i.waterline.enabled = false;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one * 0.7f;
		controlsFilter = aircraft.GetControlsFilter();
		Vector3 zero = Vector3.zero;
		foreach (Weapon weapon in weaponStation.Weapons)
		{
			zero += weapon.transform.forward;
		}
		gunDirectionRelative = aircraft.transform.InverseTransformDirection(zero);
		displayState = DisplayState.Disabled;
		UpdateDisplayState();
	}

	public override void HUDFixedUpdate(Aircraft aircraft, List<Unit> targetList)
	{
		if (targetList.Count != 0)
		{
			targetVel = ((targetList[0].rb != null) ? targetList[0].rb.velocity : Vector3.zero);
			targetAccel = (targetVel - targetVelPrev) / Time.fixedDeltaTime;
			targetAccelSmoothed = Vector3.SmoothDamp(targetAccelSmoothed, targetAccel, ref targetAccelSmoothingVel, 0.5f);
			targetVelPrev = targetVel;
		}
	}
}
