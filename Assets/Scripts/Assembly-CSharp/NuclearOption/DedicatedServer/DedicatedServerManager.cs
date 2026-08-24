using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.BuildScripts;
using NuclearOption.NetworkTransforms;
using NuclearOption.Networking;
using NuclearOption.Networking.Lobbies;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.Workshop;
using Steamworks;
using UnityEngine;

namespace NuclearOption.DedicatedServer
{
	public class DedicatedServerManager : MonoBehaviour
	{
		[SerializeField]
		private NetworkServer server;

		[SerializeField]
		private NetworkManagerNuclearOption networkManager;

		[SerializeField]
		private MapLoader mapLoader;

		[SerializeField]
		private int millisecondsInterval = 5000;

		private const int LOG_PLAYER_COUNT_INTERVAL = 12;

		private int logPlayerCountCounter;

		private MissionRotation missionRotation;

		private MissionOptions currentMissionOption;

		private Mission currentMission;

		private double noPlayerStopTime;

		public DedicatedServerKeyValues keyValues = new DedicatedServerKeyValues();

		private Callback<SteamServersConnected_t> steamServersConnected;

		public static bool AutoRun;

		public static (DedicatedServerConfig config, string path) AutoRunConfig;

		public static bool UpdateReady;

		public static DedicatedServerManager Instance => NetworkManagerNuclearOption.i.DedicatedServerManager;

		public static bool IsRunning { get; private set; }

		public DedicatedServerConfig Config { get; private set; }

		public string ConfigPath { get; private set; }

		public double NoPlayerStopTime => noPlayerStopTime;

		public MissionOptions CurrentMissionOption => currentMissionOption;

		public MissionOptions NextMissionOption => missionRotation.PeakNext();

		public MissionOptions? NextOverrideOption => missionRotation.NextOverrideOption;

		public event Action<DedicatedServerConfig> OnConfigChanged;

		public static void SetAutoRunConfig(DedicatedServerConfig config, string path)
		{
			AutoRunConfig = (config: config, path: path);
		}

		public static (DedicatedServerConfig config, string path) GetConfig()
		{
			if (AutoRunConfig.config == null)
			{
				AutoRunConfig = DedicatedServerConfig.AutoFindOrCreate();
			}
			return AutoRunConfig;
		}

		private void Awake()
		{
			if (GameManager.IsHeadless && !CommandLineArgParser.IsAutoStart)
			{
				ColorLog<DedicatedServerManager>.Info("Setting AutoRun");
				AutoRun = true;
			}
		}

		private void Start()
		{
			if (AutoRun)
			{
				StartAsync().Forget();
			}
		}

		private async UniTaskVoid StartAsync()
		{
			await MainMenu.WaitForLoaded(base.destroyCancellationToken);
			if (!SteamManager.ServerInitialized)
			{
				ColorLog<DedicatedServerManager>.InfoWarn("Could not start DedicatedServerManager because Steam failed to init");
				Application.Quit();
			}
			else
			{
				(DedicatedServerConfig, string) config = GetConfig();
				Run(config.Item1, config.Item2);
			}
		}

		public void Run(DedicatedServerConfig config, string configPath)
		{
			Config = config;
			ConfigPath = configPath;
			Config.Log(isReload: false);
			missionRotation = new MissionRotation(config.MissionRotation, config.RotationType);
			server.ErrorRateLimitEnabled = !Config.DisableErrorKick;
			this.OnConfigChanged?.Invoke(Config);
			Runner().Forget();
		}

		private async UniTask Runner()
		{
			if (IsRunning)
			{
				throw new InvalidOperationException("Already running");
			}
			IsRunning = true;
			try
			{
				await RunnerInner();
			}
			finally
			{
				IsRunning = false;
			}
		}

