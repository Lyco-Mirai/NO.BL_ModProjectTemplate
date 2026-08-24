using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuclearOption.SavedMission;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class UnitBrowser : MonoBehaviour
	{
		[Serializable]
		private class FactionFilter
		{
			public Faction faction;

			public Toggle toggle;
		}

		[Serializable]
		private class CategoryFilter
		{
			public UnitDefinition definition;

			public Toggle toggle;
		}

		private class VehicleTypeFilter
		{
			public VehicleType vehicleType;

			public Toggle toggle;
		}

		private static readonly ProfilerMarker rebuildMarker = new ProfilerMarker("Rebuild");

		[Header("UI")]
		[SerializeField]
		private UnitSelection unitSelection;

		[SerializeField]
		private Transform rowContainer;

		[SerializeField]
		private UnitBrowserRow rowPrefab;

		[SerializeField]
		private Transform poolContainer;

		[SerializeField]
		private Transform unitBrowserWindow;

		[SerializeField]
		private Button unitBrowserToggleButton;

		[Header("Counters")]
		[SerializeField]
		private TMP_Text totalCountText;

		[SerializeField]
		private TMP_Text visibleCountText;

		[Header("Search")]
		[SerializeField]
		private Toggle allUnitsToggle;

		[SerializeField]
		private TMP_InputField searchField;

		[Header("Actions")]
		[SerializeField]
		private Button resetFiltersButton;

		[SerializeField]
		private Button selectVisibleButton;

		[Header("Filters")]
		[SerializeField]
		private Toggle includeMapUnitsToggle;

		[SerializeField]
		private Faction Boscali;

		[SerializeField]
		private Toggle BDFToggle;

		[SerializeField]
		private Faction Primeva;

		[SerializeField]
		private Toggle PALAToggle;

		[SerializeField]
		private Toggle neutralToggle;

		[SerializeField]
		private List<CategoryFilter> categoryFilters = new List<CategoryFilter>();

		[SerializeField]
		private Toggle miscCategoryToggle;

		[Header("Vehicle Type Filters")]
		[SerializeField]
		private Transform vehicleTypeFilterContainer;

		[SerializeField]
		private Toggle vehicleTypeFilterPrefab;

		private readonly List<VehicleTypeFilter> vehicleTypeFilters = new List<VehicleTypeFilter>();

		private readonly List<SavedUnit> allSavedUnits = new List<SavedUnit>();

		private readonly List<SavedUnit> visibleSavedUnits = new List<SavedUnit>();

		private readonly HashSet<Unit> selectedUnits = new HashSet<Unit>();

		private readonly List<UnitBrowserRow> rows = new List<UnitBrowserRow>();

		private readonly List<UnitBrowserRow> pooledRows = new List<UnitBrowserRow>();

		public static UnitBrowser I { get; private set; }

		private void Awake()
		{
			I = this;
			poolContainer.gameObject.SetActive(value: false);
			unitBrowserWindow.gameObject.SetActive(value: false);
			BuildVehicleTypeFilters();
			SubscribeEvents();
		}

		private void OnDestroy()
		{
			UnsubscribeEvents();
			if (I == this)
			{
				I = null;
			}
		}

		private void BuildVehicleTypeFilters()
		{
			vehicleTypeFilters.Clear();
			foreach (VehicleType value in Enum.GetValues(typeof(VehicleType)))
			{
				Toggle toggle = UnityEngine.Object.Instantiate(vehicleTypeFilterPrefab, vehicleTypeFilterContainer);
				toggle.name = $"VehicleType_{value}";
				toggle.SetIsOnWithoutNotify(value: true);
				toggle.GetComponentInChildren<TMP_Text>(includeInactive: true).text = value.ToString().Replace("_", "");
				toggle.onValueChanged.AddListener(Filter_OnChanged);
				vehicleTypeFilters.Add(new VehicleTypeFilter
				{
					vehicleType = value,
					toggle = toggle
				});
			}
		}

		private void SubscribeEvents()
		{
			unitSelection = SceneSingleton<UnitSelection>.i;
			MissionManager.onMissionLoad += OnLoadMission;
			unitSelection.OnSelect += Selection_OnChanged;
			resetFiltersButton.onClick.AddListener(ResetFilters);
			selectVisibleButton.onClick.AddListener(SelectVisibleUnits);
			allUnitsToggle.onValueChanged.AddListener(AllUnitsToggle_OnChanged);
			searchField.onValueChanged.AddListener(Search_OnChanged);
			includeMapUnitsToggle.onValueChanged.AddListener(Filter_OnChanged);
			BDFToggle.onValueChanged.AddListener(Filter_OnChanged);
			PALAToggle.onValueChanged.AddListener(Filter_OnChanged);
			neutralToggle.onValueChanged.AddListener(Filter_OnChanged);
			foreach (CategoryFilter categoryFilter in categoryFilters)
			{
				categoryFilter.toggle.onValueChanged.AddListener(Filter_OnChanged);
			}
			miscCategoryToggle.onValueChanged.AddListener(Filter_OnChanged);
			unitBrowserToggleButton.onClick.AddListener(ToggleWindow);
			SubscribeRightClick(BDFToggle, IsolateFactionFilter);
			SubscribeRightClick(PALAToggle, IsolateFactionFilter);
			SubscribeRightClick(neutralToggle, IsolateFactionFilter);
			foreach (CategoryFilter categoryFilter2 in categoryFilters)
			{
				SubscribeRightClick(categoryFilter2.toggle, IsolateCategoryFilter);
			}
			SubscribeRightClick(miscCategoryToggle, IsolateCategoryFilter);
			foreach (VehicleTypeFilter vehicleTypeFilter in vehicleTypeFilters)
			{
				SubscribeRightClick(vehicleTypeFilter.toggle, IsolateVehicleTypeFilter);
			}
		}

		private void UnsubscribeEvents()
		{
			MissionManager.onMissionLoad -= OnLoadMission;
			unitSelection.OnSelect -= Selection_OnChanged;
			resetFiltersButton.onClick.RemoveListener(ResetFilters);
			selectVisibleButton.onClick.RemoveListener(SelectVisibleUnits);
			allUnitsToggle.onValueChanged.RemoveListener(AllUnitsToggle_OnChanged);
			searchField.onValueChanged.RemoveListener(Search_OnChanged);
			includeMapUnitsToggle.onValueChanged.RemoveListener(Filter_OnChanged);
			BDFToggle.onValueChanged.RemoveListener(Filter_OnChanged);
			PALAToggle.onValueChanged.RemoveListener(Filter_OnChanged);
			neutralToggle.onValueChanged.RemoveListener(Filter_OnChanged);
			foreach (CategoryFilter categoryFilter in categoryFilters)
			{
				categoryFilter.toggle.onValueChanged.RemoveListener(Filter_OnChanged);
			}
			miscCategoryToggle.onValueChanged.RemoveListener(Filter_OnChanged);
			unitBrowserToggleButton.onClick.RemoveListener(ToggleWindow);
			UnsubscribeRightClick(BDFToggle, IsolateFactionFilter);
			UnsubscribeRightClick(PALAToggle, IsolateFactionFilter);
			UnsubscribeRightClick(neutralToggle, IsolateFactionFilter);
			foreach (CategoryFilter categoryFilter2 in categoryFilters)
			{
				UnsubscribeRightClick(categoryFilter2.toggle, IsolateCategoryFilter);
			}
			UnsubscribeRightClick(miscCategoryToggle, IsolateCategoryFilter);
			foreach (VehicleTypeFilter vehicleTypeFilter in vehicleTypeFilters)
			{
				UnsubscribeRightClick(vehicleTypeFilter.toggle, IsolateVehicleTypeFilter);
			}
		}

		private static void SubscribeRightClick(Toggle toggle, Action<Toggle> handler)
		{
			toggle.GetComponent<ToggleRightClick>().RightClicked += handler;
		}

		private static void UnsubscribeRightClick(Toggle toggle, Action<Toggle> handler)
		{
			toggle.GetComponent<ToggleRightClick>().RightClicked -= handler;
		}

		private void IsolateFactionFilter(Toggle selected)
		{
			BDFToggle.SetIsOnWithoutNotify(selected == BDFToggle);
			PALAToggle.SetIsOnWithoutNotify(selected == PALAToggle);
			neutralToggle.SetIsOnWithoutNotify(selected == neutralToggle);
			Rebuild();
		}

		private void IsolateCategoryFilter(Toggle selected)
		{
			foreach (CategoryFilter categoryFilter in categoryFilters)
			{
				categoryFilter.toggle.SetIsOnWithoutNotify(categoryFilter.toggle == selected);
			}
			miscCategoryToggle.SetIsOnWithoutNotify(selected == miscCategoryToggle);
			Rebuild();
		}

		private void IsolateVehicleTypeFilter(Toggle selected)
		{
			foreach (CategoryFilter categoryFilter in categoryFilters)
			{
				categoryFilter.toggle.SetIsOnWithoutNotify(categoryFilter.definition is VehicleDefinition);
			}
			miscCategoryToggle.SetIsOnWithoutNotify(value: false);
			foreach (VehicleTypeFilter vehicleTypeFilter in vehicleTypeFilters)
			{
				vehicleTypeFilter.toggle.SetIsOnWithoutNotify(vehicleTypeFilter.toggle == selected);
			}
			Rebuild();
		}

		private void ToggleWindow()
		{
			unitBrowserWindow.gameObject.SetActive(!unitBrowserWindow.gameObject.activeSelf);
			if (unitBrowserWindow.gameObject.activeSelf)
			{
				Rebuild();
			}
		}

		private void AllUnitsToggle_OnChanged(bool value)
		{
			Rebuild();
		}

		private void Selection_OnChanged(SelectionDetails _)
		{
			Rebuild();
		}

		private void OnLoadMission(Mission _)
		{
			Rebuild();
		}

		private void Filter_OnChanged(bool _)
		{
			Rebuild();
		}

		private void Search_OnChanged(string _)
		{
			Rebuild();
		}

		private static void FocusCamera(SavedUnit savedUnit)
		{
			Unit unit = savedUnit?.Unit;
			if (!(unit == null) && !(unit.definition == null) && !(SceneSingleton<CameraStateManager>.i == null))
			{
				float num = Mathf.Max(unit.definition.height, 1f);
				float num2 = Mathf.Max(unit.definition.length, 1f);
				SceneSingleton<CameraStateManager>.i.transform.position = unit.transform.position + 0.8f * num * Vector3.up - 2f * num2 * SceneSingleton<CameraStateManager>.i.transform.forward;
				SceneSingleton<CameraStateManager>.i.transform.LookAt(unit.transform);
			}
		}

		private void Row_OnToggleChanged(SavedUnit savedUnit, bool selected)
		{
			Unit unit = savedUnit?.Unit;
			if (unit == null)
			{
				return;
			}
			if (selected)
			{
				if (!selectedUnits.Contains(unit))
				{
					unitSelection.AddToMultiSelection(unit);
				}
			}
			else if (selectedUnits.Contains(unit))
			{
				Deselect(unit);
			}
		}

		public void SelectVisibleUnits()
		{
			List<Unit> list = new List<Unit>();
			foreach (SavedUnit visibleSavedUnit in visibleSavedUnits)
			{
				if (visibleSavedUnit?.Unit != null && PassesFilters(visibleSavedUnit))
				{
					list.Add(visibleSavedUnit.Unit);
				}
			}
			unitSelection.ReplaceSelection(list);
		}

		private void Deselect(Unit unit)
		{
			SelectionDetails selectionDetails = unitSelection.SelectionDetails;
			if (!(selectionDetails is UnitSelectionDetails unitSelectionDetails))
			{
				if (selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails && multiSelectSelectionDetails.Items.Any((SingleSelectionDetails x) => x is UnitSelectionDetails unitSelectionDetails2 && unitSelectionDetails2.Unit == unit))
				{
					unitSelection.RemoveFromMultiSelection(unit);
				}
			}
			else if (unitSelectionDetails.Unit == unit)
			{
				unitSelection.ClearSelection();
			}
		}

		public void ResetFilters()
		{
			searchField.SetTextWithoutNotify("");
			allUnitsToggle.SetIsOnWithoutNotify(value: false);
			includeMapUnitsToggle.SetIsOnWithoutNotify(value: false);
			BDFToggle.SetIsOnWithoutNotify(value: true);
			PALAToggle.SetIsOnWithoutNotify(value: true);
			neutralToggle.SetIsOnWithoutNotify(value: true);
			foreach (CategoryFilter categoryFilter in categoryFilters)
			{
				categoryFilter.toggle.SetIsOnWithoutNotify(value: true);
			}
			miscCategoryToggle.SetIsOnWithoutNotify(value: true);
			foreach (VehicleTypeFilter vehicleTypeFilter in vehicleTypeFilters)
			{
				vehicleTypeFilter.toggle.SetIsOnWithoutNotify(value: true);
			}
			Rebuild();
		}

		public void Rebuild()
		{
			if (!unitBrowserWindow.gameObject.activeInHierarchy)
			{
				return;
			}
			using (rebuildMarker.Auto())
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				RefreshUnitCache();
				PoolRows();
				visibleSavedUnits.Clear();
				CacheSelectedUnits();
				foreach (SavedUnit allSavedUnit in allSavedUnits)
				{
					Unit unit = allSavedUnit?.Unit;
					if (!(unit == null) && !(allUnitsToggle.isOn ? (!PassesFilters(allSavedUnit)) : (!selectedUnits.Contains(unit))))
					{
						visibleSavedUnits.Add(allSavedUnit);
					}
				}
				totalCountText.text = $"Total: {allSavedUnits.Count}";
				visibleCountText.text = $"Vis: {visibleSavedUnits.Count}";
				foreach (SavedUnit visibleSavedUnit in visibleSavedUnits)
				{
					UnitBrowserRow row = GetRow();
					rows.Add(row);
					row.Setup(visibleSavedUnit, selectedUnits.Contains(visibleSavedUnit.Unit), Row_OnToggleChanged, FocusCamera);
					row.gameObject.SetActive(value: true);
				}
				stopwatch.Stop();
				UnityEngine.Debug.Log($"[UnitBrowserWindow.Rebuild] VisibleUnits:{visibleSavedUnits.Count} |Time:{stopwatch.Elapsed.TotalMilliseconds:F2} ms |Norm:{stopwatch.Elapsed.TotalMilliseconds / (double)visibleSavedUnits.Count:F2} ms");
			}
		}

		private void RefreshUnitCache()
		{
			MissionManager.GetAllSavedUnitsNonAlloc(allSavedUnits, includeBuiltIn: true);
		}

		private void CacheSelectedUnits()
		{
			selectedUnits.Clear();
			SelectionDetails selectionDetails = unitSelection.SelectionDetails;
			if (!(selectionDetails is UnitSelectionDetails unitSelectionDetails))
			{
				if (!(selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails))
				{
					return;
				}
				{
					foreach (SingleSelectionDetails item in multiSelectSelectionDetails.Items)
					{
						if (item is UnitSelectionDetails unitSelectionDetails2 && unitSelectionDetails2.Unit != null)
						{
							selectedUnits.Add(unitSelectionDetails2.Unit);
						}
					}
					return;
				}
			}
			if (unitSelectionDetails.Unit != null)
			{
				selectedUnits.Add(unitSelectionDetails.Unit);
			}
		}

		private UnitBrowserRow GetRow()
		{
			if (pooledRows.Count > 0)
			{
				List<UnitBrowserRow> list = pooledRows;
				UnitBrowserRow result = list[list.Count - 1];
				pooledRows.RemoveAt(pooledRows.Count - 1);
				return result;
			}
			UnitBrowserRow unitBrowserRow = UnityEngine.Object.Instantiate(rowPrefab, rowContainer);
			unitBrowserRow.gameObject.SetActive(value: false);
			return unitBrowserRow;
		}

		private void PoolRow(UnitBrowserRow row)
		{
			row.gameObject.SetActive(value: false);
			row.Clear();
			pooledRows.Add(row);
		}

		private void PoolRows()
		{
			for (int num = rows.Count - 1; num >= 0; num--)
			{
				PoolRow(rows[num]);
			}
			rows.Clear();
		}

		public bool PassesFilters(SavedUnit savedUnit)
		{
			Unit unit = savedUnit?.Unit;
			if (unit == null)
			{
				return false;
			}
			if (!MatchesSearch(unit))
			{
				return false;
			}
			if (!MatchesMapUnitFilter(unit))
			{
				return false;
			}
			if (!MatchesFactionFilter(unit))
			{
				return false;
			}
			if (!MatchesCategoryFilter(unit))
			{
				return false;
			}
			if (unit is GroundVehicle && !MatchesVehicleTypeFilter(unit))
			{
				return false;
			}
			return true;
		}

		private bool MatchesSearch(Unit unit)
		{
			string value = searchField.text.Trim();
			if (string.IsNullOrEmpty(value))
			{
				return true;
			}
			return UnitBrowserRow.GetLabel(unit).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private bool MatchesMapUnitFilter(Unit unit)
		{
			if (!includeMapUnitsToggle.isOn)
			{
				return string.IsNullOrEmpty(unit.MapUniqueName);
			}
			return true;
		}

		private bool MatchesFactionFilter(Unit unit)
		{
			Faction faction = ((unit.NetworkHQ != null) ? unit.NetworkHQ.faction : null);
			if (faction == Boscali)
			{
				return BDFToggle.isOn;
			}
			if (faction == Primeva)
			{
				return PALAToggle.isOn;
			}
			return neutralToggle.isOn;
		}

		private bool MatchesCategoryFilter(Unit unit)
		{
			Type type = unit.definition.GetType();
			foreach (CategoryFilter categoryFilter in categoryFilters)
			{
				if (categoryFilter.definition.GetType() == type)
				{
					return categoryFilter.toggle.isOn;
				}
			}
			return miscCategoryToggle.isOn;
		}

		private bool MatchesVehicleTypeFilter(Unit unit)
		{
			if (!(unit.definition is VehicleDefinition vehicleDefinition))
			{
				return true;
			}
			foreach (VehicleTypeFilter vehicleTypeFilter in vehicleTypeFilters)
			{
				if (vehicleTypeFilter.vehicleType == vehicleDefinition.vehicleType)
				{
					return vehicleTypeFilter.toggle.isOn;
				}
			}
			return true;
		}
	}
}
