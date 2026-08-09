using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class UnitInventory_V5_OLD
	{
		public string AttachedUnitUniqueName;

		public List<StoredUnitCount_V5_OLD> StoredList = new List<StoredUnitCount_V5_OLD>();
	}
}
