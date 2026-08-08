using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Weapon : MonoBehaviour
{
	public Unit attachedUnit;

	public WeaponInfo info;

	public int ammo;

	public int priority;

	protected WeaponStation weaponStation;

	protected Hardpoint hardpoint;

	protected WeaponMount mount;

	protected float lastFired;

	protected Unit currentTarget;

	public bool Rearmable = true;

	public float RequestRearmLevel = 0.5f;

	[HideInInspector]
	public bool Safety;

	public virtual void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
	}

	public virtual void RemoteSingleFire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
	}

	public virtual void AttachToUnit(Unit unit)
	{
		attachedUnit = unit;
	}

	public virtual void SetTarget(Unit unit)
	{
	}

	public async UniTask TrackFiringVisibility()
	{
		if (Time.timeSinceLevelLoad - lastFired < 4f)
		{
			return;
		}
		lastFired = Time.timeSinceLevelLoad;
		attachedUnit.ModifyVisibility(info.visibilityWhenFired);
		CancellationToken cancel = base.destroyCancellationToken;
		while (Time.timeSinceLevelLoad - lastFired < 4f)
		{
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		attachedUnit.ModifyVisibility(0f - info.visibilityWhenFired);
	}

	public virtual void Rearm(int ammoToRearm, WeaponStation weaponStation)
	{
	}

	public virtual int GetAmmoLoaded()
	{
		return 1;
	}

	public virtual int GetAmmoTotal()
	{
		return 1;
	}

	public virtual int GetFullAmmo()
	{
		return 1;
	}

	public virtual float GetReloadProgress()
	{
		return 0f;
	}

	public virtual bool HasMagazines()
	{
		return false;
	}

	public bool IsAttached()
	{
		if (hardpoint != null && hardpoint.part.IsDetached())
		{
			return false;
		}
		return true;
	}

	public virtual void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount weaponMount)
	{
		AttachToUnit(aircraft);
		this.hardpoint = hardpoint;
		mount = weaponMount;
	}

	public void SetWeaponStation(WeaponStation weaponStation)
	{
		this.weaponStation = weaponStation;
	}

	public void ReportReloading(bool reloading)
	{
		if (weaponStation != null)
		{
			weaponStation.Reloading = reloading;
		}
	}

	public virtual float GetMass()
	{
		return info.massPerRound * (float)GetAmmoTotal();
	}

	public Unit GetTarget()
	{
		return currentTarget;
	}
}
