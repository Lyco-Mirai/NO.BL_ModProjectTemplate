using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Chat;
using NuclearOption.Networking;
using NuclearOption.Networking.Authentication;
using Steamworks;
using UnityEngine;

namespace NuclearOption.DedicatedServer.Commands
{
	public static class DefaultServerCommands
	{
		[Serializable]
		public struct GetServerIdResult
		{
			public string serverId;
		}

		[Serializable]
		public struct GetTimeRemainingResult
		{
			public float currentTime;

			public float maxTime;
		}

		[Serializable]
		public struct GetMissionResult
		{
			public MissionOptions currentMission;

			public MissionOptions nextMission;
		}

		[Serializable]
		public struct GetMissionRotationResult
		{
			public string rotationType;

			public MissionOptions[] rotation;

			public bool hasNextOverride;

			public MissionOptions nextOverride;
		}

		[Serializable]
		public struct SetMissionRotationArgs
		{
			public string rotationType;

			public MissionOptions[] rotation;

			public bool clearNextOverride;
		}

		[Serializable]
		public struct GetPlayerListResult
		{
			public GetPlayerListResultItem[] Players;
		}

		[Serializable]
		public struct GetPlayerListResultItem
		{
			public string steamId;

			public string faction;
		}

		public static List<ServerCommand> CreateCommands()
		{
			return new List<ServerCommand>
			{
				new ServerCommand("update-ready", RunUpdateReady),
				new ServerCommand("send-chat-message", RunSendChatMessage),
				new ServerCommand("reload-config", RunReloadConfig),
				new ServerCommand("get-mission-time", RunGetMissionTime),
				new ServerCommand("get-mission", RunGetMission),
				new ServerCommand("set-time-remaining", RunSetTimeRemaining),
				new ServerCommand("set-next-mission", RunSetNextMission),
				new ServerCommand("get-server-id", RunGetServerId),
				new ServerCommand("get-mission-rotation", RunGetMissionRotation),
				new ServerCommand("set-mission-rotation", RunSetMissionRotation),
				new ServerCommand("clear-next-mission", RunClearNextMission),
				new ServerCommand("get-player-list", RunGetPlayerList)
			};
		}

		private static CommandResponse RunGetServerId(ServerRemoteCommands server, string[] arguments)
		{
			ulong steamID = SteamGameServer.GetSteamID().m_SteamID;
			return CommandResponse.Create(StatusCode.Success, new GetServerIdResult
			{
				serverId = steamID.ToString()
			});
		}

