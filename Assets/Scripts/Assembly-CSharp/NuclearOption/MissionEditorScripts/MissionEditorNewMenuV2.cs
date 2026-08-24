using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using NuclearOption.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorNewMenuV2 : MonoBehaviour
	{
		[Header("Name")]
		[SerializeField]
		private TMP_InputField missionName;

		[SerializeField]
		private GameObject nameWarning;

		[SerializeField]
		private TextMeshProUGUI nameWarningText;

		[Header("Player mode")]
		[SerializeField]
		private BetterToggleGroup playerModeToggleGroup;

		[Header("Factions")]
		[SerializeField]
		private BetterToggleGroup factionToggleGroup;

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
		private Button createUnnamedButton;

		[SerializeField]
		private GameObject loadingNotification;

		private MapDetails selectedMap;

		private void Awake()
		{
			missionName.onValueChanged.AddListener(NameChanged);
			missionName.onEndEdit.AddListener(NameEditEnd);
			createButton.onClick.AddListener(Create);
			createUnnamedButton.onClick.AddListener(CreateUnnamed);
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
			string validName;
			bool flag = MissionGroup.UserGroup.Exist(newName, out validName);
			nameWarning.SetActive(flag);
			if (flag)
			{
				nameWarningText.text = "'" + validName + "' already exists";
			}
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
			playerModeToggleGroup.SetFlags(3, notify: true);
			factionToggleGroup.SetFlags(3, notify: true);
			NameChanged("");
		}

		private void CheckCreateInteractable()
		{
			createButton.interactable = !string.IsNullOrEmpty(missionName.text);
			createUnnamedButton.gameObject.SetActive(string.IsNullOrEmpty(missionName.text));
		}

		private void Create()
		{
			string text = missionName.text;
			PlayerModeFlags flags = (PlayerModeFlags)playerModeToggleGroup.GetFlags();
			if (flags == (PlayerModeFlags)0)
			{
				throw new InvalidOperationException("Invalid Player Mode toggle");
			}
			int flags2 = factionToggleGroup.GetFlags();
			if (flags2 == 0)
			{
				throw new InvalidOperationException("Invalid faction toggle");
			}
			List<string> list = new List<string>();
			if ((flags2 & 1) != 0)
			{
				list.Add("Boscali");
			}
			if ((flags2 & 2) != 0)
			{
				list.Add("Primeva");
			}
			if (loadingNotification != null)
			{
				loadingNotification.SetActive(value: true);
			}
			MapKey map = MapKey.GameWorldPrefab(selectedMap.PrefabName);
			MissionEditor.LoadEditor(new NewMissionConfig(text, map, flags.ToMode(), list)).Forget();
		}

		private void CreateUnnamed()
		{
			if (loadingNotification != null)
			{
				loadingNotification.SetActive(value: true);
			}
			MissionEditor.LoadEditor(NewMissionConfig.DefaultMission(MapKey.GameWorldPrefab(selectedMap.PrefabName))).Forget();
		}
	}
}
