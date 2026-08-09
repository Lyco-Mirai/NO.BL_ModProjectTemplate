using NuclearOption.DedicatedServer;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.Networking.Authentication;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.UI
{
	public class LeaderBoardRightClickMenu : RightClickDropdownMenuBase
	{
		[Header("Dropdown Buttons")]
		[SerializeField]
		private Button showProfileButton;

		[Space]
		[SerializeField]
		private Button muteButton;

		[SerializeField]
		private TextMeshProUGUI muteButtonText;

		[SerializeField]
		private string muteString = "Mute";

		[SerializeField]
		private string unmuteString = "Unmute";

		[Space]
		[SerializeField]
		private Button blockButton;

		[SerializeField]
		private TextMeshProUGUI blockButtonText;

		[SerializeField]
		private string blockString = "Block";

		[SerializeField]
		private string unblockString = "Unblock";

		[Space]
		[SerializeField]
		private Button kickButton;

		[SerializeField]
		private TextMeshProUGUI kickButtonText;

		[SerializeField]
		private Button banButton;

		[Space]
		[Header("Kick Style")]
		[SerializeField]
		private ButtonStyleController kickButtonStyleController;

		[SerializeField]
		private ButtonStyle disabledKickStyle;

		private ButtonStyle enabledKickStyle;

		private float _nextKickButtonUpdateTime;

		private double NetworkTime => NetworkSceneSingleton<MissionManager>.i.NetworkTime.Time;

		private void OnValidate()
		{
		}

		protected override void Awake()
		{
			base.Awake();
			enabledKickStyle = kickButtonStyleController.GetCurrentStyle();
			showProfileButton.onClick.AddListener(showProfileClicked);
			muteButton.onClick.AddListener(muteClicked);
			blockButton.onClick.AddListener(blockClicked);
			kickButton.onClick.AddListener(kickClicked);
			banButton.onClick.AddListener(banClicked);
		}

		protected override void Update()
		{
			base.Update();
			if (dropdownPanel.activeSelf && Time.unscaledTime >= _nextKickButtonUpdateTime)
			{
				_nextKickButtonUpdateTime = Time.unscaledTime + 0.2f;
				UpdateKickButtonState();
			}
		}

		protected override void OnShowPanel()
		{
			base.OnShowPanel();
			if (TryGetPlayer(out var player))
			{
				muteButtonText.text = (GameManager.MutedList.Contains(player) ? unmuteString : muteString);
				blockButtonText.text = (GameManager.BlockList.Contains(player) ? unblockString : blockString);
				bool active = NetworkManagerNuclearOption.i.Server.Active;
				if (GameManager.IsLocalPlayer(player))
				{
					kickButton.gameObject.SetActive(value: false);
					banButton.gameObject.SetActive(value: false);
				}
				else
				{
					banButton.gameObject.SetActive(active);
					UpdateKickButtonState();
				}
			}
		}

		private void UpdateKickButtonState()
		{
			if (!TryGetPlayer(out var player))
			{
				return;
			}
			if (GameManager.IsLocalPlayer(player) || player.IsHostPlayer)
			{
				kickButton.gameObject.SetActive(value: false);
				return;
			}
			bool active = NetworkManagerNuclearOption.i.Server.Active;
			VoteKickConfig.ClientConfig clientConfig = NetworkSceneSingleton<VoteKickManager>.i.ClientConfig;
			if (!active && !clientConfig.Enabled)
			{
				kickButton.gameObject.SetActive(value: false);
				return;
			}
			if (active)
			{
				SetKickButton("Kick", interactable: true);
				return;
			}
			VoteKickState state = NetworkSceneSingleton<VoteKickManager>.i.State;
			if (state.Active)
			{
				if (state.TargetID == player.CSteamID)
				{
					if (SceneSingleton<VoteKickUI>.i.HasVoted)
					{
						SetKickButton("Already Voted", interactable: false);
						return;
					}
					float f = (float)(state.VoteEndTimeNetwork - NetworkTime);
					SetKickButton($"Vote Yes ({Mathf.CeilToInt(f)}s)", interactable: true);
				}
				else
				{
					float f2 = (float)(state.VoteEndTimeNetwork - NetworkTime) + clientConfig.NewVoteLockout;
					SetKickButton($"Vote Kick ({Mathf.CeilToInt(f2)}s)", interactable: false);
				}
			}
			else
			{
				float num = (float)(state.LockoutEndTimeNetwork - NetworkTime);
				float localRequestCooldownRemaining = SceneSingleton<VoteKickUI>.i.LocalRequestCooldownRemaining;
				if (num > 0f)
				{
					SetKickButton($"Vote Kick ({Mathf.CeilToInt(num)}s)", interactable: false);
				}
				else if (localRequestCooldownRemaining > 0f)
				{
					SetKickButton($"Vote Kick ({Mathf.CeilToInt(localRequestCooldownRemaining)}s)", interactable: false);
				}
				else
				{
					SetKickButton("Vote Kick", interactable: true);
				}
			}
		}

		private void SetKickButton(string text, bool interactable)
		{
			kickButton.gameObject.SetActive(value: true);
			kickButtonText.text = text;
			kickButton.interactable = interactable;
			kickButtonStyleController.ApplyStyle(interactable ? enabledKickStyle : disabledKickStyle);
		}

		private bool GetEntry(out LeaderboardPlayerEntry entry)
		{
			entry = null;
			if (trigger != null)
			{
				return trigger.TryGetComponent<LeaderboardPlayerEntry>(out entry);
			}
			return false;
		}

		private bool TryGetPlayer(out Player player)
		{
			if (GetEntry(out var entry))
			{
				player = entry.Player;
				return true;
			}
			player = null;
			return false;
		}

		private void showProfileClicked()
		{
			if (TryGetPlayer(out var player))
			{
				SteamFriends.ActivateGameOverlayToUser("steamid", player.CSteamID);
				HideMenuAsync().Forget();
			}
		}

		private void muteClicked()
		{
			if (GetEntry(out var entry))
			{
				GameManager.MutedList.Toggle(entry.Player);
				entry.UpdateBlockMute();
				HideMenuAsync().Forget();
			}
		}

		private void blockClicked()
		{
			if (GetEntry(out var entry))
			{
				if (GameManager.BlockList.Contains(entry.Player))
				{
					GameManager.BlockList.Remove(entry.Player.CSteamID);
				}
				else
				{
					BanAndAppendId(GameManager.BlockList, GameManager.BlockFilePath, entry.Player);
				}
				entry.UpdateBlockMute();
				HideMenuAsync().Forget();
			}
		}

		private void kickClicked()
		{
			if (TryGetPlayer(out var player) && !GameManager.IsLocalPlayer(player))
			{
				if (NetworkManagerNuclearOption.i.Server.Active)
				{
					NetworkManagerNuclearOption.i.KickPlayerAsync(player).Forget();
				}
				else if (NetworkSceneSingleton<VoteKickManager>.i.State.IsVoteActive(player.CSteamID))
				{
					SceneSingleton<VoteKickUI>.i.CastVote(yes: true);
				}
				else
				{
					SceneSingleton<VoteKickUI>.i.RequestVoteKickLocal(player);
				}
				HideMenuAsync().Forget();
			}
		}

		private void banClicked()
		{
			if (TryGetPlayer(out var player) && !GameManager.IsLocalPlayer(player))
			{
				NetworkManagerNuclearOption.i.KickPlayerAsync(player).Forget();
				AllowBanList banList = NetworkManagerNuclearOption.i.Authenticator.BanList;
				string userBanListPath = NetworkAuthenticatorNuclearOption.UserBanListPath;
				BanAndAppendId(banList, userBanListPath, player);
				HideMenuAsync().Forget();
			}
		}

		public static void BanAndAppendId(AllowBanList list, string path, Player player)
		{
			string text = player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard) ?? "";
			if (text.Length > 20)
			{
				text = text.Substring(0, 20);
			}
			AllowBanList.BanAndAppendId(list, path, player.CSteamID, text);
		}
	}
}
