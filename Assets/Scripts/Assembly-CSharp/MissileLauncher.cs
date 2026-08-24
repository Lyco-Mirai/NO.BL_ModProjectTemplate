using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MissileLauncher : Weapon
{
	[SerializeField]
	private float fireInterval;

	[SerializeField]
	private float reloadTime;

	[SerializeField]
	public MissileDefinition missile;

	[SerializeField]
	private float railLength;

	[SerializeField]
	private float railSpeed;

	[SerializeField]
	private Vector3 ejectionVelocity;

	[SerializeField]
	private Transform[] launchTransforms;

	[SerializeField]
	private float cellSeperation;

	[SerializeField]
	private int cellColumns = 1;

	[SerializeField]
	private int cellRows = 1;

	[SerializeField]
	private AudioSource launchSound;

	[SerializeField]
	private ParticleSystem launchParticles;

	private bool reloadInProgress;

	private int maxAmmo;

	private int reloadingAmmo;

	private float startedReloadTime;

	private int currentCell;

	private Transform launchTransform;

	private void OnEnable()
	{
		lastFired = 0f - fireInterval;
		for (int i = 0; i < launchTransforms.Length; i++)
		{
			launchTransforms[i].gameObject.SetActive(value: false);
		}
		if (launchTransform == null)
		{
			launchTransform = new GameObject("launchTransform").transform;
			launchTransform.parent = base.transform;
			launchTransform.localPosition = Vector3.zero;
			launchTransform.localRotation = Quaternion.identity;
		}
		maxAmmo = ammo;
		float num = 1f;
		if (attachedUnit is GroundVehicle groundVehicle)
		{
			num = Mathf.Clamp(0.5f * (1f + groundVehicle.skill), 0.5f, 1.5f);
		}
		else if (attachedUnit is Ship ship)
		{
			num = Mathf.Clamp(0.5f * (1f + ship.skill), 0.5f, 1.5f);
		}
		reloadTime /= num;
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (Time.timeSinceLevelLoad - lastFired < fireInterval || ammo <= 0)
		{
			return;
		}
		TrackFiringVisibility().Forget();
		lastFired = Time.timeSinceLevelLoad;
		ammo--;
		if (ammo == 0)
		{
			ReportReloading(reloading: true);
		}
		if (launchTransforms.Length != 0)
		{
			launchTransform = launchTransforms[currentCell].transform;
			currentCell++;
			if (currentCell > launchTransforms.Length - 1)
			{
				currentCell = 0;
			}
		}
		else if (cellColumns > 0)
		{
			launchTransform.localPosition = new Vector3((float)(currentCell % cellColumns) * cellSeperation, (float)(currentCell / cellColumns % cellRows) * cellSeperation, 0f);
			currentCell++;
			if (currentCell > cellRows * cellColumns)
			{
				currentCell = 0;
			}
		}
		weaponStation.UpdateLastFired(1);
		if (owner.LocalSim)
		{
			Vector3 velocity = inheritedVelocity + ejectionVelocity.x * launchTransform.right + ejectionVelocity.y * launchTransform.up + ejectionVelocity.z * launchTransform.forward;
			NetworkSceneSingleton<Spawner>.i.SpawnMissile(missile, launchTransform.position, launchTransform.rotation, velocity, target, owner);
		}
		if (launchParticles != null)
		{
			launchParticles.transform.position = launchTransform.position;
			launchParticles.Play();
		}
		if (launchSound != null)
		{
			launchSound.pitch = Random.Range(0.95f, 1.05f);
			launchSound.Play();
		}
		weaponStation.AccountAmmo();
		weaponStation.Updated();
		if (attachedUnit.IsServer)
		{
			attachedUnit.RpcSyncAmmoCount(weaponStation.Number, weaponStation.Ammo);
		}
		if (attachedUnit != null && attachedUnit.NetworkHQ != null && attachedUnit.IsServer)
		{
			attachedUnit.NetworkHQ.missionStatsTracker.MunitionCost(attachedUnit, info.costPerRound);
		}
	}

	private async UniTask Reload()
	{
		startedReloadTime = Time.timeSinceLevelLoad;
		reloadInProgress = true;
		CancellationToken cancel = base.destroyCancellationToken;
		while (Time.timeSinceLevelLoad < Mathf.Max(startedReloadTime, lastFired + reloadTime))
		{
			weaponStation.Updated();
			await UniTask.WaitForSeconds(1);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		ammo += reloadingAmmo;
		reloadingAmmo = 0;
		currentCell = Mathf.Max(currentCell - reloadingAmmo, 0);
		weaponStation.AccountAmmo();
		weaponStation.Updated();
		reloadInProgress = false;
		ReportReloading(reloading: false);
	}

	public override void Rearm(int ammoToRearm, WeaponStation weaponStation)
	{
		base.weaponStation = weaponStation;
		reloadingAmmo += ammoToRearm;
		if (!reloadInProgress)
		{
			Reload().Forget();
		}
	}

	public override int GetAmmoLoaded()
	{
		return ammo;
	}

	public override int GetAmmoTotal()
	{
		return ammo + reloadingAmmo;
	}

	public override int GetFullAmmo()
	{
		return maxAmmo;
	}

	public override float GetReloadProgress()
	{
		if (!reloadInProgress)
		{
			return 0f;
		}
		return (Time.timeSinceLevelLoad - Mathf.Max(startedReloadTime, lastFired)) / reloadTime;
	}
}
