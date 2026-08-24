using System;
using System.Collections.Generic;
using RoadPathfinding;
using Unity.Profiling;
using UnityEngine;

public class AIPilotTaxiState : PilotBaseState
{
	private const int MAX_STUCK_TIME_SPEED = 30;

	private const int MAX_STUCK_TIME_YAW = 60;

	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIPilotTaxiState.FixedUpdateState");

	private RoadNetwork taxiNetwork;

	private Airbase airbase;

	private PathfindingAgent pathfinder;

	private AircraftParameters aircraftParameters;

	private List<Obstacle> obstacles = new List<Obstacle>();

	private float lastObstacleRefresh;

	private float lastObstacleCheck;

	private float lastRunwayCrossingCheck;

	private float lastRunwayClearanceCheck;

	private float brakeUrgency;

	private float lastAirbaseSearch;

	private float lastTaxiClearanceCheck;

	private float startupTime;

	private bool toRunway;

	private bool yielding;

	private bool waitingAtRunwayCrossing;

	private bool waitingForTakeoffClearance;

	private bool taxiClearance;

	private bool takeoffQueued;

	private bool disembarking;

	private bool takeoffCleared;

	private Transform exitTaxiPoint;

	private Transform destinationPoint;

	private UnitPart mainPart;

	private Vector3 obstacleAvoidVector;

	private GlobalPosition yieldPosition;

	private Airbase.Runway takeoffRunway;

	private GlobalPosition stuckRealPosition;

	private float stuckPreviousYawSign;

	private float stuckTimerSpeed;

	private float stuckTimerYaw;

	public float stuckTimerSpeedPercent => stuckTimerSpeed / 30f;

	public float stuckTimerYawPercent => stuckTimerYaw / 60f;

	public override void EnterState(Pilot pilot)
	{
		base.pilot = pilot;
		takeoffRunway = null;
		stateDisplayName = "taxiing";
		airbase = null;
		aircraft = pilot.aircraft;
		lastAirbaseSearch = -3f;
		if (pathfinder == null)
		{
			pathfinder = new PathfindingAgent(aircraft);
		}
		exitTaxiPoint = null;
		destinationPoint = null;
		toRunway = false;
		takeoffQueued = false;
		takeoffCleared = false;
		waitingForTakeoffClearance = false;
		waitingAtRunwayCrossing = false;
		stuckTimerSpeed = 0f;
		stuckTimerYaw = 0f;
		obstacles.Clear();
		aircraftParameters = aircraft.GetAircraftParameters();
		mainPart = aircraft.gameObject.GetComponent<UnitPart>();
		mainPart.onApplyDamage += Taxi_OnTakeDamage;
		controlInputs = aircraft.GetInputs();
		pilot.On5sCheck += EjectCheck;
		taxiClearance = pilot.flightInfo.HasTakenOff;
		startupTime = (pilot.flightInfo.HasTakenOff ? 100f : 0f);
	}

	private bool WaitingAtRunwayCrossing(Airbase airbase)
	{
		if (Time.timeSinceLevelLoad - lastRunwayCrossingCheck < 1f)
		{
			return waitingAtRunwayCrossing;
		}
		lastRunwayCrossingCheck = Time.timeSinceLevelLoad;
		if (airbase.AircraftApproachingRunwayInUse(aircraft))
		{
			stateDisplayName = "waiting at runway crossing";
			waitingAtRunwayCrossing = true;
		}
		else
		{
			stateDisplayName = "taxiing to takeoff";
			waitingAtRunwayCrossing = false;
		}
		return waitingAtRunwayCrossing;
	}

