using System.Collections.Generic;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class AirbasePanel : MonoBehaviour
	{
		private static readonly string builtInAirbaseMessage = "default airbase, some value can not be changed";

		private static readonly string attachedAirbaseMessage = "attached airbase, some value can not be changed";

		[SerializeField]
		private Button removeButton;

		[SerializeField]
		private TextMeshProUGUI removeText;

		[SerializeField]
		private TextMeshProUGUI builtInNotice;

		[SerializeField]
		private TMP_InputField airbaseUniqueName;

		[SerializeField]
		private TMP_InputField airbaseDisplayName;

		[SerializeField]
		private TMP_Dropdown factionDropdown;

		[SerializeField]
		private Toggle disableToggle;

		[SerializeField]
		private Toggle capturableToggle;

		[SerializeField]
		private Slider captureDefenseSlider;

		[SerializeField]
		private TextMeshProUGUI captureDefenseSliderLabel;

		[SerializeField]
		private Slider captureRangeSlider;

		[SerializeField]
		private TextMeshProUGUI captureRangeSliderLabel;

		[SerializeField]
		private Color captureRangeSliderLabelNormalColor;

		[SerializeField]
		private Color captureRangeSliderLabelInactiveColor;

		[SerializeField]
		private Image captureRangeSliderFill;

		[SerializeField]
		private Color captureRangeSliderFillNormalColor;

		[SerializeField]
		private Color captureRangeSliderFillInactiveColor;

		[SerializeField]
		private Button roadButton;

		[SerializeField]
		private Button unitButton;

		[Header("custom airbase fields")]
		[SerializeField]
		private Vector3DataField centerPositionField;

		[SerializeField]
		private Vector3DataField selectionPositionField;

		[SerializeField]
		private ReferenceDataField towerReferenceField;

		[SerializeField]
		private ReferenceList buildingList;

		[SerializeField]
		private RectTransform dataDrawerParent;

		[SerializeField]
		private ReferenceList runwayList;

		[Header("References")]
		[SerializeField]
		private UIPrefabs uiPrefabs;

		[SerializeField]
		private PositionHandle positionHandlePrefab;

		[SerializeField]
		private RoadEditor roadEditorPrefab;

		[SerializeField]
		private RunwayTab runwayTabPrefab;

		private PositionHandle centerHandle;

		private PositionHandle selectionHandle;

		private EditorTabs editorTabs;

		private DataDrawer dataDrawer;

		private List<string> factionOptions;

		private Dictionary<ValueWrapperGlobalPosition, PositionHandle> verticalLandingHandles = new Dictionary<ValueWrapperGlobalPosition, PositionHandle>();

		private Dictionary<ValueWrapperGlobalPosition, PositionHandle> servicePointsHandles = new Dictionary<ValueWrapperGlobalPosition, PositionHandle>();

		private Airbase airbase;

		private SavedAirbase savedAirbase;

		private void Awake()
		{
			editorTabs = GetComponentInParent<EditorTabs>();
			dataDrawer = new DataDrawer(dataDrawerParent, uiPrefabs);
			dataDrawer.Width = dataDrawerParent.sizeDelta.x;
			removeButton.onClick.AddListener(RemoveAirbase);
			airbaseUniqueName.onEndEdit.AddListener(AirbaseUniqueNameChanged);
			airbaseDisplayName.onEndEdit.AddListener(AirbaseDisplayNameChanged);
			factionDropdown.onValueChanged.AddListener(FactionChanged);
			disableToggle.onValueChanged.AddListener(DisabledChanged);
			capturableToggle.onValueChanged.AddListener(CapturableChanged);
			captureDefenseSlider.minValue = 1f;
			captureDefenseSlider.maxValue = 1000f;
			captureDefenseSlider.onValueChanged.AddListener(CaptureDefenseChanged);
			captureRangeSlider.minValue = 10f;
			captureRangeSlider.maxValue = 10000f;
			captureRangeSlider.onValueChanged.AddListener(CaptureRangeChanged);
			roadButton.onClick.AddListener(RoadButtonClicked);
			unitButton.onClick.AddListener(UnitButtonClicked);
		}

		private void Start()
		{
			PopulateFactionOptions();
			buildingList.TitleText.text = "Buildings";
			buildingList.SelectExistingDropdown.HideUnitTypeFilter();
			AddNoAirbaseFilterToggle();
			buildingList.ItemAdded += BuildListItemAdded;
			buildingList.ItemRemoved += BuildListItemRemoved;
			runwayList.TitleText.text = "Runways";
			runwayList.SetHeight(300);
			SceneSingleton<UnitSelection>.i.OnSelect += AirbaseMenu_OnSelect;
			Setup((SceneSingleton<UnitSelection>.i.SelectionDetails as AirbaseSelectionDetails)?.Airbase);
		}

		private void AddNoAirbaseFilterToggle()
		{
			ReferencePopup selectExistingDropdown = buildingList.SelectExistingDropdown;
			FilterSet.SetupToggleFilter(Object.Instantiate(dataDrawer.Prefabs.BoolFieldPrefab, selectExistingDropdown.filterHolder.transform), "noAirbase", "No Airbase", value: true, selectExistingDropdown.FilterSet, NoAirbase);
			static bool NoAirbase(object savedRef)
			{
				return string.IsNullOrEmpty(((SavedBuilding)savedRef).Airbase);
			}
		}

		private void OnDestroy()
		{
			Cleanup();
			SceneSingleton<UnitSelection>.i.OnSelect -= AirbaseMenu_OnSelect;
		}

		private void Cleanup()
		{
			if (centerHandle != null)
			{
				Object.Destroy(centerHandle.gameObject);
			}
			if (selectionHandle != null)
			{
				Object.Destroy(selectionHandle.gameObject);
			}
			foreach (PositionHandle value in verticalLandingHandles.Values)
			{
				if (value != null)
				{
					Object.Destroy(value.gameObject);
				}
			}
			verticalLandingHandles.Clear();
			foreach (PositionHandle value2 in servicePointsHandles.Values)
			{
				if (value2 != null)
				{
					Object.Destroy(value2.gameObject);
				}
			}
			servicePointsHandles.Clear();
		}

		public void Setup(Airbase airbase)
		{
			Cleanup();
			this.airbase = airbase;
			savedAirbase = ((airbase != null) ? airbase.SavedAirbase : null);
			bool flag;
			bool flag2;
			bool flag3;
			bool flag4;
			bool interactable;
			if (airbase == null)
			{
				flag = false;
				flag2 = false;
				flag3 = false;
				flag4 = false;
				interactable = false;
			}
			else
			{
				flag = true;
				flag2 = airbase.BuiltIn;
				flag3 = airbase.AttachedAirbase;
				flag4 = !flag2 && !flag3;
				interactable = airbase.SavedAirbaseOverride;
			}
			if (flag2)
			{
				builtInNotice.text = builtInAirbaseMessage;
				builtInNotice.gameObject.SetActive(value: true);
			}
			else if (flag3)
			{
				builtInNotice.text = attachedAirbaseMessage;
				builtInNotice.gameObject.SetActive(value: true);
			}
			else
			{
				builtInNotice.gameObject.SetActive(value: false);
			}
			airbaseUniqueName.interactable = flag4;
			airbaseDisplayName.interactable = flag;
			factionDropdown.interactable = flag4 || flag2;
			disableToggle.interactable = flag;
			capturableToggle.interactable = flag;
			captureDefenseSlider.interactable = flag;
			captureRangeSlider.interactable = flag4;
			captureRangeSliderLabel.color = (flag4 ? captureRangeSliderLabelNormalColor : captureRangeSliderLabelInactiveColor);
			captureRangeSliderFill.color = (flag4 ? captureRangeSliderFillNormalColor : captureRangeSliderFillInactiveColor);
			removeButton.interactable = interactable;
			removeText.text = (flag4 ? "Delete airbase" : "Remove overrides");
			airbaseUniqueName.SetTextWithoutNotify(savedAirbase?.UniqueName);
			airbaseDisplayName.SetTextWithoutNotify(savedAirbase?.DisplayName);
			int valueWithoutNotify = ((airbase != null && airbase.CurrentHQ != null) ? factionOptions.IndexOf(airbase.CurrentHQ.faction.factionName) : 0);
			factionDropdown.SetValueWithoutNotify(valueWithoutNotify);
			disableToggle.SetIsOnWithoutNotify(savedAirbase?.Disabled ?? false);
			capturableToggle.SetIsOnWithoutNotify(savedAirbase?.Capturable ?? false);
			captureDefenseSlider.SetValueWithoutNotify(savedAirbase?.CaptureDefense ?? 1f);
			captureDefenseSliderLabel.text = captureDefenseSlider.value.ToString("0.0");
			captureRangeSlider.SetValueWithoutNotify(savedAirbase?.CaptureRange ?? 1000f);
			captureRangeSliderLabel.text = captureRangeSlider.value.ToString("0.0");
			roadButton.gameObject.SetActive(flag2 || flag4);
			unitButton.gameObject.SetActive(flag3);
			if (flag4)
			{
				centerHandle = Object.Instantiate(positionHandlePrefab);
				centerHandle.SetHue(Color.green);
				centerHandle.Setup(savedAirbase.CenterWrapper, () => "Center " + airbase.SavedAirbase.DisplayName, null);
				centerPositionField.Setup("Center", savedAirbase.CenterWrapper);
				selectionHandle = Object.Instantiate(positionHandlePrefab);
				selectionHandle.SetHue(Color.blue);
				selectionHandle.Setup(savedAirbase.SelectionPositionWrapper, () => "Selection " + airbase.SavedAirbase.DisplayName, null);
				selectionPositionField.Setup("Selection", savedAirbase.SelectionPositionWrapper);
			}
			else
			{
				centerPositionField.SetupReadOnly("Center", (airbase != null) ? airbase.center.GlobalPosition() : default(GlobalPosition));
				selectionPositionField.SetupReadOnly("Selection", (airbase != null) ? airbase.aircraftSelectionTransform.GlobalPosition() : default(GlobalPosition));
			}
			if (flag && !flag3)
			{
				towerReferenceField.Popup.HideUnitTypeFilter();
				List<SavedBuilding> list = new List<SavedBuilding>();
				MissionManager.GetAllSavedBuildingsNonAlloc(list, includeBuiltIn: true);
				towerReferenceField.Setup("Tower", list, savedAirbase.TowerRef, delegate(SavedBuilding value)
				{
					CheckOverride();
					savedAirbase.TowerRef = value;
					if (value != null && value.AirbaseRef != savedAirbase)
					{
						value.SetAirbase(savedAirbase);
					}
				});
				buildingList.SetHeight(300);
				buildingList.SetupList(savedAirbase.BuildingsRef, PickerName, (SavedBuilding x) => x.ToUIString(), delegate
				{
					List<SavedBuilding> list2 = new List<SavedBuilding>();
					MissionManager.GetAllSavedBuildingsNonAlloc(list2, includeBuiltIn: true);
					return list2;
				}, ReferenceList.ButtonsEvents.OverrideNullOnly);
			}
			else
			{
				towerReferenceField.gameObject.SetActive(value: false);
				buildingList.gameObject.SetActive(value: false);
			}
			if (flag4)
			{
				SetupVerticalLandingList();
				SetupServicePointsList();
				runwayList.Setup(CreateNewRunway, null, ShowRunwayPanel, RemoveRunway);
				runwayList.SetupList(savedAirbase.runways, (SavedRunway o) => o.ToUIString(), ReferenceList.ButtonsEvents.DontAdd);
			}
			else
			{
				runwayList.gameObject.SetActive(value: false);
			}
			FixLayout.ForceRebuildRecursive((RectTransform)base.transform);
			static string PickerName(SavedBuilding building)
			{
				if (string.IsNullOrEmpty(building.Airbase))
				{
					return building.UniqueName;
				}
				return building.UniqueName + " - " + building.Airbase.AddColor(new Color(0.4f, 0.4f, 0.4f)).AddSize(0.75f);
			}
		}

		private void SetupVerticalLandingList()
		{
			EmptyDataList emptyDataList = dataDrawer.DrawList(300, savedAirbase.VerticalLandingPointsWrappers, DrawVector3ListContent, CreateWrapper, OnDelete);
			emptyDataList.AllowSwapItems = false;
			emptyDataList.TitleText.text = "Vertical Landing Points";
			emptyDataList.RefreshList();
			for (int i = 0; i < savedAirbase.VerticalLandingPointsWrappers.Count; i++)
			{
				ValueWrapperGlobalPosition wrapper = savedAirbase.VerticalLandingPointsWrappers[i];
				OnCreate(i, wrapper);
			}
			ValueWrapperGlobalPosition CreateWrapper()
			{
				ValueWrapperGlobalPosition valueWrapperGlobalPosition = new ValueWrapperGlobalPosition(savedAirbase.CenterWrapper.Value + Vector3.up * 5f);
				int count = savedAirbase.VerticalLandingPointsWrappers.Count;
				OnCreate(count, valueWrapperGlobalPosition);
				return valueWrapperGlobalPosition;
			}
			void OnCreate(int index, ValueWrapperGlobalPosition valueWrapperGlobalPosition)
			{
				PositionHandle positionHandle = Object.Instantiate(positionHandlePrefab);
				positionHandle.SetHue(Color.yellow);
				positionHandle.Setup(valueWrapperGlobalPosition, () => $"Vertical Landing {index + 1} " + airbase.SavedAirbase.DisplayName, null);
				verticalLandingHandles[valueWrapperGlobalPosition] = positionHandle;
			}
			void OnDelete(ValueWrapperGlobalPosition item)
			{
				if (verticalLandingHandles.TryGetValue(item, out var value))
				{
					SceneSingleton<UnitSelection>.i.ClearIfSelected(value);
					Object.Destroy(value.gameObject);
				}
				else
				{
					Debug.LogError("Could not find handle");
				}
			}
		}

		private void SetupServicePointsList()
		{
			EmptyDataList emptyDataList = dataDrawer.DrawList(300, savedAirbase.ServicePointsWrappers, DrawVector3ListContent, CreateWrapper, OnDelete);
			emptyDataList.AllowSwapItems = false;
			emptyDataList.TitleText.text = "Service Points";
			emptyDataList.RefreshList();
			for (int i = 0; i < savedAirbase.ServicePointsWrappers.Count; i++)
			{
				ValueWrapperGlobalPosition wrapper = savedAirbase.ServicePointsWrappers[i];
				OnCreate(i, wrapper);
			}
			ValueWrapperGlobalPosition CreateWrapper()
			{
				ValueWrapperGlobalPosition valueWrapperGlobalPosition = new ValueWrapperGlobalPosition(savedAirbase.CenterWrapper.Value + Vector3.up * 5f);
				int count = savedAirbase.ServicePointsWrappers.Count;
				OnCreate(count, valueWrapperGlobalPosition);
				return valueWrapperGlobalPosition;
			}
			void OnCreate(int index, ValueWrapperGlobalPosition valueWrapperGlobalPosition)
			{
				PositionHandle positionHandle = Object.Instantiate(positionHandlePrefab);
				positionHandle.SetHue(new Color(1f, 0.3f, 0.08f));
				positionHandle.Setup(valueWrapperGlobalPosition, () => $"Service points {index + 1} " + airbase.SavedAirbase.DisplayName, null);
				servicePointsHandles[valueWrapperGlobalPosition] = positionHandle;
			}
			void OnDelete(ValueWrapperGlobalPosition item)
			{
				if (servicePointsHandles.TryGetValue(item, out var value))
				{
					SceneSingleton<UnitSelection>.i.ClearIfSelected(value);
					Object.Destroy(value.gameObject);
				}
				else
				{
					Debug.LogError("Could not find handle");
				}
			}
		}

		private static void DrawVector3ListContent(int index, ValueWrapperGlobalPosition wrapper, DataDrawer dataDrawer)
		{
			Vector3DataField vector3DataField = dataDrawer.InstantiateWithParent(dataDrawer.Prefabs.VectorFieldPrefab);
			vector3DataField.LabelLayout.minWidth = 40f;
			vector3DataField.Setup($"{index + 1}", wrapper);
		}

		private void CreateNewRunway()
		{
			SavedRunway savedRunway = new SavedRunway();
			savedRunway.Start = savedAirbase.Center - new Vector3(50f, 0f, 0f);
			savedRunway.End = savedAirbase.Center - new Vector3(-50f, 0f, 0f);
			savedAirbase.runways.Add(savedRunway);
			ShowRunwayPanel(savedRunway);
		}

		private void ShowRunwayPanel(SavedRunway runway)
		{
			Airbase airbase = this.airbase;
			editorTabs.ChangeTab(runwayTabPrefab, clearUnit: true).Setup(airbase, runway);
		}

		private void ShowRunwayPanel(int index)
		{
			SavedRunway runway = savedAirbase.runways[index];
			ShowRunwayPanel(runway);
		}

		private void RemoveRunway(int index)
		{
			savedAirbase.runways.RemoveAt(index);
			runwayList.RefreshList();
		}

		private void BuildListItemAdded(ISaveableReference savedRef)
		{
			CheckOverride();
			((SavedBuilding)savedRef).SetAirbase(savedAirbase);
		}

		private void BuildListItemRemoved(ISaveableReference savedRef)
		{
			((SavedBuilding)savedRef).RemoveAirbase();
		}

		private void AirbaseMenu_OnSelect(SelectionDetails selectionDetails)
		{
			if (selectionDetails == null)
			{
				Object.Destroy(base.gameObject);
			}
		}

		private void PopulateFactionOptions()
		{
			factionOptions = FactionHelper.GetFactionsAndNeutral();
			factionDropdown.ClearOptions();
			factionDropdown.AddOptions(factionOptions);
		}

		public static string NewAirbaseUniqueName(string nameField)
		{
			bool flag = string.IsNullOrEmpty(nameField);
			string text = (flag ? "CustomAirbase" : nameField);
			string text2 = text;
			Dictionary<string, Airbase> allAirbase = MissionManager.GetAllAirbase();
			SaveHelper.MakeUnique(ref text2, allAirbase, !flag);
			ColorLog<Airbase>.Info("Creating Unique airbase name, want=" + text + " unique=" + text2);
			return text2;
		}

		private void CheckOverride()
		{
			if (!airbase.SavedAirbaseOverride)
			{
				CreateOverride();
			}
		}

		private void CreateOverride()
		{
			string uniqueName = airbase.SavedAirbase.UniqueName;
			Mission currentMission = MissionManager.CurrentMission;
			int num = currentMission.airbases.FindIndex((SavedAirbase x) => x.UniqueName == uniqueName);
			if (num != -1)
			{
				Debug.LogError("Airbase with name " + uniqueName + " already in mission but trying to create a new override for it");
				savedAirbase = currentMission.airbases[num];
			}
			else
			{
				savedAirbase = SavedAirbase.CreateOverride(airbase.SavedAirbase);
				savedAirbase.SavedInMission = true;
				currentMission.airbases.Add(savedAirbase);
			}
			SavedAirbase oldRef = airbase.SavedAirbase;
			airbase.LinkSavedAirbase(savedAirbase, customAirbase: false);
			currentMission.ReferenceReplaced(oldRef, savedAirbase);
			removeButton.interactable = true;
		}

		public void RemoveAirbase()
		{
			MissionEditor.RemoveAirbase(airbase);
			if (airbase.BuiltIn || airbase.AttachedAirbase)
			{
				Setup(airbase);
				return;
			}
			airbase = null;
			savedAirbase = null;
			Object.Destroy(base.gameObject);
		}

		private void RoadButtonClicked()
		{
			Airbase airbase = this.airbase;
			editorTabs.ChangeTab(roadEditorPrefab, clearUnit: true).SelectNetwork(airbase, focus: false);
		}

		private void UnitButtonClicked()
		{
			if (airbase.TryGetAttachedUnit(out var attachedUnit))
			{
				SceneSingleton<UnitSelection>.i.SetSelection(attachedUnit);
			}
		}

		private void AirbaseUniqueNameChanged(string value)
		{
			if (value.StartsWith("<UNIT_AIRBASE>++"))
			{
				value = value.Substring("<UNIT_AIRBASE>++".Length);
			}
			if (savedAirbase != null)
			{
				string text = NewAirbaseUniqueName(value);
				FactionRegistry.ChangeAirbaseName(airbase, text);
				savedAirbase.Rename(text);
				airbaseUniqueName.SetTextWithoutNotify(text);
				airbase.name = text;
			}
		}

		private void AirbaseDisplayNameChanged(string value)
		{
			CheckOverride();
			if (savedAirbase != null)
			{
				savedAirbase.DisplayName = value;
			}
		}

		private void FactionChanged(int index)
		{
			CheckOverride();
			string text = factionOptions[index];
			savedAirbase.faction = text;
			foreach (SavedBuilding item in savedAirbase.BuildingsRef)
			{
				item.faction = text;
				if (item.Unit != null)
				{
					item.Unit.NetworkHQ = FactionRegistry.HqFromName(text);
				}
			}
			buildingList.RefreshList();
			airbase.EditorSetFaction(text, setAttachedAirbaseFaction: false);
		}

		private void DisabledChanged(bool value)
		{
			CheckOverride();
			if (savedAirbase != null)
			{
				savedAirbase.Disabled = value;
			}
		}

		private void CapturableChanged(bool value)
		{
			CheckOverride();
			if (savedAirbase != null)
			{
				savedAirbase.Capturable = value;
			}
		}

		private void CaptureDefenseChanged(float value)
		{
			CheckOverride();
			if (savedAirbase != null)
			{
				savedAirbase.CaptureDefense = value;
			}
			captureDefenseSliderLabel.text = value.ToString("0.0");
		}

		private void CaptureRangeChanged(float value)
		{
			CheckOverride();
			if (savedAirbase != null)
			{
				savedAirbase.CaptureRange = value;
			}
			if (airbase != null)
			{
				AirbaseEditorRadius airbaseEditorRadius = AirbaseEditorRadius.Find(airbase.center);
				if (airbaseEditorRadius != null)
				{
					airbaseEditorRadius.Setup(airbase.CurrentHQ.GetColorOrGray(), value);
				}
			}
			captureRangeSliderLabel.text = value.ToString("0.0");
		}
	}
}
