using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MessageUI : SceneSingleton<MessageUI>
{
	[SerializeField]
	private int maxLines = 10;

	[SerializeField]
	private float messageRemoveDelayBase = 8f;

	[SerializeField]
	private float killFeedRemoveDelayBase = 5f;

	[SerializeField]
	private float removeDelayPerCharacter = 0.1f;

	[SerializeField]
	private TextMeshProUGUI messageText;

	[SerializeField]
	private TextMeshProUGUI killFeedText;

	[SerializeField]
	private GameObject messageBackground;

	[SerializeField]
	private ContentSizeFitter contentSizeFitter;

	[Header("Chat")]
	[SerializeField]
	private ChatBox chat;

	private bool boxEnabled;

	private Player player;

	private MessageFeed messageFeed;

	private MessageFeed killFeed;

	public static void SetFixedBoxSize()
	{
		SceneSingleton<MessageUI>.i.contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		(SceneSingleton<MessageUI>.i.messageBackground.GetComponent(typeof(RectTransform)) as RectTransform).sizeDelta = new Vector2(0f, 100f);
	}

	public static void SetDynamicBoxSize()
	{
		if (SceneSingleton<MessageUI>.i != null)
		{
			SceneSingleton<MessageUI>.i.contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			LayoutRebuilder.ForceRebuildLayoutImmediate(SceneSingleton<MessageUI>.i.transform as RectTransform);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		messageFeed = new MessageFeed(messageText, maxLines);
		killFeed = new MessageFeed(killFeedText, maxLines);
		boxEnabled = false;
		messageBackground.SetActive(value: false);
		chat.gameObject.SetActive(value: false);
		player = ReInput.players.GetPlayer(0);
		GameManager.OnGameStateChanged.AddListener(OnGameStateChanged);
	}

	private void OnDestroy()
	{
		GameManager.OnGameStateChanged.RemoveListener(OnGameStateChanged);
	}

	private void OnGameStateChanged()
	{
		if (base.gameObject != null)
		{
			base.gameObject.SetActive(GameManager.gameState.IsSingleOrMultiplayer());
		}
	}

	public void GameMessage(string message)
	{
		string[] array = message.Split('\n');
		float removeTime = Time.timeSinceLevelLoad + messageRemoveDelayBase + (float)message.Length * removeDelayPerCharacter;
		for (int i = 0; i < array.Length; i++)
		{
			messageFeed.Enqueue(array[i], removeTime);
		}
	}

	public void KillFeed(string message)
	{
		if (PlayerSettings.killFeedNbLines != 0)
		{
			if (killFeed.NbLines > 5 * PlayerSettings.killFeedNbLines)
			{
				killFeed.Dequeue();
			}
			string[] array = message.Split('\n');
			float removeTime = Time.timeSinceLevelLoad + killFeedRemoveDelayBase + (float)message.Length * removeDelayPerCharacter;
			for (int i = 0; i < array.Length; i++)
			{
				killFeed.Enqueue(array[i], removeTime);
			}
		}
	}

	public async UniTask DelayedGameMessage(string message, float secondsBetweenLine)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		string[] lines = message.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			await UniTask.Delay((int)(secondsBetweenLine * 1000f));
			if (cancel.IsCancellationRequested)
			{
				break;
			}
			float removeTime = Time.timeSinceLevelLoad + messageRemoveDelayBase + (float)message.Length * removeDelayPerCharacter;
			messageFeed.Enqueue(lines[i], removeTime);
		}
	}

	private void LateUpdate()
	{
		CheckBoxEnabled();
		CheckChatBox();
		float timeSinceLevelLoad = Time.timeSinceLevelLoad;
		messageFeed.Update(timeSinceLevelLoad);
		killFeed.Update(timeSinceLevelLoad);
	}

	private void CheckBoxEnabled()
	{
		if (boxEnabled && ShouldHide())
		{
			boxEnabled = false;
			messageBackground.SetActive(value: false);
			chat.gameObject.SetActive(value: false);
		}
		else if (!boxEnabled && !ShouldHide())
		{
			boxEnabled = true;
			messageBackground.SetActive(value: true);
		}
	}

	private bool ShouldHide()
	{
		if (!PlayerSettings.cinematicMode)
		{
			if (messageFeed.NoText && killFeed.NoText)
			{
				return !chat.gameObject.activeSelf;
			}
			return false;
		}
		return true;
	}

	private void CheckChatBox()
	{
		if (ChatBox.ChatAllowed && !PlayerSettings.cinematicMode && !InputFieldChecker.InsideInputField && player.GetButtonDown("Open Chat"))
		{
			chat.gameObject.SetActive(value: true);
		}
	}
}
