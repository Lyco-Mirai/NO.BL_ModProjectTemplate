using System;
using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDBombingState : HUDWeaponState
{
	[SerializeField]
	private Image alignmentBar;

	private float dropTime = 100f;

	[SerializeField]
	private TextMeshProUGUI dropCountdown;

	[SerializeField]
	private TextMeshProUGUI ccipFallTime;

	[SerializeField]
	private TextMeshProUGUI ccrpFallTime;

	[SerializeField]
	private Image upperMarker;

	[SerializeField]
	private Image lowerMarker;

	[SerializeField]
	private Image ccipPipper;

	[SerializeField]
	private Image ccipLine;

	[SerializeField]
	private Image ccrpCircle;

	private WeaponStation weaponStation;

	private float lastCCIPCheck;

	private GlobalPosition ccipImpactPoint;

	private GlobalPosition averageTargetPosition;

	private GlobalPosition lastAverageTargetPosition;

	private bool initialized;

	private Vector3 ccipImpactPointSmoothed;

	private List<GlobalPosition> simPoints = new List<GlobalPosition>();

	private float dragCoef;

	private float mass;

	private float finArea;

	public override void UpdateWeaponDisplay(Aircraft aircraft, List<Unit> targetList)
	{
		Camera mainCamera = SceneSingleton<CameraStateManager>.i.mainCamera;
		if (!initialized)
		{
			ccipImpactPoint = aircraft.GlobalPosition() + aircraft.rb.velocity * 8f - Vector3.up * 4.905f * 8f * 8f;
			ccipImpactPointSmoothed = aircraft.GlobalPosition().AsVector3() + aircraft.rb.velocity * 8f - Vector3.up * 4.905f * 8f * 8f;
			initialized = true;
		}
		SceneSingleton<CombatHUD>.i.targetDesignator.color = Color.Lerp(Color.black, ThemeManager.Active.ColorTheme.AllClear, Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.GetHUDCenter().position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<FlightHud>.i.velocityVector.color = ThemeManager.Active.ColorTheme.AllClear.WithAlpha(Mathf.Clamp01(Vector3.Distance(SceneSingleton<FlightHud>.i.velocityVector.transform.position, SceneSingleton<CombatHUD>.i.targetDesignator.transform.position) * 0.015f - 0.15f));
		SceneSingleton<CombatHUD>.i.targetDesignator.enabled = !aircraft.gearDeployed;
		base.transform.localPosition = Vector3.zero;
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (Unit target in targetList)
		{
			if (!target.disabled && aircraft.NetworkHQ.TryGetKnownPosition(target, out var knownPosition))
			{
				zero += knownPosition.ToLocalPosition();
				num++;
			}
		}
		if (num > 0)
		{
			zero /= (float)num;
		}
		averageTargetPosition = zero.ToGlobalPosition();
		averageTargetPosition += Vector3.up * weaponStation.WeaponInfo.airburstHeight;
		Vector3 forward = aircraft.transform.forward;
		forward.y = 0f;
		Vector3 vector = averageTargetPosition - aircraft.GlobalPosition();
		Vector3 vector2 = vector;
		vector2.y = 0f;
		float num2 = Vector3.Dot(aircraft.transform.forward, Vector3.up);
		int num3;
		if (weaponStation != null)
		{
			num3 = ((weaponStation.Ammo > 0) ? 1 : 0);
			if (num3 != 0 && num > 0 && num2 > -0.01f && Vector3.Dot(mainCamera.transform.forward, aircraft.transform.forward) > 0f && Vector3.Dot(forward, vector2) > 0.5f && vector.y < 0f)
			{
				if (!alignmentBar.gameObject.activeSelf)
				{
					alignmentBar.gameObject.SetActive(value: true);
				}
				if (!dropCountdown.enabled)
				{
					dropCountdown.enabled = true;
					ccrpFallTime.enabled = true;
					ccrpCircle.enabled = true;
				}
				float initialHeight = 0f - vector.y;
				float magnitude = vector2.magnitude;
				float num4 = Kinematics.FallTime(initialHeight, aircraft.rb.velocity.y);
				float num5 = Vector3.Dot(aircraft.rb.velocity.normalized, vector2.normalized);
				if (Mathf.Abs(num5) < 0.001f)
				{
					num5 = 0.001f;
				}
				float num6 = magnitude / (num5 * aircraft.rb.velocity.magnitude);
				dropTime = num6 - num4;
				dropCountdown.text = $"REL {dropTime:F1}";
				dropCountdown.enabled = dropTime < 100f && dropTime > -10f;
				dropCountdown.transform.eulerAngles = Vector3.zero;
				ccrpFallTime.text = $"ToF {num4:F1}";
				float num7 = 0f - vector.y;
				num7 += Vector3.Project(vector2, aircraft.transform.forward).y;
				Vector3 position = mainCamera.WorldToScreenPoint(averageTargetPosition.ToLocalPosition() + Vector3.up * num7);
				position.z = 0f;
				alignmentBar.transform.position = position;
				position = mainCamera.WorldToScreenPoint(averageTargetPosition.ToLocalPosition() + Vector3.up * num7 * 0.9f);
				position.z = 0f;
				float z = (0f - Mathf.Atan2(position.x - alignmentBar.transform.position.x, position.y - alignmentBar.transform.position.y)) * 57.29578f + 180f;
				alignmentBar.transform.eulerAngles = new Vector3(0f, 0f, z);
				if (float.IsFinite(dropTime))
				{
					dropTime = Mathf.Clamp(dropTime, -10f, 10f);
					upperMarker.transform.localPosition = new Vector3(0f, dropTime * 15f, 0f);
					lowerMarker.transform.localPosition = new Vector3(0f, dropTime * -15f, 0f);
					ccrpCircle.fillAmount = Mathf.Clamp01(1f - Mathf.Abs(dropTime / 10f));
				}
				if (dropTime > 0f)
				{
					dropCountdown.gameObject.transform.localPosition = Vector3.right * 30f;
					dropCountdown.color = ThemeManager.Active.ColorTheme.Warning;
					ccrpCircle.color = ThemeManager.Active.ColorTheme.Warning;
					ccrpFallTime.color = ThemeManager.Active.ColorTheme.Warning;
				}
				if (dropTime < 0f)
				{
					dropCountdown.gameObject.transform.localPosition = -Vector3.right * 30f;
					dropCountdown.color = ThemeManager.Active.ColorTheme.Alert;
					ccrpCircle.color = ThemeManager.Active.ColorTheme.Alert;
					ccrpFallTime.color = ThemeManager.Active.ColorTheme.Alert;
				}
				if (Mathf.Abs(dropTime) < 2f)
				{
					dropCountdown.color = ThemeManager.Active.ColorTheme.AllClear;
					ccrpCircle.color = ThemeManager.Active.ColorTheme.AllClear;
					ccrpFallTime.color = ThemeManager.Active.ColorTheme.AllClear;
				}
				goto IL_07e4;
			}
		}
		else
		{
			num3 = 0;
		}
		if (alignmentBar.gameObject.activeSelf)
		{
			alignmentBar.gameObject.SetActive(value: false);
		}
		if (dropCountdown.enabled)
		{
			dropCountdown.enabled = false;
			ccrpFallTime.enabled = false;
			ccrpCircle.enabled = false;
		}
		goto IL_07e4;
		IL_07e4:
		if (num3 != 0 && !alignmentBar.gameObject.activeSelf && !aircraft.gearDeployed && (num == 0 || Vector3.Dot(SceneSingleton<CameraStateManager>.i.transform.forward, vector.normalized) > 0.4f))
		{
			CCIPTrajectory(aircraft);
			UpdatePipperPosition(aircraft);
			return;
		}
		ccipLine.enabled = false;
		ccipPipper.enabled = false;
		ccipFallTime.enabled = false;
		ccipImpactPoint = aircraft.GlobalPosition() + aircraft.rb.velocity * 8f - Vector3.up * 4.905f * 8f * 8f;
		ccipImpactPointSmoothed = aircraft.transform.position + aircraft.rb.velocity * 8f - Datum.origin.position;
	}

	public void CCIPTrajectory(Aircraft aircraft)
	{
		if (Time.timeSinceLevelLoad - lastCCIPCheck < 0.1f)
		{
			return;
		}
		lastCCIPCheck = Time.timeSinceLevelLoad;
		simPoints.Clear();
		GlobalPosition globalPosition = aircraft.GlobalPosition() - Vector3.up * aircraft.definition.spawnOffset.y;
		Vector3 vector = aircraft.rb.velocity - Vector3.up * 9.81f * 0.25f + weaponStation.WeaponInfo.muzzleVelocity * aircraft.transform.forward;
		float num = 0.5f;
		float airDensity = LevelInfo.GetAirDensity(aircraft.GlobalPosition().y);
		float num2 = 0.5f * dragCoef * airDensity * finArea / mass;
		for (int i = 0; i < 32; i++)
		{
			num += 0.04f;
			globalPosition += vector * num;
			simPoints.Add(globalPosition);
			if (globalPosition.y < 0f)
			{
				Ray ray = new Ray(globalPosition.ToLocalPosition() - vector * num, vector * num);
				if (simPoints.Count > 1 && Datum.WaterPlane().Raycast(ray, out var enter))
				{
					List<GlobalPosition> list = simPoints;
					int index = list.Count - 1;
					List<GlobalPosition> list2 = simPoints;
					list[index] = list2[list2.Count - 2] + vector.normalized * enter;
				}
				break;
			}
			vector -= (Vector3.up * 9.81f + vector.normalized * num2 * vector.sqrMagnitude) * num;
		}
		if (globalPosition.y > 0f)
		{
			ccipPipper.enabled = false;
			ccipLine.enabled = false;
			ccipFallTime.enabled = false;
		}
		else if (SplitSearchTrajectory())
		{
			ccipPipper.enabled = true;
			ccipLine.enabled = true;
			ccipFallTime.enabled = true;
			float num3 = Kinematics.FallTime(aircraft.radarAlt, aircraft.rb.velocity.y);
			ccipFallTime.text = $"ToF {num3:F1}";
		}
		else
		{
			ccipPipper.enabled = false;
			ccipLine.enabled = false;
			ccipFallTime.enabled = false;
		}
	}

	private void UpdatePipperPosition(Aircraft aircraft)
	{
		Camera main = Camera.main;
		ccipImpactPointSmoothed = Vector3.Lerp(ccipImpactPointSmoothed, ccipImpactPoint.AsVector3() + aircraft.rb.velocity * 0.3f, 5f * Time.deltaTime);
		if (Vector3.Dot((ccipImpactPointSmoothed + Datum.origin.position - main.transform.position).normalized, main.transform.forward) < 0.5f)
		{
			ccipPipper.enabled = false;
			ccipLine.enabled = false;
			return;
		}
		Vector3 vector = main.WorldToScreenPoint(ccipImpactPointSmoothed + Datum.origin.position);
		vector.z = 0f;
		ccipPipper.transform.position = vector;
		Vector3 position = SceneSingleton<FlightHud>.i.velocityVector.transform.position;
		position.z = 0f;
		float num = 1080f / (float)Screen.height;
		Vector3 lhs = position - vector;
		ccipLine.transform.position = vector + lhs.normalized * 22f / num;
		position -= lhs.normalized * 8f / num;
		float y = (ccipLine.transform.position - position).magnitude * num;
		float z = (0f - Mathf.Atan2(lhs.x, lhs.y)) * 57.29578f;
		if (Vector3.Dot(lhs, position - ccipLine.transform.position) < 0f)
		{
			ccipLine.enabled = false;
		}
		ccipLine.transform.eulerAngles = new Vector3(0f, 0f, z);
		ccipLine.transform.localScale = new Vector3(1f, y, 1f);
	}

	private bool SplitSearchTrajectory()
	{
		List<GlobalPosition> list = simPoints;
		ccipImpactPoint = list[list.Count - 1];
		int num = 0;
		int num2 = 0;
		_ = simPoints.Count;
		while (simPoints.Count > 2 && num < 20)
		{
			num++;
			int num3 = Mathf.FloorToInt((float)simPoints.Count * 0.5f);
			num2++;
			if (Physics.Linecast(simPoints[0].ToLocalPosition(), simPoints[num3].ToLocalPosition(), out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				ccipImpactPoint = hitInfo.point.ToGlobalPosition();
				for (int num4 = simPoints.Count - 1; num4 > num3; num4--)
				{
					simPoints.RemoveAt(num4);
				}
				continue;
			}
			num2++;
			Vector3 start = simPoints[num3].ToLocalPosition();
			List<GlobalPosition> list2 = simPoints;
			if (!Physics.Linecast(start, list2[list2.Count - 1].ToLocalPosition(), out var hitInfo2, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				break;
			}
			ccipImpactPoint = hitInfo2.point.ToGlobalPosition();
			for (int num5 = num3 - 1; num5 >= 0; num5--)
			{
				simPoints.RemoveAt(num5);
			}
		}
		return true;
	}

	public override void SetHUDWeaponState(Image targetDesignator, Aircraft aircraft, WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
		weaponInfo = weaponStation.WeaponInfo;
		targetDesignator.color = ThemeManager.Active.ColorTheme.AllClear;
		targetDesignator.transform.localScale = Vector3.one;
		dragCoef = weaponInfo.weaponPrefab.GetComponent<Missile>().GetDragCoef(MathF.PI / 360f);
		finArea = weaponInfo.weaponPrefab.GetComponent<Missile>().GetFinArea();
		mass = weaponInfo.massPerRound;
		SceneSingleton<CameraStateManager>.i.SetDesiredFoV(PlayerSettings.defaultFoV, 0f);
		SceneSingleton<FlightHud>.i.waterline.enabled = true;
		SceneSingleton<FlightHud>.i.velocityVector.transform.localScale = Vector3.one;
	}

	public override void HUDFixedUpdate(Aircraft aircraft, List<Unit> targetList)
	{
	}
}
