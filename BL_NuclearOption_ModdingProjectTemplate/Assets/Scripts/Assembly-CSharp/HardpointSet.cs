using System;
using System.Collections.Generic;
using JamesFrowen.ScriptableVariables;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class HardpointSet
{
	[Header("Config")]
	[FormerlySerializedAs("hardpointName")]
	public string name;

	[Tooltip("List of HardpointIndexes. if other hardpoint has mount, then this must have no mount")]
	public List<byte> precludingHardpointSets;

	[Tooltip("Optional: link with previous hardpointSet for symmetry")]
	public bool SymmetryWithPrev;

	[Tooltip("Optional name of hardpointSet when combined as symmetry pair")]
	public string SymmetryName;

	[Tooltip("What mounts to show as options. Note: other mounts can still be loaded by customizing the mission json")]
	public List<WeaponMount> weaponOptions = new List<WeaponMount>();

	[Header("References")]
	[Tooltip("Runtime mount")]
	[ReadOnly]
	[HideInInspector]
	public WeaponMount weaponMount;

	public List<Hardpoint> hardpoints = new List<Hardpoint>();

	public void SpawnMounts(Aircraft aircraft, WeaponMount weaponMount)
	{
		this.weaponMount = weaponMount;
		foreach (Hardpoint hardpoint in hardpoints)
		{
			hardpoint.SpawnMount(aircraft, weaponMount);
			if (weaponMount != null)
			{
				hardpoint.ShowPylon(weaponLoaded: true);
			}
		}
	}

	public void RemoveMounts()
	{
		foreach (Hardpoint hardpoint in hardpoints)
		{
			hardpoint.ShowPylon(weaponLoaded: false);
		}
		if (weaponMount == null)
		{
			return;
		}
		weaponMount = null;
		foreach (Hardpoint hardpoint2 in hardpoints)
		{
			hardpoint2.RemoveMount();
		}
	}

	public bool BlockedByOtherHardpoint(Loadout loadout)
	{
		for (int i = 0; i < loadout.weapons.Count; i++)
		{
			if (!(loadout.weapons[i] == null) && precludingHardpointSets.Contains((byte)i))
			{
				return true;
			}
		}
		return false;
	}
}
