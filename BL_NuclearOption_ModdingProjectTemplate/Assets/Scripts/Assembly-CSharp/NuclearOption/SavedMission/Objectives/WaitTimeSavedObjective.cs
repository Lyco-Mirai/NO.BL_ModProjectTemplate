using System;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class WaitTimeSavedObjective : SavedObjective
	{
		public float seconds = 5f;

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.WaitSeconds;

		public WaitTimeSavedObjective()
		{
		}

		public WaitTimeSavedObjective(string name)
			: base(name)
		{
		}
	}
}
