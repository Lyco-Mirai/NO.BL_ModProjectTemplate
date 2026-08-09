using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.Networking.Lobbies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class LeaderboardMenu : SceneSingleton<LeaderboardMenu>
	{
		[Header("Components")]
		[SerializeField]
		private LeaderboardFactionList factionList;

		[Header("Titles")]
		[SerializeField]
		private TextMeshProUGUI mainTitleText;

		[SerializeField]
		private TextMeshProUGUI secondaryTitleText;

		[Header("Panels")]
		[SerializeField]
		private GameObject missionFailedPanel;

		[SerializeField]
		private GameObject missionSucceededPanel;

		[Header("Buttons")]
		[SerializeField]
		private Button primaryActionButton;

		[SerializeField]
		private TextMeshProUGUI primaryActionText;

		[SerializeField]
		private string resumeLabel = "RESUME";

		[SerializeField]
		private string spectateLabel = "SPECTATE";

		[SerializeField]
		private Button settingsButton;

		[SerializeField]
		private Button hideUIButton;

		[SerializeField]
		private Button openJoinMenuButton;

		[SerializeField]
		private GameObject restartButton;

		private GameObject settingsMenu;

		private MenuMode currentMode;

		private float lastMissionTimeUpdate;

		public bool SettingsMenuOpen => settingsMenu != null;

		public static bool IsOpen()
		{
			if (SceneSingleton<LeaderboardMenu>.i != null)
			{
				return SceneSingleton<LeaderboardMenu>.i.gameObject.activeSelf;
			}
			return false;
		}

		protected override void Awake()
		{
			base.Awake();
			primaryActionButton.onClick.AddListener(OnPrimaryActionClicked);
			settingsButton.onClick.AddListener(OnSettingsClicked);
			hideUIButton.onClick.AddListener(OnHideUIClicked);
			openJoinMenuButton.onClick.AddListener(OnOpenJoinMenuClicked);
		}

		private string GetMissionTitle()
		{
			if (GameManager.gameState == GameState.Multiplayer)
			{
				string currentLobbyName = SteamLobby.instance.CurrentLobbyName;
				if (!string.IsNullOrEmpty(currentLobbyName))
				{
					return currentLobbyName;
				}
			}
			return MissionManager.CurrentMission?.Name ?? "No Mission";
		}

		public void Open(MenuMode mode)
		{
			currentMode = mode;
			base.gameObject.SetActive(value: true);
			mainTitleText.text = GetMissionTitle();
			primaryActionText.text = ((mode == MenuMode.Join) ? spectateLabel : resumeLabel);
			primaryActionButton.Select();
			restartButton.SetActive(mode == MenuMode.Leaderboard && RestartMissionButton.RestartAllowed());
			hideUIButton.gameObject.SetActive(mode == MenuMode.Leaderboard);
			openJoinMenuButton.gameObject.SetActive(mode == MenuMode.Leaderboard && GameplayUI.ShouldShowSpectatorPanel());
			foreach (LeaderboardFactionItem display in factionList.Displays)
			{
				display.SetMenuMode(mode);
			}
			if (mode == MenuMode.Join)
			{
				secondaryTitleText.text = "SELECT FACTION";
				DynamicMap.AllowedToOpen = false;
				GameplayUI.AllowPauseKeybind = false;
			}
			else
			{
				secondaryTitleText.text = UnitConverter.TimeOfDay(NetworkSceneSingleton<MissionManager>.i.MissionTime / 3600f, includeSeconds: true);
				if (GameManager.gameState == GameState.SinglePlayer)
				{
					TimeScaleManager.Scale = 0f;
					AudioListener.pause = true;
				}
				missionSucceededPanel.SetActive(GameManager.gameResolution == GameResolution.Victory);
				missionFailedPanel.SetActive(GameManager.gameResolution == GameResolution.Defeat);
				DynamicMap.AllowedToOpen = false;
			}
			FixLayout.ForceRebuildAtEndOfFrame(base.transform.AsRectTransform());
		}

		public void Close()
		{
			ResetState();
			base.gameObject.SetActive(value: false);
			if (currentMode == MenuMode.Join)
			{
				SceneSingleton<GameplayUI>.i.CloseJoinMenu();
			}
			else
			{
				SceneSingleton<GameplayUI>.i.ResumeGame();
			}
		}

		public void ResetState()
		{
			TimeScaleManager.Scale = (GameplayUI.GameSlowMotion ? 0.05f : 1f);
			AudioListener.pause = false;
			DynamicMap.AllowedToOpen = true;
		}

		private void OnDestroy()
		{
			ResetState();
		}

		private void OnPrimaryActionClicked()
		{
			if (currentMode == MenuMode.Join)
			{
				OnSpectateClicked();
			}
			else
			{
				OnResumeClicked();
			}
		}

		private void OnResumeClicked()
		{
			Close();
		}

		private void OnSpectateClicked()
		{
			if (GameManager.GetLocalPlayer<Player>(out var localPlayer))
			{
				localPlayer.SetFaction(null);
				SceneSingleton<DynamicMap>.i.SetFaction(null);
				Close();
				SceneSingleton<DynamicMap>.i.Maximize();
			}
		}

		private void OnOpenJoinMenuClicked()
		{
			Close();
			SceneSingleton<GameplayUI>.i.ShowJoinMenu();
		}

		public void JoinFactionCallback(FactionHQ HQ)
		{
			if (!GameManager.GetLocalPlayer<Player>(out var localPlayer) || HQ.preventJoin)
			{
				return;
			}
			if (SceneSingleton<CameraStateManager>.i.currentState is CameraFreeState)
			{
				MissionManager.CurrentMission.GetFactionFromHq(HQ, out var missionFaction);
				if (missionFaction.cameraStartPosition.IsOverride)
				{
					SceneSingleton<CameraStateManager>.i.SetCameraPosition(missionFaction.cameraStartPosition.Value);
				}
			}
			localPlayer.SetFaction(HQ);
			MusicManager.i.CrossFadeMusic(NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.GetStartMusic(HQ.faction), 2f, 0f, repeat: false, allowReplay: false, replacePlaying: true);
			SceneSingleton<DynamicMap>.i.SetFaction(HQ);
			Close();
			SceneSingleton<DynamicMap>.i.Maximize();
		}

		private void OnSettingsClicked()
		{
			if (settingsMenu != null)
			{
				Object.Destroy(settingsMenu);
			}
			settingsMenu = Object.Instantiate(GameAssets.i.settingsMenu, SceneSingleton<GameplayUI>.i.menuCanvas.transform);
		}

		private void OnHideUIClicked()
		{
			SceneSingleton<GameplayUI>.i.menuCanvas.enabled = false;
			GameManager.flightControlsEnabled = true;
			CursorManager.ForceHidden(hidden: true);
		}

		private void Update()
		{
			if (currentMode == MenuMode.Leaderboard && secondaryTitleText != null && UnitConverter.TimeOfDay(NetworkSceneSingleton<MissionManager>.i.MissionTime / 3600f, includeSeconds: true, ref lastMissionTimeUpdate, out var timeString))
			{
				secondaryTitleText.text = timeString;
			}
		}
	}
}
