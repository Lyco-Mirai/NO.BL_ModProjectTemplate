using System.Collections.Generic;
using Mirage;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

public static class WeaponChecker
{
	public static bool CanAffordRearm(Player player, WeaponManager weaponManager, bool includeCargo)
	{
		float num = GetLoadoutFullValue(weaponManager, includeCargo) - weaponManager.GetCurrentValue(includeCargo);
		int num2 = GetLoadoutFullWarheads(weaponManager) - weaponManager.GetCurrentWarheads();
		Debug.Log("Checking rearm affordability: current value: " + UnitConverter.ValueReading(weaponManager.GetCurrentValue(includeCargo)) + ", rearmed value: " + UnitConverter.ValueReading(GetLoadoutFullValue(weaponManager, includeCargo)) + ", difference: " + UnitConverter.ValueReading(num) + ", player funds: " + UnitConverter.ValueReading(player.Allocation));
		/*if (num <= player.Allocation)
		{
			return num2 <= player.HQ.GetWarheadStockpile();
		}*/
		return false;
	}

	public static float GetLoadoutFullValue(WeaponManager weaponManager, bool includeCargo)
	{
		float num = 0f;
		Loadout currentLoadout = weaponManager.GetCurrentLoadout();
		int num2 = weaponManager.hardpointSets.Length;
		for (int i = 0; i < currentLoadout.weapons.Count && i < num2; i++)
		{
			int count = weaponManager.hardpointSets[i].hardpoints.Count;
			WeaponMount weaponMount = currentLoadout.weapons[i];
			if (!(weaponMount == null) && !(weaponMount.info == null) && (includeCargo || !weaponMount.info.cargo))
			{
				num += (float)count * (weaponMount.emptyCost + weaponMount.info.costPerRound * (float)weaponMount.ammo);
			}
		}
		return num;
	}

	public static int GetLoadoutFullWarheads(WeaponManager weaponManager)
	{
		int num = 0;
		Loadout currentLoadout = weaponManager.GetCurrentLoadout();
		int num2 = weaponManager.hardpointSets.Length;
		for (int i = 0; i < currentLoadout.weapons.Count && i < num2; i++)
		{
			int count = weaponManager.hardpointSets[i].hardpoints.Count;
			WeaponMount weaponMount = currentLoadout.weapons[i];
			if (!(weaponMount == null) && !(weaponMount.info == null) && weaponMount.info.nuclear)
			{
				num += count * weaponMount.ammo;
			}
		}
		return num;
	}

	public static void VetLoadout(AircraftDefinition definition, Loadout requestedLoadout, Player player, Airbase airbase, INetworkPlayer sender)
	{
		FactionHQ currentHQ = airbase.CurrentHQ;
		WeaponManager weaponManager = definition.unitPrefab.GetComponent<Aircraft>().weaponManager;
		float budget = player.Allocation;
		int num = weaponManager.hardpointSets.Length;
		if (requestedLoadout.weapons.Count > num)
		{
			ColorLog<WeaponMount>.InfoWarn($"VetLoadout failed: requested loadout has more weapons than hardpoints ({requestedLoadout.weapons.Count} > {num})");
			sender?.SetError(5, NuclearOptionPlayerErrorFlags.InvalidLoadout);
			requestedLoadout.weapons.RemoveRange(num, requestedLoadout.weapons.Count - num);
		}
		for (int i = 0; i < requestedLoadout.weapons.Count; i++)
		{
			HardpointSet hardpointSet = weaponManager.hardpointSets[i];
			WeaponMount weaponMount = requestedLoadout.weapons[i];
			if (!(weaponMount == null) && !VetWeapon(ref budget, weaponMount, hardpointSet, requestedLoadout, player, currentHQ, airbase, out var failReason, out var failCost))
			{
				ColorLog<WeaponMount>.InfoWarn($"VetWeapon failed for {player} {weaponMount} {failReason}");
				requestedLoadout.weapons[i] = null;
				sender?.SetError(failCost, NuclearOptionPlayerErrorFlags.InvalidLoadout);
			}
		}
	}

	public static bool VetWeapon(ref float budget, WeaponMount requestedMount, HardpointSet hardpointSet, Loadout requestedLoadout, Player player, FactionHQ hq, Airbase airbase, out string failReason, out int failCost)
	{
		if (!MountAllowedConflict(hardpointSet, requestedLoadout))
		{
			failReason = "Mount has conflict";
			failCost = 1;
			return false;
		}
		if (!MountAllowedHQ(requestedMount, hq))
		{
			failReason = "Mount is disabled";
			failCost = 1;
			return false;
		}
		if (!MountAllowedAirbase(requestedMount, airbase))
		{
			failReason = "Mount unavailable at airbase";
			failCost = 1;
			return false;
		}
		if (!MountAllowedHardpoint(requestedMount, hardpointSet))
		{
			failReason = "Mount is not in hardpointSet's options";
			failCost = 5;
			return false;
		}
		if (!MountAllowedCost(requestedMount, hardpointSet, ref budget))
		{
			failReason = "Mount exceeds player budget";
			failCost = 1;
			return false;
		}
		if (!MountAllowedNuclear(requestedMount, hardpointSet, airbase, player, hq))
		{
			failCost = 1;
			failReason = "Mount fails nuclear restrictions or warhead supply";
			return false;
		}
		failReason = null;
		failCost = 0;
		return true;
	}

