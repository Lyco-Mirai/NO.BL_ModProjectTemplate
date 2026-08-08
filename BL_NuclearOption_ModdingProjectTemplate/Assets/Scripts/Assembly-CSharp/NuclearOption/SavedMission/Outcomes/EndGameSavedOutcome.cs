using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class EndGameSavedOutcome : SavedOutcome
	{
		public EndType endType;

		public float endDelay;

		public override OutcomeType OutcomeTypeEnum => OutcomeType.EndGame;
	}
}
