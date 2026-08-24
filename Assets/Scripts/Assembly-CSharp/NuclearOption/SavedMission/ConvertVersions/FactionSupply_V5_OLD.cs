using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class FactionSupply_V5_OLD
	{
		public string unitType;

		public int count;
	}
}
