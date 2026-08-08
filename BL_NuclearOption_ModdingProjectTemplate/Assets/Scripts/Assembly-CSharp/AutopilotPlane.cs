using NuclearOption.DebugScripts;
using UnityEngine;

public class AutopilotPlane : Autopilot
{
	[SerializeField]
	private bool preventInvertedFlight;

	[SerializeField]
	private AoALimiter aoaLimiter;

	private float onTargetSmoothed;

	private Vector3 targetVelPrev;

	private Vector3 targetAccel;

	private GameObject errorVectorDebug;

	public override void Awake()
	{
		base.Awake();
		aoaLimiter.Initialize(aircraft);
		terrainWarning = new TerrainWarningSystem(aircraft);
	}

	public override void AutoAim(GlobalPosition destination, bool aimVelocity, bool ignoreCollisions, bool runwayAlign, float effort, float bankAllowed, bool followTerrain, float altitudeHold, Vector3 targetVel)
	{
		if (exclusionZoneChecker.TryGetWarning(out var avoidanceVector))
		{
			destination = aircraft.GlobalPosition() + avoidanceVector.normalized * 10000f;
			controlInputs.throttle = 1f;
		}
		Vector3 vector = ((aimVelocity && aircraft.radarAlt > 0.2f) ? aircraft.rb.velocity : aircraft.cockpit.xform.forward);
		if (!aimVelocity && aircraft.weaponManager.currentWeaponStation != null && aircraft.weaponManager.currentWeaponStation.WeaponInfo.gun)
		{
			foreach (Weapon weapon in aircraft.weaponManager.currentWeaponStation.Weapons)
			{
				vector += weapon.transform.forward * 1000f;
			}
		}
		float num = Vector3.Angle(vector, destination - aircraft.GlobalPosition());
		float landingSpeed = aircraftParameters.landingSpeed;
		float airspeed = airspeedChecker.GetAirspeed();
		float num2 = FastMath.Distance(destination, aircraft.GlobalPosition());
		altitudeHold *= Mathf.Lerp(0.1f, 1f, (airspeed - landingSpeed * 0.9f) / landingSpeed);
		float num3 = ((effort > 1f || aircraft.radarAlt < 1f) ? 1f : Mathf.Clamp01(airspeed / aircraftParameters.cornerSpeed));
		if (followTerrain)
		{
			Vector3 direction = Vector3.RotateTowards(new Vector3(vector.x, 0f, vector.z), FastMath.NormalizedDirection(aircraft.GlobalPosition(), destination), 0.5f * num3 * num3, 0f);
			waypoint = terrainWarning.GetFollowTerrainWaypoint(direction, altitudeHold, this);
			waypointDelta = waypoint - aircraft.GlobalPosition();
		}
		else
		{
			Vector3 vector2 = vector.normalized * 1000f;
			waypointDelta = Vector3.RotateTowards(new Vector3(vector2.x, 0f, vector2.z), FastMath.NormalizedDirection(aircraft.GlobalPosition(), destination), 0.9f * num3 * num3, 0f);
		}
		if (num > 60f && num2 < 2000f && aircraft.GlobalPosition().y - destination.y < 1000f)
		{
			waypointDelta += 2000f * Mathf.Clamp01(airspeed / aircraftParameters.cornerSpeed - 1f) * Vector3.up;
		}
		terrainWarning.CheckTerrain();
		if (!ignoreCollisions && terrainWarning.urgency > 0f)
		{
			waypointDelta += terrainWarning.urgency * 100f * Vector3.up;
			targetVel += Vector3.up * 200f;
		}
		Vector3 normalized = waypointDelta.normalized;
		Vector3 vector3 = normalized;
		if (airspeed < landingSpeed)
		{
			vector3 += Vector3.up * 0.5f;
		}
		bool flag = targetVel.sqrMagnitude > 900f;
		vector3 = vector3.normalized;
		if (!flag)
		{
			vector3 += Vector3.up / Mathf.Max(num, 5f);
		}
		vector3 += Mathf.Clamp01(num - 15f) * 0.001f * Vector3.up;
		float num4 = TargetCalc.GetAngleOnAxis(aircraft.cockpit.xform.forward, normalized, aircraft.cockpit.xform.up);
		if (runwayAlign || aircraft.radarAlt < 0.5f)
		{
			num4 = Mathf.Clamp(num4 * 3f, -20f, 20f);
			num4 -= 30f * aircraft.rb.angularVelocity.y;
		}
		if (!aimVelocity && !flag && num < 10f)
		{
			vector3 = Vector3.up + aircraft.cockpit.xform.right * Mathf.Clamp(num4 * 0.25f, -0.3f, 0.3f);
		}
		if (preventInvertedFlight)
		{
			bankAllowed = Mathf.Min(bankAllowed, 135f);
		}
		bankAllowed *= Mathf.Clamp(aircraft.radarAlt * 0.003f - 1f, 0.6f, 1.2f);
		float num5 = normalized.y - aircraft.rb.velocity.y;
		bankAllowed *= Mathf.Clamp(num5 * 0.05f + 1f, 1.2f, 0.5f);
		bankAllowed *= Mathf.Max(num3 * num3, 0.45f);
		float num6 = 0f;
		if (aircraft.radarAlt > 1f)
		{
			if (bankAllowed < 180f)
			{
				float angleOnAxis = TargetCalc.GetAngleOnAxis(aircraft.cockpit.xform.up, Vector3.up, -vector);
				float angleOnAxis2 = TargetCalc.GetAngleOnAxis(vector3, Vector3.up, -vector);
				angleOnAxis2 = Mathf.Clamp(angleOnAxis2, 0f - bankAllowed, bankAllowed);
				num6 = angleOnAxis - angleOnAxis2;
			}
			else
			{
				num6 = TargetCalc.GetAngleOnAxis(aircraft.cockpit.xform.up, vector3, -vector);
			}
		}
		float angleOnAxis3 = TargetCalc.GetAngleOnAxis(vector, normalized, aircraft.cockpit.xform.right);
		angleOnAxis3 *= Mathf.Lerp(1f, 0.1f, Mathf.Abs(num6) * 0.015f);
		if (num > 20f && !runwayAlign && aircraft.radarAlt > 0.5f)
		{
			num4 = 0f;
		}
		forwardFlightController.ApplyInputs(controlInputs, airspeed, new Vector3(angleOnAxis3, num4, num6));
		aircraft.FilterInputs();
		if (DebugVis.Enabled)
		{
			DebugVis.Create(ref waypointDebug, GameAssets.i.waypointDebug, Datum.origin);
			DebugVis.Create(ref aimVectorDebug, GameAssets.i.debugArrow, aircraft.cockpit.transform);
			DebugVis.Create(ref vectorDebug2, GameAssets.i.debugArrow, Datum.origin);
			DebugVis.Create(ref errorVectorDebug, GameAssets.i.debugArrow, aircraft.transform);
			if (SceneSingleton<CameraStateManager>.i.followingUnit == aircraft)
			{
				errorVectorDebug.transform.position = aircraft.transform.position + aircraft.transform.forward * 10f;
				errorVectorDebug.transform.rotation = Quaternion.LookRotation(vector3);
				errorVectorDebug.transform.localScale = new Vector3(2f, 2f, 5f);
				waypointDebug.SetActive(value: true);
				aimVectorDebug.SetActive(value: true);
				waypointDebug.transform.position = aircraft.transform.position + waypointDelta;
				waypointDebug.transform.rotation = Quaternion.LookRotation(waypointDelta);
				waypointDebug.transform.localScale = Vector3.one * 2f;
				aimVectorDebug.transform.localPosition = Vector3.zero;
				aimVectorDebug.transform.rotation = Quaternion.LookRotation(waypointDelta);
				aimVectorDebug.transform.localScale = new Vector3(2f, 2f, 1000f);
			}
			else
			{
				waypointDebug.SetActive(value: false);
				aimVectorDebug.SetActive(value: false);
			}
		}
	}
}
