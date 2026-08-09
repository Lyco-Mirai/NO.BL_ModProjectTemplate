using System;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class NoSavedObjective : SavedObjective
	{
		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.None;

		public NoSavedObjective()
		{
		}

		public NoSavedObjective(string name)
			: base(name)
		{
		}
	}
}
