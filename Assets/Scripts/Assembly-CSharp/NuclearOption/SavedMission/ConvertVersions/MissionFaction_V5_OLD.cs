using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class MissionFaction_V5_OLD
	{
		public string factionName;

		public bool preventJoin;

		public bool preventDonation;

		public List<FactionSupply_V5_OLD> supplies = new List<FactionSupply_V5_OLD>();

		public float startingBalance;

		public float playerJoinAllowance = 20f;

		public float playerTaxRate = 0.2f;

		public float regularIncome = 5f;

		public float excessFundsDistributePercent = 0.25f;

		public float killReward = 1f;

		public float airSkillMultiplier = 1f;

		public float surfaceSkillMultiplier = 1f;

		public int startingWarheads;

		public int reserveWarheads;

		public int reserveAirframes;

		public int extraReservesPerPlayer = 1;

		public int AIAircraftLimit = 6;

		public float reduceAIPerFriendlyPlayer = 1f;

		public float addAIPerEnemyPlayer = 1f;

		[Obsolete("Use V2 instead", true)]
		public List<MissionObjective_V5_OLD> objectives = new List<MissionObjective_V5_OLD>();

		public Restrictions_V5_OLD restrictions = new Restrictions_V5_OLD();

		public Override_V5_OLD<PositionRotation_V5_OLD> cameraStartPosition;
	}
}
