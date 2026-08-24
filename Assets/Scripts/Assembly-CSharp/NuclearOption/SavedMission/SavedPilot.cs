using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedPilot : SavedUnit
	{
		public SavedPilot()
		{
		}

		public SavedPilot(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