		private async UniTask RunnerInner()
		{
			CancellationToken cancel = base.destroyCancellationToken;
			ColorLog<DedicatedServerManager>.Info("Starting Server");
			await StartServer();
			if (!string.IsNullOrEmpty(Config.MissionDirectory))
			{
				MissionGroup.UserGroup.SetDirectory(Config.MissionDirectory);
			}
			await DownloadAllRotationMissions();
			ColorLog<DedicatedServerManager>.Info("Starting run loop");
			while (!cancel.IsCancellationRequested)
			{
				try
				{
					currentMission = null;
					if (!(await PreloadAndUpdateLobby(missionRotation.PeakNext(), setStartTime: false)).Item1)
					{
						continue;
					}
					networkManager.Authenticator.ClearRejoinSaveData(serverStopping: false);
					networkManager.Authenticator.ClearMissionKickList();
					if (!HasPlayers())
					{
						if (UpdateReady)
						{
							ColorLog<DedicatedServerManager>.Info("Closing for update");
							Application.Quit();
							break;
						}
						if (GameManager.gameState != GameState.ServerWaiting)
						{
							ColorLog<DedicatedServerManager>.Info("No players, unloading scene");
							await LoadEmptyScene();
						}
						ColorLog<DedicatedServerManager>.Info("Waiting for Players before loading next map");
						while (!HasPlayers())
						{
							await UniTask.Delay(millisecondsInterval, ignoreTimeScale: true);
							if (cancel.IsCancellationRequested)
							{
								return;
							}
							if (UpdateReady)
							{
								ColorLog<DedicatedServerManager>.Info("Closing for update");
								Application.Quit();
								return;
							}
						}
					}
					currentMissionOption = missionRotation.GetNext();
					var (flag, mission) = await PreloadAndUpdateLobby(currentMissionOption, setStartTime: true);
					if (!flag)
					{
						continue;
					}
					currentMission = mission;
					if (!(await LoadNext(currentMission)))
					{
						missionRotation.RemoveBrokenMap(currentMissionOption.Key);
						continue;
					}
					if (networkManager.TryGetComponent<SendTransformBatcher>(out var component))
					{
						component.clientAuthDebugStream?.LogBlank();
					}
					noPlayerStopTime = Time.unscaledTimeAsDouble + (double)Config.NoPlayerStopTime;
					while (!GameShouldStop())
					{
						await UniTask.Delay(millisecondsInterval, ignoreTimeScale: true);
						if (cancel.IsCancellationRequested)
						{
							return;
						}
					}
					if (GameManager.gameResolution != GameResolution.Ongoing)
					{
						ColorLog<DedicatedServerManager>.Info($"Mission complete. Waiting {Config.PostMissionDelay} seconds before closing...");
						await UniTask.Delay((int)(Config.PostMissionDelay * 1000f), ignoreTimeScale: true);
						if (cancel.IsCancellationRequested)
						{
							break;
						}
					}
					goto IL_05fe;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					goto IL_05fe;
				}
				IL_05fe:
				await UniTask.Yield();
			}
		}

		private async UniTask StartServer()
		{
			if (!SteamLogOn())
			{
				Debug.LogError("Failed to host lobby");
				CommandLineArgParser.Quit();
				return;
			}
			ColorLog<DedicatedServerManager>.Info("Starting Network Server");
			HostOptions options = new HostOptions(CommandLineArgParser.SocketType ?? SocketType.SteamGameServer, GameState.ServerWaiting, MapLoader.Empty)
			{
				MaxConnections = Config.MaxPlayers,
				UdpPort = Config.Port.AsNullable(),
				Password = Config.Password
			};
			await NetworkManagerNuclearOption.i.StartHostAsync(options);
			LoadAllowBanList();
			if (!SteamGameServer.BLoggedOn())
			{
				await WaitForLogon();
			}
			ColorLog<DedicatedServerManager>.Info("Steam Game Server logged on successfully.");
		}

		private async UniTask WaitForLogon()
		{
			ColorLog<DedicatedServerManager>.Info("Waiting for Steam Game Server logon...");
			CancellationToken cancel = base.destroyCancellationToken;
			float unscaledTime = Time.unscaledTime;
			float lastLogTime = unscaledTime;
			while (!SteamGameServer.BLoggedOn())
			{
				await UniTask.Yield();
				if (cancel.IsCancellationRequested)
				{
					break;
				}
				if (Time.unscaledTime - lastLogTime >= 5f)
				{
					lastLogTime = Time.unscaledTime;
				}
			}
		}

		public void LoadAllowBanList()
		{
			LoadAllowBanList(Config.BanListPaths, NetworkManagerNuclearOption.i.Authenticator.BanList);
			LoadAllowBanList(Config.ErrorKickImmuneListPaths, NetworkManagerNuclearOption.i.Authenticator.ErrorKickImmuneList);
		}

		private static void LoadAllowBanList(string[] list, AllowBanList authList)
		{
			if (list != null && list.Length != 0)
			{
				foreach (string path in list)
				{
					authList.Load(path);
				}
			}
		}

