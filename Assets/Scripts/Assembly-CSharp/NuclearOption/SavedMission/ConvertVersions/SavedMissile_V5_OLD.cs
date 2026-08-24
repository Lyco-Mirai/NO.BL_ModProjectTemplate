using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedMissile_V5_OLD : SavedUnit_V5_OLD
	{
		public float startingSpeed = 100f;

		public string targetUnitName = "";

		public string guidingUnit = "";
	}
}
