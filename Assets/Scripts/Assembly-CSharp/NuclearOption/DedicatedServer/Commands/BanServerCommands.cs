using System.Collections.Generic;
using Mirage;
using NuclearOption.Networking;
using Steamworks;
using UnityEngine;

namespace NuclearOption.DedicatedServer.Commands
{
	public static class BanServerCommands
	{
		public static List<ServerCommand> CreateCommands()
		{
			return new List<ServerCommand>
			{
				new ServerCommand("kick-player", RunKickPlayer),
				new ServerCommand("unkick-player", RunUnKickPlayer),
				new ServerCommand("clear-kicked-player", RunClearKickedPlayers),
				new ServerCommand("clear-kicked-players", RunClearKickedPlayers),
				new ServerCommand("banlist-reload", RunBanListReload),
				new ServerCommand("banlist-add", RunBanListAdd),
				new ServerCommand("banlist-remove", RunBanListRemove),
				new ServerCommand("banlist-clear", RunBanListClear)
			};
		}

		private static bool ParseSteamId(string[] arguments, int index, out CSteamID id, out CommandResponse errorResponse)
		{
			if (arguments.Length <= index)
			{
				Debug.LogWarning($"Wrong number of arguments, expected atleast {index} arguments");
				id = default(CSteamID);
				errorResponse = CommandResponse.Create(StatusCode.BadArguments, $"expected atleast {index} arguments");
				return false;
			}
			if (!ulong.TryParse(arguments[0], out var result))
			{
				Debug.LogWarning("Failed to parse arg[0] as ulong SteamID");
				id = default(CSteamID);
				errorResponse = CommandResponse.Create(StatusCode.BadArguments, "failed to parse " + arguments[0] + " as ulong SteamID");
				return false;
			}
			id = new CSteamID(result);
			errorResponse = default(CommandResponse);
			return true;
		}

		private static bool CheckBanListFileExists(out CommandResponse errorResponse)
		{
			if (DedicatedServerManager.Instance.Config.BanListPaths.Length == 0)
			{
				Debug.LogWarning("Could not modify ban list because server config had no ban list files");
				errorResponse = CommandResponse.Create(StatusCode.ConfigError, "No ban files because `Config.BanListPaths` was empty");
				return false;
			}
			errorResponse = default(CommandResponse);
			return true;
		}

		private static string SafeGet(string[] arguments, int index, string defaultValue)
		{
			if (arguments.Length <= index)
			{
				return defaultValue;
			}
			return arguments[index];
		}

		private static bool OptionalBool(string[] arguments, int index, bool defaultValue, out bool value, out CommandResponse errorResponse)
		{
			if (arguments.Length <= index)
			{
				value = defaultValue;
				errorResponse = default(CommandResponse);
				return true;
			}
			if (bool.TryParse(arguments[index], out value))
			{
				errorResponse = default(CommandResponse);
				return true;
			}
			errorResponse = CommandResponse.Create(StatusCode.BadArguments, "failed to parse " + arguments[1] + " as bool");
			return false;
		}

		private static CommandResponse RunClearKickedPlayers(ServerRemoteCommands server, string[] arguments)
		{
			server.RunOnMainThread(delegate
			{
				NetworkManagerNuclearOption.i.Authenticator.KickList.Clear();
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunKickPlayer(ServerRemoteCommands server, string[] arguments)
		{
			if (!ParseSteamId(arguments, 0, out var steamId, out var errorResponse))
			{
				return errorResponse;
			}
			server.RunOnMainThread(delegate
			{
				if (!KickPlayer(steamId))
				{
					Debug.LogWarning($"RunKickPlayer failed to find user with id={steamId}, but adding them to kicked list so they can not rejoin");
					NetworkManagerNuclearOption.i.Authenticator.KickList.Add(steamId, "");
				}
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static bool KickPlayer(CSteamID steamId)
		{
			foreach (INetworkPlayer authenticatedPlayer in NetworkManagerNuclearOption.i.Server.AuthenticatedPlayers)
			{
				if (authenticatedPlayer.GetAuthData().SteamID == steamId)
				{
					if (authenticatedPlayer.TryGetPlayer<Player>(out var player))
					{
						NetworkManagerNuclearOption.i.KickPlayerAsync(player).Forget();
					}
					else
					{
						NetworkManagerNuclearOption.i.KickPlayer(authenticatedPlayer);
					}
					Debug.Log($"RunKickPlayer kicking {player}");
					return true;
				}
			}
			return false;
		}

		private static CommandResponse RunUnKickPlayer(ServerRemoteCommands server, string[] arguments)
		{
			if (!ParseSteamId(arguments, 0, out var steamId, out var errorResponse))
			{
				return errorResponse;
			}
			server.RunOnMainThread(delegate
			{
				NetworkManagerNuclearOption.i.Authenticator.KickList.Remove(steamId);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunBanListReload(ServerRemoteCommands server, string[] arguments)
		{
			server.RunOnMainThread(delegate
			{
				DedicatedServerManager.Instance.LoadAllowBanList();
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunBanListAdd(ServerRemoteCommands server, string[] arguments)
		{
			if (!ParseSteamId(arguments, 0, out var steamId, out var errorResponse))
			{
				return errorResponse;
			}
			string reason = SafeGet(arguments, 1, null);
			reason = reason?.Replace("\r", "").Replace("\n", " ");
			if (!CheckBanListFileExists(out var errorResponse2))
			{
				return errorResponse2;
			}
			server.RunOnMainThread(delegate
			{
				NetworkManagerNuclearOption.i.Authenticator.BanPlayer(steamId, reason);
				KickPlayer(steamId);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunBanListRemove(ServerRemoteCommands server, string[] arguments)
		{
			if (!ParseSteamId(arguments, 0, out var steamId, out var errorResponse))
			{
				return errorResponse;
			}
			if (!CheckBanListFileExists(out var errorResponse2))
			{
				return errorResponse2;
			}
			server.RunOnMainThread(delegate
			{
				NetworkManagerNuclearOption.i.Authenticator.BanList.Remove(steamId);
				AllowBanList.RemoveId(DedicatedServerManager.Instance.Config.BanListPaths[0], steamId);
			});
			return CommandResponse.Create(StatusCode.Success);
		}

		private static CommandResponse RunBanListClear(ServerRemoteCommands server, string[] arguments)
		{
			server.RunOnMainThread(delegate
			{
				NetworkManagerNuclearOption.i.Authenticator.BanList.Clear();
			});
			return CommandResponse.Create(StatusCode.Success);
		}
	}
}
