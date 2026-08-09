using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class AIPilotCombatModes : PilotBaseState
{
	private enum AttackMode
	{
		FlyingToTarget = 0,
		BreakOffAttack = 1,
		RetreatStandoff = 2,
		NoTarget = 3,
		Bombing = 4,
		GlideBombing = 5,
		Jammer = 6,
		EnergyWeapon = 7,
		UsingMissiles = 8,
		UsingLaserGuided = 9,
		UsingFixedGuns = 10
	}

	private enum EvadeMode
	{
		None = 0,
		IR = 1,
		Radar = 2
	}

	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIPilotCombatState.FixedUpdateState");

	private AttackMode attackMode;

	private EvadeMode evadeMode;

	private GlobalPosition targetKnownPosition;

	private Vector3 targetVel;

	private Vector3 targetVelPrev;

	private Vector3 targetVector;

	private Vector3 aimTargetVel;

	private Vector3 missileEvadeVector;

	private Vector3 randomErrorVector;

	private Vector3 threadAvoidVector;

	private AircraftParameters aircraftParameters;

	private Unit currentTarget;

	private TrackingInfo currentTargetTracking;

	private bool allowTargetAssessment;

	private float lastFiredTime;

	private float breakOffTimer;

	private float missileReactTime;

	private float missileImpactTime;

	private float targetDist;

	private float targetAngle;

	private float lastTargetAngle;

	private float lastGunFiredTime;

	private GameObject modeDebug;

	private GameObject targetDebug;

	private GameObject aimLeadDebug;

	private List<Missile> missileAlerts;

	private List<Missile> activeLaserGuidedMissiles;

	private float timeWithoutTarget;

	private float aimEffort;

	private float strafeTimer;

	private float notchingCooldown;

	private float lastBombTargetCheck;

	private float targetHeight;

	private float bombLastDropped;

	private float fireTimer;

	private float climbFactor;

	private bool aimVelocity;

	private bool followTerrain;

	private bool ignoreCollision;

	private bool climbing;

	private bool targetObscured;

	private bool targetAccurate;

	private string countermeasureType;

	private TerrainWarningSystem terrainWarning;

	private WeaponInfo currentWeaponInfo;

	private CombatAI.TargetSearchResults targetSearchResults;

	public AIPilotCombatModes(Aircraft aircraft)
	{
		base.aircraft = aircraft;
		terrainWarning = aircraft.autopilot.GetTerrainWarningSystem();
		missileAlerts = aircraft.GetMissileWarningSystem().knownMissiles;
		aircraft.GetMissileWarningSystem().onMissileWarning += AICombat_OnMissileAlert;
		friendlyAircraftProximity = new FriendlyAircraftProximity();
	}

	public override void EnterState(Pilot pilot)
	{
		base.pilot = pilot;
		allowTargetAssessment = true;
		timeWithoutTarget = 0f;
		aircraft = pilot.aircraft;
		targetHeight = aircraft.radarAlt;
		pilot.flightInfo.HasTakenOff = true;
		aircraft.SetFlightAssist(enabled: true);
		aircraft.weaponManager.SetGunsLinked(gunsLinked: true);
		bombLastDropped = -100f;
		lastGunFiredTime = -100f;
		if (!(pilot.aircraft.NetworkHQ == null))
		{
			controlInputs = aircraft.GetInputs();
			if (threatVector == null)
			{
				threatVector = new ThreatVector(aircraft);
			}
			if (fuelChecker == null)
			{
				fuelChecker = new FuelChecker(aircraft);
			}
			if (activeLaserGuidedMissiles == null)
			{
				activeLaserGuidedMissiles = new List<Missile>();
			}
			activeLaserGuidedMissiles.Clear();
			if (pilot.aircraft.gearState == LandingGear.GearState.LockedExtended)
			{
				pilot.aircraft.SetGear(deployed: false);
			}
			pilot.On10sCheck += AIPilotCombatState_On10sInterval;
			pilot.On5sCheck += AIPilotCombatState_On5sInterval;
			pilot.On2sCheck += AIPilotCombatState_On2sInterval;
			pilot.On1sCheck += AIPilotCombatState_On1sInterval;
			aircraft.onRadarWarning += AICombat_OnRadarWarning;
			aircraft.onRegisterMissile += AICombat_OnRegisterMissile;
			aircraft.onDeregisterMissile += AICombat_OnDeregisterMissile;
			aircraftParameters = aircraft.definition.aircraftParameters;
		}
	}

	private void AIPilotCombatState_On10sInterval()
	{
		FindNearestAirbase();
	}

	private void AIPilotCombatState_On5sInterval()
	{
		EjectionCheck();
		AssessHQTargets();
	}

	private void AIPilotCombatState_On2sInterval()
	{
	}

	private void AIPilotCombatState_On1sInterval()
	{
		ManageTarget();
		ManageAltitude();
		ChooseCombatMode();
		ChooseEvadeMode();
		if (!(currentTarget == null) && !currentTarget.disabled && !targetObscured && !(currentTarget.definition.roleIdentity.antiAir < 0.2f) && !(aircraft.speed < aircraftParameters.cornerSpeed) && !FastMath.OutOfRange(currentTarget.GlobalPosition(), aircraft.GlobalPosition(), currentTarget.GetMaxRange()) && aircraft.countermeasureManager.GetFlareAmmoProportion() > 0.5f)
		{
			aircraft.countermeasureManager.PopFlares();
		}
	}

	private void AICombat_OnMissileAlert(MissileWarning.OnMissileWarning e)
	{
		randomErrorVector = Random.insideUnitSphere;
		if (missileReactTime <= 0f)
		{
			missileReactTime = Random.Range(-1f, -4f);
		}
	}

	private void AICombat_OnRadarWarning(Aircraft.OnRadarWarning source)
	{
		if (source.detected)
		{
			targetHeight -= (source.isTarget ? (50f * source.emitter.definition.roleIdentity.antiAir) : (20f * source.emitter.definition.roleIdentity.antiAir));
			targetHeight = Mathf.Max(targetHeight, aircraftParameters.minimumRadarAlt);
		}
	}

	private void AICombat_OnRegisterMissile(Missile e)
	{
		if (e.GetWeaponInfo().laserGuided)
		{
			activeLaserGuidedMissiles.Add(e);
		}
		if (!(currentTarget != null) || !currentTarget.HasRadarEmission())
		{
			return;
		}
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo.jammer)
			{
				aircraft.weaponManager.currentWeaponStation = weaponStation;
				currentWeaponInfo = weaponStation.WeaponInfo;
				SetCombatMode(AttackMode.Jammer);
				allowTargetAssessment = false;
				followTerrain = true;
				destination = targetKnownPosition;
				break;
			}
		}
	}

	private void AICombat_OnDeregisterMissile(Missile e)
	{
		if (e.GetWeaponInfo().laserGuided)
		{
			activeLaserGuidedMissiles.Remove(e);
		}
	}

	public override void LeaveState()
	{
		aircraft.GetMissileWarningSystem().onMissileWarning -= AICombat_OnMissileAlert;
		aircraft.onRadarWarning -= AICombat_OnRadarWarning;
		pilot.On10sCheck -= AIPilotCombatState_On10sInterval;
		pilot.On5sCheck -= AIPilotCombatState_On5sInterval;
		pilot.On2sCheck -= AIPilotCombatState_On2sInterval;
		pilot.On1sCheck -= AIPilotCombatState_On1sInterval;
		aircraft.onRegisterMissile -= AICombat_OnRegisterMissile;
		aircraft.onDeregisterMissile -= AICombat_OnDeregisterMissile;
	}

	private void ApplyThreatAvoidance()
	{
		threatVector.CheckThreats(currentTarget);
		if (currentTarget != null)
		{
			float num = threatVector.threatVector.magnitude / Mathf.Max(targetSearchResults.opportunity, 0.01f);
			num /= 2f * (aircraft.bravery + friendlyAircraftProximity.CheckMorale(aircraft));
			Vector3 current = destination - aircraft.GlobalPosition();
			current.y = 0f;
			current = Vector3.RotateTowards(current, -threatVector.threatVector, Mathf.Min(num * 0.25f, 1.3962634f), 0f);
			destination = aircraft.GlobalPosition() + current;
		}
	}

	private void FlyToTarget(bool checkMode)
	{
		if (!checkMode)
		{
			return;
		}
		controlInputs.throttle = 1f;
		aimEffort = 0.25f;
		followTerrain = true;
		if (!fuelChecker.HasEnoughFuel() && nearestAirbase != null)
		{
			pilot.SwitchState(pilot.AILandingState);
			return;
		}
		if (currentTarget == null || currentTarget.disabled || currentWeaponInfo == null || !aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out destination))
		{
			SetCombatMode(AttackMode.NoTarget);
			return;
		}
		if (currentTarget.radarAlt > 10f)
		{
			targetHeight = currentTarget.radarAlt;
		}
		targetVel = ((currentTarget.rb != null) ? currentTarget.rb.velocity : Vector3.zero);
		destination += targetVel * Mathf.Min(targetDist / aircraft.speed, 20f);
		ApplyThreatAvoidance();
		if (targetAccurate && currentWeaponInfo.missile && targetDist < currentWeaponInfo.targetRequirements.maxRange && (currentWeaponInfo.overHorizon || !targetObscured) && aircraft.speed >= currentWeaponInfo.targetRequirements.minOwnerSpeed && (float)currentTargetTracking.missileAttacks <= currentWeaponInfo.CalcAttacksNeeded(currentTarget))
		{
			SetCombatMode(AttackMode.UsingMissiles);
		}
		else if (targetAccurate && currentWeaponInfo.laserGuided && targetDist < currentWeaponInfo.targetRequirements.maxRange * 1.2f && !targetObscured)
		{
			SetCombatMode(AttackMode.UsingLaserGuided);
		}
		else if (targetAccurate && currentWeaponInfo.bomb && Time.timeSinceLevelLoad - bombLastDropped > 10f && targetDist < aircraft.radarAlt * 2f + 5000f)
		{
			SetCombatMode(AttackMode.Bombing);
		}
		else if (targetAccurate && currentWeaponInfo.glideBomb && Time.timeSinceLevelLoad - bombLastDropped > 10f)
		{
			SetCombatMode(AttackMode.GlideBombing);
		}
		else if (targetAccurate && currentWeaponInfo.gun && currentWeaponInfo.boresight && !targetObscured && targetDist < currentWeaponInfo.targetRequirements.maxRange * 2f && targetAngle < Mathf.Min(50f, targetDist * 0.1f))
		{
			SetCombatMode(AttackMode.UsingFixedGuns);
		}
		else if (targetAccurate && currentWeaponInfo.jammer && !targetObscured && targetDist < currentWeaponInfo.targetRequirements.maxRange)
		{
			SetCombatMode(AttackMode.Jammer);
		}
		else if (targetAccurate && !targetObscured && currentWeaponInfo.energy && targetDist < currentWeaponInfo.targetRequirements.maxRange * 1.2f)
		{
			SetCombatMode(AttackMode.EnergyWeapon);
		}
	}

	private void NoTarget(bool enterMode, bool checkMode)
	{
		if (enterMode)
		{
			Vector3 forward = aircraft.transform.forward;
			forward.y = 0f;
			destination = aircraft.GlobalPosition() + (forward.normalized + Vector3.up * 0.2f) * 1000f;
		}
		else
		{
			if (!checkMode)
			{
				return;
			}
			timeWithoutTarget += 1f;
			controlInputs.throttle = aircraftParameters.cruiseThrottle;
			if (!fuelChecker.HasEnoughFuel() && nearestAirbase != null)
			{
				pilot.SwitchState(pilot.AILandingState);
				return;
			}
			if (currentTarget != null && !currentTarget.disabled && currentWeaponInfo != null)
			{
				timeWithoutTarget = 0f;
				SetCombatMode(AttackMode.FlyingToTarget);
				return;
			}
			if (timeWithoutTarget > 15f && missileAlerts.Count == 0)
			{
				if (pilot.AILandingState == null)
				{
					pilot.AILandingState = new AIPilotLandingState();
				}
				pilot.SwitchState(pilot.AILandingState);
			}
			targetVel = Vector3.zero;
			if (!pilot.flightInfo.EnemyContact && !targetSearchResults.outOfAmmo && MissionPosition.TryGetClosestPosition(aircraft, out var globalPosition))
			{
				destination = globalPosition;
				timeWithoutTarget = 0f;
			}
			else if (nearestAirbase != null)
			{
				destination = nearestAirbase.center.position.ToGlobalPosition();
			}
		}
	}

	protected override void FindNearestAirbase()
	{
		RunwayQuery query = new RunwayQuery
		{
			RunwayType = RunwayQueryType.Landing,
			MinSize = (aircraftParameters.verticalLanding ? aircraft.definition.length : aircraftParameters.takeoffDistance),
			LandingSpeed = (aircraftParameters.verticalLanding ? 0f : aircraftParameters.takeoffSpeed),
			TailHook = aircraft.weaponManager.HasTailHook()
		};
		nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position, query);
	}

	private void AssessHQTargets()
	{
		if (!allowTargetAssessment || (aircraft.weaponManager.currentWeaponStation != null && aircraft.weaponManager.currentWeaponStation.SalvoInProgress))
		{
			return;
		}
		Unit unit = currentTarget;
		targetSearchResults = CombatAI.ChooseHQTarget(aircraft, aircraft.bravery, aircraft.weaponStations);
		if (targetSearchResults.chosenWeaponStation != null)
		{
			aircraft.weaponManager.currentWeaponStation = targetSearchResults.chosenWeaponStation;
			currentWeaponInfo = targetSearchResults.chosenWeaponStation.WeaponInfo;
		}
		else
		{
			currentWeaponInfo = null;
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
		if (PlayerSettings.debugVis && SceneSingleton<CameraStateManager>.i.followingUnit == aircraft)
		{
			string text = ((targetSearchResults.target != null) ? targetSearchResults.target.unitName : "null");
			string text2 = ((targetSearchResults.chosenWeaponStation != null) ? targetSearchResults.chosenWeaponStation.WeaponInfo.weaponName : "null");
			Debug.Log(aircraft.unitName + " chose target " + text + " for weapon " + text2);
		}
	}

	private void BreakOffAttack(bool enterMode, bool checkMode)
	{
		if (enterMode)
		{
			Vector3 forward = aircraft.transform.forward;
			forward.y = 0f;
			destination = aircraft.GlobalPosition() + (forward.normalized + Vector3.up * 0.2f) * 1000f;
		}
		else if (checkMode)
		{
			breakOffTimer -= 1f;
			if (breakOffTimer < 0f || nearestAirbase == null)
			{
				SetCombatMode(AttackMode.FlyingToTarget);
				return;
			}
			destination = nearestAirbase.center.position.ToGlobalPosition();
			aimVelocity = true;
			followTerrain = true;
			controlInputs.throttle = 1f;
		}
	}

	private void RetreatToStandoff(bool enterMode, bool checkMode)
	{
		if (enterMode)
		{
			Vector3 forward = base.aircraft.transform.forward;
			forward.y = 0f;
			destination = base.aircraft.GlobalPosition() + (forward.normalized + Vector3.up * 0.2f) * 1000f;
		}
		else
		{
			if (!checkMode)
			{
				return;
			}
			if (currentTarget == null || currentWeaponInfo == null)
			{
				SetCombatMode(AttackMode.NoTarget);
				return;
			}
			float num = Mathf.Max(aircraftParameters.turningRadius * 2f, currentWeaponInfo.targetRequirements.minRange * 2f);
			float num2 = 0f;
			if (currentTarget is Aircraft aircraft)
			{
				num2 = aircraft.speed / base.aircraft.speed;
			}
			if (currentTarget.disabled || base.aircraft.transform.position.y - currentTarget.transform.position.y > aircraftParameters.turningRadius || num2 > 0.6f || targetDist > num)
			{
				SetCombatMode(AttackMode.FlyingToTarget);
				return;
			}
			Vector3 vector = base.aircraft.GlobalPosition() - currentTarget.GlobalPosition();
			vector.y = 0f;
			destination = base.aircraft.GlobalPosition() + vector.normalized * num;
			followTerrain = true;
			aimVelocity = true;
			ignoreCollision = true;
			controlInputs.throttle = 1f;
			aimEffort = 0.5f;
		}
	}

	private void ManageTarget()
	{
		if (!(currentTarget == null) && !currentTarget.disabled && aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out targetKnownPosition))
		{
			targetVector = targetKnownPosition - aircraft.GlobalPosition();
			targetDist = FastMath.Distance(targetKnownPosition, aircraft.GlobalPosition());
			targetAngle = Vector3.Angle(targetVector, aircraft.transform.forward);
			targetObscured = !currentTarget.LineOfSight(aircraft.transform.position - Vector3.up * aircraft.definition.spawnOffset.y, 1000f);
			targetAccurate = aircraft.NetworkHQ.IsTargetPositionAccurate(currentTarget, 100f);
		}
	}

	private void ManageAltitude()
	{
		if (targetHeight < aircraftParameters.minimumRadarAlt)
		{
			targetHeight = Mathf.Lerp(targetHeight, aircraftParameters.minimumRadarAlt, 0.5f);
		}
		if (targetHeight < aircraft.radarAlt + 200f)
		{
			targetHeight += 5f;
		}
		if (!(currentTarget == null) && !(currentWeaponInfo == null))
		{
			bool flag = aircraft.transform.position.GlobalY() < targetKnownPosition.y;
			bool flag2 = targetDist < Mathf.Max(currentWeaponInfo.targetRequirements.maxRange, 4000f);
			bool flag3 = false;
			if (targetObscured && flag2)
			{
				flag3 = true;
			}
			else if (currentTarget.radarAlt > 10f && currentTarget.transform.position.y > aircraft.transform.position.y)
			{
				flag3 = true;
			}
			if (targetDist < 5000f && flag)
			{
				flag3 = true;
			}
			targetHeight += (flag3 ? 20 : (-10));
		}
	}

	private void UseGlideBombs(bool enterMode, bool checkMode)
	{
		if (enterMode)
		{
			bombLastDropped = 0f;
			allowTargetAssessment = false;
		}
		if (checkMode)
		{
			followTerrain = targetAngle > 20f;
			controlInputs.throttle = 1f;
			aimEffort = 0.5f;
			if (currentTarget == null || currentTarget.disabled || targetDist < currentWeaponInfo.targetRequirements.minRange)
			{
				SetCombatMode(AttackMode.NoTarget);
				allowTargetAssessment = true;
				return;
			}
			float y = (aircraft.GlobalPosition() - currentTarget.GlobalPosition()).y;
			float num = aircraft.speed * aircraft.speed * 0.03f;
			float num2 = (y + num) / targetDist;
			if (!targetObscured && num2 > 0.2f && Time.timeSinceLevelLoad - bombLastDropped > 5f && targetAngle < currentWeaponInfo.targetRequirements.minAlignment)
			{
				List<Unit> targetList = aircraft.weaponManager.GetTargetList();
				targetList.Clear();
				int num3 = CombatAI.LookForMissileTargets(aircraft, currentTarget, aircraft.weaponManager.currentWeaponStation, targetList);
				aircraft.weaponManager.TargetListChanged();
				if (num3 > 0)
				{
					bombLastDropped = Time.timeSinceLevelLoad;
					lastFiredTime = Time.timeSinceLevelLoad;
					pilot.Fire();
				}
			}
			return;
		}
		float num4 = Mathf.Min(aircraft.speed * 0.12f, 50f) / Mathf.Max(aircraft.speed, 10f);
		Vector3 normalized = targetVector.normalized;
		normalized.y = 0f;
		destination = aircraft.GlobalPosition() + (normalized + Vector3.up * num4) * 500f;
		if (Time.timeSinceLevelLoad - bombLastDropped < 3f)
		{
			destination = aircraft.GlobalPosition() + (aircraft.rb.velocity.normalized + Vector3.up * 0.1f) * 500f;
			if (Time.timeSinceLevelLoad - bombLastDropped > 2f)
			{
				breakOffTimer = 2f;
				SetCombatMode(AttackMode.BreakOffAttack);
				allowTargetAssessment = true;
			}
		}
	}

	private void UseBombs(bool checkMode)
	{
		Vector3 vector = targetVector;
		vector.y = 0f;
		Vector3 forward = aircraft.transform.forward;
		forward.y = 0f;
		if (checkMode)
		{
			followTerrain = targetAngle > 20f;
			allowTargetAssessment = false;
			controlInputs.throttle = 1f;
			aimEffort = 0.5f;
			if (currentTarget == null || currentTarget.disabled || currentWeaponInfo == null)
			{
				SetCombatMode(AttackMode.NoTarget);
				allowTargetAssessment = true;
			}
			else if (targetDist < 2000f + aircraft.radarAlt * 0.5f + aircraft.speed * 3.6f && Vector3.Angle(forward.normalized, vector.normalized) > 20f)
			{
				breakOffTimer = 15f;
				SetCombatMode(AttackMode.BreakOffAttack);
				allowTargetAssessment = true;
			}
		}
		else
		{
			if (currentTarget == null)
			{
				return;
			}
			float initialHeight = aircraft.transform.position.y - (currentTarget.transform.position.y + currentWeaponInfo.airburstHeight);
			Vector3 vector2 = new Vector3(currentTarget.transform.position.x - aircraft.transform.position.x, 0f, currentTarget.transform.position.z - aircraft.transform.position.z);
			float magnitude = vector2.magnitude;
			float y = (aircraft.rb.velocity + aircraft.transform.forward * currentWeaponInfo.muzzleVelocity).y;
			float num = Kinematics.FallTime(initialHeight, y);
			float a = Vector3.Dot(aircraft.rb.velocity + aircraft.transform.forward * currentWeaponInfo.muzzleVelocity, vector2.normalized);
			a = Mathf.Max(a, 1f);
			float num2 = magnitude / a;
			if (num2 < 10f && num < 8f)
			{
				breakOffTimer = 15f;
				SetCombatMode(AttackMode.BreakOffAttack);
				allowTargetAssessment = true;
				return;
			}
			if (Time.timeSinceLevelLoad - lastBombTargetCheck > 2f)
			{
				lastBombTargetCheck = Time.timeSinceLevelLoad;
				List<Unit> targetList = aircraft.weaponManager.GetTargetList();
				targetList.Clear();
				CombatAI.LookForBombingTargets(aircraft, currentTarget, aircraft.weaponManager.currentWeaponStation, targetList);
				aircraft.weaponManager.TargetListChanged();
			}
			if (currentTarget.transform.position.y + 100f > aircraft.transform.position.y)
			{
				targetHeight += 50f * Time.deltaTime;
			}
			float num3 = num2 - num;
			if (Mathf.Abs(num3) < 1.5f && Time.timeSinceLevelLoad - bombLastDropped > 4f && Vector3.Angle(vector, new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z)) < 10f)
			{
				bombLastDropped = Time.timeSinceLevelLoad;
				lastFiredTime = Time.timeSinceLevelLoad;
				pilot.Fire();
			}
			float num4 = aircraft.transform.position.GlobalY();
			targetHeight = Mathf.Max(targetHeight, num4 - targetKnownPosition.y);
			targetHeight += Mathf.Min(aircraft.speed * 0.2f, 50f) * Time.deltaTime;
			float num5 = Mathf.Min(aircraft.speed * 0.12f, 50f);
			if (num3 < 15f && num4 < 2000f)
			{
				num5 *= 2f;
			}
			float num6 = num5 / Mathf.Max(aircraft.speed, 10f);
			Vector3 vector3 = currentTarget.GlobalPosition() + targetVel * Mathf.Clamp(num3, 0f, 30f) - aircraft.GlobalPosition();
			vector3.y = 0f;
			destination = aircraft.GlobalPosition() + (vector3.normalized + Vector3.up * num6) * 500f;
			if (Time.timeSinceLevelLoad - bombLastDropped < 4f)
			{
				destination = aircraft.GlobalPosition() + (aircraft.rb.velocity.normalized + Vector3.up * 0.1f) * 500f;
				if (Time.timeSinceLevelLoad - bombLastDropped > 3f)
				{
					breakOffTimer = 15f;
					SetCombatMode(AttackMode.BreakOffAttack);
					allowTargetAssessment = true;
				}
			}
		}
	}

	private void UseFixedGuns(bool checkMode)
	{
		if (checkMode)
		{
			aimEffort = 1f;
			if (currentWeaponInfo == null || currentTarget == null || currentTarget.disabled || !targetAccurate || aircraft.weaponManager.currentWeaponStation.Ammo <= 0 || !currentWeaponInfo.gun || !currentWeaponInfo.boresight || targetDist > currentWeaponInfo.targetRequirements.maxRange || targetAngle > Mathf.Min(50f, targetDist * 0.1f))
			{
				SetCombatMode(AttackMode.NoTarget);
				aimTargetVel = Vector3.zero;
				allowTargetAssessment = true;
				return;
			}
		}
		if (currentTarget == null)
		{
			return;
		}
		float num = TargetCalc.TargetLeadTime(currentTarget, aircraft.gameObject, aircraft.rb, currentWeaponInfo.muzzleVelocity, currentWeaponInfo.dragCoef, 2);
		targetVel = ((currentTarget.rb == null) ? Vector3.zero : currentTarget.rb.velocity);
		Vector3 rhs = targetVel - aircraft.rb.velocity;
		float a = Vector3.Dot(-targetVector.normalized, rhs);
		float num2 = targetDist / Mathf.Max(a, 0.1f);
		allowTargetAssessment = targetAngle > 45f;
		ignoreCollision = targetAngle > 5f || num2 < 3f;
		followTerrain = targetObscured;
		GlobalPosition globalPosition = currentTarget.GlobalPosition();
		if (targetObscured)
		{
			climbing = true;
		}
		else
		{
			Vector3 vector = (targetVel - targetVelPrev) / Time.fixedDeltaTime;
			targetVelPrev = targetVel;
			if (currentTarget.radarAlt < 1f)
			{
				float num3 = (aircraft.transform.position.y - currentTarget.transform.position.y) / Mathf.Max(targetDist, 10f);
				if (num3 < 0.1f)
				{
					climbing = true;
				}
				if (num3 > 0.2f)
				{
					climbing = false;
				}
			}
			else
			{
				climbing = false;
			}
			float num4 = ((currentTarget.speed < 30f) ? currentWeaponInfo.targetRequirements.maxRange : (currentWeaponInfo.targetRequirements.maxRange * 0.7f));
			Vector3 vector2 = targetVel * num + 0.5f * num * num * (vector + Vector3.up * 9.81f);
			globalPosition = currentTarget.GlobalPosition() + vector2;
			Vector3 zero = Vector3.zero;
			foreach (Weapon weapon in aircraft.weaponManager.currentWeaponStation.Weapons)
			{
				zero += weapon.transform.forward;
			}
			float num5 = Vector3.Angle(zero, globalPosition - aircraft.GlobalPosition());
			float num6 = Mathf.Clamp((num5 - lastTargetAngle) / Time.fixedDeltaTime, -20f, 0f);
			lastTargetAngle = num5;
			strafeTimer = ((num5 > 2f) ? 1f : Mathf.Max(strafeTimer - 1f * Time.fixedDeltaTime, 0f));
			float num7 = Mathf.Clamp(50f * currentTarget.maxRadius / targetDist, 0.5f, 3f);
			float num8 = Mathf.Min(num5 * 0.05f, 1f);
			globalPosition += targetVel * num8 + 0.5f * num8 * (vector + Vector3.up * 9.81f);
			if (targetDist < num4 && num5 + Mathf.Min(num6 * 0.25f, 0f) < num7)
			{
				pilot.Fire();
				lastGunFiredTime = Time.timeSinceLevelLoad;
			}
			targetHeight = aircraft.radarAlt;
		}
		climbFactor += (climbing ? (100f * Time.deltaTime) : (-500f * Time.deltaTime));
		climbFactor = Mathf.Max(climbFactor, 0f);
		destination = globalPosition + Vector3.up * climbFactor;
		if (num2 > 0f && targetAngle < 15f)
		{
			int num9 = ((!(Vector3.Dot(currentTarget.transform.forward, aircraft.transform.forward) > 0f)) ? 1 : 2);
			if (currentTarget.speed < 30f)
			{
				num9 = 2;
			}
			if (num2 < (float)num9)
			{
				breakOffTimer = ((currentTarget.speed < 30f) ? 10 : 2);
				SetCombatMode(AttackMode.BreakOffAttack);
				aimTargetVel = Vector3.zero;
				allowTargetAssessment = true;
				return;
			}
		}
		aimVelocity = false;
		aimTargetVel = targetVel;
		controlInputs.throttle = ((!(aircraft.speed > aircraftParameters.cornerSpeed) || !(aircraft.speed > currentTarget.speed * 1.5f)) ? 1 : 0);
		if (currentTarget.speed > 30f)
		{
			if (targetDist < 500f && targetAngle < 45f && aircraft.speed - currentTarget.speed > 60f)
			{
				controlInputs.throttle = 0f;
			}
			ignoreCollision = true;
		}
		if (aimLeadDebug != null)
		{
			aimLeadDebug.transform.localPosition = globalPosition.AsVector3();
			aimLeadDebug.transform.localScale = Vector3.one * 0.0005f * FastMath.Distance(globalPosition, SceneSingleton<CameraStateManager>.i.transform.GlobalPosition());
			aimLeadDebug.transform.LookAt(aircraft.transform.position);
		}
	}

	private void UseJammer(bool checkMode)
	{
		if (checkMode)
		{
			allowTargetAssessment = false;
			controlInputs.throttle = 1f;
			aimEffort = 0.5f;
			if (currentTarget == null || currentTarget.disabled || !targetAccurate)
			{
				allowTargetAssessment = true;
				SetCombatMode(AttackMode.NoTarget);
				return;
			}
			List<Unit> targetList = aircraft.weaponManager.GetTargetList();
			targetList.Clear();
			CombatAI.LookForJammingTargets(aircraft, currentTarget, aircraft.weaponManager.currentWeaponStation, targetList);
			aircraft.weaponManager.TargetListChanged();
			float num = currentWeaponInfo.targetRequirements.maxRange * 0.5f;
			Vector3 vector = targetKnownPosition - aircraft.GlobalPosition();
			vector.y = 0f;
			Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
			if (Vector3.Dot(vector2, aircraft.transform.forward) < Vector3.Dot(-vector2, aircraft.transform.forward))
			{
				vector2 *= -1f;
			}
			float num2 = (targetDist - num) / num;
			if (Mathf.Abs(num2) > -0.4f && num2 < 0.4f)
			{
				num2 *= 0.5f;
				if (targetObscured)
				{
					targetHeight += 20f * Time.deltaTime;
				}
			}
			else
			{
				num2 *= 3f;
			}
			Vector3 vector3 = Vector3.RotateTowards(vector2, vector, num2, 1f);
			destination = aircraft.GlobalPosition() + vector3.normalized * 8000f;
			if (nearestAirbase != null)
			{
				Vector3 lhs = FastMath.NormalizedDirection(nearestAirbase.center.GlobalPosition(), targetKnownPosition);
				Vector3 rhs = FastMath.NormalizedDirection(aircraft.GlobalPosition(), targetKnownPosition);
				if (Vector3.Dot(lhs, rhs) < 0.6f)
				{
					destination = nearestAirbase.center.GlobalPosition();
					destination.y = aircraft.GlobalPosition().y + 100f;
				}
			}
		}
		else if (!targetObscured)
		{
			pilot.Fire();
		}
	}

	private void UseEnergyWeapon(bool checkMode)
	{
		if (checkMode)
		{
			aimEffort = 0.5f;
			allowTargetAssessment = true;
			controlInputs.throttle = 1f;
			if (currentWeaponInfo == null || currentTarget == null || currentTarget.disabled || aircraft.weaponManager.currentWeaponStation.Ammo <= 0 || !currentWeaponInfo.energy || targetDist > currentWeaponInfo.targetRequirements.maxRange || targetDist < currentWeaponInfo.targetRequirements.minRange || targetObscured || !targetAccurate || Physics.Linecast(aircraft.transform.position, aircraft.transform.position + aircraft.rb.velocity * 5f, PhysicsLayers.StaticsMask))
			{
				SetCombatMode(AttackMode.NoTarget);
				return;
			}
			aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out destination);
			followTerrain = true;
		}
		else if (currentTarget != null && targetAngle < currentWeaponInfo.targetRequirements.minAlignment && Time.timeSinceLevelLoad - lastFiredTime > currentWeaponInfo.fireInterval)
		{
			pilot.Fire();
		}
	}

	private void UseMissiles(bool checkMode)
	{
		WeaponStation currentWeaponStation = aircraft.weaponManager.currentWeaponStation;
		if (checkMode)
		{
			aimEffort = 0.7f;
			controlInputs.throttle = 1f;
			followTerrain = targetAngle > 20f;
			if (currentTarget == null || currentTarget.disabled)
			{
				SetCombatMode(AttackMode.NoTarget);
			}
			else if (currentWeaponStation.Ammo <= 0 || Physics.Linecast(aircraft.transform.position, aircraft.transform.position + aircraft.rb.velocity * 5f, PhysicsLayers.StaticsMask))
			{
				breakOffTimer = 2f;
				SetCombatMode(AttackMode.BreakOffAttack);
			}
			else if (targetDist < currentWeaponInfo.targetRequirements.minRange)
			{
				SetCombatMode(AttackMode.RetreatStandoff);
			}
			else if (targetDist > currentWeaponInfo.targetRequirements.maxRange * 1.2f || !currentWeaponInfo.missile || (!currentWeaponInfo.overHorizon && targetObscured) || aircraft.speed < currentWeaponInfo.targetRequirements.minOwnerSpeed || (float)currentTargetTracking.missileAttacks > currentWeaponStation.WeaponInfo.CalcAttacksNeeded(currentTarget) || !targetAccurate)
			{
				SetCombatMode(AttackMode.FlyingToTarget);
			}
			return;
		}
		if (currentWeaponInfo == null || currentTarget == null || currentTarget.disabled)
		{
			SetCombatMode(AttackMode.NoTarget);
			return;
		}
		if (currentWeaponInfo.muzzleVelocity > 0f && currentTarget.rb != null && aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition))
		{
			float num = targetDist / currentWeaponInfo.muzzleVelocity;
			targetDist = FastMath.Distance(destination = knownPosition + currentTarget.rb.velocity * num, aircraft.GlobalPosition());
			targetAngle = Vector3.Angle(destination - aircraft.GlobalPosition(), aircraft.transform.forward);
		}
		if (!(targetDist < currentWeaponInfo.targetRequirements.maxRange) || !(targetAngle < currentWeaponInfo.targetRequirements.minAlignment) || !(Time.timeSinceLevelLoad - lastFiredTime > 2.5f))
		{
			return;
		}
		List<Unit> targetList = aircraft.weaponManager.GetTargetList();
		targetList.Clear();
		int num2 = CombatAI.LookForMissileTargets(aircraft, currentTarget, aircraft.weaponManager.currentWeaponStation, targetList);
		aircraft.weaponManager.TargetListChanged();
		if (num2 <= 0)
		{
			return;
		}
		pilot.Fire();
		lastFiredTime = Time.timeSinceLevelLoad;
		if (targetDist < currentWeaponInfo.targetRequirements.minRange * 2f)
		{
			breakOffTimer = 5f;
			if (nearestAirbase != null)
			{
				destination = nearestAirbase.center.GlobalPosition();
			}
			destination.y = aircraft.GlobalPosition().y + 100f;
			SetCombatMode(AttackMode.BreakOffAttack);
		}
	}

	private void UseLaserGuided(bool enterMode, bool checkMode)
	{
		if (!checkMode)
		{
			return;
		}
		aimEffort = 0.5f;
		controlInputs.throttle = 1f;
		followTerrain = targetAngle > 20f;
		if (currentTarget == null || currentTarget.disabled || currentWeaponInfo == null || (activeLaserGuidedMissiles.Count == 0 && aircraft.weaponManager.currentWeaponStation.Ammo <= 0))
		{
			allowTargetAssessment = true;
			SetCombatMode(AttackMode.NoTarget);
		}
		else if (!currentWeaponInfo.laserGuided)
		{
			allowTargetAssessment = true;
			SetCombatMode(AttackMode.FlyingToTarget);
		}
		else if (activeLaserGuidedMissiles.Count == 0)
		{
			aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out destination);
			if (targetDist < currentWeaponInfo.targetRequirements.maxRange && targetAngle < currentWeaponInfo.targetRequirements.minAlignment)
			{
				List<Unit> targetList = aircraft.weaponManager.GetTargetList();
				targetList.Clear();
				int num = CombatAI.LookForMissileTargets(aircraft, currentTarget, aircraft.weaponManager.currentWeaponStation, targetList);
				aircraft.weaponManager.TargetListChanged();
				if (num > 0)
				{
					pilot.Fire();
					allowTargetAssessment = false;
				}
			}
		}
		else
		{
			allowTargetAssessment = false;
			destination = ((nearestAirbase != null) ? nearestAirbase.center.GlobalPosition() : (aircraft.GlobalPosition() - targetVector.normalized * 1000f));
		}
	}

	private void ChooseCombatMode()
	{
		if (aircraft.disabled)
		{
			pilot.SwitchState(pilot.parkedState);
			return;
		}
		switch (attackMode)
		{
		case AttackMode.NoTarget:
			NoTarget(enterMode: false, checkMode: true);
			break;
		case AttackMode.FlyingToTarget:
			FlyToTarget(checkMode: true);
			break;
		case AttackMode.BreakOffAttack:
			BreakOffAttack(enterMode: false, checkMode: true);
			break;
		case AttackMode.RetreatStandoff:
			RetreatToStandoff(enterMode: false, checkMode: true);
			break;
		case AttackMode.UsingMissiles:
			UseMissiles(checkMode: true);
			break;
		case AttackMode.Jammer:
			UseJammer(checkMode: true);
			break;
		case AttackMode.EnergyWeapon:
			UseEnergyWeapon(checkMode: true);
			break;
		case AttackMode.UsingFixedGuns:
			UseFixedGuns(checkMode: true);
			break;
		case AttackMode.UsingLaserGuided:
			UseLaserGuided(enterMode: false, checkMode: true);
			break;
		case AttackMode.Bombing:
			UseBombs(checkMode: true);
			break;
		case AttackMode.GlideBombing:
			UseGlideBombs(enterMode: false, checkMode: true);
			break;
		}
	}

	private void ChooseEvadeMode()
	{
		for (int num = missileAlerts.Count - 1; num >= 0; num--)
		{
			if (missileAlerts[num] == null || missileAlerts[num].targetID != aircraft.persistentID)
			{
				missileAlerts.RemoveAt(num);
			}
		}
		if (missileAlerts.Count == 0)
		{
			evadingMissile = null;
			if (pilot.aircraft.countermeasureTrigger)
			{
				aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
			}
			SetEvadeMode(EvadeMode.None);
			return;
		}
		if (missileAlerts.Count > 1)
		{
			missileAlerts.Sort((Missile a, Missile b) => Vector3.SqrMagnitude(a.transform.position - aircraft.transform.position).CompareTo(Vector3.SqrMagnitude(b.transform.position - aircraft.transform.position)));
		}
		countermeasureType = aircraft.countermeasureManager.ChooseCountermeasure(missileAlerts[0]);
		if (missileReactTime > 0f)
		{
			SetEvadeMode((countermeasureType == "IR") ? EvadeMode.IR : EvadeMode.Radar);
			evadingMissile = missileAlerts[0];
		}
	}

	private void SetCombatMode(AttackMode newCombatMode)
	{
		if (attackMode != newCombatMode)
		{
			attackMode = newCombatMode;
			switch (newCombatMode)
			{
			case AttackMode.BreakOffAttack:
				BreakOffAttack(enterMode: true, checkMode: false);
				stateDisplayName = "Breaking Off Attack";
				break;
			case AttackMode.RetreatStandoff:
				stateDisplayName = "Retreating to Standoff";
				RetreatToStandoff(enterMode: true, checkMode: false);
				break;
			case AttackMode.NoTarget:
				stateDisplayName = "No Target";
				NoTarget(enterMode: true, checkMode: false);
				break;
			case AttackMode.Bombing:
				stateDisplayName = "Bombing";
				break;
			case AttackMode.GlideBombing:
				stateDisplayName = "Glide Bombing";
				break;
			case AttackMode.UsingMissiles:
				stateDisplayName = "Using Missiles";
				break;
			case AttackMode.UsingLaserGuided:
				UseLaserGuided(enterMode: true, checkMode: false);
				stateDisplayName = "Using Laser Guided Weapon";
				break;
			case AttackMode.UsingFixedGuns:
				stateDisplayName = "Using Guns";
				break;
			case AttackMode.Jammer:
				stateDisplayName = "Using Radar Jammer";
				break;
			case AttackMode.EnergyWeapon:
				stateDisplayName = "Using Energy Weapon";
				break;
			case AttackMode.FlyingToTarget:
				stateDisplayName = "Flying To Target";
				break;
			}
		}
	}

	private void SetEvadeMode(EvadeMode evadeMode)
	{
		if (this.evadeMode != evadeMode)
		{
			this.evadeMode = evadeMode;
		}
	}

	private void RunAttackMode()
	{
		switch (attackMode)
		{
		case AttackMode.FlyingToTarget:
			FlyToTarget(checkMode: false);
			break;
		case AttackMode.NoTarget:
			NoTarget(enterMode: false, checkMode: false);
			break;
		case AttackMode.Bombing:
			UseBombs(checkMode: false);
			break;
		case AttackMode.GlideBombing:
			UseGlideBombs(enterMode: false, checkMode: false);
			break;
		case AttackMode.UsingMissiles:
			UseMissiles(checkMode: false);
			break;
		case AttackMode.UsingLaserGuided:
			UseLaserGuided(enterMode: false, checkMode: false);
			break;
		case AttackMode.Jammer:
			UseJammer(checkMode: false);
			break;
		case AttackMode.EnergyWeapon:
			UseEnergyWeapon(checkMode: false);
			break;
		case AttackMode.UsingFixedGuns:
			UseFixedGuns(checkMode: false);
			break;
		case AttackMode.BreakOffAttack:
			BreakOffAttack(enterMode: false, checkMode: false);
			break;
		case AttackMode.RetreatStandoff:
			RetreatToStandoff(enterMode: false, checkMode: false);
			break;
		}
	}

	private void EvadeModeIR()
	{
		if (missileReactTime > 0f)
		{
			controlInputs.throttle = 0f;
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

	private void EvadeModeRadar(out GlobalPosition evadeDestination)
	{
		targetHeight = 10f;
		if (missileAlerts.Count > 0)
		{
			Vector3 vector = missileAlerts[0].transform.position - aircraft.transform.position;
			float magnitude = vector.magnitude;
			float num = Mathf.Max(Vector3.Dot(-vector.normalized, missileAlerts[0].rb.velocity - aircraft.rb.velocity), 1f);
			missileImpactTime = magnitude / num;
			Vector3 rhs = Vector3.Cross(missileAlerts[0].GetEvasionPoint() - aircraft.GlobalPosition(), aircraft.rb.velocity);
			missileEvadeVector = Vector3.Cross((aircraft.GlobalPosition() - missileAlerts[0].GetEvasionPoint()).normalized, rhs);
		}
		if (Vector3.Dot(missileEvadeVector, aircraft.transform.forward) < 0f)
		{
			missileEvadeVector *= -1f;
		}
		followTerrain = true;
		aimVelocity = true;
		aimEffort = 1f;
		bool flag = missileImpactTime < 8f && Vector3.Angle(missileEvadeVector, aircraft.rb.velocity) < 20f;
		if (!aircraft.countermeasureTrigger && flag)
		{
			aircraft.Countermeasures(active: true, aircraft.countermeasureManager.activeIndex);
		}
		if (aircraft.countermeasureTrigger && !flag)
		{
			aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
		}
		float t = ((missileImpactTime > 7f) ? 0.3f : 1f);
		Vector3 position = Vector3.Lerp(destination.ToLocalPosition(), aircraft.transform.position + missileEvadeVector * 1000f + randomErrorVector * 100f / Mathf.Max(aircraft.skill, 0.01f), t);
		evadeDestination = position.ToGlobalPosition();
		if (missileImpactTime < 2f)
		{
			destination += Vector3.up * 1000f;
			followTerrain = false;
		}
		controlInputs.throttle = 1f;
	}

	private void RunEvadeMode(out GlobalPosition evadeDestination)
	{
		evadeDestination = destination;
		if (missileAlerts.Count == 0)
		{
			if (aircraft.countermeasureTrigger && missileReactTime != 0f)
			{
				aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
				missileReactTime = 0f;
			}
		}
		else
		{
			Vector3 zero = Vector3.zero;
			for (int num = missileAlerts.Count - 1; num >= 0; num--)
			{
				zero += aircraft.transform.position - missileAlerts[num].transform.position;
			}
			missileReactTime += ((missileReactTime < 0f) ? (Time.fixedDeltaTime * Mathf.Max(aircraft.skill, 0.1f) * Mathf.Max(Vector3.Dot(-zero.normalized, aircraft.transform.forward), 0.4f)) : (Time.fixedDeltaTime * Mathf.Max(aircraft.skill, 0.1f)));
		}
		switch (evadeMode)
		{
		case EvadeMode.Radar:
			EvadeModeRadar(out evadeDestination);
			break;
		case EvadeMode.IR:
			EvadeModeIR();
			break;
		}
	}

	private void EjectionCheck()
	{
		bool flag = false;
		if (aircraft.cockpit.xform.position.y < Datum.LocalSeaY)
		{
			flag = true;
		}
		if (aircraft.speed < 1f && aircraft.radarAlt < 1f)
		{
			flag = true;
		}
		if (aircraft.radarAlt > 40f && (Vector3.Dot(aircraft.cockpit.xform.forward, aircraft.rb.velocity) < 0f || aircraft.partDamageTracker.GetDetachedRatio() > 0.12f))
		{
			flag = true;
		}
		if (flag)
		{
			pilot.aircraft.StartEjectionSequence();
		}
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			RunAttackMode();
			RunEvadeMode(out var evadeDestination);
			if (Time.timeSinceLevelLoad - lastGunFiredTime < 0.5f && currentWeaponInfo != null && currentWeaponInfo.gun)
			{
				pilot.Fire();
			}
			targetHeight = Mathf.Clamp(targetHeight, aircraft.maxRadius, 8000f);
			aircraft.autopilot.AutoAim(evadeDestination, aimVelocity, ignoreCollision, runwayAlign: false, aimEffort, 180f, followTerrain, targetHeight, aimTargetVel);
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}
}
