using System.Collections.Generic;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class EditObjectivePanel : MonoBehaviour, ISidePanel
	{
		[Header("Main")]
		[SerializeField]
		private TextMeshProUGUI typeText;

		[SerializeField]
		private TMP_InputField uniqueNameText;

		[SerializeField]
		private TMP_InputField displayNameText;

		[SerializeField]
		private TextMeshProUGUI noFactionError;

		[SerializeField]
		private FactionDataField factionField;

		[SerializeField]
		private Toggle hiddenToggle;

		[Header("Controls")]
		[SerializeField]
		private Button deleteButton;

		[SerializeField]
		private Button closeButton;

		[Header("Data")]
		[SerializeField]
		private UIPrefabs prefabs;

		[SerializeField]
		private RectTransform dataContent;

		[SerializeField]
		private GameObject dataPlaceholder;

		[SerializeField]
		private float dataWidth;

		[Header("Outcomes")]
		[SerializeField]
		private RectTransform outcomeParent;

		[SerializeField]
		private int outcomeListHeight;

		[Header("Outcomes Panel")]
		[SerializeField]
		private NewOutcomePanel newOutcomePanelPrefab;

		[SerializeField]
		private EditOutcomePanel editOutcomePanelPrefab;

		private ReferenceList outcomeList;

		private DataDrawer dataDrawer;

		private Objective objective;

		private PanelDrawOptions options;

		private bool isMissionStart;

		private IObjectiveEditorUpdate objectiveEditorUpdates;

		public SidePanel Panel { get; set; }

		private void Awake()
		{
			string text = GoogleIconFont.FontString("\ue002");
			noFactionError.text = text + " Objective needs a faction";
			noFactionError.gameObject.SetActive(value: false);
			uniqueNameText.onEndEdit.AddListener(OnEdit);
			displayNameText.onEndEdit.AddListener(OnEdit);
			hiddenToggle.onValueChanged.AddListener(OnEdit);
			deleteButton.onClick.AddListener(DeleteClicked);
			closeButton.onClick.AddListener(CloseClicked);
		}

		private void OnEnable()
		{
			if (objective != null)
			{
				SetObjective(objective);
			}
		}

		private void OnDisable()
		{
			if (objective != null)
			{
				objective.FactionWrapper.UnregisterOnChange(this);
			}
			objectiveEditorUpdates?.Destroy();
			objectiveEditorUpdates = null;
			dataDrawer?.Cleanup();
		}

		private void OnEdit(string _)
		{
			OnEdit();
		}

		private void OnEdit(int _)
		{
			OnEdit();
		}

		private void OnEdit(bool _)
		{
			OnEdit();
		}

		private void OnEdit()
		{
			string text = uniqueNameText.text;
			MissionManager.Objectives.MakeUnique(ref text, objective);
			objective.Rename(text);
			uniqueNameText.SetTextWithoutNotify(objective.SavedObjective.UniqueName);
			objective.SavedObjective.DisplayName = displayNameText.text;
			objective.SavedObjective.Hidden = hiddenToggle.isOn;
			AfterEdit();
		}

		private void AfterEdit()
		{
			Panel.Parent?.Refresh();
			CheckFactionError();
			dataDrawer?.InvokeAfterEdit();
		}

		private FactionHQ GetFactionHq(string name)
		{
			if (name == "None")
			{
				return null;
			}
			foreach (MissionFaction faction in MissionManager.CurrentMission.factions)
			{
				if (faction.factionName == name)
				{
					return faction.FactionHQ;
				}
			}
			throw new KeyNotFoundException("Could not find a faction with name:'" + name + "'");
		}

		public void SetObjective(Objective objective, PanelDrawOptions options = default(PanelDrawOptions))
		{
			objectiveEditorUpdates?.Destroy();
			objectiveEditorUpdates = null;
			if (this.objective != null)
			{
				this.objective.FactionWrapper.UnregisterOnChange(this);
			}
			this.objective = objective;
			this.options = options;
			SavedObjective savedObjective = objective.SavedObjective;
			isMissionStart = savedObjective.UniqueName == MissionObjectivesFactory.MissionStartName;
			objective.FactionWrapper.RegisterOnChange((object)this, (ValueWrapper<string>.OnChangeDelegate)delegate
			{
				CheckFactionError();
				AfterEdit();
			});
			factionField.Setup(objective.FactionWrapper);
			string factionLabelOverride = objective.FactionLabelOverride;
			if (!string.IsNullOrEmpty(factionLabelOverride))
			{
				factionField.SetNoFactionString(factionLabelOverride);
			}
			factionField.Interactable = !isMissionStart;
			CheckFactionError();
			typeText.text = savedObjective.ObjectiveTypeEnum.ToNicifyString();
			uniqueNameText.SetTextWithoutNotify(savedObjective.UniqueName);
			uniqueNameText.interactable = !isMissionStart;
			displayNameText.SetTextWithoutNotify(savedObjective.DisplayName);
			hiddenToggle.SetIsOnWithoutNotify(savedObjective.Hidden);
			CheckDestroyAllowed(objective);
			DrawData();
			if (outcomeList == null)
			{
				outcomeList = Object.Instantiate(prefabs.ReferenceListPrefab, outcomeParent);
			}
			outcomeList.TitleText.text = "Outcomes";
			outcomeList.SetHeight(outcomeListHeight);
			if (options.Context == PanelDrawContext.GraphEditor)
			{
				outcomeList.Setup(null, null, delegate(int index)
				{
					options.OnRequestSelectAndFocus?.Invoke(objective.Outcomes[index]);
				}, null);
			}
			else
			{
				outcomeList.Setup(OpenNewOutcomePanel, null, EditOutcome, RemoveOutcome);
			}
			ReferenceList.ButtonsEvents addButtonEvents = ((options.Context == PanelDrawContext.GraphEditor) ? ReferenceList.ButtonsEvents.DontAdd : ReferenceList.ButtonsEvents.OverrideNullOnly);
			outcomeList.SetupList(objective.Outcomes, (Outcome o) => o.ToUIString(oneLine: true), (Outcome o) => o.ToUIString(), () => MissionManager.Objectives.AllOutcomes, addButtonEvents);
			objectiveEditorUpdates = objective.CreateEditorUpdate(GetComponentInParent<Canvas>(), prefabs);
		}

		private void CheckDestroyAllowed(ISaveableReference objective)
		{
			deleteButton.gameObject.SetActive(options.Context == PanelDrawContext.Standard && objective.CanBeReference);
		}

		private void DrawData()
		{
			if (dataDrawer == null)
			{
				dataDrawer = new DataDrawer(dataContent, prefabs);
			}
			else
			{
				dataDrawer.Reset();
			}
			dataDrawer.TrackedValueChanged += DataDrawer_TrackedValueChanged;
			dataDrawer.Width = dataWidth;
			dataDrawer.Options = options;
			objective.DrawData(dataDrawer);
			dataPlaceholder.SetActive(!dataDrawer.Success);
		}

		private void DataDrawer_TrackedValueChanged()
		{
			CheckFactionError();
		}

		private void CheckFactionError()
		{
			bool flag = objective.NeedsFaction && FactionHelper.EmptyOrNoFaction(objective.SavedObjective.Faction);
			if (flag != noFactionError.gameObject.activeSelf)
			{
				noFactionError.gameObject.SetActive(flag);
				FixLayout.ForceRebuildAtEndOfFrame(base.transform.AsRectTransform());
			}
		}

		private void CloseClicked()
		{
			Panel.Destroy();
		}

		private void DeleteClicked()
		{
			MissionObjectives objectives = MissionManager.Objectives;
			int index = objectives.AllObjectives.IndexOf(objective);
			objectives.RemoveObjectiveAt(index);
			Panel.Destroy();
			Panel.Parent?.Refresh();
		}

		private void OpenNewOutcomePanel()
		{
			Panel.Child.Create(newOutcomePanelPrefab).Setup(objective);
		}

		private void EditOutcome(int index)
		{
			EditOutcome(objective.Outcomes[index]);
		}

		private void EditOutcome(Outcome outcome)
		{
			Panel.Child.Create(editOutcomePanelPrefab).SetOutcome(outcome, objective);
		}

		public void RemoveOutcome(int index)
		{
			objective.Outcomes.RemoveAt(index);
			outcomeList.RefreshList();
		}

		void ISidePanel.PanelRefresh()
		{
			outcomeList.RefreshList();
		}

		private void Update()
		{
			objectiveEditorUpdates?.Update();
		}
	}
}
