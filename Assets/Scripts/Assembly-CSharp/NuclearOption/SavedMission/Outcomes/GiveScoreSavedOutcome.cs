using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class GiveScoreSavedOutcome : SavedOutcome
	{
		public bool bothFactions;

		public ChangeType playerFundsType;

		public float playerFunds;

		public ChangeType factionFundsType;

		public float factionFunds;

		public ChangeType playerScoreType;

		public float playerScore;

		public ChangeType rankType;

		public int rank;

		public ChangeType factionScoreType;

		public float factionScore;

		public override OutcomeType OutcomeTypeEnum => OutcomeType.GiveScore;
	}
}
