using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class Loadout
	{
		[MaxLength(16)]
		public List<WeaponMount> weapons = new List<WeaponMount>();

		public bool AllowedByHQ(WeaponManager weaponManager, FactionHQ hq)
		{
			int num = 0;
			int warheadAvailableForAI = hq.GetWarheadAvailableForAI();
			for (int i = 0; i < weaponManager.hardpointSets.Length; i++)
			{
				HardpointSet hardpointSet = weaponManager.hardpointSets[i];
				if (i >= weapons.Count)
				{
					return false;
				}
				WeaponMount weaponMount = weapons[i];
				if (weaponMount == null || weaponMount.info == null)
				{
					continue;
				}
				if (hq.restrictedWeapons.Contains(weaponMount.name))
				{
					return false;
				}
				if (weaponMount.info.nuclear)
				{
					if (!MissionManager.AllowTactical())
					{
						return false;
					}
					if (weaponMount.info.strategic && !MissionManager.AllowStrategic())
					{
						return false;
					}
					num += weaponMount.ammo * hardpointSet.hardpoints.Count;
				}
			}
			if (num != 0)
			{
				return num <= warheadAvailableForAI;
			}
			return true;
		}
	}
}
