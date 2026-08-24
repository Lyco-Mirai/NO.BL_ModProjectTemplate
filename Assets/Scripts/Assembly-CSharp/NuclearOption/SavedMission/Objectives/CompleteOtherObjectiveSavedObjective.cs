using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class CompleteOtherObjectiveSavedObjective : SavedObjective
	{
		public CompleteOrder completeOrder = CompleteOrder.CompleteAll;

		public float completeSomePercent = 0.5f;

		public List<string> targetObjectives = new List<string>();

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.CompleteOtherObjective;

		public CompleteOtherObjectiveSavedObjective()
		{
		}

		public CompleteOtherObjectiveSavedObjective(string name)
			: base(name)
		{
		}
	}
}
