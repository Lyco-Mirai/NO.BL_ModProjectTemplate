using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MobileArtilleryAI : MonoBehaviour
{
	private enum AIState
	{
		TargetSearch = 0,
		DestinationSearch = 1,
		DrivingToDestination = 2,
		Attack = 3,
		Rearm = 4
	}

	[SerializeField]
	private AIState currentState;

	[SerializeField]
	private Turret turret;

	[SerializeField]
	private GroundVehicle vehicle;

	[SerializeField]
	private float attackDurationMin = 60f;

	[SerializeField]
	private float attackDurationMax = 120f;

	private GlobalPosition destination;

	private GlobalPosition? lastAttackPosition;

	private GlobalPosition? currentTargetPosition;

	private Weapon weapon;

	private float spawnTime;

	private float maxRange;

	private float minRange;

	private bool acknowledgedReload;

	private bool leftRoad;

	private void Awake()
	{
		vehicle.onInitialize += MobileArtilleryAI_OnInitialize;
		maxRange = turret.GetWeapon().info.targetRequirements.maxRange;
		minRange = turret.GetWeapon().info.targetRequirements.minRange;
		weapon = turret.GetWeapon();
	}

	private void MobileArtilleryAI_OnInitialize()
	{
		if (vehicle.IsServer && vehicle.NetworkHQ != null)
		{
			spawnTime = NetworkSceneSingleton<MissionManager>.i.MissionTime;
			vehicle.StowTurrets(stowed: true);
			TargetSearch().Forget();
		}
	}

	private async UniTask TargetSearch()
	{
		currentState = AIState.TargetSearch;
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.WaitForSeconds(5f + Random.value);
		if (vehicle.GetHoldPosition())
		{
			Attack().Forget();
			return;
		}
		while (!cancel.IsCancellationRequested && !vehicle.disabled && currentState == AIState.TargetSearch)
		{
			GlobalPosition globalPosition = vehicle.GlobalPosition();
			GlobalPosition globalPosition2 = globalPosition;
			float num = 0f;
			float num2 = 0f;
			if (MissionPosition.TryGetClosestPosition(vehicle, out var globalPosition3))
			{
				num = FastMath.Distance(vehicle.GlobalPosition(), globalPosition3);
				globalPosition2 = globalPosition3;
			}
			if (vehicle.NetworkHQ.TryGetNearestGroundEnemy(vehicle.GlobalPosition(), out var nearestUnit))
			{
				num2 = FastMath.Distance(nearestUnit.GetPosition(), globalPosition3);
				globalPosition = nearestUnit.GetPosition();
			}
			if (num > 0f || num2 > 0f)
			{
				currentTargetPosition = ((num < num2) ? globalPosition2 : globalPosition);
				DestinationSearch().Forget();
			}
			await UniTask.WaitForSeconds(10);
		}
	}

	private async UniTask DestinationSearch()
	{
		currentState = AIState.DestinationSearch;
		CancellationToken cancel = base.destroyCancellationToken;
		if (NetworkSceneSingleton<MissionManager>.i.MissionTime < spawnTime + 10f && FastMath.InRange(currentTargetPosition.Value, vehicle.GlobalPosition(), maxRange) && FastMath.OutOfRange(currentTargetPosition.Value, vehicle.GlobalPosition(), minRange))
		{
			Attack().Forget();
			return;
		}
		while (!cancel.IsCancellationRequested && !vehicle.disabled && currentState == AIState.DestinationSearch)
		{
			if (TryFindNewDestination(vehicle.GlobalPosition(), currentTargetPosition.Value))
			{
				DriveToDestination().Forget();
			}
			await UniTask.WaitForSeconds(10);
		}
	}

	private async UniTask DriveToDestination()
	{
		currentState = AIState.DrivingToDestination;
		CancellationToken cancel = base.destroyCancellationToken;
		int driveTime = 0;
		vehicle.StowTurrets(stowed: true);
		while (!cancel.IsCancellationRequested && !vehicle.disabled && currentState == AIState.DrivingToDestination)
		{
			driveTime++;
			bool flag = !lastAttackPosition.HasValue || FastMath.OutOfRange(lastAttackPosition.Value, vehicle.GlobalPosition(), 3000f);
			bool flag2 = currentTargetPosition.HasValue && FastMath.InRange(vehicle.GlobalPosition(), currentTargetPosition.Value, maxRange) && FastMath.OutOfRange(vehicle.GlobalPosition(), currentTargetPosition.Value, minRange);
			if ((driveTime > 10 && vehicle.rb.isKinematic) || FastMath.InRange(destination, vehicle.GlobalPosition(), 50f) || (flag && flag2))
			{
				if (!leftRoad)
				{
					vehicle.LeaveRoad(250f);
					leftRoad = true;
				}
				else if (Mathf.Abs(vehicle.speed) < 1f)
				{
					Attack().Forget();
				}
			}
			await UniTask.WaitForSeconds(1);
		}
	}

	private async UniTask SeekRearm()
	{
		currentState = AIState.Rearm;
		CancellationToken cancel = base.destroyCancellationToken;
		GlobalPosition currentDestination = vehicle.GlobalPosition();
		float ammoMassNeeded = weapon.info.massPerRound * (float)(weapon.GetFullAmmo() - weapon.GetAmmoTotal());
		while (!cancel.IsCancellationRequested)
		{
			if (vehicle.NetworkHQ.RearmMissionController.TryGetNearestRearmer(vehicle.GlobalPosition(), out var nearestRearmer, includeShips: false, ammoMassNeeded))
			{
				if (PlayerSettings.debugVis && SceneSingleton<CameraStateManager>.i.followingUnit == vehicle)
				{
					Debug.Log($"Found rearmer: {nearestRearmer.gameObject.name}, range: {FastMath.Distance(nearestRearmer.transform.GlobalPosition(), currentDestination)}");
				}
				if (FastMath.OutOfRange(nearestRearmer.transform.GlobalPosition(), currentDestination, 50f) || (Mathf.Abs(vehicle.speed) < 1f && FastMath.OutOfRange(nearestRearmer.transform.GlobalPosition(), vehicle.GlobalPosition(), 50f)))
				{
					currentDestination = nearestRearmer.transform.GlobalPosition();
					Vector3 vector = FastMath.NormalizedDirection(currentDestination, vehicle.GlobalPosition());
					if (leftRoad)
					{
						vehicle.ReturnToRoad(currentDestination + vector * 30f);
						leftRoad = false;
					}
					else
					{
						vehicle.UnitCommand.SetDestination(currentDestination + vector * 30f, playerCommand: false);
					}
				}
			}
			if (weapon.ammo > 0)
			{
				TargetSearch().Forget();
				break;
			}
			await UniTask.WaitForSeconds(10);
		}
	}

	private async UniTask Attack()
	{
		acknowledgedReload = false;
		currentState = AIState.Attack;
		CancellationToken cancel = base.destroyCancellationToken;
		lastAttackPosition = vehicle.GlobalPosition();
		int startingAmmo = weapon.ammo;
		vehicle.StowTurrets(stowed: false);
		float timer = Random.Range(attackDurationMin, attackDurationMax);
		while (!cancel.IsCancellationRequested && !vehicle.disabled && currentState == AIState.Attack)
		{
			vehicle.StopImmediately();
			if (weapon.GetReloadProgress() > 0f || weapon.ammo <= 0)
			{
				if (!acknowledgedReload)
				{
					acknowledgedReload = true;
					turret.SetStowed(stowed: true);
					if (weapon.ammo <= 0 && !vehicle.GetHoldPosition())
					{
						SeekRearm().Forget();
						break;
					}
					if (timer <= 0f)
					{
						TargetSearch().Forget();
					}
				}
			}
			else if (acknowledgedReload)
			{
				acknowledgedReload = false;
				turret.SetStowed(stowed: false);
			}
			if (timer <= 0f && weapon.ammo == startingAmmo)
			{
				TargetSearch().Forget();
			}
			if (!vehicle.GetHoldPosition())
			{
				timer -= 1f;
			}
			await UniTask.WaitForSeconds(1);
		}
	}

	private bool TryFindNewDestination(GlobalPosition currentPosition, GlobalPosition targetPosition)
	{
		targetPosition.y = 0f;
		currentPosition.y = 0f;
		Vector3 vector = FastMath.NormalizedDirection(targetPosition, currentPosition);
		GlobalPosition globalPosition = targetPosition + vector * maxRange * 0.67f;
		Vector3 vector2 = Random.insideUnitSphere * 3000f;
		if (NetworkSceneSingleton<LevelInfo>.i.roadNetwork.TryGetNearestPoint(globalPosition + vector2, out var nearestPoint, out var _))
		{
			destination = nearestPoint;
			vehicle.ReturnToRoad(destination);
			leftRoad = false;
			return true;
		}
		return false;
	}
}
