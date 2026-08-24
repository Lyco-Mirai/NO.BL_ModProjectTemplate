using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class MissionFaction
	{
		public string factionName = "";

		public bool preventJoin;

		public bool preventDonation;

		public List<UnitCount> supplies = new List<UnitCount>();

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

		public Restrictions restrictions = new Restrictions();

		public Override<PositionRotation> cameraStartPosition;

		[NonSerialized]
		public FactionHQ FactionHQ;

		public MissionFaction()
		{
		}

		public MissionFaction(string factionName)
		{
			this.factionName = factionName;
		}
	}
}
