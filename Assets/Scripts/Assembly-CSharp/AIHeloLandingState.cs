using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class AIHeloLandingState : PilotBaseState
{
	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIHeloLandingState.FixedUpdateState");

	private RotorShaft rotorShaft;

	private float maxRPM;

	private AircraftParameters aircraftParameters;

	private float lastEjectionCheck;

	private float stuckTimer;

	private float descentAmount;

	private float lastLandingClearCheck;

	private float lastNearbyAircraftCheck;

	private float ejectionCheckTimer;

	private float targetHeight;

	private bool touchedDown;

	private bool landingClear;

	private Vector3 avoidAircraftVector;

	private Vector3 waitOffset;

	private Airbase.VerticalLandingPoint landingPoint;

	private bool reachedApproachPoint;

	private List<Aircraft> nearbyAircraft;

	private GameObject modeDebug;

	private GameObject targetDebug;

	private GameObject aimLeadDebug;

	private Renderer modeDebugRenderer;

	public override void EnterState(Pilot pilot)
	{
		touchedDown = false;
		stateDisplayName = "landing";
		base.pilot = pilot;
		aircraft = pilot.aircraft;
		ejectionCheckTimer = 0f;
		aircraftParameters = aircraft.GetAircraftParameters();
		controlInputs = aircraft.GetInputs();
		RunwayQuery runwayQuery = new RunwayQuery
		{
			RunwayType = RunwayQueryType.Vertical,
			MinSize = aircraft.maxRadius
		};
		if (aircraft.NetworkHQ.TryGetNearestAirbase(aircraft.transform.position, out nearestAirbase, runwayQuery) && nearestAirbase.TryRequestVerticalLanding(aircraft, runwayQuery, out landingPoint))
		{
			destination = landingPoint.GetApproachPoint(aircraft);
			reachedApproachPoint = false;
		}
		foreach (UnitPart item in aircraft.partLookup)
		{
			if (item != null && item.gameObject.GetComponent<IEngine>() is RotorShaft rotorShaft)
			{
				this.rotorShaft = rotorShaft;
				maxRPM = rotorShaft.GetMaxRPM();
				break;
			}
		}
	}

	private bool LandingPointClear()
	{
		if (Time.timeSinceLevelLoad - lastLandingClearCheck < 1f)
		{
			return landingClear;
		}
		landingClear = !landingPoint.IsOccupied(aircraft);
		return landingClear;
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			if (Time.timeSinceLevelLoad - lastEjectionCheck > 1f)
			{
				EjectionCheck();
			}
			if (nearestAirbase == null)
			{
				aircraft.StartEjectionSequence();
				return;
			}
			if (landingPoint == null || !landingPoint.IsAvailable())
			{
				pilot.SwitchState(pilot.AIHeloCombatState);
				return;
			}
			if (!reachedApproachPoint)
			{
				targetHeight = 45f;
				if (FastMath.InRange(destination = landingPoint.GetApproachPoint(aircraft), aircraft.GlobalPosition(), 200f))
				{
					if (pilot.aircraft.gearState == LandingGear.GearState.LockedRetracted)
					{
						pilot.aircraft.SetGear(deployed: true);
					}
					landingPoint.RegisterLanding(aircraft);
					reachedApproachPoint = true;
				}
			}
			else
			{
				if (pilot.aircraft.gearState == LandingGear.GearState.LockedRetracted)
				{
					pilot.aircraft.SetGear(deployed: true);
				}
				destination = landingPoint.point.position.ToGlobalPosition();
				if (!aircraft.IsAutoHoverEnabled() && FastMath.InRange(destination, aircraft.GlobalPosition(), 200f))
				{
					aircraft.GetControlsFilter().SetAutoHover(enabled: true);
					aircraft.SetFlightAssist(enabled: false);
				}
				if (!LandingPointClear())
				{
					destination += waitOffset;
					targetHeight = 30f;
					stuckTimer = 0f;
					if (Time.timeSinceLevelLoad - lastNearbyAircraftCheck > 1f)
					{
						waitOffset = 75f * landingPoint.point.forward;
						lastNearbyAircraftCheck = Time.timeSinceLevelLoad;
						avoidAircraftVector = Vector3.zero;
						foreach (Aircraft item in landingPoint.GetLandingQueue())
						{
							if (item != aircraft && item != null)
							{
								avoidAircraftVector += FastMath.Direction(item.GlobalPosition(), aircraft.GlobalPosition());
							}
						}
						avoidAircraftVector.y = 0f;
					}
					stateDisplayName = "waiting to land";
					destination += avoidAircraftVector.normalized * 50f;
				}
				else
				{
					stateDisplayName = "touching down";
				}
			}
			Vector3 vector = destination - aircraft.GlobalPosition();
			vector.y = 0f;
			float magnitude = vector.magnitude;
			Vector3 vector2 = ((landingPoint != null) ? landingPoint.GetVelocity() : Vector3.zero);
			_ = (aircraft.rb.velocity - vector2).magnitude;
			if (reachedApproachPoint)
			{
				targetHeight = ((magnitude < 15f) ? (targetHeight - 3f * Time.fixedDeltaTime) : 30f);
			}
			if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
			{
				if (targetDebug == null)
				{
					targetDebug = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
					targetDebug.transform.localScale = Vector3.one * 10f;
				}
				targetDebug.transform.position = destination.ToLocalPosition();
			}
			if (aircraft.radarAlt < 0.2f || touchedDown)
			{
				controlInputs.brake = 1f;
				controlInputs.throttle = 0f;
				controlInputs.pitch = 0f;
				controlInputs.yaw = 0f;
				controlInputs.roll = 0f;
				aircraft.FilterInputs();
			}
			else if (aircraft.IsAutoHoverEnabled())
			{
				Unit attachedUnit;
				Vector3 aimDirection = (nearestAirbase.TryGetAttachedUnit(out attachedUnit) ? attachedUnit.transform.forward : (-landingPoint.point.forward));
				aircraft.autopilot.Hover(destination, targetHeight, aimDirection);
			}
			else
			{
				aircraft.autopilot.AutoAim(destination, targetHeight, destination - aircraft.GlobalPosition(), vector2, magnitude > 200f);
			}
		}
	}

	private void EjectionCheck()
	{
		lastEjectionCheck = Time.timeSinceLevelLoad;
		if (aircraft.speed < 1f)
		{
			stuckTimer += 1f;
			if (aircraft.radarAlt < 1f)
			{
				touchedDown = true;
				ejectionCheckTimer += 1f;
			}
		}
		if (stuckTimer > 20f || ejectionCheckTimer > 2f)
		{
			aircraft.StartEjectionSequence();
			pilot.SwitchState(pilot.parkedState);
		}
		else if (rotorShaft != null && rotorShaft.GetRPM() < maxRPM * 0.1f)
		{
			aircraft.StartEjectionSequence();
			pilot.SwitchState(pilot.parkedState);
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	public override void LeaveState()
	{
	}
}
