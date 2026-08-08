using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Collections;
using Mirage.Events;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Social;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking
{
	public class Player : BasePlayer
	{
		[SerializeField]
		private float[] rankThresholds = new float[6] { 0f, 5f, 15f, 30f, 60f, 120f };

		[SyncVar(initialOnly = true)]
		public bool IsHostPlayer;

		[CompilerGenerated]
		[SyncVar(hook = "OnPlayerIndexChanged")]
		private int _003CPlayerIndex_003Ek__BackingField;

		[CompilerGenerated]
		[SyncVar(hook = "OnServerTagChanged")]
		private string _003CServerTag_003Ek__BackingField;

		[CompilerGenerated]
		[SyncVar(hook = "HQChanged")]
		private NetworkBehaviorSyncvar _003CHQ_003Ek__BackingField;

		[CompilerGenerated]
		[SyncVar]
		private float _003CPlayerScore_003Ek__BackingField;

		private float scoreOffset;

		[CompilerGenerated]
		[SyncVar(hook = "RankChanged")]
		private int _003CPlayerRank_003Ek__BackingField;

		[CompilerGenerated]
		[SyncVar(hook = "AllocationChanged")]
		private float _003CAllocation_003Ek__BackingField;

		public readonly SyncList<OwnedAirframe> OwnedAirframes = new SyncList<OwnedAirframe>();

		[SyncVar]
		public OwnedAirframe? AirframeInUse;

		public float lastReserveGranted = -1000f;

		private PlayerName _playerNameCache;

		private float _lastRequestedNameTime;

		private AddLateEvent<PlayerName> _onNameResolved = new AddLateEvent<PlayerName>();

		public int Teamkills;

		private float previousContribution = -60f;

		[NonSerialized]
		private const int SYNC_VAR_COUNT = 9;

		[NonSerialized]
		private const int RPC_COUNT = 17;

		public int PlayerIndex
		{
			[CompilerGenerated]
			get
			{
				return _003CPlayerIndex_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CPlayerIndex_003Ek__BackingField = value;
			}
		}

		public string ServerTag
		{
			[CompilerGenerated]
			get
			{
				return _003CServerTag_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CServerTag_003Ek__BackingField = value;
			}
		}

		/*public FactionHQ HQ
		{
			[CompilerGenerated]
			get
			{
				return Network_003CHQ_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CHQ_003Ek__BackingField = value;
			}
		}*/

		public float PlayerScore
		{
			[CompilerGenerated]
			get
			{
				return _003CPlayerScore_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CPlayerScore_003Ek__BackingField = value;
			}
		}

		public int PlayerRank
		{
			[CompilerGenerated]
			get
			{
				return _003CPlayerRank_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CPlayerRank_003Ek__BackingField = value;
			}
		}

		public float Allocation
		{
			[CompilerGenerated]
			get
			{
				return _003CAllocation_003Ek__BackingField;
			}
			[CompilerGenerated]
			private set
			{
				Network_003CAllocation_003Ek__BackingField = value;
			}
		}

		public Aircraft Aircraft { get; private set; }

		public PilotDismounted PilotDismounted { get; private set; }

		public PersistentID? UnitID { get; private set; }

		public PlayerRef PlayerRef { get; private set; }

		public IAddLateEvent<PlayerName> OnNameResolved => _onNameResolved;

		public bool AircraftSpawnPending { get; private set; }

		public bool NetworkIsHostPlayer
		{
			get
			{
				return IsHostPlayer;
			}
			set
			{
				IsHostPlayer = value;
			}
		}

		public int Network_003CPlayerIndex_003Ek__BackingField
		{
			get
			{
				return PlayerIndex;
			}
			set
			{
				if (!SyncVarEqual(value, PlayerIndex))
				{
					int oldIndex = PlayerIndex;
					PlayerIndex = value;
					SetDirtyBit(4uL);
					if (!GetSyncVarHookGuard(4uL) && base.IsHost)
					{
						SetSyncVarHookGuard(4uL, value: true);
						OnPlayerIndexChanged(oldIndex, value);
						SetSyncVarHookGuard(4uL, value: false);
					}
				}
			}
		}

		public string Network_003CServerTag_003Ek__BackingField
		{
			get
			{
				return ServerTag;
			}
			set
			{
				if (!SyncVarEqual(value, ServerTag))
				{
					string oldTag = ServerTag;
					ServerTag = value;
					SetDirtyBit(8uL);
					if (!GetSyncVarHookGuard(8uL) && base.IsHost)
					{
						SetSyncVarHookGuard(8uL, value: true);
						OnServerTagChanged(oldTag, value);
						SetSyncVarHookGuard(8uL, value: false);
					}
				}
			}
		}

		
		/*public FactionHQ Network_003CHQ_003Ek__BackingField
		{
			get
			{
				//return (FactionHQ)HQ.Value;
			}
			set
			{
				
				if (!SyncVarEqual(value, (FactionHQ)HQ.Value))
				{
					FactionHQ factionHQ = (FactionHQ)HQ.Value;
					HQ.Value = value;
					SetDirtyBit(16uL);
					if (!GetSyncVarHookGuard(16uL) && base.IsHost)
					{
						SetSyncVarHookGuard(16uL, value: true);
						HQChanged();
						SetSyncVarHookGuard(16uL, value: false);
					}
				}
			}
		}*/

		public float Network_003CPlayerScore_003Ek__BackingField
		{
			get
			{
				return PlayerScore;
			}
			set
			{
				if (!SyncVarEqual(value, PlayerScore))
				{
					float num = PlayerScore;
					PlayerScore = value;
					SetDirtyBit(32uL);
				}
			}
		}

		public int Network_003CPlayerRank_003Ek__BackingField
		{
			get
			{
				return PlayerRank;
			}
			set
			{
				if (!SyncVarEqual(value, PlayerRank))
				{
					int _ = PlayerRank;
					PlayerRank = value;
					SetDirtyBit(64uL);
					if (!GetSyncVarHookGuard(64uL) && base.IsHost)
					{
						SetSyncVarHookGuard(64uL, value: true);
						RankChanged(_, value);
						SetSyncVarHookGuard(64uL, value: false);
					}
				}
			}
		}

		public float Network_003CAllocation_003Ek__BackingField
		{
			get
			{
				return Allocation;
			}
			set
			{
				if (!SyncVarEqual(value, Allocation))
				{
					float prev = Allocation;
					Allocation = value;
					SetDirtyBit(128uL);
					if (!GetSyncVarHookGuard(128uL) && base.IsHost)
					{
						SetSyncVarHookGuard(128uL, value: true);
						AllocationChanged(prev, value);
						SetSyncVarHookGuard(128uL, value: false);
					}
				}
			}
		}

		public OwnedAirframe? NetworkAirframeInUse
		{
			get
			{
				return AirframeInUse;
			}
			set
			{
				if (!SyncVarEqual(value, AirframeInUse))
				{
					OwnedAirframe? airframeInUse = AirframeInUse;
					AirframeInUse = value;
					SetDirtyBit(256uL);
				}
			}
		}

		public event Action<ReserveNotice> onReserveNotice;

		protected override void Awake()
		{
			base.Awake();
			base.Identity.OnStopServer.AddListener(OnStopServer);
			base.Identity.OnStartClient.AddListener(OnStartClient);
			base.Identity.OnStartLocalPlayer.AddListener(OnStartLocalPlayer);
			base.Identity.OnStopClient.AddListener(OnStopClient);
		}

		public void SetSpawnPending(bool pending)
		{
			AircraftSpawnPending = pending;
		}

		public void SetAircraft(Aircraft aircraft)
		{
			ColorLog<Player>.Info($"{this} SetAircraft id={aircraft.persistentID} {aircraft.Identity}");
			if (base.IsServer && Aircraft != null)
			{
				RemoveAircraftAuthority(Aircraft);
				Aircraft.StartEjectionSequence();
			}
			Aircraft = aircraft;
			UnitID = aircraft.persistentID;
			if (UnitRegistry.TryGetPersistentUnit(aircraft.persistentID, out var persistentUnit))
			{
				persistentUnit.player = this;
			}
			else
			{
				Debug.LogError($"Aircraft with id={aircraft.persistentID} was not found in UnitRegistry");
			}
		}

		public void RemoveAircraft(Aircraft aircraft)
		{
			Aircraft = null;
		}

		[Server]
		private void RemoveAircraftAuthority(Aircraft aircraft)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'RemoveAircraftAuthority' called when server not active");
			}
			ColorLog<Player>.Info($"{this} Removing auth from id={aircraft.persistentID} {aircraft.Identity}");
			INetworkPlayer owner = base.Identity.Owner;
			NetworkIdentity identity = aircraft.Identity;
			INetworkPlayer owner2 = identity.Owner;
			if (owner == owner2)
			{
				identity.RemoveClientAuthority();
			}
			else
			{
				Debug.LogError($"Aircraft owner was incorrect, expected:{owner} but was {owner2}");
			}
		}

		public void SetPilotDismounted(PilotDismounted pilotDismounted)
		{
			PilotDismounted = pilotDismounted;
		}

		public void RemovePilotDismounted(PilotDismounted pilotDismounted)
		{
			PilotDismounted = null;
		}

		public override string ToString()
		{
			if (base.NetId == 0)
			{
				return "Player(Unspawned)";
			}
			if (SteamManager.ClientInitialized)
			{
				return $"Player({base.Owner},{_playerNameCache?.RawSteamName},netId={base.NetId})";
			}
			return $"Player({base.Owner},netId={base.NetId})";
		}

		public void Steam_OnPersonaStateChanged()
		{
			if (_playerNameCache == null)
			{
				GetPlayerName();
			}
		}

		[Server]
		public void SetPlayerIndex(int index)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'SetPlayerIndex' called when server not active");
			}
			Network_003CPlayerIndex_003Ek__BackingField = index;
			RebuildNameCache();
		}

		private void OnPlayerIndexChanged(int oldIndex, int newIndex)
		{
			Network_003CPlayerIndex_003Ek__BackingField = newIndex;
			RebuildNameCache();
		}

		[Server]
		public void SetServerTag(string tag)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'SetServerTag' called when server not active");
			}
			Network_003CServerTag_003Ek__BackingField = tag;
			RebuildNameCache();
		}

		private void OnServerTagChanged(string oldTag, string newTag)
		{
			Network_003CServerTag_003Ek__BackingField = newTag;
			RebuildNameCache();
		}

		public void RebuildNameCache()
		{
			_playerNameCache?.RebuildCachedNames(PlayerIndex, ServerTag);
		}

		public string GetDisplayName(PlayerNameContext context)
		{
			return GetPlayerName().GetDisplayName(context);
		}

		public PlayerName GetPlayerName()
		{
			if (_playerNameCache != null)
			{
				return _playerNameCache;
			}
			if (!SteamManager.ClientInitialized)
			{
				string clientReportedSteamName = GetAuthData().ClientReportedSteamName;
				_playerNameCache = PlayerName.FallbackNoSteamId(clientReportedSteamName);
				_playerNameCache.RebuildCachedNames(PlayerIndex, ServerTag);
				return _playerNameCache;
			}
			if (!base.CSteamID.IsValid())
			{
				_playerNameCache = PlayerName.FallbackNoSteamId(null);
				_playerNameCache.RebuildCachedNames(PlayerIndex, ServerTag);
				return _playerNameCache;
			}
			if (UnitRegistry.cachedPlayerNames.TryGetValue(base.CSteamID, out var value))
			{
				ColorLog<Player>.Info($"{base.CSteamID} cached name:{value.RawSteamName}");
				_playerNameCache = value;
				_playerNameCache.RebuildCachedNames(PlayerIndex, ServerTag);
				_onNameResolved?.Invoke(_playerNameCache);
				return value;
			}
			if (TryGetNameFromSteam(base.CSteamID, out var playerName))
			{
				string input = playerName.SanitizeRichText(32);
				input = input.ReplaceCharactersNotInFont(GameAssets.i.playerNameFont);
				ColorLog<Player>.Info($"{base.CSteamID} resolved name:{playerName}");
				PlayerName playerName2 = new PlayerName(playerName, input);
				playerName2.RebuildCachedNames(PlayerIndex, ServerTag);
				UnitRegistry.cachedPlayerNames[base.CSteamID] = playerName2;
				_playerNameCache = playerName2;
				_onNameResolved?.Invoke(_playerNameCache);
				return playerName2;
			}
			if (Time.time - _lastRequestedNameTime > 5f)
			{
				_lastRequestedNameTime = Time.time;
				SteamFriends.RequestUserInformation(base.CSteamID, bRequireNameOnly: true);
			}
			PlayerName playerName3 = PlayerName.FallbackSteamID(base.CSteamID);
			playerName3.RebuildCachedNames(PlayerIndex, ServerTag);
			return playerName3;
		}

		private static bool TryGetNameFromSteam(CSteamID steamID, out string playerName)
		{
			playerName = SteamFriends.GetFriendPersonaName(steamID);
			if (!string.IsNullOrEmpty(playerName))
			{
				return playerName != "[unknown]";
			}
			return false;
		}

		public static PlayerName GetPlayerNameBySteamID(CSteamID steamID)
		{
			foreach (Player value2 in UnitRegistry.playerLookup.Values)
			{
				if (value2.CSteamID == steamID)
				{
					return value2.GetPlayerName();
				}
			}
			if (UnitRegistry.cachedPlayerNames.TryGetValue(steamID, out var value))
			{
				return value;
			}
			return PlayerName.FallbackSteamID(steamID);
		}

		public void PurchaseFuel(float fuelMass)
		{
			float num = 1f;
			Allocation -= fuelMass * num;
		}

		protected override void OnStartServer()
		{
			base.OnStartServer();
			SavedPlayerData saveData = GetAuthData().SaveData;
			if (saveData.Faction != null)
			{
				SetFaction(saveData.Faction, skipPreventJoin: true);
			}
			if (saveData.PlayerIndex > 0)
			{
				SetPlayerIndex(saveData.PlayerIndex);
			}
			if (base.CSteamID.IsValid())
			{
				VoteKickManager.RegisterConnectedPlayer(this);
			}
		}

		private void OnStopServer()
		{
			if (base.CSteamID.IsValid())
			{
				VoteKickManager.RegisterDisconnectedPlayer(this);
			}
		}

		public bool OwnsAirframe(AircraftDefinition aircraftDef, bool includeReserved)
		{
			foreach (OwnedAirframe ownedAirframe in OwnedAirframes)
			{
				if (ownedAirframe.Definition == aircraftDef && (includeReserved || !ownedAirframe.Reserved))
				{
					return true;
				}
			}
			return false;
		}

		public bool PossessesReservedAirframe(AircraftDefinition aircraftDef)
		{
			foreach (OwnedAirframe ownedAirframe in OwnedAirframes)
			{
				if (ownedAirframe.Definition == aircraftDef && ownedAirframe.Reserved)
				{
					return true;
				}
			}
			return false;
		}

		public bool PossessesReservedAirframe()
		{
			foreach (OwnedAirframe ownedAirframe in OwnedAirframes)
			{
				if (ownedAirframe.Reserved)
				{
					return true;
				}
			}
			return false;
		}

		public int OwnedAirframeTypeCount(AircraftDefinition aircraftDef, bool includeReserved)
		{
			int num = 0;
			foreach (OwnedAirframe ownedAirframe in OwnedAirframes)
			{
				if (ownedAirframe.Definition == aircraftDef && (includeReserved || !ownedAirframe.Reserved))
				{
					num++;
				}
			}
			return num;
		}

		public bool CanAffordAirframe(AircraftDefinition aircraftDef)
		{
			float num = 0f;
			foreach (OwnedAirframe ownedAirframe in OwnedAirframes)
			{
				num += ownedAirframe.Definition.value;
			}
			return Allocation + num > aircraftDef.value + 2f;
		}

		[RateLimit(Refill = 4, MaxTokens = 50, Penalty = 5)]
		[ServerRpc]
		public UniTask<ReserveNotice> CmdCheckReservingAirframe(AircraftDefinition aircraftDef)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				return UserCode_CmdCheckReservingAirframe_1562388900(aircraftDef);
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteAircraftDefinition(aircraftDef);
			UniTask<ReserveNotice> result = ServerRpcSender.SendWithReturn<ReserveNotice>(this, 0, writer, requireAuthority: true);
			writer.Release();
			return result;
		}

		[RateLimit(Refill = 2, MaxTokens = 10, Penalty = 5)]
		[ServerRpc]
		public void CmdRequestReserveAirframe(AircraftDefinition aircraftDef)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdRequestReserveAirframe_1710514814(aircraftDef);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteAircraftDefinition(aircraftDef);
			ServerRpcSender.Send(this, 1, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		[ClientRpc(target = RpcTarget.Owner)]
		public void RpcReserveNotice(ReserveNotice reserveNotice)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Owner, null, excludeOwner: false))
			{
				UserCode_RpcReserveNotice__002D1128933059(reserveNotice);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			GeneratedNetworkCode._Write_NuclearOption_002EReserveNotice(writer, reserveNotice);
			ClientRpcSender.SendTarget(this, 2, writer, Mirage.Channel.Reliable, null);
			writer.Release();
		}

		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
		[ServerRpc]
		public void CmdPurchaseAirframe(AircraftDefinition aircraftDef)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdPurchaseAirframe_787674834(aircraftDef);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteAircraftDefinition(aircraftDef);
			ServerRpcSender.Send(this, 3, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
		[ServerRpc]
		public void CmdSellAirframe(AircraftDefinition aircraftDef)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdSellAirframe_1853434019(aircraftDef);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteAircraftDefinition(aircraftDef);
			ServerRpcSender.Send(this, 4, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
		[ServerRpc]
		public void CmdReturnAirframe(AircraftDefinition aircraftDef)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdReturnAirframe__002D1503330303(aircraftDef);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteAircraftDefinition(aircraftDef);
			ServerRpcSender.Send(this, 5, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		[Server]
		public void CreditAirframe(AircraftDefinition aircraftDef, int amount, bool reserved)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'CreditAirframe' called when server not active");
			}
			if (amount == 0)
			{
				ColorLog<Player>.LogError($"{this} CreditAirframe should not be called with amount = 0");
				return;
			}
			if (amount > 0)
			{
				for (int i = 0; i < amount; i++)
				{
					OwnedAirframes.Add(new OwnedAirframe(aircraftDef, reserved));
				}
				return;
			}
			int num = 0;
			for (int num2 = OwnedAirframes.Count - 1; num2 >= 0; num2--)
			{
				if (OwnedAirframes[num2].Definition == aircraftDef)
				{
					OwnedAirframes.RemoveAt(num2);
					num++;
					if (num >= amount)
					{
						break;
					}
				}
			}
		}

		[RateLimit(Refill = 5, MaxTokens = 20, Penalty = 2)]
		[ServerRpc]
		public void CmdDonateFactionFunds(float amount)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdDonateFactionFunds__002D1612289293(amount);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteSingleConverter(amount);
			ServerRpcSender.Send(this, 6, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
		[ServerRpc]
		public void CmdPurchaseConvoy(int convoyIndex)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				UserCode_CmdPurchaseConvoy__002D772415385(convoyIndex);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WritePackedInt32(convoyIndex);
			ServerRpcSender.Send(this, 7, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		[RateLimit(Refill = 2, MaxTokens = 10, Penalty = 2)]
		[ServerRpc]
		public UniTask<float> CmdGetDelayContribute()
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				return UserCode_CmdGetDelayContribute_219629401();
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			UniTask<float> result = ServerRpcSender.SendWithReturn<float>(this, 8, writer, requireAuthority: true);
			writer.Release();
			return result;
		}

		[RateLimit(Refill = 2, MaxTokens = 10, Penalty = 2)]
		[ServerRpc]
		public UniTask<float> CmdGetDelaySpawnConvoy()
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
			{
				return UserCode_CmdGetDelaySpawnConvoy__002D1168798609();
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			UniTask<float> result = ServerRpcSender.SendWithReturn<float>(this, 9, writer, requireAuthority: true);
			writer.Release();
			return result;
		}

		[Server]
		public void AddAllocation(float amount)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'AddAllocation' called when server not active");
			}
			Allocation += amount;
		}

		[Server]
		public void SetAllocation(float newValue)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'SetAllocation' called when server not active");
			}
			Network_003CAllocation_003Ek__BackingField = newValue;
		}

		private bool NoOrSameFaction()
		{
			/*if (GameManager.GetLocalHQ(out var localHq))
			{
				return localHq == Network_003CHQ_003Ek__BackingField;
			}*/
			return true;
		}

		[ClientRpc]
		private void RpcConvoyDonationMessage(string convoyName)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
			{
				UserCode_RpcConvoyDonationMessage_1829517611(convoyName);
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteString(convoyName);
			ClientRpcSender.Send(this, 10, writer, Mirage.Channel.Reliable, excludeOwner: false);
			writer.Release();
		}

		[ClientRpc]
		private void RpcUnitDonationMessage(UnitDefinition unitDefinition, int quantity)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
			{
				UserCode_RpcUnitDonationMessage_178674173(unitDefinition, quantity);
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteUnitDefinition(unitDefinition);
			writer.WritePackedInt32(quantity);
			ClientRpcSender.Send(this, 11, writer, Mirage.Channel.Reliable, excludeOwner: false);
			writer.Release();
		}

		[ClientRpc]
		private void RpcFundsDonationMessage(float amount)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
			{
				UserCode_RpcFundsDonationMessage__002D1432044318(amount);
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteSingleConverter(amount);
			ClientRpcSender.Send(this, 12, writer, Mirage.Channel.Reliable, excludeOwner: false);
			writer.Release();
		}

		private void OnStartClient()
		{
			PlayerRef = new PlayerRef(this);
			UnitRegistry.AddPlayer(PlayerRef, this);
			if (!GameManager.IsLocalPlayer(this))
			{
				GetPlayerName();
				OnNameResolved.AddListener(ShowJoinMessage);
			}
		}

		private void ShowJoinMessage(PlayerName _)
		{
			OnNameResolved.RemoveListener(ShowJoinMessage);
			if (NetworkSceneSingleton<MessageManager>.i != null)
			{
				NetworkSceneSingleton<MessageManager>.i.JoinMessage(this);
			}
		}

		private void OnStartLocalPlayer()
		{
			GameManager.SetLocalPlayer(this);
			if (SceneSingleton<ReserveReport>.i != null)
			{
				SceneSingleton<ReserveReport>.i.Initialize(this);
			}
			if (GameManager.gameState.IsSingleOrMultiplayer())
			{
				WaitShowJoinMenu().Forget();
			}
			HQChanged();
		}

		private void OnStopClient()
		{
			if (NetworkSceneSingleton<MessageManager>.i != null)
			{
				NetworkSceneSingleton<MessageManager>.i.DisconnectedMessage(this);
			}
		}

		public void OnDestroy()
		{
			if (Aircraft != null)
			{
				Aircraft.NetworkplayerRef = PlayerRef.Invalid;
			}
			UnitRegistry.RemovePlayer(PlayerRef);
		}

		private async UniTask WaitShowJoinMenu()
		{
			CancellationToken cancel = base.destroyCancellationToken;
			await UniTask.Delay(1000);
			if (!cancel.IsCancellationRequested)
			{
				/*
				if (GameManager.GetLocalAircraft(out var _))
				{
					MusicManager.i.FadeOut(2f);
				}
				else if (Network_003CHQ_003Ek__BackingField != null)
				{
					MusicManager.i.CrossFadeMusic(NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.GetStartMusic(Network_003CHQ_003Ek__BackingField.faction), 2f, 0f, repeat: false, allowReplay: false, replacePlaying: true);
					SceneSingleton<DynamicMap>.i.SetFaction(Network_003CHQ_003Ek__BackingField);
					SceneSingleton<DynamicMap>.i.Maximize();
				}
				else
				{
					SceneSingleton<GameplayUI>.i.ShowJoinMenu();
				}*/
			}
		}

		[Server]
		public void AddScore(float score)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'AddScore' called when server not active");
			}
			SetScore(PlayerScore + score);
		}

		[Server]
		public void SetScore(float newScore)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'SetScore' called when server not active");
			}
			Network_003CPlayerScore_003Ek__BackingField = newScore;
			int num = CalculateRank(GetRankScore());
			if (num > PlayerRank)
			{
				SetRank(num, setScoreOffset: false);
			}
		}

		private float GetRankScore()
		{
			return (PlayerScore - scoreOffset) * MissionHelper.RankMultiplier;
		}

		[Server]
		public void SetRank(int newRank, bool setScoreOffset)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'SetRank' called when server not active");
			}
			newRank = Math.Clamp(newRank, 0, rankThresholds.Length - 1);
			ColorLog<Player>.Info($"{this} Player rank up old={PlayerRank} new={newRank}");
			Network_003CPlayerRank_003Ek__BackingField = newRank;
			if (setScoreOffset)
			{
				float num = rankThresholds[newRank];
				scoreOffset = PlayerScore - num;
			}
		}

		private int CalculateRank(float score)
		{
			int result = 0;
			for (int i = 0; i < rankThresholds.Length && !(rankThresholds[i] > score); i++)
			{
				result = i;
			}
			return result;
		}

		public float ScoreNeededForNextRank()
		{
			int num = PlayerRank + 1;
			if (num >= rankThresholds.Length)
			{
				return 0f;
			}
			float num2 = rankThresholds[num];
			float rankScore = GetRankScore();
			return num2 - rankScore;
		}

		private void RankChanged(int _, int __)
		{
			if (base.IsLocalPlayer && Time.timeSinceLevelLoad > 10f && !PlayerSettings.cinematicMode)
			{
				KillDisplay.FlashRank(PlayerRank);
			}
		}

		private void AllocationChanged(float prev, float current)
		{
			if (base.IsLocalPlayer && Time.timeSinceLevelLoad > 10f && !PlayerSettings.cinematicMode)
			{
				SceneSingleton<AllocationDisplay>.i.Show(this, current - prev);
			}
		}

		public void ShowMap(float delay)
		{
			UniTask.Void(async delegate
			{
				CancellationToken cancel = base.destroyCancellationToken;
				for (float timer = 0f; timer < delay; timer += Time.deltaTime)
				{
					await UniTask.Yield();
					if (cancel.IsCancellationRequested || DynamicMap.mapMaximized)
					{
						return;
					}
				}
				if (GameManager.gameResolution != GameResolution.Defeat)
				{
					SceneSingleton<DynamicMap>.i.Maximize();
				}
				else
				{
					MusicManager.i.PlayMusic(GameAssets.i.missionFailedMusic, repeat: false);
					SceneSingleton<GameplayUI>.i.PauseGame();
				}
			});
		}

		public void AttachToAircraft(Aircraft aircraft)
		{
			aircraft.pilots[0].player = this;
			aircraft.pilots[0].aircraft = aircraft;
			SceneSingleton<GameplayUI>.i.hurt.gameObject.SetActive(value: false);
			CursorManager.Refresh();
		}

		[Server]
		public void FlyOwnedAirframe(AircraftDefinition airframe)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'FlyOwnedAirframe' called when server not active");
			}
			for (int num = OwnedAirframes.Count - 1; num >= 0; num--)
			{
				if (OwnedAirframes[num].Definition == airframe)
				{
					NetworkAirframeInUse = OwnedAirframes[num];
					OwnedAirframes.RemoveAt(num);
					break;
				}
			}
		}

		[Server]
		public void RecoverAirframeInUse(AircraftDefinition airframe)
		{
			if (!base.IsServer)
			{
				throw new MethodInvocationException("[Server] function 'RecoverAirframeInUse' called when server not active");
			}
			if (AirframeInUse.HasValue)
			{
				OwnedAirframes.Add(AirframeInUse.Value);
			}
			else
			{
				OwnedAirframes.Add(new OwnedAirframe(airframe, reserved: true));
			}
			NetworkAirframeInUse = null;
		}

		public void SetFaction(FactionHQ newHQ, bool skipPreventJoin = false)
		{/*
			if ((!(newHQ == null) || !(Network_003CHQ_003Ek__BackingField == null)) && ValidateFactionChange(newHQ, skipPreventJoin))
			{
				if (base.IsServer)
				{
					ServerApplyFaction(newHQ);
				}
				else
				{
					CmdSetFaction(newHQ);
				}
				Network_003CHQ_003Ek__BackingField = newHQ;
				HQChanged();
			}*/
		}

		private void HQChanged()
		{
			/*
			if (base.IsLocalPlayer)
			{
				if (Network_003CHQ_003Ek__BackingField != null)
				{
					RichPresenceManager.SetFaction(Network_003CHQ_003Ek__BackingField.faction.factionName ?? "", PresenceActivity.Preparing);
				}
				else
				{
					RichPresenceManager.SetActivity(PresenceActivity.Spectating);
				}
				if (NetworkSceneSingleton<MissionMessages>.i != null)
				{
					NetworkSceneSingleton<MissionMessages>.i.ActiveDialogueChanged();
				}
			}*/
		}

		private bool ValidateFactionChange(FactionHQ newHQ, bool skipPreventJoin = false)
		{/*
			if (newHQ == null)
			{
				ColorLog<Player>.LogError($"{this} Setting faction, but newHQ was null.");
				return false;
			}
			if (Network_003CHQ_003Ek__BackingField == newHQ)
			{
				return false;
			}
			if (Network_003CHQ_003Ek__BackingField != null)
			{
				Debug.LogError($"Faction already set to {Network_003CHQ_003Ek__BackingField} it can't be changed to {newHQ}");
				return false;
			}
			if (MissionManager.CurrentMission == null)
			{
				Debug.LogError("No mission loaded, can't join a faction");
				return false;
			}
			if (!skipPreventJoin && newHQ.preventJoin)
			{
				Debug.LogError("Faction has Prevent Join");
				return false;
			}*/
			return true;
		}

		[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
		[ServerRpc(allowServerToCall = true)]
		private void CmdSetFaction(FactionHQ newHQ, INetworkPlayer sender = null)
		{
			if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: true))
			{
				UserCode_CmdSetFaction__002D1594139491(newHQ, base.Server.LocalPlayer);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			GeneratedNetworkCode._Write_FactionHQ(writer, newHQ);
			ServerRpcSender.Send(this, 13, writer, Mirage.Channel.Reliable, requireAuthority: true);
			writer.Release();
		}

		private void ServerApplyFaction(FactionHQ newHQ)
		{
			/*
			Network_003CHQ_003Ek__BackingField = newHQ;
			Network_003CHQ_003Ek__BackingField.AddPlayer(this);
			Network_003CHQ_003Ek__BackingField.RequestTrackingStates(this);*/
		}

		[ClientRpc(target = RpcTarget.Owner)]
		public void RpcShowSortieBonus(float score)
		{/*
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Owner, null, excludeOwner: false))
			{
				UserCode_RpcShowSortieBonus_366758595(score);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteSingleConverter(score);
			ClientRpcSender.SendTarget(this, 14, writer, Mirage.Channel.Reliable, null);
			writer.Release();*/
		}

		[ClientRpc(target = RpcTarget.Owner)]
		public void KickReason(string reason)
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Owner, null, excludeOwner: false))
			{
				UserCode_KickReason__002D1396912589(reason);
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			writer.WriteString(reason);
			ClientRpcSender.SendTarget(this, 15, writer, Mirage.Channel.Reliable, null);
			writer.Release();
		}

		[ClientRpc(target = RpcTarget.Owner)]
		public void RpcClearSpawnPending()
		{
			if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Owner, null, excludeOwner: false))
			{
				UserCode_RpcClearSpawnPending_2024023721();
				return;
			}
			PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
			ClientRpcSender.SendTarget(this, 16, writer, Mirage.Channel.Reliable, null);
			writer.Release();
		}

		public Player()
		{
			InitSyncObject(OwnedAirframes);
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
				writer.WriteBooleanExtension(IsHostPlayer);
				writer.WritePackedInt32(PlayerIndex);
				writer.WriteString(ServerTag);
				//writer.WriteNetworkBehaviorSyncVar(HQ);
				writer.WriteSingleConverter(PlayerScore);
				writer.WritePackedInt32(PlayerRank);
				writer.WriteSingleConverter(Allocation);
				GeneratedNetworkCode._Write_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E(writer, AirframeInUse);
				return true;
			}
			writer.Write((ulong)((long)syncVarDirtyBits >> 1), 8);
			if ((syncVarDirtyBits & 4L) != 0L)
			{
				writer.WritePackedInt32(PlayerIndex);
				result = true;
			}
			if ((syncVarDirtyBits & 8L) != 0L)
			{
				writer.WriteString(ServerTag);
				result = true;
			}
			if ((syncVarDirtyBits & 0x10L) != 0L)
			{
				//writer.WriteNetworkBehaviorSyncVar(HQ);
				result = true;
			}
			if ((syncVarDirtyBits & 0x20L) != 0L)
			{
				writer.WriteSingleConverter(PlayerScore);
				result = true;
			}
			if ((syncVarDirtyBits & 0x40L) != 0L)
			{
				writer.WritePackedInt32(PlayerRank);
				result = true;
			}
			if ((syncVarDirtyBits & 0x80L) != 0L)
			{
				writer.WriteSingleConverter(Allocation);
				result = true;
			}
			if ((syncVarDirtyBits & 0x100L) != 0L)
			{
				GeneratedNetworkCode._Write_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E(writer, AirframeInUse);
				result = true;
			}
			return result;
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				IsHostPlayer = reader.ReadBooleanExtension();
				int num = PlayerIndex;
				PlayerIndex = reader.ReadPackedInt32();
				string text = ServerTag;
				ServerTag = reader.ReadString();
				//FactionHQ value = (FactionHQ)HQ.Value;
				//HQ = reader.ReadNetworkBehaviourSyncVar();
				PlayerScore = reader.ReadSingleConverter();
				int num2 = PlayerRank;
				PlayerRank = reader.ReadPackedInt32();
				float num3 = Allocation;
				Allocation = reader.ReadSingleConverter();
				AirframeInUse = GeneratedNetworkCode._Read_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E(reader);
				if (!base.IsServer && !SyncVarEqual(num, PlayerIndex))
				{
					OnPlayerIndexChanged(num, PlayerIndex);
				}
				if (!base.IsServer && !SyncVarEqual(text, ServerTag))
				{
					OnServerTagChanged(text, ServerTag);
				}
				/*if (!base.IsServer && !SyncVarEqual(value, (FactionHQ)HQ.Value))
				{
					HQChanged();
				}*/
				if (!base.IsServer && !SyncVarEqual(num2, PlayerRank))
				{
					RankChanged(num2, PlayerRank);
				}
				if (!base.IsServer && !SyncVarEqual(num3, Allocation))
				{
					AllocationChanged(num3, Allocation);
				}
				return;
			}
			ulong num4 = reader.Read(8);
			SetDeserializeMask(num4, 1);
			if ((num4 & 2L) != 0L)
			{
				int num5 = PlayerIndex;
				PlayerIndex = reader.ReadPackedInt32();
				if (!base.IsServer && !SyncVarEqual(num5, PlayerIndex))
				{
					OnPlayerIndexChanged(num5, PlayerIndex);
				}
			}
			if ((num4 & 4L) != 0L)
			{
				string text2 = ServerTag;
				ServerTag = reader.ReadString();
				if (!base.IsServer && !SyncVarEqual(text2, ServerTag))
				{
					OnServerTagChanged(text2, ServerTag);
				}
			}
			if ((num4 & 8L) != 0L)
			{
				//FactionHQ value2 = (FactionHQ)HQ.Value;
				//HQ = reader.ReadNetworkBehaviourSyncVar();
				/*if (!base.IsServer && !SyncVarEqual(value2, (FactionHQ)HQ.Value))
				{
					HQChanged();
				}*/
			}
			if ((num4 & 0x10L) != 0L)
			{
				PlayerScore = reader.ReadSingleConverter();
			}
			if ((num4 & 0x20L) != 0L)
			{
				int num6 = PlayerRank;
				PlayerRank = reader.ReadPackedInt32();
				if (!base.IsServer && !SyncVarEqual(num6, PlayerRank))
				{
					RankChanged(num6, PlayerRank);
				}
			}
			if ((num4 & 0x40L) != 0L)
			{
				float num7 = Allocation;
				Allocation = reader.ReadSingleConverter();
				if (!base.IsServer && !SyncVarEqual(num7, Allocation))
				{
					AllocationChanged(num7, Allocation);
				}
			}
			if ((num4 & 0x80L) != 0L)
			{
				AirframeInUse = GeneratedNetworkCode._Read_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E(reader);
			}
		}

		public UniTask<ReserveNotice> UserCode_CmdCheckReservingAirframe_1562388900(AircraftDefinition aircraftDef)
		{
			
			ReserveNotice value = new ReserveNotice(ReserveEvent.acceptedInQueue, aircraftDef, isReserving: false, 0);
			/*
			if (aircraftDef == null)
			{
				base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.InvalidValue);
				return UniTask.FromResult(ReserveNotice.Invalid);
			}
			if (Network_003CHQ_003Ek__BackingField == null)
			{
				ColorLog<Player>.InfoWarn($"{this} called CmdCheckReservingAirframe without a faction");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.FactionViolation);
				return UniTask.FromResult(ReserveNotice.Invalid);
			}
			if (Network_003CHQ_003Ek__BackingField.IsReservingAirframe(this, aircraftDef))
			{
				value.isReserving = true;
				value.outcome = ReserveEvent.rejectedDuplicate;
			}
			if (PossessesReservedAirframe())
			{
				value.isReserving = false;
				value.outcome = ReserveEvent.rejectedPossessesReserved;
			}
			if (PlayerRank < aircraftDef.aircraftParameters.rankRequired)
			{
				value.isReserving = false;
				value.outcome = ReserveEvent.rejectedRank;
			}
			if (OwnsAirframe(aircraftDef, includeReserved: true))
			{
				value.isReserving = false;
				value.outcome = ReserveEvent.rejectedOwned;
			}*/
			return UniTask.FromResult(value);
		}

		protected static UniTask<ReserveNotice> Skeleton_CmdCheckReservingAirframe_1562388900(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			return ((Player)behaviour).UserCode_CmdCheckReservingAirframe_1562388900(reader.ReadAircraftDefinition());
		}

		public void UserCode_CmdRequestReserveAirframe_1710514814(AircraftDefinition aircraftDef)
		{
			/*
			if (aircraftDef == null)
			{
				base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.InvalidValue);
			}
			else if (Network_003CHQ_003Ek__BackingField == null)
			{
				ColorLog<Player>.InfoWarn($"{this} called CmdRequestReserveAirframe without a faction");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.FactionViolation);
			}
			else
			{
				Network_003CHQ_003Ek__BackingField.RequestReservedAirframe(this, aircraftDef);
			}*/
		}

		protected static void Skeleton_CmdRequestReserveAirframe_1710514814(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			//((Player)behaviour).UserCode_CmdRequestReserveAirframe_1710514814(reader.ReadAircraftDefinition());
		}

		public void UserCode_RpcReserveNotice__002D1128933059(ReserveNotice reserveNotice)
		{
			if (reserveNotice.outcome == ReserveEvent.granted)
			{
				lastReserveGranted = Time.timeSinceLevelLoad;
			}
			this.onReserveNotice?.Invoke(reserveNotice);
		}

		protected static void Skeleton_RpcReserveNotice__002D1128933059(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			//((Player)behaviour).UserCode_RpcReserveNotice__002D1128933059(GeneratedNetworkCode._Read_NuclearOption_002EReserveNotice(reader));
		}

		public void UserCode_CmdPurchaseAirframe_787674834(AircraftDefinition aircraftDef)
		{
			if (aircraftDef == null)
			{
				base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.InvalidValue);
				return;
			}
			ColorLog<Player>.Info($"{this} Purchasing Airframe {aircraftDef.jsonKey}");
			if (aircraftDef.value > Allocation)
			{
				ColorLog<Player>.InfoWarn($"{this} may be trying to cheat by buying an airframe they can't afford");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.InventoryCost);
			}
			else
			{
				CreditAirframe(aircraftDef, 1, reserved: false);
				Allocation -= aircraftDef.value;
			}
		}

		protected static void Skeleton_CmdPurchaseAirframe_787674834(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_CmdPurchaseAirframe_787674834(reader.ReadAircraftDefinition());
		}

		public void UserCode_CmdSellAirframe_1853434019(AircraftDefinition aircraftDef)
		{
			if (aircraftDef == null)
			{
				base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.InvalidValue);
			}
			else if (!OwnsAirframe(aircraftDef, includeReserved: false))
			{
				ColorLog<Player>.InfoWarn($"{this} may be trying to cheat by selling an airframe they don't own");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.InventoryCost);
			}
			else
			{
				CreditAirframe(aircraftDef, -1, reserved: false);
				AddAllocation(aircraftDef.value);
			}
		}

		protected static void Skeleton_CmdSellAirframe_1853434019(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_CmdSellAirframe_1853434019(reader.ReadAircraftDefinition());
		}

		public void UserCode_CmdReturnAirframe__002D1503330303(AircraftDefinition aircraftDef)
		{
			if (aircraftDef == null)
			{
				base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			}
			else if (!OwnsAirframe(aircraftDef, includeReserved: true))
			{
				ColorLog<Player>.InfoWarn($"{this} may be trying to cheat by donating an airframe they don't own");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.InventoryCost);
			}
			else
			{
				CreditAirframe(aircraftDef, -1, reserved: false);
				//Network_003CHQ_003Ek__BackingField.AddSupplyUnit(aircraftDef, 1);
			}
		}

		protected static void Skeleton_CmdReturnAirframe__002D1503330303(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_CmdReturnAirframe__002D1503330303(reader.ReadAircraftDefinition());
		}

		public void UserCode_CmdDonateFactionFunds__002D1612289293(float amount)
		{
			/*
			if (!NetworkFloatHelper.Validate(amount, logErrors: false, null))
			{
				base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			}
			else if (Network_003CHQ_003Ek__BackingField == null)
			{
				ColorLog<Player>.InfoWarn($"{this} called CmdDonateFactionFunds without a faction");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.FactionViolation);
			}
			else if (amount < 0f || Allocation < amount)
			{
				ColorLog<Player>.InfoWarn($"{this} may be trying to cheat by donating more than their allocation");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.InventoryCost);
			}
			else if (!(Time.timeSinceLevelLoad < previousContribution + 60f))
			{
				AddAllocation(0f - amount);
				Network_003CHQ_003Ek__BackingField.AddBonusFunds(amount);
				RpcFundsDonationMessage(amount);
				previousContribution = Time.timeSinceLevelLoad;
			}*/
		}

		protected static void Skeleton_CmdDonateFactionFunds__002D1612289293(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_CmdDonateFactionFunds__002D1612289293(reader.ReadSingleConverter());
		}

		public void UserCode_CmdPurchaseConvoy__002D772415385(int convoyIndex)
		{
			/*
			if (Time.timeSinceLevelLoad < previousContribution + 60f)
			{
				return;
			}
			if (Network_003CHQ_003Ek__BackingField == null)
			{
				ColorLog<Player>.InfoWarn($"{this} called CmdPurchaseConvoy without a faction");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.FactionViolation);
				return;
			}
			if (!Network_003CHQ_003Ek__BackingField.faction.TryGetConvoyGroup(convoyIndex, out var convoyGroup))
			{
				base.Owner.SetError(10, NuclearOptionPlayerErrorFlags.InvalidValue);
				return;
			}
			float cost = convoyGroup.GetCost();
			if (Allocation < cost)
			{
				ColorLog<Player>.InfoWarn($"{this} may be trying to cheat by buying a convoy they can't afford");
				base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.InventoryCost);
				return;
			}
			Network_003CHQ_003Ek__BackingField.AddConvoy(convoyGroup);
			AddAllocation(0f - cost);
			RpcConvoyDonationMessage(convoyGroup.Name);
			previousContribution = Time.timeSinceLevelLoad;*/
		}

		protected static void Skeleton_CmdPurchaseConvoy__002D772415385(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_CmdPurchaseConvoy__002D772415385(reader.ReadPackedInt32());
		}

		public UniTask<float> UserCode_CmdGetDelayContribute_219629401()
		{
			return UniTask.FromResult(Mathf.Max(60f + previousContribution - Time.timeSinceLevelLoad, 0f));
		}

		protected static UniTask<float> Skeleton_CmdGetDelayContribute_219629401(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			return ((Player)behaviour).UserCode_CmdGetDelayContribute_219629401();
		}

		public UniTask<float> UserCode_CmdGetDelaySpawnConvoy__002D1168798609()
		{
			/*
			if (Network_003CHQ_003Ek__BackingField == null)
			{
				base.Owner.SetError(5, PlayerErrorFlags.RpcNullException);
				return UniTask.FromResult(0f);
			}
			return UniTask.FromResult(Network_003CHQ_003Ek__BackingField.CmdGetDelaySpawnConvoy());*/
			return UniTask.FromResult(0f);
		}

		protected static UniTask<float> Skeleton_CmdGetDelaySpawnConvoy__002D1168798609(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			return ((Player)behaviour).UserCode_CmdGetDelaySpawnConvoy__002D1168798609();
		}

		private void UserCode_RpcConvoyDonationMessage_1829517611(string convoyName)
		{
			if (NoOrSameFaction())
			{
				SceneSingleton<GameplayUI>.i.GameMessage(GetDisplayName(PlayerNameContext.ChatOrLeaderboard) + " provisioned " + convoyName);
			}
		}

		protected static void Skeleton_RpcConvoyDonationMessage_1829517611(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_RpcConvoyDonationMessage_1829517611(reader.ReadString());
		}

		private void UserCode_RpcUnitDonationMessage_178674173(UnitDefinition unitDefinition, int quantity)
		{
			if (NoOrSameFaction())
			{
				SceneSingleton<GameplayUI>.i.GameMessage($"{GetDisplayName(PlayerNameContext.ChatOrLeaderboard)} donated {quantity} {unitDefinition.unitName} to the war effort");
			}
		}

		protected static void Skeleton_RpcUnitDonationMessage_178674173(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_RpcUnitDonationMessage_178674173(reader.ReadUnitDefinition(), reader.ReadPackedInt32());
		}

		private void UserCode_RpcFundsDonationMessage__002D1432044318(float amount)
		{
			if (NoOrSameFaction())
			{
				SceneSingleton<GameplayUI>.i.GameMessage(GetDisplayName(PlayerNameContext.ChatOrLeaderboard) + " donated " + UnitConverter.ValueReading(amount) + " to the war effort");
			}
		}

		protected static void Skeleton_RpcFundsDonationMessage__002D1432044318(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_RpcFundsDonationMessage__002D1432044318(reader.ReadSingleConverter());
		}

		private void UserCode_CmdSetFaction__002D1594139491(FactionHQ newHQ, INetworkPlayer sender)
		{
			if (!ValidateFactionChange(newHQ))
			{
				sender?.SetError(2, NuclearOptionPlayerErrorFlags.InvalidState);
			}
			else
			{
				ServerApplyFaction(newHQ);
			}
		}

		protected static void Skeleton_CmdSetFaction__002D1594139491(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_CmdSetFaction__002D1594139491(GeneratedNetworkCode._Read_FactionHQ(reader), senderConnection);
		}

		public void UserCode_RpcShowSortieBonus_366758595(float score)
		{
			if (!PlayerSettings.cinematicMode)
			{
				if (SceneSingleton<KillDisplay>.i == null)
				{
					UnityEngine.Object.Instantiate(GameAssets.i.killDisplay, SceneSingleton<GameplayUI>.i.gameplayCanvas.transform);
				}
				SceneSingleton<KillDisplay>.i.DisplayBonus(score);
			}
		}

		protected static void Skeleton_RpcShowSortieBonus_366758595(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_RpcShowSortieBonus_366758595(reader.ReadSingleConverter());
		}

		public void UserCode_KickReason__002D1396912589(string reason)
		{
			string hostSteamName = NetworkManagerNuclearOption.GetHostSteamName();
			GameManager.SetDisconnectReason(new DisconnectInfo(string.IsNullOrEmpty(reason) ? (hostSteamName + " kicked you from the game") : (hostSteamName + " kicked you from the game\nreason: " + reason)));
		}

		protected static void Skeleton_KickReason__002D1396912589(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_KickReason__002D1396912589(reader.ReadString());
		}

		public void UserCode_RpcClearSpawnPending_2024023721()
		{
			Debug.Log("Clearing spawn pending");
			SetSpawnPending(pending: false);
		}

		protected static void Skeleton_RpcClearSpawnPending_2024023721(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
		{
			((Player)behaviour).UserCode_RpcClearSpawnPending_2024023721();
		}

		protected override int GetRpcCount()
		{
			return 17;
		}

		protected override void RegisterRpc(RemoteCallCollection collection)
		{
			base.RegisterRpc(collection);
			collection.RegisterRequest(0, "NuclearOption.Networking.Player.CmdCheckReservingAirframe", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdCheckReservingAirframe_1562388900, RpcRateLimitConfig.Enabled(1f, 4, 50, 5));
			collection.Register(1, "NuclearOption.Networking.Player.CmdRequestReserveAirframe", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdRequestReserveAirframe_1710514814, RpcRateLimitConfig.Enabled(1f, 2, 10, 5));
			collection.Register(2, "NuclearOption.Networking.Player.RpcReserveNotice", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcReserveNotice__002D1128933059, RpcRateLimitConfig.Disabled());
			collection.Register(3, "NuclearOption.Networking.Player.CmdPurchaseAirframe", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdPurchaseAirframe_787674834, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
			collection.Register(4, "NuclearOption.Networking.Player.CmdSellAirframe", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSellAirframe_1853434019, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
			collection.Register(5, "NuclearOption.Networking.Player.CmdReturnAirframe", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdReturnAirframe__002D1503330303, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
			collection.Register(6, "NuclearOption.Networking.Player.CmdDonateFactionFunds", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdDonateFactionFunds__002D1612289293, RpcRateLimitConfig.Enabled(1f, 5, 20, 2));
			collection.Register(7, "NuclearOption.Networking.Player.CmdPurchaseConvoy", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdPurchaseConvoy__002D772415385, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
			collection.RegisterRequest(8, "NuclearOption.Networking.Player.CmdGetDelayContribute", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdGetDelayContribute_219629401, RpcRateLimitConfig.Enabled(1f, 2, 10, 2));
			collection.RegisterRequest(9, "NuclearOption.Networking.Player.CmdGetDelaySpawnConvoy", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdGetDelaySpawnConvoy__002D1168798609, RpcRateLimitConfig.Enabled(1f, 2, 10, 2));
			collection.Register(10, "NuclearOption.Networking.Player.RpcConvoyDonationMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcConvoyDonationMessage_1829517611, RpcRateLimitConfig.Disabled());
			collection.Register(11, "NuclearOption.Networking.Player.RpcUnitDonationMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcUnitDonationMessage_178674173, RpcRateLimitConfig.Disabled());
			collection.Register(12, "NuclearOption.Networking.Player.RpcFundsDonationMessage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcFundsDonationMessage__002D1432044318, RpcRateLimitConfig.Disabled());
			collection.Register(13, "NuclearOption.Networking.Player.CmdSetFaction", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetFaction__002D1594139491, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
			collection.Register(14, "NuclearOption.Networking.Player.RpcShowSortieBonus", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcShowSortieBonus_366758595, RpcRateLimitConfig.Disabled());
			collection.Register(15, "NuclearOption.Networking.Player.KickReason", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_KickReason__002D1396912589, RpcRateLimitConfig.Disabled());
			collection.Register(16, "NuclearOption.Networking.Player.RpcClearSpawnPending", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcClearSpawnPending_2024023721, RpcRateLimitConfig.Disabled());
		}
	}
}
