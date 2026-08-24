using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Turret : MonoBehaviour
{
	private enum TargetAcquisitionMode
	{
		parentUnitTargetDetector = 0,
		assignedTargetDetectors = 1,
		searchForRadar = 2,
		datalink = 3,
		fireControl = 4,
		fireSupport = 5
	}

	[SerializeField]
	private Unit attachedUnit;

	[SerializeField]
	private TargetAcquisitionMode targetAcquisitionMode = TargetAcquisitionMode.assignedTargetDetectors;

	[SerializeField]
	private FireControl fireControl;

	[SerializeField]
	private bool firesWithoutAiming;

	[SerializeField]
	private List<TargetDetector> targetDetectors;

	[SerializeField]
	private WeaponStation[] weaponStations;

	[SerializeField]
	private Weapon aimSafetyWeapon;

	[SerializeField]
	private float traverseRate;

	[SerializeField]
	private float traverseRange;

	[SerializeField]
	private float elevationRate = 30f;

	[SerializeField]
	private float minElevation;

	[SerializeField]
	private float maxElevation;

	[SerializeField]
	private Transform elevationTransform;

	[SerializeField]
	private FiringCone[] firingCones;

	[SerializeField]
	private float targetAssessmentInterval = 2f;

	[SerializeField]
	private AimSolver aimSolver;

	[SerializeField]
	private float lockTime = 0.5f;

	[SerializeField]
	private float armorTierOptimism;

	[SerializeField]
	private bool newTargetSearchAfterFire;

	[SerializeField]
	private bool onlyDefensive;

	[SerializeField]
	private UnitPart[] criticalParts;

	private Aircraft aircraft;

	private bool disabled;

	[SerializeField]
	private bool manual;

	[SerializeField]
	private bool ready;

	[SerializeField]
	private bool onTarget;

	[SerializeField]
	private bool stowed;

	private float lastVectorSent;

	private byte turretIndex;

	private Vector3 manualVector;

	private float traverseAngle;

	private float elevationAngle;

	private float traverseError;

	private float elevationError;

	private float targetRange;

	private float timeOnTarget;

	private float lastTargetAssessment;

	private Vector3 aimVector;

	private float maxRange;

	private List<Unit> potentialTargets = new List<Unit>();

	private Unit target;

	private RaycastHit hit;

	private WeaponStation currentWeaponStation;

	private GameObject debugArrow;

	private void Awake()
	{
		if (attachedUnit == null)
		{
			attachedUnit = GetComponentInParent<UnitPart>().parentUnit;
		}
		if (weaponStations.Length != 0)
		{
			WeaponStation[] array = weaponStations;
			foreach (WeaponStation obj in array)
			{
				obj.WeaponInfo = obj.Weapons[0].info;
			}
			currentWeaponStation = weaponStations[0];
			if (elevationTransform == null)
			{
				elevationTransform = weaponStations[0].Weapons[0].transform;
			}
		}
		elevationAngle = elevationTransform.localEulerAngles.x;
		if (elevationAngle > 90f)
		{
			elevationAngle -= 360f;
		}
		attachedUnit.onInitialize += Turret_OnInitialize;
		attachedUnit.onDisableUnit += Turret_OnUnitDisabled;
		if (attachedUnit is Aircraft aircraft)
		{
			this.aircraft = aircraft;
		}
		UnitPart[] array2 = criticalParts;
		foreach (UnitPart part in array2)
		{
			RegisterPart(part);
		}
	}

	private void OnDestroy()
	{
		potentialTargets.Clear();
		target = null;
		foreach (TargetDetector targetDetector in targetDetectors)
		{
			DeregisterTargetDetector(targetDetector);
		}
	}

	private void Turret_OnInitialize()
	{
		if (aircraft == null)
		{
			WeaponStation[] array = weaponStations;
			for (int i = 0; i < array.Length; i++)
			{
				WeaponStation weaponStation = (currentWeaponStation = array[i]);
				weaponStation.Number = (byte)attachedUnit.weaponStations.Count;
				weaponStation.WeaponInfo = weaponStation.Weapons[0].info;
				weaponStation.Rearm(weaponStation.FullAmmo - weaponStation.Ammo);
				weaponStation.AssignTurret(this);
				attachedUnit.RegisterWeaponStation(weaponStation);
			}
			AssessWeapons();
		}
		float a = 0.7f;
		if (attachedUnit is GroundVehicle groundVehicle)
		{
			a = groundVehicle.skill;
		}
		else if (attachedUnit is Ship ship)
		{
			a = ship.skill;
		}
		lockTime /= Mathf.Max(a, 0.1f);
		traverseRate *= Mathf.Max(a, 0.33f);
		if (!attachedUnit.IsServer)
		{
			return;
		}
		if (targetAcquisitionMode == TargetAcquisitionMode.datalink && attachedUnit.NetworkHQ != null)
		{
			this.StartSlowUpdateDelayed(targetAssessmentInterval, DatalinkTargetSearch);
			return;
		}
		if (targetAcquisitionMode == TargetAcquisitionMode.fireControl && fireControl != null && weaponStations.Length != 0)
		{
			WeaponStation[] array = weaponStations;
			foreach (WeaponStation weaponStation2 in array)
			{
				fireControl.SubscribeWeaponStation(weaponStation2);
			}
			return;
		}
		if (targetAcquisitionMode == TargetAcquisitionMode.assignedTargetDetectors)
		{
			foreach (TargetDetector targetDetector in targetDetectors)
			{
				RegisterTargetDetector(targetDetector);
			}
			return;
		}
		if (targetAcquisitionMode == TargetAcquisitionMode.fireSupport && attachedUnit.NetworkHQ != null)
		{
			this.StartSlowUpdateDelayed(targetAssessmentInterval, FireSupportTargetSearch);
		}
		else if (!stowed && targetAcquisitionMode == TargetAcquisitionMode.searchForRadar && attachedUnit.NetworkHQ != null)
		{
			SearchForRadar().Forget();
		}
	}

	public void AttachToWeaponManager(Aircraft aircraft)
	{
		currentWeaponStation = weaponStations[0];
		attachedUnit = aircraft;
		bool flag = false;
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (weaponStation.WeaponInfo == currentWeaponStation.WeaponInfo)
			{
				weaponStations = new WeaponStation[1];
				weaponStations[0] = weaponStation;
				currentWeaponStation = weaponStation;
				turretIndex = weaponStation.AssignTurret(this);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			aircraft.RegisterWeaponStation(currentWeaponStation);
		}
		if (targetAcquisitionMode == TargetAcquisitionMode.parentUnitTargetDetector && attachedUnit.NetworkHQ != null)
		{
			RegisterTargetDetector((attachedUnit as Aircraft).EOTS);
		}
		if (targetAcquisitionMode == TargetAcquisitionMode.datalink && attachedUnit.NetworkHQ != null)
		{
			this.StartSlowUpdateDelayed(targetAssessmentInterval, DatalinkTargetSearch);
		}
	}

	private void RegisterTargetDetector(TargetDetector targetDetector)
	{
		if (!targetDetectors.Contains(targetDetector))
		{
			targetDetectors.Add(targetDetector);
		}
		if (targetDetector is Radar)
		{
			attachedUnit.radar = targetDetector as Radar;
		}
		targetDetector.onDetectTarget += Turret_OnDetectTarget;
		targetDetector.onScan += Turret_OnCompleteScan;
		targetDetector.onDisabled += Turret_OnTargetDetectorDisabled;
	}

	private void DeregisterTargetDetector(TargetDetector targetDetector)
	{
		if (!(targetDetector == null))
		{
			targetDetector.onDetectTarget -= Turret_OnDetectTarget;
			targetDetector.onScan -= Turret_OnCompleteScan;
			targetDetector.onDisabled -= Turret_OnTargetDetectorDisabled;
		}
	}

	private void RegisterPart(UnitPart part)
	{
		part.onApplyDamage += Turret_OnPartDamage;
		part.onPartDetached += Turret_OnDetachedFromUnit;
	}

	private void DeregisterPart(UnitPart part)
	{
		if (part != null)
		{
			part.onApplyDamage -= Turret_OnPartDamage;
			part.onPartDetached -= Turret_OnDetachedFromUnit;
		}
	}

	public void FireControlDisabled()
	{
		fireControl = null;
		SearchForRadar().Forget();
	}

	private async UniTask SearchForRadar()
	{
		if (this == null)
		{
			return;
		}
		CancellationToken cancel = base.destroyCancellationToken;
		attachedUnit.radar = null;
		targetDetectors.Clear();
		if (fireControl != null)
		{
			fireControl.DeregisterTurret(this);
		}
		fireControl = null;
		await UniTask.Delay(1000);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		while (attachedUnit != null && !attachedUnit.disabled)
		{
			if (attachedUnit.NetworkHQ.TryGetFireControl(attachedUnit.GlobalPosition(), 300f, out fireControl) && fireControl.TryGetRadar(out var radar))
			{
				attachedUnit.radar = radar;
				fireControl.RegisterTurret(this);
				break;
			}
			if (attachedUnit.NetworkHQ.TryGetRadar(attachedUnit.GlobalPosition(), 300f, out var nearestRadar))
			{
				RegisterTargetDetector(nearestRadar);
				break;
			}
			await UniTask.Delay(10000);
			if (cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private void DatalinkTargetSearch()
	{
		if (!manual && currentWeaponStation.Ammo > 0 && !stowed)
		{
			bool lineOfSight = currentWeaponStation.WeaponInfo.targetRequirements.lineOfSight;
			potentialTargets = attachedUnit.NetworkHQ.GetTargetsWithinCone(potentialTargets, elevationTransform, firingCones, lineOfSight);
			ChooseTarget(clearAfterSearch: true);
		}
	}

	private void FireSupportTargetSearch()
	{
		if (!manual && currentWeaponStation.Ammo > 0)
		{
			bool lineOfSight = currentWeaponStation.WeaponInfo.targetRequirements.lineOfSight;
			potentialTargets = attachedUnit.NetworkHQ.GetTargetsWithinCone(potentialTargets, elevationTransform, firingCones, lineOfSight);
			ChooseTarget(clearAfterSearch: true);
		}
	}

	private void Turret_OnDetectTarget(Unit unit)
	{
		if (!manual && !potentialTargets.Contains(unit))
		{
			potentialTargets.Add(unit);
		}
	}

	private void Turret_OnCompleteScan()
	{
		if (!manual && Time.timeSinceLevelLoad - lastTargetAssessment > targetAssessmentInterval)
		{
			ChooseTarget(clearAfterSearch: true);
		}
	}

	public Unit GetAttachedUnit()
	{
		return attachedUnit;
	}

	public bool HasAmmo()
	{
		return weaponStations[0].Ammo > 0;
	}

	private void ChooseTarget(bool clearAfterSearch)
	{
		if (disabled || attachedUnit.disabled)
		{
			return;
		}
		lastTargetAssessment = Time.timeSinceLevelLoad;
		if (target != null)
		{
			target.onDisableUnit -= Turret_OnTargetDisabled;
			if (attachedUnit.NetworkHQ.trackingDatabase.ContainsKey(target.persistentID))
			{
				attachedUnit.NetworkHQ.trackingDatabase[target.persistentID].attackers--;
			}
		}
		Unit unit = target;
		WeaponStation weaponStation = currentWeaponStation;
		target = null;
		float priorityThreshold = 0f;
		foreach (Unit potentialTarget in potentialTargets)
		{
			AssessTargetPriority(potentialTarget, ref priorityThreshold);
		}
		if (target != null)
		{
			target.onDisableUnit += Turret_OnTargetDisabled;
			attachedUnit.NetworkHQ.trackingDatabase[target.persistentID].attackers++;
		}
		Span<PersistentID> span = stackalloc PersistentID[1];
		span[0] = ((target != null) ? target.persistentID : PersistentID.None);
		if (target != unit || currentWeaponStation != weaponStation)
		{
			if (attachedUnit is Aircraft aircraft)
			{
				if (currentWeaponStation.TurretCount() > 1)
				{
					aircraft.SetStationTurretTarget(currentWeaponStation.Number, turretIndex, span[0]);
				}
				else
				{
					aircraft.SetStationTargets(currentWeaponStation.Number, span);
				}
			}
			else
			{
				attachedUnit.RpcSetStationTargets(currentWeaponStation.Number, span);
			}
		}
		if (clearAfterSearch)
		{
			potentialTargets.Clear();
		}
	}

	private void AssessTargetPriority(Unit targetCandidate, ref float priorityThreshold)
	{
		TrackingInfo trackingData = attachedUnit.NetworkHQ.GetTrackingData(targetCandidate.persistentID);
		if (trackingData == null || targetCandidate == null || targetCandidate.disabled || targetCandidate.NetworkHQ == null)
		{
			return;
		}
		Vector3 targetVector = targetCandidate.transform.position - base.transform.position;
		if (!FiringConeChecker.VectorWithinFiringCones(firingCones, targetVector, out var _))
		{
			return;
		}
		float magnitude = targetVector.magnitude;
		if (onlyDefensive && (!(targetCandidate is Missile missile) || missile.targetID != attachedUnit.persistentID))
		{
			return;
		}
		WeaponStation[] array = weaponStations;
		foreach (WeaponStation weaponStation in array)
		{
			float num = weaponStation.WeaponInfo.targetRequirements.maxRange / magnitude;
			if (num < 0.7f)
			{
				continue;
			}
			OpportunityThreat opportunityThreat = CombatAI.AnalyzeTarget(weaponStation, attachedUnit, trackingData, armorTierOptimism, magnitude, 1.2f);
			float num2 = opportunityThreat.opportunity * (1f + opportunityThreat.threat);
			if (num2 != 0f)
			{
				if (weaponStation.Reloading || weaponStation.Ammo <= 0)
				{
					num2 *= 0.01f;
				}
				if (num < 1f || magnitude < weaponStation.WeaponInfo.targetRequirements.minRange)
				{
					num2 *= 0.01f;
				}
				if (targetCandidate.definition.armorTier > weaponStation.WeaponInfo.armorTierEffectiveness)
				{
					num2 *= 0.2f;
				}
				if (targetAcquisitionMode == TargetAcquisitionMode.datalink && targetCandidate.speed > 0f && num < 2f)
				{
					num2 *= 0.6f;
				}
				if (num2 > priorityThreshold)
				{
					target = targetCandidate;
					currentWeaponStation = weaponStation;
					priorityThreshold = num2;
				}
			}
		}
	}

	public Unit GetTarget()
	{
		return target;
	}

	public WeaponStation GetWeaponStation()
	{
		return currentWeaponStation;
	}

	public bool HasFiringCone(out Vector3 firingConeForward, out float angle)
	{
		firingConeForward = base.transform.forward;
		angle = 0f;
		if (firingCones.Length == 0)
		{
			return false;
		}
		firingConeForward = firingCones[0].GetDirection();
		angle = firingCones[0].GetAngle();
		return true;
	}

	public bool IsOnTarget()
	{
		return onTarget;
	}

	public float GetTraverseRange()
	{
		return traverseRange;
	}

	public Weapon GetWeapon()
	{
		if (aimSafetyWeapon != null)
		{
			return aimSafetyWeapon;
		}
		return weaponStations[0].Weapons[0];
	}

	public WeaponStation[] GetWeaponStations()
	{
		return weaponStations;
	}

	private void Turret_OnPartDamage(UnitPart.OnApplyDamage e)
	{
		if (e.hitPoints < 0f)
		{
			disabled = true;
			base.enabled = false;
		}
	}

	private void Turret_OnDetachedFromUnit(UnitPart unitPart)
	{
		disabled = true;
		base.enabled = false;
	}

	public Vector3 GetDirection()
	{
		return elevationTransform.position + elevationTransform.forward * 10000f;
	}

	private void Turret_OnTargetDetectorDisabled(TargetDetector targetDetector)
	{
		if (!attachedUnit.disabled)
		{
			targetDetectors.Remove(targetDetector);
			if (!stowed && targetDetectors.Count == 0 && targetAcquisitionMode == TargetAcquisitionMode.searchForRadar && attachedUnit.NetworkHQ != null)
			{
				SearchForRadar().Forget();
			}
		}
	}

	private void Turret_OnUnitDisabled(Unit unit)
	{
		DestroyTurret();
	}

	public void DestroyTurret()
	{
		attachedUnit.onDisableUnit -= Turret_OnUnitDisabled;
		if (target != null && attachedUnit.NetworkHQ != null && attachedUnit.NetworkHQ.trackingDatabase.TryGetValue(target.persistentID, out var value))
		{
			value.attackers--;
		}
		foreach (TargetDetector targetDetector in targetDetectors)
		{
			DeregisterTargetDetector(targetDetector);
		}
		UnitPart[] array = criticalParts;
		foreach (UnitPart part in array)
		{
			DeregisterPart(part);
		}
		UnityEngine.Object.Destroy(this);
	}

	public void SetTarget(PersistentID id, byte stationIndex)
	{
		if (!attachedUnit.disabled)
		{
			UnitRegistry.TryGetUnit(id, out target);
			currentWeaponStation = attachedUnit.weaponStations[stationIndex];
			maxRange = currentWeaponStation.WeaponInfo.targetRequirements.maxRange;
			base.enabled = target != null || manual;
			aimSolver.SetTarget(attachedUnit, target, currentWeaponStation.Weapons[0].transform, currentWeaponStation.WeaponInfo);
		}
	}

	public void SetTargetFromController(Unit target)
	{
		if (this.target != target)
		{
			Span<PersistentID> span = stackalloc PersistentID[1];
			span[0] = ((target != null) ? target.persistentID : PersistentID.None);
			attachedUnit.RpcSetStationTargets(0, span);
		}
	}

	public void SetStowed(bool stowed)
	{
		if (this.stowed == stowed)
		{
			return;
		}
		this.stowed = stowed;
		base.enabled = !stowed;
		if (stowed)
		{
			StowForward().Forget();
		}
		if (targetAcquisitionMode != TargetAcquisitionMode.searchForRadar || !(attachedUnit.NetworkHQ != null))
		{
			return;
		}
		if (stowed)
		{
			attachedUnit.radar = null;
			foreach (TargetDetector targetDetector in targetDetectors)
			{
				DeregisterTargetDetector(targetDetector);
			}
			if (fireControl != null)
			{
				fireControl.DeregisterTurret(this);
			}
		}
		else
		{
			SearchForRadar().Forget();
		}
	}

	public void SetManual(bool enabled)
	{
		manual = enabled;
		base.enabled = true;
	}

	public void SetVector(Vector3 vector)
	{
		manualVector = vector;
	}

	public void AssessWeapons()
	{
		foreach (WeaponStation weaponStation in attachedUnit.weaponStations)
		{
			weaponStation.AssessAmmo();
		}
	}

	private void Turret_OnTargetDisabled(Unit unit)
	{
		ChooseTarget(clearAfterSearch: false);
	}

	private void AimTurret(Vector3 aimVector)
	{
		targetRange = aimVector.magnitude;
		Vector3 vector = aimVector;
		if (!FiringConeChecker.VectorWithinFiringCones(firingCones, vector, out var nearestAllowableVector))
		{
			vector = nearestAllowableVector;
		}
		float num = Vector3.Angle(aimVector, vector);
		traverseError = TargetCalc.GetAngleOnAxis(base.transform.forward, vector, base.transform.up);
		traverseAngle += Mathf.Clamp(traverseError, (0f - traverseRate) * Time.fixedDeltaTime, traverseRate * Time.fixedDeltaTime);
		if (traverseRange < 360f)
		{
			traverseAngle = Mathf.Clamp(traverseAngle, 0f - traverseRange, traverseRange);
		}
		elevationError = TargetCalc.GetAngleOnAxis(elevationTransform.forward, vector, base.transform.right);
		elevationAngle += Mathf.Clamp(elevationError, (0f - elevationRate) * Time.fixedDeltaTime, elevationRate * Time.fixedDeltaTime);
		elevationAngle = Mathf.Clamp(elevationAngle, 0f - maxElevation, 0f - minElevation);
		elevationTransform.localEulerAngles = new Vector3(elevationAngle, 0f, 0f);
		base.transform.localEulerAngles = new Vector3(0f, traverseAngle, 0f);
		onTarget = num + Mathf.Abs(traverseError) + Mathf.Abs(elevationError) < 1f;
		if (aimSafetyWeapon != null)
		{
			aimSafetyWeapon.Safety = !onTarget;
		}
	}

	private bool AimTurret(WeaponStation weaponStation)
	{
		if (currentWeaponStation.Ammo == 0)
		{
			return false;
		}
		if (firesWithoutAiming)
		{
			GlobalPosition b = attachedUnit.GlobalPosition();
			attachedUnit.NetworkHQ.TryGetKnownPosition(target, out var knownPosition);
			targetRange = FastMath.Distance(knownPosition, b);
			return targetRange < maxRange;
		}
		aimVector = aimSolver.GetAimVector(out targetRange);
		Vector3 vector = aimVector;
		if (!FiringConeChecker.VectorWithinFiringCones(firingCones, vector, out var nearestAllowableVector))
		{
			vector = nearestAllowableVector;
		}
		float num = Vector3.Angle(aimVector, vector);
		traverseError = TargetCalc.GetAngleOnAxis(base.transform.forward, vector, base.transform.up);
		traverseAngle += Mathf.Clamp(traverseError, (0f - traverseRate) * Time.fixedDeltaTime, traverseRate * Time.fixedDeltaTime);
		if (Mathf.Abs(traverseError) < 45f && minElevation != maxElevation)
		{
			elevationError = TargetCalc.GetAngleOnAxis(elevationTransform.forward, vector, base.transform.right);
			elevationAngle += Mathf.Clamp(elevationError, (0f - elevationRate) * Time.fixedDeltaTime, elevationRate * Time.fixedDeltaTime);
			elevationAngle = Mathf.Clamp(elevationAngle, 0f - maxElevation, 0f - minElevation);
			elevationTransform.localEulerAngles = new Vector3(elevationAngle, 0f, 0f);
		}
		else
		{
			elevationError = 0f;
		}
		if (!weaponStation.WeaponInfo.boresight && !weaponStation.WeaponInfo.gun)
		{
			elevationError = 0f;
		}
		if (traverseRange < 360f)
		{
			traverseAngle = Mathf.Clamp(traverseAngle, 0f - traverseRange, traverseRange);
		}
		base.transform.localEulerAngles = new Vector3(0f, traverseAngle, 0f);
		onTarget = num + Mathf.Abs(traverseError) + Mathf.Abs(elevationError) < 1f;
		if (aimSafetyWeapon != null)
		{
			aimSafetyWeapon.Safety = !onTarget;
		}
		return onTarget;
	}

	public Vector2 GetTurretAimError()
	{
		float angleOnAxis = TargetCalc.GetAngleOnAxis(base.transform.forward, aimVector, base.transform.up);
		float angleOnAxis2 = TargetCalc.GetAngleOnAxis(base.transform.forward, aimVector, base.transform.right);
		return new Vector2(angleOnAxis, angleOnAxis2);
	}

	private async UniTask StowForward()
	{
		_ = base.destroyCancellationToken;
		while (stowed)
		{
			traverseAngle -= Mathf.Clamp(traverseAngle, (0f - traverseRate) * Time.deltaTime, traverseRate * Time.deltaTime);
			base.transform.localEulerAngles = new Vector3(0f, traverseAngle, 0f);
			elevationAngle -= Mathf.Clamp(elevationAngle, (0f - elevationRate) * Time.deltaTime, elevationRate * Time.deltaTime);
			elevationTransform.localEulerAngles = new Vector3(elevationAngle, 0f, 0f);
			if (Mathf.Abs(traverseAngle) < 1f && Mathf.Abs(elevationAngle) < 1f)
			{
				base.transform.localEulerAngles = Vector3.zero;
				elevationTransform.localEulerAngles = Vector3.zero;
				break;
			}
			await UniTask.Yield();
		}
	}

	public void SetObservedBullet(BulletSim.Bullet bullet)
	{
		if (!(target == null) && !(target.speed > 100f))
		{
			aimSolver.SetObservedBullet(bullet);
		}
	}

	private void FixedUpdate()
	{
		bool flag = !manual && targetAcquisitionMode != TargetAcquisitionMode.datalink && targetAcquisitionMode != TargetAcquisitionMode.fireSupport && targetDetectors.Count == 0 && fireControl == null;
		if (attachedUnit == null || attachedUnit.disabled || disabled || flag || stowed)
		{
			base.enabled = false;
			return;
		}
		if (manual)
		{
			if (target == null)
			{
				if (aircraft.LocalSim && SceneSingleton<CameraStateManager>.i.currentState == SceneSingleton<CameraStateManager>.i.cockpitState)
				{
					SetVector(SceneSingleton<CameraStateManager>.i.transform.forward);
					if (Time.timeSinceLevelLoad - lastVectorSent > 0.2f)
					{
						aircraft.SetTurretVector(currentWeaponStation.Number, manualVector);
						lastVectorSent = Time.timeSinceLevelLoad;
					}
				}
				AimTurret(manualVector);
				return;
			}
		}
		else if (target == null || target.disabled)
		{
			base.enabled = false;
			return;
		}
		if (AimTurret(currentWeaponStation))
		{
			timeOnTarget += Time.deltaTime;
		}
		else
		{
			timeOnTarget = 0f;
		}
		if (manual || !attachedUnit.LocalSim || !(targetRange < maxRange) || !(timeOnTarget > lockTime) || !currentWeaponStation.Ready() || !(target.NetworkHQ != attachedUnit.NetworkHQ))
		{
			return;
		}
		Transform transform = elevationTransform;
		if ((!(attachedUnit == SceneSingleton<CombatHUD>.i.aircraft) || SceneSingleton<CombatHUD>.i.turretAutoControl) && (firesWithoutAiming || !Physics.Linecast(transform.position, transform.position + transform.forward * 200f, out hit, ~(int)PhysicsLayers.ExclusionZonesMask) || !(hit.distance < targetRange - (target.maxRadius + 50f))))
		{
			currentWeaponStation.Fire(attachedUnit, target);
			if (attachedUnit.IsServer && !currentWeaponStation.WeaponInfo.gun && newTargetSearchAfterFire && fireControl == null)
			{
				ChooseTarget(clearAfterSearch: false);
			}
		}
	}
}
