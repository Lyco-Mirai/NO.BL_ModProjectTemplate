using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Authentication;
using Mirage.SocketLayer;
using Mirage.SteamworksSocket;
using NuclearOption.DedicatedServer;
using NuclearOption.NetworkTransforms;
using NuclearOption.Networking.Lobbies;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking.Authentication
{
	public class NetworkAuthenticatorNuclearOption : NetworkAuthenticator<NetworkAuthenticatorNuclearOption.AuthMessage>
	{
		private struct AuthTicketResult
		{
			public bool success;

			public string failReason;

			public CSteamID m_SteamID;

			public CSteamID m_OwnerSteamID;

			public static AuthTicketResult Success(CSteamID id, CSteamID owner)
			{
				return new AuthTicketResult
				{
					success = true,
					m_SteamID = id,
					m_OwnerSteamID = owner
				};
			}

			public static AuthTicketResult Fail(string reason)
			{
				return new AuthTicketResult
				{
					success = false,
					failReason = reason
				};
			}
		}

		[NetworkMessage]
		public struct AuthMessage
		{
			public uint BuildHash;

			public PlayerType JoinAs;

			public ArraySegment<byte> SteamAuthToken;

			public string SteamName;

			public static void LogMessage(INetworkPlayer player, AuthMessage message)
			{
				string text = ((message.SteamAuthToken.Array != null) ? message.SteamAuthToken.Count.ToString() : "NULL");
				string text2 = ((message.SteamAuthToken.Array != null) ? HashArray(message.SteamAuthToken).ToString("X") : "NULL");
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"{player} - JoinAs:{message.JoinAs}, SteamName:{message.SteamName}, BuildHash:{message.BuildHash}, SteamAuthToken Length:{text} Hash:{text2}");
			}
		}

		[NetworkMessage]
		public struct PasswordChallenge
		{
			public ArraySegment<byte> Nonce;
		}

		[NetworkMessage]
		public struct PasswordResponse
		{
			public ArraySegment<byte> Response;
		}

		[NetworkMessage]
		public struct AuthFailReason
		{
			public string Reason;
		}

		[NetworkMessage]
		public struct BuildHashMismatch
		{
			public uint BuildHash;
		}

		public class Challenge
		{
			public enum Result
			{
				Pending = 0,
				Success = 1,
				Fail = 2
			}

			public readonly LobbyPassword.PasswordChallenge PasswordChallenge;

			private readonly UniTaskCompletionSource<Result> result = new UniTaskCompletionSource<Result>();

			public Challenge(LobbyPassword.PasswordChallenge passwordChallenge)
			{
				PasswordChallenge = passwordChallenge;
			}

			public async UniTask<(bool success, string failReason)> WaitForResult(CancellationToken cancellation)
			{
				var (flag, result) = await this.result.Task.AttachExternalCancellation(cancellation).SuppressCancellationThrow();
				if (flag)
				{
					return (false, "Async cancelled while password check was running");
				}
				return result switch
				{
					Result.Success => (true, null), 
					Result.Fail => (false, "Failed Password check"), 
					_ => (false, "Internal error from challenge"), 
				};
			}

			public void Check(PasswordResponse response)
			{
				bool flag = PasswordChallenge.VerifyResponse(response.Response);
				result.TrySetResult(flag ? Result.Success : Result.Fail);
			}
		}

		public class AuthData
		{
			public readonly bool UsingSteamTransport;

			public readonly CSteamID SteamID;

			public readonly CSteamID OwnerID;

			public readonly SavedPlayerData SaveData;

			public readonly PlayerType JoinAs;

			public readonly string ClientReportedSteamName;

			public bool SteamSessionOk;

			public double SteamSessionDisconnectTime;

			public const float STEAM_DISCONNECT_GRACE_TIME = 60f;

			private AuthData(PlayerType joinAs, bool usingSteamTransport, CSteamID steamID, CSteamID ownerID, string clientReportedSteamName, SavedPlayerData saveData = null)
			{
				JoinAs = joinAs;
				UsingSteamTransport = usingSteamTransport;
				SteamID = steamID;
				OwnerID = ownerID;
				SaveData = saveData ?? new SavedPlayerData();
				ClientReportedSteamName = clientReportedSteamName;
				SteamSessionOk = true;
				SteamSessionDisconnectTime = 0.0;
			}

			public static AuthData FromSteam(PlayerType joinAs, CSteamID steamID, CSteamID ownerID, string clientReportedSteamName, SavedPlayerData saveData)
			{
				return new AuthData(joinAs, usingSteamTransport: true, steamID, ownerID, clientReportedSteamName, saveData);
			}

			public static AuthData FromSteamHost(PlayerType joinAs, CSteamID steamID, string clientReportedSteamName, SavedPlayerData saveData)
			{
				return new AuthData(joinAs, usingSteamTransport: true, steamID, steamID, clientReportedSteamName, saveData);
			}

			public static AuthData FromUdp(PlayerType joinAs, string clientReportedSteamName, SavedPlayerData saveData = null)
			{
				return new AuthData(joinAs, usingSteamTransport: false, default(CSteamID), default(CSteamID), clientReportedSteamName, saveData);
			}
		}

		private static string UserBanListPathCache;

		private const int MAX_AUTH_TICKET_SIZE = 1024;

		private const string BuildHashFilePath = "build-hash.txt";

		private static uint? buildHash;

		public SteamworksSocketFactory SteamTransport;

		[SerializeField]
		private NetworkServer server;

		[SerializeField]
		private NetworkClient client;

		[SerializeField]
		private TimeoutConfig timeoutConfig;

		private TimeoutManager timeoutManager;

		public readonly AllowBanList KickList = new AllowBanList();

		public readonly AllowBanList BanList = new AllowBanList();

		public readonly AllowBanList MissionKickList = new AllowBanList();

		public readonly AllowBanList ErrorKickImmuneList = new AllowBanList();

		private readonly Dictionary<CSteamID, INetworkPlayer> steamPlayerLookup = new Dictionary<CSteamID, INetworkPlayer>();

		private LobbyPassword lobbyPassword;

		private readonly Dictionary<INetworkPlayer, Challenge> challenges = new Dictionary<INetworkPlayer, Challenge>();

		private readonly Dictionary<CSteamID, UniTaskCompletionSource<AuthTicketResult>> steamAuthTokenLookup = new Dictionary<CSteamID, UniTaskCompletionSource<AuthTicketResult>>();

		private string clientPassword;

		private HAuthTicket clientAuthTicket;

		private Callback<ValidateAuthTicketResponse_t> serverAuthCallback;

		private Callback<GetAuthSessionTicketResponse_t> clientAuthCallback;

		private UniTaskCompletionSource<GetAuthSessionTicketResponse_t> getTicketResult;

		private CancellationTokenSource clientDisconnectCancel;

		private CancellationTokenSource serverStopCancel;

		private Pool<byte[]> bufferPool = new Pool<byte[]>((Pool<byte[]> p) => new byte[1024], 1, 10);

		public static string UserBanListPath => UserBanListPathCache ?? (UserBanListPathCache = Path.Combine(Application.persistentDataPath, "banlist.txt"));

		private void Awake()
		{
			if (server.Active)
			{
				Debug.LogError("Server should not be active before NetworkAuthenticatorNuclearOption.Awake is called");
			}
			timeoutManager = new TimeoutManager(timeoutConfig);
			SteamTransport.AcceptCallback = SteamNetAcceptCallback;
			server.SetErrorRateLimitReachedCallback(ErrorRateLimitReached);
			server.SetAuthenticationFailedCallback(ServerAuthFailed);
			server.Started.AddListener(OnStartServer);
			server.Stopped.AddListener(OnStopServer);
			server.Disconnected.AddListener(OnServerDisconnected);
			client.Connected.AddListener(delegate(INetworkPlayer p)
			{
				OnClientConnected(p).Forget();
			});
			client.Disconnected.AddListener(OnClientDisconnected);
		}

		private void ErrorRateLimitReached(INetworkPlayer player)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"ErrorKick called because of errors from {player}, error flags:{(NuclearOptionPlayerErrorFlags.Names)player.ErrorFlags}");
			try
			{
				if (player.ErrorFlags.HasFlag(NuclearOptionPlayerErrorFlags.InvalidTransformSnapshot) && player.TryGetPlayer<Player>(out var player2) && player2.Aircraft != null && player2.Aircraft.TryGetComponent<AircraftNetworkTransform>(out var component))
				{
					component.DumpTelemetryHistory();
				}
				if (player.ConnectionHandle is SteamConnection { SteamID: var steamID })
				{
					if (ErrorKickImmuneList.Contains(steamID))
					{
						ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Skipping ErrorKick because {steamID} is in ErrorKickImmuneList");
						return;
					}
					if (timeoutManager.OnKickFromError(steamID, (NuclearOptionPlayerErrorFlags.Names)player.ErrorFlags))
					{
						ColorLog<NetworkAuthenticatorNuclearOption>.Info($"ErrorKick limit reached banning {steamID}");
						if (DedicatedServerManager.IsRunning)
						{
							BanPlayer(steamID, "Error Auto Ban");
						}
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			player.Disconnect();
		}

		private bool SteamNetAcceptCallback(SteamNetConnectionStatusChangedCallback_t param)
		{
			CSteamID cSteamID = new CSteamID(param.m_info.m_identityRemote.GetSteamID64());
			if (IsKickedOrBanned(cSteamID))
			{
				return false;
			}
			if (timeoutManager.HasTimeout(cSteamID))
			{
				return false;
			}
			return true;
		}

		private void OnDestroy()
		{
			DisposeServerCallbacks();
			DisposeClientCallbacks();
		}

		private void DisposeServerCallbacks()
		{
			if (serverAuthCallback != null)
			{
				serverAuthCallback.Dispose();
				serverAuthCallback = null;
			}
		}

		private void DisposeClientCallbacks()
		{
			if (clientAuthCallback != null)
			{
				clientAuthCallback.Dispose();
				clientAuthCallback = null;
			}
		}

		private void ServerAuthFailed(INetworkPlayer player, AuthenticationResult result)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"{player} failed to authenticated: {result.Reason}");
			player.Disconnect();
		}

		private async UniTask SendFailReason(INetworkPlayer player, string failReason)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Sending fail reason to {player}, reason={failReason}");
			player.Send(new AuthFailReason
			{
				Reason = failReason
			});
			await UniTask.Delay(500);
		}

		private void HandleAuthFailReason(INetworkPlayer player, AuthFailReason message)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info("Received fail reason, reason=" + message.Reason);
			GameManager.SetDisconnectReason(new DisconnectInfo(message.Reason));
		}

		private void HandleBuildHashMismatch(INetworkPlayer player, BuildHashMismatch message)
		{
			if (BuildHashDifferent(message.BuildHash))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Client ({GetBuildHash():X}) had different build hash than Server ({message.BuildHash:X})");
				FlashErrorMessageSingleton.ShowError("Build Number did not match server!", 20f);
			}
		}

		private void OnStartServer()
		{
			serverStopCancel?.Cancel();
			serverStopCancel = new CancellationTokenSource();
			LoadUserBanList();
			server.MessageHandler.RegisterHandler<PasswordResponse>(HandlePasswordResponse, allowUnauthenticated: true);
			if (server.SocketFactory == SteamTransport)
			{
				if (SteamManager.ServerInitialized)
				{
					serverAuthCallback = Callback<ValidateAuthTicketResponse_t>.CreateGameServer(ValidateAuthTicketResponse);
				}
				else
				{
					serverAuthCallback = Callback<ValidateAuthTicketResponse_t>.Create(ValidateAuthTicketResponse);
				}
			}
			CheckSteamSessionDisconnectsLoop(serverStopCancel.Token).Forget();
		}

		private void LoadUserBanList()
		{
			try
			{
				BanList.Load(UserBanListPath, allowMissing: true);
			}
			catch (Exception arg)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.LogError($"Failed to load UserBanList: {arg}");
			}
		}

		private void OnStopServer()
		{
			serverStopCancel?.Cancel();
			serverStopCancel = null;
			KickList.Clear();
			MissionKickList.Clear();
			challenges.Clear();
			steamAuthTokenLookup.Clear();
			lobbyPassword = null;
			ClearRejoinSaveData(serverStopping: true);
			DisposeServerCallbacks();
		}

		private void OnServerDisconnected(INetworkPlayer player)
		{
			if (player.ConnectionHandle is SteamConnection { SteamID: var steamID })
			{
				if (SteamManager.ServerInitialized)
				{
					SteamGameServer.EndAuthSession(steamID);
				}
				else
				{
					SteamUser.EndAuthSession(steamID);
				}
				UniTaskCompletionSource<AuthTicketResult> value;
				if (player.IsAuthenticated)
				{
					ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Ending auth session for {steamID}");
					timeoutManager.OnDisconnect(steamID);
				}
				else if (steamAuthTokenLookup.Remove(steamID, out value))
				{
					value.TrySetResult(AuthTicketResult.Fail("player disconnected before auth finished"));
				}
			}
		}

		private async UniTaskVoid OnClientConnected(INetworkPlayer player)
		{
			if (!string.IsNullOrEmpty(clientPassword))
			{
				client.MessageHandler.RegisterHandler<PasswordChallenge>(HandlePasswordChallenge, allowUnauthenticated: true);
			}
			client.MessageHandler.RegisterHandler<AuthFailReason>(HandleAuthFailReason, allowUnauthenticated: true);
			client.MessageHandler.RegisterHandler<BuildHashMismatch>(HandleBuildHashMismatch, allowUnauthenticated: true);
			clientAuthCallback = Callback<GetAuthSessionTicketResponse_t>.Create(GetAuthSessionTicketResponse);
			PlayerType joinAs = ((!DedicatedServerManager.IsRunning || !player.IsHost) ? PlayerType.Normal : PlayerType.DedicatedServer);
			byte[] buffer = bufferPool.Take();
			try
			{
				ArraySegment<byte> steamAuthToken = default(ArraySegment<byte>);
				if (player.ConnectionHandle is SteamConnection handle)
				{
					JoinLobbyOverlay.Open(JoinProgress.Joining("Getting AuthTicket"));
					clientDisconnectCancel?.Cancel();
					clientDisconnectCancel = new CancellationTokenSource();
					ArraySegment<byte>? arraySegment = await GetSteamAuthTicket(handle, buffer, clientDisconnectCancel.Token);
					if (!arraySegment.HasValue)
					{
						ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("GetSteamAuthTicket cancelled, aborting OnClientConnected task");
						player.Disconnect();
						return;
					}
					steamAuthToken = arraySegment.Value;
					if (!player.IsConnected)
					{
						ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("Client disconnected while waiting for auth ticket");
						CancelAuthTicket();
						return;
					}
				}
				if (!player.IsHost)
				{
					JoinLobbyOverlay.Open(JoinProgress.Joining("Authenticating with Server"));
				}
				AuthMessage message = new AuthMessage
				{
					BuildHash = GetBuildHash(),
					JoinAs = joinAs,
					SteamAuthToken = steamAuthToken,
					SteamName = (SteamManager.ClientInitialized ? SteamFriends.GetPersonaName() : null)
				};
				AuthMessage.LogMessage(client.Player, message);
				SendAuthentication(client, message);
			}
			finally
			{
				bufferPool.Put(buffer);
			}
		}

		private void GetAuthSessionTicketResponse(GetAuthSessionTicketResponse_t param)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"GetAuthSessionTicketResponse handle:{param.m_hAuthTicket}, result:{param.m_eResult}");
			if (param.m_hAuthTicket == clientAuthTicket)
			{
				if (getTicketResult != null)
				{
					getTicketResult.TrySetResult(param);
				}
				else
				{
					ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("No pending Ticket Result");
				}
			}
			else
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn($"GetAuthSessionTicketResponse did not match current ticket:{clientAuthTicket}");
			}
		}

		private async UniTask<ArraySegment<byte>?> GetSteamAuthTicket(SteamConnection handle, byte[] buffer, CancellationToken cancel, int attempt = 0)
		{
			if (getTicketResult != null)
			{
				getTicketResult.TrySetCanceled();
				getTicketResult = null;
			}
			if (CancelAuthTicket())
			{
				await UniTask.Delay(500);
				if (cancel.IsCancellationRequested)
				{
					return null;
				}
				if (getTicketResult != null)
				{
					Debug.LogError("New getTicketResult assigned while waiting delay after CancelAuthTicket");
					return null;
				}
			}
			getTicketResult = new UniTaskCompletionSource<GetAuthSessionTicketResponse_t>();
			SteamNetworkingIdentity pSteamNetworkingIdentity = handle.SteamNetworkingIdentity;
			uint pcbTicket;
			HAuthTicket ticket = (clientAuthTicket = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out pcbTicket, ref pSteamNetworkingIdentity));
			ArraySegment<byte> segment = new ArraySegment<byte>(buffer, 0, (int)pcbTicket);
			pSteamNetworkingIdentity.ToString(out var buf);
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"GetAuthSessionTicket handle:{clientAuthTicket}, connect-to:{buf}, size:{pcbTicket}, hash:{HashArray(segment):X}");
			(bool, (bool, GetAuthSessionTicketResponse_t)) obj = await getTicketResult.Task.AttachExternalCancellation(cancel).TimeoutWithoutException(TimeSpan.FromSeconds(5.0), DelayType.UnscaledDeltaTime).SuppressCancellationThrow();
			(bool, GetAuthSessionTicketResponse_t) item = obj.Item2;
			bool item2 = obj.Item1;
			bool item3 = item.Item1;
			GetAuthSessionTicketResponse_t item4 = item.Item2;
			getTicketResult = null;
			if (item2)
			{
				return null;
			}
			if (item3)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("Reached timeout waiting for GetAuthSessionTicketResponse");
				return segment;
			}
			if (item4.m_hAuthTicket != ticket)
			{
				Debug.LogError("Ticket number returned from task did not match, cancelling");
				return null;
			}
			if (item4.m_eResult == EResult.k_EResultOK)
			{
				return segment;
			}
			if (attempt < 3)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("Reached timeout waiting for GetAuthSessionTicketResponse");
				await UniTask.Delay(500 * (attempt + 1));
				if (cancel.IsCancellationRequested)
				{
					return null;
				}
				return await GetSteamAuthTicket(handle, buffer, cancel, attempt + 1);
			}
			ColorLog<NetworkAuthenticatorNuclearOption>.LogError("Max retries reached, aborting auth");
			return null;
		}

		private void OnApplicationQuit()
		{
			CancelAuthTicket();
		}

		private void OnClientDisconnected(ClientStoppedReason arg0)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Handling client disconnect, CancelSource:{clientDisconnectCancel != null}, HasTicket:{clientAuthTicket != HAuthTicket.Invalid}");
			clientDisconnectCancel?.Cancel();
			clientDisconnectCancel = null;
			CancelAuthTicket();
			DisposeClientCallbacks();
		}

		private bool CancelAuthTicket()
		{
			if (clientAuthTicket != HAuthTicket.Invalid)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"CancelAuthTicket handle:{clientAuthTicket}");
				SteamUser.CancelAuthTicket(clientAuthTicket);
				clientAuthTicket = HAuthTicket.Invalid;
				return true;
			}
			return false;
		}

		private bool TryGetSteamOwner(CSteamID id, out CSteamID ownerId)
		{
			if (steamPlayerLookup.TryGetValue(id, out var value))
			{
				return TryGetSteamOwner(value, out ownerId);
			}
			ownerId = default(CSteamID);
			return false;
		}

		private bool TryGetSteamOwner(INetworkPlayer conn, out CSteamID ownerId)
		{
			AuthData authData = conn.GetAuthData();
			if (authData != null && authData.OwnerID.IsValid() && authData.OwnerID != authData.SteamID)
			{
				ownerId = authData.OwnerID;
				return true;
			}
			ownerId = default(CSteamID);
			return false;
		}

		public void OnKick(INetworkPlayer conn)
		{
			if (conn.IsHost)
			{
				return;
			}
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Kicking {conn}");
			CSteamID steamId = GetSteamId(conn);
			if (steamId == CSteamID.Nil)
			{
				return;
			}
			KickList.Add(steamId, "");
			if (TryGetSteamOwner(conn, out var ownerId))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Kicking owner {ownerId} because borrower {steamId} was kicked");
				KickList.Add(ownerId, "");
				if (steamPlayerLookup.TryGetValue(ownerId, out var value))
				{
					value.Disconnect();
				}
			}
		}

		public void BanPlayer(CSteamID id, string reason, string optionalBanFile = null)
		{
			string path = optionalBanFile ?? (DedicatedServerManager.IsRunning ? DedicatedServerManager.Instance.Config.BanListPaths[0] : UserBanListPath);
			AllowBanList.BanAndAppendId(BanList, path, id, reason);
			if (TryGetSteamOwner(id, out var ownerId))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Banning owner {ownerId} because borrower {id} was banned");
				string reason2 = (string.IsNullOrEmpty(reason) ? "Associated family sharing account banned" : (reason + " (Owner of banned borrower)"));
				AllowBanList.BanAndAppendId(BanList, path, ownerId, reason2);
				if (steamPlayerLookup.TryGetValue(ownerId, out var value))
				{
					value.Disconnect();
				}
			}
		}

		public void OnMissionKick(INetworkPlayer conn)
		{
			if (!conn.IsHost)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Mission-kicking {conn}");
				CSteamID steamId = GetSteamId(conn);
				OnMissionKickInner(conn, steamId);
			}
		}

		public void OnMissionKick(CSteamID id)
		{
			if (!steamPlayerLookup.TryGetValue(id, out var value) || !value.IsHost)
			{
				OnMissionKickInner(value, id);
			}
		}

		private void OnMissionKickInner(INetworkPlayer conn, CSteamID id)
		{
			if (id == CSteamID.Nil)
			{
				return;
			}
			MissionKickList.Add(id, "");
			if (conn != null && TryGetSteamOwner(conn, out var ownerId))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Mission-kicking owner {ownerId} because borrower {id} was mission-kicked");
				MissionKickList.Add(ownerId, "");
				if (steamPlayerLookup.TryGetValue(ownerId, out var value))
				{
					value.Disconnect();
				}
			}
		}

		public void ClearMissionKickList()
		{
			MissionKickList.Clear();
		}

		protected override async UniTask<AuthenticationResult> AuthenticateAsync(INetworkPlayer player, AuthMessage message, CancellationToken cancellationToken)
		{
			AuthenticationResult result = await AuthenticateAsyncInternal(player, message, cancellationToken);
			if (result.Success)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Accepting {player}, reason:{result.Reason}");
			}
			return result;
		}

		protected async UniTask<AuthenticationResult> AuthenticateAsyncInternal(INetworkPlayer player, AuthMessage message, CancellationToken cancellationToken)
		{
			AuthMessage.LogMessage(player, message);
			if (BuildHashDifferent(message.BuildHash))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Client ({message.BuildHash:X}) had different build hash than Server ({GetBuildHash():X})");
				player.Send(new BuildHashMismatch
				{
					BuildHash = GetBuildHash()
				});
			}
			AuthenticationResult? authenticationResult = ValidateJoinAs(player, message);
			if (authenticationResult.HasValue)
			{
				return authenticationResult.Value;
			}
			if (server.SocketFactory == SteamTransport)
			{
				return await SteamAuthenticate(player, message, cancellationToken);
			}
			return await UDPAuthenticate(player, message, cancellationToken);
		}

		private AuthenticationResult? ValidateJoinAs(INetworkPlayer player, AuthMessage message)
		{
			if (message.JoinAs == PlayerType.Unknown)
			{
				return AuthenticationResult.CreateFail("Auth message did not give a player type", this);
			}
			if (message.JoinAs == PlayerType.DedicatedServer)
			{
				if (player.IsHost)
				{
					AuthData data = ((server.SocketFactory == SteamTransport) ? AuthData.FromSteamHost(message.JoinAs, GetSteamId(player), message.SteamName, null) : AuthData.FromUdp(message.JoinAs, message.SteamName));
					return AuthenticationResult.CreateSuccess("Dedicated Server", this, data);
				}
				return AuthenticationResult.CreateFail("Auth message did not give a player type", this);
			}
			if (message.JoinAs == PlayerType.Spectator)
			{
				return AuthenticationResult.CreateFail("Not Implemented", this);
			}
			return null;
		}

		private async UniTask<AuthenticationResult> SteamAuthenticate(INetworkPlayer player, AuthMessage message, CancellationToken cancellationToken)
		{
			CSteamID id = GetSteamId(player);
			if (player.IsHost)
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Accepting host {player}");
				return AuthenticationResult.CreateSuccess("Host player", this, AuthData.FromSteamHost(message.JoinAs, id, message.SteamName, null));
			}
			if (IsKickedOrBanned(player, id, out var result))
			{
				return result;
			}
			if (timeoutManager.HasTimeout(id))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"[{player}, {id}] on timed out, Rejecting connection");
				return AuthenticationResult.CreateFail("Player in timeout list", this);
			}
			if (message.SteamAuthToken.Array == null || message.SteamAuthToken.Count == 0)
			{
				return AuthenticationResult.CreateFail("No Auth token given", this);
			}
			byte[] buffer = bufferPool.Take();
			try
			{
				Buffer.BlockCopy(message.SteamAuthToken.Array, message.SteamAuthToken.Offset, buffer, 0, message.SteamAuthToken.Count);
				ArraySegment<byte> authToken = new ArraySegment<byte>(buffer, 0, message.SteamAuthToken.Count);
				if (lobbyPassword != null)
				{
					var (flag, failReason) = await RunPasswordCheck(player, cancellationToken);
					if (!flag)
					{
						ColorLog<NetworkAuthenticatorNuclearOption>.Info($"[{player}, {id}] password wrong, adding 2 second timeout");
						timeoutManager.AddCustomTimeout(id, 0, 2f);
						if (!cancellationToken.IsCancellationRequested)
						{
							await SendFailReason(player, "Password incorrect");
						}
						return AuthenticationResult.CreateFail(failReason, this);
					}
				}
				AuthTicketResult authTicketResult = await CheckSteamAuthToken(id, authToken, cancellationToken);
				if (!authTicketResult.success)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						await SendFailReason(player, "Steam Authentication failed");
					}
					return AuthenticationResult.CreateFail(authTicketResult.failReason, this);
				}
				if (authTicketResult.m_SteamID != authTicketResult.m_OwnerSteamID && IsKickedOrBanned(player, authTicketResult.m_OwnerSteamID, out var result2))
				{
					ColorLog<NetworkAuthenticatorNuclearOption>.Info("Kicking because m_OwnerSteamID is on kick or ban list");
					return result2;
				}
				if (IsOwnerAlreadyConnected(id, authTicketResult.m_OwnerSteamID))
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						await SendFailReason(player, "This Steam game license is already in use on this server.");
					}
					return AuthenticationResult.CreateFail("Steam account owner ID is already connected", this);
				}
				SavedPlayerData saveData = CheckOldSaveData(player, id);
				steamPlayerLookup[id] = player;
				return AuthenticationResult.CreateSuccess("Auth successful", this, AuthData.FromSteam(message.JoinAs, id, authTicketResult.m_OwnerSteamID, message.SteamName, saveData));
			}
			finally
			{
				bufferPool.Put(buffer);
			}
		}

		private bool IsOwnerAlreadyConnected(CSteamID requesterID, CSteamID ownerID)
		{
			foreach (INetworkPlayer value in steamPlayerLookup.Values)
			{
				if (value.IsConnected)
				{
					AuthData data = value.Authentication.GetData<AuthData>();
					if (!(data.OwnerID != ownerID) && !(data.SteamID == requesterID))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool IsKickedOrBanned(CSteamID id)
		{
			if (!KickList.Contains(id) && !MissionKickList.Contains(id))
			{
				return BanList.Contains(id);
			}
			return true;
		}

		private bool IsKickedOrBanned(INetworkPlayer player, CSteamID id, out AuthenticationResult result)
		{
			if (KickList.Contains(id))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"[{player}, {id}] on kick list, Rejecting connection");
				result = AuthenticationResult.CreateFail("Player in kick list", this);
				return true;
			}
			if (MissionKickList.Contains(id))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"[{player}, {id}] on mission kick list, Rejecting connection");
				result = AuthenticationResult.CreateFail("Player in mission kick list", this);
				return true;
			}
			if (BanList.Contains(id))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"[{player}, {id}] on ban list, Rejecting connection");
				result = AuthenticationResult.CreateFail("Player in ban list", this);
				return true;
			}
			result = default(AuthenticationResult);
			return false;
		}

		private async UniTask<AuthTicketResult> CheckSteamAuthToken(CSteamID id, ArraySegment<byte> steamAuthToken, CancellationToken cancellationToken)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Begin Auth session for {id}");
			EBeginAuthSessionResult eBeginAuthSessionResult = (SteamManager.ServerInitialized ? SteamGameServer.BeginAuthSession(steamAuthToken.Array, steamAuthToken.Count, id) : SteamUser.BeginAuthSession(steamAuthToken.Array, steamAuthToken.Count, id));
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"BeginAuthSession result={eBeginAuthSessionResult}");
			if (eBeginAuthSessionResult == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
			{
				UniTaskCompletionSource<AuthTicketResult> uniTaskCompletionSource = new UniTaskCompletionSource<AuthTicketResult>();
				steamAuthTokenLookup.Add(id, uniTaskCompletionSource);
				var (flag, result) = await uniTaskCompletionSource.Task.AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();
				if (flag)
				{
					return AuthTicketResult.Fail("Cancelled");
				}
				return result;
			}
			return AuthTicketResult.Fail($"Failed to begin auth session: {eBeginAuthSessionResult}");
		}

		private void ValidateAuthTicketResponse(ValidateAuthTicketResponse_t param)
		{
			UniTaskCompletionSource<AuthTicketResult> value;
			bool flag = steamAuthTokenLookup.Remove(param.m_SteamID, out value);
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"ValidateAuthTicketResponse_t hasPending:{flag}, id:{param.m_SteamID}, ownerId:{param.m_OwnerSteamID}, {param.m_eAuthSessionResponse}");
			INetworkPlayer value2;
			if (flag)
			{
				value.TrySetResult((param.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK) ? AuthTicketResult.Success(param.m_SteamID, param.m_OwnerSteamID) : AuthTicketResult.Fail($"Steam Auth failed: {param.m_eAuthSessionResponse}"));
			}
			else if (steamPlayerLookup.TryGetValue(param.m_SteamID, out value2))
			{
				switch (param.m_eAuthSessionResponse)
				{
				case EAuthSessionResponse.k_EAuthSessionResponseOK:
					value2.GetAuthData().SteamSessionOk = true;
					break;
				case EAuthSessionResponse.k_EAuthSessionResponseUserNotConnectedToSteam:
				{
					ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Player {param.m_SteamID} disconnected from steam waiting 60 seconds before disconnecting them");
					AuthData authData = value2.GetAuthData();
					authData.SteamSessionOk = false;
					authData.SteamSessionDisconnectTime = Time.unscaledTimeAsDouble;
					break;
				}
				default:
					ColorLog<NetworkAuthenticatorNuclearOption>.Info("Disconnecting player because auth session failed");
					if (value2.IsConnected)
					{
						value2.Disconnect();
					}
					break;
				}
			}
			else
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn($"Could not find player {param.m_SteamID} in lookup");
			}
		}

		private async UniTaskVoid CheckSteamSessionDisconnectsLoop(CancellationToken cancellation)
		{
			while (!cancellation.IsCancellationRequested)
			{
				CheckSteamSessionDisconnects();
				await UniTask.Delay(1000, ignoreTimeScale: true);
			}
		}

		private void CheckSteamSessionDisconnects()
		{
			double unscaledTimeAsDouble = Time.unscaledTimeAsDouble;
			foreach (INetworkPlayer value in steamPlayerLookup.Values)
			{
				if (!value.IsConnected)
				{
					continue;
				}
				AuthData authData = value.GetAuthData();
				if (!authData.SteamSessionOk)
				{
					double num = authData.SteamSessionDisconnectTime + 60.0;
					if (unscaledTimeAsDouble > num)
					{
						ColorLog<NetworkAuthenticatorNuclearOption>.Info("Disconnecting player because they were disconnected from steam for 60 seconds");
						value.Disconnect();
					}
				}
			}
		}

		private async UniTask<AuthenticationResult> UDPAuthenticate(INetworkPlayer player, AuthMessage message, CancellationToken cancellationToken)
		{
			if (lobbyPassword != null)
			{
				var (flag, reason) = await RunPasswordCheck(player, cancellationToken);
				if (!flag)
				{
					return AuthenticationResult.CreateFail(reason, this);
				}
			}
			return AuthenticationResult.CreateSuccess("UDP auth success", this, AuthData.FromUdp(message.JoinAs, message.SteamName));
		}

		private SavedPlayerData CheckOldSaveData(INetworkPlayer player, CSteamID id)
		{
			SavedPlayerData savedPlayerData = null;
			if (steamPlayerLookup.TryGetValue(id, out var value))
			{
				savedPlayerData = value.Authentication.GetData<AuthData>().SaveData;
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Found old save data for {player} -> (faction={savedPlayerData.Faction}, rank={savedPlayerData.Rank})");
				savedPlayerData.Rejoined = true;
				if (value.TryGetPlayer<Player>(out var player2))
				{
					savedPlayerData.Save(player2);
				}
				if (value.Connection.State == ConnectionState.Connected)
				{
					Debug.LogWarning($"New player connected with steam id {id}, disconnecting old player");
				}
				value.Disconnect();
			}
			else
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.Info($"New player {player}");
			}
			return savedPlayerData;
		}

		public static CSteamID GetSteamId(INetworkPlayer player)
		{
			if (player.IsHost)
			{
				if (SteamManager.ServerInitialized)
				{
					return GameServer.GetSteamID();
				}
				return SteamUser.GetSteamID();
			}
			if (player.ConnectionHandle is SteamConnection steamConnection)
			{
				return steamConnection.SteamID;
			}
			return default(CSteamID);
		}

		public void ClearRejoinSaveData(bool serverStopping)
		{
			if (serverStopping)
			{
				steamPlayerLookup.Clear();
				return;
			}
			KeyValuePair<CSteamID, INetworkPlayer>[] array = steamPlayerLookup.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<CSteamID, INetworkPlayer> keyValuePair = array[i];
				INetworkPlayer value = keyValuePair.Value;
				if (value.IsConnected)
				{
					value.Authentication.GetData<AuthData>().SaveData.Clear();
				}
				else
				{
					steamPlayerLookup.Remove(keyValuePair.Key);
				}
			}
			NetworkManagerNuclearOption.i.ResetPlayerIndicesOnMissionChange();
		}

		public void SetServerPassword(string lobbyPassword)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info("Enabling password on server");
			this.lobbyPassword = new LobbyPassword(lobbyPassword);
		}

		public void ClearServerPassword()
		{
			lobbyPassword = null;
		}

		public void SetClientPassword(string lobbyPassword)
		{
			clientPassword = lobbyPassword;
		}

		public void ClearClientPassword()
		{
			clientPassword = null;
		}

		private async UniTask<(bool success, string failReason)> RunPasswordCheck(INetworkPlayer player, CancellationToken cancellationToken)
		{
			if (challenges.ContainsKey(player))
			{
				return (false, "Client was already pending password challenge");
			}
			Challenge challenge = new Challenge(lobbyPassword.GenerateChallenge());
			challenges.Add(player, challenge);
			PasswordChallenge message = new PasswordChallenge
			{
				Nonce = challenge.PasswordChallenge.GetNonceBytes()
			};
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Sending PasswordChallenge to {player} nonce length: {message.Nonce.Count}");
			player.Send(message);
			return await challenge.WaitForResult(cancellationToken);
		}

		private void HandlePasswordChallenge(INetworkPlayer player, PasswordChallenge challenge)
		{
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Receiving PasswordChallenge nonce length: {challenge.Nonce.Count}");
			if (string.IsNullOrEmpty(clientPassword))
			{
				Debug.LogError("Server gave password challenge but client did has no password set");
				player.Disconnect();
				return;
			}
			byte[] array = LobbyPassword.GenerateResponse(clientPassword, challenge.Nonce);
			PasswordResponse message = new PasswordResponse
			{
				Response = array
			};
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Sending PasswordResponse response length: {message.Response.Count}");
			player.Send(message);
		}

		private void HandlePasswordResponse(INetworkPlayer player, PasswordResponse response)
		{
			if (lobbyPassword == null)
			{
				player.Disconnect();
				return;
			}
			ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Receiving PasswordResponse from {player} response length: {response.Response.Count}");
			if (!challenges.TryGetValue(player, out var value))
			{
				ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("No pending challenge for player");
				player.Disconnect();
			}
			else
			{
				challenges.Remove(player);
				value.Check(response);
			}
		}

		private static uint GetBuildHash()
		{
			if (!buildHash.HasValue)
			{
				try
				{
					if (File.Exists("build-hash.txt"))
					{
						string text = File.ReadAllText("build-hash.txt").Trim();
						if (text.Length > 8)
						{
							text = text.Substring(0, 8);
						}
						buildHash = Convert.ToUInt32(text, 16);
						ColorLog<NetworkAuthenticatorNuclearOption>.Info($"Loading BuildHash: {buildHash}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error reading build hash: " + ex.Message);
				}
				if (!buildHash.HasValue)
				{
					ColorLog<NetworkAuthenticatorNuclearOption>.InfoWarn("Failed to get BuildHash");
					buildHash = 0u;
				}
			}
			return buildHash.Value;
		}

		private static bool BuildHashDifferent(uint other)
		{
			uint num = GetBuildHash();
			if (num != 0 && other != 0)
			{
				return num != other;
			}
			return false;
		}

		private static int HashArray(ArraySegment<byte> segment)
		{
			int num = 23;
			byte[] array = segment.Array;
			int offset = segment.Offset;
			int count = segment.Count;
			for (int i = 0; i < count; i++)
			{
				num = num * 31 + array[offset + i];
			}
			return num;
		}
	}
}
