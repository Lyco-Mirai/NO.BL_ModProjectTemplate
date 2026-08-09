using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct QuickLoadMissionFaction_V5_OLD
	{
		public string factionName;

		public int minSortieSize;

		public int maxSortieSize;

		public bool preventJoin;
	}
}
