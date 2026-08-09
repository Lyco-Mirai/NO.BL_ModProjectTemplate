using System;
using Discord.Sdk;
using NuclearOption.Networking.Lobbies;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Social
{
	public class DiscordManager : MonoBehaviour
	{
		private const ulong APPLICATION_ID = 1509204014749716671uL;

		private const string PARTY_ID_PREFIX = "no-party-";

		private const string JOIN_SECRET_PREFIX = "steam-";

		private Client _client;

		private PresenceState _lastState;

		private bool _hasPendingState;

		private bool _clientAvailable = true;

		private const int MAX_CONSECUTIVE_FAILURES = 5;

		private int _consecutiveFailures;

		private Activity _activity;

		private ActivityParty _party;

		private ActivitySecrets _secrets;

		private ActivityAssets _assets;

		private ActivityTimestamps _timestamps;

		private bool _presenceSet;

		public event Action<string> OnJoinRequested;

		private void Awake()
		{
			if (!GameManager.IsHeadless)
			{
				InitClient();
			}
		}

		private void OnDestroy()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}

		private void InitClient()
		{
			_client = new Client();
			_client.AddLogCallback(OnDiscordLog, LoggingSeverity.Warning);
			_client.SetActivityJoinCallback(OnActivityJoin);
			_client.SetApplicationId(1509204014749716671uL);
			if (SteamManager.ClientInitialized)
			{
				_client.RegisterLaunchSteamApplication(1509204014749716671uL, SteamUtils.GetAppID().m_AppId);
			}
			_client.Connect();
			ColorLog<DiscordManager>.Info("Discord client created (RPC mode)");
			if (_hasPendingState)
			{
				_hasPendingState = false;
				PushPresence(_lastState);
			}
		}

		private void OnActivityJoin(string joinSecret)
		{
			InviteJoinModal.TryJoin(delegate
			{
				ColorLog<DiscordManager>.Info("Activity join clicked, secret=" + joinSecret);
				if (!string.IsNullOrEmpty(joinSecret))
				{
					if (ulong.TryParse(joinSecret.StartsWith("steam-") ? joinSecret.Substring("steam-".Length) : joinSecret, out var result))
					{
						CSteamID cSteamID = new CSteamID(result);
						ColorLog<DiscordManager>.Info($"Handoff to SteamLobby: {cSteamID}");
						if (SteamLobby.instance != null)
						{
							SteamLobby.instance.CliJoinBySteamId(cSteamID);
						}
						else
						{
							ColorLog<DiscordManager>.LogError("SteamLobby.instance is null, cannot join");
						}
						this.OnJoinRequested?.Invoke(joinSecret);
					}
					else
					{
						ColorLog<DiscordManager>.LogError("Invalid join secret format: " + joinSecret);
					}
				}
			});
		}

		private void OnDiscordLog(string message, LoggingSeverity severity)
		{
			if (severity == LoggingSeverity.Error)
			{
				ColorLog<DiscordManager>.LogError("[SDK] " + message);
			}
			else
			{
				ColorLog<DiscordManager>.InfoWarn("[SDK] " + message);
			}
		}

		public void UpdatePresence(PresenceState state)
		{
			_lastState = state;
			if (!PlayerSettings.discordRichPresenceEnabled)
			{
				if (_presenceSet && _client != null)
				{
					_client.ClearRichPresence();
					_presenceSet = false;
				}
			}
			else if (_client == null)
			{
				_hasPendingState = true;
			}
			else
			{
				_presenceSet = true;
				PushPresence(state);
			}
		}

		private void PushPresence(PresenceState state)
		{
			if (!_clientAvailable)
			{
				return;
			}
			string stateText = GetStateText(state);
			string detailsText = GetDetailsText(state);
			bool num = state.PartyId != CSteamID.Nil && state.MaxPlayers > 0;
			if (_activity == null)
			{
				_activity = new Activity();
			}
			_activity.SetName("Nuclear Option");
			_activity.SetType(ActivityTypes.Playing);
			_activity.SetState(stateText);
			_activity.SetDetails(detailsText);
			if (num)
			{
				if (_party == null)
				{
					_party = new ActivityParty();
				}
				_party.SetId("no-party-" + state.PartyId.m_SteamID);
				_party.SetCurrentSize(Math.Max(1, state.CurrentPlayers));
				_party.SetMaxSize(state.MaxPlayers);
				_activity.SetParty(_party);
				if (_secrets == null)
				{
					_secrets = new ActivitySecrets();
				}
				_secrets.SetJoin("steam-" + state.PartyId.m_SteamID);
				_activity.SetSecrets(_secrets);
			}
			if (_assets == null)
			{
				_assets = new ActivityAssets();
			}
			_assets.SetLargeImage((!string.IsNullOrEmpty(state.MapKey)) ? state.MapKey.ToLower() : "default");
			if (!string.IsNullOrEmpty(state.MissionName))
			{
				_assets.SetLargeText(state.MissionName);
			}
			_activity.SetAssets(_assets);
			if (state.StartTime != default(DateTime))
			{
				if (_timestamps == null)
				{
					_timestamps = new ActivityTimestamps();
				}
				_timestamps.SetStart((ulong)new DateTimeOffset(state.StartTime).ToUnixTimeSeconds());
				_activity.SetTimestamps(_timestamps);
			}
			_activity.SetSupportedPlatforms(ActivityGamePlatforms.Desktop);
			_client.UpdateRichPresence(_activity, delegate(ClientResult result)
			{
				if (result.Successful())
				{
					_consecutiveFailures = 0;
					_clientAvailable = true;
					ColorLog<DiscordManager>.Info("Rich Presence updated OK");
				}
				else
				{
					_consecutiveFailures++;
					if (_consecutiveFailures >= 5)
					{
						_clientAvailable = false;
						ColorLog<DiscordManager>.InfoWarn($"Discord unavailable ({result.Type()}) after {_consecutiveFailures} failures — disabling further updates");
					}
				}
			});
		}

		private static string GetStateText(PresenceState state)
		{
			switch (state.Context)
			{
			case GameState.Menu:
				return "Main Menu";
			case GameState.Editor:
				if (!string.IsNullOrEmpty(state.MissionName))
				{
					return "Editing: " + state.MissionName;
				}
				return "Mission Editor";
			case GameState.Encyclopedia:
				return "Encyclopedia";
			case GameState.SinglePlayer:
				return "Singleplayer";
			case GameState.Multiplayer:
				return "In Multiplayer";
			default:
				return null;
			}
		}

		private static string GetDetailsText(PresenceState state)
		{
			if (!state.Context.IsSingleOrMultiplayer())
			{
				return null;
			}
			string text = state.Activity switch
			{
				PresenceActivity.Flying => (!string.IsNullOrEmpty(state.AircraftName)) ? ("Flying " + state.AircraftName) : "Flying", 
				PresenceActivity.Spectating => "Spectating", 
				PresenceActivity.SelectingAircraft => "Selecting Aircraft", 
				PresenceActivity.Preparing => "Preparing", 
				PresenceActivity.Crashed => (!string.IsNullOrEmpty(state.AircraftName)) ? ("Crashed (" + state.AircraftName + ")") : "Crashed", 
				PresenceActivity.Ejected => "Ejected", 
				PresenceActivity.Landed => (!string.IsNullOrEmpty(state.AircraftName)) ? ("Landed (" + state.AircraftName + ")") : "Landed", 
				_ => null, 
			};
			string text2 = ((!string.IsNullOrEmpty(state.MissionName)) ? state.MissionName : null);
			if (text != null && text2 != null)
			{
				return text + " | " + text2;
			}
			if (text != null)
			{
				return text;
			}
			if (text2 != null)
			{
				return text2;
			}
			return null;
		}
	}
}
