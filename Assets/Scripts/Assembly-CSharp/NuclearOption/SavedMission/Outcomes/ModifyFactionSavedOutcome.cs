using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class ModifyFactionSavedOutcome : SavedOutcome
	{
		public bool bothFactions;

		public Override<float> excessFundsThreshold;

		public Override<float> playerJoinAllowance;

		public Override<float> playerTaxRate;

		public Override<float> regularIncome;

		public Override<float> killReward;

		public Override<bool> preventDonation;

		public Override<float> aiAircraftLimit;

		public Override<float> reduceAIPerFriendlyPlayer;

		public Override<float> addAIPerEnemyPlayer;

		public Override<float> warheadsReserve;

		public Override<float> reserveAirframes;

		public Override<float> extraReservesPerPlayer;

		public Override<float> excessFundsDistributePercent;

		public Override<bool> preventJoin;

		public override OutcomeType OutcomeTypeEnum => OutcomeType.ModifyFaction;
	}
}
