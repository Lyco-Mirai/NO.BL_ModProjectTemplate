using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorNewMenu : MonoBehaviour
	{
		[Header("Name")]
		[SerializeField]
		private TMP_InputField missionName;

		[SerializeField]
		private GameObject nameWarning;

		[Header("Player mode")]
		[SerializeField]
		private Toggle singlePlayerToggle;

		[SerializeField]
		private Toggle multiPlayerToggle;

		[SerializeField]
		private GameObject playerModeInvalid;

		[Header("Factions")]
		[SerializeField]
		private Toggle boscaliToggle;

		[SerializeField]
		private Toggle primevaToggle;

		[SerializeField]
		private GameObject factionInvalid;

		[Header("Maps")]
		[SerializeField]
		private MapLoader mapLoader;

		[SerializeField]
		private RectTransform mapListParent;

		[SerializeField]
		private NewMissionMapButton mapButtonPrefab;

		[SerializeField]
		private NewMissionMapButton[] mapButtons;

		[Header("Buttons")]
		[SerializeField]
		private Button createButton;

		[SerializeField]
		private GameObject loadingNotification;

		private MapDetails selectedMap;

		private void Awake()
		{
			singlePlayerToggle.onValueChanged.AddListener(OnPlayerModeToggle);
			multiPlayerToggle.onValueChanged.AddListener(OnPlayerModeToggle);
			boscaliToggle.onValueChanged.AddListener(OnFactionToggle);
			primevaToggle.onValueChanged.AddListener(OnFactionToggle);
			missionName.onValueChanged.AddListener(NameChanged);
			missionName.onEndEdit.AddListener(NameEditEnd);
			createButton.onClick.AddListener(Create);
			for (int i = 0; i < mapButtons.Length; i++)
			{
				MapDetails map = mapLoader.Maps[i];
				mapButtons[i].Button.onClick.AddListener(delegate
				{
					MapSelected(map);
				});
			}
			MapSelected(mapLoader.Maps.First());
		}

		private void MapSelected(MapDetails mapDetails)
		{
			selectedMap = mapDetails;
			NewMissionMapButton[] array = mapButtons;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnMapSelected(selectedMap);
			}
		}

		private void NameChanged(string newName)
		{
			nameWarning.SetActive(MissionGroup.UserGroup.Exist(newName, out var _));
			FixLayout.ForceRebuildRecursive((RectTransform)base.transform);
			CheckCreateInteractable();
		}

		private void NameEditEnd(string newName)
		{
			if (MissionSaveLoad.ValidateName(ref newName, allowEmpty: true))
			{
				missionName.SetTextWithoutNotify(newName);
			}
			NameChanged(newName);
		}

		private void OnEnable()
		{
			missionName.text = "";
			singlePlayerToggle.isOn = true;
			multiPlayerToggle.isOn = true;
			boscaliToggle.isOn = true;
			primevaToggle.isOn = true;
			NameChanged("");
			OnPlayerModeToggle(_: false);
			OnFactionToggle(_: false);
		}

		private void OnPlayerModeToggle(bool _)
		{
			playerModeInvalid.SetActive(!singlePlayerToggle.isOn && !multiPlayerToggle.isOn);
			CheckCreateInteractable();
			FixLayout.ForceRebuildRecursive((RectTransform)base.transform);
		}

		private void OnFactionToggle(bool _)
		{
			factionInvalid.SetActive(!boscaliToggle.isOn && !primevaToggle.isOn);
			CheckCreateInteractable();
			FixLayout.ForceRebuildRecursive((RectTransform)base.transform);
		}

		private void CheckCreateInteractable()
		{
			createButton.interactable = !string.IsNullOrEmpty(missionName.text) && !playerModeInvalid.activeSelf && !factionInvalid.activeSelf;
		}

		private void Create()
		{
			string text = missionName.text;
			PlayerMode playerMode;
			if (singlePlayerToggle.isOn && multiPlayerToggle.isOn)
			{
				playerMode = PlayerMode.SingleAndMultiplayer;
			}
			else if (singlePlayerToggle.isOn)
			{
				playerMode = PlayerMode.Singleplayer;
			}
			else
			{
				if (!multiPlayerToggle.isOn)
				{
					throw new InvalidOperationException("Invalid Player Mode toggle");
				}
				playerMode = PlayerMode.Multiplayer;
			}
			List<string> list = new List<string>();
			if (boscaliToggle.isOn)
			{
				list.Add("Boscali");
			}
			if (primevaToggle.isOn)
			{
				list.Add("Primeva");
			}
			if (list.Count == 0)
			{
				throw new InvalidOperationException("Invalid faction toggle");
			}
			if (loadingNotification != null)
			{
				loadingNotification.SetActive(value: true);
			}
			MapKey map = MapKey.GameWorldPrefab(selectedMap.PrefabName);
			MissionEditor.LoadEditor(new NewMissionConfig(text, map, playerMode, list)).Forget();
		}
	}
}