	public static bool MountAllowedCost(WeaponMount mount, HardpointSet hardpointSet, ref float budget)
	{
		if (mount == null)
		{
			return true;
		}
		int count = hardpointSet.hardpoints.Count;
		float num = mount.emptyCost;
		if (mount.info != null)
		{
			num += mount.info.costPerRound * (float)mount.ammo;
		}
		float num2 = (float)count * num;
		if (num2 <= budget)
		{
			budget -= num2;
			return true;
		}
		return false;
	}

	public static bool MountAllowedHardpoint(WeaponMount mount, HardpointSet hardpointSet)
	{
		return hardpointSet.weaponOptions.Contains(mount);
	}

	public static bool MountAllowedHQ(WeaponMount mount, FactionHQ hq)
	{
		if (mount == null)
		{
			return true;
		}
		if (mount.NotAllowed(MissionManager.AllowEventContent))
		{
			return false;
		}
		if (hq != null && hq.restrictedWeapons.Contains(mount.name))
		{
			return false;
		}
		return true;
	}

	public static bool MountAllowedAirbase(WeaponMount mount, Airbase airbase)
	{
		if (airbase == null || mount == null || mount.info == null)
		{
			return true;
		}
		if (mount.info.rearmShip)
		{
			return !airbase.AttachedAirbase;
		}
		return true;
	}

	public static bool MountAllowedConflict(HardpointSet hardpointSet, Loadout requestedLoadout)
	{
		return !hardpointSet.BlockedByOtherHardpoint(requestedLoadout);
	}

	public static bool MountAllowedNuclear(WeaponMount mount, HardpointSet hardpointSet, Airbase airbase, Player player, FactionHQ hq)
	{
		if (mount == null || mount.info == null || !mount.info.nuclear || airbase == null)
		{
			return true;
		}
		int num = ((player != null) ? player.PlayerRank : 0);
		if (!MissionManager.AllowTactical() || (float)num < NetworkSceneSingleton<MissionManager>.i.tacticalMinRank)
		{
			return false;
		}
		if (mount.info.strategic && (!MissionManager.AllowStrategic() || (float)num < NetworkSceneSingleton<MissionManager>.i.strategicMinRank))
		{
			return false;
		}
		return hardpointSet.hardpoints.Count * mount.ammo <= airbase.GetWarheads();
	}

	public static void GetAvailableWeaponsNonAlloc(Player player, HardpointSet hardpointSet, Airbase airbase, FactionHQ hq, bool allowEmpty, List<WeaponMount> outAvailable)
	{
		outAvailable.Clear();
		foreach (WeaponMount weaponOption in hardpointSet.weaponOptions)
		{
			if (weaponOption != null && MountAllowedHQ(weaponOption, hq) && MountAllowedAirbase(weaponOption, airbase) && MountAllowedNuclear(weaponOption, hardpointSet, airbase, player, hq))
			{
				outAvailable.Add(weaponOption);
			}
		}
		if (allowEmpty && outAvailable.Count == 0)
		{
			outAvailable.Add(null);
		}
	}

	public static void PreferNukesFilter(int warheadsAvailable, HardpointSet hardpointSet, List<WeaponMount> listToFilter)
	{
		if (warheadsAvailable <= 0 || !MissionManager.AllowTactical())
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (WeaponMount item in listToFilter)
		{
			if (!(item == null) && !(item.info == null) && item.info.nuclear && hardpointSet.hardpoints.Count * item.ammo <= warheadsAvailable)
			{
				if (item.info.strategic)
				{
					flag2 = true;
				}
				else
				{
					flag = true;
				}
			}
		}
		if (!flag && !flag2)
		{
			return;
		}
		for (int num = listToFilter.Count - 1; num >= 0; num--)
		{
			WeaponMount weaponMount = listToFilter[num];
			if (weaponMount == null || weaponMount.info == null)
			{
				listToFilter.RemoveAt(num);
			}
			else if (!weaponMount.info.nuclear)
			{
				listToFilter.RemoveAt(num);
			}
			else if (hardpointSet.hardpoints.Count * weaponMount.ammo > warheadsAvailable)
			{
				listToFilter.RemoveAt(num);
			}
			else if (flag2 && !weaponMount.info.strategic)
			{
				listToFilter.RemoveAt(num);
			}
		}
		if (flag2)
		{
			foreach (WeaponMount item2 in listToFilter)
			{
				_ = item2;
			}
			return;
		}
		if (!flag)
		{
			return;
		}
		foreach (WeaponMount item3 in listToFilter)
		{
			_ = item3;
		}
	}
}