	private bool WaitingForTakeoffClearance(float destinationDist)
	{
		if (Time.timeSinceLevelLoad - lastRunwayClearanceCheck < 1f)
		{
			return waitingForTakeoffClearance;
		}
		lastRunwayClearanceCheck = Time.timeSinceLevelLoad;
		if (destinationDist > 100f)
		{
			waitingForTakeoffClearance = false;
			return false;
		}
		if (destinationDist < 12f)
		{
			if (!takeoffQueued)
			{
				takeoffRunway.QueueTakeoff(aircraft);
			}
			pilot.AITakeoffState = new AIPilotTakeoffState();
			pilot.SwitchState(pilot.AITakeoffState);
			return false;
		}
		bool flag = takeoffRunway.IsAvailableForTakeoff(aircraft);
		if (flag && !takeoffQueued && brakeUrgency < 0.1f)
		{
			takeoffRunway.QueueTakeoff(aircraft);
			takeoffQueued = true;
		}
		if (brakeUrgency > 0.1f && takeoffQueued)
		{
			takeoffRunway.DequeueTakeoff(aircraft);
			takeoffQueued = false;
			flag = false;
		}
		if (flag)
		{
			stateDisplayName = "entering takeoff runway";
			waitingForTakeoffClearance = false;
		}
		else
		{
			stateDisplayName = "waiting for takeoff clearance";
			waitingForTakeoffClearance = true;
		}
		return waitingForTakeoffClearance;
	}

	private void Disembark()
	{
		controlInputs.brake = 1f;
		controlInputs.throttle = 0f;
		if (aircraft.speed < 1f)
		{
			pilot.aircraft.StartEjectionSequence();
			pilot.SwitchState(pilot.parkedState);
		}
	}

	private void SearchForAirbase()
	{
		if (Time.timeSinceLevelLoad - lastAirbaseSearch < 3f)
		{
			return;
		}
		lastAirbaseSearch = Time.timeSinceLevelLoad;
		stateDisplayName = "orienting to nearest airbase";
		if (aircraftParameters.verticalLanding)
		{
			aircraft.SetFlightAssist(enabled: false);
			controlInputs.customAxis1 = 1f;
		}
		if (aircraft.NetworkHQ.AnyNearAirbase(aircraft.transform.position, out airbase))
		{
			taxiNetwork = airbase.GetTaxiNetwork();
			if (pilot.flightInfo.HasTakenOff)
			{
				stateDisplayName = "taxiing to resupply";
				if (airbase.AircraftIsOnRunway(aircraft, landingRunwaysOnly: true, out var outRunway))
				{
					Transform exitPoint;
					if (!taxiNetwork.Exists())
					{
						if (airbase.TryGetNearestServicePoint(aircraft.transform.position, out var nearestServicePoint))
						{
							destinationPoint = nearestServicePoint;
							pathfinder.SetMovingTarget(destinationPoint);
						}
						else
						{
							disembarking = true;
						}
					}
					else if (outRunway.TryGetExitTaxiPoint(aircraft.transform, 0f, out exitPoint))
					{
						exitTaxiPoint = exitPoint;
						pathfinder.Pathfind(null, exitTaxiPoint.GlobalPosition(), null);
					}
					else
					{
						disembarking = true;
					}
				}
				else
				{
					disembarking = true;
				}
				return;
			}
			airbase.RpcRegisterUsage(aircraft, isUsing: true, null);
			stateDisplayName = "taxiing to runway";
			Airbase.Runway.RunwayUsage? runwayUsage = airbase.GetTakeoffRunway(aircraft, 0f);
			if (runwayUsage.HasValue)
			{
				takeoffRunway = runwayUsage.Value.Runway;
				destinationPoint = (runwayUsage.Value.Reverse ? takeoffRunway.End : takeoffRunway.Start);
				if (taxiNetwork.Exists())
				{
					pathfinder.Pathfind(taxiNetwork, destinationPoint.GlobalPosition(), null);
				}
				else
				{
					pathfinder.SetMovingTarget(destinationPoint);
				}
				toRunway = true;
			}
		}
		else
		{
			stateDisplayName = "continuing to search for airbase";
		}
	}

	private void Taxi_OnTakeDamage(UnitPart.OnApplyDamage _)
	{
		disembarking = true;
		mainPart.onApplyDamage -= Taxi_OnTakeDamage;
	}

	private void RefreshObstacles()
	{
		if (Time.timeSinceLevelLoad - lastObstacleRefresh < 4f)
		{
			return;
		}
		lastObstacleRefresh = Time.timeSinceLevelLoad;
		obstacles.Clear();
		foreach (Aircraft item in airbase.ControlledAircraft)
		{
			if (!(item == null) && !(item == aircraft) && FastMath.InRange(item.transform.position, item.transform.position, 300f))
			{
				obstacles.Add(new Obstacle(item.transform, item.maxRadius, item.obstacleTop));
			}
		}
	}

