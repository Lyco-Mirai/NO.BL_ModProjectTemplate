using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class CompleteObjectiveSavedOutcome : SavedOutcome
	{
		public CompleteObjectiveOutcome.Options options;

		public List<string> objectivesToStart = new List<string>();

		public override OutcomeType OutcomeTypeEnum => OutcomeType.StopOrCompleteObjective;
	}
}
