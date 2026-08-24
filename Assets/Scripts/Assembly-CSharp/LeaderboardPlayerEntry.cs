using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPlayerEntry : MonoBehaviour
{
	private static readonly string mutedText = "<size=80%><color=#AAA>(M)</color></size>";

	private static readonly string blockedText = "<size=80%><color=#FAA>(B)</color></size>";

	[Header("Player Stats")]
	[SerializeField]
	private TextMeshProUGUI textName;

	[SerializeField]
	private TextMeshProUGUI textRank;

	[SerializeField]
	private TextMeshProUGUI textScore;

	[Header("Vote Kick Highlight")]
	[SerializeField]
	private Color voteKickedNameColor = Color.red;

	[SerializeField]
	private Color voteKickedBgColor = new Color(1f, 0f, 0f, 0.2f);

	[SerializeField]
	private Image backgroundImage;

	[Header("Vote Kick Buttons")]
	[SerializeField]
	private GameObject voteButtonsParent;

	[SerializeField]
	private Button voteYesButton;

	[SerializeField]
	private Button voteNoButton;

	private bool muted;

	private bool blocked;

	private string playerNameCached;

	private string previousPrefix;

	private int previousRank;

	private float previousScore;

	private Color defaultNameColor;

	private Color defaultBgColor;

	private bool previousIsVoteKicked;

	private bool previousShowButtons;

	private bool playerNameEventAdded;

	public Player Player { get; private set; }

	private void OnValidate()
	{
	}

	private void Awake()
	{
		defaultNameColor = textName.color;
		defaultBgColor = backgroundImage.color;
		voteYesButton.onClick.AddListener(delegate
		{
			CastVote(yes: true);
		});
		voteNoButton.onClick.AddListener(delegate
		{
			CastVote(yes: false);
		});
		voteButtonsParent.SetActive(value: false);
	}

	public void Setup(Player player)
	{
		if (playerNameEventAdded && Player != null)
		{
			Player.OnNameResolved.RemoveListener(OnNameResolved);
		}
		Player = player;
		Player.OnNameResolved.AddListener(OnNameResolved);
		playerNameEventAdded = true;
		playerNameCached = Player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
		UpdateBlockMute(forceUpdate: true);
	}

	private void OnNameResolved(PlayerName playerName)
	{
		Player.OnNameResolved.RemoveListener(OnNameResolved);
		playerNameEventAdded = false;
		playerNameCached = playerName.GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
		UpdateScore(forceUpdate: true);
	}

	private void OnDisable()
	{
		if (playerNameEventAdded && Player != null)
		{
			Player.OnNameResolved.RemoveListener(OnNameResolved);
			Player = null;
		}
		playerNameEventAdded = false;
	}

	public void UpdateBlockMute(bool forceUpdate = false)
	{
		muted = GameManager.MutedList.Contains(Player);
		blocked = GameManager.BlockList.Contains(Player);
		UpdateScore(forceUpdate);
	}

	public void UpdateScore(bool forceUpdate = false)
	{
		if (!(Player == null))
		{
			string text = (blocked ? blockedText : ((!muted) ? string.Empty : mutedText));
			if (forceUpdate || previousPrefix != text)
			{
				textName.text = text + " " + playerNameCached;
				previousPrefix = text;
			}
			if (forceUpdate || previousRank != Player.PlayerRank)
			{
				textRank.text = $"{Player.PlayerRank:F0}";
				previousRank = Player.PlayerRank;
			}
			if (forceUpdate || previousScore != Player.PlayerScore)
			{
				textScore.text = $"{Player.PlayerScore:F1}";
				previousScore = Player.PlayerScore;
			}
			bool flag = NetworkSceneSingleton<VoteKickManager>.i.State.IsVoteActive(Player.CSteamID);
			if (forceUpdate || previousIsVoteKicked != flag)
			{
				textName.color = (flag ? voteKickedNameColor : defaultNameColor);
				backgroundImage.color = (flag ? voteKickedBgColor : defaultBgColor);
				previousIsVoteKicked = flag;
			}
			bool flag2 = flag && !SceneSingleton<VoteKickUI>.i.HasVoted && !GameManager.IsLocalPlayer(Player);
			if (forceUpdate || previousShowButtons != flag2)
			{
				voteButtonsParent.SetActive(flag2);
				previousShowButtons = flag2;
			}
		}
	}

	private void CastVote(bool yes)
	{
		if (!(Player == null) && !GameManager.IsLocalPlayer(Player) && NetworkSceneSingleton<VoteKickManager>.i != null && NetworkSceneSingleton<VoteKickManager>.i.State.IsVoteActive(Player.CSteamID) && SceneSingleton<VoteKickUI>.i != null)
		{
			SceneSingleton<VoteKickUI>.i.CastVote(yes);
		}
	}
}