		private bool SteamLogOn()
		{
			ColorLog<DedicatedServerManager>.Info("Advertising dedicated server with name " + Config.ServerName);
			SteamGameServer.SetModDir("NuclearOption");
			SteamGameServer.SetProduct(SteamManager.SteamAppId);
			SteamGameServer.SetGameDescription("Nuclear Option Server");
			SteamGameServer.SetServerName(Config.ServerName ?? "Nuclear Option Server");
			SteamGameServer.SetMapName("Loading");
			SteamGameServer.SetDedicatedServer(bDedicated: true);
			keyValues.SetKeyValue("version", Application.version ?? "");
			UpdateIsModdedServer(keyValues);
			if (Config.HasPassword)
			{
				keyValues.SetKeyValue("short_password", LobbyPassword.GetShortPassword(Config.Password));
			}
			else
			{
				keyValues.SetKeyValue("short_password", null);
			}
			keyValues.ApplyValuesToSteam();
			SteamGameServer.SetMaxPlayerCount(Config.MaxPlayers);
			SteamGameServer.SetBotPlayerCount(0);
			ColorLog<DedicatedServerManager>.Info("SteamGameServer.LogOnAnonymous");
			steamServersConnected = Callback<SteamServersConnected_t>.CreateGameServer(OnSteamServersConnected);
			SteamGameServer.LogOnAnonymous();
			ColorLog<DedicatedServerManager>.Info($"Set Advertise Server: {!Config.Hidden}");
			SteamGameServer.SetAdvertiseServerActive(!Config.Hidden);
			return true;
		}

		private void OnSteamServersConnected(SteamServersConnected_t callback)
		{
			ColorLog<DedicatedServerManager>.Info($"Server ID: {SteamGameServer.GetSteamID()}");
			steamServersConnected?.Dispose();
			steamServersConnected = null;
		}

		private void OnDestroy()
		{
			steamServersConnected?.Dispose();
			steamServersConnected = null;
		}

		private async UniTask<(bool success, Mission mission)> PreloadAndUpdateLobby(MissionOptions option, bool setStartTime)
		{
			if (string.Equals(option.Key.Group, "Workshop", StringComparison.OrdinalIgnoreCase) && !(await EnsureWorkshopItemDownloaded(option.Key)))
			{
				missionRotation.RemoveBrokenMap(option.Key);
				return (false, null);
			}
			if (!PreLoadMission(option, out var mission))
			{
				ColorLog<DedicatedServerManager>.Info("Failed to load mission");
				missionRotation.RemoveBrokenMap(option.Key);
				return (false, null);
			}
			UpdateLobby(mission, setStartTime);
			return (true, mission);
		}

		public void UpdateLobby(Mission mission, bool setStartTime)
		{
			try
			{
				ColorLog<DedicatedServerManager>.Info("Updating server data for Mission=" + mission.Name);
				string value = mission.Name ?? "";
				string mapName;
				string value2 = (mapLoader.TryGetMapName(mission.MapKey, out mapName) ? mapName : "");
				keyValues.SetKeyValue("mission_name", value);
				keyValues.SetKeyValue("mission_description", mission.missionSettings.description ?? "");
				UpdateIsModdedServer(keyValues);
				keyValues.SetKeyValue("mission_pvp_type", MissionTag.GetPvpTypeLobbyString(mission));
				keyValues.SetKeyValue("map_name", value2);
				ulong valueOrDefault = (mission.LoadKey?.WorkshopId?.m_PublishedFileId).GetValueOrDefault();
				keyValues.SetKeyValue("mission_workshop_id", valueOrDefault.ToString("X"));
				if (setStartTime)
				{
					keyValues.SetKeyValue("start_time", LobbyInstance.CreateStartTime());
					keyValues.ApplyValuesToSteam();
				}
				else
				{
					keyValues.SetKeyValue("start_time", "no-game");
					keyValues.ApplyValuesToSteam();
				}
				keyValues.ApplyValuesToSteam();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private async UniTask DownloadAllRotationMissions()
		{
			ColorLog<DedicatedServerManager>.Info("Checking and downloading all Workshop rotation missions...");
			List<UniTask<bool>> tasks = new List<UniTask<bool>>();
			MissionOptions[] array = Config.MissionRotation;
			for (int i = 0; i < array.Length; i++)
			{
				MissionOptions missionOptions = array[i];
				if (string.Equals(missionOptions.Key.Group, "Workshop", StringComparison.OrdinalIgnoreCase))
				{
					UniTask<bool> item = EnsureWorkshopItemDownloaded(missionOptions.Key);
					if (item.Status == UniTaskStatus.Pending)
					{
						tasks.Add(item);
					}
				}
			}
			if (tasks.Count <= 0)
			{
				return;
			}
			ColorLog<DedicatedServerManager>.Info($"Waiting for {tasks.Count} items to download");
			bool[] obj = await UniTask.WhenAll(tasks);
			int num = 0;
			bool[] array2 = obj;
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i])
				{
					num++;
				}
			}
			ColorLog<DedicatedServerManager>.Info($"Finished downloading, success: {num}/{tasks.Count}");
		}

