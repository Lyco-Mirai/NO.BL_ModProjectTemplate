using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedBuilding_V5_OLD : SavedUnit_V5_OLD
	{
		[Serializable]
		[Obsolete("V5", true)]
		public class FactoryOptions_V5_OLD
		{
			public string productionType;

			public float productionTime = 900f;
		}

		public bool capturable;

		public string Airbase;

		public FactoryOptions_V5_OLD factoryOptions;

		[Obsolete("from v1, Use globalPosition instead")]
		public float placementOffset;
	}
}
