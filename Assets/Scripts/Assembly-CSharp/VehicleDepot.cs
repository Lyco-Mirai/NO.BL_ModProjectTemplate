using System;
using NuclearOption.Networking;
using UnityEngine;

public class VehicleDepot : Building
{
	[SerializeField]
	private Transform spawnTransform;

	[SerializeField]
	private float spawnCooldown;

	private float lastSpawnedTime;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 10;

	[NonSerialized]
	private const int RPC_COUNT = 23;

	protected override void OnStartClient()
	{
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			return;
		}
		base.transform.position = startPosition.ToLocalPosition();
		RegisterUnit(null);
		InitializeUnit();
		if (base.NetworkHQ != null && NetworkManagerNuclearOption.i.Server.Active)
		{
			base.NetworkHQ.AddDepot(this);
		}
		if (!base.IsServer || !(repairTime < 999999f))
		{
			return;
		}
		foreach (UnitPart item in partLookup)
		{
			item.onApplyDamage += base.Building_OnPartDamage;
		}
	}

	public bool TrySpawnVehicle(VehicleDefinition vehicleDefinition)
	{
		if (Time.timeSinceLevelLoad - lastSpawnedTime < spawnCooldown)
		{
			return false;
		}
		lastSpawnedTime = Time.timeSinceLevelLoad;
		NetworkSceneSingleton<Spawner>.i.SpawnVehicle(vehicleDefinition.unitPrefab, spawnTransform.GlobalPosition() + Vector3.up * vehicleDefinition.spawnOffset.y, spawnTransform.rotation, Vector3.zero, base.NetworkHQ, null, 1f, holdPosition: false, null).MoveFromDepot();
		return true;
	}

	public override void OnRepairComplete()
	{
		base.OnRepairComplete();
		if (base.NetworkHQ != null && NetworkManagerNuclearOption.i.Server.Active)
		{
			base.NetworkHQ.AddDepot(this);
		}
	}

	private void MirageProcessed()
	{
	}

	protected override int GetRpcCount()
	{
		return 23;
	}
}
