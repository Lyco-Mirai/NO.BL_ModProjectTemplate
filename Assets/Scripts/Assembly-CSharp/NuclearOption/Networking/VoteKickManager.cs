using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.DedicatedServer;
using NuclearOption.Networking.Authentication;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking
{
	public class VoteKickManager : NetworkSceneSingleton<VoteKickManager>
	{
		[CompilerGenerated]
		private VoteKickConfig _003CServerConfig_003Ek__BackingField;

		[CompilerGenerated]
		[SyncVar]
		private VoteKickConfig.ClientConfig _003CClientConfig_003Ek__BackingField;

		[CompilerGenerated]
		[SyncVar(hook = "OnVoteKickStateChanged")]
		private VoteKickState _003CState_003Ek__BackingField;

		private static readonly Dictionary<CSteamID, (Player player, float disconnectTime)> _historyConnectedPlayers = new Dictionary<CSteamID, (Player, float)>();

		private HashSet<CSteamID> _votedPlayers = new HashSet<CSteamID>();

		private Dictionary<CSteamID, float> _requestCooldown = new Dictionary<CSteamID, float>();

		private Dictionary<CSteamID, int> _kickCounts = new Dictionary<CSteamID, int>();

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 2;

		[NonSerialized]
		private const int RPC_COUNT = 2;

		public VoteKickConfig ServerConfig
		{
			[CompilerGenerated]
			[Server(error = true)]
			get
			{
				if (!base.IsServer)
				{
					throw new MethodInvocationException("[Server] function 'get_ServerConfig' called when server not active");
				}
				return _003CServerConfig_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				_003CServerConfig_003Ek__BackingField = value;
			}
		}

		public VoteKickConfig.ClientConfig ClientConfig
		{
			[CompilerGenerated]
			get
			{
				return _003CClientConfig_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CClientConfig_003Ek__BackingField = value;
			}
		}

		public VoteKickState State
		{
			[CompilerGenerated]
			get
			{
				return _003CState_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CState_003Ek__BackingField = value;
			}
		}

		public VoteKickConfig.ClientConfig Network_003CClientConfig_003Ek__BackingField
		{
			get
			{
				return ClientConfig;
			}
			set
			{
				if (!SyncVarEqual(value, ClientConfig))
				{
					VoteKickConfig.ClientConfig clientConfig = ClientConfig;
					ClientConfig = value;
					SetDirtyBit(1uL);
				}
			}
		}

		public VoteKickState Network_003CState_003Ek__BackingField
		{
			get
			{
				return State;
			}
			set
			{
				if (!SyncVarEqual(value, State))
				{
					VoteKickState voteKickState = State;
					State = value;
					SetDirtyBit(2uL);
					if (!GetSyncVarHookGuard(2uL) && base.IsHost)
					{
						SetSyncVarHookGuard(2uL, value: true);
						VoteKickManager.OnVoteKickStateChanged?.Invoke(value);
						SetSyncVarHookGuard(2uL, value: false);
					}
				}
			}
		}

		public static event Action<VoteKickState> OnVoteKickStateChanged;

		public static void RegisterConnectedPlayer(Player player)
		{
			_historyConnectedPlayers[player.CSteamID] = (player, 0f);
		}

		public static void RegisterDisconnectedPlayer(Player player)
		{
			_historyConnectedPlayers[player.CSteamID] = (player, Time.time);
		}

		protected override void Awake()
		{
			base.Awake();
			base.Identity.OnStartServer.AddListener(OnStartServer);
		}

		private void OnDestroy()
		{
			if (DedicatedServerManager.IsRunning && DedicatedServerManager.Instance != null)
			{
				DedicatedServerManager.Instance.OnConfigChanged -= OnConfigChanged;
			}
		}

		private void OnStartServer()
		{
			_historyConnectedPlayers.Clear();
			VoteKickConfig config;
			if (DedicatedServerManager.IsRunning)
			{
				config = DedicatedServerManager.Instance.Config.VoteKick;
				DedicatedServerManager.Instance.OnConfigChanged += OnConfigChanged;
			}
			else
			{
				config = VoteKickConfig.CreateDefault();
			}
			SetConfig(config);
		}

		private void OnConfigChanged(DedicatedServerConfig config)
		{
			SetConfig(config.VoteKick);
		}

		private void SetConfig(VoteKickConfig config)
		{
			ServerConfig = config;
			Network_003CClientConfig_003Ek__BackingField = config.GetClientConfig();
		}

		private void Update()
		{
			if (base.IsServer && State.Active && base.NetworkTime.Time > State.VoteEndTimeNetwork)
			{
				ResolveVoteEndTime();
			}
		}

		[ServerRpc(requireAuthority = false)]
		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 1)]
		public UniTask<VoteKickState?> RequestVoteKick(CSteamID targetID, INetworkPlayer sender = null)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
			{
				return UserCode_RequestVoteKick_1855498883(targetID, base.Server.LocalPlayer);
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			GeneratedNetworkCode._Write_Steamworks_002ECSteamID(writer, targetID);
			UniTask<VoteKickState?> result = ServerRpcSender.SendWithReturn<VoteKickState?>(this, 0, writer, requireAuthority: false);
			writer.Release();
			return result;
		}

		private VoteKickState? RequestVoteKickInternal(CSteamID targetID, INetworkPlayer sender = null)
		{
			if (!ServerConfig.Enabled)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return null;
			}
			if (State.Active)
			{
				sender.SetError(1, PlayerErrorFlags.InvalidState);
				return null;
			}
			if (base.NetworkTime.Time < State.LockoutEndTimeNetwork)
			{
				sender.SetError(10, PlayerErrorFlags.InvalidState);
				return null;
			}
			CSteamID steamId = NetworkAuthenticatorNuclearOption.GetSteamId(sender);
			if (steamId == targetID)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return null;
			}
			if (_requestCooldown.TryGetValue(steamId, out var value) && Time.time < value)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return null;
			}
			if (!_historyConnectedPlayers.TryGetValue(targetID, out (Player, float) value2))
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return null;
			}
			var (player, num) = value2;
			if ((!(player != null) || player.Owner == null || !player.Owner.IsConnected) && Time.time - num > 300f)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return null;
			}
			if (TryGetTargetPlayer(targetID, out var targetPlayer) && targetPlayer.IsHost)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return null;
			}
			_votedPlayers.Clear();
			_votedPlayers.Add(steamId);
			_votedPlayers.Add(targetID);
			_requestCooldown[steamId] = Time.time + ServerConfig.RequesterCooldown;
			(int eligibleVoters, int votesRequired) tuple2 = CalculateVoteThreshold();
			int item = tuple2.eligibleVoters;
			int item2 = tuple2.votesRequired;
			int voteID = State.VoteID + 1;
			Network_003CState_003Ek__BackingField = new VoteKickState
			{
				Active = true,
				Passed = false,
				TargetID = targetID,
				YesVotes = 1,
				NoVotes = 0,
				VotesRequired = item2,
				EligibleVoters = item,
				VoteEndTimeNetwork = base.NetworkTime.Time + (double)ServerConfig.VoteDuration,
				LockoutEndTimeNetwork = State.LockoutEndTimeNetwork,
				VoteID = voteID
			};
			return State;
		}

		private (int eligibleVoters, int votesRequired) CalculateVoteThreshold()
		{
			int count = base.Server.AuthenticatedPlayers.Count;
			count--;
			if (DedicatedServerManager.IsRunning)
			{
				count--;
			}
			int num = Mathf.Max(1, count);
			int item = Mathf.Max(ServerConfig.MinVotes, Mathf.CeilToInt((float)num * ServerConfig.PassRatio));
			return (eligibleVoters: num, votesRequired: item);
		}

		[ServerRpc(requireAuthority = false)]
		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 1)]
		public void CastVote(CSteamID targetID, bool voteYes, INetworkPlayer sender = null)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
			{
				UserCode_CastVote_2104259060(targetID, voteYes, base.Server.LocalPlayer);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			GeneratedNetworkCode._Write_Steamworks_002ECSteamID(writer, targetID);
			writer.WriteBooleanExtension(voteYes);
			ServerRpcSender.Send(this, 1, writer, Mirage.Channel.Reliable, requireAuthority: false);
			writer.Release();
		}

		private void CheckEarlyResolveVote()
		{
			if (State.YesVotes >= State.VotesRequired)
			{
				ResolveVote(passed: true);
			}
			else if (State.EligibleVoters - State.NoVotes < State.VotesRequired)
			{
				ResolveVote(passed: false);
			}
		}

		private void ResolveVoteEndTime()
		{
			bool passed = State.YesVotes >= State.VotesRequired;
			ResolveVote(passed);
		}

		private void ResolveVote(bool passed)
		{
			if (passed)
			{
				ExecuteKick(State.TargetID);
			}
			VoteKickState state = State;
			state.Active = false;
			state.Passed = passed;
			state.VoteEndTimeNetwork = 0.0;
			state.LockoutEndTimeNetwork = base.NetworkTime.Time + (double)ServerConfig.NewVoteLockout;
			Network_003CState_003Ek__BackingField = state;
		}

		private bool TryGetTargetPlayer(CSteamID id, out INetworkPlayer targetPlayer)
		{
			foreach (INetworkPlayer authenticatedPlayer in base.Server.AuthenticatedPlayers)
			{
				if (NetworkAuthenticatorNuclearOption.GetSteamId(authenticatedPlayer) == id)
				{
					targetPlayer = authenticatedPlayer;
					return true;
				}
			}
			targetPlayer = null;
			return false;
		}

		private void ExecuteKick(CSteamID id)
		{
			NetworkAuthenticatorNuclearOption authenticator = NetworkManagerNuclearOption.i.Authenticator;
			_historyConnectedPlayers.Remove(id);
			if (TryGetTargetPlayer(id, out var targetPlayer))
			{
				authenticator.OnMissionKick(targetPlayer);
				targetPlayer.Disconnect();
			}
			else
			{
				ColorLog<VoteKickManager>.InfoWarn($"Could not find player with id: {id}");
				authenticator.OnMissionKick(id);
			}
			_kickCounts.TryGetValue(id, out var value);
			value++;
			_kickCounts[id] = value;
			if (value >= ServerConfig.AutoBanThreshold)
			{
				string reason = (DedicatedServerManager.IsRunning ? "Vote-Kick Auto Ban" : ("(Vote-Kick Ban) " + SafeGetPlayerName(id)));
				authenticator.BanPlayer(id, reason);
			}
			static string SafeGetPlayerName(CSteamID steamID)
			{
				try
				{
					string text = Player.GetPlayerNameBySteamID(steamID)?.RawSteamName ?? "";
					return (text.Length > 20) ? text.Substring(0, 20) : text;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					return "";
				}
			}
		}

		private void MirageProcessed()
		{
		}

		public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
		{
			ulong syncVarDirtyBits = base.SyncVarDirtyBits;
			bool result = base.SerializeSyncVars(writer, initialize);
			if (initialize)
			{
				GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig(writer, ClientConfig);
				GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EVoteKickState(writer, State);
				return true;
			}
			writer.Write(syncVarDirtyBits, 2);
			if ((syncVarDirtyBits & 1L) != 0L)
			{
				GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig(writer, ClientConfig);
				result = true;
			}
			if ((syncVarDirtyBits & 2L) != 0L)
			{
				GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EVoteKickState(writer, State);
				result = true;
			}
			return result;
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				ClientConfig = GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig(reader);
				VoteKickState value = State;
				State = GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EVoteKickState(reader);
				if (!base.IsServer && !SyncVarEqual(value, State))
				{
					VoteKickManager.OnVoteKickStateChanged?.Invoke(State);
				}
				return;
			}
			ulong num = reader.Read(2);
			SetDeserializeMask(num, 0);
			if ((num & 1L) != 0L)
			{
				ClientConfig = GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig(reader);
			}
			if ((num & 2L) != 0L)
			{
				VoteKickState value2 = State;
				State = GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EVoteKickState(reader);
				if (!base.IsServer && !SyncVarEqual(value2, State))
				{
					VoteKickManager.OnVoteKickStateChanged?.Invoke(State);
				}
			}
		}

		public UniTask<VoteKickState?> UserCode_RequestVoteKick_1855498883(CSteamID targetID, INetworkPlayer sender)
		{
			return UniTask.FromResult(RequestVoteKickInternal(targetID, sender));
		}

		protected static UniTask<VoteKickState?> Skeleton_RequestVoteKick_1855498883(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			return ((VoteKickManager)behaviour).UserCode_RequestVoteKick_1855498883(GeneratedNetworkCode._Read_Steamworks_002ECSteamID(reader), senderConnection);
		}

		public void UserCode_CastVote_2104259060(CSteamID targetID, bool voteYes, INetworkPlayer sender)
		{
			if (!State.Active)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return;
			}
			if (State.TargetID != targetID)
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return;
			}
			CSteamID steamId = NetworkAuthenticatorNuclearOption.GetSteamId(sender);
			if (_votedPlayers.Contains(steamId))
			{
				sender.SetError(20, PlayerErrorFlags.InvalidState);
				return;
			}
			VoteKickState state = State;
			if (voteYes)
			{
				state.YesVotes++;
			}
			else
			{
				state.NoVotes++;
			}
			Network_003CState_003Ek__BackingField = state;
			_votedPlayers.Add(steamId);
			if (sender != null && sender.IsHost)
			{
				ResolveVote(voteYes);
			}
			else
			{
				CheckEarlyResolveVote();
			}
		}

		protected static void Skeleton_CastVote_2104259060(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((VoteKickManager)behaviour).UserCode_CastVote_2104259060(GeneratedNetworkCode._Read_Steamworks_002ECSteamID(reader), reader.ReadBooleanExtension(), senderConnection);
		}

		protected override int GetRpcCount()
		{
			return 2;
		}

		protected override void RegisterRpc(RemoteCallCollection collection)
		{
			base.RegisterRpc(collection);
			collection.RegisterRequest(0, "NuclearOption.Networking.VoteKickManager.RequestVoteKick", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_RequestVoteKick_1855498883, RpcRateLimitConfig.Enabled(1f, 1, 5, 1));
			collection.Register(1, "NuclearOption.Networking.VoteKickManager.CastVote", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CastVote_2104259060, RpcRateLimitConfig.Enabled(1f, 1, 5, 1));
		}
	}
}
