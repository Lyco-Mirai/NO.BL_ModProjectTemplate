using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class LobbyPasswordPromptSingleton : MonoBehaviour
	{
		private static LobbyPasswordPromptSingleton instance;

		[SerializeField]
		private GameObject panel;

		[SerializeField]
		private TMP_InputField inputField;

		[SerializeField]
		private Button submitButton;

		[SerializeField]
		private Button cancelButton;

		private LobbyInstance lobby;

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
				submitButton.onClick.AddListener(OnSubmit);
				inputField.onSubmit.AddListener(delegate
				{
					OnSubmit();
				});
				inputField.onValueChanged.AddListener(delegate(string txt)
				{
					submitButton.interactable = !string.IsNullOrEmpty(txt);
				});
				cancelButton.onClick.AddListener(OnCancel);
			}
			else
			{
				Debug.LogError("2 LobbyPasswordPrompt existed at once");
			}
		}

		public static void ShowPrompt(LobbyInstance lobby)
		{
			if (instance == null)
			{
				Object.Instantiate(GameAssets.i.lobbyPasswordPrompt);
			}
			instance.ShowPromptInternal(lobby);
		}

		private void ShowPromptInternal(LobbyInstance lobby)
		{
			panel.SetActive(value: true);
			inputField.text = "";
			this.lobby = lobby;
		}

		private void OnSubmit()
		{
			string text = inputField.text;
			if (string.IsNullOrEmpty(text))
			{
				Debug.LogWarning("ignoring password submit because it was empty");
				return;
			}
			SteamLobby.instance.TryJoinLobby(lobby, text, promptIfPasswordNeeded: false);
			panel.SetActive(value: false);
		}

		private void OnCancel()
		{
			panel.SetActive(value: false);
		}
	}
}
