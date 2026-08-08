using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.Workshop;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class LobbyDetailsModal : MonoBehaviour
	{
		[Header("References")]
		[SerializeField]
		private Sprite defaultMissionImage;

		[SerializeField]
		private GameObject holder;

		[Header("Fields")]
		[SerializeField]
		private TextMeshProUGUI lobbyNameText;

		[SerializeField]
		private TextMeshProUGUI missionNameText;

		[SerializeField]
		private TextMeshProUGUI mapText;

		[SerializeField]
		private Image missionImage;

		[SerializeField]
		private TextMeshProUGUI missionDescriptionText;

		[SerializeField]
		private TextMeshProUGUI serverTypeText;

		[SerializeField]
		private GameObject moddedWarning;

		[SerializeField]
		private TextMeshProUGUI missionTypeText;

		[SerializeField]
		private TextMeshProUGUI pingText;

		[SerializeField]
		private TextMeshProUGUI upTimeText;

		[SerializeField]
		private TextMeshProUGUI playersText;

		[Header("Buttons")]
		[SerializeField]
		private Button openWorkshopButton;

		[SerializeField]
		private Button closeDetails;

		[SerializeField]
		private Button joinButton;

		[SerializeField]
		private Button blockPlayerButton;

		[SerializeField]
		private GameObject blockPlayerButtonConfirmOverlay;

		[SerializeField]
		private Button blockPlayerButtonConfirm;

		[SerializeField]
		private Button blockPlayerButtonCancel;

		private LobbyInstance lobby;

		private CancellationTokenSource detailsOpenCancelSource;

		private LobbyList lobbyList;

		private void Awake()
		{
			openWorkshopButton.onClick.AddListener(OnOpenWorkshop);
			closeDetails.onClick.AddListener(Hide);
			joinButton.onClick.AddListener(Join);
			blockPlayerButtonConfirmOverlay.SetActive(value: false);
			blockPlayerButton.gameObject.SetActive(value: false);
		}

		public void Show(LobbyList lobbyList, LobbyInstance lobby)
		{
			if (this.lobby != null)
			{
				throw new InvalidOperationException("LobbyDetailPopup already had lobby, it can't be shown multiple times");
			}
			if (lobby == null)
			{
				throw new ArgumentException("can't show invalid lobby");
			}
			this.lobbyList = lobbyList;
			this.lobby = lobby;
			detailsOpenCancelSource = new CancellationTokenSource();
			holder.SetActive(value: true);
			lobbyNameText.text = lobby.LobbyNameSanitized;
			missionNameText.text = lobby.MissionNameSanitized;
			mapText.text = lobby.MapNameSanitized;
			missionDescriptionText.text = lobby.MissionDescriptionSanitized;
			serverTypeText.text = (lobby.DedicatedServer ? "Dedicated Server" : "Player Hosted");
			moddedWarning.SetActive(lobby.ModdedServer);
			TextMeshProUGUI textMeshProUGUI = missionTypeText;
			textMeshProUGUI.text = lobby.MissionPvpType switch
			{
				MissionPvpType.Pvp => "PVP", 
				MissionPvpType.Pve => "PVE", 
				_ => "", 
			};
			PublishedFileId_t missionWorkshopId = lobby.MissionWorkshopId;
			openWorkshopButton.gameObject.SetActive(missionWorkshopId != PublishedFileId_t.Invalid);
			SetPreviewImage(missionWorkshopId, lobby.MissionNameRaw, detailsOpenCancelSource.Token).Forget();
			UpdateLoop(detailsOpenCancelSource.Token).Forget();
		}

		private async UniTask SetPreviewImage(PublishedFileId_t id, string missionName, CancellationToken cancellationToken)
		{
			missionImage.color = new Color(0.2f, 0.2f, 0.2f);
			missionImage.sprite = null;
			Sprite sprite = await GetSprite(id, missionName, cancellationToken);
			missionImage.color = Color.white;
			missionImage.sprite = sprite ?? defaultMissionImage;
		}

		private static async UniTask<Sprite> GetSprite(PublishedFileId_t id, string missionName, CancellationToken cancellationToken)
		{
			if (id != PublishedFileId_t.Invalid)
			{
				var (flag, steamWorkshopItem) = await SteamWorkshop.GetDetails(id);
				if (flag)
				{
					return await steamWorkshopItem.GetPreview(cancellationToken);
				}
			}
			if (MissionGroup.BuiltIn.ContainsMission(missionName))
			{
				return await MissionGroup.BuiltIn.GetPreview(missionName, cancellationToken);
			}
			return null;
		}

		private async UniTask UpdateLoop(CancellationToken cancellation)
		{
			int pingCheck = 10;
			DateTime? startTime = lobby.StartTime;
			if (!startTime.HasValue)
			{
				upTimeText.text = "";
			}
			while (!cancellation.IsCancellationRequested)
			{
				if (startTime.HasValue)
				{
					upTimeText.text = LobbyInstance.TimeSpanString(startTime, includeSeconds: true);
				}
				pingCheck++;
				if (pingCheck >= 10)
				{
					pingCheck = 0;
					int? num = lobby.CalculatePing();
					pingText.text = (num.HasValue ? $"{num.Value}ms" : "");
					if (lobby.GetPlayerCounts(out var current, out var max))
					{
						joinButton.interactable = current < max;
						playersText.text = $"{current}/{max}";
					}
					else
					{
						joinButton.interactable = false;
						playersText.text = "";
					}
				}
				await UniTask.Delay(1000, ignoreTimeScale: true);
			}
		}

		private void OnDisable()
		{
			Hide();
		}

		private void Hide()
		{
			lobby = null;
			detailsOpenCancelSource?.Cancel();
			detailsOpenCancelSource = null;
			if (holder.activeSelf)
			{
				holder.SetActive(value: false);
			}
		}

		private void OnOpenWorkshop()
		{
			PublishedFileId_t missionWorkshopId = lobby.MissionWorkshopId;
			if (missionWorkshopId != PublishedFileId_t.Invalid)
			{
				SteamWorkshopItem.OpenSteamPage(missionWorkshopId);
			}
		}

		private void Join()
		{
			SteamLobby.instance.TryJoinLobby(lobby, null, promptIfPasswordNeeded: true);
		}

		private void BlockPlayer()
		{
			blockPlayerButtonConfirmOverlay.SetActive(value: false);
			throw new NotImplementedException("no way to verify ownerId, so can't block from lobby");
		}
	}
}
