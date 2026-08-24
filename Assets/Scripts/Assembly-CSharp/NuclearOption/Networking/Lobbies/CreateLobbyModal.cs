using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.UI;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.Networking.Lobbies
{
	public class CreateLobbyModal : MonoBehaviour
	{
		private static readonly string noPasswordString = "<color=#FF0><font=\"MaterialSymbolsSharp\">\ue002</font></color> <i>no password</i>";

		[Header("Modal")]
		[SerializeField]
		private GameObject holder;

		[SerializeField]
		private Button close;

		[SerializeField]
		private GameObject creatingLobbyOverlay;

		[Header("Mission picker")]
		[SerializeField]
		private Button openMissionPickerButton;

		[SerializeField]
		private MissionsPicker missionsPicker;

		[SerializeField]
		private TextMeshProUGUI missionNameText;

		[SerializeField]
		private string noMissionText = "<select a mission>";

		[SerializeField]
		private MapLoader mapLoader;

		[Header("New Lobby Settings")]
		[SerializeField]
		private TMP_InputField inputLobbyName;

		[SerializeField]
		private Slider maxPlayersSlider;

		[SerializeField]
		private GameObject tooManyPlayerWarningHolder;

		[SerializeField]
		private BetterToggleGroup lobbyTypeGroup;

		[SerializeField]
		private BoxToggle passwordInputToggle;

		[SerializeField]
		private TextMeshProUGUI passwordPlaceholder;

		[SerializeField]
		private TMP_InputField passwordInput;

		[SerializeField]
		private Button createLobby;

		private bool usingDefaultLobbyName;

		private Mission selectedMission;

		private Color missionNameTextStartColor;

		private LobbyList lobbyList;

		private void OnValidate()
		{
			if (missionNameText != null && selectedMission == null)
			{
				missionNameText.text = noMissionText;
			}
		}

		public void Show(LobbyList lobbyList)
		{
			holder.SetActive(value: true);
			this.lobbyList = lobbyList;
		}

		public void Hide()
		{
			holder.SetActive(value: false);
		}

		private void OnEnable()
		{
			creatingLobbyOverlay.SetActive(value: false);
			inputLobbyName.text = "";
		}

		private void Awake()
		{
			SteamLobby.instance.CheckRelayLocationTask();
			close.onClick.AddListener(Hide);
			inputLobbyName.onValueChanged.AddListener(delegate(string name)
			{
				usingDefaultLobbyName = string.IsNullOrEmpty(name);
			});
			missionNameTextStartColor = missionNameText.color;
			usingDefaultLobbyName = true;
			OnMissionConfirmed(null);
			missionsPicker.SetPickerFilter(new MissionsPicker.Filter
			{
				DisallowedGroups = new List<MissionGroup> { MissionGroup.Tutorial },
				RequiredTags = new List<MissionTag> { MissionTag.Multiplayer }
			});
			missionsPicker.OnMissionConfirmed += OnMissionConfirmed;
			openMissionPickerButton.onClick.AddListener(missionsPicker.ShowPicker);
			createLobby.onClick.AddListener(UniTask.UnityAction(HostLobby));
			maxPlayersSlider.onValueChanged.AddListener(MaxPlayersSlider);
			passwordInputToggle.isOn = false;
			PasswordToggleChanged(passwordInput);
			passwordInputToggle.onValueChanged.AddListener(PasswordToggleChanged);
		}

		private void MaxPlayersSlider(float _)
		{
			int num = (int)maxPlayersSlider.value;
			tooManyPlayerWarningHolder.SetActive(num > lobbyList.TooManyPlayerLimit);
		}

		private void PasswordToggleChanged(bool hasPassword)
		{
			passwordPlaceholder.text = (hasPassword ? "<i>password</i>" : noPasswordString);
		}

		private void OnMissionConfirmed(Mission mission)
		{
			if (usingDefaultLobbyName)
			{
				string text = mission?.Name ?? "Mission";
				inputLobbyName.SetTextWithoutNotify(text + " [Hosted by " + PlayerSettings.playerName_Unsanitized + "]");
			}
			missionNameText.text = mission?.Name ?? "<select a mission>";
			missionNameText.color = ((mission != null) ? missionNameTextStartColor : (missionNameTextStartColor * 0.6f));
			selectedMission = mission;
			createLobby.interactable = mission != null;
			missionsPicker.HidePicker();
		}

		public async UniTaskVoid HostLobby()
		{
			MissionManager.SetMission(selectedMission, checkIfSame: false);
			int maxPlayers = (int)maxPlayersSlider.value;
			ELobbyType lobbyType = lobbyTypeGroup.GetIndex() switch
			{
				2 => ELobbyType.k_ELobbyTypePrivate, 
				1 => ELobbyType.k_ELobbyTypeFriendsOnly, 
				_ => ELobbyType.k_ELobbyTypePublic, 
			};
			creatingLobbyOverlay.SetActive(value: true);
			HostedLobbyInstance? hostedLobbyInstance = await SteamLobby.instance.HostLobby(maxPlayers, lobbyType);
			if (hostedLobbyInstance.HasValue)
			{
				HostedLobbyInstance value = hostedLobbyInstance.Value;
				string text = inputLobbyName.text;
				string text2 = ((!string.IsNullOrEmpty(text)) ? text : (SteamFriends.GetPersonaName() + "'s lobby"));
				SteamLobby.instance.CurrentLobbyName = text2;
				value.SetData("name", text2.SanitizeRichText(128));
				value.SetData("mission_name", selectedMission.Name.SanitizeRichText(128));
				value.SetData("mission_description", selectedMission.missionSettings.description.SanitizeRichText(1000));
				value.SetData("mission_pvp_type", MissionTag.GetPvpTypeLobbyString(selectedMission));
				if (mapLoader.TryGetMapName(selectedMission.MapKey, out var mapName))
				{
					value.SetData("map_name", mapName);
				}
				PublishedFileId_t? publishedFileId_t = selectedMission.LoadKey?.WorkshopId;
				if (publishedFileId_t.HasValue)
				{
					value.SetData("mission_workshop_id", publishedFileId_t.Value.m_PublishedFileId.ToString("X"));
				}
				string text3 = passwordInput.text;
				bool flag = passwordInputToggle.isOn && !string.IsNullOrEmpty(text3);
				value.SetData("short_password", flag ? LobbyPassword.GetShortPassword(text3) : "");
				bool valueOrDefault = NetworkManagerNuclearOption.ModdedServer == true;
				bool valueOrDefault2 = selectedMission?.missionSettings?.allowEventContent == true;
				value.SetData("modded_server", LobbyInstance.BoolToTag(valueOrDefault || valueOrDefault2));
				HostOptions options = new HostOptions(SocketType.Steam, GameState.Multiplayer, selectedMission.MapKey)
				{
					MaxConnections = maxPlayers - 1,
					Password = (flag ? text3 : null)
				};
				await NetworkManagerNuclearOption.i.StartHostAsync(options);
			}
			if (creatingLobbyOverlay != null)
			{
				creatingLobbyOverlay.SetActive(value: false);
			}
		}
	}
}
