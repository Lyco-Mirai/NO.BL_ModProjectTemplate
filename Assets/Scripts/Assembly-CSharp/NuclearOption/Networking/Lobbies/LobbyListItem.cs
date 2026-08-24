using System;
using NuclearOption.MissionEditorScripts.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class LobbyListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
	{
		private const string PRIMARY_HEX = "#58e187";

		private const string MUTED_HEX = "#8a93a2";

		private const string MODDED_COLOR = "#FF7700";

		private const string STATUS_SUCCESS_HEX = "#4ade80";

		private const string STATUS_WARNING_HEX = "#facc15";

		private const string STATUS_DANGER_HEX = "#f87171";

		public static readonly Color StatusSuccessColor;

		public static readonly Color StatusWarningColor;

		public static readonly Color StatusDangerColor;

		public static readonly Color TextMutedColor;

		private static readonly string serverIcon;

		private static readonly string playerIcon;

		private static readonly string lockIcon;

		private static readonly string moddedIcon;

		[Header("Colors")]
		[SerializeField]
		private Image backgroundImage;

		[SerializeField]
		private Color normalBackground;

		[SerializeField]
		private Color hoverBackground;

		[Space]
		[SerializeField]
		private Graphic boarderImage;

		[SerializeField]
		private Color normalBoarder;

		[SerializeField]
		private Color hoverBoarder;

		[Header("Text")]
		[SerializeField]
		private TextMeshProUGUI lobbyNameText;

		[SerializeField]
		private TextMeshProUGUI missionNameText;

		[SerializeField]
		private TextMeshProUGUI iconsText;

		[SerializeField]
		private TextMeshProUGUI playerText;

		[SerializeField]
		private TextMeshProUGUI uptimeText;

		[SerializeField]
		private TextMeshProUGUI pingText;

		[SerializeField]
		private ShowHoverText tooManyPlayersWarning;

		[SerializeField]
		private ShowTMPLinkHoverText iconTooltip;

		private LobbyList multiplayerLobbyList;

		private bool isPasswordProtected;

		private bool shown;

		public LobbyInstance lobby { get; private set; }

		public string LobbyName { get; private set; }

		public string MissionName { get; private set; }

		public string MapName { get; private set; }

		public int PlayerCount { get; private set; }

		public int? Ping { get; private set; }

		public DateTime? StartTime { get; private set; }

		public bool IsFull { get; private set; }

		public bool IsServer => lobby is ServerLobbyInstance;

		static LobbyListItem()
		{
			serverIcon = "\ue875".AddColor("#58e187").AddLink("tooltip_server");
			playerIcon = "\ue7fd".AddColor("#8a93a2").AddLink("tooltip_player");
			lockIcon = "\ue897".AddColor("#8a93a2").AddLink("tooltip_locked");
			moddedIcon = "\uef48".AddColor("#FF7700").AddLink("tooltip_modded");
			ColorUtility.TryParseHtmlString("#4ade80", out StatusSuccessColor);
			ColorUtility.TryParseHtmlString("#facc15", out StatusWarningColor);
			ColorUtility.TryParseHtmlString("#f87171", out StatusDangerColor);
			ColorUtility.TryParseHtmlString("#8a93a2", out TextMutedColor);
		}

		public bool Show(LobbyList multiplayerLobbyList, LobbyInstance lobby)
		{
			if (shown && !this.lobby.Equals(lobby))
			{
				Debug.LogError("LobbyDataEntry Shown twice");
				return false;
			}
			LobbyName = lobby.LobbyNameSanitized;
			if (!LobbyInstance.ValidName(LobbyName))
			{
				return false;
			}
			tooManyPlayersWarning.SetHover(multiplayerLobbyList.TooManyPlayerHover);
			tooManyPlayersWarning.SetText(null);
			iconTooltip.SetHover(multiplayerLobbyList.IconTooltipHover);
			this.multiplayerLobbyList = multiplayerLobbyList;
			this.lobby = lobby;
			lobbyNameText.text = LobbyName;
			MissionName = lobby.MissionNameSanitized;
			MapName = lobby.MapNameSanitized;
			if (string.IsNullOrEmpty(MapName))
			{
				missionNameText.text = MissionName ?? "";
			}
			else if (string.IsNullOrEmpty(MissionName))
			{
				missionNameText.text = MapName ?? "";
			}
			else
			{
				missionNameText.text = MapName + " | " + MissionName;
			}
			isPasswordProtected = lobby.IsPasswordProtected(out var _);
			bool dedicatedServer = lobby.DedicatedServer;
			string text = (isPasswordProtected ? ((!dedicatedServer) ? (playerIcon + " " + lockIcon) : (serverIcon + " " + lockIcon)) : ((!dedicatedServer) ? playerIcon : serverIcon));
			if (lobby.ModdedServer)
			{
				text = moddedIcon + text;
			}
			iconsText.text = text;
			if (lobby.GetPlayerCounts(out var current, out var max))
			{
				PlayerCount = current;
				string arg = ((max <= multiplayerLobbyList.TooManyPlayerLimit) ? "" : (GoogleIconFont.FontString("\ue002").AddColor(Color.yellow) + " "));
				playerText.text = $"{arg}[{current} / {max}]";
			}
			else
			{
				playerText.text = "";
			}
			tooManyPlayersWarning.enabled = max > multiplayerLobbyList.TooManyPlayerLimit;
			StartTime = lobby.StartTime;
			uptimeText.text = LobbyInstance.TimeSpanString(StartTime, includeSeconds: false);
			SetPingTextAndColor(lobby.CalculatePing());
			shown = true;
			base.gameObject.SetActive(value: true);
			return true;
		}

		private void SetPingTextAndColor(int? ping)
		{
			Ping = ping;
			string text = (ping.HasValue ? $"{ping.Value}ms" : "");
			pingText.color = ColorFromPing(ping);
			pingText.text = text;
		}

		private Color ColorFromPing(int? ping)
		{
			if (ping.HasValue)
			{
				int valueOrDefault = ping.GetValueOrDefault();
				if (valueOrDefault >= 60)
				{
					if (valueOrDefault < 120)
					{
						return StatusWarningColor;
					}
					return StatusDangerColor;
				}
				return StatusSuccessColor;
			}
			return TextMutedColor;
		}

		public void Hide()
		{
			shown = false;
			lobby = null;
			ShowBoarder(show: false);
			base.gameObject.SetActive(value: false);
		}

		public void UpdatePing()
		{
			SetPingTextAndColor(lobby.CalculatePing());
		}

		public void UpdateTime()
		{
			uptimeText.text = LobbyInstance.TimeSpanString(StartTime, includeSeconds: false);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			multiplayerLobbyList.ShowLobbyPopup(lobby);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			ShowBoarder(show: true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			ShowBoarder(show: false);
		}

		private void ShowBoarder(bool show)
		{
			backgroundImage.color = (show ? hoverBackground : normalBackground);
			boarderImage.color = (show ? hoverBoarder : normalBoarder);
		}
	}
}
