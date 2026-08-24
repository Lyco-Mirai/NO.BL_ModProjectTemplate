using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuitMissionButton : ButtonController
{
	[SerializeField]
	private TextMeshProUGUI buttonText;

	[SerializeField]
	private string normalText;

	[SerializeField]
	private string returnToEditorText;

	[SerializeField]
	private bool confirmSafetyIfOtherPlayers;

	[SerializeField]
	private Button confirmLeaveButton;

	[SerializeField]
	private GameObject confirmLeavePanel;

	[SerializeField]
	private TextMeshProUGUI leaveWarning;

	private bool quitting;

	protected override void Awake()
	{
		base.Awake();
		if (confirmSafetyIfOtherPlayers)
		{
			confirmLeaveButton.onClick.AddListener(onClick);
		}
	}

	private void OnEnable()
	{
		buttonText.text = (GameManager.IsPlayingFromEditor ? returnToEditorText : normalText);
	}

	protected override void onClick()
	{
		if (!quitting)
		{
			if (GameManager.IsPlayingFromEditor)
			{
				quitting = true;
				MissionEditor.ReturnToEditor().Forget();
			}
			else if (confirmSafetyIfOtherPlayers)
			{
				TryQuitGame();
			}
			else
			{
				QuitGame();
			}
		}
	}

	public void TryQuitGame()
	{
		if (NetworkManagerNuclearOption.HasOtherPlayers(out var numberOfPlayers))
		{
			if (!confirmLeavePanel.activeSelf)
			{
				confirmLeavePanel.SetActive(value: true);
				leaveWarning.text = $"Are you sure you want to end the game? Doing so will kick {numberOfPlayers - 1} other players.";
			}
			else
			{
				HostQuit();
			}
		}
		else
		{
			QuitGame();
		}
	}

	private void HostQuit()
	{
		quitting = true;
		NetworkManagerNuclearOption.i.Server.SendToAll(default(HostEndedMessage), authenticatedOnly: false, excludeLocalPlayer: true);
		UniTask.Void(async delegate
		{
			await UniTask.Delay(100);
			QuitGame();
		});
	}

	private void QuitGame()
	{
		quitting = true;
		if (SceneSingleton<GameplayUI>.i != null)
		{
			SceneSingleton<GameplayUI>.i.ResumeGame();
		}
		NetworkManagerNuclearOption.i.Stop(setDisconnectReason: true);
	}
}
