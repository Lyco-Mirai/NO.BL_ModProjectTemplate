using Steamworks;

namespace NuclearOption.Networking.Lobbies
{
	public struct LobbySearchFilter
	{
		public bool HideFull;

		public bool HideEmpty;

		public bool HidePasswordProtected;

		public MissionPvpType MissionPvpType;

		public FilterServerType ServerType;

		public ELobbyDistanceFilter? distanceFilter;

		public bool ignoreVersionFilter;

		public bool PingDistanceAllowed(int ping)
		{
			return distanceFilter switch
			{
				ELobbyDistanceFilter.k_ELobbyDistanceFilterClose => ping < 50, 
				ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault => ping < 90, 
				ELobbyDistanceFilter.k_ELobbyDistanceFilterFar => ping < 160, 
				_ => true, 
			};
		}
	}
}
