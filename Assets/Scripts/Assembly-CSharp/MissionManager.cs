using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Collections;
using Mirage.Serialization;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.Networking.Lobbies;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.Social;
using UnityEngine;

public class MissionManager : NetworkSceneSingleton<MissionManager>
{
	[NonSerialized]
	[SyncVar]
	public float tacticalThreshold;

	[NonSerialized]
	[SyncVar]
	public float tacticalMinRank;

	[NonSerialized]
	[SyncVar]
	public float strategicThreshold;

	[NonSerialized]
	[SyncVar]
	public float strategicMinRank;

	[NonSerialized]
	[SyncVar]
	public float currentEscalation;

	private readonly SyncList<string> activeObjectiveNames = new SyncList<string>();

	private readonly SyncDictionary<string, List<int>> activeObjectiveData = new SyncDictionary<string, List<int>>();

	private static bool clientEventsAdded;

	private bool OnSyncMissionChangedRunningFromOnStartClient;

	[NonSerialized]
	[SyncVar]
	public double multiplayerStartTime;

	[NonSerialized]
	[SyncVar]
	public int wrecksMaxNumber = -1;

	[NonSerialized]
	[SyncVar]
	public float wrecksDecayTime = -1f;

	private bool hasDoneGetAllAirbaseAssert;

	public List<Wreckage> listWrecks = new List<Wreckage>();

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 8;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public static bool IsRunning { get; private set; }

	public static Mission CurrentMission { get; private set; }

	public static MissionObjectives Objectives => CurrentMission.RuntimeObjectives;

	public static bool AllowEventContent => CurrentMission?.missionSettings?.allowEventContent == true;

	public static MissionRunner Runner { get; private set; }

	public float MissionTime
	{
		get
		{
			if (GameManager.gameState == GameState.Multiplayer)
			{
				return (float)(base.NetworkTime.Time - multiplayerStartTime);
			}
			return Time.timeSinceLevelLoad;
		}
	}

	public float NetworktacticalThreshold
	{
		get
		{
			return tacticalThreshold;
		}
		set
		{
			if (!SyncVarEqual(value, tacticalThreshold))
			{
				float num = tacticalThreshold;
				tacticalThreshold = value;
				SetDirtyBit(1uL);
			}
		}
	}

	public float NetworktacticalMinRank
	{
		get
		{
			return tacticalMinRank;
		}
		set
		{
			if (!SyncVarEqual(value, tacticalMinRank))
			{
				float num = tacticalMinRank;
				tacticalMinRank = value;
				SetDirtyBit(2uL);
			}
		}
	}

	public float NetworkstrategicThreshold
	{
		get
		{
			return strategicThreshold;
		}
		set
		{
			if (!SyncVarEqual(value, strategicThreshold))
			{
				float num = strategicThreshold;
				strategicThreshold = value;
				SetDirtyBit(4uL);
			}
		}
	}

	public float NetworkstrategicMinRank
	{
		get
		{
			return strategicMinRank;
		}
		set
		{
			if (!SyncVarEqual(value, strategicMinRank))
			{
				float num = strategicMinRank;
				strategicMinRank = value;
				SetDirtyBit(8uL);
			}
		}
	}

	public float NetworkcurrentEscalation
	{
		get
		{
			return currentEscalation;
		}
		set
		{
			if (!SyncVarEqual(value, currentEscalation))
			{
				float num = currentEscalation;
				currentEscalation = value;
				SetDirtyBit(16uL);
			}
		}
	}

	public double NetworkmultiplayerStartTime
	{
		get
		{
			return multiplayerStartTime;
		}
		set
		{
			if (!SyncVarEqual(value, multiplayerStartTime))
			{
				double num = multiplayerStartTime;
				multiplayerStartTime = value;
				SetDirtyBit(32uL);
			}
		}
	}

	public int NetworkwrecksMaxNumber
	{
		get
		{
			return wrecksMaxNumber;
		}
		set
		{
			if (!SyncVarEqual(value, wrecksMaxNumber))
			{
				int num = wrecksMaxNumber;
				wrecksMaxNumber = value;
				SetDirtyBit(64uL);
			}
		}
	}