		private static async UniTask<bool> EnsureWorkshopItemDownloaded(MissionKeySaveable key)
		{
			if (!ulong.TryParse(key.Name, out var result))
			{
				Debug.LogError("Invalid Workshop ID: " + key.Name);
				return false;
			}
			try
			{
				return await SteamWorkshop.DownloadItemServer(new PublishedFileId_t(result));
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to download workshop item " + key.Name + ": " + ex.Message);
				return false;
			}
		}

		private static bool PreLoadMission(MissionOptions missionOptions, out Mission mission)
		{
			ColorLog<DedicatedServerManager>.Info("Loading next Mission " + missionOptions.Key.Name);
			if (!missionOptions.Key.TryGetKey(out var missionKey))
			{
				mission = null;
				return false;
			}
			if (!MissionSaveLoad.TryLoad(missionKey, out mission, out var error))
			{
				Debug.LogError(error);
				return false;
			}
			return true;
		}

		private async UniTask<bool> LoadNext(Mission mission)
		{
			networkManager.SendLoadWaitingMessage();
			networkManager.SendServerLoadingMessage(mission.Name);
			if (GameManager.gameState != GameState.ServerWaiting)
			{
				await LoadEmptyScene();
			}
			if (!(await LoadMissionMap(mission)))
			{
				return false;
			}
			networkManager.SendLoadMapMessageAll();
			return true;
		}

		private async UniTask<bool> LoadMissionMap(Mission mission)
		{
			MissionManager.SetMission(mission, checkIfSame: false);
			GameManager.SetGameState(GameState.Multiplayer);
			GameManager.SetupGame();
			Progress<float> progress = new Progress<float>(delegate(float value)
			{
				NetworkManagerNuclearOption.i.SendServerLoadingMessage($"{mission.Name}\nLoading Scene: {(int)(value * 100f)}%");
			});
			(bool, MapLoader.LoadResult) tuple = await networkManager.ServerLoadMapScene(mission.MapKey, progress);
			if (!tuple.Item1)
			{
				Debug.LogError($"Load {mission.MapKey} failed with result={tuple.Item2}");
				return false;
			}
			NetworkManagerNuclearOption.i.SendServerLoadingMessage(mission.Name + "\nInitializing Mission");
			networkManager.Host_SceneLoaded();
			return true;
		}

