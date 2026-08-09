using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedContainer : SavedUnit
	{
		public SavedContainer()
		{
		}

		public SavedContainer(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
