using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpecialFlareEjector : Weapon
{
	[SerializeField]
	private GameObject flarePrefab;

	[SerializeField]
	private float ejectionVelocityVariance;

	private bool triggerPulled;

	public Transform ejectionPoint;

	[Tooltip("in seconds")]
	[SerializeField]
	private float ejectionInterval;

	private float lastEjectionTime;

	public override void AttachToUnit(Unit unit)
	{
		base.AttachToUnit(unit);
		base.enabled = false;
	}

	public override void SetTarget(Unit target)
	{
	}

	public override void Fire(Unit firingUnit, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (attachedUnit.disabled || Time.timeSinceLevelLoad - lastEjectionTime < ejectionInterval)
		{
			return;
		}
		lastEjectionTime = Time.timeSinceLevelLoad;
		weaponStation.UpdateLastFired(1);
		if (hardpoint != null)
		{
			if (hardpoint.part.IsDetached())
			{
				return;
			}
			if (info.useWeaponDoors)
			{
				hardpoint.SpringOpenBayDoors();
			}
		}
		triggerPulled = true;
		base.weaponStation = weaponStation;
		base.enabled = true;
		EjectFlare();
	}

	private async UniTask EjectFlare()
	{
		await UniTask.WaitForFixedUpdate();
		base.enabled = true;
		GameObject obj = NetworkSceneSingleton<Spawner>.i.SpawnLocal(flarePrefab, Datum.origin);
		obj.transform.position = ejectionPoint.position;
		Vector3 velocity = attachedUnit.rb.velocity;
		obj.GetComponent<SpecialFlare>().LaunchFlare(ejectionPoint, velocity + ejectionPoint.forward * ((float)Random.Range(-1, 1) * ejectionVelocityVariance));
	}
}
