using System;
using UnityEngine;

[Serializable]
public class Hardpoint
{
	[Serializable]
	private class HardpointPylon
	{
		[SerializeField]
		private bool cargo;

		[SerializeField]
		private WeaponMount mount;

		[SerializeField]
		private Renderer renderer;

		public bool MatchesMount(WeaponMount mount)
		{
			if (mount == null)
			{
				return false;
			}
			if (cargo)
			{
				if (!mount.Cargo)
				{
					return mount.Troops;
				}
				return true;
			}
			if (!(this.mount == mount))
			{
				return this.mount == null;
			}
			return true;
		}

		public void ShowPylon(bool visible)
		{
			renderer.enabled = visible;
		}
	}

	public Transform transform;

	public UnitPart part;

	public BayDoor[] bayDoors;

	public float doorOpenDuration;

	private WeaponMount mount;

	[SerializeField]
	private HardpointPylon[] pylonOptions;

	public Renderer Pylon;

	public Renderer Plug;

	public Weapon[] BuiltInWeapons;

	public Turret[] BuiltInTurrets;

	public int HardpointIndex = -1;

	private GameObject spawnedPrefab;

	public GameObject SpawnMount(Aircraft aircraft, WeaponMount weaponMount)
	{
		if (transform == null || part.IsDetached())
		{
			return null;
		}
		if (spawnedPrefab != null)
		{
			Debug.LogError("attempting to spawn " + weaponMount.mountName + " on pylon which is already occupied!");
		}
		Turret[] builtInTurrets = BuiltInTurrets;
		for (int i = 0; i < builtInTurrets.Length; i++)
		{
			builtInTurrets[i].AttachToWeaponManager(aircraft);
		}
		Weapon[] builtInWeapons = BuiltInWeapons;
		for (int i = 0; i < builtInWeapons.Length; i++)
		{
			Gun gun = (Gun)builtInWeapons[i];
			gun.LoadAmmunition(weaponMount);
			aircraft.weaponManager.RegisterWeapon(gun, weaponMount, this);
		}
		mount = weaponMount;
		ModifyMass(mount.emptyMass);
		ModifyDrag(mount.emptyDrag);
		ModifyRCS(mount.emptyRCS);
		spawnedPrefab = UnityEngine.Object.Instantiate(weaponMount.prefab, transform);
		if (spawnedPrefab.TryGetComponent<ColorableMount>(out var component))
		{
			component.AttachToAircraft(aircraft);
		}
		if (weaponMount.radar)
		{
			spawnedPrefab.GetComponentInChildren<Radar>().AttachToUnit(aircraft);
			AeroPart component2 = spawnedPrefab.GetComponent<AeroPart>();
			if (component2 != null && aircraft.LocalSim)
			{
				component2.CreateRB(aircraft.rb.GetPointVelocity(transform.position), transform.position);
				component2.CreateJoints();
			}
		}
		if (weaponMount.countermeasure)
		{
			spawnedPrefab.GetComponentInChildren<Countermeasure>().AttachToUnit(aircraft);
		}
		builtInWeapons = spawnedPrefab.GetComponentsInChildren<Weapon>();
		foreach (Weapon weapon in builtInWeapons)
		{
			aircraft.weaponManager.RegisterWeapon(weapon, weaponMount, this);
		}
		if (weaponMount.turret)
		{
			spawnedPrefab.GetComponentInChildren<Turret>().AttachToWeaponManager(aircraft);
		}
		return spawnedPrefab;
	}

	public void SpringOpenBayDoors()
	{
		BayDoor[] array = bayDoors;
		foreach (BayDoor bayDoor in array)
		{
			if (bayDoor != null)
			{
				bayDoor.OpenDoor(doorOpenDuration);
			}
		}
	}

	public void ModifyMass(float change)
	{
		if (change != 0f && part != null && part.rb != null)
		{
			part.ModifyMass(change);
		}
	}

	public BayDoor GetCargoDoor()
	{
		if (bayDoors.Length == 0)
		{
			return null;
		}
		return bayDoors[0];
	}

	public WeaponMount GetMount()
	{
		return mount;
	}

	public void ModifyDrag(float change)
	{
		part.ModifyDrag(change);
	}

	public void ModifyRCS(float change)
	{
		part.parentUnit.ModifyRCS(change);
	}

	public void RemoveMount()
	{
		if (mount == null)
		{
			return;
		}
		ModifyRCS(0f - mount.emptyRCS);
		ModifyDrag(0f - mount.emptyDrag);
		ModifyMass(0f - mount.emptyMass);
		Weapon[] builtInWeapons = BuiltInWeapons;
		for (int i = 0; i < builtInWeapons.Length; i++)
		{
			((Gun)builtInWeapons[i]).LoadAmmunition(null);
		}
		mount = null;
		if (!(spawnedPrefab == null))
		{
			if (spawnedPrefab.TryGetComponent<UnitPart>(out var component))
			{
				component.RemoveFromUnit();
			}
			UnityEngine.Object.Destroy(spawnedPrefab);
			spawnedPrefab = null;
		}
	}

	public void ShowPylon(bool weaponLoaded)
	{
		if (weaponLoaded)
		{
			if (Plug != null)
			{
				Plug.enabled = false;
			}
			bool flag = false;
			HardpointPylon[] array = pylonOptions;
			foreach (HardpointPylon hardpointPylon in array)
			{
				if (flag)
				{
					hardpointPylon.ShowPylon(visible: false);
					continue;
				}
				flag = hardpointPylon.MatchesMount(mount);
				hardpointPylon.ShowPylon(flag);
			}
		}
		else
		{
			HardpointPylon[] array = pylonOptions;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ShowPylon(visible: false);
			}
			if (Plug != null)
			{
				Plug.enabled = true;
			}
		}
	}
}
