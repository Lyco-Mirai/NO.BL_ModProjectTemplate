using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedMissile : SavedUnit
	{
		public float startingSpeed = 100f;

		public string targetUnitName = "";

		public string guidingUnit = "";

		public SavedMissile()
		{
		}

		public SavedMissile(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
