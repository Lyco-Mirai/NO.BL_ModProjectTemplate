using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedAircraft : SavedUnit
	{
		public bool playerControlled;

		public int playerControlledPriority;

		public SavedLoadout savedLoadout;

		public int livery;

		public LiveryKey.KeyType liveryType;

		public string liveryName = "";

		public float fuel = 1f;

		public float skill = 1f;

		public float bravery = 0.5f;

		public float startingSpeed;

		public LiveryKey liveryKey
		{
			get
			{
				return new LiveryKey(liveryType, livery, liveryName);
			}
			set
			{
				value.Save(out liveryType, out livery, out liveryName);
			}
		}

		public SavedAircraft()
		{
		}

		public SavedAircraft(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
