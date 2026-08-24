using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.ConvertVersions;
using NuclearOption.UI.HeaderSorting;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorLoadMenuV2 : MonoBehaviour, IMissionEditorFileMenu
	{
		[Serializable]
		public struct MissionGroupButton
		{
			public string Name;

			public Button Button;

			public ButtonStyleController StyleController;

			public MissionGroup Group { get; set; }
		}

		[Serializable]
		public enum SortMode
		{
			None = 0,
			Name = 1,
			LastModified = 2
		}

		private static readonly ProfilerMarker updateMissionListMarker = new ProfilerMarker("MissionEditorLoadMenuV2.UpdateMissionList");

		private static readonly ProfilerMarker updateListUIMarker = new ProfilerMarker("MissionEditorLoadMenuV2.UpdateListUI");

		[Header("References")]
		[SerializeField]
		private FileMenu fileMenu;

		[Header("Mission Groups")]
		[SerializeField]
		private RectTransform missionGroupsHolder;

		[SerializeField]
		private MissionGroupButton[] missionGroups;

		[SerializeField]
		private int defaultMissionGroups;

		[SerializeField]
		private ButtonStyle activeStyle;

		[SerializeField]
		private ButtonStyle inactiveStyle;

		[SerializeField]
		private Button openUserFolder;

		[Header("Mission list")]
		[SerializeField]
		private RectTransform missionListHolder;

		[SerializeField]
		private MissionEditorLoadMenuV2ListItem itemPrefab;

		[SerializeField]
		private MissionEditorLoadMenuV2ListItem subItemPrefab;

		[Header("Sort headers")]
		[SerializeField]
		private ListSortButton sortByNameButton;

		[SerializeField]
		private ListSortButton sortByLastModifiedButton;

		[SerializeField]
		private Color activeHeaderColor = Color.white;

		[SerializeField]
		private Color inactiveHeaderColor = Color.grey;

		[Header("Mission preview")]
		[SerializeField]
		private TextMeshProUGUI previewTitle;

		[SerializeField]
		private TextMeshProUGUI previewSubtitle;

		[SerializeField]
		private TextMeshProUGUI previewVersionText;

		[SerializeField]
		private Color oldVersionColor = new Color(1f, 0.6f, 0.2f);

		[SerializeField]
		private Color currentVersionColor = new Color(0.7f, 0.7f, 0.7f);

		[SerializeField]
		private Image previewImage;

		[SerializeField]
		private TextMeshProUGUI previewDescription;

		[SerializeField]
		private Button loadMissionButton;

		[SerializeField]
		private ButtonStyleController loadMissionButtonStyle;

		[SerializeField]
		private ButtonStyle loadMissionActiveStyle;

		[SerializeField]
		private ButtonStyle loadMissionInactiveStyle;

		[SerializeField]
		private Sprite defaultMissionImage;

		[Header("Loading")]
		[SerializeField]
		private GameObject loadingNotification;

		[Header("Failed to load")]
		[SerializeField]
		private GameObject failedOverlay;

		[SerializeField]
		private TextMeshProUGUI failedText;

		[SerializeField]
		private Button closeFailed;

		private CancellationTokenSource cancelGetPreview;

		private SortHeaderController sortController;

		private SortOrder? activeSortOrder;

		private SortOrder defaultSortOrder;

		private MissionGroup activeGroup;

		private readonly Dictionary<MissionGroup, SortOrder> groupSortOrders = new Dictionary<MissionGroup, SortOrder>();

		private MissionKey? selectedKey;

		private readonly List<MissionFileInfo> missionList = new List<MissionFileInfo>();

		private readonly List<MissionEditorLoadMenuV2ListItem> childList = new List<MissionEditorLoadMenuV2ListItem>();

		public MissionEditorLoadMenuV2ListItem SubItemPrefab => subItemPrefab;

		private void Awake()
		{
			openUserFolder.onClick.AddListener(MissionGroup.UserGroup.OpenFolder);
			closeFailed.onClick.AddListener(delegate
			{
				failedOverlay.SetActive(value: false);
			});
			loadMissionButton.onClick.AddListener(delegate
			{
				LoadMission(selectedKey.Value);
			});
			loadMissionButton.interactable = false;
			loadMissionButtonStyle.ApplyStyle(loadMissionInactiveStyle);
			SetupChildList();
			sortController = new SortHeaderController(activeHeaderColor, inactiveHeaderColor, OnSortHeaderButtonClicked);
			sortController.Register(sortByNameButton, 1);
			sortController.Register(sortByLastModifiedButton, 2);
			SetupMissionGroupButtons();
		}

		private void OnEnable()
		{
			ClearPreview();
			UpdateMissionList();
		}

		private void SetupChildList()
		{
			int childCount = missionListHolder.childCount;
			for (int i = 0; i < childCount; i++)
			{
				if (missionListHolder.GetChild(i).gameObject.TryGetComponent<MissionEditorLoadMenuV2ListItem>(out var component))
				{
					childList.Add(component);
				}
				else
				{
					Debug.LogError("child of missionListHolder did not have MissionEditorLoadMenuV2ListItem");
				}
			}
		}

		private void OnSortHeaderButtonClicked(SortOrder order)
		{
			activeSortOrder = order;
			if (activeGroup != null)
			{
				groupSortOrders[activeGroup] = order;
			}
			UpdateListUI();
		}

		private void OnValidate()
		{
			if (defaultMissionGroups >= missionGroups.Length)
			{
				Debug.LogError($"Default group ({defaultMissionGroups}) is greater than group count ({missionGroups.Length})");
			}
		}

		private void SetupMissionGroupButtons()
		{
			if (defaultMissionGroups >= missionGroups.Length)
			{
				Debug.LogError($"Default group ({defaultMissionGroups}) is greater than group count ({missionGroups.Length})");
			}
			for (int i = 0; i < missionGroups.Length; i++)
			{
				MissionGroupButton item = missionGroups[i];
				item.Group = MissionGroup.GetGroup(item.Name);
				item.Button.onClick.AddListener(delegate
				{
					SetActiveGroup(item);
				});
				if (i == defaultMissionGroups)
				{
					SetActiveGroup(item);
				}
			}
		}

		public void SetActiveGroup(MissionGroupButton item)
		{
			MissionGroupButton[] array = missionGroups;
			for (int i = 0; i < array.Length; i++)
			{
				MissionGroupButton missionGroupButton = array[i];
				bool flag = missionGroupButton.StyleController == item.StyleController;
				missionGroupButton.StyleController.ApplyStyle(flag ? activeStyle : inactiveStyle);
			}
			activeGroup = item.Group;
			defaultSortOrder = ((activeGroup is MissionGroup.UserGroup) ? new SortOrder(2, isAscending: false) : new SortOrder(1, isAscending: true));
			if (groupSortOrders.TryGetValue(activeGroup, out var value))
			{
				activeSortOrder = value;
			}
			else
			{
				activeSortOrder = defaultSortOrder;
			}
			ClearPreview();
			UpdateMissionList();
		}

		private void UpdateMissionList()
		{
			using (updateMissionListMarker.Auto())
			{
				using (BenchmarkScope.Create("UpdateMissionList"))
				{
					missionList.Clear();
					foreach (MissionKey mission in activeGroup.GetMissions())
					{
						MissionFileInfo fileInfo = MissionSaveLoad.GetFileInfo(mission);
						missionList.Add(fileInfo);
					}
					UpdateListUI();
				}
			}
		}

		private void UpdateListUI()
		{
			using (updateListUIMarker.Auto())
			{
				SortOrder sortOrder = activeSortOrder ?? defaultSortOrder;
				sortController?.UpdateVisuals(sortOrder);
				missionList.Sort(GetComparer(sortOrder));
				Comparison<MissionFileInfo> versionComparer = GetVersionComparer(sortOrder);
				foreach (MissionFileInfo mission in missionList)
				{
					if (mission.HasExtraSaves)
					{
						mission.SubItems.Sort(versionComparer);
					}
				}
				while (missionList.Count < childList.Count)
				{
					GameObject obj = childList[childList.Count - 1].gameObject;
					obj.SetActive(value: false);
					UnityEngine.Object.Destroy(obj);
					childList.RemoveAt(childList.Count - 1);
				}
				while (missionList.Count > childList.Count)
				{
					MissionEditorLoadMenuV2ListItem item = UnityEngine.Object.Instantiate(itemPrefab, missionListHolder);
					childList.Add(item);
				}
				for (int i = 0; i < missionList.Count; i++)
				{
					MissionFileInfo data = missionList[i];
					childList[i].Setup(this, data);
				}
				FixLayout.ForceRebuildRecursive(base.transform.AsRectTransform());
			}
		}

		public void LoadMission(MissionKey key)
		{
			ColorLog<MissionEditorLoadMenuV2>.Info($"LoadMission: {key}");
			if (!key.TryLoad(out var mission, out var error))
			{
				Debug.LogError(error);
				return;
			}
			if (loadingNotification != null)
			{
				loadingNotification.SetActive(value: true);
			}
			if (fileMenu != null)
			{
				fileMenu.Close();
			}
			MissionEditor.LoadEditor(mission).Forget();
		}

		public void SelectMission(MissionKey key)
		{
			ColorLog<MissionEditorLoadMenuV2>.Info($"SelectMission: {key}");
			if (key.TryQuickLoad(out var mission))
			{
				selectedKey = key;
				previewTitle.text = mission.Name;
				previewDescription.text = mission.missionSettings.description;
				bool flag = key.Group == MissionGroup.EditorMissions;
				previewSubtitle.text = (flag ? key.Key : "");
				previewSubtitle.gameObject.SetActive(flag);
				previewVersionText.text = $"v{mission.JsonVersion}";
				previewVersionText.color = ((mission.JsonVersion < MissionVersionUpgrade.LatestVersion) ? oldVersionColor : currentVersionColor);
				loadMissionButton.interactable = true;
				loadMissionButtonStyle.ApplyStyle(loadMissionActiveStyle);
				GetPreviewAsync(key).Forget();
			}
			else
			{
				ClearPreview();
				previewTitle.text = "<LOAD ERROR>";
				Debug.LogError($"Failed to quick load mission header: {key}");
				failedOverlay.SetActive(value: true);
				failedText.text = "Failed to load mission preview.";
			}
			FixLayout.ForceRebuildRecursive(base.transform.AsRectTransform());
		}

		private void ClearPreview()
		{
			selectedKey = null;
			previewTitle.text = "";
			previewSubtitle.text = "";
			previewSubtitle.gameObject.SetActive(value: false);
			previewDescription.text = "";
			previewVersionText.text = "";
			cancelGetPreview?.Cancel();
			previewImage.color = Color.clear;
			previewImage.sprite = null;
			loadMissionButton.interactable = false;
			loadMissionButtonStyle.ApplyStyle(loadMissionInactiveStyle);
		}

		private async UniTaskVoid GetPreviewAsync(MissionKey item)
		{
			cancelGetPreview?.Cancel();
			cancelGetPreview = new CancellationTokenSource();
			CancellationToken token = cancelGetPreview.Token;
			previewImage.color = new Color(0.2f, 0.2f, 0.2f);
			Sprite sprite = await item.GetPreview(token);
			if (!token.IsCancellationRequested)
			{
				previewImage.color = Color.white;
				previewImage.sprite = sprite ?? defaultMissionImage;
			}
		}

		public static Comparison<MissionFileInfo> GetComparer(SortOrder order)
		{
			Comparison<MissionFileInfo> comparison = GetComparer((SortMode)order.Mode);
			if (order.IsAscending)
			{
				return comparison;
			}
			return (MissionFileInfo a, MissionFileInfo b) => comparison(b, a);
		}

		public static Comparison<MissionFileInfo> GetComparer(SortMode mode)
		{
			return mode switch
			{
				SortMode.Name => (MissionFileInfo a, MissionFileInfo b) => a.key.Name.CompareTo(b.key.Name), 
				SortMode.LastModified => (MissionFileInfo a, MissionFileInfo b) => SortHelper.CompareNullable(a.LastEdit, b.LastEdit), 
				_ => (MissionFileInfo a, MissionFileInfo b) => 0, 
			};
		}

		public static Comparison<MissionFileInfo> GetVersionComparer(SortOrder order)
		{
			Comparison<MissionFileInfo> comparison = GetVersionComparer((SortMode)order.Mode);
			if (order.IsAscending)
			{
				return comparison;
			}
			return (MissionFileInfo a, MissionFileInfo b) => comparison(b, a);
		}

		public static Comparison<MissionFileInfo> GetVersionComparer(SortMode mode)
		{
			return mode switch
			{
				SortMode.Name => (MissionFileInfo a, MissionFileInfo b) => a.key.Key.CompareTo(b.key.Key), 
				SortMode.LastModified => (MissionFileInfo a, MissionFileInfo b) => SortHelper.CompareNullable(a.LastEdit, b.LastEdit), 
				_ => (MissionFileInfo a, MissionFileInfo b) => 0, 
			};
		}

		public void _Editor_CreateMissionGroups()
		{
			List<Transform> list = new List<Transform>();
			for (int i = 0; i < missionGroupsHolder.childCount; i++)
			{
				list.Add(missionGroupsHolder.GetChild(i));
			}
			if (list.Count == 0)
			{
				Debug.LogError("Need 1 button inside missionGroupsHolder");
				return;
			}
			MissionGroup.Init();
			MissionGroup[] array = new MissionGroup[3]
			{
				MissionGroup.Default,
				MissionGroup.User,
				MissionGroup.BuiltIn
			};
			while (list.Count < array.Length)
			{
				Transform item = UnityEngine.Object.Instantiate(list[0], missionGroupsHolder);
				list.Add(item);
			}
			while (list.Count > array.Length)
			{
				int index = list.Count - 1;
				UnityEngine.Object.DestroyImmediate(list[index].gameObject);
				list.RemoveAt(index);
			}
			if (missionGroups?.Length != array.Length)
			{
				Array.Resize(ref missionGroups, array.Length);
			}
			for (int j = 0; j < array.Length; j++)
			{
				MissionGroup missionGroup = array[j];
				Transform transform = list[j];
				missionGroups[j] = new MissionGroupButton
				{
					Name = missionGroup.Name,
					Button = transform.GetComponent<Button>(),
					StyleController = transform.GetComponent<ButtonStyleController>()
				};
				bool flag = missionGroup == MissionGroup.User;
				missionGroups[j].StyleController.ApplyStyle(flag ? activeStyle : inactiveStyle);
				transform.GetComponentInChildren<TextMeshProUGUI>().text = missionGroup.Name;
			}
		}
	}
}
