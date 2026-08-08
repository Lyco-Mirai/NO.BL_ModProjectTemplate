using NuclearOption.SavedMission;
using Steamworks;

namespace NuclearOption.Social
{
	public static class SteamRichPresenceHelper
	{
		private static PresenceState _lastState;

		private static bool _initialized;

		private static bool _presenceSet;

		public static void UpdatePresence(PresenceState state)
		{
			if (!SteamManager.ClientInitialized)
			{
				return;
			}
			if (!PlayerSettings.steamRichPresenceEnabled)
			{
				if (_presenceSet)
				{
					Clear();
					_lastState = default(PresenceState);
					_presenceSet = false;
				}
				return;
			}
			_presenceSet = true;
			if (!_initialized)
			{
				_initialized = true;
				Clear();
			}
			if (state.MissionName != _lastState.MissionName)
			{
				SteamFriends.SetRichPresence("mission", state.MissionName ?? "");
			}
			if (state.AircraftName != _lastState.AircraftName)
			{
				SteamFriends.SetRichPresence("aircraft", state.AircraftName ?? "");
			}
			if (state.AirbaseName != _lastState.AirbaseName)
			{
				SteamFriends.SetRichPresence("airbase", state.AirbaseName ?? "");
			}
			if (state.FactionName != _lastState.FactionName)
			{
				SteamFriends.SetRichPresence("faction", state.FactionName ?? "");
			}
			if (state.PartyId != _lastState.PartyId)
			{
				if (state.PartyId != CSteamID.Nil)
				{
					SteamFriends.SetRichPresence("steam_player_group", state.PartyId.m_SteamID.ToString());
					SteamFriends.SetRichPresence("connect", $"+join_lobby {state.PartyId.m_SteamID}");
				}
				else
				{
					SteamFriends.SetRichPresence("steam_player_group", null);
					SteamFriends.SetRichPresence("connect", null);
				}
			}
			if (state.CurrentPlayers != _lastState.CurrentPlayers || state.PartyId != _lastState.PartyId)
			{
				if (state.PartyId != CSteamID.Nil)
				{
					SteamFriends.SetRichPresence("steam_player_group_size", state.CurrentPlayers.ToString());
				}
				else
				{
					SteamFriends.SetRichPresence("steam_player_group_size", null);
				}
			}
			if (state.Context != _lastState.Context || state.Activity != _lastState.Activity || state.MissionName != _lastState.MissionName)
			{
				SteamFriends.SetRichPresence("steam_display", GetDisplayToken(state));
			}
			_lastState = state;
		}

		private static void Clear()
		{
			SteamFriends.SetRichPresence("steam_display", "");
			SteamFriends.SetRichPresence("mission", "");
			SteamFriends.SetRichPresence("aircraft", "");
			SteamFriends.SetRichPresence("airbase", "");
			SteamFriends.SetRichPresence("faction", "");
			SteamFriends.SetRichPresence("connect", "");
			SteamFriends.SetRichPresence("steam_player_group", "");
			SteamFriends.SetRichPresence("steam_player_group_size", "");
		}

		private static string GetDisplayToken(PresenceState state)
		{
			switch (state.Context)
			{
			default:
				return "#Status_AtMainMenu";
			case GameState.Editor:
				if (!string.IsNullOrEmpty(state.MissionName))
				{
					return "#Status_EditingMission";
				}
				return "#Status_EditingMission_NoName";
			case GameState.Encyclopedia:
				return "#Status_AtEncyclopedia";
			case GameState.SinglePlayer:
			case GameState.Multiplayer:
				return GetInGameToken(state);
			}
		}

		private static string GetInGameToken(PresenceState state)
		{
			string text = ((string.IsNullOrEmpty(state.MissionName) || state.MissionName == Mission.NullMission.Name) ? "_NoName" : "");
			return state.Activity switch
			{
				PresenceActivity.Flying => "#Status_Flying" + text, 
				PresenceActivity.Spectating => "#Status_Spectating" + text, 
				PresenceActivity.Preparing => "#Status_Preparing" + text, 
				PresenceActivity.SelectingAircraft => "#Status_SelectingAircraft" + text, 
				PresenceActivity.Crashed => "#Status_Crashed" + text, 
				PresenceActivity.Ejected => "#Status_Ejected" + text, 
				PresenceActivity.Landed => "#Status_Landed" + text, 
				_ => "#Status_InMission" + text, 
			};
		}
	}
}
