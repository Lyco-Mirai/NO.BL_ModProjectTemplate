using System;
using System.Collections.Generic;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class WeaponStation
{
	[FormerlySerializedAs("weapons")]
	public List<Weapon> Weapons = new List<Weapon>();

	public List<Turret> Turrets = new List<Turret>();

	[FormerlySerializedAs("weaponInfo")]
	[HideInInspector]
	public WeaponInfo WeaponInfo;

	[HideInInspector]
	public byte Number;

	[HideInInspector]
	public bool Cargo;

	[HideInInspector]
	public bool Reloading;

	[HideInInspector]
	public bool WeaponActive;

	[HideInInspector]
	public bool SalvoInProgress;

	[HideInInspector]
	public float LastFiredTime;

	private readonly bool gearSafety;

	private readonly bool groundSafety;

	private readonly bool sortWeapons;

	public int Ammo;

	public int FullAmmo;

	private int weaponIndex;

	public Dictionary<UnitDefinition, OpportunityThreat> TypeLookup = new Dictionary<UnitDefinition, OpportunityThreat>();

	public event Action OnUpdated;

	public WeaponStation(Unit unit, bool cargo, bool gearSafety, bool groundSafety, bool sortWeapons)
	{
		Number = (byte)unit.weaponStations.Count;
		Cargo = cargo;
		this.gearSafety = gearSafety;
		this.groundSafety = groundSafety;
		this.sortWeapons = sortWeapons;
	}

	public void SetStationActive(Aircraft aircraft, bool isActive)
	{
		if (!HasTurret())
		{
			return;
		}
		foreach (Turret turret in Turrets)
		{
			turret.SetManual(isActive && aircraft.Player != null);
		}
		if (isActive && aircraft == SceneSingleton<CombatHUD>.i.aircraft)
		{
			SceneSingleton<AircraftActionsReport>.i.ReportText(WeaponInfo.weaponName + " Turret under pilot control", 4f);
		}
	}

	public void SetTurretVector(Vector3 vector)
	{
		if (Turrets == null)
		{
			return;
		}
		foreach (Turret turret in Turrets)
		{
			turret.SetVector(vector);
		}
	}

	public void RegisterWeapon(Weapon weapon, Aircraft aircraft, WeaponMount weaponMount, Hardpoint hardpoint)
	{
		if (!(weapon is Gun) || !Weapons.Contains(weapon))
		{
			Weapons.Add(weapon);
		}
		weapon.AttachToHardpoint(aircraft, hardpoint, weaponMount);
		weapon.SetWeaponStation(this);
		WeaponInfo = weapon.info;
	}

	public bool SafetyIsOn(Aircraft aircraft)
	{
		if (aircraft.gearState == LandingGear.GearState.LockedRetracted || !gearSafety)
		{
			if (aircraft.radarAlt < 0.5f)
			{
				return groundSafety;
			}
			return false;
		}
		return true;
	}

	public bool Ready()
	{
		if (Time.timeSinceLevelLoad - LastFiredTime > WeaponInfo.fireInterval && Ammo > 0)
		{
			return GetReloadStatusMin() <= 0f;
		}
		return false;
	}

	public float GetReloadStatusMax()
	{
		if (!Reloading)
		{
			return 0f;
		}
		float num = 0f;
		foreach (Weapon weapon in Weapons)
		{
			num = Mathf.Max(num, weapon.GetReloadProgress());
		}
		return num;
	}

	public float GetReloadStatusMin()
	{
		if (!Reloading)
		{
			return 0f;
		}
		float num = 1f;
		foreach (Weapon weapon in Weapons)
		{
			num = Mathf.Min(num, weapon.GetReloadProgress());
		}
		return num;
	}

	public string GetAmmoReadout()
	{
		int num = 0;
		int num2 = 0;
		foreach (Weapon weapon in Weapons)
		{
			num += weapon.GetAmmoLoaded();
			num2 += weapon.GetAmmoTotal();
		}
		if (num2 != num)
		{
			return $"{num} / {Ammo}";
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			return $"{num}";
		}
		return $"{Ammo}";
	}

	public int GetAmmoLoaded()
	{
		int num = 0;
		foreach (Weapon weapon in Weapons)
		{
			num += weapon.GetAmmoLoaded();
		}
		return num;
	}

	public int GetAmmoTotal()
	{
		int num = 0;
		foreach (Weapon weapon in Weapons)
		{
			num += weapon.GetAmmoTotal();
		}
		return num;
	}

	public byte AssignTurret(Turret turret)
	{
		if (Turrets == null)
		{
			Turrets = new List<Turret>();
		}
		Turrets.Add(turret);
		return (byte)(Turrets.Count - 1);
	}

	public bool HasTurret()
	{
		if (Turrets != null)
		{
			return Turrets.Count > 0;
		}
		return false;
	}

	public float TurretTraverseRange()
	{
		if (Turrets.Count == 0)
		{
			return 0f;
		}
		return Turrets[0].GetTraverseRange();
	}

	public int TurretCount()
	{
		return Turrets.Count;
	}

	public bool GetFiringConeDirection(out Vector3 aimDirection, out float coneAngle)
	{
		aimDirection = Vector3.zero;
		coneAngle = 0f;
		if (Turrets == null || Turrets.Count == 0)
		{
			return false;
		}
		if (Turrets[0].HasFiringCone(out var firingConeForward, out var angle))
		{
			aimDirection = firingConeForward;
			coneAngle = angle;
			return true;
		}
		return false;
	}

	public Turret GetTurret()
	{
		if (Turrets == null || Turrets.Count == 0)
		{
			return null;
		}
		return Turrets[0];
	}

	public Vector2 GetTurretRelativeAim()
	{
		Vector2 result = Vector2.zero;
		if (Turrets != null || Turrets.Count > 0)
		{
			result = Turrets[0].GetTurretAimError();
		}
		return result;
	}

	public void Updated()
	{
		this.OnUpdated?.Invoke();
		if (GameManager.GetLocalAircraft(out var localAircraft))
		{
			localAircraft.weaponManager.InvokeOnStationFired();
		}
	}

	public void Rearm(int ammoToRearm)
	{
		FullAmmo = 0;
		foreach (Weapon weapon in Weapons)
		{
			FullAmmo += weapon.GetFullAmmo();
		}
		if (Weapons[0] is MountedMissile)
		{
			int num = 0;
			for (int num2 = Weapons.Count - 1; num2 >= 0; num2--)
			{
				if (Weapons[num2].GetAmmoLoaded() != 1)
				{
					Weapons[num2].Rearm(1, this);
					num++;
					weaponIndex--;
					if (num >= ammoToRearm)
					{
						break;
					}
				}
			}
		}
		else
		{
			int ammoToRearm2 = Mathf.FloorToInt(ammoToRearm / Weapons.Count);
			foreach (Weapon weapon2 in Weapons)
			{
				weapon2.Rearm(ammoToRearm2, this);
			}
		}
		AccountAmmo();
		Reloading = false;
		Updated();
	}

	public float GetAmmoLevel()
	{
		return (float)Ammo / (float)Mathf.Max(FullAmmo, 0);
	}

	public void SetStationTargets(ReadOnlySpan<PersistentID> targetIDs)
	{
		if (targetIDs.Length == 0)
		{
			foreach (Weapon weapon in Weapons)
			{
				weapon.SetTarget(null);
			}
			{
				foreach (Turret turret in Turrets)
				{
					turret.SetTarget(PersistentID.None, Number);
				}
				return;
			}
		}
		int num = 0;
		foreach (Turret turret2 in Turrets)
		{
			if (num >= targetIDs.Length)
			{
				num = 0;
			}
			turret2.SetTarget(targetIDs[num], Number);
			num++;
		}
		num = 0;
		for (int i = 0; i < Weapons.Count; i++)
		{
			if (num >= targetIDs.Length)
			{
				num = 0;
			}
			if (UnitRegistry.TryGetUnit(targetIDs[num], out var unit))
			{
				Weapons[i].SetTarget(unit);
			}
			num++;
		}
	}

	public void SetStationTurretTarget(byte turretIndex, PersistentID targetID)
	{
		if (HasTurret() && turretIndex < Turrets.Count)
		{
			Turrets[turretIndex].SetTarget(targetID, Number);
		}
	}

	public Unit GetStationTarget()
	{
		Unit result = null;
		foreach (Weapon weapon in Weapons)
		{
			result = weapon.GetTarget();
		}
		foreach (Turret turret in Turrets)
		{
			result = turret.GetTarget();
		}
		return result;
	}

	public float PrioritizeByPosition(Transform transform, Aircraft aircraft)
	{
		Vector3 vector = transform.position - aircraft.transform.position;
		vector.y = 0f;
		return vector.sqrMagnitude + 0.1f * Vector3.Dot(vector.normalized, aircraft.transform.right);
	}

	public float GenerateCargoPriority(Weapon mount, Aircraft aircraft)
	{
		Vector3 lhs = mount.transform.position - aircraft.transform.position;
		lhs.y = 0f;
		return Vector3.Dot(lhs, aircraft.transform.forward);
	}

	public void SortWeapons(Aircraft aircraft)
	{
		for (int num = Weapons.Count - 1; num >= 0; num--)
		{
			if (Weapons[num] == null)
			{
				Weapons.RemoveAt(num);
			}
		}
		if (Cargo)
		{
			Weapons.Sort((Weapon a, Weapon b) => GenerateCargoPriority(a, aircraft).CompareTo(GenerateCargoPriority(b, aircraft)));
		}
		if (Weapons.Count < 2)
		{
			return;
		}
		if (!sortWeapons)
		{
			Weapons.Sort((Weapon a, Weapon b) => a.priority.CompareTo(b.priority));
		}
		else
		{
			Weapons.Sort((Weapon a, Weapon b) => (PrioritizeByPosition(b.transform, aircraft) * 0.001f - (float)b.priority).CompareTo(PrioritizeByPosition(a.transform, aircraft) * 0.001f - (float)a.priority));
		}
	}

	public void AssessAmmo()
	{
		if (Weapons.Count > 0 && Weapons[0] != null)
		{
			WeaponInfo = Weapons[0].info;
		}
		foreach (Weapon weapon in Weapons)
		{
			weapon.SetWeaponStation(this);
		}
	}

	public OpportunityThreat CalcOpportunityThreat(UnitDefinition definition, Unit attachedUnit)
	{
		if (TypeLookup == null)
		{
			TypeLookup = new Dictionary<UnitDefinition, OpportunityThreat>();
		}
		if (!TypeLookup.ContainsKey(definition))
		{
			TypeLookup.Add(definition, new OpportunityThreat(definition.GetOpportunity(WeaponInfo.effectiveness) * definition.GetOpportunity(attachedUnit.definition.roleIdentity), attachedUnit.definition.ThreatPosedBy(definition.roleIdentity)));
		}
		return TypeLookup[definition];
	}

	public void RemoteFireAuto(Unit owner)
	{
		float num = 1f;
		foreach (Weapon weapon in Weapons)
		{
			Vector3 inheritedVelocity = ((owner.rb != null) ? owner.rb.velocity : default(Vector3));
			weapon.Fire(owner, null, inheritedVelocity, this, default(GlobalPosition));
			num = (weapon.Rearmable ? weapon.RequestRearmLevel : 100f);
		}
		if (GetAmmoLevel() < num)
		{
			owner.RequestRearm();
		}
	}

	public void RemoteFireSingle(Unit owner)
	{
		float num = 1f;
		foreach (Weapon weapon in Weapons)
		{
			Vector3 inheritedVelocity = ((owner.rb != null) ? owner.rb.velocity : default(Vector3));
			weapon.RemoteSingleFire(owner, null, inheritedVelocity, this, default(GlobalPosition));
			num = weapon.RequestRearmLevel;
		}
		if (GetAmmoLevel() < num)
		{
			owner.RequestRearm();
		}
	}

	public void LaunchMount(Unit owner, Unit target, GlobalPosition aimpoint)
	{
		if (weaponIndex >= Weapons.Count)
		{
			return;
		}
		Weapon weapon = Weapons[weaponIndex];
		int roundsFired = 0;
		if (weapon.IsAttached())
		{
			weapon.Fire(owner, target, owner.rb.velocity, this, aimpoint);
			if (weapon.Rearmable)
			{
				owner.RequestRearm();
			}
		}
		if (!WeaponInfo.troops && !WeaponInfo.sling)
		{
			roundsFired = 1;
			weaponIndex++;
		}
		if (Cargo && weaponIndex < Weapons.Count)
		{
			WeaponInfo = Weapons[weaponIndex].info;
		}
		UpdateLastFired(roundsFired);
		Updated();
	}

	public void Fire(Unit owner, Unit target)
	{
		if (owner is Aircraft aircraft && SafetyIsOn(aircraft))
		{
			return;
		}
		float num = 1f;
		bool flag = false;
		foreach (Weapon weapon in Weapons)
		{
			Vector3 inheritedVelocity = ((owner.rb != null) ? owner.rb.velocity : default(Vector3));
			if (weapon.IsAttached())
			{
				weapon.Fire(owner, target, inheritedVelocity, this, default(GlobalPosition));
				if (weapon.Rearmable)
				{
					flag = true;
					num = Mathf.Min(num, weapon.RequestRearmLevel);
				}
			}
		}
		if (flag && (float)GetAmmoTotal() / (float)FullAmmo < num)
		{
			owner.RequestRearm();
		}
		if (WeaponInfo.fireInterval == 0f)
		{
			owner.SetFiringState(Number, firing: true);
		}
	}

	public void AccountAmmo()
	{
		Ammo = 0;
		FullAmmo = 0;
		foreach (Weapon weapon in Weapons)
		{
			Ammo += weapon.ammo;
			FullAmmo += weapon.GetFullAmmo();
		}
	}

	public void UpdateLastFired(int roundsFired)
	{
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			AccountAmmo();
		}
		else
		{
			Ammo -= roundsFired;
		}
		LastFiredTime = Time.timeSinceLevelLoad;
	}
}
