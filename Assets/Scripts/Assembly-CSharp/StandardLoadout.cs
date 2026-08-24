using System;
using NuclearOption.SavedMission;
using UnityEngine;

[Serializable]
public class StandardLoadout
{
	public bool disabled;

	public string Name;

	public Loadout loadout;

	[Range(0f, 1f)]
	public float FuelRatio;

	public bool AllowedByHQ(WeaponManager weaponManager, FactionHQ hq)
	{
		return loadout.AllowedByHQ(weaponManager, hq);
	}
}
