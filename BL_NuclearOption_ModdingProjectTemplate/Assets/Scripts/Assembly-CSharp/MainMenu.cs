using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage.Logging;
using NuclearOption.BuildScripts;
using NuclearOption.DedicatedServer;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.Social;
using Rewired.UI.ControlMapper;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	public enum LoadingState
	{
		None = 0,
		Loading = 1,
		Loaded = 2
	}

	[SerializeField]
	private TMP_InputField inputPlayerName;

	[SerializeField]
	private Transform overlayMenuLayer;

	[SerializeField]
	private GameObject unableToConnect;

	[SerializeField]
	private GameObject disconnectNotification;

	[SerializeField]
	private Text disconnectReason;

	[SerializeField]
	private GameObject roadmapPanel;

	[SerializeField]
	private GameObject controlChangesPanel;

	[SerializeField]
	private GameObject hintPanel;

	[SerializeField]
	private GameObject inputSystemUpdatePanel;

	[SerializeField]
	private Button missionsButton;

	[SerializeField]
	private GameObject firstLoadOverlay;

	[SerializeField]
	private CanvasGroup firstLoadOverlayFade;

	[SerializeField]
	private GameObject workshopPrefab;

	[SerializeField]
	private FileMenu openMissionEditorMenu;

	[SerializeField]
	private GraphicsHelperSO graphicsSettings;

	[Header("Steam app id")]
	[SerializeField]
	private string steamAppID;

	[Header("Links")]
	[SerializeField]
	private string changelogURL = "https://store.steampowered.com/news/app/2168680";

	[SerializeField]
	private string linktreeURL = "https://linktr.ee/shockfrontstudios";

	[SerializeField]
	private string merchStoreURL = "https://tr.ee/wuaapf";

	private static bool addedQuitting;

	public static LoadingState State { get; private set; }

	public static bool ApplicationIsQuitting { get; private set; }

	public static async UniTask WaitForLoaded(CancellationToken cancellation)
	{
		await UniTask.WaitUntil(() => State == LoadingState.Loaded, PlayerLoopTiming.Update, cancellation);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		Debug.Log("[MainMenu] RuntimeInitializeOnLoadMethod");
		State = LoadingState.None;
		ApplicationIsQuitting = false;
		if (!addedQuitting)
		{
			addedQuitting = true;
			Application.quitting += delegate
			{
				Debug.Log("[MainMenu] Application_quitting");
				ApplicationIsQuitting = true;
			};
		}
	}

	private void Awake()
	{
		if (State == LoadingState.None)
		{
			Debug.Log($"Nuclear Option {Application.version} Starting at {DateTime.UtcNow} UTC");
			if (!LogTimeUpdater.IsRunning)
			{
				LogTimeUpdater.RunForever().Forget();
			}
			MirageLogHandler.Settings logSettings = new MirageLogHandler.Settings(Application.isEditor, label: true, () => $"{LogTimeUpdater.unscaledTime:0.000}");
			LogFactory.ReplaceLogHandler((string name) => new MirageLogHandler(logSettings, name));
			PlayerSettings.FirstInit(graphicsSettings);
			GameManager.PreSetupGame();
			firstLoadOverlay.SetActive(value: true);
		}
		openMissionEditorMenu.Close();
		roadmapPanel.SetActive(value: false);
		if (!PlayerPrefs.HasKey("FirstStartup0.32"))
		{
			PlayerPrefs.SetInt("FirstStartup0.32", 1);
		}
		else
		{
			controlChangesPanel.SetActive(value: false);
		}
		if (!PlayerPrefs.HasKey("RewiredMigrated"))
		{
			inputSystemUpdatePanel.SetActive(value: true);
		}
	}

	private void Start()
	{
		StartAsync().Forget();
	}

	private async UniTaskVoid StartAsync()
	{
		if (State == LoadingState.None)
		{
			List<UniTask> list = new List<UniTask>();
			State = LoadingState.Loading;
			CancellationToken cancel = base.destroyCancellationToken;
			list.Add(Encyclopedia.Preload(cancel));
			list.Add(GameAssets.Preload(cancel));
			list.Add(DebugUI.Preload(cancel));
			list.Add(NetworkManagerNuclearOption.Preload(cancel));
			list.Add(SoundManager.Preload(cancel));
			list.Add(SteamManager.CheckIdFileAsync(steamAppID));
			list.Add(MissionSaveLoad.ConvertMissionToFolders());
			list.Add(EditorMissionGroup.CleanUpOldAutoSavesSideThread());
			list.Add(ResourcesAsyncLoader.LoadPrefab("Rewired", cancel, delegate(GameObject clone)
			{
				GameManager.controlMapper = clone.GetComponentInChildren<ControlMapper>();
				GameManager.controlMapper.ScreenClosedEvent += delegate
				{
					if (SceneSingleton<GameplayUI>.i != null)
					{
						SceneSingleton<GameplayUI>.i.menuCanvas.enabled = true;
					}
				};
				if (!PlayerPrefs.HasKey("RewiredMigrated"))
				{
					RewiredSaveDataMigrator.RunAsync().Forget();
				}
			}));
			list.Add(ResourcesAsyncLoader.LoadPrefab("TobiiSettingsPanel", cancel, delegate(GameObject clone)
			{
				GameManager.tobiiSettingsUI = clone.GetComponentInChildren<TobiiSettingsUI>(includeInactive: true);
			}));
			list.Add(ResourcesAsyncLoader.LoadPrefab("EventSystem", cancel, delegate(GameObject clone)
			{
				GameManager.eventSystem = clone.GetComponent<EventSystem>();
			}));
			ColorLog<MainMenu>.Info("Waiting for Tasks");
			await UniTask.WhenAll(list);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			ColorLog<MainMenu>.Info("All Task finished");
			State = LoadingState.Loaded;
			LoadOverlayFade(cancel).Forget();
			if (DedicatedServerManager.AutoRun || CommandLineArgParser.ForceSteamServerInit)
			{
				NetworkManagerNuclearOption.i.SteamManager.InitAsServer(DedicatedServerManager.GetConfig().config);
				PlayerSettings.playerName = "Server";
				PlayerSettings.playerName_Unsanitized = "Server";
			}
			else
			{
				NetworkManagerNuclearOption.i.SteamManager.InitAsClient();
				StringHelper.GetSanitizeSteamName(out var rawName, out var safeName, GameAssets.i.playerNameFont);
				PlayerSettings.playerName = safeName;
				PlayerSettings.playerName_Unsanitized = rawName;
			}
		}
		else
		{
			firstLoadOverlay.SetActive(value: false);
		}
		if (!DedicatedServerManager.AutoRun)
		{
			CursorManager.Refresh();
			missionsButton.Select();
			RichPresenceManager.SetState(GameState.Menu);
			inputPlayerName.text = PlayerSettings.playerName ?? "";
			MusicManager.i.PlayMenuMusic();
		}
		TimeScaleManager.Scale = 1f;
		GameManager.SetLocalPlayer(null);
		GameManager.SetGameState(GameState.Menu);
		if (GameManager.disconnectInfo?.ShowReason ?? false)
		{
			disconnectNotification.SetActive(value: true);
			disconnectReason.text = GameManager.disconnectInfo.Message;
		}
		GameManager.ClearDisconnectReason();
		GameManager.SetupGame();
	}

	private async UniTaskVoid LoadOverlayFade(CancellationToken cancel)
	{
		float unscaledTime = Time.unscaledTime;
		float start = unscaledTime;
		float end = start + 0.5f;
		while (end > unscaledTime)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			unscaledTime = Time.unscaledTime;
			firstLoadOverlayFade.alpha = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(start, end, unscaledTime));
		}
		firstLoadOverlay.SetActive(value: false);
	}

	private void Update()
	{
		if (overlayMenuLayer.childCount == 0 && !hintPanel.activeSelf)
		{
			ShowHints();
		}
		else if (overlayMenuLayer.childCount > 0 && hintPanel.activeSelf)
		{
			HideHints();
		}
	}

	public void SelectMissionEditor()
	{
		openMissionEditorMenu.Show(FileMenu.TabIndex.New);
	}

	public void SelectMissions()
	{
		SceneManager.LoadScene(MapLoader.MissionsMenu);
	}

	public void EnterName()
	{
		PlayerSettings.playerName = inputPlayerName.text;
	}

	public void LinktreeButton()
	{
		Application.OpenURL(linktreeURL);
	}

	public void ChangelogButton()
	{
		Application.OpenURL(changelogURL);
	}

	public void MerchStoreButton()
	{
		Application.OpenURL(merchStoreURL);
	}

	public void SelectMultiplayer()
	{
		if (!SteamAPI.IsSteamRunning())
		{
			unableToConnect.SetActive(value: true);
		}
		else
		{
			SceneManager.LoadScene(MapLoader.MultiplayerMenu);
		}
	}

	public void SelectSettings()
	{
		UnityEngine.Object.Instantiate(GameAssets.i.settingsMenu, overlayMenuLayer);
	}

	public void SelectEncyclopedia()
	{
		MissionManager.SetNullMission();
		NetworkManagerNuclearOption.i.StartHost(new HostOptions(SocketType.Offline, GameState.Encyclopedia, MapLoader.Encyclopedia));
	}

	public void SelectWorkshop()
	{
		UnityEngine.Object.Instantiate(workshopPrefab, overlayMenuLayer);
	}

	public void ShowHints()
	{
		hintPanel.SetActive(value: true);
	}

	public void HideHints()
	{
		hintPanel.SetActive(value: false);
	}

	public void QuitGame()
	{
		Application.Quit();
	}
}
