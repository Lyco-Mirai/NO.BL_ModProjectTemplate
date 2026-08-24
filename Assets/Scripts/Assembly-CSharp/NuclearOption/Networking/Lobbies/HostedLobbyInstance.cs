using System;
using Steamworks;

namespace NuclearOption.Networking.Lobbies
{
	public readonly struct HostedLobbyInstance : IEquatable<HostedLobbyInstance>
	{
		public readonly CSteamID Id;

		public bool IsValid => Id.IsValid();

		public HostedLobbyInstance(CSteamID id)
		{
			Id = id;
		}

		public void SetData(string key, string value)
		{
			SteamMatchmaking.SetLobbyData(Id, key, value);
		}

		public void SetStartTime()
		{
			SetData("start_time", LobbyInstance.CreateStartTime());
		}

		public void SetLobbyEnded()
		{
			SetData("name", "Game Ended");
		}

		public void SetCurrentPlayers(int current, int max)
		{
			SteamMatchmaking.SetLobbyData(Id, "open_member_spots", (max - current).ToString());
		}

		public bool Equals(HostedLobbyInstance other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (obj is HostedLobbyInstance other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}
