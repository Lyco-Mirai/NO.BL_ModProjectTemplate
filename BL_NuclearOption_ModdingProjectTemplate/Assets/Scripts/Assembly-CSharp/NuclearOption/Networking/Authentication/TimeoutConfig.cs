using System;

namespace NuclearOption.Networking.Authentication
{
	[Serializable]
	public class TimeoutConfig
	{
		public int FreeDisconnectAllowed = 3;

		public float RapidDisconnectWindowSeconds = 60f;

		public float BaseTimeoutSeconds = 30f;

		public float TimeoutMultiplier = 2f;

		public float MaxTimeoutSeconds = 21600f;

		public float SpamConnectPenaltySeconds = 10f;

		public float PenaltyDecayRateSeconds = 600f;

		public float ErrorKickBaseTimeout = 300f;

		public bool BanOnRepeatedErrorKicks = true;

		public int MaxErrorKicksBeforeBan = 5;

		public float ErrorKickDecayRateSeconds = 7200f;

		public NuclearOptionPlayerErrorFlags.Names InstantBanErrorFlags = NuclearOptionPlayerErrorFlags.Names.Critical;

		public bool CheckInstantBanErrorFlags(NuclearOptionPlayerErrorFlags.Names flags)
		{
			return (InstantBanErrorFlags & flags) != 0;
		}
	}
}
