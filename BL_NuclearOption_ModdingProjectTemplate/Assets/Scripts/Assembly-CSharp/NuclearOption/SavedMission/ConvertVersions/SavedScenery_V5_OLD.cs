using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedScenery_V5_OLD : SavedUnit_V5_OLD
	{
		public bool indestructible;
	}
}
