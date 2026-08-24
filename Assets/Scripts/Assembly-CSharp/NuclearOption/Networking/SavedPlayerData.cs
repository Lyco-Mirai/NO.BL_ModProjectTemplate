using System.Collections.Generic;

namespace NuclearOption.Networking
{
	public class SavedPlayerData
	{
		public bool Rejoined;

		public FactionHQ Faction;

		public float Score;

		public int Rank;

		public float Allocation;

		public int PlayerIndex;

		public readonly List<OwnedAirframe> OwnedAirframes = new List<OwnedAirframe>();

		public void Save(Player player)
		{
			//Faction = player.HQ;
			Score = player.PlayerScore;
			Rank = player.PlayerRank;
			Allocation = player.Allocation;
			PlayerIndex = player.PlayerIndex;
			OwnedAirframes.Clear();
			OwnedAirframes.AddRange(player.OwnedAirframes);
		}

		public void OnRejoinFaction(Player player)
		{
			player.AddAllocation(Allocation);
			if (Allocation > 0f)
			{
				Faction.AddFunds(0f - Allocation);
			}
			player.SetScore(Score);
			player.SetRank(Rank, setScoreOffset: true);
			player.OwnedAirframes.AddRange(OwnedAirframes);
		}

		public void Clear()
		{
			Faction = null;
			Rejoined = false;
			Score = 0f;
			Rank = 0;
			Allocation = 0f;
			PlayerIndex = 0;
			OwnedAirframes.Clear();
		}
	}
}
