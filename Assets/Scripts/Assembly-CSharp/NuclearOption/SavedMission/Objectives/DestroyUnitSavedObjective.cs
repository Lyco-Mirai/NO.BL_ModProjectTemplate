using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class DestroyUnitSavedObjective : SavedObjective
	{
		public CompleteOrder completeOrder = CompleteOrder.CompleteAll;

		public float completeSomePercent = 0.5f;

		public List<string> targetUnits = new List<string>();

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.DestroyUnits;

		public DestroyUnitSavedObjective()
		{
		}

		public DestroyUnitSavedObjective(string name)
			: base(name)
		{
		}
	}
}
