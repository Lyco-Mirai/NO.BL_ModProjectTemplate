using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5: use Loadout instead, this is only used in v2 mission")]
	public struct LoadoutOld_V5_OLD
	{
		public List<byte> weaponSelections;
	}
}