	private void CheckObstacles()
	{
		if (Time.timeSinceLevelLoad - lastObstacleCheck < 1f)
		{
			return;
		}
		brakeUrgency = 0f;
		lastObstacleCheck = Time.timeSinceLevelLoad;
		obstacleAvoidVector = Vector3.zero;
		foreach (Obstacle obstacle in obstacles)
		{
			if (obstacle.Transform == null)
			{
				continue;
			}
			Vector3 vector = obstacle.Transform.position - aircraft.transform.position;
			Vector3 normalized = vector.normalized;
			float num = Vector3.Dot(normalized, aircraft.transform.forward);
			float num2 = Vector3.Dot(-normalized, obstacle.Transform.forward);
			if (Vector3.Dot(normalized, aircraft.transform.forward) < 0f)
			{
				continue;
			}
			float magnitude = vector.magnitude;
			magnitude -= obstacle.Radius + aircraft.maxRadius;
			if (magnitude < 50f && num2 > 0f)
			{
				if (!yielding && aircraft.pilots[0].flightInfo.HasTakenOff)
				{
					yielding = true;
					Vector3 vector2 = Vector3.RotateTowards(aircraft.transform.forward, -normalized, MathF.PI / 2f, 0f);
					yieldPosition = aircraft.GlobalPosition() + vector2 * 50f;
					break;
				}
				if (!(num > num2))
				{
					break;
				}
			}
			float num3 = Mathf.Sqrt(Mathf.Max(magnitude - 10f, 0f));
			brakeUrgency += ((aircraft.speed > num3) ? 1 : 0);
		}
	}

	public override void LeaveState()
	{
		mainPart.onApplyDamage -= Taxi_OnTakeDamage;
		pilot.On5sCheck -= EjectCheck;
	}

