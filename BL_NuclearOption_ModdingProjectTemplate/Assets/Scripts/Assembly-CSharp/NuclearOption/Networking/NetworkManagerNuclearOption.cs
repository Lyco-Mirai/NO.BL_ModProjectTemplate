using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JamesFrowen.Mirage.DebugScripts;
using Mirage;
using Mirage.Logging;
using Mirage.SocketLayer;
using Mirage.Sockets.Udp;
using Mirage.SteamworksSocket;
using NuclearOption.Debugging;
using NuclearOption.DedicatedServer;
using NuclearOption.Networking.Authentication;
using NuclearOption.Networking.Lobbies;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.Social;
using Steamworks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace NuclearOption.Networking
{
	public class NetworkManagerNuclearOption : NetworkManager
	{
		private static readonly ILogger logger = LogFactory.GetLogger<NetworkManagerNuclearOption>();

		private const int DEFAULT_MAX_CONNECTIONS = 16;

		private static readonly ResourcesAsyncLoader<NetworkManagerNuclearOption> loader = ResourcesAsyncLoader.Create("networkManager", (Func<GameObject, NetworkManagerNuclearOption>)null);

		[Header("References")]
		[SerializeField]
		private SteamManager steamManager;

		[SerializeField]
		private DedicatedServerManager dedicatedServerManager;

		[Header("Transports")]
		[SerializeField]
		private SteamworksSocketFactory steamTransport;

		[SerializeField]
		private UdpSocketFactory udpTransport;

		[SerializeField]
		private LagSocketFactory lagTransport;

		[SerializeField]
		private NetworkAuthenticatorNuclearOption authenticator;

		[Header("Prefabs and Scenes")]
		[SerializeField]
		private Player gamePlayerPrefab;

		[SerializeField]
		private MapLoader mapLoader;

		[SerializeField]
		private WaitingForDedicatedServerMenu waitingPrefab;

		private WaitingForDedicatedServerMenu waitingPanel;

		private ServerLoadingProgressMessage latestWaitingMessage;

		private bool loadingScene;

		private bool stopping;

		private int nextPlayerIndex;

		private Callback<PersonaStateChange_t> personaStateChangeCallback;

		public static NetworkManagerNuclearOption i => loader.Get();

		public static bool? ModdedServer { get; private set; }

		public NetworkMission NetworkMission { get; private set; }

		public MapKey? MapKey { get; private set; }

		public static bool IsLoadingScene
		{
			get
			{
				if (loader.IsLoaded)
				{
					return loader.Get().loadingScene;
				}
				return false;
			}
		}

		public List<Player> GamePlayers { get; } = new List<Player>();

		public DedicatedServerManager DedicatedServerManager => dedicatedServerManager;

		public NetworkAuthenticatorNuclearOption Authenticator => authenticator;

		public SteamManager SteamManager => steamManager;

		public static async UniTask Preload(CancellationToken cancel)
		{
			await loader.Load(cancel);
		}

		public void SetModdedServer(bool value)
		{
			ModdedServer = value;
		}

		public void Awake()
		{
			loader.AssetNotLoaded();
			mapLoader.ClearMap();
			Server.ErrorRateLimitEnabled = true;
			Client.DisconnectOnException = Application.isEditor;
			Server.RethrowException = Application.isEditor;
			Client.RethrowException = Application.isEditor;
			Server.Started.AddListener(OnServerStarted);
			Server.Authenticated.AddListener(OnServerAuthenticated);
			Server.Disconnected.AddListener(OnServerDisconnect);
			Server.Stopped.AddListener(ServerStopped);
			Client.Started.AddListener(ClientStarted);
			Client.Authenticated.AddListener(OnClientAuthenticated);
			Client.Disconnected.AddListener(ClientDisconnected);
			Server.Authenticated.AddListener(LogServerConnected);
			Client.Authenticated.AddListener(LogClientConnected);
			Server.Disconnected.AddListener(LogServerDisconnected);
			Client.Disconnected.AddListener(LogClientDisconnected);
			NetworkMission = new NetworkMission(Server, Client);
			GameManager.BlockList.Clear();
			GameManager.BlockList.Load(GameManager.BlockFilePath, allowMissing: true);
		}

		private void LogServerConnected(INetworkPlayer player)
		{
			if (logger.LogEnabled())
			{
				logger.Log($"Player connected: {player}");
			}
		}

		private void LogClientConnected(INetworkPlayer player)
		{
			if (logger.LogEnabled())
			{
				logger.Log($"Client connected: {player}");
			}
		}

		private void LogServerDisconnected(INetworkPlayer player)
		{
			if (logger.LogEnabled())
			{
				logger.Log($"Player disconnected: {player}");
			}
		}

		private void LogClientDisconnected(ClientStoppedReason reason)
		{
			if (logger.LogEnabled())
			{
				logger.Log($"Client disconnected {reason}");
			}
		}

		public void ServerMissionStart(Mission mission)
		{
			NetworkMission.SetRunning();
			foreach (INetworkPlayer authenticatedPlayer in Server.AuthenticatedPlayers)
			{
				if (authenticatedPlayer.SceneIsReady && authenticatedPlayer.TryGetPlayer<Player>(out var player))
				{
					ServerMissionStartPlayer(mission, player);
				}
			}
		}

		private void ServerMissionStartPlayer(Mission mission, Player player)
		{
			SavedPlayerData saveData = player.GetAuthData().SaveData;
			if (!saveData.Rejoined)
			{
				ColorLog<NetworkManagerNuclearOption>.Info($"setting playerStartingRank for {player} to {mission.missionSettings.playerStartingRank}");
				player.SetRank(mission.missionSettings.playerStartingRank, setScoreOffset: true);
			}
			else
			{
				ColorLog<NetworkManagerNuclearOption>.Info($"skipping playerStartingRank for {player} because they are rejoining. rejoin rank:{saveData.Rank}");
			}
			if (player.Aircraft == null)
			{
				NetworkSceneSingleton<Spawner>.i.TrySpawnPlayerControlled(player, out var _);
			}
		}

		private void OnServerStarted()
		{
			nextPlayerIndex = 0;
			RichPresenceManager.SetIsHost(isHost: true);
			if (MissionManager.CurrentMission != null)
			{
				SetNetworkMission(MissionManager.CurrentMission, NetworkMission.State.Loaded);
			}
			else
			{
				NetworkMission.Clear();
			}
		}

		public void SetNetworkMission(Mission mission, NetworkMission.State state)
		{
			if (Server.Listening)
			{
				NetworkMission.Set(mission, state);
			}
		}

		private void OnServerAuthenticated(INetworkPlayer player)
		{
			AssignPlayerIndex(player);
			if (MapKey.HasValue)
			{
				SendLoadMapMessageOne(player);
			}
			UpdateSteamPlayerCount();
		}

		public void AssignPlayerIndex(INetworkPlayer player)
		{
			SavedPlayerData saveData = player.GetAuthData().SaveData;
			if (saveData.PlayerIndex == 0)
			{
				nextPlayerIndex++;
				saveData.PlayerIndex = nextPlayerIndex;
				if (logger.LogEnabled())
				{
					logger.Log($"{player} Assigned PlayerIndex {nextPlayerIndex}");
				}
			}
			else if (logger.LogEnabled())
			{
				logger.Log($"{player} reconnecting with index {saveData.PlayerIndex}");
			}
			if (player.TryGetPlayer<Player>(out var player2))
			{
				player2.SetPlayerIndex(saveData.PlayerIndex);
			}
		}

		public void ResetPlayerIndicesOnMissionChange()
		{
			nextPlayerIndex = 0;
			foreach (INetworkPlayer authenticatedPlayer in Server.AuthenticatedPlayers)
			{
				if (!authenticatedPlayer.IsConnected)
				{
					continue;
				}
				NetworkAuthenticatorNuclearOption.AuthData authData = authenticatedPlayer.GetAuthData();
				if (authData != null && authData.SaveData != null)
				{
					nextPlayerIndex++;
					authData.SaveData.PlayerIndex = nextPlayerIndex;
					if (authenticatedPlayer.TryGetPlayer<Player>(out var player))
					{
						player.SetPlayerIndex(nextPlayerIndex);
					}
				}
			}
		}

		private void UpdateSteamPlayerCount()
		{
			int num = Server.AuthenticatedPlayers.Count;
			if (DedicatedServerManager.IsRunning)
			{
				num--;
			}
			SteamLobby.instance.SetServerPlayerCount(num);
		}

		public void SendLoadMapMessageOne(INetworkPlayer player)
		{
			if (!player.IsHost)
			{
				player.SceneIsReady = false;
				player.Send(new LoadMapMessage
				{
					Key = MapKey.Value
				});
			}
		}

		public void SendLoadMapMessageAll()
		{
			MarkAllPlayersNotReady();
			Server.SendToAll(new LoadMapMessage
			{
				Key = MapKey.Value
			}, authenticatedOnly: true, excludeLocalPlayer: true);
		}

		public void SendLoadWaitingMessage()
		{
			MarkAllPlayersNotReady();
			Server.SendToAll(default(LoadWaitingSceneMessage), authenticatedOnly: true, excludeLocalPlayer: true);
		}

		private void MarkAllPlayersNotReady()
		{
			foreach (INetworkPlayer authenticatedPlayer in Server.AuthenticatedPlayers)
			{
				if (!authenticatedPlayer.IsHost)
				{
					authenticatedPlayer.SceneIsReady = false;
				}
			}
		}

		public void SendServerLoadingMessage(string message)
		{
			ServerLoadingProgressMessage msg = new ServerLoadingProgressMessage
			{
				LoadingMessage = message
			};
			Server.SendToAll(msg, authenticatedOnly: true, excludeLocalPlayer: true);
		}

		private void HandleSceneReadyMessage(INetworkPlayer player, SceneReadyMessage msg)
		{
			player.SceneIsReady = true;
			if (loadingScene)
			{
				if (logger.LogEnabled())
				{
					logger.Log($"{player} finished loading scene, but server is still loading");
				}
				return;
			}
			if (player.HasCharacter)
			{
				player.Disconnect();
				Debug.LogError("Player already had character");
				return;
			}
			long timestamp = BenchmarkScope.GetTimestamp();
			SpawnCharacter(player);
			double num = BenchmarkScope.MillisecondsSince(timestamp);
			if (num > 50.0)
			{
				ColorLog<NetworkManagerNuclearOption>.InfoWarn($"SpawnCharacter for {player} took {num:F2}ms");
			}
			else
			{
				ColorLog<NetworkManagerNuclearOption>.Info($"SpawnCharacter for {player} took {num:F2}ms");
			}
		}

		private void SpawnCharacter(INetworkPlayer networkPlayer)
		{
			switch (networkPlayer.GetAuthData().JoinAs)
			{
			default:
				throw new ArgumentException("Can't spawn player of type Unknown");
			case PlayerType.Normal:
			{
				if (logger.LogEnabled())
				{
					logger.Log($"Spawning Normal Player for {networkPlayer}");
				}
				Player player = UnityEngine.Object.Instantiate(gamePlayerPrefab);
				player.NetworkIsHostPlayer = networkPlayer.IsHost;
				ServerObjectManager.AddCharacter(networkPlayer, player.Identity);
				if (MissionManager.IsRunning)
				{
					ServerMissionStartPlayer(MissionManager.CurrentMission, player);
				}
				break;
			}
			case PlayerType.Spectator:
			{
				if (logger.LogEnabled())
				{
					logger.Log($"Spawning Spectator Player for {networkPlayer}");
				}
				SpectatorPlayer spectatorPlayer = PlayerHelper.CreatePrefab<SpectatorPlayer>();
				ServerObjectManager.AddCharacter(networkPlayer, spectatorPlayer.Identity);
				break;
			}
			case PlayerType.DedicatedServer:
			{
				if (logger.LogEnabled())
				{
					logger.Log($"Spawning Dedicated Server Player for {networkPlayer}");
				}
				DedicatedServerPlayer dedicatedServerPlayer = PlayerHelper.CreatePrefab<DedicatedServerPlayer>();
				ServerObjectManager.AddCharacter(networkPlayer, dedicatedServerPlayer.Identity);
				break;
			}
			}
		}

		private void OnServerDisconnect(INetworkPlayer networkPlayer)
		{
			if (networkPlayer.TryGetPlayer<Player>(out var player))
			{
				if (UnitRegistry.TryGetUnit(player.UnitID, out var unit))
				{
					unit.Networkdisabled = true;
				}/*
				if (player.HQ != null)
				{
					player.HQ.RemovePlayer(player);
				}*/
				networkPlayer.RemoveAllOwnedObject(sendAuthorityChangeEvent: false);
				ServerObjectManager.Destroy(player.Identity);
			}
			UpdateSteamPlayerCount();
		}

		private void ServerStopped()
		{
			RichPresenceManager.SetIsHost(isHost: false);
			if (!stopping)
			{
				HandleStopAsync(null).Forget();
			}
		}

		private void ClientStarted()
		{
			((IMessageReceiver)Client.MessageHandler).RegisterHandler((MessageDelegateWithPlayer<LoadMapMessage>)HandleSceneMessage);
			((IMessageReceiver)Client.MessageHandler).RegisterHandler((MessageDelegateWithPlayer<LoadWaitingSceneMessage>)HandleLoadWaitingMessage);
			((IMessageReceiver)Client.MessageHandler).RegisterHandler((MessageDelegate<HostEndedMessage>)HandleHostEndedMessage, false);
			((IMessageReceiver)Client.MessageHandler).RegisterHandler((MessageDelegate<ServerLoadingProgressMessage>)HandleWaitingSceneProgressMessage, false);
			if (SteamManager.ClientInitialized)
			{
				personaStateChangeCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
			}
			RegisterPrefabs();
		}

		private void OnClientAuthenticated(INetworkPlayer player)
		{
			if (!player.IsHost)
			{
				JoinLobbyOverlay.Open(JoinProgress.Joining("Connecting complete, waiting for map"));
			}
			NetworkTime time = Client.World.Time;
			time.PingInterval = 0.2f;
			time.PingWindowSize = 12;
			if (!player.IsHost)
			{
				CursorManager.SetFlag(CursorFlags.Loading, value: true);
				GameManager.SetGameState(GameState.Multiplayer);
			}
		}

		private void HandleSceneMessage(INetworkPlayer player, LoadMapMessage msg)
		{
			JoinLobbyOverlay.Close();
			ClientLoadScene(msg).Forget();
		}

		private async UniTask ClientLoadScene(LoadMapMessage msg)
		{
			SetLoading(isLoading: true);
			LoadingScreen loadingScreen = LoadingScreen.GetLoadingScreen();
			loadingScreen.ShowLoadingScreen();
			if (logger.logEnabled)
			{
				logger.Log($"Client starting to load {msg.Key}");
			}
			MapLoader.LoadResult loadResult;
			try
			{
				loadResult = await mapLoader.Load(msg.Key, loadingScreen);
			}
			catch (Exception arg)
			{
				logger.LogError($"Exception in Scene load: {arg}");
				loadResult = MapLoader.LoadResult.Failed;
			}
			SetLoading(isLoading: false);
			GameManager.ResetGameResolution();
			try
			{
				if ((uint)loadResult <= 2u || (uint)(loadResult - 3) > 2u)
				{
					logger.LogError($"Load {msg.Key} failed with result={loadResult}");
					Client.Disconnect();
				}
				else
				{
					if (logger.LogEnabled())
					{
						logger.Log("Client finished loading, telling server we have finished");
					}
					Client.Send(default(SceneReadyMessage));
				}
			}
			catch (Exception arg2)
			{
				logger.LogError($"Exception handling LoadResult: {arg2}");
			}
			UniTask.Void(async delegate
			{
				await UniTask.Yield();
				await UniTask.Yield();
				if (loadingScreen != null)
				{
					loadingScreen.HideLoadingScreen();
				}
			});
		}

		private void HandleLoadWaitingMessage(INetworkPlayer player, LoadWaitingSceneMessage _)
		{
			JoinLobbyOverlay.Close();
			ClientLoadEmpty().Forget();
		}

		private async UniTask ClientLoadEmpty()
		{
			await LoadSystemScene(MapLoader.Empty, warnIfAlreadyLoaded: true);
			await UniTask.Yield();
			waitingPanel = UnityEngine.Object.Instantiate(waitingPrefab);
			waitingPanel.SetLoadingMessage(latestWaitingMessage.LoadingMessage);
		}

		private void ClientDisconnected(ClientStoppedReason disconnectReason)
		{
			if (personaStateChangeCallback != null)
			{
				personaStateChangeCallback.Dispose();
				personaStateChangeCallback = null;
			}
			if (Server.Active)
			{
				return;
			}
			if (GameManager.disconnectInfo == null)
			{
				switch (disconnectReason)
				{
				case ClientStoppedReason.ServerFull:
					GameManager.SetDisconnectReason(new DisconnectInfo("Failed to join: Server full"));
					break;
				case ClientStoppedReason.ConnectingTimeout:
					GameManager.SetDisconnectReason(new DisconnectInfo("Failed to join: Timeout"));
					break;
				case ClientStoppedReason.KeyInvalid:
					GameManager.SetDisconnectReason(new DisconnectInfo("Failed to join: Invalid version"));
					break;
				case ClientStoppedReason.Timeout:
					GameManager.SetDisconnectReason(new DisconnectInfo("Disconnected: Timeout"));
					break;
				default:
					GameManager.SetDisconnectReason(new DisconnectInfo("Disconnected: Unknown"));
					break;
				}
			}
			if (!stopping)
			{
				HandleStopAsync(disconnectReason).Forget();
			}
		}

		private void SetLoading(bool isLoading)
		{
			loadingScene = isLoading;
			CursorManager.SetFlag(CursorFlags.Loading, isLoading);
			if (isLoading)
			{
				CursorManager.SetFlag(CursorFlags.EmptyScene, value: false);
			}
			else
			{
				PlayerLoopPerformanceTracker.CheckSetup();
			}
		}

		public async UniTask<bool> LoadSystemScene(string scene, bool warnIfAlreadyLoaded)
		{
			Scene activeScene = SceneManager.GetActiveScene();
			MapKey = null;
			if (logger.LogEnabled())
			{
				logger.Log("Trying to load '" + scene + "'");
			}
			if (activeScene.name == scene || activeScene.path == scene)
			{
				if (warnIfAlreadyLoaded)
				{
					Debug.LogWarning("Already loaded " + scene);
				}
				else if (logger.LogEnabled())
				{
					logger.Log("Already loaded " + scene);
				}
				return false;
			}
			if (logger.LogEnabled())
			{
				logger.Log("Start load scene '" + scene + "'. Old Scene = " + activeScene.name);
			}
			SetLoading(isLoading: true);
			try
			{
				mapLoader.ClearMap();
				MissionManager.SetNullMission();
				SlowUpdateExtensions.DestroyRunner();
				LoadingScreen loadingScreen = LoadingScreen.GetLoadingScreen();
				loadingScreen.ShowLoadingScreen();
				AsyncOperation asyncOperation;
				if (scene == MapLoader.Empty)
				{
					CursorManager.SetFlag(CursorFlags.EmptyScene, value: true);
					asyncOperation = mapLoader.LoadEmpty();
					if (asyncOperation == null)
					{
						return false;
					}
				}
				else
				{
					asyncOperation = SceneManager.LoadSceneAsync(scene);
					if (asyncOperation == null)
					{
						logger.LogWarning("LoadSceneAsync returned null");
						return false;
					}
				}
				await asyncOperation.ToUniTask(loadingScreen);
				GameManager.ResetGame();
				await MemoryCleanup();
				loadingScreen.HideLoadingScreen();
			}
			finally
			{
				SetLoading(isLoading: false);
			}
			if (Client.Active)
			{
				if (logger.LogEnabled())
				{
					logger.Log("Client preparing scene objects");
				}
				ClientObjectManager.PrepareToSpawnSceneObjects();
			}
			if (Server.Active)
			{
				if (logger.LogEnabled())
				{
					logger.Log("Server spawning scene Objects");
				}
				ServerObjectManager.SpawnSceneObjects();
			}
			if (logger.LogEnabled())
			{
				logger.Log("Finished load scene '" + scene + "'");
			}
			return true;
		}

		public static async UniTask MemoryCleanup()
		{
			await Resources.UnloadUnusedAssets();
			LogMonoHeap("MonoHeap: {0} (BEFORE CLEANUP)");
			GC.Collect(2, GCCollectionMode.Forced, blocking: true);
			GC.WaitForPendingFinalizers();
			LogMonoHeap("MonoHeap: {0} (After CLEANUP)");
		}

		public static void LogMonoHeap(string format)
		{
			int num = 1048576;
			long num2 = Profiler.GetMonoUsedSizeLong() / num;
			long num3 = Profiler.GetMonoHeapSizeLong() / num;
			Debug.Log(string.Format(format, $"{num2} / {num3} MB"));
		}

		private void RegisterPrefabs()
		{
			ClientObjectManager.RegisterPrefab(gamePlayerPrefab.Identity);
			PlayerHelper.RegisterPrefabs(ClientObjectManager);
			ClientObjectManager.RegisterPrefab(GameAssets.i.airbasePrefab.GetNetworkIdentity());
			RegisterAll(Encyclopedia.i.aircraft);
			RegisterAll(Encyclopedia.i.vehicles);
			RegisterAll(Encyclopedia.i.missiles);
			RegisterAll(Encyclopedia.i.buildings);
			RegisterAll(Encyclopedia.i.ships);
			RegisterAll(Encyclopedia.i.scenery);
			RegisterAll(Encyclopedia.i.otherUnits);
		}

		private void RegisterAll<T>(List<T> list) where T : UnitDefinition
		{
			foreach (T item in list)
			{
				ClientObjectManager.RegisterPrefab(item.unitPrefab.GetNetworkIdentity());
			}
		}

		public async UniTaskVoid KickPlayerAsync(Player player)
		{
			if (!Server.Active)
			{
				throw new MethodInvocationException("KickPlayerAsync called when server is not active");
			}
			INetworkPlayer conn = player.Owner;
			try
			{
				authenticator.OnKick(conn);
				player.KickReason(string.Empty);
				await UniTask.Delay(100);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			conn.Disconnect();
		}

		public void KickPlayer(INetworkPlayer conn)
		{
			if (!Server.Active)
			{
				throw new MethodInvocationException("KickPlayer called when server is not active");
			}
			authenticator.OnKick(conn);
			conn.Disconnect();
		}

		public void StartHost(HostOptions options)
		{
			StartHostAsync(options).Forget();
		}

		public async UniTask StartHostAsync(HostOptions options)
		{
			if (Server.Active)
			{
				throw new InvalidOperationException("Can't start server because it is already running");
			}
			if (Client.Active)
			{
				throw new InvalidOperationException("Can't start server because client is running");
			}
			ConfigureNetwork(options.SocketType, options.MaxConnections ?? 16);
			if (options.UdpPort.HasValue)
			{
				udpTransport.Port = (ushort)options.UdpPort.Value;
			}
			if (string.IsNullOrEmpty(options.Password))
			{
				authenticator.ClearServerPassword();
			}
			else
			{
				authenticator.SetServerPassword(options.Password);
			}
			Server.StartServer(Client);
			((IMessageReceiver)Server.MessageHandler).RegisterHandler((MessageDelegateWithPlayer<SceneReadyMessage>)HandleSceneReadyMessage);
			CursorManager.SetFlag(CursorFlags.Loading, value: true);
			GameManager.SetGameState(options.GameState);
			GameManager.SetupGame();
			if (options.GameState == GameState.Editor)
			{
				TimeScaleManager.Scale = 0f;
			}
			if (!string.IsNullOrEmpty(options.SystemScene))
			{
				if (await LoadSystemScene(options.SystemScene, warnIfAlreadyLoaded: true))
				{
					Host_SceneLoaded();
				}
				else
				{
					logger.LogError("Failed to load system scene " + options.SystemScene);
				}
				return;
			}
			(bool, MapLoader.LoadResult) tuple = await ServerLoadMapScene(options.Map, null);
			if (tuple.Item1)
			{
				Host_SceneLoaded();
				return;
			}
			logger.LogError($"Load {options.Map} failed with result={tuple.Item2}");
			Server.Stop();
		}

		public async UniTask<(bool success, MapLoader.LoadResult result)> ServerLoadMapScene(MapKey mapKey, IProgress<float> progress)
		{
			Server.LocalPlayer.SceneIsReady = false;
			SetLoading(isLoading: true);
			LoadingScreen loading = null;
			bool withLoadingScreen = !GameManager.IsHeadless;
			if (withLoadingScreen)
			{
				loading = LoadingScreen.GetLoadingScreen();
				loading.ShowLoadingScreen();
			}
			MapLoader.LoadResult loadResult;
			try
			{
				if (mapKey.IsDefault())
				{
					ColorLog<NetworkManagerNuclearOption>.Info($"Replacing empty key with default: {mapKey} -> {mapLoader.DefaultMap}");
					mapKey = mapLoader.DefaultMap;
				}
				MapKey = mapKey;
				IProgress<float> progress2 = null;
				if (progress != null || withLoadingScreen)
				{
					progress2 = new Progress<float>(delegate(float value)
					{
						if (progress != null)
						{
							progress.Report(value);
						}
						if (withLoadingScreen)
						{
							((IProgress<float>)loading).Report(value);
						}
					});
				}
				loadResult = await mapLoader.Load(mapKey, progress2);
			}
			finally
			{
				SetLoading(isLoading: false);
				if (withLoadingScreen)
				{
					loading.HideLoadingScreen();
				}
			}
			if ((uint)loadResult <= 2u || (uint)(loadResult - 3) > 2u)
			{
				return (false, loadResult);
			}
			return (true, loadResult);
		}

		public void Host_SceneLoaded()
		{
			if (logger.LogEnabled())
			{
				logger.Log("Server finished loading");
			}
			Server.LocalPlayer.SceneIsReady = true;
			if (logger.LogEnabled())
			{
				logger.Log("Spawning Player object for host and connections");
			}
			foreach (INetworkPlayer authenticatedPlayer in Server.AuthenticatedPlayers)
			{
				if (authenticatedPlayer.SceneIsReady)
				{
					SpawnCharacter(authenticatedPlayer);
				}
			}
			if (GameManager.gameState == GameState.Editor)
			{
				SceneSingleton<GameplayUI>.i.OpenMissionEditor();
			}
		}

		public void StartClient(ConnectOptions options)
		{
			if (Server.Active)
			{
				throw new InvalidOperationException("Can't start client because server is running");
			}
			if (Client.Active)
			{
				throw new InvalidOperationException("Can't start client because it is already running");
			}
			ConfigureNetwork(options.SocketType, 1);
			MissionManager.SetNullMission();
			if (string.IsNullOrEmpty(options.Password))
			{
				authenticator.ClearClientPassword();
			}
			else
			{
				authenticator.SetClientPassword(options.Password);
			}
			switch (options.SocketType)
			{
			case SocketType.UDP:
			case SocketType.LagUdp:
				Client.Connect(options.UdpHost, options.UdpPort.HasValue ? new ushort?((ushort)options.UdpPort.Value) : ((ushort?)null));
				break;
			case SocketType.Steam:
			case SocketType.SteamGameServer:
			case SocketType.LagSteam:
				Client.Connect(options.SteamLobbyIDString);
				break;
			default:
				throw new ArgumentException("can not use SocketType.Offline for client");
			}
		}

		private void ConfigureNetwork(SocketType type, int maxConnections)
		{
			/*
			Config peerConfig = new Config
			{
				MaxConnections = maxConnections,
				ConnectAttemptInterval = 0.25f,
				MaxConnectAttempts = 40,
				TimeoutDuration = 30f,
				MaxReliableFragments = 100,
				MaxReliablePacketsInSendBufferPerConnection = 3000
			};
			Server.PeerConfig = peerConfig;
			Client.PeerConfig = peerConfig;
			Server.Listening = type != SocketType.Offline;
			switch (type)
			{
			case SocketType.Offline:
			case SocketType.UDP:
				Debug.Log("Setting UDP socket");
				Server.SocketFactory = udpTransport;
				Client.SocketFactory = udpTransport;
				break;
			case SocketType.Steam:
			case SocketType.SteamGameServer:
				Debug.Log("Setting Steam socket");
				steamTransport.GameServer = type == SocketType.SteamGameServer;
				Server.SocketFactory = steamTransport;
				Client.SocketFactory = steamTransport;
				break;
			case SocketType.LagUdp:
				Server.SocketFactory = lagTransport;
				Client.SocketFactory = lagTransport;
				//lagTransport.inner = udpTransport;
				break;
			case SocketType.LagSteam:
				Server.SocketFactory = lagTransport;
				Client.SocketFactory = lagTransport;
				//lagTransport.inner = steamTransport;
				break;
			}*/
		}

		public void Stop(bool setDisconnectReason)
		{
			ColorLog<NetworkManagerNuclearOption>.Info($"Stop called, setDisconnectReason: {setDisconnectReason}. StackTrace:\n{Environment.StackTrace}");
			StopAsync(setDisconnectReason).Forget();
		}

		public async UniTask StopAsync(bool setDisconnectReason)
		{
			ColorLog<NetworkManagerNuclearOption>.Info($"StopAsync called, setDisconnectReason: {setDisconnectReason}. StackTrace:\n{Environment.StackTrace}");
			if (setDisconnectReason)
			{
				GameManager.SetDisconnectReason(DisconnectInfo.NoReason());
			}
			UniTask uniTask = HandleStopAsync(null);
			if (Server.Active)
			{
				Server.Stop();
			}
			else if (Client.Active)
			{
				Client.Disconnect();
			}
			else
			{
				Debug.LogWarning("Stop called when network was not active");
			}
			await uniTask;
		}

		private async UniTask HandleStopAsync(ClientStoppedReason? disconnectReason)
		{
			if (stopping)
			{
				Debug.LogError("Already Stopping network");
				return;
			}
			stopping = true;
			try
			{
				if (SceneManager.GetActiveScene().path == MapLoader.MultiplayerMenu)
				{
					JoinLobbyOverlay.Open(JoinProgress.GetFailReason(disconnectReason));
					return;
				}
				MusicManager.i.StopMusic();
				GameManager.SetGameState(GameState.Menu);
				await LoadSystemScene(MapLoader.MainMenu, warnIfAlreadyLoaded: false);
			}
			finally
			{
				stopping = false;
			}
		}

		public static void HandleHostEndedMessage(HostEndedMessage msg)
		{
			GameManager.SetDisconnectReason(new DisconnectInfo(GetHostSteamName() + " ended the game"));
		}

		public void HandleWaitingSceneProgressMessage(ServerLoadingProgressMessage msg)
		{
			latestWaitingMessage = msg;
			if (waitingPanel != null)
			{
				waitingPanel.SetLoadingMessage(msg.LoadingMessage);
			}
		}

		public static bool HasOtherPlayers(out int numberOfPlayers)
		{
			numberOfPlayers = UnitRegistry.playerLookup.Count;
			if (i.Server.Active && GameManager.gameState == GameState.Multiplayer)
			{
				return numberOfPlayers > 1;
			}
			return false;
		}

		private void OnPersonaStateChange(PersonaStateChange_t param)
		{
			CSteamID cSteamID = new CSteamID(param.m_ulSteamID);
			foreach (Player value in UnitRegistry.playerLookup.Values)
			{
				if (value.CSteamID == cSteamID)
				{
					value.Steam_OnPersonaStateChanged();
					break;
				}
			}
		}

		public static string GetHostSteamName()
		{
			foreach (Player value in UnitRegistry.playerLookup.Values)
			{
				if (value.IsHostPlayer)
				{
					return value.GetDisplayName(PlayerNameContext.Other);
				}
			}
			return "server";
		}
	}
}
