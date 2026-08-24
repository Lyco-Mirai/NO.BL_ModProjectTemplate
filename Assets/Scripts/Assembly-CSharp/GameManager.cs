using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Mirage.Events;
using NuclearOption.AddressableScripts;
using NuclearOption.DedicatedServer;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using NuclearOption.Social;
using NuclearOption.UIStyleSystem;
using Rewired;
using Rewired.UI.ControlMapper;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class GameManager
{
	public static Rewired.Player playerInput;

	public static bool IsPlayingFromEditor;

	public static bool IsHeadless;

	public static int? OverrideTargetFrameRate;

	public static int TargetFrameRate = -1;

	public static AddLateEvent _onGameStateChanged = new AddLateEvent();

	public static readonly List<ISceneSingleton> SceneSingletons = new List<ISceneSingleton>();

	public static Dictionary<AircraftDefinition, AircraftCustomization> aircraftCustomization = new Dictionary<AircraftDefinition, AircraftCustomization>();

	public static int playerLivery;

	public static Transform playerSpawnPoint;

	public static ControlMapper controlMapper;

	public static GameObject controlMapperCanvas;

	public static TobiiSettingsUI tobiiSettingsUI;

	public static bool flightControlsEnabled = true;

	public static EventSystem eventSystem;

	private static BasePlayer _localPlayer;

	private static string blockFileCache;

	public static GameState gameState { get; private set; }

	public static bool ShowEffects
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return !IsHeadless;
		}
	}

	public static IAddLateEvent OnGameStateChanged => _onGameStateChanged;

	public static GameResolution gameResolution { get; private set; }

	public static DisconnectInfo disconnectInfo { get; private set; }

	public static string BlockFilePath => blockFileCache ?? (blockFileCache = Path.Join(Application.persistentDataPath, "blocklist.txt"));

	public static AllowBanList MutedList { get; } = new AllowBanList();

	public static AllowBanList BlockList { get; } = new AllowBanList();

	private GameManager()
	{
	}

	public static void SetLocalPlayer(BasePlayer player)
	{
		_localPlayer = player;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetLocalPlayer<T>(out T localPlayer) where T : BasePlayer
	{
		localPlayer = _localPlayer as T;
		return localPlayer != null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetLocalHQ(out FactionHQ localHq)
	{
		/*
		if (GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			localHq = localPlayer.HQ;
			return localHq != null;
		}*/
		localHq = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetLocalFaction(out Faction localFaction)
	{
		if (GetLocalHQ(out var localHq))
		{
			localFaction = localHq.faction;
			return true;
		}
		localFaction = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetLocalAircraft(out Aircraft localAircraft)
	{
		if (GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			localAircraft = localPlayer.Aircraft;
			return localAircraft != null;
		}
		localAircraft = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetLocalPilotDismounted(out PilotDismounted pilot)
	{
		if (GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			pilot = localPlayer.PilotDismounted;
			return pilot != null;
		}
		pilot = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLocalPlayer<T>(T playerToCheck) where T : BasePlayer
	{
		if (GetLocalPlayer<T>(out var localPlayer))
		{
			return playerToCheck == localPlayer;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLocalAircraft(Unit unitToCheck)
	{
		if (unitToCheck is Aircraft aircraftToCheck)
		{
			return IsLocalAircraft(aircraftToCheck);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLocalAircraft(Aircraft aircraftToCheck)
	{
		if (GetLocalAircraft(out var localAircraft))
		{
			return aircraftToCheck == localAircraft;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLocalHQ(FactionHQ hqToCheck)
	{
		if (GetLocalHQ(out var localHq))
		{
			return hqToCheck == localHq;
		}
		return false;
	}

	public static void FinishGame(GameResolution resolution)
	{
		if (gameResolution != GameResolution.Ongoing)
		{
			Debug.LogWarning($"GameResolution set twice. CurrentValue={gameResolution} newValue={resolution}");
			return;
		}
		ColorLog<GameResolution>.Info($"FinishGame {resolution}");
		gameResolution = resolution;
	}

	public static void ResetGameResolution()
	{
		ColorLog<GameResolution>.Info("ResetGameResolution");
		gameResolution = GameResolution.Ongoing;
	}

	public static void SetGameState(GameState gameState)
	{
		GameManager.gameState = gameState;
		_onGameStateChanged.Invoke();
		RichPresenceManager.SetState(gameState);
		LimitFrameRate(gameState);
		bool flag = gameState.IsSingleOrMultiplayer();
		CursorManager.SetFlag(CursorFlags.NotInGame, !flag);
		if (!flag)
		{
			CursorManager.ClearGameplayFlags();
		}
		if (gameState == GameState.Menu)
		{
			IsPlayingFromEditor = false;
		}
	}

	public static void ClearDisconnectReason()
	{
		disconnectInfo = null;
	}

	public static void SetDisconnectReason(DisconnectInfo disconnectInfo)
	{
		if (GameManager.disconnectInfo != null && GameManager.disconnectInfo.ShowReason && disconnectInfo.ShowReason)
		{
			GameManager.disconnectInfo.Merge(disconnectInfo);
		}
		else
		{
			GameManager.disconnectInfo = disconnectInfo;
		}
	}

	public static void LimitFrameRate(GameState gameState)
	{
		bool flag;
		switch (gameState)
		{
		case GameState.ServerWaiting:
			Application.targetFrameRate = 5;
			ColorLog<GameManager>.Info("Setting ServerWaiting FrameRate to 5 fps");
			return;
		case GameState.SinglePlayer:
		case GameState.Multiplayer:
		case GameState.Editor:
		case GameState.Encyclopedia:
			flag = false;
			break;
		default:
			flag = true;
			break;
		}
		int num = (Application.targetFrameRate = ((!OverrideTargetFrameRate.HasValue) ? ((IsHeadless || flag) ? 60 : TargetFrameRate) : OverrideTargetFrameRate.Value));
		ColorLog<GameManager>.Info($"Setting target FrameRate {num}");
	}

	public static void PreSetupGame()
	{
		IsHeadless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
		SetJobCount();
		PlayerSettings.screenResolutionHelper.LoadPrefs();
	}

	public static void SetupGame()
	{
		playerInput = ReInput.players.GetPlayer(0);
		PlayerSettings.LoadPrefs();
		PlayerSettings.ApplyPrefs();
		MusicManager.ResetPlayedMusic();
		HitValidator.Initialize();
		ThemeManager.InitThemeManager();
		ResetGame();
		if (gameState != GameState.Editor)
		{
			ResetGameResolution();
		}
	}

	private static void SetJobCount()
	{
		int jobWorkerMaximumCount = JobsUtility.JobWorkerMaximumCount;
		if (jobWorkerMaximumCount > 2)
		{
			int num = (JobsUtility.JobWorkerCount = Mathf.Clamp((jobWorkerMaximumCount + 1) / 2, 2, 8));
			Debug.Log($"Setting Job worker Count to {num}");
		}
	}

	public static void ResetGame()
	{
		for (int num = SceneSingletons.Count - 1; num >= 0; num--)
		{
			if (SceneSingletons[num].ClearInstance())
			{
				SceneSingletons.RemoveAt(num);
			}
		}
		JobsAllocatorShared.SortAll();
		ModLoadCache.Clear();
		FactionRegistry.Clear();
		UnitRegistry.Clear();
		BattlefieldGrid.Clear();
		aircraftCustomization.Clear();
		playerSpawnPoint = null;
		SetLocalPlayer(null);
	}
}
