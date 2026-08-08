using Steamworks;

namespace NuclearOption.Networking.Authentication
{
	public class PlayerTimeout
	{
		public readonly CSteamID SteamId;

		public double LastDisconnectTime;

		public double LastJoinWhileTimeoutLogTime;

		public double LastErrorKickTime;

		public double TimeoutExpiry;

		public int ViolationLevel;

		public int ErrorKickCount;

		public PlayerTimeout(CSteamID id)
		{
			SteamId = id;
			LastDisconnectTime = 0.0;
			TimeoutExpiry = 0.0;
			LastErrorKickTime = 0.0;
		}
	}
}
