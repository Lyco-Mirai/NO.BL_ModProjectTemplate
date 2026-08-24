using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class AIHeloTransportState : PilotBaseState
{
	public enum TransportMode
	{
		CombatVehicle = 0,
		LandSuppy = 1,
		NavalSupply = 2,
		Radar = 3,
		Waiting = 4
	}

	private struct TransportDestination
	{
		public bool validMission;

		public bool dropConditionsMet;

		public GlobalPosition touchdownPoint;

		public GlobalPosition enemyPosition;

		public GlobalPosition LZ;

		public TrackingInfo nearestEnemy;

		public float slope;

		public int touchdownPointAttempts;

		public TransportDestination(GlobalPosition landingPosition, GlobalPosition enemyPosition, float levelAmount)
		{
			validMission = false;
			dropConditionsMet = false;
			touchdownPoint = landingPosition;
			this.enemyPosition = enemyPosition;
			LZ = enemyPosition;
			nearestEnemy = null;
			slope = levelAmount;
			touchdownPointAttempts = 0;
		}

		public void UpdateLZ(Aircraft aircraft, GlobalPosition? targetPosition, float targetRadius, ref Vector3 approachDirection)
		{
			if (!targetPosition.HasValue)
			{
				slope = 90f;
				touchdownPointAttempts = 0;
				return;
			}
			if (FastMath.InRange(aircraft.GlobalPosition(), touchdownPoint, 3000f))
			{
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log("[HeloTransportState] situation changed, but comitting to landing");
				}
				return;
			}
			approachDirection = FastMath.NormalizedDirection(targetPosition.Value, aircraft.GlobalPosition());
			approachDirection.y = 0f;
			GlobalPosition globalPosition = targetPosition.Value + approachDirection * (60f + targetRadius);
			float num = Mathf.Min(CombatAI.GetSafeStandoffDist(globalPosition, aircraft.NetworkHQ), 10000f);
			globalPosition += approachDirection * num;
			if (!FastMath.InRange(globalPosition, LZ, 1000f))
			{
				LZ = globalPosition;
				slope = 90f;
				touchdownPointAttempts = 0;
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log($"[HeloTransportState] situation changed, generating new LZ at a distance of {(LZ - targetPosition.Value).magnitude}m");
					GameObject gameObject = Object.Instantiate(GameAssets.i.debugArrowGreen, Datum.origin);
					gameObject.transform.localPosition = targetPosition.Value.AsVector3() + Vector3.up * 200f;
					gameObject.transform.rotation = Quaternion.LookRotation(-Vector3.up);
					gameObject.transform.localScale = new Vector3(50f, 50f, 200f);
					Object.Destroy(gameObject, 3f);
				}
			}
		}

		public void UpdateLZ(Aircraft aircraft, Unit unitToRearm)
		{
			Vector3 normalized = (aircraft.GlobalPosition() - unitToRearm.GlobalPosition()).normalized;
			normalized.y = 0f;
			GlobalPosition globalPosition = unitToRearm.GlobalPosition() + normalized * (30f + unitToRearm.maxRadius);
			if (unitToRearm is Ship ship)
			{
				float num = Mathf.Min(FastMath.Distance(aircraft.GlobalPosition(), unitToRearm.GlobalPosition()) / Mathf.Max(aircraft.speed, 1f), 30f);
				slope = 0f;
				if (ship.speed * 10f > ship.maxRadius)
				{
					Vector3 velocity = ship.rb.velocity;
					velocity.y = 0f;
					velocity = velocity.normalized * ship.maxRadius + velocity * (20f + num);
					touchdownPoint = ship.GlobalPosition() + velocity;
					GlobalPosition b = ship.GlobalPosition() + Vector3.Project(aircraft.GlobalPosition() - ship.GlobalPosition(), velocity);
					dropConditionsMet = FastMath.InRange(aircraft.GlobalPosition(), b, 50f);
				}
				else
				{
					touchdownPoint = globalPosition;
					dropConditionsMet = FastMath.InRange(aircraft.GlobalPosition(), touchdownPoint, 50f);
				}
			}
			else if (FastMath.SquareDistance(aircraft.GlobalPosition(), touchdownPoint) < 4000000f)
			{
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log("[HeloTransportState] situation changed, but comitting to landing");
				}
			}
			else if (FastMath.SquareDistance(globalPosition, LZ) > 1000000f)
			{
				LZ = globalPosition;
				slope = 90f;
				touchdownPointAttempts = 0;
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log("[HeloTransportState] situation changed, generating new LZ at a distance of 50m from " + unitToRearm.unitName);
				}
			}
		}

		public void UpdateTouchdownPoint(float maxRadius, Aircraft aircraft)
		{
			if (slope < 3f)
			{
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log($"[HeloTransportState] Satisfied with current touchdown point slope: {slope}");
				}
				return;
			}
			if (slope < 20f && FastMath.SquareDistance(aircraft.GlobalPosition(), touchdownPoint) < 1000000f)
			{
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log($"[HeloTransportState] Too close to landing point to change, touchdown slope: {slope}");
				}
				return;
			}
			Vector2 vector = Random.insideUnitCircle * Mathf.Min(50 * touchdownPointAttempts, maxRadius);
			GlobalPosition position = LZ + new Vector3(vector.x, 0f, vector.y);
			if (!Physics.Linecast(position.ToLocalPosition() + Vector3.up * 4000f, position.ToLocalPosition() - Vector3.up * 4000f, out var hitInfo, PhysicsLayers.StaticsMask))
			{
				return;
			}
			touchdownPointAttempts++;
			float num = Vector3.Angle(hitInfo.normal, Vector3.up);
			if (!(num < 20f) || !(hitInfo.point.y > Datum.LocalSeaY) || !(num < slope))
			{
				return;
			}
			aircraft.NetworkHQ.DeregisterDropZone(touchdownPoint);
			if (!aircraft.NetworkHQ.IsDropZoneClear(hitInfo.point.ToGlobalPosition()))
			{
				if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
				{
					Debug.Log($"[HeloTransportState] Drop zone not clear, continuing search {num}");
				}
				return;
			}
			if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
			{
				Debug.Log($"[HeloTransportState] Found touchdown point with slope of {num}, old point had a slope of {slope}");
			}
			slope = num;
			touchdownPoint = hitInfo.point.ToGlobalPosition();
			aircraft.NetworkHQ.RegisterDropZone(touchdownPoint);
		}
	}

	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIHeloTransportState.FixedUpdateState");

	private TransportMode transportMode;

	private RotorShaft rotorShaft;

	private float maxRPM;

	private bool airdrop;

	private TransportDestination transportDestination;

	private AircraftParameters aircraftParameters;

	private float lastEjectionCheck;

	private float touchedDownTime;

	private float lastEnemyCheck;

	private float lastLandingSpotCheck;

	private float missileReactTime;

	private float countermeasuresLastSelected;

	private float targetDist;

	private float missileAimTime;

	private float lastFiredTime;

	private float lastTargetAssessTime;

	private float lastLoSCheck;

	private float lastAirbaseSearch;

	private float timeWithoutMission;

	private float targetHeight;

	private bool deployedCargo;

	private bool targetLoS;

	private List<Missile> missileAlerts = new List<Missile>();

	private Unit currentTarget;

	private TrackingInfo currentTargetTracking;

	private TrackingInfo nearestGroundEnemy;

	private CombatAI.TargetSearchResults targetSearchResults;

	private Vector3 approachDirection;

	private string countermeasureType;

	private GameObject targetDebug;

	public AIHeloTransportState(Aircraft aircraft)
	{
		base.aircraft = aircraft;
		missileAlerts = aircraft.GetMissileWarningSystem().knownMissiles;
		aircraft.GetMissileWarningSystem().onMissileWarning += HeloTransport_OnMissileAlert;
	}

	public override void EnterState(Pilot pilot)
	{
		stateDisplayName = "transporting cargo";
		transportMode = TransportMode.Waiting;
		base.pilot = pilot;
		aircraft = pilot.aircraft;
		aircraftParameters = aircraft.GetAircraftParameters();
		touchedDownTime = 0f;
		deployedCargo = false;
		approachDirection = aircraft.transform.forward;
		timeWithoutMission = 0f;
		airdrop = false;
		nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position);
		aircraft.SetFlightAssistToDefault();
		controlInputs = aircraft.GetInputs();
		if (aircraft.GetControlsFilter() is HeloControlsFilter heloControlsFilter)
		{
			rotorShaft = heloControlsFilter.GetRotorShaft();
			if (rotorShaft != null)
			{
				maxRPM = rotorShaft.GetMaxRPM();
			}
		}
		if (aircraft.NetworkHQ.TryGetNearestGroundEnemy(aircraft.GlobalPosition(), out var nearestUnit))
		{
			Vector3 vector = nearestUnit.lastKnownPosition - aircraft.GlobalPosition();
			vector.y = 0f;
			transportDestination = new TransportDestination(nearestUnit.lastKnownPosition - vector.normalized * 50f, nearestUnit.lastKnownPosition - vector.normalized * 50f, 90f);
		}
	}

	private void HeloTransport_OnMissileAlert(MissileWarning.OnMissileWarning e)
	{
		if (missileReactTime <= 0f)
		{
			missileReactTime = -0.5f / Mathf.Clamp(Vector3.Dot(FastMath.NormalizedDirection(aircraft.transform.position, e.missile.transform.position), aircraft.transform.forward), 0.2f, 1f);
		}
	}

	private void ChooseCountermeasures()
	{
		countermeasuresLastSelected = Time.timeSinceLevelLoad;
		if (missileAlerts.Count > 1)
		{
			missileAlerts.Sort((Missile a, Missile b) => Vector3.Distance(a.transform.position, aircraft.transform.position).CompareTo(Vector3.Distance(b.transform.position, aircraft.transform.position)));
		}
		countermeasureType = aircraft.countermeasureManager.ChooseCountermeasure(missileAlerts[0]);
	}

	private void Countermeasures()
	{
		if (missileAlerts.Count == 0)
		{
			missileReactTime = 0f;
			if (pilot.aircraft.countermeasureTrigger)
			{
				aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
			}
			return;
		}
		if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
		{
			Debug.Log($"[HeloTransportState] reacting to {missileAlerts.Count} incoming missiles");
		}
		if (Time.timeSinceLevelLoad - countermeasuresLastSelected > 2f)
		{
			ChooseCountermeasures();
		}
		Vector3 zero = Vector3.zero;
		for (int num = missileAlerts.Count - 1; num >= 0; num--)
		{
			zero += aircraft.transform.position - missileAlerts[num].transform.position;
		}
		missileReactTime += ((missileReactTime < 0f) ? Time.deltaTime : Time.deltaTime);
		if (!(countermeasureType == "IR"))
		{
			return;
		}
		if (missileReactTime > 0f && missileReactTime < 2f)
		{
			if (!pilot.aircraft.countermeasureTrigger)
			{
				aircraft.Countermeasures(active: true, aircraft.countermeasureManager.activeIndex);
			}
		}
		else if (pilot.aircraft.countermeasureTrigger)
		{
			aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
		}
	}

	private void SearchForLandingSpot()
	{
		if (Time.timeSinceLevelLoad - lastLandingSpotCheck < 3f)
		{
			return;
		}
		lastLandingSpotCheck = Time.timeSinceLevelLoad;
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo.cargo)
			{
				aircraft.weaponManager.currentWeaponStation = weaponStation;
				break;
			}
		}
		pilot.flightInfo.EnemyContact = true;
		bool rearmShip = aircraft.weaponManager.currentWeaponStation.WeaponInfo.rearmShip;
		bool rearmGround = aircraft.weaponManager.currentWeaponStation.WeaponInfo.rearmGround;
		if (rearmShip || rearmGround)
		{
			if (aircraft.NetworkHQ.RearmMissionController.TryGetUnitNeedingRearm(rearmShip, rearmGround, out var lowestAmmoUnit))
			{
				transportDestination.validMission = true;
				transportDestination.UpdateLZ(aircraft, lowestAmmoUnit);
				if (rearmShip)
				{
					transportMode = TransportMode.NavalSupply;
					stateDisplayName = "Delivering Naval Supplies";
					airdrop = true;
				}
				else
				{
					transportMode = TransportMode.LandSuppy;
					transportDestination.UpdateTouchdownPoint(100f, aircraft);
					stateDisplayName = "Delivering Supplies";
					airdrop = false;
				}
			}
			else
			{
				transportMode = TransportMode.Waiting;
				transportDestination.validMission = false;
				OrbitAirbase();
				stateDisplayName = "Awaiting Cargo Mission";
				airdrop = false;
			}
			return;
		}
		GlobalPosition? targetPosition = null;
		float range = float.MaxValue;
		float targetRadius = 0f;
		if (aircraft.NetworkHQ.TryGetNearestGroundEnemy(aircraft.GlobalPosition(), out var nearestUnit) && nearestUnit.TryGetUnit(out var unit))
		{
			stateDisplayName = "Transporting Vehicles (contact)";
			targetPosition = nearestUnit.lastKnownPosition;
			range = FastMath.Distance(targetPosition.Value, aircraft.GlobalPosition());
			targetRadius = unit.maxRadius * 2f;
			airdrop = true;
		}
		if (MissionPosition.TryGetClosestObjectivePosition(aircraft, out var result) && FastMath.InRange(aircraft.GlobalPosition(), result.Position, range))
		{
			airdrop = false;
			targetPosition = result.Position;
			stateDisplayName = "Transporting Vehicles (objective)";
			targetRadius = 100f;
		}
		if (targetPosition.HasValue)
		{
			transportDestination.validMission = true;
			transportDestination.UpdateLZ(aircraft, targetPosition, targetRadius, ref approachDirection);
			transportDestination.UpdateTouchdownPoint(1000f, aircraft);
		}
		else
		{
			transportMode = TransportMode.Waiting;
			transportDestination.validMission = false;
			OrbitAirbase();
			stateDisplayName = "Awaiting Cargo Mission";
		}
	}

	private Airbase GetNearestAirbase()
	{
		if (Time.timeSinceLevelLoad - lastAirbaseSearch > 3f)
		{
			lastAirbaseSearch = Time.timeSinceLevelLoad;
			nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position);
		}
		return nearestAirbase;
	}

	public void OrbitAirbase()
	{
		if (nearestAirbase == null)
		{
			transportDestination.touchdownPoint = aircraft.GlobalPosition();
			return;
		}
		timeWithoutMission += 3f;
		if (timeWithoutMission > 45f)
		{
			pilot.SwitchState(pilot.AIHeloLandingState);
			return;
		}
		int num = 2000;
		Vector3 vector = nearestAirbase.center.GlobalPosition() - aircraft.GlobalPosition();
		vector.y = 0f;
		Vector3 current = Vector3.Cross(vector, Vector3.up);
		if (Vector3.Dot(vector, aircraft.transform.right) < 0f)
		{
			current *= -1f;
		}
		float num2 = vector.magnitude / (float)num;
		Vector3 vector2 = Vector3.RotateTowards(maxRadiansDelta: (!(Mathf.Abs(num2) > -0.4f) || !(num2 < 0.4f)) ? (num2 * 3f) : (num2 * 0.5f), current: current, target: vector, maxMagnitudeDelta: 1f);
		transportDestination.touchdownPoint = aircraft.GlobalPosition() + vector2.normalized * 4000f;
	}

	private void TargetSearch()
	{
		if (Time.timeSinceLevelLoad - lastTargetAssessTime < 5f || (aircraft.weaponManager.currentWeaponStation != null && aircraft.weaponManager.currentWeaponStation.SalvoInProgress))
		{
			return;
		}
		if (aircraft.radarAlt < 2f)
		{
			foreach (WeaponStation weaponStation in aircraft.weaponStations)
			{
				if (weaponStation.WeaponInfo.cargo)
				{
					aircraft.weaponManager.currentWeaponStation = weaponStation;
					break;
				}
			}
			aircraft.weaponManager.ClearTargetList();
			return;
		}
		lastTargetAssessTime = Time.timeSinceLevelLoad;
		Unit unit = currentTarget;
		targetSearchResults = CombatAI.ChooseHQTarget(aircraft, 1f, aircraft.weaponStations);
		if (targetSearchResults.target != null)
		{
			targetDist = FastMath.Distance(targetSearchResults.target.GlobalPosition(), aircraft.GlobalPosition());
		}
		if (targetSearchResults.chosenWeaponStation != null)
		{
			aircraft.weaponManager.currentWeaponStation = targetSearchResults.chosenWeaponStation;
		}
		if (targetSearchResults.target != unit)
		{
			currentTarget = targetSearchResults.target;
			aircraft.weaponManager.ClearTargetList();
			if (currentTarget != null)
			{
				pilot.flightInfo.EnemyContact = true;
				currentTargetTracking = aircraft.NetworkHQ.GetTrackingData(currentTarget.persistentID);
				aircraft.weaponManager.AddTargetList(currentTarget);
			}
		}
	}

	private void LoSCheck()
	{
		if (!(Time.timeSinceLevelLoad - lastLoSCheck < 1f))
		{
			lastLoSCheck = Time.timeSinceLevelLoad;
			targetLoS = currentTarget.LineOfSight(aircraft.transform.position - Vector3.up * aircraft.definition.spawnOffset.y, 1000f);
		}
	}

	private void DefendWithMissiles()
	{
		if (currentTarget == null || aircraft.radarAlt < 10f)
		{
			return;
		}
		WeaponStation currentWeaponStation = aircraft.weaponManager.currentWeaponStation;
		WeaponInfo weaponInfo = currentWeaponStation.WeaponInfo;
		if (weaponInfo.bomb || weaponInfo.gun || weaponInfo.cargo || targetDist < weaponInfo.targetRequirements.minRange || targetDist > weaponInfo.targetRequirements.maxRange || currentWeaponStation.Ammo <= 0)
		{
			return;
		}
		LoSCheck();
		float num = Vector3.Angle(currentTarget.transform.position - aircraft.transform.position, aircraft.transform.forward);
		if (!targetLoS || num > 30f)
		{
			return;
		}
		float num2 = currentWeaponStation.WeaponInfo.CalcAttacksNeeded(currentTarget);
		if ((float)currentTargetTracking.missileAttacks > num2)
		{
			return;
		}
		missileAimTime = ((num < weaponInfo.targetRequirements.minAlignment) ? (missileAimTime + Time.deltaTime) : 0f);
		if (!currentWeaponStation.SalvoInProgress && num < weaponInfo.targetRequirements.minAlignment && Time.timeSinceLevelLoad - lastFiredTime > 2.5f && aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition) && FastMath.InRange(knownPosition, currentTarget.GlobalPosition(), 500f))
		{
			List<Unit> targetList = aircraft.weaponManager.GetTargetList();
			targetList.Clear();
			int num3 = CombatAI.LookForMissileTargets(aircraft, currentTarget, currentWeaponStation, targetList);
			aircraft.weaponManager.TargetListChanged();
			if (num3 > 0)
			{
				pilot.Fire();
				lastTargetAssessTime = Time.timeSinceLevelLoad - 2f;
				lastFiredTime = Time.timeSinceLevelLoad;
			}
		}
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			Countermeasures();
			Vector3 vector = destination - aircraft.GlobalPosition();
			vector.y = 0f;
			float magnitude = vector.magnitude;
			float num = aircraftParameters.minimumRadarAlt;
			if (magnitude < 1500f)
			{
				num *= 0.3f;
				if (magnitude < 200f)
				{
					num = Mathf.Lerp(0f, num, (magnitude - 10f) / 200f);
				}
			}
			bool flag = !airdrop && magnitude < 1000f;
			if (aircraft.gearDeployed != flag)
			{
				aircraft.SetGear(flag);
			}
			aircraft.SetFlightAssist(enabled: true);
			SearchForLandingSpot();
			destination = transportDestination.touchdownPoint;
			if (PlayerSettings.debugVis && aircraft == SceneSingleton<CameraStateManager>.i.followingUnit)
			{
				if (targetDebug == null)
				{
					targetDebug = Object.Instantiate(GameAssets.i.debugArrowGreen, aircraft.transform);
				}
				targetDebug.transform.rotation = Quaternion.LookRotation(destination - aircraft.GlobalPosition());
				targetDebug.transform.localScale = new Vector3(1f, 1f, magnitude);
			}
			bool followTerrain = magnitude > 200f;
			if (transportMode == TransportMode.NavalSupply && magnitude < 100f)
			{
				num = 10f;
			}
			if (airdrop)
			{
				vector.y = 0f;
				num = 200f;
				aircraft.autopilot.AutoAim(aircraft.GlobalPosition() + vector.normalized * 10000f, num, Vector3.zero, Vector3.zero, followTerrain);
				if (FastMath.InRange(destination, aircraft.GlobalPosition(), 1000f))
				{
					Vector3 vector2 = FastMath.Direction(aircraft.GlobalPosition(), destination);
					vector2.y = 0f;
					float num2 = aircraft.speed * 1.5f;
					num2 += 2.5f * aircraft.speed;
					if (vector2.sqrMagnitude < num2 * num2)
					{
						DeployCargo();
						pilot.SwitchState(pilot.AIHeloCombatState);
					}
				}
			}
			else if (magnitude < 300f)
			{
				if (!aircraft.IsAutoHoverEnabled())
				{
					aircraft.GetControlsFilter().SetAutoHover(enabled: true);
					aircraft.SetFlightAssist(enabled: false);
				}
				Vector3 aimDirection = ((magnitude > 50f) ? (destination - aircraft.GlobalPosition()) : aircraft.cockpit.xform.forward);
				targetHeight = ((magnitude > 20f) ? 20f : (targetHeight - 2f * Time.fixedDeltaTime));
				aircraft.autopilot.Hover(destination, targetHeight, aimDirection);
			}
			else
			{
				aircraft.autopilot.AutoAim(destination, num, Vector3.zero, Vector3.zero, followTerrain);
			}
			EjectionCheck();
			if (transportMode == TransportMode.NavalSupply && transportDestination.dropConditionsMet)
			{
				touchedDownTime += Time.deltaTime;
				DeployCargo();
				if (touchedDownTime > 1f)
				{
					pilot.SwitchState(pilot.AIHeloTakeoffState);
				}
				return;
			}
			if (aircraft.radarAlt < 2f)
			{
				controlInputs.brake = 1f;
				controlInputs.throttle = 0f;
				controlInputs.pitch = 0f;
				controlInputs.yaw = 0f;
				controlInputs.roll = 0f;
				if (aircraft.speed < 10f)
				{
					pilot.flightInfo.EnemyContact = true;
					touchedDownTime += Time.deltaTime;
					DeployCargo();
					if (touchedDownTime > 7f)
					{
						pilot.SwitchState(pilot.AIHeloTakeoffState);
					}
					return;
				}
			}
			TargetSearch();
			DefendWithMissiles();
		}
	}

	private void DeployCargo()
	{
		if (deployedCargo)
		{
			return;
		}
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo.cargo)
			{
				aircraft.weaponManager.currentWeaponStation = weaponStation;
			}
		}
		if (!deployedCargo)
		{
			pilot.Fire();
			pilot.flightInfo.LastCargoDelivery = Time.timeSinceLevelLoad;
			deployedCargo = true;
			pilot.flightInfo.EnemyContact = true;
		}
	}

	private void EjectionCheck()
	{
		if (Time.timeSinceLevelLoad - lastEjectionCheck < 5f)
		{
			return;
		}
		lastEjectionCheck = Time.timeSinceLevelLoad;
		bool flag = false;
		if (aircraft.cockpit.xform.position.y < Datum.LocalSeaY)
		{
			flag = true;
		}
		bool num = aircraft.speed < 1f && aircraft.radarAlt < 5f;
		bool flag2 = aircraft.radarAlt > 40f;
		if (num && (GetNearestAirbase() == null || FastMath.SquareDistance(aircraft.GlobalPosition(), transportDestination.touchdownPoint) > 40000f))
		{
			flag = true;
		}
		if (num || flag2)
		{
			if (aircraft.partDamageTracker.GetDetachedRatio() > 0.12f)
			{
				flag = true;
			}
			if (rotorShaft != null && rotorShaft.GetRPM() < maxRPM * 0.1f)
			{
				flag = true;
			}
		}
		if (flag)
		{
			pilot.aircraft.StartEjectionSequence();
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	public override void LeaveState()
	{
		aircraft.NetworkHQ.DeregisterDropZone(transportDestination.touchdownPoint);
	}
}
