using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class ObjectiveEditorV2 : MonoBehaviour, ISidePanel
	{
		[SerializeField]
		private UIPrefabs prefabs;

		[Header("Panels")]
		[SerializeField]
		private RectTransform listParent;

		[SerializeField]
		private NewObjectivePanel newObjectivePanelPrefab;

		[SerializeField]
		private EditObjectivePanel editObjectivePanelPrefab;

		[SerializeField]
		private NewOutcomePanel newOutcomePanelPrefab;

		[SerializeField]
		private EditOutcomePanel editOutcomePanelPrefab;

		[SerializeField]
		private int listPanelHeight = 600;

		[Header("Buttons")]
		[SerializeField]
		private Button showObjectivesButton;

		[SerializeField]
		private Button showOutcomesButton;

		[SerializeField]
		private Button showUnitsButton;

		[SerializeField]
		private Button showAirbaseButton;

		[SerializeField]
		private Button showUnitTypesButton;

		private readonly List<SavedUnit> unitList = new List<SavedUnit>();

		private readonly List<SavedAirbase> airbaseList = new List<SavedAirbase>();

		private EditorTabs editorTabs;

		private ReferenceList activeList;

		private SidePanel selectedPanel;

		public SidePanel Panel { get; set; }

		private void Awake()
		{
			showObjectivesButton.onClick.AddListener(ShowObjectiveList);
			showOutcomesButton.onClick.AddListener(ShowOutcomeList);
			showUnitsButton.onClick.AddListener(ShowUnitList);
			showAirbaseButton.onClick.AddListener(ShowAirbaseList);
			showUnitTypesButton.onClick.AddListener(ShowUnitTypesList);
			editorTabs = GetComponentInParent<EditorTabs>();
			Panel = new SidePanel(editorTabs, this);
			selectedPanel = new SidePanel(editorTabs);
			SidePanel sidePanel = new SidePanel(editorTabs);
			Panel.Child = selectedPanel;
			selectedPanel.Child = sidePanel;
			selectedPanel.Parent = Panel;
			sidePanel.Parent = selectedPanel;
		}

		private void OnEnable()
		{
			ShowObjectiveList();
		}

		public void ShowObjectiveList()
		{
			CreateReferenceList();
			activeList.TitleText.text = "Objectives";
			activeList.SetHeight(listPanelHeight);
			activeList.Setup(ShowNewObjectivePanel, null, ShowEditObjective, RemoveObjective);
			List<Objective> allObjectives = MissionManager.CurrentMission.RuntimeObjectives.AllObjectives;
			activeList.SetupList(allObjectives, (Objective o) => o.ToUIString(), ReferenceList.ButtonsEvents.DontAdd);
			FilterSet.AddFilterFaction(activeList.transform, activeList.FilterSet, prefabs.Dropdown);
		}

		public void ShowOutcomeList()
		{
			CreateReferenceList();
			activeList.TitleText.text = "Outcomes";
			activeList.SetHeight(listPanelHeight);
			activeList.Setup(ShowNewOutcomePanel, null, ShowEditOutcome, RemoveOutcome);
			List<Outcome> allOutcomes = MissionManager.Objectives.AllOutcomes;
			activeList.SetupList(allOutcomes, (Outcome o) => o.ToUIString(), ReferenceList.ButtonsEvents.DontAdd);
		}

		public void ShowUnitList()
		{
			CreateReferenceList();
			activeList.TitleText.text = "Units";
			activeList.EditButtonText = "Focus";
			activeList.SetHeight(listPanelHeight);
			activeList.Setup(null, null, FocusUnit, RemoveUnit);
			_ = MissionManager.CurrentMission;
			MissionManager.GetAllSavedUnitsNonAlloc(unitList, includeBuiltIn: true);
			activeList.SetupList(unitList, (SavedUnit o) => o.ToUIString());
			FilterSet.AddFilterUnitType(activeList.transform, activeList.FilterSet, prefabs.Dropdown);
			FilterSet.AddFilterFaction(activeList.transform, activeList.FilterSet, prefabs.Dropdown);
			FilterSet.AddFilterPlacement(activeList.transform, activeList.FilterSet, prefabs.Dropdown, includeAttached: false);
		}

		public void ShowAirbaseList()
		{
			CreateReferenceList();
			activeList.TitleText.text = "Airbase";
			activeList.EditButtonText = "Focus";
			activeList.SetHeight(listPanelHeight);
			activeList.Setup(null, null, FocusAirbase, null);
			_ = MissionManager.CurrentMission;
			MissionManager.GetAllSavedAirbaseNonAlloc(airbaseList);
			activeList.SetupList(airbaseList, (SavedAirbase o) => o.ToUIString());
			FilterSet.AddFilterFaction(activeList.transform, activeList.FilterSet, prefabs.Dropdown);
			FilterSet.AddFilterPlacement(activeList.transform, activeList.FilterSet, prefabs.Dropdown, includeAttached: true);
		}

		public void ShowUnitTypesList()
		{
			throw new NotImplementedException();
		}

		private void CreateReferenceList()
		{
			if (activeList != null)
			{
				UnityEngine.Object.Destroy(activeList.gameObject);
			}
			activeList = UnityEngine.Object.Instantiate(prefabs.ReferenceListPrefab, listParent);
			selectedPanel.Destroy();
			editorTabs.RequestRebuild();
		}

		void ISidePanel.PanelRefresh()
		{
			if (activeList != null)
			{
				activeList.RefreshList();
			}
		}

		private void ShowNewObjectivePanel()
		{
			selectedPanel.Create(newObjectivePanelPrefab);
		}

		private void ShowEditObjective(int index)
		{
			MissionObjectives objectives = MissionManager.Objectives;
			ShowEditObjective(objectives.AllObjectives[index]);
		}

		public void ShowEditObjective(Objective objective)
		{
			selectedPanel.Create(editObjectivePanelPrefab).SetObjective(objective);
		}

		private void RemoveObjective(int index)
		{
			MissionManager.Objectives.RemoveObjectiveAt(index);
			ShowObjectiveList();
		}

		private void ShowNewOutcomePanel()
		{
			selectedPanel.Create(newOutcomePanelPrefab).Setup(null);
		}

		public void ShowEditOutcome(int index)
		{
			MissionObjectives objectives = MissionManager.Objectives;
			ShowEditOutcome(objectives.AllOutcomes[index]);
		}

		public void ShowEditOutcome(Outcome outcome)
		{
			selectedPanel.Create(editOutcomePanelPrefab).SetOutcome(outcome);
		}

		private void RemoveOutcome(int index)
		{
			MissionManager.Objectives.RemoveOutcomeAt(index);
			ShowOutcomeList();
		}

		private void FocusUnit(int index)
		{
			FocusUnit(unitList[index]);
		}

		private static void FocusUnit(SavedUnit savedUnit)
		{
			Unit unit = savedUnit.Unit;
			SceneSingleton<CameraStateManager>.i.SetFollowingUnit(unit);
			SceneSingleton<UnitSelection>.i.SetSelection(unit);
		}

		private void RemoveUnit(int index)
		{
			SavedUnit saved = unitList[index];
			SceneSingleton<MissionEditor>.i.RemoveUnit(saved);
			unitList.RemoveAt(index);
			activeList.RefreshList();
		}

		private void FocusAirbase(int index)
		{
			FocusAirbase(airbaseList[index]);
		}

		private static void FocusAirbase(SavedAirbase savedAirbase)
		{
			Airbase airbase = savedAirbase.Airbase;
			SceneSingleton<CameraStateManager>.i.FocusAirbase(airbase, allowMoveToDropFocus: true);
			SceneSingleton<UnitSelection>.i.SetSelection(airbase);
		}
	}
}
