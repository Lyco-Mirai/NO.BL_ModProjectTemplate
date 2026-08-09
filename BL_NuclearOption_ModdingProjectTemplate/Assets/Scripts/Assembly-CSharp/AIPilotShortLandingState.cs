using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class AIPilotShortLandingState : PilotBaseState
{
	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIPilotShortLandingState.FixedUpdateState");

	private Airbase airbase;

	private AircraftParameters aircraftParameters;

	private GameObject aimVis;

	private GameObject nextWaypointDebug;

	private Airbase.Runway.RunwayUsage runwayUsage;

	private ControlsFilter controlsFilter;

	private Transform runwayExitPoint;

	private List<Missile> missileAlerts;

	private float timeOnGround;

	private float lastSlowCheck;

	private float lastExitPointCheck;

	private float lastAirbaseSearch;

	private float speedAdjustment;

	private float stuckTimer;

	private float lastEjectionCheck;

	private float touchdownSpeed;

	private float approachSpeed;

	private float hoverTargetHeight;

	private float adjustedShortLandingSpeed;

	private bool requestedLanding;

	private bool startedRollingVerticalApproach;

	private bool touchedDown;

	private bool reachedApproachPoint;

	private bool runwayExcursion;

	private bool abortingLanding;

	private bool stationaryOnGround;

	public override void EnterState(Pilot pilot)
	{
		pilot.flightInfo.HasTakenOff = true;
		aircraft = pilot.aircraft;
		aircraftParameters = aircraft.GetAircraftParameters();
		controlsFilter = aircraft.GetControlsFilter();
		controlsFilter.SetAutoHover(enabled: false);
		stateDisplayName = "Short Landing";
		base.pilot = pilot;
		controlInputs = aircraft.GetInputs();
		missileAlerts = aircraft.GetMissileWarningSystem().knownMissiles;
		timeOnGround = 0f;
		stuckTimer = 0f;
		requestedLanding = false;
		touchedDown = false;
		reachedApproachPoint = false;
		runwayExcursion = false;
		abortingLanding = false;
		runwayExitPoint = null;
		approachSpeed = Mathf.Lerp(aircraftParameters.takeoffSpeed, aircraftParameters.cornerSpeed, 0.5f);
		touchdownSpeed = 25f;
		SearchBestAirbase();
		if (PlayerSettings.debugVis)
		{
			aimVis = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugPoint, Datum.origin);
			aimVis.transform.localScale = Vector3.one * 2f;
		}
	}

	private void SearchBestAirbase()
	{
		lastAirbaseSearch = Time.timeSinceLevelLoad;
		float mass = aircraft.GetMass();
		float maxWeight = aircraft.definition.aircraftInfo.maxWeight;
		speedAdjustment = Mathf.Sqrt(mass / maxWeight);
		float num = speedAdjustment * aircraftParameters.takeoffSpeed;
		RunwayQuery runwayQuery = new RunwayQuery
		{
			RunwayType = RunwayQueryType.Landing,
			MinSize = (aircraftParameters.verticalLanding ? aircraft.definition.length : aircraftParameters.takeoffDistance),
			LandingSpeed = (aircraftParameters.verticalLanding ? 0f : num),
			TailHook = aircraft.weaponManager.HasTailHook()
		};
		airbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position, runwayQuery);
		Airbase.Runway.RunwayUsage? runwayUsage = ((airbase != null) ? airbase.RequestLanding(aircraft, runwayQuery) : ((Airbase.Runway.RunwayUsage?)null));
		if (runwayUsage.HasValue)
		{
			this.runwayUsage = runwayUsage.Value;
			return;
		}
		aircraft.StartEjectionSequence();
		pilot.SwitchState(null);
	}

	private void CheckApproachParameters()
	{
		if (Time.timeSinceLevelLoad - lastSlowCheck < 2f)
		{
			return;
		}
		lastSlowCheck = Time.timeSinceLevelLoad;
		if (timeOnGround > 2f)
		{
			runwayExcursion = !runwayUsage.Runway.AircraftOnRunway(aircraft) || (timeOnGround > 10f && aircraft.speed < 1f);
		}
		if (runwayExcursion && aircraft.speed > aircraftParameters.takeoffSpeed * 0.5f && !aircraft.IsAutoHoverEnabled())
		{
			abortingLanding = true;
			touchedDown = false;
			runwayExcursion = false;
			reachedApproachPoint = false;
		}
		else
		{
			if (aircraft.radarAlt < 10f)
			{
				return;
			}
			if (runwayUsage.Runway.IsSuitable(default(RunwayQuery)))
			{
				adjustedShortLandingSpeed = Mathf.Pow(aircraft.GetMass() / aircraft.definition.aircraftInfo.maxWeight, 2f) * aircraftParameters.shortLandingSpeed;
				{
					foreach (Aircraft landing in runwayUsage.Runway.GetLandingList())
					{
						if (landing == null || landing == aircraft)
						{
							continue;
						}
						Vector3 to = aircraft.rb.velocity - landing.rb.velocity;
						Vector3 vector = landing.transform.position - aircraft.transform.position;
						if (!(Vector3.Dot(vector, aircraft.transform.forward) < 0f))
						{
							float magnitude = to.magnitude;
							float magnitude2 = vector.magnitude;
							if (magnitude > (magnitude2 - 200f) * 0.1f && Vector3.Angle(vector, to) < 10f)
							{
								abortingLanding = true;
							}
						}
					}
					return;
				}
			}
			abortingLanding = true;
		}
	}

	public bool TrySetExitPoint(Airbase.Runway runway)
	{
		if (Time.timeSinceLevelLoad - lastExitPointCheck > 10f)
		{
			lastExitPointCheck = Time.timeSinceLevelLoad;
			runway.TryGetExitTaxiPoint(aircraft.transform, aircraft.speed, out runwayExitPoint);
		}
		return runwayExitPoint != null;
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			if (abortingLanding)
			{
				Vector3 velocity = aircraft.rb.velocity;
				velocity.y = 0f;
				GlobalPosition globalPosition = aircraft.GlobalPosition() + velocity.normalized * 1000f + Vector3.up * 8f;
				aircraft.autopilot.AutoAim(globalPosition, aimVelocity: true, ignoreCollisions: false, runwayAlign: false, 0.95f, 45f, followTerrain: false, 100f, Vector3.zero);
				controlInputs.throttle = 1f;
				controlInputs.customAxis1 = 1f;
				aircraft.SetFlightAssist(enabled: true);
				aircraft.GetControlsFilter().SetAutoHover(enabled: false);
				aircraft.SetGear(deployed: false);
				runwayUsage.Runway.DeregisterLanding(aircraft);
				if (aircraft.speed > aircraftParameters.cornerSpeed)
				{
					pilot.SwitchState(pilot.AICombatState);
				}
				return;
			}
			if (missileAlerts.Count > 0)
			{
				abortingLanding = true;
				return;
			}
			if (aircraft.speed < 1f)
			{
				stuckTimer += Time.deltaTime;
				if (stuckTimer > 5f)
				{
					pilot.aircraft.StartEjectionSequence();
					pilot.SwitchState(pilot.parkedState);
				}
			}
			if (runwayExcursion)
			{
				runwayUsage.Runway.DeregisterLanding(aircraft);
				controlInputs.throttle = 0f;
				controlInputs.brake = 1f;
				if (aircraft.speed < 1f)
				{
					pilot.aircraft.StartEjectionSequence();
					pilot.SwitchState(pilot.parkedState);
				}
				return;
			}
			EjectionCheck();
			CheckApproachParameters();
			Vector3 normalized = runwayUsage.Runway.GetDirection(runwayUsage.Reverse).normalized;
			Transform transform = (runwayUsage.Reverse ? runwayUsage.Runway.End : runwayUsage.Runway.Start);
			Vector3 vector = transform.position - aircraft.transform.position;
			float num = vector.magnitude;
			_ = aircraft.transform.position;
			_ = transform.position;
			_ = aircraft.definition.spawnOffset;
			float num2 = Vector3.Dot(aircraft.rb.velocity - runwayUsage.Runway.GetVelocity(), vector.normalized);
			float num3 = Mathf.Min(num / num2, 30f);
			if (runwayUsage.Runway.GetVelocity() != Vector3.zero)
			{
				vector = transform.position + runwayUsage.Runway.GetVelocity() * num3 - aircraft.transform.position;
				num = vector.magnitude;
			}
			float num4 = Vector3.Dot(new Vector3(vector.x, 0f, vector.z).normalized, normalized);
			float num5 = Vector3.Dot(new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z).normalized, normalized);
			if ((num4 < 0.5f || num5 < 0.5f) && aircraft.gearState == LandingGear.GearState.LockedExtended)
			{
				num *= -1f;
			}
			_ = aircraftParameters.takeoffSpeed;
			_ = speedAdjustment;
			if (num5 > 0.95f && num4 > 0.95f)
			{
				_ = aircraftParameters.takeoffSpeed;
				_ = speedAdjustment;
				Mathf.Max(num - 700f, 0f);
				if (aircraft.gearState == LandingGear.GearState.LockedRetracted)
				{
					aircraft.SetGear(deployed: true);
				}
				if (!requestedLanding && num < 2000f)
				{
					requestedLanding = true;
					airbase.RpcRegisterUsage(aircraft, isUsing: true, runwayUsage.Runway.index);
				}
			}
			controlInputs.brake = (touchedDown ? (controlInputs.brake + 2f * Time.deltaTime) : 0f);
			if (aircraft.radarAlt < 0.5f)
			{
				touchedDown = true;
			}
			GlobalPosition globalPosition2 = runwayUsage.GetGlideslopeAimpoint(aircraft, num - Mathf.Lerp(200f, 700f, num * 0.0002f), num3).ToGlobalPosition();
			float glideslopeError = runwayUsage.GetGlideslopeError(aircraft, num3);
			if (!touchedDown)
			{
				if (!aircraft.flightAssist)
				{
					aircraft.SetFlightAssist(enabled: true);
				}
				float num6 = runwayUsage.Runway.GetVelocity().magnitude + 2f * Mathf.Sqrt(runwayUsage.Runway.Length);
				float a = Mathf.Sqrt(num6 * num6 + 3f * num);
				a = Mathf.Max(a, adjustedShortLandingSpeed);
				float num7 = aircraft.speed - a;
				float num8 = 0.35f - num7 * 0.1f;
				controlInputs.customAxis1 = ((aircraft.speed > 130f) ? 0f : num8);
				if (aircraft.IsAutoHoverEnabled())
				{
					Vector3 direction = runwayUsage.Runway.GetDirection(runwayUsage.Reverse);
					GlobalPosition globalPosition3 = ((runwayUsage.Runway.Start.position + runwayUsage.Runway.End.position) * 0.5f).ToGlobalPosition();
					Vector3 vector2 = aircraft.GlobalPosition() - globalPosition3;
					vector2.y = 0f;
					stateDisplayName = "Short Landing Touchdown";
					if (Vector3.Dot(aircraft.cockpit.xform.forward, runwayUsage.Runway.Start.position - aircraft.transform.position) < 0f)
					{
						hoverTargetHeight -= 2f * Time.fixedDeltaTime;
					}
					else
					{
						hoverTargetHeight = 10f;
					}
					aircraft.autopilot.Hover(globalPosition3, hoverTargetHeight, direction);
					return;
				}
				stateDisplayName = "Short Landing Approach";
				globalPosition2 += Vector3.up * 10f;
				float num9 = num7 * -0.1f;
				if (controlsFilter.ReverseThrust)
				{
					num9 = Mathf.Max(num9, 0f);
				}
				controlInputs.throttle = 0.8f + num9;
				if (num < 500f && (num3 < 5f || aircraft.GlobalPosition().y < globalPosition2.y))
				{
					hoverTargetHeight = 15f;
					aircraft.GetControlsFilter().SetAutoHover(enabled: true);
				}
			}
			if (touchedDown)
			{
				controlInputs.customAxis1 = 1f;
				controlInputs.throttle = 0f;
				timeOnGround += Time.deltaTime;
				Vector3 position = runwayUsage.Runway.GetNearestPoint(aircraft.transform.position, extend: false) + runwayUsage.Runway.GetDirection(runwayUsage.Reverse).normalized * 100f;
				position.y = globalPosition2.ToLocalPosition().y;
				bool flag = TrySetExitPoint(runwayUsage.Runway);
				if (flag && timeOnGround > 2f)
				{
					float num10 = Vector3.Distance(runwayExitPoint.position, aircraft.transform.position);
					globalPosition2 = runwayExitPoint.position.ToGlobalPosition();
					float num11 = 15f + Mathf.Sqrt(Mathf.Max(num10 - 30f, 1f));
					if (runwayUsage.Runway.airbase.AttachedAirbase)
					{
						num11 *= 0.5f;
					}
					controlInputs.throttle = ((aircraft.speed < num11) ? 0.5f : 0f);
					controlInputs.brake = ((aircraft.speed > num11) ? 1 : 0);
					if (num10 < aircraft.maxRadius + 20f)
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
				if (!flag && aircraft.speed < 15f)
				{
					if (pilot.AITaxiState == null)
					{
						pilot.AITaxiState = new AIPilotTaxiState();
					}
					pilot.SwitchState(pilot.AITaxiState);
				}
				else
				{
					globalPosition2 = position.ToGlobalPosition();
					SetAutopilot(globalPosition2, num, alignWithRunway: true);
				}
			}
			else
			{
				SetAutoLand(globalPosition2, glideslopeError, num, num3 < 5f);
			}
		}
	}

	private void SetAutopilot(GlobalPosition aimpoint, float touchdownDist, bool alignWithRunway)
	{
		aircraft.autopilot.AutoAim(aimpoint, aimVelocity: true, ignoreCollisions: false, alignWithRunway, 0.95f, 135f, followTerrain: true, 0.06f * touchdownDist, Vector3.zero);
		if (aimVis != null)
		{
			aimVis.transform.localPosition = aimpoint.AsVector3();
		}
	}

	private void SetAutoLand(GlobalPosition aimpoint, float heightError, float touchdownDist, bool alignWithRunway)
	{
		aircraft.autopilot.AutoAim(aimpoint, aimVelocity: true, ignoreCollisions: false, alignWithRunway, 0.95f, 135f, followTerrain: false, 0.05f * touchdownDist, Vector3.zero);
		if (aimVis != null)
		{
			aimVis.transform.localPosition = aimpoint.AsVector3();
		}
	}

	private void EjectionCheck()
	{
		if (!(Time.timeSinceLevelLoad - lastEjectionCheck < 3f))
		{
			lastEjectionCheck = Time.timeSinceLevelLoad;
			bool flag = aircraft.radarAlt < 1f && aircraft.speed < 1f;
			if (aircraft.transform.position.y < Datum.LocalSeaY || aircraft.cockpit.IsDetached() || (stationaryOnGround && flag))
			{
				pilot.aircraft.StartEjectionSequence();
			}
			stationaryOnGround = flag;
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	public override void LeaveState()
	{
	}
}
