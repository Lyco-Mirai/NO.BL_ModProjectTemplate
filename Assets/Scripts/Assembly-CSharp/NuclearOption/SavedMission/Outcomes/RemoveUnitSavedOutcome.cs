using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class RemoveUnitSavedOutcome : SavedOutcome
	{
		public List<string> UnitsToRemove = new List<string>();

		public override OutcomeType OutcomeTypeEnum => OutcomeType.RemoveUnit;
	}
}