	private void Wait()
	{
		controlInputs.brake = 1f;
		controlInputs.throttle = 0.01f;
		stuckTimerSpeed = 0f;
		stuckTimerYaw = 0f;
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			if (pilot.aircraft.rb == null)
			{
				return;
			}
			if (disembarking || IsStuck(pilot.aircraft))
			{
				Disembark();
				return;
			}
			if (airbase == null)
			{
				SearchForAirbase();
				controlInputs.brake = 1f;
				controlInputs.throttle = 0.1f;
				return;
			}
			if (exitTaxiPoint != null && Vector3.Distance(exitTaxiPoint.position, aircraft.transform.position) < 20f)
			{
				exitTaxiPoint = null;
				if (airbase.TryGetNearestServicePoint(aircraft.transform.position, out destinationPoint))
				{
					RoadNetwork roadNetwork = airbase.GetTaxiNetwork();
					if (roadNetwork.Exists())
					{
						pathfinder.Pathfind(roadNetwork, destinationPoint.GlobalPosition(), null);
					}
					else
					{
						pathfinder.SetMovingTarget(destinationPoint);
					}
				}
				else
				{
					disembarking = true;
				}
			}
			SteeringInfo? steerpoint = pathfinder.GetSteerpoint(pilot.aircraft.GlobalPosition(), pilot.aircraft.transform.forward, aircraft.speed * 1.5f, stayOnRoad: false);
			float num3;
			if (steerpoint.HasValue)
			{
				SteeringInfo value = steerpoint.Value;
				controlInputs.throttle = 1f;
				float magnitude = value.steerVector.magnitude;
				float num = 1f + Vector3.Angle(aircraft.transform.forward, value.steerVector) * 0.02f;
				float num2 = 1f + value.nextWaypointAngle * 0.02f;
				num3 = (15f / num2 + Mathf.Min(Mathf.Sqrt(magnitude), 15f)) / num;
				if (takeoffQueued)
				{
					num3 *= 0.5f;
				}
				num3 = Mathf.Max(num3, 4f);
			}
			else
			{
				num3 = 4f;
				controlInputs.throttle = 0f;
			}
			RefreshObstacles();
			CheckObstacles();
			controlInputs.throttle = Mathf.Clamp(0.5f + (num3 - aircraft.speed) * 0.3f, 0f, 0.5f);
			controlInputs.brake = ((aircraft.speed > num3) ? 1 : 0);
			controlInputs.throttle -= brakeUrgency;
			controlInputs.brake += brakeUrgency;
			if (!steerpoint.HasValue)
			{
				return;
			}
			SteeringInfo value2 = steerpoint.Value;
			if ((controlInputs.throttle < 0.1f && aircraft.speed < 2f) || Time.timeSinceLevelLoad - pilot.flightInfo.spawnTime < 8f)
			{
				controlInputs.brake += 0.5f;
				stuckTimerSpeed = 0f;
				stuckTimerYaw = 0f;
			}
			controlInputs.throttle = Mathf.Max(controlInputs.throttle, 0.01f);
			Vector3 vector = value2.steerVector.normalized + obstacleAvoidVector;
			if (yielding)
			{
				float num4 = Vector3.Dot((yieldPosition - aircraft.GlobalPosition()).normalized, aircraft.transform.forward);
				float num5 = 6f + num4 * 3f;
				controlInputs.throttle = ((aircraft.speed > num5) ? 0.01f : 0.5f);
				controlInputs.brake = ((aircraft.speed > num5) ? 0.5f : 0f);
				aircraft.autopilot.AutoAim(yieldPosition, aimVelocity: false, ignoreCollisions: true, runwayAlign: false, 1f, 0f, followTerrain: false, 0f, Vector3.zero);
				if (airbase.AttachedAirbase || FastMath.InRange(yieldPosition, aircraft.GlobalPosition(), aircraft.maxRadius + 20f))
				{
					Disembark();
				}
				return;
			}
			aircraft.autopilot.AutoAim(aircraft.GlobalPosition() + vector * 20f, aimVelocity: false, ignoreCollisions: true, runwayAlign: false, 1f, 0f, followTerrain: false, 0f, Vector3.zero);
			if (destinationPoint == null)
			{
				Disembark();
				return;
			}
			float num6 = Vector3.Distance(destinationPoint.position, aircraft.transform.position);
			if (toRunway)
			{
				if (FastMath.InRange(aircraft.startPosition, aircraft.GlobalPosition(), 10f))
				{
					controlInputs.yaw = 0f;
				}
				if (WaitingForTakeoffClearance(num6) || WaitingAtRunwayCrossing(airbase))
				{
					Wait();
				}
			}
			else if (num6 < aircraft.maxRadius * 5f)
			{
				controlInputs.throttle = ((aircraft.speed > 6f) ? 0.01f : 0.5f);
				controlInputs.brake = ((aircraft.speed > 6f) ? 0.5f : 0f);
				if (num6 < aircraft.maxRadius * 2f)
				{
					disembarking = true;
				}
			}
		}
	}

	private bool IsStuck(Aircraft aircraft)
	{
		float deltaTime = Time.deltaTime;
		stuckTimerSpeed = (CheckStuckSpeed(aircraft, deltaTime) ? (stuckTimerSpeed + deltaTime) : 0f);
		stuckTimerYaw = (CheckStuckYaw() ? (stuckTimerYaw + deltaTime) : 0f);
		if (brakeUrgency > 0.1f)
		{
			stuckTimerSpeed -= 0.75f * deltaTime;
			stuckTimerYaw -= 0.75f * deltaTime;
			stuckTimerSpeed = Mathf.Max(stuckTimerSpeed, 0f);
			stuckTimerYaw = Mathf.Max(stuckTimerYaw, 0f);
		}
		if (!(stuckTimerSpeed > 30f))
		{
			return stuckTimerYaw > 60f;
		}
		return true;
	}

	private bool CheckStuckSpeed(Aircraft aircraft, float deltaTime)
	{
		if (aircraft.speed < 1f)
		{
			return true;
		}
		GlobalPosition b = aircraft.GlobalPosition();
		if (FastMath.SquareDistance(stuckRealPosition, b) / (deltaTime * deltaTime) < 2.25f)
		{
			return true;
		}
		return false;
	}

	private bool CheckStuckYaw()
	{
		float num = Mathf.Sign(controlInputs.yaw);
		bool flag = num == stuckPreviousYawSign;
		stuckPreviousYawSign = num;
		if (controlInputs.brake < 0.8f && Mathf.Abs(controlInputs.yaw) > 0.05f && flag)
		{
			return true;
		}
		return false;
	}

	private void EjectCheck()
	{
		if (Mathf.Abs(Vector3.Dot(aircraft.cockpit.xform.right, Vector3.up)) > 0.05f)
		{
			disembarking = true;
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}
}
