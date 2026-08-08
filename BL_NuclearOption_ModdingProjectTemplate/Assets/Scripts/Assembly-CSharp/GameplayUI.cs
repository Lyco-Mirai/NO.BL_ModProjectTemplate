using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.UI;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : SceneSingleton<GameplayUI>
{
	public enum ActiveCanvas
	{
		Gameplay = 0,
		Menu = 1
	}

	private static bool _leaderboardMenuOpen = false;

	public Image hurt;

	public Canvas gameplayCanvas;

	public Canvas menuCanvas;

	public float pilotHitPoints;

	public DialogueBox DialogueBox;

	public MessageUI MessageUI;

	public Transform topPanelTransform;

	[SerializeField]
	private GameObject selectAirbasePanel;

	[SerializeField]
	private TMP_Text airbaseName;

	[SerializeField]
	private Button selectAircraftButton;

	[SerializeField]
	private GameObject aircraftSelectionMenu;

	[SerializeField]
	private LeaderboardMenu leaderboardMenu;

	[SerializeField]
	private GameObject spectatorPanel;

	private Airbase homeAirbase;

	private float lastResumed;

	private Rewired.Player player;

	[SerializeField]
	private GameObject factionInfoPanel_BDF;

	[SerializeField]
	private GameObject factionInfoPanel_PALA;

	public static bool GameIsPaused => _leaderboardMenuOpen;

	public static bool GameSlowMotion { get; private set; } = false;

	public static bool AllowPauseKeybind { get; set; } = true;

	protected override void Awake()
	{
		base.Awake();
		_leaderboardMenuOpen = false;
		GameSlowMotion = false;
		AllowPauseKeybind = true;
		player = ReInput.players.GetPlayer(0);
		HideSpectatorPanel();
	}

	public void SetActiveCanvas(ActiveCanvas canvas)
	{
		menuCanvas.enabled = canvas == ActiveCanvas.Menu;
		gameplayCanvas.enabled = canvas == ActiveCanvas.Gameplay;
	}

	public void ShowSelectAirbase()
	{
		selectAirbasePanel.SetActive(value: true);
		bool active = false;
		/*
		if (GameManager.gameResolution == GameResolution.Ongoing && GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer) && localPlayer.HQ != null && (SceneSingleton<CombatHUD>.i.aircraft == null || SceneSingleton<CombatHUD>.i.aircraft.disabled) && !localPlayer.AircraftSpawnPending && homeAirbase != null)
		{
			airbaseName.text = homeAirbase.SavedAirbase.DisplayName;
			active = true;
		}*/
		if (GameManager.gameResolution == GameResolution.Defeat)
		{
			airbaseName.text = "Mission Failed, no spawn points available ";
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(selectAirbasePanel.GetComponent<RectTransform>());
		selectAircraftButton.gameObject.SetActive(active);
	}

	public void HideSelectAirbase()
	{
		selectAirbasePanel.SetActive(value: false);
	}

	public void ShowJoinMenu()
	{
		if (_leaderboardMenuOpen)
		{
			_leaderboardMenuOpen = false;
			GameManager.flightControlsEnabled = true;
			lastResumed = Time.timeSinceLevelLoad;
		}
		SetActiveCanvas(ActiveCanvas.Menu);
		CursorManager.SetFlag(CursorFlags.GameMenu, value: true);
		SceneSingleton<DynamicMap>.i.Minimize();
		leaderboardMenu.Open(MenuMode.Join);
	}

	public void CloseJoinMenu()
	{
		AllowPauseKeybind = true;
		CursorManager.SetFlag(CursorFlags.GameMenu, value: false);
		SetActiveCanvas(ActiveCanvas.Gameplay);
		if (SceneSingleton<CombatHUD>.i.aircraft == null)
		{
			SceneSingleton<DynamicMap>.i.Maximize();
		}
	}

	public void OpenMissionEditor()
	{
		Object.Instantiate(GameAssets.i.missionEditor, menuCanvas.transform);
		SetActiveCanvas(ActiveCanvas.Menu);
	}

	public static bool ShouldShowSpectatorPanel()
	{
		/*
		if (GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			return localPlayer.HQ == null;
		}*/
		return false;
	}

	public void CheckShowSpectatorPanel()
	{
		if (ShouldShowSpectatorPanel())
		{
			spectatorPanel.SetActive(value: true);
			HideSelectAirbase();
		}
	}

	public void HideSpectatorPanel()
	{
		spectatorPanel.SetActive(value: false);
	}

	public void SpectatorPanelDeselectAll()
	{
		factionInfoPanel_BDF.GetComponent<InfoPanel_Faction>().DeselectPlayers();
		factionInfoPanel_PALA.GetComponent<InfoPanel_Faction>().DeselectPlayers();
	}

	public void GameMessage(string message)
	{
		MessageUI.GameMessage(message);
	}

	public void GameMessage(string message, float delay)
	{
		MessageUI.DelayedGameMessage(message, delay).Forget();
	}

	public void KillFeed(string message)
	{
		MessageUI.KillFeed(message);
	}

	public void SelectAirbase(Airbase airbase)
	{
		homeAirbase = airbase;
		selectAirbasePanel.SetActive(value: true);
		airbaseName.text = airbase.SavedAirbase.DisplayName;
		selectAircraftButton.gameObject.SetActive(value: true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(selectAirbasePanel.GetComponent<RectTransform>());
	}

	public void SelectAircraft()
	{
		if (!GameManager.GetLocalPlayer<NuclearOption.Networking.Player>(out var localPlayer))
		{
			Debug.LogError("SelectAircraft was clicked but no local player");
			return;
		}/*
		FactionHQ hQ = localPlayer.HQ;
		if (hQ == null)
		{
			Debug.LogError("SelectAircraft was clicked without local faction");
		}
		else if (hQ != homeAirbase.CurrentHQ)
		{
			Debug.LogWarning("SelectAircraft was clicked but airbase faction was not local faction");
		}
		else
		{
			Object.Instantiate(aircraftSelectionMenu, gameplayCanvas.transform).GetComponent<AircraftSelectionMenu>().Initialize(localPlayer, homeAirbase);
		}*/
	}

	public void FlashHurt(float damage, float remainingHitPoints)
	{
		pilotHitPoints = remainingHitPoints;
		hurt.gameObject.SetActive(value: true);
		float a = hurt.color.a;
		a += Mathf.Max(damage * 0.01f, 0.2f);
		hurt.color = new Color(1f, 1f, 1f, a);
	}

	public void PauseGame()
	{
		if (!_leaderboardMenuOpen)
		{
			_leaderboardMenuOpen = true;
			CursorManager.SetFlag(CursorFlags.GameMenu, value: true);
			SetActiveCanvas(ActiveCanvas.Menu);
			SceneSingleton<DynamicMap>.i.Minimize();
			FlightHud.EnableCanvas(enable: false);
			leaderboardMenu.Open(MenuMode.Leaderboard);
		}
	}

	public void ResumeGame()
	{
		if (_leaderboardMenuOpen)
		{
			_leaderboardMenuOpen = false;
			GameManager.flightControlsEnabled = true;
			CursorManager.SetFlag(CursorFlags.GameMenu, value: false);
			SetActiveCanvas(ActiveCanvas.Gameplay);
			if (SceneSingleton<CameraStateManager>.i.currentState == SceneSingleton<CameraStateManager>.i.cockpitState)
			{
				FlightHud.EnableCanvas(enable: true);
			}
			if (SceneSingleton<CombatHUD>.i.aircraft == null)
			{
				SceneSingleton<DynamicMap>.i.Maximize();
			}
			lastResumed = Time.timeSinceLevelLoad;
		}
	}

	private void OnDestroy()
	{
		_leaderboardMenuOpen = false;
		CursorManager.SetFlag(CursorFlags.GameMenu, value: false);
	}

	public void Update()
	{
		if (GameManager.playerInput.GetButtonDown("Pause") || Input.GetKeyDown(KeyCode.Escape))
		{
			if (leaderboardMenu.gameObject.activeSelf)
			{
				if (!leaderboardMenu.SettingsMenuOpen)
				{
					if (!menuCanvas.enabled)
					{
						SetActiveCanvas(ActiveCanvas.Menu);
						GameManager.flightControlsEnabled = false;
						CursorManager.ForceHidden(hidden: false);
					}
					else if (GameIsPaused)
					{
						CursorManager.ForceHidden(hidden: false);
						leaderboardMenu.Close();
					}
				}
			}
			else if (AllowPauseKeybind && Time.timeSinceLevelLoad - lastResumed > 0.1f)
			{
				lastResumed = Time.timeSinceLevelLoad;
				if (GameManager.gameState == GameState.Multiplayer || GameManager.gameState == GameState.SinglePlayer)
				{
					PauseGame();
				}
			}
		}
		if (hurt.gameObject.activeSelf)
		{
			float a = hurt.color.a;
			a -= 0.002f * Time.deltaTime * Mathf.Max(pilotHitPoints, 10f);
			a = Mathf.Clamp01(a);
			hurt.color = new Color(1f, 1f, 1f, a);
			if (a == 0f)
			{
				hurt.gameObject.SetActive(value: false);
			}
		}
		if (Application.isEditor && GameManager.gameState == GameState.SinglePlayer && Input.GetKeyDown(KeyCode.Y))
		{
			TimeScaleManager.Scale = 4f;
		}
		if (GameManager.gameState == GameState.SinglePlayer && GameManager.playerInput.GetButtonDown("Slow Motion"))
		{
			GameSlowMotion = !GameSlowMotion;
			GameMessage(GameSlowMotion ? "Slow Motion Enabled" : "Slow Motion Disabled");
			if (GameIsPaused)
			{
				return;
			}
			SetTimeFactor(GameSlowMotion ? 0.05f : 1f);
		}
		if (!(SceneSingleton<CombatHUD>.i.aircraft != null) && SceneSingleton<CameraStateManager>.i.currentState != SceneSingleton<CameraStateManager>.i.selectionState)
		{
			int num = 0;
			int num2 = 0;
			if (num2 != 0 || num != 0)
			{
				SwitchSpectatedAircraft(num, num2);
			}
		}
	}

	public void SwitchSpectatedAircraft(int facChange, int pChange)
	{
		List<Aircraft> list = new List<Aircraft>();
		List<Aircraft> list2 = new List<Aircraft>();
		CameraStateManager cam = SceneSingleton<CameraStateManager>.i;
		int num = 0;
		int num2 = 0;
		FactionHQ factionHQ = null;
		/*
		foreach (Unit allUnit in UnitRegistry.allUnits)
		{
			if (allUnit is Aircraft item && (SceneSingleton<DynamicMap>.i.HQ == null || (SceneSingleton<DynamicMap>.i.HQ != null && SceneSingleton<DynamicMap>.i.HQ.IsTargetBeingTracked(allUnit))))
			{
				list.Add(item);
			}
		}*/
		if (list.Count == 0)
		{
			return;
		}
		if (cam.followingUnit != null && cam.followingUnit is Aircraft { NetworkHQ: var networkHQ } aircraft)
		{
			foreach (Aircraft item2 in list)
			{
				if ((facChange == 0 && item2.NetworkHQ == networkHQ) || (facChange != 0 && item2.NetworkHQ != networkHQ))
				{
					list2.Add(item2);
				}
			}
			if (facChange != 0)
			{
				list2 = list2.OrderBy((Aircraft _x) => FastMath.Distance(_x.transform.GlobalPosition(), cam.transform.GlobalPosition())).ToList();
			}
			num = (list2.Contains(aircraft) ? list2.IndexOf(aircraft) : 0);
		}
		else
		{
			list2 = list;
		}
		if (list2.Count != 0)
		{
			num2 = num + pChange;
			if (num2 > list2.Count - 1)
			{
				num2 = 0;
			}
			else if (num2 < 0)
			{
				num2 = list2.Count - 1;
			}
			cam.SetFollowingUnit(list2[num2]);
			SceneSingleton<DynamicMap>.i.DeselectAllIcons();
			SceneSingleton<DynamicMap>.i.SelectIcon(list2[num2]);
		}
	}

	public void SetTimeFactor(float value)
	{
		TimeScaleManager.Scale = value;
	}
}
