using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class AIPilotLandingState : PilotBaseState
{
	private enum LandingMode
	{
		Joining_Pattern = 0,
		Turning_to_Final = 1,
		Stabilized_Approach = 2,
		Vertical_Touchdown = 3,
		Touched_Down = 4,
		Aborting_Landing = 5
	}

	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIPilotLandingState.FixedUpdateState");

	private LandingMode landingMode;

	private Airbase airbase;

	private AircraftParameters aircraftParameters;

	private GameObject aimVis;

	private GameObject nextWaypointDebug;

	private Airbase.Runway.RunwayUsage runwayUsage;

	private ControlsFilter controlsFilter;

	private Vector3 glideslopeCorrection;

	private Transform runwayExitPoint;

	private List<Missile> missileAlerts;

	private float timeOnGround;

	private float touchdownTime;

	private float alignedTime;

	private float adjustedLandingSpeed;

	private float hoverTargetHeight;

	private bool requestedLanding;

	private bool reachedFinal;

	private bool hasTailHook;

	public override void EnterState(Pilot pilot)
	{
		pilot.flightInfo.HasTakenOff = true;
		aircraft = pilot.aircraft;
		aircraftParameters = aircraft.GetAircraftParameters();
		pilot.On2sCheck += LandingState_CheckMode;
		pilot.On10sCheck += LandingState_SearchAirbase;
		pilot.On5sCheck += EjectionCheck;
		controlsFilter = aircraft.GetControlsFilter();
		base.pilot = pilot;
		controlInputs = aircraft.GetInputs();
		missileAlerts = aircraft.GetMissileWarningSystem().knownMissiles;
		if (fuelChecker == null)
		{
			fuelChecker = new FuelChecker(aircraft);
		}
		alignedTime = 0f;
		timeOnGround = 0f;
		requestedLanding = false;
		reachedFinal = false;
		hasTailHook = aircraft.weaponManager.HasTailHook();
		runwayExitPoint = null;
		SwitchMode(LandingMode.Joining_Pattern);
		stateDisplayName = "Joining Pattern";
		LandingState_SearchAirbase();
		if (PlayerSettings.debugVis)
		{
			aimVis = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugPoint, Datum.origin);
			aimVis.transform.localScale = Vector3.one * 2f;
		}
	}

	private void LandingState_SearchAirbase()
	{
		if (landingMode != LandingMode.Joining_Pattern)
		{
			return;
		}
		float mass = aircraft.GetMass();
		float maxWeight = aircraft.definition.aircraftInfo.maxWeight;
		adjustedLandingSpeed = Mathf.Sqrt(mass / maxWeight) * aircraftParameters.landingSpeed;
		RunwayQuery runwayQuery = new RunwayQuery
		{
			RunwayType = RunwayQueryType.Landing,
			MinSize = (aircraftParameters.verticalLanding ? aircraft.definition.length : aircraftParameters.takeoffDistance),
			LandingSpeed = (aircraftParameters.verticalLanding ? 0f : adjustedLandingSpeed),
			TailHook = aircraft.weaponManager.HasTailHook()
		};
		airbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position, runwayQuery);
		Airbase.Runway.RunwayUsage? runwayUsage = ((airbase != null) ? airbase.RequestLanding(aircraft, runwayQuery) : ((Airbase.Runway.RunwayUsage?)null));
		if (runwayUsage.HasValue)
		{
			this.runwayUsage = runwayUsage.Value;
			if (aircraftParameters.verticalLanding)
			{
				adjustedLandingSpeed = (airbase.AttachedAirbase ? 90 : 90);
			}
		}
		else
		{
			aircraft.StartEjectionSequence();
			pilot.SwitchState(null);
		}
	}

	private void JoinPattern(bool checkMode)
	{
		Vector3 normalized = runwayUsage.Runway.GetDirection(runwayUsage.Reverse).normalized;
		Vector3 glideslopeAimpoint = runwayUsage.GetGlideslopeAimpoint(aircraft, aircraftParameters.turningRadius * 3f, 30f);
		Vector3 vector = glideslopeAimpoint - aircraft.transform.position;
		float num = (Mathf.Sin((Vector3.Angle(vector, normalized) - 90f) * (MathF.PI / 180f)) + 1f) * aircraftParameters.turningRadius * 2f;
		glideslopeAimpoint += Vector3.RotateTowards(-normalized * num, -vector, MathF.PI / 2f, 0f);
		Vector3 rhs = glideslopeAimpoint - aircraft.cockpit.xform.position;
		if (checkMode)
		{
			if (FastMath.InRange(glideslopeAimpoint.ToGlobalPosition(), aircraft.GlobalPosition(), aircraftParameters.turningRadius) && Vector3.Dot(aircraft.transform.forward, rhs) < 0f)
			{
				SwitchMode(LandingMode.Turning_to_Final);
			}
		}
		else
		{
			aircraft.autopilot.AutoAim(glideslopeAimpoint.ToGlobalPosition(), aimVelocity: false, ignoreCollisions: false, runwayAlign: false, 0.9f, 135f, followTerrain: true, aircraftParameters.turningRadius * 3f * 0.05f, Vector3.zero);
			float num2 = aircraftParameters.cornerSpeed + FastMath.Distance(aircraft.GlobalPosition(), glideslopeAimpoint.ToGlobalPosition()) * 0.02f;
			float num3 = aircraft.speed - num2;
			controlInputs.throttle = Mathf.Clamp(0.5f - num3 * 0.1f, 0f, aircraftParameters.cruiseThrottle);
		}
	}

	private void FinalTurn(bool checkMode)
	{
		GlobalPosition touchdownPoint = runwayUsage.GetTouchdownPoint();
		float num = FastMath.Distance(touchdownPoint, aircraft.cockpit.xform.GlobalPosition());
		GlobalPosition globalPosition = runwayUsage.GetGlideslopeAimpoint(aircraft, aircraftParameters.turningRadius * 3f, 30f).ToGlobalPosition();
		if (checkMode)
		{
			if (Vector3.Angle(touchdownPoint - aircraft.GlobalPosition(), aircraft.transform.forward) < 10f)
			{
				alignedTime += 2f;
				if (alignedTime > 2f)
				{
					SwitchMode(LandingMode.Stabilized_Approach);
				}
			}
			return;
		}
		GlobalPosition globalPosition2 = globalPosition;
		if (FastMath.InRange(globalPosition, aircraft.GlobalPosition(), aircraftParameters.turningRadius * 0.5f))
		{
			reachedFinal = true;
		}
		if (reachedFinal)
		{
			globalPosition2 = touchdownPoint + num * 0.05f * Vector3.up;
		}
		float num2 = adjustedLandingSpeed + 0.015f * Mathf.Max(num - 500f, 0f);
		float num3 = aircraft.speed - num2;
		controlInputs.throttle = Mathf.Clamp(0.5f - num3 * 0.1f, 0f, aircraftParameters.cruiseThrottle);
		aircraft.autopilot.AutoAim(globalPosition2, aimVelocity: false, ignoreCollisions: false, runwayAlign: false, 1.1f, 135f, followTerrain: false, num * 0.05f, Vector3.zero);
	}

	private void StabilizedApproach(bool checkMode)
	{
		GlobalPosition touchdownPoint = runwayUsage.GetTouchdownPoint();
		float num = FastMath.Distance(touchdownPoint, aircraft.GlobalPosition());
		if (checkMode)
		{
			RunwayQuery query = new RunwayQuery
			{
				RunwayType = RunwayQueryType.Landing,
				MinSize = (aircraftParameters.verticalLanding ? 0f : aircraftParameters.takeoffDistance),
				LandingSpeed = (aircraftParameters.verticalLanding ? 0f : adjustedLandingSpeed),
				TailHook = aircraft.weaponManager.HasTailHook()
			};
			if (!runwayUsage.Runway.IsSuitable(query))
			{
				SwitchMode(LandingMode.Aborting_Landing);
				return;
			}
			if (aircraft.gearState == LandingGear.GearState.LockedRetracted)
			{
				aircraft.SetGear(deployed: true);
			}
			if (!requestedLanding && FastMath.InRange(touchdownPoint, aircraft.GlobalPosition(), 2500f))
			{
				requestedLanding = true;
				airbase.RpcRegisterUsage(aircraft, isUsing: true, runwayUsage.Runway.index);
			}
			if (aircraftParameters.verticalLanding)
			{
				return;
			}
			{
				foreach (Aircraft landing in runwayUsage.Runway.GetLandingList())
				{
					if (landing == null || landing == aircraft)
					{
						continue;
					}
					Vector3 vector = aircraft.rb.velocity - landing.rb.velocity;
					Vector3 lhs = landing.transform.position - aircraft.transform.position;
					if (!(Vector3.Dot(lhs, aircraft.transform.forward) < 0f))
					{
						float magnitude = vector.magnitude;
						float magnitude2 = lhs.magnitude;
						if (magnitude > (magnitude2 - 200f) * 0.1f)
						{
							SwitchMode(LandingMode.Aborting_Landing);
							break;
						}
					}
				}
				return;
			}
		}
		if (touchdownTime < 10f)
		{
			GlobalPosition nearestGlideslopePoint = runwayUsage.GetNearestGlideslopePoint(aircraft.GlobalPosition(), aircraft.definition.spawnOffset.y, touchdownTime);
			Vector3 vector2 = aircraft.GlobalPosition() - nearestGlideslopePoint;
			if (glideslopeCorrection == Vector3.zero)
			{
				glideslopeCorrection = new Vector3(0f, Mathf.Clamp(vector2.y, -1f, 1f), 0f);
			}
			else
			{
				glideslopeCorrection += new Vector3(Mathf.Clamp(vector2.x, -4f, 4f) * 0.2f, Mathf.Clamp(vector2.y, -1f, 1f) * 0.2f, Mathf.Clamp(vector2.z, -4f, 4f) * 0.2f) * Time.fixedDeltaTime;
			}
		}
		else
		{
			glideslopeCorrection = Vector3.zero;
		}
		Vector3 vector3 = touchdownPoint - aircraft.GlobalPosition();
		float num2 = Vector3.Dot(aircraft.rb.velocity - runwayUsage.Runway.GetVelocity(), vector3.normalized);
		touchdownTime = Mathf.Min(num / num2, 30f);
		if (aircraft.radarAlt < 0.1f)
		{
			SwitchMode(LandingMode.Touched_Down);
			return;
		}
		if (runwayUsage.Runway.GetVelocity() != Vector3.zero)
		{
			vector3 = touchdownPoint + runwayUsage.Runway.GetVelocity() * touchdownTime - aircraft.GlobalPosition();
			num = vector3.magnitude;
		}
		Vector3 vector4 = new Vector3(vector3.x, 0f, vector3.z);
		Vector3 normalized = runwayUsage.Runway.GetDirection(runwayUsage.Reverse).normalized;
		float num3 = Vector3.Dot(vector4.normalized, normalized);
		float num4 = Vector3.Dot(new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z).normalized, normalized);
		if ((num3 < 0f || num4 < 0f) && aircraft.gearState == LandingGear.GearState.LockedExtended)
		{
			num *= -1f;
		}
		float num5 = adjustedLandingSpeed + 0.015f * Mathf.Max(num - 500f, 0f);
		float num6 = 200f + num * 0.2f;
		GlobalPosition nearestGlideslopePoint2 = runwayUsage.GetNearestGlideslopePoint(aircraft.GlobalPosition() + aircraft.transform.forward * num6, aircraft.definition.spawnOffset.y, touchdownTime);
		nearestGlideslopePoint2 -= glideslopeCorrection;
		float num7 = runwayUsage.GetGlideslopeError(aircraft, touchdownTime);
		Vector3 rhs = aircraft.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(aircraft.GlobalPosition());
		float num8 = Vector3.Dot(aircraft.transform.forward, rhs);
		controlInputs.throttle = Mathf.Clamp((num5 + 5f - num8) * 0.1f, 0f, aircraftParameters.cruiseThrottle);
		if (aircraftParameters.verticalLanding)
		{
			float magnitude3 = runwayUsage.Runway.GetVelocity().magnitude;
			float num9 = Mathf.Min(Mathf.Sqrt(runwayUsage.Runway.Length) * 1f, 50f) + Mathf.Sqrt(Mathf.Max(num - 50f, 0f)) * 0.7f + magnitude3;
			float num10 = aircraft.speed - num9;
			float num11 = 10f;
			if (runwayUsage.Runway.Length < 250f)
			{
				num7 -= num11;
				nearestGlideslopePoint2 += Vector3.up * num11;
				if (touchdownTime < 4f)
				{
					SwitchMode(LandingMode.Vertical_Touchdown);
					return;
				}
			}
			controlInputs.customAxis1 = 0.6f - num10 * 0.05f;
			controlInputs.throttle = 0.8f - num10 * 0.05f;
			controlInputs.throttle += Mathf.Clamp01(0.5f - num7 * 0.1f);
			if (controlsFilter.ReverseThrust && num10 > 10f)
			{
				controlInputs.throttle = 1f;
			}
			controlInputs.throttle = Mathf.Max(controlInputs.throttle, 0.6f);
			if (aircraft.speed > 130f)
			{
				controlInputs.throttle = 0f;
			}
			if (touchdownTime < 10f)
			{
				controlInputs.throttle = Mathf.Max(0.8f - num7 * 0.05f, 0.6f);
			}
		}
		float num12 = (aircraftParameters.verticalLanding ? 1f : 2f);
		if (touchdownTime < num12 && (!hasTailHook || !runwayUsage.Runway.Arrestor))
		{
			controlInputs.throttle = 0f;
			nearestGlideslopePoint2 += Vector3.up * 5f;
		}
		aircraft.autopilot.AutoAim(nearestGlideslopePoint2, aimVelocity: true, ignoreCollisions: true, touchdownTime < 5f, 1.01f, 65f, followTerrain: false, 0f, Vector3.zero);
		if (aimVis != null)
		{
			aimVis.transform.localPosition = nearestGlideslopePoint2.AsVector3();
		}
	}

	private void VerticalTouchdown(bool checkMode)
	{
		if (checkMode)
		{
			return;
		}
		Vector3 direction = runwayUsage.Runway.GetDirection(runwayUsage.Reverse);
		GlobalPosition globalPosition = runwayUsage.Runway.GetNearestPoint(aircraft.transform, extend: false).ToGlobalPosition();
		Vector3 vector = aircraft.GlobalPosition() - globalPosition;
		vector.y = 0f;
		_ = vector.magnitude;
		if (!controlsFilter.IsAutoHoverEnabled())
		{
			controlsFilter.SetAutoHover(enabled: true);
		}
		if (aircraft.radarAlt < 0.5f)
		{
			SwitchMode(LandingMode.Touched_Down);
		}
		if (Vector3.Dot(aircraft.cockpit.xform.forward, runwayUsage.GetEnd().position - aircraft.transform.position) < 0f)
		{
			SwitchMode(LandingMode.Aborting_Landing);
			return;
		}
		if (Vector3.Dot(aircraft.cockpit.xform.forward, runwayUsage.GetTouchdownPoint() - aircraft.GlobalPosition()) < 0f)
		{
			hoverTargetHeight -= 4f * Time.fixedDeltaTime;
		}
		else
		{
			hoverTargetHeight = 10f;
		}
		aircraft.autopilot.Hover(globalPosition, hoverTargetHeight, direction);
	}

	private void TouchedDown(bool checkMode)
	{
		timeOnGround += Time.fixedDeltaTime;
		if (checkMode)
		{
			if (!(runwayExitPoint == null))
			{
				return;
			}
			runwayUsage.Runway.TryGetExitTaxiPoint(aircraft.transform, aircraft.speed, out runwayExitPoint);
			if (!aircraftParameters.verticalLanding && aircraft.speed < 15f)
			{
				if (pilot.AITaxiState == null)
				{
					pilot.AITaxiState = new AIPilotTaxiState();
				}
				pilot.SwitchState(pilot.AITaxiState);
			}
			return;
		}
		if (aircraft.radarAlt > 1f)
		{
			timeOnGround = 0f;
			SwitchMode(LandingMode.Aborting_Landing);
		}
		if (timeOnGround > 10f && (!runwayUsage.Runway.AircraftOnRunway(aircraft) || aircraft.speed < 1f))
		{
			if (aircraft.speed < 1f)
			{
				pilot.aircraft.StartEjectionSequence();
			}
			controlInputs.brake = 1f;
			controlInputs.throttle = 0f;
			return;
		}
		controlInputs.brake = Mathf.Clamp01(timeOnGround * 2f);
		controlInputs.throttle = 0f;
		controlInputs.customAxis1 = 1f;
		if (hasTailHook && airbase.AttachedAirbase && timeOnGround < 3f)
		{
			controlInputs.throttle = 1f;
			controlInputs.brake = 0f;
		}
		Vector3 position = runwayUsage.Runway.GetNearestPoint(aircraft.transform.position, extend: false) + runwayUsage.Runway.GetDirection(runwayUsage.Reverse).normalized * 100f;
		GlobalPosition position2 = runwayUsage.GetGlideslopeAimpoint(aircraft, 300f, touchdownTime).ToGlobalPosition();
		position.y = position2.ToLocalPosition().y;
		if (runwayExitPoint != null && timeOnGround > 2f)
		{
			float num = Vector3.Distance(runwayExitPoint.position, aircraft.transform.position);
			position2 = runwayExitPoint.position.ToGlobalPosition();
			float num2 = 15f + Mathf.Sqrt(Mathf.Max(num - 30f, 1f));
			if (runwayUsage.Runway.airbase.AttachedAirbase)
			{
				num2 *= 0.5f;
			}
			controlInputs.throttle = ((aircraft.speed < num2) ? 0.5f : 0f);
			controlInputs.brake = ((aircraft.speed > num2) ? 1 : 0);
			if (num < aircraft.maxRadius + 20f)
			{
				if (pilot.AITaxiState == null)
				{
					pilot.AITaxiState = new AIPilotTaxiState();
				}
				runwayUsage.Runway.DeregisterLanding(aircraft);
				pilot.SwitchState(pilot.AITaxiState);
				return;
			}
		}
		position2 = position.ToGlobalPosition();
		aircraft.autopilot.AutoAim(position2, aimVelocity: false, ignoreCollisions: true, runwayAlign: true, 1.01f, 5f, followTerrain: false, 0f, Vector3.zero);
	}

	private void AbortingLanding(bool checkMode)
	{
		if (checkMode)
		{
			if (airbase != null && requestedLanding)
			{
				airbase.RpcRegisterUsage(aircraft, isUsing: false, runwayUsage.Runway.index);
				requestedLanding = false;
			}
			if (controlsFilter.IsAutoHoverEnabled())
			{
				controlsFilter.SetAutoHover(enabled: false);
			}
			if (aircraft.speed > aircraftParameters.cornerSpeed)
			{
				pilot.SwitchState(pilot.AICombatState);
			}
		}
		else
		{
			Vector3 velocity = aircraft.rb.velocity;
			velocity.y = 0f;
			GlobalPosition globalPosition = aircraft.GlobalPosition() + velocity.normalized * 1000f + Vector3.up * 8f;
			aircraft.autopilot.AutoAim(globalPosition, aimVelocity: true, ignoreCollisions: true, runwayAlign: false, 0.95f, 135f, followTerrain: true, 200f, Vector3.zero);
			controlInputs.throttle = 1f;
			controlInputs.customAxis1 = 1f;
			aircraft.SetFlightAssist(enabled: true);
			aircraft.SetGear(deployed: false);
		}
	}

	private void LandingState_CheckMode()
	{
		if (aircraft.radarAlt > 30f && missileAlerts.Count > 0 && fuelChecker.HasEnoughFuel())
		{
			if (airbase != null && requestedLanding)
			{
				airbase.RpcRegisterUsage(aircraft, isUsing: false, runwayUsage.Runway.index);
				requestedLanding = false;
			}
			pilot.SwitchState(pilot.AICombatState);
			return;
		}
		switch (landingMode)
		{
		case LandingMode.Joining_Pattern:
			JoinPattern(checkMode: true);
			break;
		case LandingMode.Turning_to_Final:
			FinalTurn(checkMode: true);
			break;
		case LandingMode.Stabilized_Approach:
			StabilizedApproach(checkMode: true);
			break;
		case LandingMode.Vertical_Touchdown:
			VerticalTouchdown(checkMode: true);
			break;
		case LandingMode.Touched_Down:
			TouchedDown(checkMode: true);
			break;
		case LandingMode.Aborting_Landing:
			AbortingLanding(checkMode: true);
			break;
		}
	}

	private void RunMode()
	{
		switch (landingMode)
		{
		case LandingMode.Joining_Pattern:
			JoinPattern(checkMode: false);
			break;
		case LandingMode.Turning_to_Final:
			FinalTurn(checkMode: false);
			break;
		case LandingMode.Stabilized_Approach:
			StabilizedApproach(checkMode: false);
			break;
		case LandingMode.Vertical_Touchdown:
			VerticalTouchdown(checkMode: false);
			break;
		case LandingMode.Touched_Down:
			TouchedDown(checkMode: false);
			break;
		case LandingMode.Aborting_Landing:
			AbortingLanding(checkMode: false);
			break;
		}
	}

	private void SwitchMode(LandingMode newMode)
	{
		if (landingMode != newMode)
		{
			landingMode = newMode;
			stateDisplayName = landingMode.ToString().Replace("_", " ");
			if (newMode == LandingMode.Aborting_Landing)
			{
				runwayUsage.Runway.DeregisterLanding(aircraft);
			}
		}
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			RunMode();
		}
	}

	private void EjectionCheck()
	{
		if (aircraft.cockpit.xform.position.y < Datum.LocalSeaY || ((landingMode == LandingMode.Joining_Pattern || landingMode == LandingMode.Turning_to_Final || landingMode == LandingMode.Aborting_Landing) && aircraft.speed < 1f && aircraft.radarAlt < 5f) || (aircraft.radarAlt > 40f && (Vector3.Dot(aircraft.cockpit.xform.forward, aircraft.rb.velocity) < 0f || aircraft.partDamageTracker.GetDetachedRatio() > 0.12f)))
		{
			pilot.aircraft.StartEjectionSequence();
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	public override void LeaveState()
	{
		landingMode = LandingMode.Joining_Pattern;
		pilot.On2sCheck -= LandingState_CheckMode;
		pilot.On10sCheck -= LandingState_SearchAirbase;
		pilot.On5sCheck -= EjectionCheck;
	}
}
