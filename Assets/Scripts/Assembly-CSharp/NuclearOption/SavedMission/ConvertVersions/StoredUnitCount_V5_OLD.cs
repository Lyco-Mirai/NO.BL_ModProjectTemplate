using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class StoredUnitCount_V5_OLD
	{
		public string UnitType;

		public int Count;
	}
}
