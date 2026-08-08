using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class AIHeloCombatState : PilotBaseState
{
	private enum CombatMode
	{
		FlyingToTarget = 0,
		EvadingMissiles = 1,
		BreakOffAttack = 2,
		NoTarget = 3,
		Bombing = 4,
		GunshipMode = 5,
		UsingMissiles = 6,
		UsingBoresightWeapon = 7
	}

	private static readonly ProfilerMarker fixedUpdateStateMarker = new ProfilerMarker("AIHeloCombatState.FixedUpdateState");

	private CombatMode combatMode;

	private RotorShaft rotorShaft;

	private Unit currentTarget;

	private GlobalPosition targetKnownPosition;

	private Vector3 targetVector;

	private Vector3 targetVel;

	private Vector3 missileEvadeVector;

	private Vector3 randomErrorVector;

	private bool lineOfSight;

	private bool tooClose;

	private bool aimBoresight;

	private AircraftParameters aircraftParameters;

	private WeaponInfo currentWeaponInfo;

	private float targetDist;

	private float targetAngle;

	private float timeWithoutTarget;

	private float lastFiredTime;

	private float missileAimTime;

	private float missileReactTime;

	private float notchingCooldown;

	private float breakOffTimer;

	private float desiredHeight;

	private float dangerLevel;

	private float gunshipTime;

	private float bombLastDropped;

	private float lastBombTargetCheck;

	private string countermeasureType;

	private GameObject targetDebug;

	private GameObject aimLeadDebug;

	public AIHeloCombatState(Pilot pilot)
	{
		Initialize(pilot);
	}

	public override void EnterState(Pilot pilot)
	{
		base.pilot = pilot;
		controlInputs = aircraft.GetInputs();
		aircraft.weaponManager.SetGunsLinked(gunsLinked: true);
		aircraftParameters = aircraft.GetAircraftParameters();
		nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position);
		timeWithoutTarget = 0f;
		if (aircraft.GetControlsFilter() is HeloControlsFilter heloControlsFilter)
		{
			rotorShaft = heloControlsFilter.GetRotorShaft();
		}
		aircraft.SetFlightAssistToDefault();
		aircraft.SetGear(deployed: false);
		pilot.On10sCheck += AIHeloCombatState_On10sInterval;
		pilot.On5sCheck += AIHeloCombatState_On5sInterval;
		pilot.On1sCheck += AIHeloCombatState_On1sInterval;
		aircraft.GetMissileWarningSystem().onMissileWarning += AIHeloCombatState_OnMissileAlert;
		if (PlayerSettings.debugVis)
		{
			aimLeadDebug = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.waypointDebug, Datum.origin);
			aimLeadDebug.transform.localScale = Vector3.one * 0.1f;
		}
	}

	private void AIHeloCombatState_On10sInterval()
	{
		FindNearestAirbase();
	}

	private void AIHeloCombatState_On5sInterval()
	{
		EjectionCheck();
		AssessHQTargets();
	}

	private void AIHeloCombatState_On1sInterval()
	{
		Countermeasures();
		ChooseCombatMode();
		CheckLoS();
		ManageAltitude();
	}

	private void AIHeloCombatState_OnMissileAlert(MissileWarning.OnMissileWarning e)
	{
		if (missileReactTime <= 0f)
		{
			float num = (1f + Vector3.Dot(FastMath.NormalizedDirection(aircraft.GlobalPosition(), e.missile.GlobalPosition()), -aircraft.transform.forward)) * 0.5f;
			missileReactTime = Random.Range(0.25f, 2f) * (1f + num * 2f);
		}
	}

	private void NoTarget()
	{
		desiredHeight -= 20f * Time.fixedDeltaTime;
		timeWithoutTarget += Time.fixedDeltaTime;
		targetVel = Vector3.zero;
		if (!pilot.flightInfo.EnemyContact && MissionPosition.TryGetClosestPosition(aircraft, out var globalPosition))
		{
			timeWithoutTarget = 0f;
			destination = globalPosition;
		}
		else if (nearestAirbase != null)
		{
			destination = nearestAirbase.center.position.ToGlobalPosition();
			if (timeWithoutTarget > 15f && FastMath.InRange(destination, aircraft.GlobalPosition(), 3000f))
			{
				pilot.SwitchState(pilot.AIHeloLandingState);
			}
		}
	}

	private void BreakOffAttack()
	{
		if (nearestAirbase != null)
		{
			destination = nearestAirbase.center.transform.position.ToGlobalPosition();
			if (FastMath.InRange(nearestAirbase.center.position, aircraft.transform.position, 2000f))
			{
				tooClose = false;
			}
		}
		breakOffTimer -= Time.deltaTime;
	}

	private void EvadeMissiles()
	{
		if (evadingMissile != null)
		{
			missileEvadeVector = Vector3.Cross((aircraft.GlobalPosition() - evadingMissile.GetEvasionPoint()).normalized, Vector3.up);
		}
		if (Vector3.Dot(missileEvadeVector, aircraft.transform.forward) < 0f)
		{
			missileEvadeVector *= -1f;
		}
		desiredHeight -= 40f * Time.fixedDeltaTime;
		if (missileWarningSystem.IsWarning() && (countermeasureType == "SARH" || countermeasureType == "ARH"))
		{
			notchingCooldown = 2.5f;
		}
		else
		{
			notchingCooldown -= Time.fixedDeltaTime;
		}
		if (notchingCooldown > 0f)
		{
			desiredHeight -= 25f * Time.deltaTime;
			bool flag = Vector3.Dot(missileEvadeVector, aircraft.transform.forward) > 0.5f;
			if (!aircraft.countermeasureTrigger && flag)
			{
				stateDisplayName = "defensive jamming";
				aircraft.Countermeasures(active: true, aircraft.countermeasureManager.activeIndex);
			}
			if (aircraft.countermeasureTrigger && !flag)
			{
				aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
			}
		}
		destination = aircraft.GlobalPosition() + missileEvadeVector * 10000f + randomErrorVector * 1000f / Mathf.Max(aircraft.skill, 0.01f);
		if (!missileWarningSystem.IsWarning() && notchingCooldown <= 0f)
		{
			evadingMissile = null;
			missileReactTime = 0f;
			ChooseCombatMode();
		}
	}

	private void Countermeasures()
	{
		if (!missileWarningSystem.IsWarning())
		{
			if (pilot.aircraft.countermeasureTrigger)
			{
				aircraft.Countermeasures(active: false, aircraft.countermeasureManager.activeIndex);
			}
			return;
		}
		if (missileReactTime > 0f)
		{
			missileReactTime -= 1f * Mathf.Max(aircraft.skill, 0.01f);
			if (missileReactTime <= 0f && missileWarningSystem.TryGetNearestIncoming(out evadingMissile))
			{
				countermeasureType = aircraft.countermeasureManager.ChooseCountermeasure(evadingMissile);
				SetCombatMode(CombatMode.EvadingMissiles);
			}
		}
		if (!(countermeasureType == "IR"))
		{
			return;
		}
		if (missileReactTime <= 0f)
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

	private void CheckLoS()
	{
		if (!(currentTarget == null))
		{
			lineOfSight = currentTarget.LineOfSight(aircraft.transform.position - Vector3.up * 5f, 5f);
			targetDist = FastMath.Distance(targetKnownPosition, aircraft.GlobalPosition());
			targetVel = ((currentTarget.rb != null) ? currentTarget.rb.velocity : Vector3.zero);
			targetAngle = Vector3.Angle(aircraft.transform.forward, targetKnownPosition - aircraft.GlobalPosition());
		}
	}

	private void ManageAltitude()
	{
		dangerLevel = Mathf.Lerp(dangerLevel, 0f, 0.02f);
		if (currentTarget == null || currentWeaponInfo == null)
		{
			desiredHeight -= 20f;
			return;
		}
		float num = ((combatMode == CombatMode.Bombing || combatMode == CombatMode.GunshipMode) ? 0f : (currentWeaponInfo.targetRequirements.minRange + 500f));
		if (!tooClose && targetDist < num && lineOfSight && nearestAirbase != null)
		{
			tooClose = true;
		}
		if (tooClose && targetDist > num)
		{
			tooClose = false;
		}
		bool flag = targetDist < currentWeaponInfo.targetRequirements.maxRange + 500f;
		bool flag2 = false;
		if (!tooClose && !lineOfSight && flag)
		{
			flag2 = true;
		}
		else if (currentTarget.radarAlt > 10f && currentTarget.transform.position.y > aircraft.transform.position.y)
		{
			flag2 = true;
		}
		if (tooClose)
		{
			destination = aircraft.GlobalPosition() - targetVector.normalized * 10000f;
			breakOffTimer = 4f;
			SetCombatMode(CombatMode.BreakOffAttack);
		}
		desiredHeight += (flag2 ? 10 : (-20));
	}

	private void GunshipMode()
	{
		float num = currentWeaponInfo.targetRequirements.maxRange * 0.66f;
		gunshipTime += Time.fixedDeltaTime;
		num -= gunshipTime * 5f;
		Vector3 vector = targetKnownPosition - aircraft.GlobalPosition();
		vector.y = 0f;
		Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
		if (aircraft.weaponManager.currentWeaponStation.TurretTraverseRange() < 60f && aircraft.weaponManager.currentWeaponStation.GetFiringConeDirection(out var aimDirection, out var _))
		{
			if (Vector3.Dot(aimDirection, aircraft.transform.right) < 0f)
			{
				vector2 *= -1f;
			}
		}
		else if (Vector3.Dot(vector2, aircraft.transform.forward) < Vector3.Dot(-vector2, aircraft.transform.forward))
		{
			vector2 *= -1f;
		}
		float num2 = (targetDist - num) / num;
		if (Mathf.Abs(num2) > -0.4f && num2 < 0.4f)
		{
			num2 *= 0.5f;
			if (!lineOfSight)
			{
				desiredHeight += 20f * Time.deltaTime;
			}
		}
		else
		{
			num2 *= 3f;
		}
		Vector3 vector3 = Vector3.RotateTowards(vector2, vector, num2, 1f);
		destination = aircraft.GlobalPosition() + vector3.normalized * 8000f;
	}

	private void Bombing(WeaponInfo weaponInfo)
	{
		Vector3 vector = targetVector;
		vector.y = 0f;
		Vector3 forward = aircraft.transform.forward;
		forward.y = 0f;
		if (targetDist < 2000f + aircraft.radarAlt * 0.5f + aircraft.speed * 3.6f && Vector3.Angle(forward.normalized, vector.normalized) > 20f)
		{
			breakOffTimer = 15f;
			SetCombatMode(CombatMode.BreakOffAttack);
			return;
		}
		if (!lineOfSight)
		{
			SetCombatMode(CombatMode.FlyingToTarget);
			return;
		}
		float initialHeight = aircraft.transform.position.y - (currentTarget.transform.position.y + weaponInfo.airburstHeight);
		Vector3 vector2 = new Vector3(currentTarget.transform.position.x - aircraft.transform.position.x, 0f, currentTarget.transform.position.z - aircraft.transform.position.z);
		float magnitude = vector2.magnitude;
		float y = aircraft.rb.velocity.y;
		float num = Kinematics.FallTime(initialHeight, y);
		float a = Vector3.Dot(aircraft.rb.velocity, vector2.normalized);
		a = Mathf.Max(a, 1f);
		float num2 = magnitude / a;
		if (num2 < 10f && num < 8f)
		{
			breakOffTimer = 15f;
			SetCombatMode(CombatMode.BreakOffAttack);
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
			desiredHeight += 50f * Time.deltaTime;
		}
		float num3 = num2 - num;
		if (Mathf.Abs(num3) < 1.5f)
		{
			bombLastDropped = Time.timeSinceLevelLoad;
			lastFiredTime = Time.timeSinceLevelLoad;
			breakOffTimer = 15f;
			pilot.Fire();
			SetCombatMode(CombatMode.BreakOffAttack);
		}
		float num4 = aircraft.transform.position.GlobalY();
		desiredHeight = Mathf.Max(desiredHeight, num4 - targetKnownPosition.y);
		desiredHeight += 30f * Time.deltaTime;
		destination = currentTarget.GlobalPosition() + targetVel * Mathf.Clamp(num3, 0f, 30f) + Vector3.up * desiredHeight;
		if (num3 < 15f && num4 < 2000f)
		{
			float a2 = magnitude * (Mathf.Min(aircraft.speed - num3 * 4f, 300f) / 600f) - aircraft.transform.position.y;
			a2 = Mathf.Max(a2, 0f);
			destination.y = num4 + a2;
		}
	}

	private void UseMissiles()
	{
		destination = targetKnownPosition + Vector3.up * targetDist * 0.0005f * Mathf.Min(currentWeaponInfo.targetRequirements.minAlignment * 0.5f, 30f);
		missileAimTime = ((targetAngle < currentWeaponInfo.targetRequirements.minAlignment) ? (missileAimTime + Time.deltaTime) : 0f);
		if (!aircraft.weaponManager.currentWeaponStation.SalvoInProgress && targetAngle < currentWeaponInfo.targetRequirements.minAlignment && Time.timeSinceLevelLoad - lastFiredTime > 2.5f && aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition) && FastMath.InRange(knownPosition, currentTarget.GlobalPosition(), 500f))
		{
			List<Unit> targetList = aircraft.weaponManager.GetTargetList();
			targetList.Clear();
			int num = CombatAI.LookForMissileTargets(aircraft, currentTarget, aircraft.weaponManager.currentWeaponStation, targetList);
			aircraft.weaponManager.TargetListChanged();
			if (num > 0)
			{
				pilot.Fire();
				lastFiredTime = Time.timeSinceLevelLoad;
			}
		}
	}

	private void UseBoresightWeapon()
	{
		if (!(currentTarget == null))
		{
			float num = TargetCalc.TargetLeadTime(currentTarget, aircraft.gameObject, aircraft.rb, currentWeaponInfo.muzzleVelocity, currentWeaponInfo.dragCoef, 2);
			Vector3 vector = ((currentTarget.rb != null) ? currentTarget.rb.velocity : Vector3.zero);
			vector -= aircraft.rb.velocity;
			Vector3 vector2 = Vector3.up * 9.81f;
			destination = currentTarget.GlobalPosition() + vector * num + vector2 * num * num * 0.5f;
			if (Vector3.Angle(aircraft.transform.forward, destination - aircraft.GlobalPosition()) < 150f * currentTarget.maxRadius / targetDist)
			{
				pilot.Fire();
			}
		}
	}

	private void FlyToTarget()
	{
		timeWithoutTarget = 0f;
		destination = targetKnownPosition;
	}

	private void SetCombatMode(CombatMode newCombatMode)
	{
		if (combatMode != newCombatMode)
		{
			combatMode = newCombatMode;
			switch (newCombatMode)
			{
			case CombatMode.EvadingMissiles:
				stateDisplayName = "Evading Missiles";
				break;
			case CombatMode.BreakOffAttack:
				stateDisplayName = "Breaking Off Attack";
				break;
			case CombatMode.NoTarget:
				stateDisplayName = "No Target";
				break;
			case CombatMode.Bombing:
				stateDisplayName = "Bombing";
				break;
			case CombatMode.GunshipMode:
				stateDisplayName = "Gunship";
				break;
			case CombatMode.UsingMissiles:
				stateDisplayName = "Using Missiles";
				break;
			case CombatMode.UsingBoresightWeapon:
				stateDisplayName = "Using Boresight Weapon";
				break;
			case CombatMode.FlyingToTarget:
				stateDisplayName = "Flying To Target";
				break;
			}
			aimBoresight = combatMode == CombatMode.UsingMissiles || combatMode == CombatMode.UsingBoresightWeapon;
			if (combatMode != CombatMode.GunshipMode)
			{
				gunshipTime = 0f;
			}
		}
	}

	private void ChooseCombatMode()
	{
		if (evadingMissile != null && missileReactTime <= 0f)
		{
			SetCombatMode(CombatMode.EvadingMissiles);
			return;
		}
		if (breakOffTimer > 0f && nearestAirbase != null)
		{
			SetCombatMode(CombatMode.BreakOffAttack);
			return;
		}
		if (!fuelChecker.HasEnoughFuel())
		{
			pilot.SwitchState(pilot.AIHeloLandingState);
		}
		if (aircraft.weaponStations.Count == 0)
		{
			SetCombatMode(CombatMode.NoTarget);
			return;
		}
		bool flag = currentTarget != null && aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out targetKnownPosition);
		WeaponStation currentWeaponStation = aircraft.weaponManager.currentWeaponStation;
		currentWeaponInfo = currentWeaponStation.WeaponInfo;
		bool flag2 = flag && aircraft.NetworkHQ.IsTargetPositionAccurate(currentTarget, 20f);
		if (currentTarget == null || currentTarget.disabled || !flag || currentWeaponInfo == null || currentWeaponStation.Ammo == 0)
		{
			SetCombatMode(CombatMode.NoTarget);
			return;
		}
		currentWeaponInfo = aircraft.weaponManager.currentWeaponStation?.WeaponInfo;
		if (currentWeaponInfo.bomb && Time.timeSinceLevelLoad - bombLastDropped > 10f)
		{
			SetCombatMode(CombatMode.Bombing);
			return;
		}
		if (currentWeaponStation.HasTurret() && currentWeaponStation.TurretTraverseRange() > 15f && flag2 && targetDist < currentWeaponInfo.targetRequirements.maxRange * 1.5f)
		{
			SetCombatMode(CombatMode.GunshipMode);
			return;
		}
		if (currentWeaponInfo.gun && currentWeaponStation.TurretTraverseRange() < 15f && flag2 && targetDist < currentWeaponInfo.targetRequirements.maxRange * 1.2f && targetAngle < 20f)
		{
			SetCombatMode(CombatMode.UsingBoresightWeapon);
			return;
		}
		WeaponInfo weaponInfo = currentWeaponStation.WeaponInfo;
		if (!weaponInfo.bomb && !weaponInfo.boresight && !weaponInfo.gun && targetDist > weaponInfo.targetRequirements.minRange && targetDist < weaponInfo.targetRequirements.maxRange && lineOfSight && targetAngle < 25f)
		{
			SetCombatMode(CombatMode.UsingMissiles);
		}
		else
		{
			SetCombatMode(CombatMode.FlyingToTarget);
		}
	}

	private void RunCombatModes()
	{
		switch (combatMode)
		{
		case CombatMode.FlyingToTarget:
			FlyToTarget();
			break;
		case CombatMode.NoTarget:
			NoTarget();
			break;
		case CombatMode.EvadingMissiles:
			EvadeMissiles();
			break;
		case CombatMode.Bombing:
			Bombing(currentWeaponInfo);
			break;
		case CombatMode.UsingMissiles:
			UseMissiles();
			break;
		case CombatMode.UsingBoresightWeapon:
			UseBoresightWeapon();
			break;
		case CombatMode.BreakOffAttack:
			BreakOffAttack();
			break;
		case CombatMode.GunshipMode:
			GunshipMode();
			break;
		}
	}

	public override void FixedUpdateState(Pilot pilot)
	{
		using (fixedUpdateStateMarker.Auto())
		{
			RunCombatModes();
			desiredHeight = Mathf.Clamp(desiredHeight, 0f, 1000f);
			float minimumRadarAlt = aircraftParameters.minimumRadarAlt;
			minimumRadarAlt += desiredHeight;
			if (aimBoresight)
			{
				minimumRadarAlt = Mathf.Max(minimumRadarAlt, aircraft.radarAlt + 2f);
				aircraft.autopilot.BoresightAim(destination, minimumRadarAlt);
			}
			else
			{
				aircraft.autopilot.AutoAim(destination, minimumRadarAlt, Vector3.zero, Vector3.zero, followTerrain: true);
			}
		}
	}

	private void AssessHQTargets()
	{
		if (aircraft.weaponManager.currentWeaponStation == null || aircraft.weaponManager.currentWeaponStation.SalvoInProgress)
		{
			return;
		}
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.Cargo && weaponStation.Ammo > 0 && Time.timeSinceLevelLoad - pilot.flightInfo.LastCargoDelivery > 15f)
			{
				if (pilot.AIHeloTransportState == null)
				{
					pilot.AIHeloTransportState = new AIHeloTransportState(aircraft);
				}
				pilot.SwitchState(pilot.AIHeloTransportState);
				return;
			}
		}
		CombatAI.TargetSearchResults targetSearchResults = CombatAI.ChooseHQTarget(aircraft, aircraft.bravery, aircraft.weaponStations);
		if (!(targetSearchResults.target != currentTarget))
		{
			return;
		}
		currentTarget = targetSearchResults.target;
		if (aircraft.weaponManager.GetTargetList().Count > 0)
		{
			aircraft.weaponManager.ClearTargetList();
		}
		if (currentTarget != null)
		{
			pilot.flightInfo.EnemyContact = true;
			aircraft.weaponManager.currentWeaponStation = targetSearchResults.chosenWeaponStation;
			if (combatMode != CombatMode.GunshipMode)
			{
				aircraft.weaponManager.AddTargetList(currentTarget);
			}
			targetDist = float.MaxValue;
			targetAngle = 180f;
		}
		else
		{
			SetCombatMode(CombatMode.NoTarget);
		}
	}

	private void EjectionCheck()
	{
		bool flag = false;
		if (aircraft.cockpit.xform.position.y < Datum.LocalSeaY)
		{
			flag = true;
		}
		if (aircraft.speed < 1f && aircraft.radarAlt < 5f)
		{
			flag = true;
		}
		if (aircraft.radarAlt > 40f && (aircraft.partDamageTracker.GetDetachedRatio() > 0.12f || (rotorShaft != null && rotorShaft.GetRPM() < rotorShaft.GetMaxRPM() * 0.3f)))
		{
			flag = true;
		}
		if (flag)
		{
			aircraft.StartEjectionSequence();
		}
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	public override void LeaveState()
	{
		pilot.On10sCheck -= AIHeloCombatState_On10sInterval;
		pilot.On5sCheck -= AIHeloCombatState_On5sInterval;
		pilot.On1sCheck -= AIHeloCombatState_On1sInterval;
	}
}
