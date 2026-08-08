using Cysharp.Threading.Tasks;
using Rewired;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking
{
	public class VoteKickUI : SceneSingleton<VoteKickUI>
	{
		private enum UIState
		{
			Hidden = 0,
			ActiveVote = 1,
			Resolution = 2
		}

		[Header("UI Elements")]
		[SerializeField]
		private GameObject root;

		[SerializeField]
		private TextMeshProUGUI targetNameText;

		[SerializeField]
		private TextMeshProUGUI yesCountText;

		[SerializeField]
		private TextMeshProUGUI noCountText;

		[SerializeField]
		private Slider timerBar;

		[SerializeField]
		private TextMeshProUGUI timerText;

		[SerializeField]
		private TextMeshProUGUI votedText;

		[Header("Vote Bar")]
		[SerializeField]
		private RectTransform yesFill;

		[SerializeField]
		private RectTransform noFill;

		[SerializeField]
		private RectTransform thresholdMarker;

		[SerializeField]
		private TextMeshProUGUI thresholdText;

		[Header("Voting Controls")]
		[SerializeField]
		private Button yesButton;

		[SerializeField]
		private Button noButton;

		[SerializeField]
		private TextMeshProUGUI yesButtonText;

		[SerializeField]
		private TextMeshProUGUI noButtonText;

		[SerializeField]
		private GameObject buttonsParent;

		[SerializeField]
		private GameObject timerParent;

		private Rewired.Player player;

		private bool _hasVoted;

		private UIState _uiState;

		private VoteKickState _state;

		private float _resolvedTime;

		private float _voteDuration;

		private float _localRequestCooldownEnd;

		public bool IsLocalRequestOnCooldown => Time.time < _localRequestCooldownEnd;

		public float LocalRequestCooldownRemaining => Mathf.Max(0f, _localRequestCooldownEnd - Time.time);

		public bool HasVoted => _hasVoted;

		private void OnValidate()
		{
		}

		protected override void Awake()
		{
			base.Awake();
			root.SetActive(value: false);
			if (GameManager.IsHeadless)
			{
				base.enabled = false;
				return;
			}
			VoteKickManager.OnVoteKickStateChanged += SetState;
			yesButton.onClick.AddListener(delegate
			{
				CastVote(yes: true);
			});
			noButton.onClick.AddListener(delegate
			{
				CastVote(yes: false);
			});
			SetupInputs();
		}

		private void SetupInputs()
		{
			player = ReInput.players.GetPlayer(0);
			ActionElementMap firstElementMapWithAction = player.controllers.maps.GetFirstElementMapWithAction("VoteKickYes", skipDisabledMaps: false);
			if (firstElementMapWithAction != null)
			{
				yesButtonText.text = "YES [" + firstElementMapWithAction.elementIdentifierName + "]";
			}
			else
			{
				yesButtonText.text = "YES";
			}
			ActionElementMap firstElementMapWithAction2 = player.controllers.maps.GetFirstElementMapWithAction("VoteKickNo", skipDisabledMaps: false);
			if (firstElementMapWithAction2 != null)
			{
				noButtonText.text = "NO [" + firstElementMapWithAction2.elementIdentifierName + "]";
			}
			else
			{
				noButtonText.text = "NO";
			}
		}

		private void OnDestroy()
		{
			VoteKickManager.OnVoteKickStateChanged -= SetState;
		}

		public void SetState(VoteKickState state)
		{
			bool active = _state.Active;
			if (_state.VoteID != state.VoteID)
			{
				_hasVoted = false;
			}
			_state = state;
			if (state.Active)
			{
				UpdateUI_ShowVote(state);
			}
			else if (active)
			{
				_resolvedTime = Time.time;
				UpdateUI_ShowResolved(state);
			}
			else if (_uiState == UIState.Resolution && IsShowingResolution())
			{
				UpdateUI_ShowResolved(state);
			}
		}

		private void UpdateUI_ShowVote(VoteKickState state)
		{
			_uiState = UIState.ActiveVote;
			root.SetActive(value: true);
			_voteDuration = NetworkSceneSingleton<VoteKickManager>.i.ClientConfig.VoteDuration;
			string displayName = Player.GetPlayerNameBySteamID(state.TargetID).GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
			targetNameText.text = displayName;
			UpdateVoteCounts(state);
			UpdateVoteBar(state);
			timerParent.SetActive(value: true);
			double num = Mathf.Max(0f, (float)(state.VoteEndTimeNetwork - NetworkSceneSingleton<VoteKickManager>.i.NetworkTime.Time));
			timerBar.value = (float)num / _voteDuration;
			timerText.text = $"{Mathf.CeilToInt((float)num)}s";
			RefreshHasVotedUI();
		}

		private void RefreshHasVotedUI()
		{
			bool flag = _state.TargetID == SteamUser.GetSteamID();
			if (_hasVoted || flag)
			{
				buttonsParent.SetActive(value: false);
				votedText.gameObject.SetActive(value: true);
				votedText.text = (flag ? "You are being vote-kicked" : "Waiting for vote");
			}
			else
			{
				buttonsParent.SetActive(value: true);
				votedText.gameObject.SetActive(value: false);
			}
		}

		private void UpdateUI_ShowResolved(VoteKickState state)
		{
			_uiState = UIState.Resolution;
			string displayName = Player.GetPlayerNameBySteamID(state.TargetID).GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
			targetNameText.text = displayName;
			yesCountText.text = $"{state.YesVotes} YES";
			noCountText.text = $"{state.NoVotes} NO";
			UpdateVoteBar(state);
			votedText.text = (state.Passed ? "VOTE PASSED" : "VOTE FAILED");
			timerParent.SetActive(value: false);
			buttonsParent.SetActive(value: false);
			votedText.gameObject.SetActive(value: true);
		}

		private void UpdateVoteCounts(VoteKickState state)
		{
			yesCountText.text = $"{state.YesVotes} YES";
			noCountText.text = $"{state.NoVotes} NO";
		}

		private void UpdateVoteBar(VoteKickState state)
		{
			if (state.EligibleVoters > 0)
			{
				float num = 1f / (float)state.EligibleVoters;
				yesFill.anchorMin = new Vector2(0f, 0f);
				yesFill.anchorMax = new Vector2((float)state.YesVotes * num, 1f);
				yesFill.anchoredPosition = Vector2.zero;
				yesFill.sizeDelta = Vector2.zero;
				noFill.anchorMin = new Vector2(1f - (float)state.NoVotes * num, 0f);
				noFill.anchorMax = new Vector2(1f, 1f);
				noFill.anchoredPosition = Vector2.zero;
				noFill.sizeDelta = Vector2.zero;
				float x = (float)state.VotesRequired * num;
				thresholdMarker.anchorMin = new Vector2(x, thresholdMarker.anchorMin.y);
				thresholdMarker.anchorMax = new Vector2(x, thresholdMarker.anchorMax.y);
				thresholdMarker.anchoredPosition = Vector2.zero;
				thresholdText.text = $"Required: {state.VotesRequired}";
			}
		}

		private void UpdateUI_Hide()
		{
			_uiState = UIState.Hidden;
			_state = default(VoteKickState);
			root.SetActive(value: false);
		}

		private bool IsShowingResolution()
		{
			float resolutionDisplayTime = NetworkSceneSingleton<VoteKickManager>.i.ClientConfig.ResolutionDisplayTime;
			return Time.time - _resolvedTime < resolutionDisplayTime;
		}

		private void Update()
		{
			if (!root.activeSelf)
			{
				return;
			}
			if (_uiState == UIState.Resolution)
			{
				if (!IsShowingResolution())
				{
					UpdateUI_Hide();
				}
			}
			else
			{
				if (_uiState != UIState.ActiveVote)
				{
					return;
				}
				float num = Mathf.Max(0f, (float)(_state.VoteEndTimeNetwork - NetworkSceneSingleton<VoteKickManager>.i.NetworkTime.Time));
				if (timerParent.activeSelf)
				{
					timerBar.value = num / _voteDuration;
					timerText.text = $"{Mathf.CeilToInt(num)}s";
				}
				bool flag = _state.TargetID == SteamUser.GetSteamID();
				if (!_hasVoted && !flag)
				{
					if (player.GetButtonDown("VoteKickYes"))
					{
						CastVote(yes: true);
					}
					else if (player.GetButtonDown("VoteKickNo"))
					{
						CastVote(yes: false);
					}
				}
			}
		}

		public void CastVote(bool yes)
		{
			if (_state.TargetID == default(CSteamID))
			{
				ColorLog<VoteKickUI>.InfoWarn("Cast Voted called but there was no active vote");
				return;
			}
			if (_state.TargetID == SteamUser.GetSteamID())
			{
				ColorLog<VoteKickUI>.InfoWarn("Cannot vote on a vote kick targeting yourself");
				return;
			}
			NetworkSceneSingleton<VoteKickManager>.i.CastVote(_state.TargetID, yes);
			_hasVoted = true;
			if (_uiState == UIState.ActiveVote)
			{
				RefreshHasVotedUI();
			}
		}

		public void RequestVoteKickLocal(Player targetPlayer)
		{
			RequestVoteKickLocalAsync(targetPlayer).Forget();
		}

		private async UniTaskVoid RequestVoteKickLocalAsync(Player targetPlayer)
		{
			if (targetPlayer == null)
			{
				Debug.LogWarning("RequestVoteKickLocal ignored: Target player is null.");
				return;
			}
			if (targetPlayer.IsHostPlayer)
			{
				Debug.LogWarning("RequestVoteKickLocal ignored: Cannot vote kick the host.");
				return;
			}
			if (IsLocalRequestOnCooldown)
			{
				Debug.LogWarning($"RequestVoteKickLocal ignored: Local request cooldown active. Remaining: {LocalRequestCooldownRemaining:F1}s.");
				return;
			}
			_localRequestCooldownEnd = Time.time + NetworkSceneSingleton<VoteKickManager>.i.ClientConfig.RequesterCooldown;
			VoteKickState? voteKickState = await NetworkSceneSingleton<VoteKickManager>.i.RequestVoteKick(targetPlayer.CSteamID);
			if (voteKickState.HasValue)
			{
				SetState(voteKickState.Value);
				_hasVoted = true;
				RefreshHasVotedUI();
			}
			else
			{
				_localRequestCooldownEnd = 0f;
			}
		}
	}
}