	public float NetworkwrecksDecayTime
	{
		get
		{
			return wrecksDecayTime;
		}
		set
		{
			if (!SyncVarEqual(value, wrecksDecayTime))
			{
				float num = wrecksDecayTime;
				wrecksDecayTime = value;
				SetDirtyBit(128uL);
			}
		}
	}

	public static event Action<Mission> onMissionLoad;

	public static event Action<Objective> onObjectiveStarted;

	public static event Action<Objective> onObjectiveComplete;

	public static event Action<Mission> OnEditorOrPickerMissionChanged
	{
		add
		{
			if (SceneSingleton<MissionEditor>.i != null)
			{
				onMissionLoad += value;
				value(CurrentMission);
			}
			else if (SceneSingleton<MissionsPicker>.i != null)
			{
				SceneSingleton<MissionsPicker>.i.OnMissionSelect += value;
				value(SceneSingleton<MissionsPicker>.i.Mission);
			}
		}
		remove
		{
			onMissionLoad -= value;
			if (SceneSingleton<MissionsPicker>.i != null)
			{
				SceneSingleton<MissionsPicker>.i.OnMissionSelect -= value;
			}
		}
	}

	public static bool AllowTactical()
	{
		return NetworkSceneSingleton<MissionManager>.i.currentEscalation >= NetworkSceneSingleton<MissionManager>.i.tacticalThreshold;
	}

	public static bool AllowStrategic()
	{
		return NetworkSceneSingleton<MissionManager>.i.currentEscalation >= NetworkSceneSingleton<MissionManager>.i.strategicThreshold;
	}

	private void ClientDisconnected(ClientStoppedReason _)
	{
		StopMission();
	}

	private void StopMission()
	{
		IsRunning = false;
		NetworkManagerNuclearOption.i.Client.Disconnected.RemoveListener(ClientDisconnected);
	}

	public static void NewMission(NewMissionConfig config)
	{
		Mission mission = new Mission(config.Name);
		mission.missionSettings.playerMode = config.PlayerMode;
		mission.MapKey = config.Map;
		if (mission.factions.Count == 0)
		{
			Faction[] defaultFactions = GameAssets.i.defaultFactions;
			foreach (Faction faction in defaultFactions)
			{
				mission.EnsureFactionExists(faction, out var _);
			}
		}
		SetMission(mission, checkIfSame: false);
		mission.NewMissionConfig = config;
		foreach (MissionFaction faction2 in mission.factions)
		{
			bool flag = config.CanJoinAllFactions || config.JoinableFactions.Contains(faction2.factionName);
			faction2.preventJoin = !flag;
		}
	}

	public static void SetNullMission()
	{
		SetMission(Mission.NullMission, checkIfSame: false);
	}

