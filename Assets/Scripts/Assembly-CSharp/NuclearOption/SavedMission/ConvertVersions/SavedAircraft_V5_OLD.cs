using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedAircraft_V5_OLD : SavedUnit_V5_OLD
	{
		public bool playerControlled;

		public int playerControlledPriority;

		[Obsolete("replaced by savedLoadout", true)]
		public LoadoutOld_V5_OLD loadout;

		public SavedLoadout_V5_OLD savedLoadout;

		public int livery;

		public LiveryKey.KeyType liveryType;

		public string liveryName;

		public float fuel = 1f;

		public float skill = 1f;

		public float bravery = 0.5f;

		public float startingSpeed;
	}
}
