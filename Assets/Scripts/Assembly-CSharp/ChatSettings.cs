using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatSettings : MonoBehaviour
{
	[Header("Chat Settings")]
	[SerializeField]
	private Toggle chatToggle;

	[SerializeField]
	private Toggle filterToggle;

	[SerializeField]
	private Toggle ttsToggle;

	[SerializeField]
	private Slider ttsSpeed;

	[SerializeField]
	private Slider ttsVolume;

	[SerializeField]
	private Button testTts;

	[Header("Display Settings")]
	[SerializeField]
	private TMP_Dropdown playerIndexDisplayDropdown;

	[SerializeField]
	private TMP_Dropdown serverTagDisplayDropdown;

	[Header("Activity Settings")]
	[SerializeField]
	private Toggle steamRichPresenceToggle;

	[SerializeField]
	private Toggle discordRichPresenceToggle;

	public void Awake()
	{
		chatToggle.onValueChanged.AddListener(ChatToggleChanged);
		filterToggle.onValueChanged.AddListener(FilterToggleChanged);
		ttsToggle.onValueChanged.AddListener(TtsToggleChanged);
		steamRichPresenceToggle.onValueChanged.AddListener(SteamRichPresenceToggleChanged);
		discordRichPresenceToggle.onValueChanged.AddListener(DiscordRichPresenceToggleChanged);
		playerIndexDisplayDropdown.onValueChanged.AddListener(PlayerIndexDisplayChanged);
		playerIndexDisplayDropdown.SetValueWithoutNotify((int)PlayerSettings.playerIndexDisplay);
		serverTagDisplayDropdown.onValueChanged.AddListener(ServerTagDisplayChanged);
		serverTagDisplayDropdown.SetValueWithoutNotify((int)PlayerSettings.serverTagDisplay);
		ttsSpeed.minValue = -10f;
		ttsSpeed.maxValue = 10f;
		ttsSpeed.wholeNumbers = true;
		ttsSpeed.onValueChanged.AddListener(TtsSpeedChanged);
		ttsVolume.minValue = 0f;
		ttsVolume.maxValue = 100f;
		ttsVolume.wholeNumbers = true;
		ttsVolume.onValueChanged.AddListener(TtsVolumeChanged);
		testTts.onClick.AddListener(TestTts);
		chatToggle.SetIsOnWithoutNotify(PlayerSettings.chatEnabled);
		filterToggle.SetIsOnWithoutNotify(PlayerSettings.chatFilter);
		ttsToggle.SetIsOnWithoutNotify(PlayerSettings.chatTts);
		ttsSpeed.SetValueWithoutNotify(PlayerSettings.chatTtsSpeed);
		ttsVolume.SetValueWithoutNotify(PlayerSettings.chatTtsVolume);
		steamRichPresenceToggle.SetIsOnWithoutNotify(PlayerSettings.steamRichPresenceEnabled);
		discordRichPresenceToggle.SetIsOnWithoutNotify(PlayerSettings.discordRichPresenceEnabled);
	}

	private void SteamRichPresenceToggleChanged(bool on)
	{
		PlayerSettings.steamRichPresenceEnabled = on;
		PlayerPrefs.SetInt("SteamRichPresenceEnabled", on ? 1 : 0);
		RichPresenceManager.TriggerUpdate();
	}

	private void DiscordRichPresenceToggleChanged(bool on)
	{
		PlayerSettings.discordRichPresenceEnabled = on;
		PlayerPrefs.SetInt("DiscordRichPresenceEnabled", on ? 1 : 0);
		RichPresenceManager.TriggerUpdate();
	}

	private void ChatToggleChanged(bool on)
	{
		PlayerSettings.chatEnabled = on;
		PlayerPrefs.SetInt("ChatEnabled", on ? 1 : 0);
	}

	private void FilterToggleChanged(bool on)
	{
		PlayerSettings.chatFilter = on;
		PlayerPrefs.SetInt("ChatFilter", on ? 1 : 0);
		PlayerName.RebuildAllPlayerNameCaches();
	}

	private void TtsToggleChanged(bool on)
	{
		PlayerSettings.chatTts = on;
		PlayerPrefs.SetInt("ChatTts", on ? 1 : 0);
	}

	private void TtsSpeedChanged(float speed)
	{
		PlayerSettings.chatTtsSpeed = (int)speed;
		PlayerPrefs.SetInt("ChatTtsSpeed", (int)speed);
	}

	private void TtsVolumeChanged(float volume)
	{
		PlayerSettings.chatTtsVolume = (int)volume;
		PlayerPrefs.SetInt("ChatTtsVolume", (int)volume);
	}

	private void PlayerIndexDisplayChanged(int value)
	{
		PlayerSettings.playerIndexDisplay = (PlayerNameDisplayScope)value;
		PlayerPrefs.SetInt("PlayerIndexDisplay", value);
		PlayerName.RebuildAllPlayerNameCaches();
	}

	private void ServerTagDisplayChanged(int value)
	{
		PlayerSettings.serverTagDisplay = (PlayerNameDisplayScope)value;
		PlayerPrefs.SetInt("ServerTagDisplay", value);
		PlayerName.RebuildAllPlayerNameCaches();
	}

	private void TestTts()
	{
		UniTask.Void(async delegate
		{
			await WindowsTTS.SpeakAsync(PlayerSettings.chatTtsSpeed, PlayerSettings.chatTtsVolume, "Player said: Welcome to Nuclear Option!", PlayerSettings.chatFilter);
		});
	}
}
