using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Collections;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Outcomes;
using Unity.Profiling;
using UnityEngine;

public class FactionHQ : NetworkBehaviour
{
	private struct NetworkSupplyItem
	{
		public readonly UnitDefinition unitDefinition;

		public readonly int count;

		public NetworkSupplyItem(UnitDefinition unitDefinition, int count)
		{
			this.unitDefinition = unitDefinition;
			this.count = count;
		}
	}

	private struct ReserveRequest
	{
		public AircraftDefinition aircraftDefinition;

		public float timeRequested;

		public ReserveRequest(AircraftDefinition aircraftDefinition)
		{
			this.aircraftDefinition = aircraftDefinition;
			timeRequested = Time.timeSinceLevelLoad;
		}
	}

	public struct RuntimeSupply
	{
		public readonly int Count;

		public RuntimeSupply(int count)
		{
			Count = count;
		}
	}

	public enum RewardType : byte
	{
		None = 0,
		Kill = 1,
		Recon = 2,
		Jamming = 3,
		Supply = 4,
		Refuel = 5,
		Repair = 6,
		RescuePilots = 7,
		CapturePilots = 8,
		CaptureLocation = 9
	}

	private static readonly ProfilerMarker addWarheadStockpileMarker = new ProfilerMarker("AddWarheadStockpile");

	public Faction faction;

	public MissionStatsTracker missionStatsTracker;

	public Dictionary<PersistentID, TrackingInfo> trackingDatabase = new Dictionary<PersistentID, TrackingInfo>();

	public readonly SyncList<PersistentID> factionUnits = new SyncList<PersistentID>();

	public readonly SyncList<PersistentID> factionRadarReturn = new SyncList<PersistentID>();

	public readonly SyncList<PlayerRef> factionPlayers = new SyncList<PlayerRef>();

	[ShowInInspector]
	private readonly SyncList<NetworkBehaviorSyncvar<Airbase>> airbasesUnsorted = new SyncList<NetworkBehaviorSyncvar<Airbase>>();

	private Dictionary<Unit, int> lasedUnits = new Dictionary<Unit, int>();

	[CompilerGenerated]
	[SyncVar]
	private float _003CfactionFunds_003Ek__BackingField;

	public float excessFundsThreshold;

	public float playerJoinAllowance = 20f;

	public float playerTaxRate = 0.2f;

	public float regularIncome = 10f;

	private float bonusIncome;

	public float excessFundsDistributePercent = 0.25f;

	public float killReward = 1f;

	public float airSkillMultiplier = 1f;

	public float surfaceSkillMultiplier = 1f;

	private ThreatTracker aircraftThreatTracker;

	public RearmMissionController RearmMissionController;

	[SyncVar(hook = "onPreventJoinChanged")]
	public bool preventJoin;

	[SyncVar]
	public bool preventDonation;

	[SyncVar]
	public float factionScore;

	[SyncVar]
	public List<string> restrictedWeapons = new List<string>();

	[SyncVar]
	public List<string> restrictedAircraft = new List<string>();

	public int warheadsInitial;

	public int warheadsReserve;

	public int reserveAirframes;

	public int extraReservesPerPlayer = 1;

	public int AIAircraftLimit = 6;

	public float reduceAIPerFriendlyPlayer = 1f;

	public float addAIPerEnemyPlayer = 1f;

	public readonly SyncDictionary<AircraftDefinition, RuntimeSupply> AircraftSupply = new SyncDictionary<AircraftDefinition, RuntimeSupply>();

	public readonly SyncDictionary<VehicleDefinition, RuntimeSupply> VehicleSupply = new SyncDictionary<VehicleDefinition, RuntimeSupply>();

	private Dictionary<Player, ReserveRequest> reserveRequests = new Dictionary<Player, ReserveRequest>();

	private List<UnitDefinition> unitsSpawned = new List<UnitDefinition>();

	[SerializeField]
	private List<Aircraft> activeAIAircraft = new List<Aircraft>();

	private List<Missile> activeCruiseMissiles = new List<Missile>();

	private float lastSortedTime = float.MinValue;

	private List<(VehicleDepot depot, float distance)> depotSorted = new List<(VehicleDepot, float)>();

	private List<(Airbase airbase, float distance)> airbasesSorted = new List<(Airbase, float)>();

	[SerializeField]
	private List<Radar> radars = new List<Radar>();

	[SerializeField]
	private List<FireControl> fireControls = new List<FireControl>();

	private List<ExclusionZone> exclusionZones = new List<ExclusionZone>();

	private List<GlobalPosition> activeDropZones = new List<GlobalPosition>();

	private static List<Player> playersCache = new List<Player>();

	private List<Unit> unitsNeedingRepair = new List<Unit>();

	private readonly List<(Airbase airbase, int warheads)> warheadStockpileCache = new List<(Airbase, int)>();

	private List<TrackingInfo> strategicTargets = new List<TrackingInfo>();

	private bool airbaseNeedSorting;

	private float lastConvoySpawned = -60f;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 6;

	[NonSerialized]
	private const int RPC_COUNT = 5;

	public float factionFunds
	{
		[CompilerGenerated]
		get
		{
			return _003CfactionFunds_003Ek__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			Network_003CfactionFunds_003Ek__BackingField = value;
		}
	}

	public float Network_003CfactionFunds_003Ek__BackingField
	{
		get
		{
			return factionFunds;
		}
		set
		{
			if (!SyncVarEqual(value, factionFunds))
			{
				float num = factionFunds;
				factionFunds = value;
				SetDirtyBit(1uL);
			}
		}
	}

	public bool NetworkpreventJoin
	{
		get
		{
			return preventJoin;
		}
		set
		{
			if (!SyncVarEqual(value, preventJoin))
			{
				bool flag = preventJoin;
				preventJoin = value;
				SetDirtyBit(2uL);
				if (!GetSyncVarHookGuard(2uL) && base.IsHost)
				{
					SetSyncVarHookGuard(2uL, value: true);
					this.onPreventJoinChanged?.Invoke();
					SetSyncVarHookGuard(2uL, value: false);
				}
			}
		}
	}

	public bool NetworkpreventDonation
	{
		get
		{
			return preventDonation;
		}
		set
		{
			if (!SyncVarEqual(value, preventDonation))
			{
				bool flag = preventDonation;
				preventDonation = value;
				SetDirtyBit(4uL);
			}
		}
	}

	public float NetworkfactionScore
	{
		get
		{
			return factionScore;
		}
		set
		{
			if (!SyncVarEqual(value, factionScore))
			{
				float num = factionScore;
				factionScore = value;
				SetDirtyBit(8uL);
			}
		}
	}

	public List<string> NetworkrestrictedWeapons
	{
		get
		{
			return restrictedWeapons;
		}
		set
		{
			if (!SyncVarEqual(value, restrictedWeapons))
			{
				List<string> list = restrictedWeapons;
				restrictedWeapons = value;
				SetDirtyBit(16uL);
			}
		}
	}

	public List<string> NetworkrestrictedAircraft
	{
		get
		{
			return restrictedAircraft;
		}
		set
		{
			if (!SyncVarEqual(value, restrictedAircraft))
			{
				List<string> list = restrictedAircraft;
				restrictedAircraft = value;
				SetDirtyBit(32uL);
			}
		}
	}

	public event Action onPreventJoinChanged;

	public event Action<ExclusionZone> onExclusionZone;

	public event Action onPlayerChangedFaction;

	public event Action<PersistentID> onDiscoverUnit;

	public event Action<PersistentID> onForgetUnit;

	public event Action<Unit> onRegisterUnit;

	public event Action<Unit> onRemoveUnit;

	public event Action<Airbase> onAirbaseAdded;

	public event Action<Airbase> onAirbaseRemoved;

	private void Awake()
	{
		base.Identity.OnStartClient.AddListener(OnStartClient);
	}

	public FactionHQ Value(params object[] argument){ return this; }

