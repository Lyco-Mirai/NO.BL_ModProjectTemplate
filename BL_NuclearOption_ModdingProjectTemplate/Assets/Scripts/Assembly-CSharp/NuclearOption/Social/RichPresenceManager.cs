using System;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Social
{
	public class RichPresenceManager : SceneSingleton<RichPresenceManager>
	{
		[SerializeField]
		private MapLoader mapLoader;

		[SerializeField]
		private DiscordManager discordManager;

		private static PresenceState _currentState;

		private bool _pendingUpdate;

		protected override void Awake()
		{
			base.Awake();
		}

		private string ResolveMapKey(MapKey? mapKeyNullable)
		{
			if (!mapKeyNullable.HasValue)
			{
				return null;
			}
			MapKey mapKey = mapKeyNullable.Value;
			if (mapKey.Type == MapKey.KeyType.None)
			{
				mapKey = SceneSingleton<RichPresenceManager>.i.mapLoader.DefaultMap;
			}
			if (mapKey.Type == MapKey.KeyType.GameWorldPrefab)
			{
				return mapKey.Path;
			}
			return null;
		}

		public static void SetState(GameState context)
		{
			if (!GameManager.IsHeadless)
			{
				if (context == GameState.Menu || context == GameState.Uninitialized)
				{
					_currentState.PartyId = CSteamID.Nil;
					_currentState.MaxPlayers = 0;
					_currentState.CurrentPlayers = 0;
					_currentState.MissionName = null;
					_currentState.AircraftName = null;
					_currentState.AirbaseName = null;
					_currentState.FactionName = null;
					_currentState.MapKey = null;
					_currentState.Activity = PresenceActivity.None;
				}
				_currentState.Context = context;
				_currentState.StartTime = DateTime.UtcNow;
				if ((context == GameState.SinglePlayer || context == GameState.Multiplayer) && _currentState.Activity == PresenceActivity.None)
				{
					_currentState.Activity = PresenceActivity.Spectating;
				}
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetMission(Mission mission)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.MissionName = mission?.Name;
				_currentState.MapKey = SceneSingleton<RichPresenceManager>.i.ResolveMapKey(mission?.MapKey);
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetIsHost(bool isHost)
		{
			if (!GameManager.IsHeadless && _currentState.IsHost != isHost)
			{
				_currentState.IsHost = isHost;
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetActivity(PresenceActivity activity)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.Activity = activity;
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetAircraft(string aircraft, PresenceActivity activity)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.AircraftName = aircraft;
				_currentState.Activity = activity;
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetAirbase(string airbase, PresenceActivity activity)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.AirbaseName = airbase;
				_currentState.Activity = activity;
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetFaction(string faction, PresenceActivity activity)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.FactionName = faction;
				_currentState.Activity = activity;
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetLobby(CSteamID partyId)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.PartyId = partyId;
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void SetPlayerCount(int current, int? max = null)
		{
			if (!GameManager.IsHeadless)
			{
				_currentState.CurrentPlayers = current;
				if (max.HasValue)
				{
					_currentState.MaxPlayers = max.Value;
				}
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		public static void TriggerUpdate()
		{
			if (SceneSingleton<RichPresenceManager>.i != null)
			{
				SceneSingleton<RichPresenceManager>.i._pendingUpdate = true;
			}
		}

		private void LateUpdate()
		{
			if (_pendingUpdate)
			{
				_pendingUpdate = false;
				SteamRichPresenceHelper.UpdatePresence(_currentState);
				if (SceneSingleton<RichPresenceManager>.i.discordManager != null)
				{
					SceneSingleton<RichPresenceManager>.i.discordManager.UpdatePresence(_currentState);
				}
			}
		}
	}
}
