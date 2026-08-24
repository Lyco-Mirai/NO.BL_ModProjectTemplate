using System;
using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	[Serializable]
	public class LeaderboardFactionItem : MonoBehaviour
	{
		private enum FactionMode
		{
			PreventJoin = 0,
			Joinable = 1,
			Score = 2
		}

		[Header("Join Menu")]
		[SerializeField]
		private GameObject JoinHeader;

		[SerializeField]
		private Button JoinButton;

		[SerializeField]
		private TextMeshProUGUI JoinTitleText;

		[SerializeField]
		private TextMeshProUGUI JoinScoreText;

		[Header("Score Menu")]
		[SerializeField]
		private GameObject ScoreHeader;

		[SerializeField]
		private TextMeshProUGUI ScoreTitleText;

		[SerializeField]
		private TextMeshProUGUI ScoreText;

		[SerializeField]
		private Image FactionFlag;

		[Header("Player list")]
		[SerializeField]
		private Transform PlayerListContent;

		public RectTransform ScrollView;

		[SerializeField]
		private LeaderboardPlayerEntry entryPrefab;

		[Header("Leaderboard References")]
		[SerializeField]
		private LeaderboardMenu leaderboardMenu;

		[SerializeField]
		private LeaderBoardRightClickMenu rightClickMenu;

		private bool hasSetup;

		private MenuMode menuMode;

		private FactionMode factionMode;

		private List<LeaderboardPlayerEntry> allEntries;

		private List<LeaderboardPlayerEntry> activeEntries;

		private float lastFactionScore = -1f;

		public FactionHQ HQ { get; private set; }

		private void Awake()
		{
			Setup();
		}

		private void Setup()
		{
			if (hasSetup)
			{
				return;
			}
			hasSetup = true;
			JoinButton.onClick.AddListener(OnJoinClicked);
			allEntries = new List<LeaderboardPlayerEntry>();
			activeEntries = new List<LeaderboardPlayerEntry>();
			allEntries.AddRange(PlayerListContent.GetComponentsInChildren<LeaderboardPlayerEntry>());
			foreach (LeaderboardPlayerEntry allEntry in allEntries)
			{
				allEntry.GetComponent<RightClickDropdownMenuTrigger>().Target = rightClickMenu;
				allEntry.gameObject.SetActive(value: false);
			}
		}

		public void SetupNoHQ()
		{
			JoinHeader.SetActive(value: false);
			ScoreHeader.SetActive(value: false);
		}

		public void SetupHq(FactionHQ hq)
		{
			Setup();
			HQ = hq;
			HQ.onPreventJoinChanged += RefreshMode;
			string factionExtendedName = HQ.faction.factionExtendedName;
			JoinTitleText.text = factionExtendedName;
			ScoreTitleText.text = factionExtendedName;
			FactionFlag.sprite = HQ.faction.factionColorLogo;
			UpdateFactionScore(forceUpdate: true);
			RefreshMode();
			FixLayout.ForceRebuildAtEndOfFrame(leaderboardMenu.transform.AsRectTransform());
		}

		private void OnDestroy()
		{
			if (HQ != null)
			{
				HQ.onPreventJoinChanged -= RefreshMode;
			}
		}

		public void SetMenuMode(MenuMode mode)
		{
			menuMode = mode;
			RefreshMode();
		}

		private void RefreshMode()
		{
			FactionMode factionMode = (this.factionMode = ((!(HQ == null)) ? ((menuMode != MenuMode.Leaderboard) ? ((!HQ.preventJoin) ? FactionMode.Joinable : FactionMode.PreventJoin) : FactionMode.Score) : FactionMode.PreventJoin));
			JoinHeader.SetActive(factionMode == FactionMode.Joinable);
			ScoreHeader.SetActive(factionMode == FactionMode.Score);
			UpdateFactionScore(forceUpdate: true);
		}

		private void UpdateFactionScore(bool forceUpdate = false)
		{
			if (factionMode != FactionMode.PreventJoin && (forceUpdate || Mathf.Abs(HQ.factionScore - lastFactionScore) > 0.1f))
			{
				lastFactionScore = HQ.factionScore;
				string text = $"{HQ.factionScore:F1}";
				if (factionMode == FactionMode.Joinable)
				{
					JoinScoreText.text = text;
				}
				else
				{
					ScoreText.text = text;
				}
			}
		}

		private void OnJoinClicked()
		{
			if (factionMode != FactionMode.Joinable)
			{
				Debug.LogError("Join button pressed but FactionItem was not in Mode.Joinable");
			}
			else
			{
				leaderboardMenu.JoinFactionCallback(HQ);
			}
		}

		private void Update()
		{
			if (activeEntries == null)
			{
				return;
			}
			foreach (LeaderboardPlayerEntry activeEntry in activeEntries)
			{
				activeEntry.UpdateScore();
			}
			UpdateFactionScore();
		}

		private LeaderboardPlayerEntry GetEmptyEntry()
		{
			int count = activeEntries.Count;
			if (count < allEntries.Count)
			{
				return allEntries[count];
			}
			LeaderboardPlayerEntry leaderboardPlayerEntry = UnityEngine.Object.Instantiate(entryPrefab, PlayerListContent);
			leaderboardPlayerEntry.GetComponent<RightClickDropdownMenuTrigger>().Target = rightClickMenu;
			allEntries.Add(leaderboardPlayerEntry);
			return leaderboardPlayerEntry;
		}

		public int DisplayPlayers()
		{
			if (HQ == null)
			{
				return 0;
			}
			foreach (LeaderboardPlayerEntry activeEntry in activeEntries)
			{
				activeEntry.gameObject.SetActive(value: false);
			}
			activeEntries.Clear();
			List<Player> players = HQ.GetPlayers(sortByScore: true);
			int num = 0;
			foreach (Player item in players)
			{
				if (!(item == null))
				{
					num++;
					LeaderboardPlayerEntry emptyEntry = GetEmptyEntry();
					activeEntries.Add(emptyEntry);
					emptyEntry.gameObject.SetActive(value: true);
					emptyEntry.Setup(item);
				}
			}
			UpdateFactionScore();
			return activeEntries.Count;
		}
	}
}
