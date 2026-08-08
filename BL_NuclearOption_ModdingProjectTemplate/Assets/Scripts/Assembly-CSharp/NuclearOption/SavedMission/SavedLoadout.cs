using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedLoadout
	{
		[Serializable]
		public struct SelectedMount : IEquatable<SelectedMount>
		{
			public string Key;

			public bool Equals(SelectedMount other)
			{
				return Key == other.Key;
			}

			public readonly WeaponMount GetWeaponMount(HardpointSet hardpointSet)
			{
				if (string.IsNullOrEmpty(Key))
				{
					return null;
				}
				List<WeaponMount> weaponOptions = hardpointSet.weaponOptions;
				if (weaponOptions != null && weaponOptions.Count > 0)
				{
					for (int i = 1; i < weaponOptions.Count; i++)
					{
						WeaponMount weaponMount = weaponOptions[i];
						if (weaponMount == null)
						{
							return null;
						}
						if (weaponMount.jsonKey == Key)
						{
							return weaponMount;
						}
					}
					Debug.LogWarning("Could not find " + Key + " in HardpointSet:" + hardpointSet.name + ".");
					if (Encyclopedia.WeaponLookup.TryGetValue(Key, out var value))
					{
						return value;
					}
					Debug.LogError("Could not find " + Key + " in Encyclopedia or HardpointSet:" + hardpointSet.name);
					return null;
				}
				return null;
			}
		}

		public List<SelectedMount> Selected = new List<SelectedMount>();

		public SavedLoadout()
		{
			Selected = new List<SelectedMount>();
		}

		public Loadout CreateLoadout(GameObject prefab)
		{
			WeaponManager weaponManager = prefab.GetComponent<Aircraft>().weaponManager;
			return CreateLoadout(weaponManager);
		}

		public Loadout CreateLoadout(WeaponManager weaponManager)
		{
			Loadout loadout = new Loadout();
			for (int i = 0; i < Selected.Count && i < weaponManager.hardpointSets.Length; i++)
			{
				HardpointSet hardpointSet = weaponManager.hardpointSets[i];
				WeaponMount weaponMount = Selected[i].GetWeaponMount(hardpointSet);
				if (weaponMount != null && weaponMount.NotAllowed(MissionManager.AllowEventContent))
				{
					ColorLog<SavedLoadout>.InfoWarn("Weapon Mount '" + weaponMount.mountName + "' is disabled or blocked by AllowEventContent setting");
					weaponMount = null;
				}
				loadout.weapons.Add(weaponMount);
			}
			return loadout;
		}
	}
}
