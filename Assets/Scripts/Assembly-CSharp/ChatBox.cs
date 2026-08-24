using Cysharp.Threading.Tasks;
using NuclearOption.Chat;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBox : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField input;

	[SerializeField]
	private Button sendButton;

	[SerializeField]
	private Toggle alliesOnlyToggle;

	private bool sendEnabled;

	private bool allyToggleEnabled;

	private Player player;

	public static bool ChatAllowed
	{
		get
		{
			if (GameManager.gameState == GameState.Multiplayer)
			{
				return PlayerSettings.chatEnabled;
			}
			return false;
		}
	}

	private void Awake()
	{
		sendButton.onClick.AddListener(SendChat);
		input.SetTextWithoutNotify(string.Empty);
		alliesOnlyToggle.isOn = false;
		EnableToggle(hasFaction: true);
		player = ReInput.players.GetPlayer(0);
	}

	private void OnEnable()
	{
		if (!ChatAllowed)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		CursorManager.SetFlag(CursorFlags.Chat, value: true);
		foreach (ControllerMap allMap in player.controllers.maps.GetAllMaps(ControllerType.Keyboard))
		{
			allMap.enabled = false;
		}
		foreach (ControllerMap allMap2 in player.controllers.maps.GetAllMaps(ControllerType.Mouse))
		{
			allMap2.enabled = false;
		}
		GameplayUI.AllowPauseKeybind = false;
		EnableSend(canSend: false);
	}

	public void OnDisable()
	{
		CursorManager.SetFlag(CursorFlags.Chat, value: false);
		WaitToReEnableKeyboard().Forget();
	}

	private async UniTaskVoid WaitToReEnableKeyboard()
	{
		await UniTask.Delay(100);
		foreach (ControllerMap allMap in player.controllers.maps.GetAllMaps(ControllerType.Keyboard))
		{
			allMap.enabled = true;
		}
		foreach (ControllerMap allMap2 in player.controllers.maps.GetAllMaps(ControllerType.Mouse))
		{
			allMap2.enabled = true;
		}
		GameplayUI.AllowPauseKeybind = true;
	}

	public void SendChat()
	{
		if (ChatManager.CanSend(input.text, checkAsServer: false, realSend: false))
		{
			ChatManager.SendChatMessage(input.text, !alliesOnlyToggle.isOn);
			CloseChat();
		}
	}

	private void CloseChat()
	{
		base.gameObject.SetActive(value: false);
		input.SetTextWithoutNotify(string.Empty);
	}

	private void Update()
	{
		if (player.GetButtonDown("Cancel Chat") || Input.GetKeyDown(KeyCode.Escape))
		{
			CloseChat();
			return;
		}
		bool flag = ChatManager.CanSend(input.text, checkAsServer: false, realSend: false);
		if (flag != sendEnabled)
		{
			EnableSend(flag);
		}
		Faction localFaction2;
		bool localFaction = GameManager.GetLocalFaction(out localFaction2);
		if (localFaction != allyToggleEnabled)
		{
			EnableToggle(localFaction);
		}
		if ((flag && player.GetButtonDown("Submit Chat")) || Input.GetKeyDown(KeyCode.Return))
		{
			SendChat();
		}
		input.ActivateInputField();
	}

	private void EnableSend(bool canSend)
	{
		sendButton.interactable = canSend;
		sendEnabled = canSend;
	}

	private void EnableToggle(bool hasFaction)
	{
		alliesOnlyToggle.gameObject.SetActive(hasFaction);
		allyToggleEnabled = hasFaction;
	}
}
