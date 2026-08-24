using System;

namespace NuclearOption.Networking
{
	[Serializable]
	public class VoteKickConfig
	{
		public struct ClientConfig
		{
			public bool Enabled;

			public float VoteDuration;

			public float ResolutionDisplayTime;

			public float NewVoteLockout;

			public float RequesterCooldown;
		}

		public bool Enabled = true;

		public float PassRatio = 0.6f;

		public int MinVotes = 3;

		public int AutoBanThreshold = 3;

		public float VoteDuration = 45f;

		public float ResolutionDisplayTime = 20f;

		public float NewVoteLockout = 10f;

		public float RequesterCooldown = 300f;

		public static VoteKickConfig CreateDefault()
		{
			return new VoteKickConfig();
		}

		public ClientConfig GetClientConfig()
		{
			return new ClientConfig
			{
				Enabled = Enabled,
				VoteDuration = VoteDuration,
				ResolutionDisplayTime = ResolutionDisplayTime,
				NewVoteLockout = NewVoteLockout,
				RequesterCooldown = RequesterCooldown
			};
		}
	}
}
