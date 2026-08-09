using System;
using Steamworks;

namespace NuclearOption.Networking
{
	[Serializable]
	public struct VoteKickState
	{
		public bool Active;

		public bool Passed;

		public CSteamID TargetID;

		public int YesVotes;

		public int NoVotes;

		public int VotesRequired;

		public int EligibleVoters;

		public double VoteEndTimeNetwork;

		public double LockoutEndTimeNetwork;

		public int VoteID;

		public readonly bool IsVoteActive(CSteamID cSteamID)
		{
			if (Active)
			{
				return TargetID == cSteamID;
			}
			return false;
		}
	}
}
