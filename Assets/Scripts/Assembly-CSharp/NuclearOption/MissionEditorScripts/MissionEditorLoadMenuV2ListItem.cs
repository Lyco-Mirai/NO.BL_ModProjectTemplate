using NuclearOption.SavedMission;
using NuclearOption.SavedMission.ConvertVersions;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionEditorLoadMenuV2ListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		private static readonly ProfilerMarker setupMarker = new ProfilerMarker("MissionEditorLoadMenuV2ListItem.Setup");

		[Header("UI References")]
		public TextMeshProUGUI MissionNameText;

		public TextMeshProUGUI LastModifiedText;

		public TextMeshProUGUI VersionText;

		[SerializeField]
		private Color oldVersionColor = new Color(1f, 0.6f, 0.2f);

		[SerializeField]
		private Color currentVersionColor = new Color(0.7f, 0.7f, 0.7f);

		[SerializeField]
		private SmartDateFormatter.Theme dateTheme = SmartDateFormatter.Theme.Default;

		[Header("Expand")]
		public Button expandButton;

		public TextMeshProUGUI ExpanderArrow;

		public GameObject SubItemContainer;

		[Header("Load")]
		[SerializeField]
		private float doubleClickThreshold = 0.3f;

		private float lastClickTime;

		private bool isExpanded;

		private IMissionEditorFileMenu menu;

		private MissionFileInfo data;

		private bool subItemsCreated;

		private void Awake()
		{
			lastClickTime = -100f;
			if (expandButton != null)
			{
				expandButton.onClick.AddListener(delegate
				{
					SetExpand(!isExpanded);
				});
			}
		}

		public void Setup(IMissionEditorFileMenu menu, MissionFileInfo data)
		{
			using (setupMarker.Auto())
			{
				ClearSubList();
				this.menu = menu;
				this.data = data;
				MissionNameText.text = ((data.key.Group == MissionGroup.EditorMissions) ? data.key.Key : data.key.Name);
				LastModifiedText.text = SmartDateFormatter.ToSmartDate(data.LastEdit, dateTheme);
				if (data.key.TryQuickLoad(out var mission))
				{
					VersionText.text = $"v{mission.JsonVersion}";
					VersionText.color = ((mission.JsonVersion < MissionVersionUpgrade.LatestVersion) ? oldVersionColor : currentVersionColor);
				}
				else
				{
					VersionText.text = "";
				}
				if (expandButton != null)
				{
					SetExpand(isExpanded: false);
					expandButton.gameObject.SetActive(data.HasExtraSaves);
					if (!data.HasExtraSaves)
					{
						ExpanderArrow.text = "";
					}
				}
			}
		}

		public void SetExpand(bool isExpanded)
		{
			if (data.HasExtraSaves)
			{
				this.isExpanded = isExpanded;
				SubItemContainer.SetActive(isExpanded);
				ExpanderArrow.text = (isExpanded ? "\ue5c5" : "\ue5df");
				LastModifiedText.text = SmartDateFormatter.ToSmartDate(isExpanded ? data.ExpandedLastEdit : data.LastEdit, dateTheme);
				if (isExpanded && !subItemsCreated)
				{
					subItemsCreated = true;
					CreateSubList();
				}
				FixLayout.ForceRebuildRecursive(base.transform.AsRectTransform());
			}
		}

		public void CreateSubList()
		{
			foreach (MissionFileInfo subItem in data.SubItems)
			{
				Object.Instantiate(menu.SubItemPrefab, SubItemContainer.transform).Setup(menu, subItem);
			}
		}

		public void ClearSubList()
		{
			subItemsCreated = false;
			if (!(SubItemContainer == null))
			{
				for (int num = SubItemContainer.transform.childCount - 1; num >= 0; num--)
				{
					Transform child = SubItemContainer.transform.GetChild(num);
					child.gameObject.SetActive(value: false);
					Object.Destroy(child.gameObject);
				}
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			float num = Time.unscaledTime - lastClickTime;
			Debug.Log($"Click: last:{lastClickTime}, delta:{num}");
			if (num <= doubleClickThreshold)
			{
				menu.LoadMission(data.key);
			}
			else
			{
				menu.SelectMission(data.key);
			}
			lastClickTime = Time.unscaledTime;
		}
	}
}
