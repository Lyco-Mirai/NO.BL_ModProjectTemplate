using System;
using System.Text;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class InviteJoinModal : MonoBehaviour
	{
		[SerializeField]
		private GameObject modalRoot;

		[SerializeField]
		private TextMeshProUGUI bodyText;

		[SerializeField]
		private Button confirmButton;

		[SerializeField]
		private Button cancelButton;

		private Action _pendingJoinAction;

		private readonly StringBuilder _sb = new StringBuilder();

		public static InviteJoinModal Instance { get; private set; }

		private void Awake()
		{
			confirmButton.onClick.AddListener(OnConfirm);
			cancelButton.onClick.AddListener(Hide);
			modalRoot.SetActive(value: false);
		}

		public static void TryJoin(Action joinAction)
		{
			GameState gameState = GameManager.gameState;
			if (gameState == GameState.Uninitialized || (uint)(gameState - 4) <= 1u)
			{
				joinAction?.Invoke();
				return;
			}
			if (Instance == null)
			{
				GameObject obj = UnityEngine.Object.Instantiate((GameObject)Resources.Load("InviteJoinModalCanvas"));
				Instance = obj.GetComponent<InviteJoinModal>();
				UnityEngine.Object.DontDestroyOnLoad(obj);
			}
			Instance.Open(joinAction);
		}

		private void Open(Action joinAction)
		{
			_pendingJoinAction = joinAction;
			_sb.Clear();
			int numberOfPlayers;
			if (GameManager.gameState == GameState.Editor)
			{
				_sb.Append("Leave the Mission Editor and join friend?\n\n");
				_sb.Append("<size=80%><color=#999999>A temporary auto-save will be created.</color></size>");
			}
			else if (NetworkManagerNuclearOption.HasOtherPlayers(out numberOfPlayers))
			{
				_sb.Append("Leave current game and join friend?\n\n");
				_sb.Append($"<size=80%><color=#ff6666>This will end the game and kick {numberOfPlayers - 1} other players.</color></size>");
			}
			else
			{
				_sb.Append("Leave current game and join friend?");
			}
			bodyText.text = _sb.ToString();
			modalRoot.SetActive(value: true);
		}

		public void Hide()
		{
			modalRoot.SetActive(value: false);
			_pendingJoinAction = null;
		}

		private void OnConfirm()
		{
			Action pendingJoinAction = _pendingJoinAction;
			Hide();
			if (pendingJoinAction != null)
			{
				ExecuteJoin(pendingJoinAction).Forget();
			}
		}

		private async UniTaskVoid ExecuteJoin(Action action)
		{
			if (GameManager.gameState != GameState.Editor)
			{
				await NetworkManagerNuclearOption.i.StopAsync(setDisconnectReason: true);
			}
			else
			{
				await MissionEditor.ExitEditor();
			}
			await UniTask.Yield();
			action();
		}
	}
}
