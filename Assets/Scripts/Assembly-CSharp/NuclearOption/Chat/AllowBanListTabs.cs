using System.ComponentModel;
using NuclearOption.DedicatedServer;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.Networking.Authentication;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Chat
{
	public class AllowBanListTabs : MonoBehaviour
	{
		public enum Source
		{
			Muted = 0,
			Blocked = 1,
			Kicked = 2,
			Banned = 3
		}

		[SerializeField]
		private AllowBanListController controller;

		[SerializeField]
		private TextMeshProUGUI descriptionText;

		[SerializeField]
		private Source _source;

		[Header("Buttons")]
		[SerializeField]
		private ButtonStyle ActiveStyle;

		[SerializeField]
		private ButtonStyle NormalStyle;

		[SerializeField]
		private Button mutedTabButton;

		[SerializeField]
		private ButtonStyleController mutedTabButtonController;

		[SerializeField]
		private Button blockedTabButton;

		[SerializeField]
		private ButtonStyleController blockedTabButtonController;

		[SerializeField]
		private Button kickedTabButton;

		[SerializeField]
		private ButtonStyleController kickedTabButtonController;

		[SerializeField]
		private Button bannedTabButton;

		[SerializeField]
		private ButtonStyleController bannedTabButtonController;

		[Header("Descriptions")]
		[SerializeField]
		[TextArea]
		private string muteDescription;

		[SerializeField]
		[TextArea]
		private string blockedDescription;

		[SerializeField]
		[TextArea]
		private string kickedDescription;

		[SerializeField]
		[TextArea]
		private string bannedDescription;

		private void Awake()
		{
			mutedTabButton.onClick.AddListener(delegate
			{
				SetTab(Source.Muted);
			});
			blockedTabButton.onClick.AddListener(delegate
			{
				SetTab(Source.Blocked);
			});
			kickedTabButton.onClick.AddListener(delegate
			{
				SetTab(Source.Kicked);
			});
			bannedTabButton.onClick.AddListener(delegate
			{
				SetTab(Source.Banned);
			});
		}

		private void OnEnable()
		{
			SetTab(_source);
		}

		public void SetTab(Source source)
		{
			_source = source;
			controller.Setup(GetList(_source));
			descriptionText.text = GetDescription(_source);
			mutedTabButtonController.ApplyStyle((_source == Source.Muted) ? ActiveStyle : NormalStyle);
			blockedTabButtonController.ApplyStyle((_source == Source.Blocked) ? ActiveStyle : NormalStyle);
			kickedTabButtonController.ApplyStyle((_source == Source.Kicked) ? ActiveStyle : NormalStyle);
			bannedTabButtonController.ApplyStyle((_source == Source.Banned) ? ActiveStyle : NormalStyle);
		}

		private string GetDescription(Source source)
		{
			return source switch
			{
				Source.Muted => muteDescription, 
				Source.Blocked => blockedDescription, 
				Source.Kicked => kickedDescription, 
				Source.Banned => bannedDescription, 
				_ => string.Empty, 
			};
		}

		private (AllowBanList banList, string filePath) GetList(Source source)
		{
			return source switch
			{
				Source.Muted => (banList: GameManager.MutedList, filePath: null), 
				Source.Blocked => (banList: GameManager.BlockList, filePath: GameManager.BlockFilePath), 
				Source.Kicked => (banList: NetworkManagerNuclearOption.i.Authenticator.KickList, filePath: null), 
				Source.Banned => (banList: NetworkManagerNuclearOption.i.Authenticator.BanList, filePath: NetworkAuthenticatorNuclearOption.UserBanListPath), 
				_ => throw new InvalidEnumArgumentException(), 
			};
		}
	}
}
