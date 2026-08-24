using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public struct QuickLoadMissionFaction
	{
		public string factionName;

		public int minSortieSize;

		public int maxSortieSize;

		public bool preventJoin;
	}
}
