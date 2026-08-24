using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class NoSavedOutcome : SavedOutcome
	{
		public override OutcomeType OutcomeTypeEnum => OutcomeType.None;
	}
}
