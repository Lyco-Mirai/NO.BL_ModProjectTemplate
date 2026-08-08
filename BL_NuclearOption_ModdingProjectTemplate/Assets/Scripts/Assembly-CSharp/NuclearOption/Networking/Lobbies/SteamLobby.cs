using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.SavedMission;
using NuclearOption.Social;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking.Lobbies
{
	public class SteamLobby : MonoBehaviour
	{
		private class ServerListRequest
		{
			public readonly List<ISteamQuery> RunningQueries = new List<ISteamQuery>();

			private readonly SteamLobby manager;

			private readonly ISteamMatchmakingServerListResponse response;

			private LobbySearchFilter filter;

			private HServerListRequest? request;

			private CliJoinHandler _cliJoin;

			public int searchNumber { get; private set; }

			public bool InProgress { get; private set; }

			public ServerListRequest(SteamLobby manager)
			{
				this.manager = manager;
				response = new ISteamMatchmakingServerListResponse(OnServerResponded, OnServerFailedToRespond, OnRefreshComplete);
			}

			public void RequestServerLobbies(LobbySearchFilter filter)
			{
				if (request.HasValue)
				{
					ColorLog<SteamLobby>.InfoWarn("RequestServerLobbies was already refreshing, ignoring 2nd request");
					return;
				}
				this.filter = filter;
				MatchMakingKeyValuePair_t[] filters = BuildServerRequestFilters(filter);
				ColorLog<SteamLobby>.Info($"StartRequest searchNumber={searchNumber}");
				StartRequest(filters);
			}

			public void RequestJoinId(CliJoinHandler cliJoin)
			{
				_cliJoin = cliJoin;
				MatchMakingKeyValuePair_t[] filters = new MatchMakingKeyValuePair_t[1]
				{
					new MatchMakingKeyValuePair_t
					{
						m_szKey = "steamid",
						m_szValue = cliJoin.pendingId.ToString()
					}
				};
				ColorLog<SteamLobby>.Info($"StartRequest joinLobbyId={cliJoin.pendingId}");
				StartRequest(filters);
			}

			private void StartRequest(MatchMakingKeyValuePair_t[] filters)
			{
				searchNumber++;
				InProgress = true;
				request = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), filters, (uint)filters.Length, response);
				UniTask.Void(async delegate
				{
					while (SteamMatchmakingServers.IsRefreshing(request.Value))
					{
						await UniTask.Yield();
						if (manager.destroyCancellationToken.IsCancellationRequested)
						{
							ColorLog<SteamLobby>.Info("Request cancelled from destroyCancellationToken");
							SteamMatchmakingServers.CancelQuery(request.Value);
							break;
						}
					}
					ColorLog<SteamLobby>.Info("Request finished (or cancelled)");
					if (_cliJoin != null && !manager.destroyCancellationToken.IsCancellationRequested)
					{
						_cliJoin.OnDiscoveryFailed();
					}
					_cliJoin = null;
					SteamMatchmakingServers.ReleaseRequest(request.Value);
					InProgress = false;
					request = null;
					manager.CheckPendingRefresh();
				});
			}

			public void Cancel()
			{
				ColorLog<SteamLobby>.Info("Request cancelled because refresh pressed again");
				if (request.HasValue)
				{
					SteamMatchmakingServers.CancelQuery(request.Value);
				}
				foreach (ISteamQuery runningQuery in RunningQueries)
				{
					runningQuery.Cancel(remove: false);
				}
				RunningQueries.Clear();
			}

			private static MatchMakingKeyValuePair_t[] BuildServerRequestFilters(LobbySearchFilter filter)
			{
				List<MatchMakingKeyValuePair_t> list = new List<MatchMakingKeyValuePair_t>();
				List<string> list2 = new List<string>();
				if (filter.HideFull)
				{
					list.Add(new MatchMakingKeyValuePair_t
					{
						m_szKey = "notfull"
					});
				}
				if (filter.HideEmpty)
				{
					list.Add(new MatchMakingKeyValuePair_t
					{
						m_szKey = "hasplayers"
					});
				}
				if (!filter.ignoreVersionFilter)
				{
					list2.Add("v=" + Application.version);
				}
				if (filter.HidePasswordProtected)
				{
					list2.Add("p=0");
				}
				MissionPvpType missionPvpType = filter.MissionPvpType;
				if ((uint)(missionPvpType - 1) <= 1u)
				{
					list2.Add("t=" + MissionTag.GetPvpTypeLobbyString(filter.MissionPvpType));
				}
				if (list2.Count > 0)
				{
					list.Add(new MatchMakingKeyValuePair_t
					{
						m_szKey = "gametagsand",
						m_szValue = string.Join(",", list2)
					});
				}
				return list.ToArray();
			}

			private void OnServerResponded(HServerListRequest hRequest, int iServer)
			{
				ColorLog<SteamLobby>.Info($"Request Server List, success index:{iServer}");
				gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(hRequest, iServer);
				OnServerUpdated(serverDetails, filter);
			}

			private void OnServerFailedToRespond(HServerListRequest hRequest, int iServer)
			{
				gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(hRequest, iServer);
				ColorLog<SteamLobby>.InfoWarn($"Request Server List, fail index:{iServer}, Query Address:{serverDetails.m_NetAdr.GetQueryAddressString()}");
			}

			private void OnRefreshComplete(HServerListRequest hRequest, EMatchMakingServerResponse response)
			{
				ColorLog<SteamLobby>.Info($"Request Server List, Refresh complete: {response}");
				if (!manager.playerLobbyRefreshInProgress)
				{
					manager.OnLobbyRefreshFinished?.Invoke();
				}
			}

			private void OnServerUpdated(gameserveritem_t details, LobbySearchFilter filter)
			{
				if (manager.pendingSearch.HasValue)
				{
					ColorLog<SteamLobby>.Info("Skipping OnServerUpdated because of pending refresh");
					return;
				}
				if (details == null)
				{
					Debug.LogError("No server details");
					return;
				}
				CSteamID steamID = details.m_steamID;
				ColorLog<SteamLobby>.Info($"{steamID} Game Tags:{details.GetGameTags()}");
				CliJoinHandler cliJoin = null;
				if (_cliJoin != null && steamID == _cliJoin.pendingId)
				{
					cliJoin = _cliJoin;
					_cliJoin = null;
				}
				ServerLobbyInstance serverLobbyInstance2;
				if (manager._lobbyData.TryGetValue(steamID, out var value))
				{
					if (!(value is ServerLobbyInstance serverLobbyInstance))
					{
						Debug.LogError($"Server with id {steamID} was not a Server");
						return;
					}
					serverLobbyInstance2 = serverLobbyInstance;
				}
				else
				{
					serverLobbyInstance2 = new ServerLobbyInstance();
					manager._lobbyData.Add(steamID, serverLobbyInstance2);
				}
				serverLobbyInstance2.SetDetails(details);
				new SearchQuery(manager, this, serverLobbyInstance2, filter, cliJoin).Run();
			}
		}

		private interface ISteamQuery
		{
			void Cancel(bool remove);
		}

		private class SearchQuery : ISteamQuery
		{
			private readonly SteamLobby manager;

			private readonly ServerLobbyInstance lobby;

			private readonly ServerListRequest listRequest;

			private readonly LobbySearchFilter filter;

			private readonly ISteamMatchmakingRulesResponse response;

			private readonly CliJoinHandler cliJoin;

			private HServerQuery query;

			private int searchNumber;

			public SearchQuery(SteamLobby manager, ServerListRequest listRequest, ServerLobbyInstance lobby, LobbySearchFilter filter, CliJoinHandler cliJoin)
			{
				this.manager = manager;
				this.listRequest = listRequest;
				this.lobby = lobby;
				this.filter = filter;
				this.cliJoin = cliJoin;
				response = new ISteamMatchmakingRulesResponse(RulesResponded, RulesFailedToRespond, RulesRefreshComplete);
			}

			public void Run()
			{
				searchNumber = listRequest.searchNumber;
				listRequest.RunningQueries.Add(this);
				ColorLog<SteamLobby>.Info($"Server[{lobby.details.m_steamID}] SearchQuery for search {searchNumber}");
				servernetadr_t netAdr = lobby.details.m_NetAdr;
				query = SteamMatchmakingServers.ServerRules(netAdr.GetIP(), netAdr.GetQueryPort(), response);
			}

			public void Cancel(bool remove = true)
			{
				SteamMatchmakingServers.CancelServerQuery(query);
				if (remove)
				{
					listRequest.RunningQueries.Remove(this);
				}
			}

			private void RulesResponded(string key, string value)
			{
				if (!string.IsNullOrEmpty(key))
				{
					lobby.SetRule(key, value ?? string.Empty);
				}
			}

			private void RulesFailedToRespond()
			{
				ColorLog<SteamLobby>.InfoWarn($"Server[{lobby.details.m_steamID}] Rule failed to respond");
				listRequest.RunningQueries.Remove(this);
				cliJoin?.OnDiscoveryFailed();
			}

			private void RulesRefreshComplete()
			{
				ColorLog<SteamLobby>.Info($"Server[{lobby.details.m_steamID}] Rule refresh complete");
				listRequest.RunningQueries.Remove(this);
				if (cliJoin != null)
				{
					cliJoin.OnServerFound(lobby);
				}
				else
				{
					if (filter.HidePasswordProtected && lobby.IsPasswordProtected(out var _))
					{
						return;
					}
					if (filter.HideFull || filter.HideEmpty)
					{
						lobby.GetPlayerCounts(out var current, out var max);
						if ((filter.HideFull && current >= max) || (filter.HideEmpty && current == 0))
						{
							return;
						}
					}
					if (filter.PingDistanceAllowed(lobby.details.m_nPing))
					{
						if (listRequest.searchNumber != searchNumber)
						{
							ColorLog<SteamLobby>.InfoWarn($"Server[{lobby.details.m_steamID}] searchNumber ({searchNumber} -> {listRequest.searchNumber}) changed before rule refresh completed, Skipping OnLobbyDataUpdated");
							return;
						}
						lobby.InList = true;
						manager.OnLobbyDataUpdated?.Invoke(lobby);
					}
				}
			}
		}

		private class PingQuery : ISteamQuery
		{
			private readonly SteamLobby manager;

			private readonly ServerLobbyInstance lobby;

			private readonly List<ISteamQuery> runningQueries;

			private readonly ISteamMatchmakingPingResponse response;

			private HServerQuery query;

			public PingQuery(SteamLobby manager, List<ISteamQuery> runningQueries, ServerLobbyInstance lobby)
			{
				this.manager = manager;
				this.runningQueries = runningQueries;
				this.lobby = lobby;
				response = new ISteamMatchmakingPingResponse(ServerResponded, ServerFailedToRespond);
			}

			public void Run()
			{
				runningQueries.Add(this);
				servernetadr_t netAdr = lobby.details.m_NetAdr;
				query = SteamMatchmakingServers.PingServer(netAdr.GetIP(), netAdr.GetQueryPort(), response);
			}

			public void Cancel(bool remove = true)
			{
				SteamMatchmakingServers.CancelServerQuery(query);
				if (remove)
				{
					runningQueries.Remove(this);
				}
			}

			private void ServerResponded(gameserveritem_t server)
			{
				runningQueries.Remove(this);
				lobby.SetPingResult(server.m_nPing);
				if (lobby.InList)
				{
					manager.OnLobbyPingUpdated?.Invoke(lobby);
				}
			}

			private void ServerFailedToRespond()
			{
				runningQueries.Remove(this);
			}
		}

		private class CliJoinHandler
		{
			private readonly SteamLobby manager;

			public CSteamID pendingId;

			private ServerListRequest serverRequest;

			public CliJoinHandler(SteamLobby manager)
			{
				this.manager = manager;
			}

			public void JoinBySteamId(CSteamID lobbyId)
			{
				if (lobbyId.IsLobby())
				{
					if (SteamMatchmaking.RequestLobbyData(lobbyId))
					{
						manager.joinPendingLobby = new PlayerLobbyInstance(lobbyId);
						JoinLobbyOverlay.Open(JoinProgress.Joining("Requesting lobby data..."));
					}
					else
					{
						JoinLobbyOverlay.Open(JoinProgress.Fail("Failed to request lobby data from Steam"));
					}
				}
				else if (lobbyId.BGameServerAccount())
				{
					JoinLobbyOverlay.Open(JoinProgress.Joining("Finding server..."));
					pendingId = lobbyId;
					serverRequest = new ServerListRequest(manager);
					serverRequest.RequestJoinId(this);
				}
				else
				{
					string text = $"Invalid Steam ID type: {lobbyId.GetEAccountType()}. Lobbies must be joined via Steam ID, Dedicated servers must be joined via IP.";
					ColorLog<SteamLobby>.LogError(text);
					JoinLobbyOverlay.Open(JoinProgress.Fail(text));
				}
			}

			public void OnServerFound(ServerLobbyInstance lobby)
			{
				pendingId = CSteamID.Nil;
				manager.TryJoinLobby(lobby, null, promptIfPasswordNeeded: true);
			}

			public void OnDiscoveryFailed()
			{
				string text = $"Server {pendingId} could not be found.\nIt may be offline or unlisted.";
				pendingId = CSteamID.Nil;
				ColorLog<SteamLobby>.LogError(text);
				JoinLobbyOverlay.Open(JoinProgress.Fail(text));
			}
		}

		public static SteamLobby instance;

		public NetworkManagerNuclearOption Manager;

		private Callback<LobbyCreated_t> _lobbyCreated;

		private Callback<GameLobbyJoinRequested_t> _joinRequest;

		private Callback<LobbyMatchList_t> _lobbyList;

		private Callback<LobbyDataUpdate_t> _lobbyDataUpdated;

		private readonly Dictionary<CSteamID, LobbyInstance> _lobbyData = new Dictionary<CSteamID, LobbyInstance>();

		private HostedLobbyInstance _hostedLobby;

		private int _currentLobbyMaxPlayers;

		private LobbyInstance _joinedLobby;

		private bool _initialized;

		private bool hasLocalLocation;

		private UniTaskCompletionSource<LobbyCreated_t> HostLobbyCompletion;

		public string CurrentLobbyName;

		private LobbyInstance joinPendingLobby;

		private ServerListRequest serverListRequest;

		private bool locationTaskRunning;

		private bool playerLobbyRefreshInProgress;

		private LobbySearchFilter? pendingSearch;

		private bool serverLobbiesRefreshInProgress => serverListRequest.InProgress;

		public event Action OnLobbyListCleared;

		public event Action<LobbyInstance> OnLobbyDataUpdated;

		public event Action<ServerLobbyInstance> OnLobbyPingUpdated;

		public event Action OnLobbyRefreshFinished;

		public event Action<string> OnLocationUpdated;

		private void Awake()
		{
			serverListRequest = new ServerListRequest(this);
			if (instance == null)
			{
				instance = this;
			}
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			_ = base.destroyCancellationToken;
		}

		private void Start()
		{
			StartAsync().Forget();
		}

		private async UniTaskVoid StartAsync()
		{
			await MainMenu.WaitForLoaded(base.destroyCancellationToken);
			if (!SteamManager.ClientInitialized && !SteamManager.ServerInitialized)
			{
				Debug.LogError("Steam was not Initialized");
			}
			if (SteamManager.ClientInitialized)
			{
				_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
				_joinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
				_lobbyList = Callback<LobbyMatchList_t>.Create(RequestLobbyListCallback);
				_lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(LobbyDataUpdated);
				_initialized = true;
				Manager.Client.Disconnected.AddListener(ClientDisconnected);
				Manager.Server.Stopped.AddListener(SetLobbyEnded);
			}
			_ = SteamManager.ServerInitialized;
		}

		private void OnDestroy()
		{
			if (_initialized)
			{
				_lobbyCreated.Dispose();
				_joinRequest.Dispose();
				_lobbyList.Dispose();
				_lobbyDataUpdated.Dispose();
				Manager.Client.Disconnected.RemoveListener(ClientDisconnected);
			}
		}

		private void ClientDisconnected(ClientStoppedReason _)
		{
			if (!Manager.Server.IsHost)
			{
				LeaveLobby();
			}
		}

		private void SetLobbyEnded()
		{
			_hostedLobby.SetLobbyEnded();
			LeaveLobby();
		}

		private void OnApplicationQuit()
		{
			LeaveLobby();
		}

		public async UniTask<HostedLobbyInstance?> HostLobby(int maxPlayers, ELobbyType lobbyType)
		{
			var (flag, lobbyCreated_t) = await Steam_CreateLobbyAsync(lobbyType);
			if (flag)
			{
				JoinLobbyOverlay.Open(JoinProgress.Fail("Timeout while creating lobby"));
				return null;
			}
			if (lobbyCreated_t.m_eResult != EResult.k_EResultOK)
			{
				Debug.LogError($"Fail to create lobby: {lobbyCreated_t.m_eResult}");
				JoinLobbyOverlay.Open(JoinProgress.Fail("Failed to create new lobby"));
				return null;
			}
			ColorLog<SteamLobby>.Info($"Lobby created successfully {lobbyCreated_t.m_ulSteamIDLobby}");
			CSteamID cSteamID = new CSteamID(lobbyCreated_t.m_ulSteamIDLobby);
			_hostedLobby = new HostedLobbyInstance(cSteamID);
			_hostedLobby.SetData("HostAddress", SteamUser.GetSteamID().ToString());
			_hostedLobby.SetData("version", Application.version);
			_hostedLobby.SetStartTime();
			_hostedLobby.SetData("max_members", maxPlayers.ToString());
			_currentLobbyMaxPlayers = maxPlayers;
			RichPresenceManager.SetLobby(cSteamID);
			RichPresenceManager.SetPlayerCount(1, maxPlayers);
			return _hostedLobby;
		}

		private async UniTask<(bool IsTimeout, LobbyCreated_t Result)> Steam_CreateLobbyAsync(ELobbyType lobbyType)
		{
			if (HostLobbyCompletion != null)
			{
				Debug.LogError("Steam_CreateLobbyAsync already running");
				return default((bool, LobbyCreated_t));
			}
			ColorLog<SteamLobby>.Info("Hosting Lobby");
			HostLobbyCompletion = new UniTaskCompletionSource<LobbyCreated_t>();
			SteamMatchmaking.CreateLobby(lobbyType, 250);
			(bool, LobbyCreated_t) obj = await HostLobbyCompletion.Task.TimeoutWithoutException(TimeSpan.FromSeconds(20.0));
			bool item = obj.Item1;
			LobbyCreated_t item2 = obj.Item2;
			HostLobbyCompletion = null;
			if (item)
			{
				Debug.LogError("Create lobby timeout");
			}
			return (item, item2);
		}

		private void OnLobbyCreated(LobbyCreated_t result)
		{
			ColorLog<SteamLobby>.Info("LobbyCreated_t callback");
			if (HostLobbyCompletion != null)
			{
				HostLobbyCompletion.TrySetResult(result);
			}
			else
			{
				ColorLog<SteamLobby>.LogError("LobbyCreated_t no HostLobbyCompletion event, CreateLobby may have timed out before steam returned result");
			}
		}

		public void CheckRelayLocationTask()
		{
			if (!locationTaskRunning)
			{
				GetLocationLoop().Forget();
			}
		}

		private async UniTaskVoid GetLocationLoop()
		{
			locationTaskRunning = true;
			try
			{
				await WaitForLocalLocation();
				CancellationToken cancel = base.destroyCancellationToken;
				while (!cancel.IsCancellationRequested)
				{
					if (GetLocalLocation(out var location))
					{
						LocationUpdated(location);
					}
					else
					{
						Debug.LogError("Failed to get host location");
					}
					await UniTask.Delay(TimeSpan.FromMinutes(5.0));
				}
			}
			finally
			{
				locationTaskRunning = false;
			}
		}

		public void CheckPingServers()
		{
			float time = Time.time;
			foreach (LobbyInstance value in _lobbyData.Values)
			{
				if (value is ServerLobbyInstance { InList: not false } serverLobbyInstance && !(time <= serverLobbyInstance.NextPingTime))
				{
					serverLobbyInstance.NextPingTime = time + intervalFromCount(serverLobbyInstance.PingCount);
					new PingQuery(this, serverListRequest.RunningQueries, serverLobbyInstance).Run();
				}
			}
			static float intervalFromCount(int count)
			{
				if (count < 5)
				{
					return (float)count * 2f;
				}
				return 30f;
			}
		}

		public void ClearLobbyCache()
		{
			_lobbyData.Clear();
		}

		private async UniTask WaitForLocalLocation()
		{
			if (hasLocalLocation)
			{
				return;
			}
			SteamNetworkingUtils.InitRelayNetworkAccess();
			CancellationToken cancel = base.destroyCancellationToken;
			int attempt = 0;
			float totalWait = 0f;
			while (!cancel.IsCancellationRequested)
			{
				if (GetLocalLocation(out var _))
				{
					Debug.Log("GetLocalPingLocation Success");
					hasLocalLocation = true;
					break;
				}
				attempt++;
				int num = Mathf.Clamp((attempt < 8) ? (100 * (int)Mathf.Pow(2f, attempt)) : 20000, 200, 20000);
				totalWait += (float)num;
				Debug.Log($"GetLocalPingLocation Failed, attempt={attempt}, waiting {num}ms, totalWait={totalWait / 1000f:0.0}s");
				await UniTask.Delay(num);
			}
		}

		private void LocationUpdated(string loc)
		{
			this.OnLocationUpdated?.Invoke(loc);
			if (_hostedLobby.IsValid)
			{
				_hostedLobby.SetData("HostPing", loc);
			}
		}

		public static bool EstimatePing(string hostLocation, out int ping)
		{
			if (!SteamNetworkingUtils.ParsePingLocationString(hostLocation, out var result))
			{
				ping = 0;
				return false;
			}
			ping = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref result);
			return ping != -1;
		}

		private void OnJoinRequest(GameLobbyJoinRequested_t callback)
		{
			InviteJoinModal.TryJoin(delegate
			{
				if (!SteamMatchmaking.RequestLobbyData(callback.m_steamIDLobby))
				{
					Debug.LogError("Failed to get lobby data");
					JoinLobbyOverlay.Open(JoinProgress.Fail("Lobby no longer exists or Steam is unavailable"));
				}
				else
				{
					joinPendingLobby = new PlayerLobbyInstance(callback.m_steamIDLobby);
				}
			});
		}

		public void CliJoinBySteamId(CSteamID lobbyId)
		{
			new CliJoinHandler(this).JoinBySteamId(lobbyId);
		}

		public void TryJoinLobby(LobbyInstance lobby, string password, bool promptIfPasswordNeeded)
		{
			if (lobby.HostVersion != Application.version)
			{
				Debug.LogError("Version mismatch. Unable to join lobby.");
				JoinLobbyOverlay.Open(JoinProgress.Fail("Incorrect version"));
				return;
			}
			if (promptIfPasswordNeeded && string.IsNullOrEmpty(password) && lobby.IsPasswordProtected(out var _))
			{
				Debug.LogWarning("Showing password prompt to user because lobby has password");
				JoinLobbyOverlay.Close();
				LobbyPasswordPromptSingleton.ShowPrompt(lobby);
				return;
			}
			JoinLobbyOverlay.Open(JoinProgress.Joining("Connecting via Steam"));
			if (!LobbyPassword.TestShortPassword(lobby, password))
			{
				UniTask.Void(async delegate
				{
					await UniTask.Delay(UnityEngine.Random.Range(300, 800));
					JoinLobbyOverlay.Open(JoinProgress.Fail("Password incorrect"));
					LobbyPasswordPromptSingleton.ShowPrompt(lobby);
				});
				Debug.LogError("Password does not match lobby data");
				return;
			}
			ColorLog<SteamLobby>.Info($"Connecting to '{lobby.LobbyNameSanitized}' Dedicated={lobby.DedicatedServer} Modded={lobby.ModdedServer}");
			_joinedLobby = lobby;
			CurrentLobbyName = lobby.LobbyNameSanitized;
			string hostAddress = lobby.HostAddress;
			string udpAddress = lobby.UdpAddress;
			string udpPort = lobby.UdpPort;
			ConnectOptions connectOptions;
			if (string.IsNullOrEmpty(udpAddress) || string.IsNullOrEmpty(udpPort))
			{
				if (!ulong.TryParse(hostAddress, out var _))
				{
					ColorLog<SteamLobby>.InfoWarn("Steam Socket invalid id:" + hostAddress);
					JoinLobbyOverlay.Open(JoinProgress.Fail("Did not have valid SteamId for host"));
					return;
				}
				ColorLog<SteamLobby>.Info("Connecting via steam to " + hostAddress);
				connectOptions = new ConnectOptions(SocketType.Steam, hostAddress);
			}
			else
			{
				if (!ushort.TryParse(udpPort, out var result2))
				{
					Debug.LogError("Failed to part udp port");
					LeaveLobby();
					return;
				}
				ColorLog<SteamLobby>.Info($"Connecting via UDP to {udpAddress}:{result2}");
				connectOptions = new ConnectOptions(SocketType.UDP, udpAddress, result2);
			}
			connectOptions.Password = password;
			RichPresenceManager.SetLobby(_joinedLobby.LobbyId);
			if (_joinedLobby.GetPlayerCounts(out var current, out var max))
			{
				RichPresenceManager.SetPlayerCount(current, max);
			}
			try
			{
				Manager.StartClient(connectOptions);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				JoinLobbyOverlay.Open(JoinProgress.Fail("Client Error"));
			}
		}

		public void GetLobbiesList(LobbySearchFilter lobbyFilter)
		{
			ColorLog<SteamLobby>.Info("GetLobbiesList clearing lobbies");
			RequestLobbies(lobbyFilter);
		}

		private void RequestLobbies(LobbySearchFilter filter)
		{
			if (!SteamUser.BLoggedOn())
			{
				ColorLog<SteamLobby>.Info("SteamUser not logged on");
				JoinLobbyOverlay.Open(JoinProgress.Fail("Not logged in or connected to steam"));
				return;
			}
			if (serverLobbiesRefreshInProgress || playerLobbyRefreshInProgress)
			{
				ColorLog<SteamLobby>.InfoWarn($"Refresh in progress, saving pending search. server:{serverLobbiesRefreshInProgress} player:{playerLobbyRefreshInProgress}");
				serverListRequest.Cancel();
				pendingSearch = filter;
				return;
			}
			foreach (LobbyInstance value in _lobbyData.Values)
			{
				value.InList = false;
			}
			this.OnLobbyListCleared?.Invoke();
			switch (filter.ServerType)
			{
			case FilterServerType.DedicatedServerOnly:
				serverListRequest.RequestServerLobbies(filter);
				break;
			case FilterServerType.PlayerHostedOnly:
				RequestPlayerHostedLobbies(filter);
				break;
			case FilterServerType.All:
				RequestPlayerHostedLobbies(filter);
				serverListRequest.RequestServerLobbies(filter);
				break;
			}
		}

		private bool CheckPendingRefresh()
		{
			if (base.destroyCancellationToken.IsCancellationRequested)
			{
				return false;
			}
			if (pendingSearch.HasValue)
			{
				ColorLog<SteamLobby>.Info($"Waiting for pending search server:{serverLobbiesRefreshInProgress} player:{playerLobbyRefreshInProgress}");
				if (!serverLobbiesRefreshInProgress && !playerLobbyRefreshInProgress)
				{
					ColorLog<SteamLobby>.Info("Running pending refresh now");
					LobbySearchFilter value = pendingSearch.Value;
					pendingSearch = null;
					RequestLobbies(value);
					return true;
				}
			}
			return false;
		}

		private static bool GetLocalLocation(out string location)
		{
			if (SteamNetworkingUtils.GetLocalPingLocation(out var result) == -1f)
			{
				location = null;
				return false;
			}
			SteamNetworkingUtils.ConvertPingLocationToString(ref result, out location, 1024);
			return true;
		}

		private void RequestPlayerHostedLobbies(LobbySearchFilter filter)
		{
			playerLobbyRefreshInProgress = true;
			ColorLog<SteamLobby>.Info("RequestPlayerHostedLobbies");
			SteamMatchmaking.AddRequestLobbyListDistanceFilter(filter.distanceFilter ?? ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
			if (filter.HideFull)
			{
				SteamMatchmaking.AddRequestLobbyListNumericalFilter("open_member_spots", 0, ELobbyComparison.k_ELobbyComparisonNotEqual);
			}
			if (!filter.ignoreVersionFilter)
			{
				SteamMatchmaking.AddRequestLobbyListStringFilter("version", Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
			}
			if (filter.HidePasswordProtected)
			{
				SteamMatchmaking.AddRequestLobbyListStringFilter("short_password", "", ELobbyComparison.k_ELobbyComparisonEqual);
			}
			MissionPvpType missionPvpType = filter.MissionPvpType;
			if ((uint)(missionPvpType - 1) <= 1u)
			{
				SteamMatchmaking.AddRequestLobbyListStringFilter("mission_pvp_type", MissionTag.GetPvpTypeLobbyString(filter.MissionPvpType), ELobbyComparison.k_ELobbyComparisonEqual);
			}
			SteamMatchmaking.RequestLobbyList();
		}

		private void RequestLobbyListCallback(LobbyMatchList_t result)
		{
			ColorLog<SteamLobby>.Info($"Got List of Lobbies, Count={result.m_nLobbiesMatching}");
			playerLobbyRefreshInProgress = false;
			if (CheckPendingRefresh())
			{
				ColorLog<SteamLobby>.Info("Skipping RequestLobbyData because of pending refresh");
				return;
			}
			if (!serverLobbiesRefreshInProgress)
			{
				this.OnLobbyRefreshFinished?.Invoke();
			}
			for (int i = 0; i < result.m_nLobbiesMatching; i++)
			{
				CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(i);
				if (!_lobbyData.ContainsKey(lobbyByIndex))
				{
					_lobbyData.Add(lobbyByIndex, new PlayerLobbyInstance(lobbyByIndex));
				}
				SteamMatchmaking.RequestLobbyData(lobbyByIndex);
			}
		}

		private void LobbyDataUpdated(LobbyDataUpdate_t result)
		{
			ColorLog<SteamLobby>.Info($"Got Lobby data for {result.m_ulSteamIDLobby}");
			CSteamID cSteamID = new CSteamID(result.m_ulSteamIDLobby);
			LobbyInstance value;
			if (joinPendingLobby is PlayerLobbyInstance playerLobbyInstance && playerLobbyInstance.LobbyId == cSteamID)
			{
				ColorLog<SteamLobby>.Info("Join pending, starting to join");
				joinPendingLobby = null;
				TryJoinLobby(playerLobbyInstance, null, promptIfPasswordNeeded: true);
			}
			else if (_lobbyData.TryGetValue(cSteamID, out value))
			{
				if (!ulong.TryParse(((PlayerLobbyInstance)value).HostAddress, out var result2))
				{
					ColorLog<SteamLobby>.InfoWarn($"lobby {cSteamID} has no address");
					_lobbyData.Remove(cSteamID);
				}
				else if (GameManager.BlockList.Contains(new CSteamID(result2)))
				{
					ColorLog<SteamLobby>.InfoWarn($"lobby {cSteamID} (owner {result2}) is blocked");
					_lobbyData.Remove(cSteamID);
				}
				else
				{
					ColorLog<SteamLobby>.Info($"Found lobby {cSteamID} (owner {result2}) in dictionary, updating");
					value.InList = true;
					this.OnLobbyDataUpdated?.Invoke(value);
				}
			}
			else
			{
				ColorLog<SteamLobby>.InfoWarn("Could not find lobby in dictionary");
			}
		}

		public void LeaveLobby()
		{
			if (_initialized)
			{
				if (_hostedLobby.IsValid)
				{
					SteamMatchmaking.LeaveLobby(_hostedLobby.Id);
				}
				_hostedLobby = default(HostedLobbyInstance);
				_joinedLobby = null;
				RichPresenceManager.SetLobby(CSteamID.Nil);
				RichPresenceManager.SetPlayerCount(0, 0);
			}
		}

		public void SetServerPlayerCount(int playerCount)
		{
			if (_hostedLobby.IsValid)
			{
				_hostedLobby.SetCurrentPlayers(playerCount, _currentLobbyMaxPlayers);
				RichPresenceManager.SetLobby(_hostedLobby.Id);
				RichPresenceManager.SetPlayerCount(playerCount, _currentLobbyMaxPlayers);
			}
		}
	}
}