	public List<Player> GetPlayers(bool sortByScore)
	{
		playersCache.Clear();
		foreach (PlayerRef factionPlayer in factionPlayers)
		{
			Player player = factionPlayer.Player;
			if (player != null)
			{
				playersCache.Add(player);
			}
		}
		if (sortByScore)
		{
			playersCache.Sort(SortByScore);
		}
		return playersCache;
	}

	private static int SortByScore(Player a, Player b)
	{
		return b.PlayerScore.CompareTo(a.PlayerScore);
	}

	[Server]
	public void AddPlayer(Player player)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'AddPlayer' called when server not active");
		}
		factionPlayers.Add(new PlayerRef(player));
		SavedPlayerData saveData = player.GetAuthData().SaveData;
		if (saveData.Faction != null)
		{
			ColorLog<FactionHQ>.Info($"{player} rejoined {faction.factionName}");
			saveData.OnRejoinFaction(player);
		}
		else
		{
			ColorLog<FactionHQ>.Info($"New {player} joined {faction.factionName}");
			factionFunds -= playerJoinAllowance;
			player.AddAllocation(playerJoinAllowance);
		}
		UniTask.Void(async delegate
		{
			await UniTask.Yield();
			NetworkSceneSingleton<MessageManager>.i.RpcPlayerJoinFactionMessage(player, this);
		});
	}

	[Server]
	public void RemovePlayer(Player player)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'RemovePlayer' called when server not active");
		}
		reserveRequests.Remove(player);
		factionPlayers.Remove(new PlayerRef(player));
		if (player.Allocation > 0f)
		{
			factionFunds += player.Allocation;
		}
		player.GetAuthData().SaveData.Save(player);
	}

	[Server]
	public void AddAirbase(Airbase airbase)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'AddAirbase' called when server not active");
		}
		if (!airbasesUnsorted.Contains(airbase))
		{
			Debug.Log("Adding airbase " + airbase.name + " to " + base.gameObject.name);
			airbasesUnsorted.Add(airbase);
			airbaseNeedSorting = true;
		}
	}

	[Server]
	public void RemoveAirbase(Airbase airbase)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'RemoveAirbase' called when server not active");
		}
		Debug.Log("Removing airbase " + airbase.name + " from " + base.gameObject.name);
		airbasesUnsorted.Remove(airbase);
		airbaseNeedSorting = true;
	}

	public void AddDepot(VehicleDepot depot)
	{
		depotSorted.Add((depot, float.MaxValue));
	}

	public void AddExclusionZone(Unit weapon, GlobalPosition position, float radius)
	{
		ExclusionZone exclusionZone = new ExclusionZone(weapon, position, radius);
		RpcExclusionZone(exclusionZone);
		this.onExclusionZone?.Invoke(exclusionZone);
	}

	[ClientRpc]
	public void RpcExclusionZone(ExclusionZone exclusionZone)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcExclusionZone_1543365140(exclusionZone);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_NuclearOption_002EExclusionZone(writer, exclusionZone);
		ClientRpcSender.Send(this, 0, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private async UniTask RegisterExclusionZone(ExclusionZone exclusionZone)
	{
		if (DynamicMap.GetFactionMode(this) == FactionMode.Enemy)
		{
			return;
		}
		CancellationToken cancel = base.destroyCancellationToken;
		Unit unit;
		while (!UnitRegistry.TryGetUnit(exclusionZone.sourceId, out unit))
		{
			await UniTask.Delay(100);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		exclusionZones.Add(exclusionZone);
		SceneSingleton<DynamicMap>.i.DisplayExclusionZone(exclusionZone);
		SceneSingleton<GameplayUI>.i.GameMessage("Nuclear weapon launched, exclusion zone active!");
		this.onExclusionZone?.Invoke(exclusionZone);
		if (DynamicMap.GetFactionMode(this) == FactionMode.Friendly)
		{
			SoundManager.PlayInterfaceOneShot(GameAssets.i.radioStatic);
		}
	}

	public List<ExclusionZone> GetExclusionZones()
	{
		for (int num = exclusionZones.Count - 1; num >= 0; num--)
		{
			if (!UnitRegistry.TryGetUnit(exclusionZones[num].sourceId, out var unit) || unit == null)
			{
				exclusionZones.RemoveAt(num);
			}
		}
		return exclusionZones;
	}

	public void ModifyUnitSupply(UnitDefinition unitDefinition, int count)
	{
		if (unitDefinition is AircraftDefinition aircraftDefinition)
		{
			if (AircraftSupply.TryGetValue(aircraftDefinition, out var value))
			{
				int count2 = value.Count + count;
				AircraftSupply[aircraftDefinition] = new RuntimeSupply(count2);
			}
			else
			{
				AircraftSupply[aircraftDefinition] = new RuntimeSupply(count);
			}
			return;
		}
		if (unitDefinition is VehicleDefinition vehicleDefinition)
		{
			if (VehicleSupply.TryGetValue(vehicleDefinition, out var value2))
			{
				int count3 = value2.Count + count;
				VehicleSupply[vehicleDefinition] = new RuntimeSupply(count3);
			}
			else
			{
				VehicleSupply[vehicleDefinition] = new RuntimeSupply(count);
			}
			return;
		}
		throw new ArgumentException($"UnitDefinition for supply should either be AircraftDefinition or VehicleDefinition but was {unitDefinition?.GetType()}", "unitDefinition");
	}

	public int GetUnitSupply(UnitDefinition unitDefinition)
	{
		int num = 0;
		if (unitDefinition is AircraftDefinition key)
		{
			if (AircraftSupply.TryGetValue(key, out var value))
			{
				num += value.Count;
			}
			return num;
		}
		if (unitDefinition is VehicleDefinition key2)
		{
			if (VehicleSupply.TryGetValue(key2, out var value2))
			{
				num += value2.Count;
			}
			return num;
		}
		throw new ArgumentException($"UnitDefinition for supply should either be AircraftDefinition or VehicleDefinition but was {unitDefinition?.GetType()}", "unitDefinition");
	}

	[Server]
	public float CmdGetDelaySpawnConvoy()
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'CmdGetDelaySpawnConvoy' called when server not active");
		}
		return Mathf.Max(60f + lastConvoySpawned - Time.timeSinceLevelLoad, 0f);
	}

	[Server]
	public void AddConvoy(Faction.ConvoyGroup convoy)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'AddConvoy' called when server not active");
		}
		lastConvoySpawned = Time.timeSinceLevelLoad;
		foreach (Faction.ConvoyUnit constituent in convoy.Constituents)
		{
			AddSupplyUnit(constituent.Type, constituent.Count);
		}
	}

	[Server]
	public void AddSupplyUnit(UnitDefinition unitDefinition, int amount)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'AddSupplyUnit' called when server not active");
		}
		if (amount > 0 && unitDefinition is AircraftDefinition aircraftDefinition)
		{
			Player player = null;
			float num = -1f;
			List<Player> list = new List<Player>();
			foreach (KeyValuePair<Player, ReserveRequest> reserveRequest in reserveRequests)
			{
				Player key = reserveRequest.Key;
				AircraftDefinition aircraftDefinition2 = reserveRequest.Value.aircraftDefinition;
				if (key.CanAffordAirframe(aircraftDefinition))
				{
					list.Add(key);
					key.RpcReserveNotice(new ReserveNotice(ReserveEvent.cancelledAfford, aircraftDefinition, isReserving: false, 0));
				}
				else if (key != null && aircraftDefinition2 == aircraftDefinition && key.PlayerScore > num)
				{
					player = key;
					num = key.PlayerScore;
				}
			}
			if (player != null)
			{
				reserveRequests.Remove(player);
				player.CreditAirframe(aircraftDefinition, 1, reserved: true);
				player.RpcReserveNotice(new ReserveNotice(ReserveEvent.granted, aircraftDefinition, isReserving: false, 0));
				return;
			}
			foreach (Player item in list)
			{
				reserveRequests.Remove(item);
			}
		}
		ModifyUnitSupply(unitDefinition, amount);
	}

	public int GetWarheadStockpile()
	{
		int num = 0;
		if (airbasesUnsorted != null && airbasesUnsorted.Count > 0)
		{
			for (int i = 0; i < airbasesUnsorted.Count; i++)
			{
				Airbase value = airbasesUnsorted[i].Value;
				if (value.HasStorage())
				{
					num += value.GetWarheads();
				}
			}
		}
		return num;
	}

	public int GetWarheadAvailableForAI()
	{
		return GetWarheadStockpile() - warheadsReserve;
	}

	public void AddWarheadStockpile(int amount)
	{
		using (addWarheadStockpileMarker.Auto())
		{
			long timestamp = BenchmarkScope.GetTimestamp();
			warheadStockpileCache.Clear();
			for (int i = 0; i < airbasesUnsorted.Count; i++)
			{
				Airbase value = airbasesUnsorted[i].Value;
				if (value.HasStorage())
				{
					int item = value.GetWarheads() - value.GetStorage();
					warheadStockpileCache.Add((value, item));
				}
			}
			if (warheadStockpileCache.Count == 0)
			{
				Debug.Log($"NO STORAGE FACILITY FOUND FOR {amount} WARHEADS - QUIT");
				return;
			}
			Debug.Log($"SPLIT {amount} WARHEADS BETWEEN : {warheadStockpileCache.Count} AIRBASES");
			if (warheadStockpileCache.Count == 1)
			{
				warheadStockpileCache[0].airbase.AddWarheads(amount);
				return;
			}
			Span<int> span = stackalloc int[warheadStockpileCache.Count];
			for (int j = 0; j < warheadStockpileCache.Count; j++)
			{
				span[j] = warheadStockpileCache[j].warheads;
			}
			while (amount > 0)
			{
				int num = int.MaxValue;
				int index = 0;
				for (int k = 0; k < span.Length; k++)
				{
					int num2 = span[k];
					if (num2 < num)
					{
						num = num2;
						index = k;
					}
				}
				span[index]++;
				amount--;
			}
			for (int l = 0; l < span.Length; l++)
			{
				(Airbase airbase, int warheads) tuple = warheadStockpileCache[l];
				Airbase item2 = tuple.airbase;
				int item3 = tuple.warheads;
				int num3 = span[l] - item3;
				if (num3 > 0)
				{
					item2.AddWarheads(num3);
				}
				Debug.Log($"RESULTS : {item2} RECEIVED {num3} NEW WARHEADS, NEW TOTAL {item2.GetWarheads()} WARHEADS");
			}
			Debug.Log($"AddWarheadStockpile took {BenchmarkScope.MillisecondsSince(timestamp)}ms");
		}
	}

	public void AddFunds(float funds)
	{
		factionFunds += funds;
	}

	public void SetFunds(float newFunds)
	{
		Network_003CfactionFunds_003Ek__BackingField = newFunds;
	}

	public void AddBonusFunds(float funds)
	{
		factionFunds += funds;
		bonusIncome += funds;
	}

	public void AddScore(float score)
	{
		SetScore(factionScore + score);
	}

	public void SetScore(float score)
	{
		NetworkfactionScore = score;
		CheckEscalation();
	}

	private void CheckEscalation()
	{
		float oldScore = NetworkSceneSingleton<MissionManager>.i.currentEscalation;
		float newScore = factionScore;
		if (newScore > oldScore)
		{
			NetworkSceneSingleton<MissionManager>.i.NetworkcurrentEscalation = factionScore;
			ReportIfAbove(NetworkSceneSingleton<MissionManager>.i.tacticalThreshold, "Warning: Tactical nuclear weapons are cleared for deployment");
			ReportIfAbove(NetworkSceneSingleton<MissionManager>.i.strategicThreshold, "Warning: Strategic nuclear weapons are cleared for deployment");
		}
		void ReportIfAbove(float threshold, string message)
		{
			if (oldScore < threshold && newScore >= threshold)
			{
				NetworkSceneSingleton<MessageManager>.i.RpcAllHQMessage(message);
			}
		}
	}

	private void OnStartClient()
	{
		FactionRegistry.RegisterFaction(faction, this);
		factionUnits.OnInsert += OnUnitsAdded;
		factionPlayers.OnChange += OnPlayersChange;
		airbasesUnsorted.OnInsert += OnAirbaseAdded;
		airbasesUnsorted.OnRemove += OnAirbaseRemoved;
		for (int i = 0; i < factionUnits.Count; i++)
		{
			OnUnitsAdded(i, factionUnits[i]);
		}
		SetupAirbaseFactions();
		if (base.IsServer)
		{
			ServerSetup();
		}
	}

	private void SetupAirbaseFactions()
	{
		foreach (NetworkBehaviorSyncvar<Airbase> item in airbasesUnsorted)
		{
			Airbase value = item.Value;
			if (value != null && !value.AttachedAirbase)
			{
				value.SetFactionWithoutEvent(this);
			}
		}
		SceneSingleton<DynamicMap>.i.RefreshAirbases();
	}

	public bool ContainsAirbase(Airbase airbase)
	{
		return airbasesUnsorted.Contains(airbase);
	}

	private void ServerSetup()
	{
		MissionManager.onMissionLoad += OnMissionLoad;
		if (MissionManager.CurrentMission != null)
		{
			OnMissionLoad(MissionManager.CurrentMission);
		}
		aircraftThreatTracker = new ThreatTracker(this, 0.1f, new TypeIdentity(0f, 1f, 0f, 0f, 0f));
		this.StartSlowUpdateDelayed(5f, DeployUnits);
		this.StartSlowUpdateDelayed(30f, DistributeFunds);
		if (warheadsInitial <= 0)
		{
			return;
		}
		UniTask.Delay(5000).ContinueWith(delegate
		{
			if (this != null)
			{
				AddWarheadStockpile(warheadsInitial);
			}
		}).Forget();
	}

	private void DistributeFunds()
	{
		if (!(factionFunds > 0f))
		{
			return;
		}
		float num = 0f;
		if (factionFunds > excessFundsThreshold)
		{
			num = (factionFunds - excessFundsThreshold) * Mathf.Clamp01(excessFundsDistributePercent);
		}
		float num2 = (regularIncome + bonusIncome + num) * (float)factionPlayers.Count * 0.5f;
		if (!(num2 > 0f))
		{
			return;
		}
		if (num2 > factionFunds)
		{
			num2 = factionFunds;
		}
		float amount = num2 / (float)Mathf.Max(factionPlayers.Count, 1);
		foreach (Player player in GetPlayers(sortByScore: false))
		{
			player.AddAllocation(amount);
		}
		AddFunds(0f - num2);
		bonusIncome = 0f;
	}

	public bool IsReservingAirframe(Player player, AircraftDefinition aircraftDefinition)
	{
		if (reserveRequests.TryGetValue(player, out var value))
		{
			if (value.aircraftDefinition == aircraftDefinition)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public bool IsReservingAirframe(Player player)
	{
		if (reserveRequests.TryGetValue(player, out var _))
		{
			return true;
		}
		return false;
	}

	public void RequestReservedAirframe(Player player, AircraftDefinition aircraftDefinition)
	{
		if (player.OwnsAirframe(aircraftDefinition, includeReserved: true))
		{
			player.RpcReserveNotice(new ReserveNotice(ReserveEvent.rejectedOwned, aircraftDefinition, isReserving: false, 0));
			return;
		}
		if (player.PlayerRank < aircraftDefinition.aircraftParameters.rankRequired)
		{
			player.RpcReserveNotice(new ReserveNotice(ReserveEvent.rejectedRank, aircraftDefinition, isReserving: false, 0));
			return;
		}
		if (AircraftSupply.ContainsKey(aircraftDefinition) && AircraftSupply[aircraftDefinition].Count > 0)
		{
			AddSupplyUnit(aircraftDefinition, -1);
			player.CreditAirframe(aircraftDefinition, 1, reserved: true);
			player.RpcReserveNotice(new ReserveNotice(ReserveEvent.granted, aircraftDefinition, isReserving: false, 0));
			reserveRequests.Remove(player);
			return;
		}
		if (reserveRequests.TryGetValue(player, out var value))
		{
			if (value.aircraftDefinition == aircraftDefinition)
			{
				player.RpcReserveNotice(new ReserveNotice(ReserveEvent.rejectedDuplicate, aircraftDefinition, isReserving: true, -1));
				return;
			}
			reserveRequests[player] = new ReserveRequest(aircraftDefinition);
		}
		else
		{
			reserveRequests.Add(player, new ReserveRequest(aircraftDefinition));
		}
		int num = 1;
		foreach (KeyValuePair<Player, ReserveRequest> reserveRequest in reserveRequests)
		{
			if (reserveRequest.Value.aircraftDefinition == aircraftDefinition && reserveRequest.Key.PlayerScore > player.PlayerScore)
			{
				num++;
			}
		}
		player.RpcReserveNotice(new ReserveNotice((num == 1) ? ReserveEvent.accepted : ReserveEvent.acceptedInQueue, aircraftDefinition, isReserving: true, num));
	}

	private void DeployUnits()
	{
		if (airbaseNeedSorting || Time.timeSinceLevelLoad - lastSortedTime > 30f)
		{
			SortAirbases();
			SortDepots();
			airbaseNeedSorting = false;
			lastSortedTime = Time.timeSinceLevelLoad;
		}
		DeployAIAircraft();
		DeployVehicles();
	}

	private void DeployAIAircraft()
	{
		int count = factionPlayers.Count;
		int num = 0;
		foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
		{
			if (allHQ != this)
			{
				num += allHQ.GetPlayers(sortByScore: false).Count;
			}
		}
		float num2 = (float)AIAircraftLimit + (float)num * addAIPerEnemyPlayer - (float)count * reduceAIPerFriendlyPlayer;
		if ((float)activeAIAircraft.Count >= num2)
		{
			return;
		}
		List<AircraftDefinition> aircraft = Encyclopedia.i.aircraft;
		for (int i = 0; i < aircraft.Count; i++)
		{
			int index = UnityEngine.Random.Range(i, aircraft.Count);
			AircraftDefinition value = aircraft[i];
			aircraft[i] = aircraft[index];
			aircraft[index] = value;
		}
		int num3 = reserveAirframes + count * extraReservesPerPlayer;
		foreach (AircraftDefinition item2 in aircraft)
		{
			if (!AircraftSupply.TryGetValue(item2, out var value2) || value2.Count <= num3)
			{
				continue;
			}
			foreach (var item3 in airbasesSorted)
			{
				Airbase item = item3.airbase;
				if (!(item == null) && item.CanSpawnAircraft(item2))
				{
					Loadout loadout = null;
					float fuelLevel = item2.aircraftParameters.DefaultFuelLevel;
					StandardLoadout randomStandardLoadout = item2.aircraftParameters.GetRandomStandardLoadout(item2, this);
					if (randomStandardLoadout != null)
					{
						loadout = randomStandardLoadout.loadout;
						fuelLevel = randomStandardLoadout.FuelRatio;
					}
					int randomLiveryForFaction = item2.aircraftParameters.GetRandomLiveryForFaction(faction);
					if (item.TrySpawnAircraft(null, item2, new LiveryKey(randomLiveryForFaction), loadout, fuelLevel).Allowed)
					{
						return;
					}
				}
			}
		}
	}

	private void DeployVehicles()
	{
		for (int num = depotSorted.Count - 1; num >= 0; num--)
		{
			if (depotSorted[num].depot == null || depotSorted[num].depot.disabled)
			{
				depotSorted.RemoveAt(num);
			}
		}
		unitsSpawned.Clear();
		foreach (KeyValuePair<VehicleDefinition, RuntimeSupply> item in VehicleSupply)
		{
			if (item.Value.Count <= 0)
			{
				continue;
			}
			VehicleDefinition key = item.Key;
			foreach (var item2 in depotSorted)
			{
				if (item2.depot.TrySpawnVehicle(key))
				{
					unitsSpawned.Add(key);
					break;
				}
			}
		}
		foreach (UnitDefinition item3 in unitsSpawned)
		{
			ModifyUnitSupply(item3, -1);
		}
	}

	private void OnMissionLoad(Mission mission)
	{
		mission.GetFactionFromHq(this, out var missionFaction);
		NetworkpreventJoin = missionFaction.preventJoin;
		NetworkpreventDonation = missionFaction.preventDonation;
		Network_003CfactionFunds_003Ek__BackingField = missionFaction.startingBalance;
		excessFundsThreshold = missionFaction.startingBalance;
		playerJoinAllowance = missionFaction.playerJoinAllowance;
		playerTaxRate = missionFaction.playerTaxRate;
		regularIncome = missionFaction.regularIncome;
		excessFundsDistributePercent = missionFaction.excessFundsDistributePercent;
		killReward = missionFaction.killReward;
		airSkillMultiplier = missionFaction.airSkillMultiplier;
		surfaceSkillMultiplier = missionFaction.surfaceSkillMultiplier;
		warheadsInitial = missionFaction.startingWarheads;
		warheadsReserve = missionFaction.reserveWarheads;
		reserveAirframes = missionFaction.reserveAirframes;
		extraReservesPerPlayer = missionFaction.extraReservesPerPlayer;
		AIAircraftLimit = missionFaction.AIAircraftLimit;
		reduceAIPerFriendlyPlayer = missionFaction.reduceAIPerFriendlyPlayer;
		addAIPerEnemyPlayer = missionFaction.addAIPerEnemyPlayer;
		AircraftSupply.Clear();
		VehicleSupply.Clear();
		restrictedWeapons.Clear();
		restrictedAircraft.Clear();
		foreach (UnitCount supply in missionFaction.supplies)
		{
			if (Encyclopedia.Lookup.TryGetValue(supply.UnitType, out var value))
			{
				ModifyUnitSupply(value, supply.Count);
			}
		}
		foreach (string weapon in missionFaction.restrictions.weapons)
		{
			restrictedWeapons.Add(weapon);
		}
		foreach (string item in missionFaction.restrictions.aircraft)
		{
			restrictedAircraft.Add(item);
		}
	}

	private void OnDestroy()
	{
		MissionManager.onMissionLoad -= OnMissionLoad;
	}

	[Server]
	public void RequestTrackingStates(Player requestingPlayer)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'RequestTrackingStates' called when server not active");
		}
		if (trackingDatabase.Count == 0)
		{
			return;
		}
		using PooledNetworkWriter pooledNetworkWriter = NetworkWriterPool.GetWriter();
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in trackingDatabase)
		{
			if (UnitRegistry.TryGetUnit(item.Key, out var _))
			{
				TrackingInfo value = item.Value;
				pooledNetworkWriter.Write(value.id);
				pooledNetworkWriter.Write(value.lastKnownPosition);
				pooledNetworkWriter.Write(value.lastSpottedTime);
			}
		}
		INetworkPlayer owner = requestingPlayer.Owner;
		RpcGetTrackingStateBatched(owner, pooledNetworkWriter.ToArraySegment());
	}

	[ClientRpc(target = RpcTarget.Player)]
	private void RpcGetTrackingStateBatched(INetworkPlayer _, ArraySegment<byte> segment)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Player, _, excludeOwner: false))
		{
			UserCode_RpcGetTrackingStateBatched__002D1045112216(base.Client.Player, segment);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBytesAndSizeSegment(segment);
		ClientRpcSender.SendTarget(this, 1, writer, Mirage.Channel.Reliable, _);
		writer.Release();
	}

	private void SetTrackingState(PersistentID id, GlobalPosition lastKnownPosition, float lastSpottedTime)
	{
		if (UnitRegistry.TryGetUnit(id, out var unit) && !trackingDatabase.ContainsKey(id))
		{
			trackingDatabase.Add(id, new TrackingInfo(id, lastKnownPosition, lastSpottedTime));
			unit.onDisableUnit += DeregisterTrackedUnit;
			unit.onChangeFaction += HQ_OnUnitChangeFaction;
			if (DynamicMap.IsFactionMode(this, FactionMode.Spectator | FactionMode.Friendly))
			{
				SceneSingleton<DynamicMap>.i.AddIcon(id);
			}
		}
	}

	public void UpdateLasedState(Unit unit, bool lased)
	{
		if (lasedUnits.TryGetValue(unit, out var value))
		{
			value += (lased ? 1 : (-1));
			lasedUnits[unit] = value;
			if (value <= 0)
			{
				lasedUnits.Remove(unit);
			}
		}
		else if (lased)
		{
			lasedUnits.Add(unit, 1);
		}
	}

	public bool IsTargetLased(Unit target)
	{
		int value;
		return lasedUnits.TryGetValue(target, out value);
	}

	public bool TryGetLasedTargetInView(Transform fromTransform, float fov, float maxRange, out Unit lasedTarget)
	{
		lasedTarget = null;
		foreach (KeyValuePair<Unit, int> lasedUnit in lasedUnits)
		{
			Unit key = lasedUnit.Key;
			Vector3 to = key.transform.position - fromTransform.position;
			float num = Vector3.Angle(fromTransform.forward, to);
			if (num < fov && FastMath.InRange(key.transform.position, fromTransform.position, maxRange) && key.LineOfSight(fromTransform.position, 1000f))
			{
				lasedTarget = key;
				fov = num;
			}
		}
		return lasedTarget != null;
	}

	public void RegisterFactionUnit(Unit unit)
	{
		if (!factionUnits.Contains(unit.persistentID))
		{
			factionUnits.Add(unit.persistentID);
		}
		if (unit is Aircraft aircraft && aircraft.Player == null)
		{
			activeAIAircraft.Add(aircraft);
		}
		if (unit is IRadarReturn)
		{
			factionRadarReturn.Add(unit.persistentID);
		}
		this.onRegisterUnit?.Invoke(unit);
	}

	public void RemoveFactionUnit(Unit unit)
	{
		factionUnits.Remove(unit.persistentID);
		if (unit is Aircraft item)
		{
			activeAIAircraft.Remove(item);
		}
		if (unit is IRadarReturn)
		{
			factionRadarReturn.Remove(unit.persistentID);
		}
		this.onRemoveUnit?.Invoke(unit);
	}

	public IEnumerable<Aircraft> GetActiveAircraft(bool playersOnly)
	{
		foreach (Aircraft item in UnitRegistry.allAircraft)
		{
			if (item.NetworkHQ == this && (!playersOnly || item.Player != null))
			{
				yield return item;
			}
		}
	}

	public void RegisterCruiseMissile(Missile cruiseMissile)
	{
		activeCruiseMissiles.Add(cruiseMissile);
	}

	public void DeregisterCruiseMissile(Missile cruiseMissile)
	{
		activeCruiseMissiles.Remove(cruiseMissile);
	}

	public void RegisterRadar(Radar radar)
	{
		radars.Add(radar);
	}

	public void DeregisterRadar(Radar radar)
	{
		radars.Remove(radar);
	}

	public bool TryGetRadar(GlobalPosition searchPosition, float maxRange, out Radar nearestRadar)
	{
		nearestRadar = null;
		float range = maxRange;
		foreach (Radar radar in radars)
		{
			if (FastMath.InRange(searchPosition, radar.transform.GlobalPosition(), range))
			{
				nearestRadar = radar;
				range = FastMath.Distance(searchPosition, radar.transform.GlobalPosition());
			}
		}
		return nearestRadar != null;
	}

	public void RegisterFireControl(FireControl fireControl)
	{
		fireControls.Add(fireControl);
	}

	public void DeregisterFireControl(FireControl fireControl)
	{
		fireControls.Remove(fireControl);
	}

	public bool TryGetFireControl(GlobalPosition searchPosition, float maxRange, out FireControl nearestFireControl)
	{
		nearestFireControl = null;
		float range = maxRange;
		foreach (FireControl fireControl in fireControls)
		{
			if (FastMath.InRange(searchPosition, fireControl.transform.GlobalPosition(), range))
			{
				nearestFireControl = fireControl;
				range = FastMath.Distance(searchPosition, fireControl.transform.GlobalPosition());
			}
		}
		return nearestFireControl != null;
	}

	public List<Missile> GetCruiseMissiles()
	{
		return activeCruiseMissiles;
	}

	private void OnUnitsAdded(int index, PersistentID value)
	{
		DisplayFactionUnit(value);
		if (NetworkManagerNuclearOption.i.Server.Active && UnitRegistry.TryGetUnit(value, out var unit))
		{
			this.onRegisterUnit?.Invoke(unit);
		}
	}

	private void OnAirbaseAdded(int index, NetworkBehaviorSyncvar<Airbase> value)
	{
		Airbase value2 = value.Value;
		value2.SetFactionWithoutEvent(this);
		SceneSingleton<DynamicMap>.i.RefreshAirbases();
		if (Time.timeSinceLevelLoad > 10f)
		{
			SoundManager.PlayInterfaceOneShot(GameAssets.i.radioStatic);
			if (Time.timeSinceLevelLoad > 10f && !value2.AttachedAirbase)
			{
				string displayName = value2.SavedAirbase.DisplayName;
				SceneSingleton<GameplayUI>.i.GameMessage(displayName + " has been captured by " + faction.factionExtendedName);
			}
		}
		this.onAirbaseAdded?.Invoke(value2);
	}

	private void OnAirbaseRemoved(int index, NetworkBehaviorSyncvar<Airbase> value)
	{
		SceneSingleton<DynamicMap>.i.RefreshAirbases();
		this.onAirbaseRemoved?.Invoke(value.Value);
	}

	private void OnPlayersChange()
	{
		this.onPlayerChangedFaction?.Invoke();
	}

	[RateLimit(Refill = 20, MaxTokens = 40, Penalty = 2)]
	[ServerRpc(requireAuthority = false)]
	public void CmdUpdateTrackingInfo(PersistentID id)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			UserCode_CmdUpdateTrackingInfo__002D1717917610(id);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, id);
		ServerRpcSender.Send(this, 2, writer, Mirage.Channel.Reliable, requireAuthority: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcUpdateTrackingInfo(PersistentID id)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcUpdateTrackingInfo__002D640322303(id);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, id);
		ClientRpcSender.Send(this, 3, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public bool TryGetStrategicTargets(List<TrackingInfo> targetList, int ammo, float range, Unit requestingUnit)
	{
		GlobalPosition a = requestingUnit.GlobalPosition();
		if (strategicTargets.Count == 0)
		{
			return false;
		}
		targetList.Clear();
		for (int i = 0; i < strategicTargets.Count && i < ammo; i++)
		{
			if (!FastMath.OutOfRange(a, strategicTargets[i].lastKnownPosition, range))
			{
				targetList.Add(strategicTargets[i]);
			}
		}
		return targetList.Count > 0;
	}

	private void HQ_OnUnitChangeFaction(Unit unit)
	{
		if (unit.NetworkHQ == this)
		{
			this.onForgetUnit?.Invoke(unit.persistentID);
			trackingDatabase.Remove(unit.persistentID);
			TrackingInfo trackingData = GetTrackingData(unit.persistentID);
			if (trackingData != null && strategicTargets.Contains(trackingData))
			{
				strategicTargets.Remove(trackingData);
			}
		}
	}

	public void DeclareEndGame(EndType endType)
	{
		RpcDeclareEndGame(endType);
	}

	[ClientRpc]
	public void RpcDeclareEndGame(EndType endType)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcDeclareEndGame__002D1539254804(endType);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_NuclearOption_002ESavedMission_002EOutcomes_002EEndType(writer, endType);
		ClientRpcSender.Send(this, 4, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void DisplayFactionUnit(PersistentID id)
	{
		if (DynamicMap.IsFactionMode(this, FactionMode.Spectator | FactionMode.Friendly))
		{
			SceneSingleton<DynamicMap>.i.AddIcon(id);
		}
	}

	public TrackingInfo GetTrackingData(PersistentID id)
	{
		if (!trackingDatabase.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public bool IsStrategicTarget(TrackingInfo trackingInfo)
	{
		return strategicTargets.Contains(trackingInfo);
	}

	public List<Unit> GetTargetsWithinCone(List<Unit> targetList, Transform fromTransform, FiringCone[] firingCones, bool requireLineOfSight)
	{
		targetList.Clear();
		GlobalPosition globalPosition = fromTransform.GlobalPosition();
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in trackingDatabase)
		{
			TrackingInfo value = item.Value;
			GlobalPosition position = value.GetPosition();
			if (FiringConeChecker.VectorWithinFiringCones(firingCones, position - globalPosition, out var _) && value.TryGetUnit(out var unit) && IsTargetPositionAccurate(unit, 10f) && (!requireLineOfSight || unit.LineOfSight(fromTransform.position, 1000f)))
			{
				targetList.Add(unit);
			}
		}
		return targetList;
	}

	public List<TrackingInfo> GetTargetsWithinRange(List<TrackingInfo> targetList, Transform fromTransform, float range, bool requireLineOfSight)
	{
		targetList.Clear();
		GlobalPosition a = fromTransform.GlobalPosition();
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in trackingDatabase)
		{
			TrackingInfo value = item.Value;
			if (value.TryGetUnit(out var unit) && IsTargetPositionAccurate(unit, 100f) && !(unit.NetworkHQ == null) && !(unit.NetworkHQ == this) && FastMath.InRange(a, unit.GlobalPosition(), range) && (!requireLineOfSight || unit.LineOfSight(fromTransform.position, 1000f)))
			{
				targetList.Add(value);
			}
		}
		return targetList;
	}

	public bool TryGetNearestGroundEnemy(GlobalPosition from, out TrackingInfo nearestUnit)
	{
		nearestUnit = null;
		float num = float.MaxValue;
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in trackingDatabase)
		{
			TrackingInfo value = item.Value;
			if (value.TryGetUnit(out var unit) && (unit is GroundVehicle || unit is Building))
			{
				float num2 = FastMath.SquareDistance(value.lastKnownPosition, from);
				if (num2 < num && unit.NetworkHQ != null)
				{
					nearestUnit = value;
					num = num2;
				}
			}
		}
		return nearestUnit != null;
	}

	public bool TryGetKnownPosition(Unit unit, out GlobalPosition knownPosition)
	{
		GlobalPosition? knownPosition2 = GetKnownPosition(unit);
		knownPosition = knownPosition2.GetValueOrDefault();
		return knownPosition2.HasValue;
	}

	public GlobalPosition? GetKnownPosition(Unit unit)
	{
		if (unit.NetworkHQ == this)
		{
			return unit.GlobalPosition();
		}
		if (trackingDatabase.TryGetValue(unit.persistentID, out var value))
		{
			return value.GetPosition();
		}
		return null;
	}

	public void DeregisterTrackedUnit(Unit unit)
	{
		PersistentID persistentID = unit.persistentID;
		if (trackingDatabase.ContainsKey(persistentID))
		{
			this.onForgetUnit?.Invoke(persistentID);
			TrackingInfo item = trackingDatabase[persistentID];
			if (strategicTargets.Contains(item))
			{
				strategicTargets.Remove(item);
			}
			trackingDatabase.Remove(persistentID);
		}
	}

	public bool IsTargetBeingTracked(Unit target)
	{
		PersistentID persistentID = target.persistentID;
		if (target.NetworkHQ == this)
		{
			return true;
		}
		TrackingInfo trackingData = GetTrackingData(persistentID);
		if (trackingData == null)
		{
			return false;
		}
		if (Time.timeSinceLevelLoad - trackingData.lastSpottedTime > 4f)
		{
			return false;
		}
		return true;
	}

	public bool IsTargetPositionAccurate(Unit target, float threshold)
	{
		PersistentID persistentID = target.persistentID;
		if (target.NetworkHQ == this)
		{
			return true;
		}
		TrackingInfo trackingData = GetTrackingData(persistentID);
		if (trackingData == null)
		{
			return false;
		}
		if (Time.timeSinceLevelLoad - trackingData.lastSpottedTime < 4f)
		{
			return true;
		}
		if (!TryGetKnownPosition(target, out var knownPosition))
		{
			return false;
		}
		return FastMath.InRange(target.GlobalPosition(), knownPosition, threshold);
	}

	public void RegisterDropZone(GlobalPosition position)
	{
		activeDropZones.Add(position);
	}

	public void DeregisterDropZone(GlobalPosition position)
	{
		for (int num = activeDropZones.Count - 1; num >= 0; num--)
		{
			if (FastMath.SquareDistance(activeDropZones[num], position) < 625f)
			{
				activeDropZones.RemoveAt(num);
			}
		}
	}

	public bool IsDropZoneClear(GlobalPosition position)
	{
		foreach (GlobalPosition activeDropZone in activeDropZones)
		{
			if (FastMath.SquareDistance(position, activeDropZone) < 2500f)
			{
				return false;
			}
		}
		return true;
	}

	public void NotifyNeedsRepair(Unit unit)
	{
		if (!unitsNeedingRepair.Contains(unit))
		{
			unitsNeedingRepair.Add(unit);
		}
	}

	public void NotifyRepaired(Unit unit)
	{
		unitsNeedingRepair.Remove(unit);
	}

	public bool TryGetUnitNeedingRepair(Unit repairerUnit, float range, out Unit priorityUnit)
	{
		priorityUnit = null;
		GlobalPosition repairerPosition = repairerUnit.GlobalPosition();
		float num = 0f;
		foreach (Unit item in unitsNeedingRepair)
		{
			float repairPriority = (item as IRepairable).GetRepairPriority(repairerPosition, range);
			if (repairPriority > num)
			{
				num = repairPriority;
				priorityUnit = item;
			}
		}
		return priorityUnit != null;
	}

	public IEnumerable<Airbase> GetAirbases()
	{
		foreach (NetworkBehaviorSyncvar<Airbase> item in airbasesUnsorted)
		{
			Airbase value = item.Value;
			if (value != null)
			{
				yield return value;
			}
		}
	}

	private void SortAirbases()
	{
		airbasesSorted.Clear();
		if (!MissionManager.IsRunning || airbasesUnsorted.Count == 0)
		{
			return;
		}
		if (MissionPosition.HasObjectiveWithPosition(this))
		{
			foreach (NetworkBehaviorSyncvar<Airbase> item in airbasesUnsorted)
			{
				Airbase value = item.Value;
				if (!(value == null))
				{
					float distance = GetDistance(value.center);
					airbasesSorted.Add((value, distance));
				}
			}
			airbasesSorted.Sort(((Airbase airbase, float distance) a, (Airbase airbase, float distance) b) => a.distance.CompareTo(b.distance));
			{
				foreach (var item2 in airbasesSorted)
				{
					_ = item2;
				}
				return;
			}
		}
		foreach (NetworkBehaviorSyncvar<Airbase> item3 in airbasesUnsorted)
		{
			Airbase value2 = item3.Value;
			if (!(value2 == null))
			{
				airbasesSorted.Add((value2, 0f));
			}
		}
	}

	private void SortDepots()
	{
		if (MissionManager.IsRunning && depotSorted.Count != 0 && MissionPosition.HasObjectiveWithPosition(this))
		{
			for (int i = 0; i < depotSorted.Count; i++)
			{
				float distance = GetDistance(depotSorted[i].depot.transform);
				depotSorted[i] = (depotSorted[i].depot, distance);
			}
			depotSorted.Sort(((VehicleDepot depot, float distance) a, (VehicleDepot depot, float distance) b) => a.distance.CompareTo(b.distance));
		}
	}

	private float GetDistance(Transform from)
	{
		if (!MissionPosition.TryGetClosestDistance(this, from, out var distance))
		{
			return float.MaxValue;
		}
		return distance;
	}

	public float GetAircraftThreat(PersistentID id)
	{
		return aircraftThreatTracker.GetThreat(id);
	}

	public Airbase GetNearestAirbase(Vector3 fromPosition, RunwayQuery query)
	{
		if (!TryGetNearestAirbase(fromPosition, float.MaxValue, out var nearestAirbase, query))
		{
			return null;
		}
		return nearestAirbase;
	}

	public Airbase GetNearestAirbase(Vector3 fromPosition, float range = float.MaxValue, RunwayQuery query = default(RunwayQuery))
	{
		if (!TryGetNearestAirbase(fromPosition, range, out var nearestAirbase, query))
		{
			return null;
		}
		return nearestAirbase;
	}

	public bool TryGetNearestAirbase(Vector3 fromPosition, out Airbase nearestAirbase, RunwayQuery query = default(RunwayQuery))
	{
		return TryGetNearestAirbase(fromPosition, float.MaxValue, out nearestAirbase, query);
	}

	public bool TryGetNearestAirbase(Vector3 fromPosition, float range, out Airbase nearestAirbase, RunwayQuery query = default(RunwayQuery))
	{
		bool result = false;
		nearestAirbase = null;
		float num = range * range;
		foreach (NetworkBehaviorSyncvar<Airbase> item in airbasesUnsorted)
		{
			Airbase value = item.Value;
			if (!(value == null) && value.IsSuitable(query))
			{
				float num2 = FastMath.SquareDistance(fromPosition, value.center.position);
				if (!(num2 >= num))
				{
					num = num2;
					nearestAirbase = value;
					result = true;
				}
			}
		}
		return result;
	}

	public bool AnyNearAirbase(Vector3 fromPosition, out Airbase airbase)
	{
		airbase = null;
		foreach (NetworkBehaviorSyncvar<Airbase> item in airbasesUnsorted)
		{
			Airbase value = item.Value;
			if (!(value == null))
			{
				float radius = value.GetRadius();
				if (FastMath.InRange(fromPosition, value.center.position, radius))
				{
					airbase = value;
					return true;
				}
			}
		}
		return false;
	}

	public bool TryGetNearestShip(GlobalPosition fromPosition, out Ship nearestShip, out float nearestDistance)
	{
		nearestShip = null;
		nearestDistance = float.MaxValue;
		foreach (PersistentID factionUnit in factionUnits)
		{
			if (UnitRegistry.TryGetUnit(factionUnit, out var unit) && unit is Ship ship)
			{
				float num = FastMath.SquareDistance(ship.GlobalPosition(), fromPosition);
				if (num < nearestDistance)
				{
					nearestShip = ship;
					nearestDistance = num;
				}
			}
		}
		return nearestShip != null;
	}

	public bool TryGetNearestUnitStorage(Unit fromUnit, bool requireStoredUnits, out UnitStorage nearestUnitStorage, out float nearestDistance)
	{
		nearestUnitStorage = null;
		nearestDistance = float.MaxValue;
		foreach (PersistentID factionUnit in factionUnits)
		{
			if (UnitRegistry.TryGetUnit(factionUnit, out var unit) && unit is Ship ship && ship != fromUnit)
			{
				float num = FastMath.SquareDistance(ship.GlobalPosition(), fromUnit.GlobalPosition());
				if (num < nearestDistance && ship.gameObject.TryGetComponent<UnitStorage>(out var component) && component.CanFit(fromUnit.definition) && (!requireStoredUnits || component.HasUnits()))
				{
					nearestUnitStorage = component;
					nearestDistance = num;
				}
			}
		}
		return nearestUnitStorage != null;
	}

	public VehicleDepot GetNearestDepot(Vector3 fromPosition)
	{
		if (!TryGetNearestDepot(fromPosition, float.MaxValue, out var nearestDepot))
		{
			return null;
		}
		return nearestDepot;
	}

	public VehicleDepot GetNearestDepot(Vector3 fromPosition, float range = float.MaxValue)
	{
		if (!TryGetNearestDepot(fromPosition, range, out var nearestDepot))
		{
			return null;
		}
		return nearestDepot;
	}

	public bool TryGetNearestDepot(Vector3 fromPosition, float range, out VehicleDepot nearestDepot)
	{
		bool result = false;
		nearestDepot = null;
		float num = range * range;
		foreach (var item2 in depotSorted)
		{
			VehicleDepot item = item2.depot;
			if (!(item == null))
			{
				float num2 = FastMath.SquareDistance(fromPosition, item.transform.position);
				if (!(num2 >= num))
				{
					num = num2;
					nearestDepot = item;
					result = true;
				}
			}
		}
		return result;
	}

	private void Update()
	{
		if (base.IsServer && GameManager.gameState != GameState.Editor)
		{
			aircraftThreatTracker.CheckThreats();
		}
	}

	public void ReportKillAction(Player player, Unit target, float factor)
	{
		float value = target.definition.value;
		value += target.GetAmmoValue().Current;
		Rearmer componentInChildren = target.GetComponentInChildren<Rearmer>();
		if (componentInChildren != null)
		{
			value += componentInChildren.Capacity / 1000f;
		}
		float rewardScore = factor * Mathf.Sqrt(value);
		float rewardAllocation = killReward * factor * Mathf.Sqrt(value);
		RewardPlayer(player, target, rewardAllocation, rewardScore, RewardType.Kill);
	}

	public void ReportReconAction(Player player, float totalValue)
	{
		RewardPlayer(player, null, totalValue, totalValue, RewardType.Recon);
	}

	public void ReportJammingAction(Player player, Unit target, float totalJamValue)
	{
		RewardPlayer(player, target, totalJamValue, totalJamValue, RewardType.Jamming);
	}

	public void ReportSupplyAction(Player player, Unit target, float refillValue)
	{
		float num = Mathf.Sqrt(refillValue);
		RewardPlayer(player, target, num, num, RewardType.Supply);
	}

	public void ReportRefuelAction(Player player, Unit target, float refillValue)
	{
		float num = Mathf.Sqrt(refillValue);
		RewardPlayer(player, target, num, num, RewardType.Refuel);
	}

	public void ReportRescuePilotsAction(Player player, PilotDismounted pilotDismounted)
	{
		float rewardScore = 2f * (1f + (float)pilotDismounted.GetPilotRank());
		float rewardAllocation = 2f * (1f + (float)pilotDismounted.GetPilotRank());
		RewardPlayer(player, pilotDismounted, rewardAllocation, rewardScore, RewardType.RescuePilots);
	}

	public void ReportCapturePilotsAction(Player player, PilotDismounted pilotDismounted)
	{
		float rewardScore = 3f * (1f + (float)pilotDismounted.GetPilotRank());
		float rewardAllocation = 3f * (1f + (float)pilotDismounted.GetPilotRank());
		RewardPlayer(player, pilotDismounted, rewardAllocation, rewardScore, RewardType.CapturePilots);
	}

	public void ReportCaptureLocationAction(Player player, Airbase capturedAirbase, float value)
	{
		RewardPlayer(player, null, value, value, RewardType.CaptureLocation);
	}

	public void ReportRepairAction(Player player, float fundsAwarded, float pointsAwarded)
	{
		RewardPlayer(player, null, fundsAwarded * killReward, pointsAwarded, RewardType.Repair);
	}

	[Server]
	public void RewardPlayer(Player player, Unit target, float rewardAllocation, float rewardScore, RewardType missionType)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'RewardPlayer' called when server not active");
		}
		player.AddAllocation(rewardAllocation * (1f - playerTaxRate));
		player.AddScore(rewardScore);
		if (player.Aircraft != null && !player.Aircraft.disabled)
		{
			Aircraft aircraft = player.Aircraft;
			aircraft.NetworksortieScore = aircraft.sortieScore + Mathf.Max(rewardAllocation, rewardScore);
		}
		if (missionType != RewardType.None)
		{
			NetworkSceneSingleton<MessageManager>.i.TargetCreditMessage(player.Owner, (target != null) ? target.persistentID : PersistentID.None, rewardScore, missionType);
		}
	}

	public FactionHQ()
	{
		InitSyncObject(factionUnits);
		InitSyncObject(factionRadarReturn);
		InitSyncObject(factionPlayers);
		InitSyncObject(airbasesUnsorted);
		InitSyncObject(AircraftSupply);
		InitSyncObject(VehicleSupply);
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
			writer.WriteSingleConverter(factionFunds);
			writer.WriteBooleanExtension(preventJoin);
			writer.WriteBooleanExtension(preventDonation);
			writer.WriteSingleConverter(factionScore);
			GeneratedNetworkCode._Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, restrictedWeapons);
			GeneratedNetworkCode._Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, restrictedAircraft);
			return true;
		}
		writer.Write(syncVarDirtyBits, 6);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteSingleConverter(factionFunds);
			result = true;
		}
		if ((syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteBooleanExtension(preventJoin);
			result = true;
		}
		if ((syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteBooleanExtension(preventDonation);
			result = true;
		}
		if ((syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteSingleConverter(factionScore);
			result = true;
		}
		if ((syncVarDirtyBits & 0x10L) != 0L)
		{
			GeneratedNetworkCode._Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, restrictedWeapons);
			result = true;
		}
		if ((syncVarDirtyBits & 0x20L) != 0L)
		{
			GeneratedNetworkCode._Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, restrictedAircraft);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			factionFunds = reader.ReadSingleConverter();
			bool value = preventJoin;
			preventJoin = reader.ReadBooleanExtension();
			preventDonation = reader.ReadBooleanExtension();
			factionScore = reader.ReadSingleConverter();
			restrictedWeapons = GeneratedNetworkCode._Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			restrictedAircraft = GeneratedNetworkCode._Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			if (!base.IsServer && !SyncVarEqual(value, preventJoin))
			{
				this.onPreventJoinChanged?.Invoke();
			}
			return;
		}
		ulong num = reader.Read(6);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			factionFunds = reader.ReadSingleConverter();
		}
		if ((num & 2L) != 0L)
		{
			bool value2 = preventJoin;
			preventJoin = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(value2, preventJoin))
			{
				this.onPreventJoinChanged?.Invoke();
			}
		}
		if ((num & 4L) != 0L)
		{
			preventDonation = reader.ReadBooleanExtension();
		}
		if ((num & 8L) != 0L)
		{
			factionScore = reader.ReadSingleConverter();
		}
		if ((num & 0x10L) != 0L)
		{
			restrictedWeapons = GeneratedNetworkCode._Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
		}
		if ((num & 0x20L) != 0L)
		{
			restrictedAircraft = GeneratedNetworkCode._Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
		}
	}

	public void UserCode_RpcExclusionZone_1543365140(ExclusionZone exclusionZone)
	{
		RegisterExclusionZone(exclusionZone).Forget();
	}

	protected static void Skeleton_RpcExclusionZone_1543365140(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((FactionHQ)behaviour).UserCode_RpcExclusionZone_1543365140(GeneratedNetworkCode._Read_NuclearOption_002EExclusionZone(reader));
	}

	private void UserCode_RpcGetTrackingStateBatched__002D1045112216(INetworkPlayer _, ArraySegment<byte> segment)
	{
		using PooledNetworkReader pooledNetworkReader = NetworkReaderPool.GetReader(segment, null);
		while (pooledNetworkReader.CanReadBytes(4))
		{
			PersistentID id = pooledNetworkReader.Read<PersistentID>();
			GlobalPosition lastKnownPosition = pooledNetworkReader.Read<GlobalPosition>();
			float lastSpottedTime = pooledNetworkReader.Read<float>();
			SetTrackingState(id, lastKnownPosition, lastSpottedTime);
		}
	}

	protected static void Skeleton_RpcGetTrackingStateBatched__002D1045112216(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((FactionHQ)behaviour).UserCode_RpcGetTrackingStateBatched__002D1045112216(behaviour.Client.Player, reader.ReadBytesAndSizeSegment());
	}

	public void UserCode_CmdUpdateTrackingInfo__002D1717917610(PersistentID id)
	{
		RpcUpdateTrackingInfo(id);
	}

	protected static void Skeleton_CmdUpdateTrackingInfo__002D1717917610(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((FactionHQ)behaviour).UserCode_CmdUpdateTrackingInfo__002D1717917610(GeneratedNetworkCode._Read_PersistentID(reader));
	}

	public void UserCode_RpcUpdateTrackingInfo__002D640322303(PersistentID id)
	{
		if (!UnitRegistry.TryGetUnit(id, out var unit))
		{
			return;
		}
		if (trackingDatabase.TryGetValue(id, out var value))
		{
			value.UpdateInfo(unit.GlobalPosition());
			return;
		}
		TrackingInfo item = new TrackingInfo(unit);
		trackingDatabase.Add(id, new TrackingInfo(unit));
		unit.onDisableUnit += DeregisterTrackedUnit;
		unit.onChangeFaction += HQ_OnUnitChangeFaction;
		this.onDiscoverUnit?.Invoke(id);
		if (DynamicMap.IsFactionMode(this, FactionMode.Spectator | FactionMode.Friendly))
		{
			SceneSingleton<DynamicMap>.i.AddIcon(id);
		}
		if (unit.NetworkHQ != null && unit.NetworkHQ != this && unit.definition.typeIdentity.strategic > 0f)
		{
			strategicTargets.Add(item);
		}
	}

	protected static void Skeleton_RpcUpdateTrackingInfo__002D640322303(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((FactionHQ)behaviour).UserCode_RpcUpdateTrackingInfo__002D640322303(GeneratedNetworkCode._Read_PersistentID(reader));
	}

	public void UserCode_RpcDeclareEndGame__002D1539254804(EndType endType)
	{
		if (GameManager.gameResolution == GameResolution.Ongoing)
		{
			FactionHQ localHq;
			bool num = GameManager.GetLocalHQ(out localHq) && localHq == this;
			bool flag = endType == EndType.Victory;
			if (num == flag)
			{
				GameManager.FinishGame(GameResolution.Victory);
				MusicManager.i.PlayMusic(GameAssets.i.missionSuccessMusic, repeat: false);
			}
			else
			{
				GameManager.FinishGame(GameResolution.Defeat);
				MusicManager.i.PlayMusic(GameAssets.i.missionFailedMusic, repeat: false);
			}
			SceneSingleton<GameplayUI>.i.PauseGame();
		}
	}

	protected static void Skeleton_RpcDeclareEndGame__002D1539254804(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((FactionHQ)behaviour).UserCode_RpcDeclareEndGame__002D1539254804(GeneratedNetworkCode._Read_NuclearOption_002ESavedMission_002EOutcomes_002EEndType(reader));
	}

	protected override int GetRpcCount()
	{
		return 5;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "FactionHQ.RpcExclusionZone", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcExclusionZone_1543365140, RpcRateLimitConfig.Disabled());
		collection.Register(1, "FactionHQ.RpcGetTrackingStateBatched", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcGetTrackingStateBatched__002D1045112216, RpcRateLimitConfig.Disabled());
		collection.Register(2, "FactionHQ.CmdUpdateTrackingInfo", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdUpdateTrackingInfo__002D1717917610, RpcRateLimitConfig.Enabled(1f, 20, 40, 2));
		collection.Register(3, "FactionHQ.RpcUpdateTrackingInfo", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcUpdateTrackingInfo__002D640322303, RpcRateLimitConfig.Disabled());
		collection.Register(4, "FactionHQ.RpcDeclareEndGame", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcDeclareEndGame__002D1539254804, RpcRateLimitConfig.Disabled());
	}
}
