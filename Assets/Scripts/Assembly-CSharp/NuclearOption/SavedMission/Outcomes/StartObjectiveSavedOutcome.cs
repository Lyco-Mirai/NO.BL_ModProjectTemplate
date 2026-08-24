using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class StartObjectiveSavedOutcome : SavedOutcome
	{
		public List<string> objectivesToStart = new List<string>();

		public override OutcomeType OutcomeTypeEnum => OutcomeType.StartObjective;
	}
}