		private async UniTask LoadEmptyScene()
		{
			GameManager.SetGameState(GameState.ServerWaiting);
			MissionManager.SetNullMission();
			await NetworkManagerNuclearOption.i.LoadSystemScene(MapLoader.Empty, warnIfAlreadyLoaded: false);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool HasPlayers()
		{
			return RealPlayerCount() > 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int RealPlayerCount()
		{
			return server.AuthenticatedPlayers.Count - 1;
		}

		private bool GameShouldStop()
		{
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;
			double unscaledTimeAsDouble = Time.unscaledTimeAsDouble;
			if (GameManager.gameResolution != GameResolution.Ongoing)
			{
				ColorLog<DedicatedServerManager>.Info($"Stopping mission, Game Resolution: {GameManager.gameResolution}");
				return true;
			}
			if (timeSinceLevelLoad > CurrentMissionOption.MaxTime)
			{
				ColorLog<DedicatedServerManager>.Info($"Stopping mission, Reached max time of {CurrentMissionOption.MaxTime} seconds");
				return true;
			}
			int num = RealPlayerCount();
			if (num > 0)
			{
				noPlayerStopTime = unscaledTimeAsDouble + (double)Config.NoPlayerStopTime;
			}
			else
			{
				ColorLog<DedicatedServerManager>.Info("No players");
				if (unscaledTimeAsDouble > noPlayerStopTime)
				{
					ColorLog<DedicatedServerManager>.Info($"Stopping mission, No players for {Config.NoPlayerStopTime} seconds");
					return true;
				}
			}
			logPlayerCountCounter--;
			if (logPlayerCountCounter <= 0)
			{
				ColorLog<DedicatedServerManager>.Info($"Running Mission Players:{num}, GameTime:{timeSinceLevelLoad:0} RealTime:{unscaledTimeAsDouble:0}");
				logPlayerCountCounter = 12;
			}
			return false;
		}

		public void ReloadConfig(DedicatedServerConfig optionalNewConfig, string newConfigPath)
		{
			DedicatedServerConfig config;
			if (optionalNewConfig != null)
			{
				Config = optionalNewConfig;
				ConfigPath = newConfigPath;
			}
			else if (DedicatedServerConfig.TryLoad(ConfigPath, out config))
			{
				Config = config;
			}
			else
			{
				Debug.LogError("Failed to reload config");
			}
			Config.Log(isReload: true);
			server.ErrorRateLimitEnabled = !Config.DisableErrorKick;
			SteamGameServer.SetServerName(Config.ServerName ?? "Nuclear Option Server");
			UpdateIsModdedServer(keyValues);
			if (Config.HasPassword)
			{
				keyValues.SetKeyValue("short_password", LobbyPassword.GetShortPassword(Config.Password));
			}
			else
			{
				keyValues.SetKeyValue("short_password", null);
			}
			keyValues.ApplyValuesToSteam();
			SteamGameServer.SetMaxPlayerCount(Config.MaxPlayers);
			LoadAllowBanList();
			NetworkManagerNuclearOption.i.Server.PeerConfig.MaxConnections = Config.MaxPlayers;
			if (string.IsNullOrEmpty(Config.Password))
			{
				NetworkManagerNuclearOption.i.Authenticator.ClearServerPassword();
			}
			else
			{
				NetworkManagerNuclearOption.i.Authenticator.SetServerPassword(Config.Password);
			}
			ReloadMissionRotation(Config.MissionRotation, Config.RotationType, clearNextOverride: true);
			ColorLog<DedicatedServerManager>.Info($"Set Advertise Server: {!Config.Hidden}");
			SteamGameServer.SetAdvertiseServerActive(!Config.Hidden);
			this.OnConfigChanged?.Invoke(Config);
		}

		private void UpdateIsModdedServer(DedicatedServerKeyValues keyValues)
		{
			bool flag = NetworkManagerNuclearOption.ModdedServer ?? Config.ModdedServer;
			bool valueOrDefault = currentMission?.missionSettings?.allowEventContent == true;
			keyValues.SetKeyValue("modded_server", LobbyInstance.BoolToTag(flag || valueOrDefault));
		}

		public void SetTimeRemaining(float timeRemaining)
		{
			float maxTime = Time.timeSinceLevelLoad + timeRemaining;
			currentMissionOption.MaxTime = maxTime;
		}

		public async UniTask<bool> SetNextMissionAsync(MissionOptions option)
		{
			if (string.Equals(option.Key.Group, "Workshop", StringComparison.OrdinalIgnoreCase) && !(await EnsureWorkshopItemDownloaded(option.Key)))
			{
				ColorLog<DedicatedServerManager>.Info("Failed to download workshop mission, skipping OverrideNext");
				return false;
			}
			if (!PreLoadMission(option, out var _))
			{
				ColorLog<DedicatedServerManager>.Info("Failed to load mission, skipping OverrideNext");
				return false;
			}
			missionRotation.OverrideNext(option);
			if (!HasPlayers())
			{
				await PreloadAndUpdateLobby(missionRotation.PeakNext(), setStartTime: false);
			}
			return true;
		}

		public async UniTask ClearNextMissionAsync()
		{
			missionRotation?.ClearOverride();
			if (!HasPlayers())
			{
				await PreloadAndUpdateLobby(missionRotation.PeakNext(), setStartTime: false);
			}
		}

		public void ReloadMissionRotation(MissionOptions[] newRotation, RotationType rotationType, bool clearNextOverride)
		{
			MissionOptions? nextOverrideOption = NextOverrideOption;
			Config.MissionRotation = newRotation;
			Config.RotationType = rotationType;
			missionRotation = new MissionRotation(Config.MissionRotation, Config.RotationType);
			if (!clearNextOverride && nextOverrideOption.HasValue)
			{
				missionRotation.OverrideNext(nextOverrideOption.Value);
			}
			if (!string.IsNullOrEmpty(Config.MissionDirectory))
			{
				MissionGroup.UserGroup.SetDirectory(Config.MissionDirectory);
			}
			DownloadAllRotationMissions().Forget();
			if (!HasPlayers())
			{
				PreloadAndUpdateLobby(missionRotation.PeakNext(), setStartTime: false).Forget();
			}
		}
	}
}
