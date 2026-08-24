using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class ReachUnitsSavedObjective : SavedObjective
	{
		public CompleteOrder completeOrder = CompleteOrder.InOrder;

		public float completeSomePercent = 0.5f;

		public List<SavedReachUnitData> targets = new List<SavedReachUnitData>();

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.ReachUnits;

		public ReachUnitsSavedObjective()
		{
		}

		public ReachUnitsSavedObjective(string name)
			: base(name)
		{
		}
	}
}