		private static CommandResponse RunUpdateReady(ServerRemoteCommands server, string[] arguments)
		{
			DedicatedServerManager.UpdateReady = true;
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunSendChatMessage(ServerRemoteCommands server, string[] arguments)
		{
			if (arguments.Length < 1)
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Expected Arguments [string message]");
			}
			server.RunOnMainThread(delegate
			{
				NetworkSceneSingleton<ChatManager>.i.RpcServerMessage(arguments[0], runTtsIfEnabled: false);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunReloadConfig(ServerRemoteCommands server, string[] arguments)
		{
			string newConfigPath = ((arguments.Length != 0) ? arguments[0] : null);
			DedicatedServerConfig newConfig = null;
			if (!string.IsNullOrEmpty(newConfigPath) && !DedicatedServerConfig.TryLoad(newConfigPath, out newConfig))
			{
				return CommandResponse.Create(StatusCode.BadArguments, "could not find new config at: " + newConfigPath);
			}
			server.RunOnMainThread(delegate
			{
				DedicatedServerManager.Instance.ReloadConfig(newConfig, newConfigPath);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunGetMissionTime(ServerRemoteCommands server, string[] arguments)
		{
			var (flag, body) = server.RunOnMainThreadBlocking(delegate
			{
				DedicatedServerManager instance = DedicatedServerManager.Instance;
				GetTimeRemainingResult item = default(GetTimeRemainingResult);
				if (instance.HasPlayers())
				{
					MissionOptions currentMissionOption = instance.CurrentMissionOption;
					item.currentTime = Time.timeSinceLevelLoad;
					item.maxTime = currentMissionOption.MaxTime;
				}
				return (ok: true, result: item);
			});
			if (flag)
			{
				return CommandResponse.Create(StatusCode.Success, body);
			}
			return CommandResponse.Create(StatusCode.InternalServerError);
		}

		private static CommandResponse RunGetMission(ServerRemoteCommands server, string[] arguments)
		{
			var (flag, body) = server.RunOnMainThreadBlocking(delegate
			{
				DedicatedServerManager instance = DedicatedServerManager.Instance;
				return (ok: true, result: new GetMissionResult
				{
					currentMission = instance.CurrentMissionOption,
					nextMission = instance.NextMissionOption
				});
			});
			if (flag)
			{
				return CommandResponse.Create(StatusCode.Success, body);
			}
			return CommandResponse.Create(StatusCode.InternalServerError);
		}

		private static CommandResponse RunSetTimeRemaining(ServerRemoteCommands server, string[] arguments)
		{
			if (arguments.Length == 0)
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Expected Arguments [float RemainingTime]");
			}
			if (!float.TryParse(arguments[0], out var remainingTime))
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Failed to parse " + arguments[0] + " as float RemainingTime");
			}
			server.RunOnMainThread(delegate
			{
				DedicatedServerManager.Instance.SetTimeRemaining(remainingTime);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunSetNextMission(ServerRemoteCommands server, string[] arguments)
		{
			if (arguments.Length <= 2)
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Expected Arguments [string Group, string Name, float MaxTime]");
			}
			string text = arguments[0];
			string name = arguments[1];
			if (!float.TryParse(arguments[2], out var result))
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Failed to parse " + arguments[2] + " as float MaxTime");
			}
			MissionOptions missionOption = new MissionOptions
			{
				Key = new MissionKeySaveable
				{
					Group = text,
					Name = name
				},
				MaxTime = result
			};
			server.RunOnMainThread(delegate
			{
				DedicatedServerManager.Instance.SetNextMissionAsync(missionOption).Forget();
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunGetMissionRotation(ServerRemoteCommands server, string[] arguments)
		{
			var (flag, body) = server.RunOnMainThreadBlocking(delegate
			{
				DedicatedServerManager instance = DedicatedServerManager.Instance;
				MissionOptions? nextOverrideOption = instance.NextOverrideOption;
				GetMissionRotationResult item = new GetMissionRotationResult
				{
					rotationType = instance.Config.RotationType.ToString(),
					rotation = instance.Config.MissionRotation,
					hasNextOverride = nextOverrideOption.HasValue,
					nextOverride = nextOverrideOption.GetValueOrDefault()
				};
				return (ok: true, result: item);
			});
			if (flag)
			{
				return CommandResponse.Create(StatusCode.Success, body);
			}
			return CommandResponse.Create(StatusCode.InternalServerError);
		}

		private static CommandResponse RunSetMissionRotation(ServerRemoteCommands server, string[] arguments)
		{
			if (arguments.Length < 1)
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Expected JSON string argument");
			}
			SetMissionRotationArgs args;
			try
			{
				args = JsonUtility.FromJson<SetMissionRotationArgs>(arguments[0]);
			}
			catch (Exception ex)
			{
				return CommandResponse.Create(StatusCode.JsonError, "Failed to parse JSON: " + ex.Message);
			}
			if (!Enum.TryParse<RotationType>(args.rotationType, out var rotationType))
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Invalid rotation type: " + args.rotationType);
			}
			if (args.rotation == null || args.rotation.Length == 0)
			{
				return CommandResponse.Create(StatusCode.BadArguments, "Mission rotation list must not be empty");
			}
			server.RunOnMainThread(delegate
			{
				DedicatedServerManager.Instance.ReloadMissionRotation(args.rotation, rotationType, args.clearNextOverride);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunClearNextMission(ServerRemoteCommands server, string[] arguments)
		{
			server.RunOnMainThread(delegate
			{
				DedicatedServerManager.Instance.ClearNextMissionAsync().Forget();
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunGetPlayerList(ServerRemoteCommands server, string[] arguments)
		{
			var (flag, body) = server.RunOnMainThreadBlocking(delegate
			{
				List<INetworkPlayer> list = new List<INetworkPlayer>();
				foreach (INetworkPlayer authenticatedPlayer in NetworkManagerNuclearOption.i.Server.AuthenticatedPlayers)
				{
					if ((!DedicatedServerManager.IsRunning || !authenticatedPlayer.IsHost) && !authenticatedPlayer.TryGetPlayer<DedicatedServerPlayer>(out var _))
					{
						list.Add(authenticatedPlayer);
					}
				}
				GetPlayerListResult item = new GetPlayerListResult
				{
					Players = new GetPlayerListResultItem[list.Count]
				};
				for (int i = 0; i < list.Count; i++)
				{
					INetworkPlayer networkPlayer = list[i];
					NetworkAuthenticatorNuclearOption.AuthData authData = networkPlayer.GetAuthData();
					GetPlayerListResultItem getPlayerListResultItem = new GetPlayerListResultItem
					{
						steamId = authData.SteamID.ToString(),
						faction = "None"
					};
					if (networkPlayer.TryGetPlayer<Player>(out var player2))
					{
						/*
						if (player2.HQ != null)
						{
							getPlayerListResultItem.faction = player2.HQ.faction.factionName;
						}
						else
						{
							getPlayerListResultItem.faction = "None";
						}*/
					}
					item.Players[i] = getPlayerListResultItem;
				}
				return (ok: true, result: item);
			});
			if (flag)
			{
				return CommandResponse.Create(StatusCode.Success, body);
			}
			return CommandResponse.Create(StatusCode.InternalServerError);
		}
	}
}
