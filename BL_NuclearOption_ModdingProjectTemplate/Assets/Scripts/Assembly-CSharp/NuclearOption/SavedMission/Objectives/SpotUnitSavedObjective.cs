using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class SpotUnitSavedObjective : SavedObjective
	{
		public CompleteOrder completeOrder = CompleteOrder.InOrder;

		public float completeSomePercent = 0.5f;

		public List<string> targetUnits = new List<string>();

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.SpotUnit;

		public SpotUnitSavedObjective()
		{
		}

		public SpotUnitSavedObjective(string name)
			: base(name)
		{
		}
	}
}
