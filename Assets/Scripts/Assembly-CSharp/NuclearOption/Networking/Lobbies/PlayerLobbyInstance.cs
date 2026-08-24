using Steamworks;

namespace NuclearOption.Networking.Lobbies
{
	public class PlayerLobbyInstance : LobbyInstance
	{
		private const string OWNER_KEY = "OWNER";

		private readonly CSteamID id;

		public override CSteamID LobbyId => id;

		public override bool DedicatedServer => false;

		public override string HostAddress => GetData("HostAddress");

		public override string UdpAddress => GetData("UDP_Address");

		public override string UdpPort => GetData("UDP_Port");

		public PlayerLobbyInstance(CSteamID id)
		{
			this.id = id;
		}

		protected override string GetData(string key)
		{
			return SteamMatchmaking.GetLobbyData(id, key);
		}

		public override int? CalculatePing()
		{
			if (SteamLobby.EstimatePing(GetData("HostPing"), out var ping))
			{
				return ping;
			}
			return null;
		}

		public override bool GetPlayerCounts(out int current, out int max)
		{
			string lobbyData = SteamMatchmaking.GetLobbyData(id, "max_members");
			string lobbyData2 = SteamMatchmaking.GetLobbyData(id, "open_member_spots");
			if (string.IsNullOrEmpty(lobbyData) || string.IsNullOrEmpty(lobbyData2))
			{
				current = SteamMatchmaking.GetNumLobbyMembers(id);
				max = SteamMatchmaking.GetLobbyMemberLimit(id);
				return true;
			}
			if (int.TryParse(lobbyData2, out var result) && int.TryParse(lobbyData, out max))
			{
				current = max - result;
				return true;
			}
			current = 0;
			max = 0;
			return false;
		}

		public override bool IsPasswordProtected(out string shortPassword)
		{
			shortPassword = GetData("short_password");
			return !string.IsNullOrEmpty(shortPassword);
		}
	}
}
