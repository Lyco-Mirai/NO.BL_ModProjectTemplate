using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedScenery : SavedUnit
	{
		public bool indestructible;

		public SavedScenery()
		{
		}

		public SavedScenery(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
