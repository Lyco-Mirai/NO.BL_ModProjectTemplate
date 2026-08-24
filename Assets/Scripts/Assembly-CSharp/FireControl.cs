using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FireControl : MonoBehaviour
{
	[Serializable]
	private class Deployable
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Vector3 stowedAngle;

		[SerializeField]
		private Vector3 deployedAngle;

		[SerializeField]
		private float deployRate;

		private float deployAmount;

		public bool TryDeploy()
		{
			deployAmount += deployRate * Time.deltaTime;
			transform.localEulerAngles = Vector3.Lerp(stowedAngle, deployedAngle, deployAmount);
			return deployAmount >= 1f;
		}

		public bool TryStow()
		{
			deployAmount -= deployRate * Time.deltaTime;
			transform.localEulerAngles = Vector3.Lerp(stowedAngle, deployedAngle, deployAmount);
			return deployAmount <= 0f;
		}
	}

	private enum TargetAcquisitionMode
	{
		parentUnitTargetDetector = 0,
		assignedTargetDetectors = 1,
		searchForRadar = 2,
		datalink = 3,
		strategicStrike = 4
	}

	private readonly struct ScoredTarget
	{
		public readonly Unit target;

		public readonly float score;

		public ScoredTarget(Unit target, float score)
		{
			this.target = target;
			this.score = score;
		}
	}

	private class FireControlTarget
	{
		private readonly OpportunityThreat opportunityThreat;

		private float shotsRequired;

		public readonly TrackingInfo trackingInfo;

		public FireControlTarget(OpportunityThreat opportunityThreat, TrackingInfo trackingInfo, WeaponStation weaponStation)
		{
			this.opportunityThreat = opportunityThreat;
			this.trackingInfo = trackingInfo;
			shotsRequired = (trackingInfo.TryGetUnit(out var unit) ? (weaponStation.WeaponInfo.CalcAttacksNeeded(unit) - (float)(trackingInfo.missileAttacks + trackingInfo.attackers)) : 0f);
		}

		public bool StillRelevant(FactionHQ hq)
		{
			if (trackingInfo.TryGetUnit(out var unit) && !unit.disabled && unit.NetworkHQ != null)
			{
				return unit.NetworkHQ != hq;
			}
			return false;
		}

		public float GetCombinedScore()
		{
			return opportunityThreat.GetCombinedScore();
		}

		public void QueueAttack(List<QueuedAttack> queuedAttacks, WeaponStation weaponStation, out bool finished)
		{
			shotsRequired -= 1f;
			if (trackingInfo.TryGetUnit(out var unit))
			{
				queuedAttacks.Add(new QueuedAttack(unit, trackingInfo, weaponStation));
			}
			finished = shotsRequired <= 0f;
		}
	}

	private struct QueuedAttack
	{
		private readonly Unit target;

		private readonly WeaponStation weaponStation;

		private readonly TrackingInfo trackingInfo;

		public QueuedAttack(Unit target, TrackingInfo trackingInfo, WeaponStation weaponStation)
		{
			this.target = target;
			this.trackingInfo = trackingInfo;
			this.weaponStation = weaponStation;
			trackingInfo.attackers++;
		}

		public bool StillValid(FactionHQ hq)
		{
			if (target != null && !target.disabled && target.NetworkHQ != null)
			{
				return target.NetworkHQ != hq;
			}
			return false;
		}

		public void Fire(Unit attachedUnit)
		{
			weaponStation.Fire(attachedUnit, target);
			trackingInfo.attackers--;
		}

		public void Cancel(Unit attachedUnit)
		{
			trackingInfo.attackers--;
		}
	}

	[SerializeField]
	private TargetAcquisitionMode targetAcquisitionMode;

	private List<WeaponStation> subscribedWeaponStations = new List<WeaponStation>();

	[SerializeField]
	private float targetAssessmentInterval;

	[SerializeField]
	private float salvoInterval;

	[SerializeField]
	private float planningTimePerFire;

	[SerializeField]
	private Unit attachedUnit;

	private float maxRange;

	private float minRange;

	private List<TrackingInfo> allTargets = new List<TrackingInfo>();

	private List<FireControlTarget> salvoTargets = new List<FireControlTarget>();

	private List<ScoredTarget> scoredTargets = new List<ScoredTarget>();

	private List<QueuedAttack> queuedAttacks = new List<QueuedAttack>();

	[SerializeField]
	private Radar radar;

	[SerializeField]
	private List<Turret> turrets = new List<Turret>();

	[SerializeField]
	private List<Turret> availableTurrets = new List<Turret>();

	[SerializeField]
	private Deployable[] deployables;

	private WeaponStation weaponStation;

	private float lastTargetSort;

	private bool deployed;

	private List<Missile> currentMissiles = new List<Missile>();

	private bool planningSalvo;

	private Dictionary<WeaponStation, int> stationAmmo = new Dictionary<WeaponStation, int>();

	private void Awake()
	{
		attachedUnit.onInitialize += FireControl_OnInitialize;
		attachedUnit.onDisableUnit += FireControl_OnUnitDisabled;
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			DeployAll().Forget();
		}
	}

	private void FireControl_OnInitialize()
	{
		if (attachedUnit is Ship ship)
		{
			planningTimePerFire /= Mathf.Max(ship.skill, 0.1f);
		}
		if (attachedUnit.IsServer)
		{
			if (targetAcquisitionMode == TargetAcquisitionMode.datalink)
			{
				this.StartSlowUpdate(targetAssessmentInterval, HQTargetAssessment);
				attachedUnit.onRegisterMissile += FireControl_OnRegisterMissile;
				attachedUnit.onDeregisterMissile += FireControl_OnDeregisterMissile;
			}
			if (targetAcquisitionMode == TargetAcquisitionMode.searchForRadar && attachedUnit.NetworkHQ != null)
			{
				attachedUnit.NetworkHQ.RegisterFireControl(this);
				SearchForRadar().Forget();
				this.StartSlowUpdateDelayed(1f, DistributeTurretTargets);
			}
			if (targetAcquisitionMode == TargetAcquisitionMode.strategicStrike && attachedUnit.NetworkHQ != null)
			{
				StrategicTargetCheck().Forget();
			}
		}
	}

	private void FireControl_OnUnitDisabled(Unit attachedUnit)
	{
		if (targetAcquisitionMode == TargetAcquisitionMode.searchForRadar && attachedUnit.NetworkHQ != null)
		{
			attachedUnit.NetworkHQ.DeregisterFireControl(this);
		}
		foreach (Turret turret in turrets)
		{
			turret.FireControlDisabled();
		}
	}

	private void FireControl_OnTurretDisabled(Unit turretUnit)
	{
		for (int num = turrets.Count - 1; num >= 0; num--)
		{
			Unit unit = turrets[num].GetAttachedUnit();
			if (unit.disabled)
			{
				unit.onDisableUnit += FireControl_OnTurretDisabled;
				turrets.RemoveAt(num);
			}
		}
	}

	private async UniTask SearchForRadar()
	{
		if (this == null)
		{
			return;
		}
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(250);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		while (radar == null)
		{
			if (attachedUnit.NetworkHQ.TryGetRadar(attachedUnit.GlobalPosition(), 300f, out var nearestRadar))
			{
				RegisterRadar(nearestRadar);
			}
			await UniTask.Delay(4000);
			if (cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private async UniTask StrategicTargetCheck()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		Weapon weapon = turrets[0].GetWeapon();
		await UniTask.Delay(UnityEngine.Random.Range(5000, 10000));
		while (!cancel.IsCancellationRequested)
		{
			float currentEscalation = NetworkSceneSingleton<MissionManager>.i.currentEscalation;
			float strategicThreshold = NetworkSceneSingleton<MissionManager>.i.strategicThreshold;
			bool flag = weapon.GetAmmoLoaded() > 0;
			if (attachedUnit.speed < 1f && flag && currentEscalation > strategicThreshold && !planningSalvo)
			{
				HQTargetAssessment();
			}
			await UniTask.Delay(10000);
		}
	}

	private async UniTask DeployAll()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		bool finishedDeploying = deployables.Length == 0;
		while (!finishedDeploying && !cancel.IsCancellationRequested)
		{
			finishedDeploying = true;
			Deployable[] array = deployables;
			foreach (Deployable deployable in array)
			{
				finishedDeploying = finishedDeploying && deployable.TryDeploy();
			}
			await UniTask.Yield();
		}
	}

	private void RegisterRadar(Radar radar)
	{
		this.radar = radar;
		radar.GetAttachedUnit().onDisableUnit += FireControl_OnRadarDisabled;
		foreach (Turret turret in turrets)
		{
			turret.GetAttachedUnit().radar = radar;
		}
	}

	public bool TryGetRadar(out Radar radar)
	{
		radar = this.radar;
		return radar != null;
	}

	public void DeregisterTurret(Turret turret)
	{
		turrets.Remove(turret);
		turret.GetAttachedUnit().onDisableUnit -= FireControl_OnTurretDisabled;
	}

	public void RegisterTurret(Turret turret)
	{
		turrets.Add(turret);
		weaponStation = turret.GetWeaponStation();
		turret.GetAttachedUnit().onDisableUnit += FireControl_OnTurretDisabled;
	}

	private void DistributeTurretTargets()
	{
		if (radar == null)
		{
			return;
		}
		GlobalPosition a = attachedUnit.GlobalPosition();
		List<Unit> detectedTargets = radar.detectedTargets;
		FactionHQ networkHQ = attachedUnit.NetworkHQ;
		availableTurrets.Clear();
		foreach (Turret turret in turrets)
		{
			if (turret.HasAmmo())
			{
				availableTurrets.Add(turret);
			}
		}
		if (availableTurrets.Count == 0)
		{
			return;
		}
		scoredTargets.Clear();
		foreach (Unit item in detectedTargets)
		{
			if (!(item == null) && !item.disabled)
			{
				float targetDistance = FastMath.Distance(a, item.GlobalPosition());
				OpportunityThreat opportunityThreat = CombatAI.AnalyzeTarget(weaponStation, attachedUnit, networkHQ.GetTrackingData(item.persistentID), 0f, targetDistance);
				float num = opportunityThreat.opportunity * (1f + opportunityThreat.threat);
				if (num > 0f)
				{
					scoredTargets.Add(new ScoredTarget(item, num));
				}
			}
		}
		scoredTargets.Sort(delegate(ScoredTarget scoredTarget, ScoredTarget b)
		{
			float score = scoredTarget.score;
			return score.CompareTo(b.score);
		});
		for (int num2 = 0; num2 < availableTurrets.Count; num2++)
		{
			Unit targetFromController = ((num2 < scoredTargets.Count) ? scoredTargets[num2].target : null);
			availableTurrets[num2].SetTargetFromController(targetFromController);
		}
	}

	private void FireControl_OnRadarDisabled(Unit unit)
	{
		unit.onDisableUnit -= FireControl_OnRadarDisabled;
		foreach (Turret turret in turrets)
		{
			turret.SetTargetFromController(null);
		}
		SearchForRadar().Forget();
	}

	private void FireControl_OnRegisterMissile(Missile missile)
	{
		currentMissiles.Add(missile);
	}

	private void FireControl_OnDeregisterMissile(Missile missile)
	{
		currentMissiles.Remove(missile);
	}

	public void SubscribeWeaponStation(WeaponStation weaponStation)
	{
		maxRange = Mathf.Max(maxRange, weaponStation.Weapons[0].info.targetRequirements.maxRange);
		minRange = Mathf.Min(minRange, weaponStation.Weapons[0].info.targetRequirements.minRange);
		subscribedWeaponStations.Add(weaponStation);
	}

	private void HQTargetAssessment()
	{
		if (attachedUnit.disabled || subscribedWeaponStations.Count == 0 || attachedUnit.NetworkHQ == null)
		{
			return;
		}
		bool flag = false;
		allTargets = attachedUnit.NetworkHQ.GetTargetsWithinRange(allTargets, base.transform, maxRange, requireLineOfSight: false);
		WeaponStation weaponStation = subscribedWeaponStations[0];
		foreach (TrackingInfo allTarget in allTargets)
		{
			OpportunityThreat opportunityThreat = CombatAI.AnalyzeTarget(weaponStation, attachedUnit, allTarget);
			if (opportunityThreat.GetCombinedScore() > 0f && !SalvoListContainsTarget(allTarget) && allTarget.TryGetUnit(out var unit) && weaponStation.WeaponInfo.CalcAttacksNeeded(unit) - (float)allTarget.missileAttacks > 0f && !TargetHasInboundMissiles(unit))
			{
				salvoTargets.Add(new FireControlTarget(opportunityThreat, allTarget, weaponStation));
				flag = true;
			}
		}
		if (flag)
		{
			salvoTargets.Sort((FireControlTarget a, FireControlTarget b) => a.GetCombinedScore().CompareTo(b.GetCombinedScore()));
		}
		if (!planningSalvo && salvoTargets.Count > 0)
		{
			PlanSalvo().Forget();
		}
	}

	private bool SalvoListContainsTarget(TrackingInfo trackedTarget)
	{
		foreach (FireControlTarget salvoTarget in salvoTargets)
		{
			if (salvoTarget.trackingInfo == trackedTarget)
			{
				return true;
			}
		}
		return false;
	}

	private bool TargetHasInboundMissiles(Unit target)
	{
		foreach (Missile currentMissile in currentMissiles)
		{
			if (currentMissile.targetID == target.persistentID)
			{
				return true;
			}
		}
		return false;
	}

	private async UniTask PlanSalvo()
	{
		planningSalvo = true;
		queuedAttacks.Clear();
		stationAmmo.Clear();
		int num = 0;
		foreach (WeaponStation subscribedWeaponStation in subscribedWeaponStations)
		{
			stationAmmo.Add(subscribedWeaponStation, subscribedWeaponStation.Ammo);
			num += subscribedWeaponStation.Ammo;
		}
		if (num == 0 || salvoTargets.Count == 0)
		{
			planningSalvo = false;
			return;
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = salvoTargets.Count;
		int num5 = salvoTargets.Count - 1;
		while (num > 0 && num5 >= 0 && num4 > 0 && num3 < 100)
		{
			num3++;
			if (num3 > 100)
			{
				break;
			}
			if (num2 >= subscribedWeaponStations.Count)
			{
				num2 = 0;
			}
			WeaponStation key = subscribedWeaponStations[num2];
			if (stationAmmo[key] <= 0)
			{
				num2++;
				continue;
			}
			salvoTargets[num5].QueueAttack(queuedAttacks, key, out var finished);
			stationAmmo[key]--;
			num--;
			num2++;
			if (finished)
			{
				num4--;
				num5--;
			}
		}
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(planningTimePerFire * 1000f * (float)num3));
		if (!cancel.IsCancellationRequested)
		{
			LaunchSalvo().Forget();
		}
	}

	public void DeployOrStowLaunchers(bool deploying)
	{
		DeployOrStow(deploying).Forget();
	}

	private async UniTask DeployOrStow(bool deploying)
	{
		deployed = !deploying;
		CancellationToken cancel = base.destroyCancellationToken;
		bool finished = deployables.Length == 0;
		while (!finished)
		{
			finished = true;
			Deployable[] array = deployables;
			foreach (Deployable deployable in array)
			{
				finished = finished && (deploying ? deployable.TryDeploy() : deployable.TryStow());
			}
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		deployed = deploying;
	}

	private async UniTask LaunchSalvo()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		if (deployables.Length != 0)
		{
			deployed = false;
			if (attachedUnit is GroundVehicle groundVehicle)
			{
				groundVehicle.RpcDeployFireControl(deploy: true);
			}
			while (!deployed)
			{
				await UniTask.Delay(1000);
				if (cancel.IsCancellationRequested)
				{
					return;
				}
			}
			await UniTask.Delay(2000);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		int attackNum = 1;
		foreach (QueuedAttack queuedAttack in queuedAttacks)
		{
			if (!attachedUnit.disabled && queuedAttack.StillValid(attachedUnit.NetworkHQ))
			{
				queuedAttack.Fire(attachedUnit);
				attackNum++;
				await UniTask.Delay((int)(salvoInterval * 1000f));
				if (cancel.IsCancellationRequested)
				{
					return;
				}
			}
			else
			{
				queuedAttack.Cancel(attachedUnit);
			}
		}
		planningSalvo = false;
		salvoTargets.Clear();
		if (deployables.Length == 0)
		{
			return;
		}
		deployed = true;
		if (attachedUnit is GroundVehicle groundVehicle2)
		{
			groundVehicle2.RpcDeployFireControl(deploy: false);
		}
		while (deployed)
		{
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		await UniTask.Delay(2000);
		_ = cancel.IsCancellationRequested;
	}
}
