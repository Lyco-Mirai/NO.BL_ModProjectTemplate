using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Logging;
using NuclearOption.BuildScripts.DebugTests;
using NuclearOption.DedicatedServer;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NetworkTransforms;
using NuclearOption.Networking;
using NuclearOption.Networking.Lobbies;
using NuclearOption.SavedMission;
using Steamworks;
using UnityEngine;

namespace NuclearOption.BuildScripts
{
	public class CommandLineArgParser : MonoBehaviour
	{
		[SerializeField]
		private bool runInEditor;

		public static SocketType? SocketType;

		private int? port;

		private string address;

		private string missionName;

		private GameState? state;

		private static CommandLineArgParser i;

		private LeakTest leakTest;

		private new CancellationToken destroyCancellationToken;

		public static bool IsAutoStart;

		public static bool UseSteamNetworkingVerboseLogging;

		public static bool ForceSteamServerInit;

		private void Awake()
		{
			if (i == null)
			{
				i = this;
				UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
				Parse();
			}
			else
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		public void Parse()
		{
			destroyCancellationToken = base.destroyCancellationToken;
			List<string> list = new List<string>(Environment.GetCommandLineArgs());
			addEditorArgs(list);
			CommandParser.Parse(list, getArgCommands());
		}

		private static void addEditorArgs(List<string> args)
		{
		}

		private List<CommandParser.ArgCommand> getArgCommands()
		{
			return new List<CommandParser.ArgCommand>
			{
				new CommandParser.ArgCommand("-autoHost", (CommandParser.HandleArgDelegateCoroutine)parseAutoHost),
				new CommandParser.ArgCommand("-autoConnect", (CommandParser.HandleArgDelegateCoroutine)parseAutoConnect),
				new CommandParser.ArgCommand("-joinLobby", (CommandParser.HandleArgDelegateCoroutine)parseJoinLobby),
				new CommandParser.ArgCommand("-socket", (CommandParser.HandleArgDelegate)parseSetSocket),
				new CommandParser.ArgCommand("-port", (CommandParser.HandleArgDelegate)parseSetPort),
				new CommandParser.ArgCommand("-address", (CommandParser.HandleArgDelegate)parseSetAddress),
				new CommandParser.ArgCommand("-mission", (CommandParser.HandleArgDelegate)parseSetMission),
				new CommandParser.ArgCommand("-state", (CommandParser.HandleArgDelegate)parseSetGameSate),
				new CommandParser.ArgCommand("-recordcpu", (CommandParser.HandleArgDelegate)parseRecordCpu),
				new CommandParser.ArgCommand("-closeafter", (CommandParser.HandleArgDelegate)parseCloseAfter),
				new CommandParser.ArgCommand("-limitframerate", (CommandParser.HandleArgDelegate)parseLimitFrameRate),
				new CommandParser.ArgCommand("-BenchmarkScope", (CommandParser.HandleArgDelegate)parseBenchmarkScope),
				new CommandParser.ArgCommand("-SceneLoadTest", (CommandParser.HandleArgDelegateCoroutine)parseSceneLoadTest),
				new CommandParser.ArgCommand("-leakTest", (CommandParser.HandleArgDelegate)parseLeakTest),
				new CommandParser.ArgCommand("-pwTest", (CommandParser.HandleArgDelegate)parseLobbyPasswordTest),
				new CommandParser.ArgCommand("-descTest", (CommandParser.HandleArgDelegate)parseDescriptionSplitTest),
				new CommandParser.ArgCommand("-saveLoadTest", (CommandParser.HandleArgDelegateCoroutine)parseSaveLoadTest),
				new CommandParser.ArgCommand("-createtestmission", (CommandParser.HandleArgDelegateCoroutine)parseCreateTestMission),
				new CommandParser.ArgCommand("-serializationTest", (CommandParser.HandleArgDelegate)parseSerializationTest),
				new CommandParser.ArgCommand("-upgradeMission", (CommandParser.HandleArgDelegateCoroutine)parseUpgradeMission),
				new CommandParser.ArgCommand("-DedicatedServer", (CommandParser.HandleArgDelegateCoroutine)parseDedicatedServer),
				new CommandParser.ArgCommand("-ServerRemoteCommands", (CommandParser.HandleArgDelegate)parseServerRemoteCommands),
				new CommandParser.ArgCommand("-ClientAuthDebugStream", (CommandParser.HandleArgDelegate)parseClientAuthDebugStream),
				new CommandParser.ArgCommand("-ClientAuthDebugStreamToCsv", (CommandParser.HandleArgDelegate)parseClientAuthDebugStreamToCsv),
				new CommandParser.ArgCommand("-ValidateMode", (CommandParser.HandleArgDelegate)parseValidateMode),
				new CommandParser.ArgCommand("-ClientUnderTerrainChecks", (CommandParser.HandleArgDelegate)parseClientUnderTerrainChecks),
				new CommandParser.ArgCommand("-SetLogLevel", (CommandParser.HandleArgDelegate)parseSetLogLevel),
				new CommandParser.ArgCommand("-SteamNetworkingVerboseLogging", delegate
				{
					UseSteamNetworkingVerboseLogging = true;
				}),
				new CommandParser.ArgCommand("-SteamServerInit", delegate
				{
					ForceSteamServerInit = true;
				})
			};
		}

		private void parseSetSocket(CommandParser.CommandArguments arguments)
		{
			SocketType = arguments.GetNextEnum<SocketType>(1);
		}

		private void parseSetPort(CommandParser.CommandArguments arguments)
		{
			port = arguments.GetNextInt(1);
		}

		private void parseSetAddress(CommandParser.CommandArguments arguments)
		{
			address = arguments.GetNext(1);
		}

		private void parseSetMission(CommandParser.CommandArguments arguments)
		{
			missionName = arguments.GetNext(1);
		}

		private void parseSetGameSate(CommandParser.CommandArguments arguments)
		{
			state = arguments.GetNextEnum<GameState>(1);
		}

		private void parseRecordCpu(CommandParser.CommandArguments arguments)
		{
			GameObject obj = new GameObject("FrameTimeLogger");
			FrameTimeLogger frameTimeLogger = obj.AddComponent<FrameTimeLogger>();
			UnityEngine.Object.DontDestroyOnLoad(obj);
			if (arguments.TryGetNext(1, out var value))
			{
				frameTimeLogger.OutFile = value;
			}
		}

		private void parseCloseAfter(CommandParser.CommandArguments arguments)
		{
			int seconds = arguments.GetNextInt(1);
			UniTask.Void(async delegate
			{
				await UniTask.Delay(seconds * 1000, ignoreTimeScale: true);
				if (!destroyCancellationToken.IsCancellationRequested)
				{
					Quit();
				}
			});
		}

		private void parseLimitFrameRate(CommandParser.CommandArguments arguments)
		{
			GameManager.OverrideTargetFrameRate = arguments.GetNextInt(1);
		}

		private void parseBenchmarkScope(CommandParser.CommandArguments arguments)
		{
			BenchmarkScope.ShowInRelease = true;
		}

		private async UniTask parseAutoHost(CommandParser.CommandArguments arguments)
		{
			IsAutoStart = true;
			await MainMenu.WaitForLoaded(destroyCancellationToken);
			if (!destroyCancellationToken.IsCancellationRequested)
			{
				if (string.IsNullOrEmpty(missionName))
				{
					missionName = "Free Flight";
				}
				Mission mission = LoadMission(missionName, fallbackToDefault: true);
				ColorLog<CommandLineArgParser>.Info($"Loaded Mission with key:{mission.LoadKey}");
				MissionManager.SetMission(mission, checkIfSame: false);
				if (state == GameState.Editor)
				{
					await MissionEditor.LoadEditor(mission);
					return;
				}
				HostOptions options = new HostOptions(SocketType ?? NuclearOption.Networking.SocketType.UDP, state ?? GameState.Multiplayer, mission.MapKey)
				{
					MaxConnections = null,
					UdpPort = port
				};
				await NetworkManagerNuclearOption.i.StartHostAsync(options);
			}
		}

		public static Mission LoadMission(string missionName, bool fallbackToDefault)
		{
			Mission mission;
			string error;
			if (ulong.TryParse(missionName, out var result))
			{
				MissionKey key = new MissionKey(missionName, missionName, new PublishedFileId_t(result), MissionGroup.Workshop);
				key = MissionGroup.Workshop.ResolveKey(key);
				if (MissionSaveLoad.TryLoad(key, out mission, out error))
				{
					return mission;
				}
			}
			if (MissionSaveLoad.TryLoad(new MissionKey(missionName, MissionGroup.User), out mission, out error))
			{
				return mission;
			}
			if (MissionSaveLoad.TryLoad(new MissionKey(missionName, MissionGroup.Tutorial), out mission, out error))
			{
				return mission;
			}
			if (MissionSaveLoad.TryLoad(new MissionKey(missionName, MissionGroup.BuiltIn), out mission, out error))
			{
				return mission;
			}
			if (MissionSaveLoad.TryLoad(new MissionKey(missionName, MissionGroup.Default), out mission, out error))
			{
				return mission;
			}
			if (fallbackToDefault)
			{
				Debug.LogError("Failed to load mission with name: " + missionName);
				return MissionSaveLoad.LoadDefault();
			}
			throw new Exception("Failed to load mission with name: " + missionName);
		}

		private async UniTask parseAutoConnect(CommandParser.CommandArguments arguments)
		{
			await MainMenu.WaitForLoaded(destroyCancellationToken);
			if (destroyCancellationToken.IsCancellationRequested)
			{
				return;
			}
			if (arguments.TryGetNext(1, out var value))
			{
				if (string.IsNullOrEmpty(address))
				{
					if (ulong.TryParse(value, out var _))
					{
						ColorLog<CommandLineArgParser>.Info("using Steam with id: " + value);
						SocketType = NuclearOption.Networking.SocketType.Steam;
					}
					else
					{
						ColorLog<CommandLineArgParser>.Info("Setting url address to " + value);
					}
					address = value;
				}
				else
				{
					ColorLog<CommandLineArgParser>.InfoWarn("cant set address from autoConnect because it was already set");
				}
			}
			SocketType socketType = SocketType ?? NuclearOption.Networking.SocketType.UDP;
			ConnectOptions connectOptions = ((socketType == NuclearOption.Networking.SocketType.Steam || socketType == NuclearOption.Networking.SocketType.LagSteam) ? new ConnectOptions(socketType, address) : new ConnectOptions(socketType, address, port));
			if (arguments.TryGetNext(2, out var value2))
			{
				ColorLog<CommandLineArgParser>.Info("Using password for autoconnect");
				connectOptions.Password = value2;
			}
			NetworkManagerNuclearOption.i.Client.Disconnected.AddListener(Disconnected);
			NetworkManagerNuclearOption.i.StartClient(connectOptions);
			static void Disconnected(ClientStoppedReason reason)
			{
				Debug.LogError("Disconnected from AutoConnect, closing automatically");
				Quit();
			}
		}

		private async UniTask parseJoinLobby(CommandParser.CommandArguments arguments)
		{
			if (!arguments.TryGetNextULong(1, out var value))
			{
				Debug.LogError("Invalid or missing lobby ID for -joinLobby");
				return;
			}
			CSteamID lobbyId = new CSteamID(value);
			await MainMenu.WaitForLoaded(destroyCancellationToken);
			if (!destroyCancellationToken.IsCancellationRequested)
			{
				if (SteamLobby.instance == null)
				{
					Debug.LogError("SteamLobby instance not found");
				}
				else
				{
					SteamLobby.instance.CliJoinBySteamId(lobbyId);
				}
			}
		}

		private async UniTask parseSceneLoadTest(CommandParser.CommandArguments arguments)
		{
			await new SceneLoadTest().Run();
		}

		private void parseLeakTest(CommandParser.CommandArguments arguments)
		{
			LeakTest.StartNew(ref leakTest);
		}

		private void parseLobbyPasswordTest(CommandParser.CommandArguments arguments)
		{
			LobbyPasswordTest.Run();
		}

		private void parseDescriptionSplitTest(CommandParser.CommandArguments arguments)
		{
			DescriptionTests.RunAllTests();
		}

		private async UniTask parseSaveLoadTest(CommandParser.CommandArguments arguments)
		{
			await SaveLoadTests.RunAsync(arguments.TryGetNextEnum<SaveLoadTests.SaveLoadMode>(1, out var value) ? value : SaveLoadTests.SaveLoadMode.ReloadEditor);
			Quit();
		}

		private void parseSerializationTest(CommandParser.CommandArguments arguments)
		{
			ObjectiveSerializationTests.Run();
			Quit();
		}

		private async UniTask parseCreateTestMission(CommandParser.CommandArguments arguments)
		{
			string text = "testmission";
			if (arguments.TryGetNext(1, out var value))
			{
				text = value;
			}
			await CreateTestMission.RunAsync(destroyCancellationToken, text);
			Quit();
		}

		private async UniTask parseUpgradeMission(CommandParser.CommandArguments arguments)
		{
			await MainMenu.WaitForLoaded(destroyCancellationToken);
			try
			{
				UpgradeMissionInner(ref arguments);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to upgrade mission: {arg}");
			}
			await UniTask.Yield();
			Debug.LogWarning("Finished UpgradeMission, exiting play mode");
			Quit();
		}

		private void UpgradeMissionInner(ref CommandParser.CommandArguments arguments)
		{
			if (!arguments.TryGetNext(1, out var value))
			{
				throw new ArgumentException("No input path provided for -upgradeMission");
			}
			arguments.TryGetNext(2, out var value2);
			if (value2 != null)
			{
				ColorLog<CommandLineArgParser>.Info("Upgrading mission from " + value + " to " + value2);
			}
			else
			{
				ColorLog<CommandLineArgParser>.Info("Testing mission load/upgrade from " + value + " in memory (no out path specified)");
			}
			Mission mission;
			if (File.Exists(value))
			{
				string json = File.ReadAllText(value);
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(value);
				if (!MissionSaveLoad.TryReadJson(new MissionKey(fileNameWithoutExtension, MissionGroup.Temp), json, out mission, out var error))
				{
					throw new Exception("Failed to load/upgrade mission file: " + error);
				}
			}
			else
			{
				mission = LoadMission(value, fallbackToDefault: false);
			}
			string contents = NewtonsoftHelper.ToJson(mission, prettyPrint: true);
			if (!string.IsNullOrEmpty(value2))
			{
				File.WriteAllText(value2, contents);
				ColorLog<CommandLineArgParser>.Info("Successfully processed mission from " + value + " and saved to " + value2 + ".");
			}
			else
			{
				ColorLog<CommandLineArgParser>.Info("Successfully processed mission from " + value + " in memory.");
			}
		}

		private async UniTask parseDedicatedServer(CommandParser.CommandArguments arguments)
		{
			if (arguments.TryGetNext(1, out var value))
			{
				ColorLog<CommandLineArgParser>.Info("Found config path argument: " + value);
				if (DedicatedServerConfig.TryLoad(value, out var config))
				{
					ColorLog<CommandLineArgParser>.Info("Loaded config " + value);
					ColorLog<CommandLineArgParser>.Info("Setting DedicatedServerConfig.AutoRun and config");
					DedicatedServerManager.AutoRun = true;
					DedicatedServerManager.SetAutoRunConfig(config, value);
				}
				else
				{
					Debug.LogError("Failed to load DedicatedServerConfig at " + value);
					await UniTask.Yield();
					Quit();
				}
			}
			else
			{
				ColorLog<CommandLineArgParser>.Info("Setting DedicatedServerConfig.AutoRun");
				DedicatedServerManager.AutoRun = true;
			}
		}

		private void parseServerRemoteCommands(CommandParser.CommandArguments arguments)
		{
			int value;
			ushort num = (ushort)(arguments.TryGetNextInt(1, out value) ? ((uint)value) : 7779u);
			ColorLog<CommandLineArgParser>.Info("Starting remote command server on port 7779");
			ServerRemoteCommands server = ServerRemoteCommands.GetOrCreate();
			server.AddCommands(DefaultServerCommands.CreateCommands());
			server.AddCommands(BanServerCommands.CreateCommands());
			server.Start(num);
			UniTask.Void(async delegate
			{
				CancellationToken cancel = destroyCancellationToken;
				while (!cancel.IsCancellationRequested)
				{
					server.PollAll(10);
					await UniTask.Yield();
				}
				server.Dispose();
			});
		}

		private void parseClientAuthDebugStream(CommandParser.CommandArguments arguments)
		{
			ClientAuthStream.OpenPath = arguments.GetNext(1);
		}

		private void parseClientAuthDebugStreamToCsv(CommandParser.CommandArguments arguments)
		{
			string next = arguments.GetNext(1);
			if (Directory.Exists(next))
			{
				string[] files = Directory.GetFiles(next);
				foreach (string obj in files)
				{
					string output = obj + ".csv";
					ClientAuthStream.ToCSV(obj, output);
				}
			}
			else
			{
				string text = next;
				if (!arguments.TryGetNext(2, out var value))
				{
					value = text + ".csv";
				}
				ClientAuthStream.ToCSV(text, value);
			}
			Quit();
		}

		private void parseValidateMode(CommandParser.CommandArguments arguments)
		{
			ClientAuthChecks_Simple.SetRunChecks(arguments.TryGetNextBool(1, out var value) ? new bool?(value) : ((bool?)null));
		}

		private void parseClientUnderTerrainChecks(CommandParser.CommandArguments arguments)
		{
			ClientAuthChecks_Simple.SetClientChecksEnabled(!arguments.TryGetNextBool(1, out var value) || value);
		}

		private void parseSetLogLevel(CommandParser.CommandArguments arguments)
		{
			if (arguments.TryGetNext(1, out var value) && arguments.TryGetNextInt(2, out var value2))
			{
				LogFactory.GetLogger(value).filterLogType = (LogType)value2;
				Debug.Log($"Set log level for '{value}' to {(LogType)value2}");
			}
			else
			{
				Debug.LogError("Usage: -SetLogLevel <loggerName> <logLevelInt>\nLogLevels: 0=Error, 1=Assert, 2=Warning, 3=Log, 4=Exception");
			}
		}

		public static void Quit()
		{
			Application.Quit();
		}
	}
}
