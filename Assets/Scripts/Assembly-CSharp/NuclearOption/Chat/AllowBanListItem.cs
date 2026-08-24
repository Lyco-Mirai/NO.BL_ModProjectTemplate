using JamesFrowen.ScriptableVariables.UI;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Chat
{
	public class AllowBanListItem : ListItem<AllowBanListEntry>
	{
		[Header("UI References")]
		public TextMeshProUGUI nameText;

		public TextMeshProUGUI steamIdText;

		public Button unblockButton;

		public Button showProfileButton;

		protected override void Awake()
		{
			base.Awake();
			unblockButton.onClick.AddListener(UnBlockClicked);
			showProfileButton.onClick.AddListener(ShowProfileClicked);
		}

		private void UnBlockClicked()
		{
			base.Value.DeleteAction?.Invoke(base.Value.SteamID);
		}

		public void ShowProfileClicked()
		{
			SteamFriends.ActivateGameOverlayToUser("steamid", base.Value.SteamID);
		}

		protected override void SetValue(AllowBanListEntry value)
		{
			steamIdText.text = value.SteamID.ToString();
			nameText.text = value.Name ?? "";
		}
	}
}