	public static void SetMission(Mission mission, bool checkIfSame)
	{
		if (checkIfSame && CurrentMission == mission)
		{
			ColorLog<MissionManager>.Info("Skipping set mission because missions are the same");
			return;
		}
		IsRunning = false;
		if (CurrentMission != null)
		{
			UnloadMission();
		}
		ColorLog<MissionManager>.Info("Setting CurrentMission, old:" + CurrentMission?.Name + " new:" + mission?.Name);
		CurrentMission = mission;
		MissionManager.onMissionLoad?.Invoke(CurrentMission);
		RichPresenceManager.SetMission(mission);
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			NetworkManagerNuclearOption.i.SetNetworkMission(CurrentMission, NetworkMission.State.Loaded);
		}
		if (NetworkSceneSingleton<MissionManager>.i != null && NetworkSceneSingleton<MissionManager>.i.IsServer)
		{
			NetworkSceneSingleton<MissionManager>.i.SetupInstanceValues();
		}
	}

	private static void UnloadMission()
	{
		ColorLog<MissionManager>.Info("UnloadMission");
		if (Runner != null)
		{
			Runner.OnObjectiveStart -= NetworkSceneSingleton<MissionManager>.i.ServerObjectiveStarted;
			Runner.OnObjectiveCompleted -= NetworkSceneSingleton<MissionManager>.i.ServerObjectiveComplete;
		}
		Runner = null;
		if (NetworkSceneSingleton<MissionManager>.i != null)
		{
			NetworkManagerNuclearOption.i.NetworkMission.Clear();
			if (NetworkSceneSingleton<MissionManager>.i.IsServer)
			{
				NetworkSceneSingleton<MissionManager>.i.activeObjectiveNames.Clear();
				NetworkSceneSingleton<MissionManager>.i.activeObjectiveData.Clear();
			}
			else
			{
				NetworkSceneSingleton<MissionManager>.i.activeObjectiveNames.Reset();
				NetworkSceneSingleton<MissionManager>.i.activeObjectiveData.Reset();
			}
		}
	}

	public static void StartMission()
	{
		ColorLog<MissionManager>.Info("StartMission");
		try
		{
			CurrentMission.OnSceneLoaded(NetworkSceneSingleton<MissionManager>.i);
			if (CurrentMission.LoadErrors.AnyMessages())
			{
				CurrentMission.LoadErrors.LogAllErrors(CurrentMission.Name);
				FlashErrorMessageSingleton.ShowError(CurrentMission.LoadErrors.CreateErrorSummary(CurrentMission.Name));
			}
		}
		catch (MissionLoadException ex)
		{
			Debug.LogError($"Failed to load mission: {ex}");
			LoadErrors loadErrors = ex.LoadErrors;
			loadErrors.LogAllErrors(CurrentMission.Name);
			GameManager.SetDisconnectReason(new DisconnectInfo(loadErrors.CreateErrorSummary(CurrentMission.Name)));
			NetworkManagerNuclearOption.i.Stop(setDisconnectReason: false);
			return;
		}
		NetworkSceneSingleton<LevelInfo>.i.SetStartingCamera(CurrentMission);
		using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
		List<SavedUnit> item = wrapper.Item;
		GetAllSavedUnitsNonAlloc(CurrentMission, item, includeBuiltIn: false);
		MissionObjectivesFactory.AddStartingUnits(CurrentMission.RuntimeObjectives, item);
		IsRunning = true;
		Runner = new MissionRunner(CurrentMission.RuntimeObjectives);
		if (NetworkSceneSingleton<MissionManager>.i.IsServer)
		{
			NetworkSceneSingleton<MissionManager>.i.Client.Disconnected.AddListener(NetworkSceneSingleton<MissionManager>.i.ClientDisconnected);
			Runner.OnObjectiveStart += NetworkSceneSingleton<MissionManager>.i.ServerObjectiveStarted;
			Runner.OnObjectiveCompleted += NetworkSceneSingleton<MissionManager>.i.ServerObjectiveComplete;
			Runner.OnMissionStart();
			NetworkSceneSingleton<MissionManager>.i.NetworktacticalThreshold = CurrentMission.missionSettings.nuclearEscalationThreshold;
			NetworkSceneSingleton<MissionManager>.i.NetworktacticalMinRank = CurrentMission.missionSettings.minRankTacticalWarhead;
			NetworkSceneSingleton<MissionManager>.i.NetworkstrategicThreshold = CurrentMission.missionSettings.strategicEscalationThreshold;
			NetworkSceneSingleton<MissionManager>.i.NetworkstrategicMinRank = CurrentMission.missionSettings.minRankStrategicWarhead;
			NetworkSceneSingleton<MissionManager>.i.NetworkcurrentEscalation = 0f;
			NetworkSceneSingleton<MissionManager>.i.NetworkmultiplayerStartTime = NetworkSceneSingleton<MissionManager>.i.NetworkTime.Time;
			NetworkSceneSingleton<MissionManager>.i.NetworkwrecksMaxNumber = CurrentMission.missionSettings.wrecksMaxNumber;
			NetworkSceneSingleton<MissionManager>.i.NetworkwrecksDecayTime = CurrentMission.missionSettings.wrecksDecayTime;
			Runner.Update();
			NetworkManagerNuclearOption.i.ServerMissionStart(CurrentMission);
		}
	}

	public static async UniTask RestartMission()
	{
		Mission mission = null;
		if (CurrentMission.LoadKey.HasValue)
		{
			ColorLog<MissionManager>.Info($"Restarting mission, key={CurrentMission.LoadKey.Value}");
			if (!MissionSaveLoad.TryLoad(CurrentMission.LoadKey.Value, out mission, out var error))
			{
				Debug.LogError("Failed to reload mission " + error);
				return;
			}
		}
		else
		{
			ColorLog<MissionManager>.Info("Restarting NULL mission");
		}
		LoadingScreen loadingScreen = LoadingScreen.GetLoadingScreen();
		loadingScreen.ShowLoadingScreen();
		loadingScreen.SetProgressRange(0f, 0.3f);
		bool isPlayingFromEditor = GameManager.IsPlayingFromEditor;
		TimeScaleManager.Scale = 0f;
		await NetworkManagerNuclearOption.i.StopAsync(setDisconnectReason: true);
		await UniTask.Yield();
		if (mission != null)
		{
			SetMission(mission, checkIfSame: false);
		}
		else
		{
			SetNullMission();
		}
		UniTask uniTask = NetworkManagerNuclearOption.i.StartHostAsync(new HostOptions(SocketType.Offline, GameState.SinglePlayer, mission?.MapKey ?? default(MapKey)));
		loadingScreen.SetProgressRange(0.3f, 1f);
		await uniTask;
		GameManager.IsPlayingFromEditor = isPlayingFromEditor;
		loadingScreen.HideLoadingScreen();
	}

	protected override void Awake()
	{
		base.Awake();
		base.Identity.OnStartClient.AddListener(OnStartClient);
		base.Identity.OnStopClient.AddListener(OnStopClient);
		base.Identity.OnStartServer.AddListener(OnStartServer);
		if (NetworkSceneSingleton<MissionManager>.i.wrecksMaxNumber > 0)
		{
			NetworkSceneSingleton<MissionManager>.i.StartSlowUpdate(60f, NetworkSceneSingleton<MissionManager>.i.WrecksManagement);
		}
	}

	private void OnDestroy()
	{
		ColorLog<MissionManager>.Info("OnDestroy");
		OnStopClient();
		IsRunning = false;
		Runner?.Cleanup();
		Runner = null;
		SetNullMission();
		MissionManager.onMissionLoad = null;
		MissionManager.onObjectiveStarted = null;
		MissionManager.onObjectiveComplete = null;
		listWrecks.Clear();
	}

	private void Update()
	{
		if (IsRunning)
		{
			if (base.IsServer)
			{
				Runner.Update();
			}
			else if (base.IsClient)
			{
				Runner.ClientOnlyUpdate();
			}
		}
	}

	private void WrecksManagement()
	{
		if (listWrecks.Count > wrecksMaxNumber)
		{
			int num = listWrecks.Count - wrecksMaxNumber;
			for (int i = 0; i < num; i++)
			{
				listWrecks[i].Disintegrate();
			}
		}
	}

	private void OnStartServer()
	{
		if (GameManager.gameState == GameState.Editor)
		{
			return;
		}
		SetupInstanceValues();
		UniTask.Void(async delegate
		{
			CancellationToken cancel = base.destroyCancellationToken;
			if (NetworkSceneSingleton<LevelInfo>.i == null || NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings == null)
			{
				ColorLog<MissionManager>.Info("Waiting for LevelInfo to have Map Settings");
			}
			int framesWaited = 0;
			while (NetworkSceneSingleton<LevelInfo>.i == null || NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings == null)
			{
				framesWaited++;
				await UniTask.Yield();
				if (cancel.IsCancellationRequested)
				{
					return;
				}
			}
			if (framesWaited > 0)
			{
				ColorLog<MissionManager>.Info($"Waited {framesWaited} frames for MapSettings");
			}
			await UniTask.Yield();
			await UniTask.Yield();
			if (!cancel.IsCancellationRequested)
			{
				StartMission();
			}
		});
	}

	private void SetupInstanceValues()
	{
		if (CurrentMission != null)
		{
			activeObjectiveNames.Clear();
			activeObjectiveData.Clear();
		}
	}

	private void OnStartClient()
	{
		if (!base.IsServer)
		{
			ColorLog<MissionManager>.Info("Client Start");
			OnSyncMissionChangedRunningFromOnStartClient = true;
			try
			{
				NetworkManagerNuclearOption.i.NetworkMission.Changed.AddListener(OnSyncMissionChanged);
				clientEventsAdded = true;
			}
			finally
			{
				OnSyncMissionChangedRunningFromOnStartClient = false;
			}
			activeObjectiveNames.OnInsert += ActiveObjectiveNames_OnAdd;
			activeObjectiveNames.OnRemove += ActiveObjectiveNames_OnRemove;
			activeObjectiveNames.OnClear += ActiveObjectiveNames_OnClear;
			activeObjectiveData.OnInsert += ActiveObjectiveData_OnAdd;
		}
	}

	private void OnStopClient()
	{
		ColorLog<MissionManager>.Info("Client stop");
		if (clientEventsAdded)
		{
			clientEventsAdded = false;
			NetworkManagerNuclearOption.i.NetworkMission.Changed.RemoveListener(OnSyncMissionChanged);
		}
	}

	private void OnSyncMissionChanged((NetworkMission.SyncMission syncMission, NetworkMission.State state, bool stateChangeOnly) tuple)
	{
		var (syncMission, state, flag) = tuple;
		if (OnSyncMissionChangedRunningFromOnStartClient && flag)
		{
			flag = false;
			ColorLog<MissionManager>.Info("ignoring stateChangeOnly because calling from OnStartClient");
		}
		if (flag)
		{
			ColorLog<MissionManager>.Info($"OnSyncMissionChanged StateOnly {state}");
		}
		else
		{
			ColorLog<MissionManager>.Info(string.Format("OnSyncMissionChanged: ({0}, {1})", state, syncMission.Name ?? "NULL"));
		}
		if (!flag)
		{
			CurrentMission = syncMission.Mission;
			CurrentMission.AfterLoad(syncMission.Name);
			RichPresenceManager.SetMission(CurrentMission);
		}
		if (state == NetworkMission.State.Running)
		{
			ColorLog<MissionManager>.Info("Will start mission next frame");
			UniTask.Void(async delegate
			{
				CancellationToken cancel = base.destroyCancellationToken;
				while (!base.Client.Player.HasCharacter)
				{
					ColorLog<MissionManager>.Info("Waiting for Local player it spawn");
					await UniTask.Yield();
					if (cancel.IsCancellationRequested)
					{
						return;
					}
				}
				ColorLog<MissionManager>.Info("Waiting extra frame before start mission");
				await UniTask.Yield();
				if (!cancel.IsCancellationRequested)
				{
					foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
					{
						CurrentMission.GetFactionFromHq(allHQ, out var _);
					}
					StartMission();
					Runner.SetAllRemoteActiveObjectives(activeObjectiveNames, activeObjectiveData);
				}
			});
		}
		else
		{
			ColorLog<MissionManager>.Info("Mission not running, wating for server to start it");
		}
	}

	private void ActiveObjectiveNames_OnAdd(int index, string value)
	{
		if (Runner != null)
		{
			Objective obj = Runner.AddRemoteActiveObjective(value);
			MissionManager.onObjectiveStarted?.Invoke(obj);
		}
	}

	private void ActiveObjectiveNames_OnRemove(int index, string value)
	{
		if (Runner != null)
		{
			Objective obj = Runner.RemoveRemoteActiveObjective(value);
			MissionManager.onObjectiveComplete?.Invoke(obj);
		}
	}

	private void ActiveObjectiveNames_OnClear()
	{
		Runner?.ClearRemoteActiveObjectives();
	}

	private void ActiveObjectiveData_OnAdd(string key, List<int> item)
	{
		if (!base.IsServer)
		{
			UpdateNetworkObjectiveData(key, item);
		}
	}

	private void UpdateNetworkObjectiveData(string uniqueName, List<int> data)
	{
		(CurrentMission?.RuntimeObjectives?.GetObjective(uniqueName))?.ReceiveNetworkData(data);
	}

	public void UpdateNetworkData(Objective obj, List<int> data)
	{
		activeObjectiveData[obj.SavedObjective.UniqueName] = data;
	}

	private void ServerObjectiveStarted(Objective obj)
	{
		activeObjectiveNames.Add(obj.SavedObjective.UniqueName);
		MissionManager.onObjectiveStarted?.Invoke(obj);
	}

	private void ServerObjectiveComplete(Objective obj)
	{
		activeObjectiveNames.Remove(obj.SavedObjective.UniqueName);
		activeObjectiveData.Remove(obj.SavedObjective.UniqueName);
		MissionManager.onObjectiveComplete?.Invoke(obj);
	}

	public static Dictionary<string, Airbase> GetAllAirbase()
	{
		return FactionRegistry.airbaseLookup;
	}

	private static void GetAllAirbaseAssert(Dictionary<string, Airbase> all)
	{
	}

	public static void GetAllSavedAirbaseNonAlloc(List<SavedAirbase> list)
	{
		list.Clear();
		foreach (Airbase value in GetAllAirbase().Values)
		{
			list.Add(value.SavedAirbase);
		}
	}

	public static bool TryFindSavedAirbase(string uniqueName, out SavedAirbase saved)
	{
		using AutoPool<List<SavedAirbase>>.Wrapper wrapper = AutoPool<List<SavedAirbase>>.Take();
		List<SavedAirbase> item = wrapper.Item;
		GetAllSavedAirbaseNonAlloc(item);
		int count = item.Count;
		for (int i = 0; i < count; i++)
		{
			SavedAirbase savedAirbase = item[i];
			if (savedAirbase.UniqueName == uniqueName)
			{
				saved = savedAirbase;
				return true;
			}
		}
		saved = null;
		return false;
	}

	public static void GetAllSavedUnitsNonAlloc(List<SavedUnit> list, bool includeBuiltIn)
	{
		Mission currentMission = CurrentMission;
		if (currentMission != null)
		{
			GetAllSavedUnitsNonAlloc(currentMission, list, includeBuiltIn);
		}
		else
		{
			list.Clear();
		}
	}

	public static void GetAllSavedUnitsNonAlloc(Mission mission, List<SavedUnit> list, bool includeBuiltIn)
	{
		list.Clear();
		list.AddRange(mission.aircraft);
		list.AddRange(mission.vehicles);
		list.AddRange(mission.ships);
		list.AddRange(mission.buildings);
		list.AddRange(mission.scenery);
		list.AddRange(mission.containers);
		list.AddRange(mission.missiles);
		list.AddRange(mission.pilots);
		if (!includeBuiltIn)
		{
			return;
		}
		using AutoPool<HashSet<SavedUnit>>.Wrapper wrapper = AutoPool<HashSet<SavedUnit>>.Take();
		HashSet<SavedUnit> item = wrapper.Item;
		item.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			item.Add(list[i]);
		}
		using AutoPool<List<SavedUnit>>.Wrapper wrapper2 = AutoPool<List<SavedUnit>>.Take();
		List<SavedUnit> item2 = wrapper2.Item;
		item2.Clear();
		foreach (Unit allUnit in UnitRegistry.allUnits)
		{
			SavedUnit savedUnit = allUnit.SavedUnit;
			if (savedUnit != null && item.Add(savedUnit))
			{
				item2.Add(savedUnit);
			}
		}
		list.AddRange(item2);
	}

	public static void GetAllSavedBuildingsNonAlloc(List<SavedBuilding> list, bool includeBuiltIn)
	{
		Mission currentMission = CurrentMission;
		if (currentMission != null)
		{
			GetAllSavedBuildingsNonAlloc(currentMission, list, includeBuiltIn);
		}
		else
		{
			list.Clear();
		}
	}

	public static void GetAllSavedBuildingsNonAlloc(Mission mission, List<SavedBuilding> list, bool includeBuiltIn)
	{
		list.Clear();
		list.AddRange(mission.buildings);
		if (!includeBuiltIn)
		{
			return;
		}
		using AutoPool<HashSet<SavedBuilding>>.Wrapper wrapper = AutoPool<HashSet<SavedBuilding>>.Take();
		HashSet<SavedBuilding> item = wrapper.Item;
		item.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			item.Add(list[i]);
		}
		using AutoPool<List<SavedUnit>>.Wrapper wrapper2 = AutoPool<List<SavedUnit>>.Take();
		List<SavedUnit> item2 = wrapper2.Item;
		item2.Clear();
		foreach (Unit allUnit in UnitRegistry.allUnits)
		{
			if (allUnit is Building)
			{
				SavedBuilding savedBuilding = (SavedBuilding)allUnit.SavedUnit;
				if (savedBuilding != null && item.Add(savedBuilding))
				{
					item2.Add(savedBuilding);
				}
			}
		}
		foreach (SavedUnit item3 in item2)
		{
			list.Add((SavedBuilding)item3);
		}
	}

	public MissionManager()
	{
		InitSyncObject(activeObjectiveNames);
		InitSyncObject(activeObjectiveData);
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
			writer.WriteSingleConverter(tacticalThreshold);
			writer.WriteSingleConverter(tacticalMinRank);
			writer.WriteSingleConverter(strategicThreshold);
			writer.WriteSingleConverter(strategicMinRank);
			writer.WriteSingleConverter(currentEscalation);
			writer.WriteDoubleConverter(multiplayerStartTime);
			writer.WritePackedInt32(wrecksMaxNumber);
			writer.WriteSingleConverter(wrecksDecayTime);
			return true;
		}
		writer.Write(syncVarDirtyBits, 8);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteSingleConverter(tacticalThreshold);
			result = true;
		}
		if ((syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteSingleConverter(tacticalMinRank);
			result = true;
		}
		if ((syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteSingleConverter(strategicThreshold);
			result = true;
		}
		if ((syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteSingleConverter(strategicMinRank);
			result = true;
		}
		if ((syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteSingleConverter(currentEscalation);
			result = true;
		}
		if ((syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteDoubleConverter(multiplayerStartTime);
			result = true;
		}
		if ((syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WritePackedInt32(wrecksMaxNumber);
			result = true;
		}
		if ((syncVarDirtyBits & 0x80L) != 0L)
		{
			writer.WriteSingleConverter(wrecksDecayTime);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			tacticalThreshold = reader.ReadSingleConverter();
			tacticalMinRank = reader.ReadSingleConverter();
			strategicThreshold = reader.ReadSingleConverter();
			strategicMinRank = reader.ReadSingleConverter();
			currentEscalation = reader.ReadSingleConverter();
			multiplayerStartTime = reader.ReadDoubleConverter();
			wrecksMaxNumber = reader.ReadPackedInt32();
			wrecksDecayTime = reader.ReadSingleConverter();
			return;
		}
		ulong num = reader.Read(8);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			tacticalThreshold = reader.ReadSingleConverter();
		}
		if ((num & 2L) != 0L)
		{
			tacticalMinRank = reader.ReadSingleConverter();
		}
		if ((num & 4L) != 0L)
		{
			strategicThreshold = reader.ReadSingleConverter();
		}
		if ((num & 8L) != 0L)
		{
			strategicMinRank = reader.ReadSingleConverter();
		}
		if ((num & 0x10L) != 0L)
		{
			currentEscalation = reader.ReadSingleConverter();
		}
		if ((num & 0x20L) != 0L)
		{
			multiplayerStartTime = reader.ReadDoubleConverter();
		}
		if ((num & 0x40L) != 0L)
		{
			wrecksMaxNumber = reader.ReadPackedInt32();
		}
		if ((num & 0x80L) != 0L)
		{
			wrecksDecayTime = reader.ReadSingleConverter();
		}
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
