using System;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class SuccessfulSortieSavedObjective : SavedObjective
	{
		public float minimumScore;

		public bool additive;

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.SuccessfulSortie;

		public SuccessfulSortieSavedObjective()
		{
		}

		public SuccessfulSortieSavedObjective(string name)
			: base(name)
		{
		}
	}
}
