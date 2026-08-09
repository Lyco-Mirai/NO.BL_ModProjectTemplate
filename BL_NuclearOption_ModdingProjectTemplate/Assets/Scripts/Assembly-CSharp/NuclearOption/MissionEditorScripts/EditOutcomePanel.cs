using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class EditOutcomePanel : MonoBehaviour, ISidePanel
	{
		[Header("Main")]
		[SerializeField]
		private TextMeshProUGUI typeText;

		[SerializeField]
		private TMP_InputField uniqueNameText;

		[Header("Controls")]
		[SerializeField]
		private Button deleteButton;

		[SerializeField]
		private TextMeshProUGUI deleteText;

		[SerializeField]
		private Button closeButton;

		[Header("Data")]
		[SerializeField]
		private RectTransform dataContent;

		[SerializeField]
		private GameObject dataPlaceholder;

		[SerializeField]
		private UIPrefabs prefabs;

		[SerializeField]
		private float dataWidth;

		private Outcome outcome;

		private Objective parentObjective;

		private PanelDrawOptions options;

		public SidePanel Panel { get; set; }

		private void Awake()
		{
			uniqueNameText.onEndEdit.AddListener(OnEdit);
			deleteButton.onClick.AddListener(DeleteClicked);
			closeButton.onClick.AddListener(CloseClicked);
		}

		private void OnEdit(string _)
		{
			OnEdit();
		}

		private void OnEdit()
		{
			string text = uniqueNameText.text;
			MissionManager.Objectives.MakeUnique(ref text, outcome);
			outcome.Rename(text);
			uniqueNameText.SetTextWithoutNotify(outcome.SavedOutcome.UniqueName);
			Panel.Parent.Refresh();
		}

		public void SetOutcome(Outcome outcome, Objective parentObjective = null, PanelDrawOptions options = default(PanelDrawOptions))
		{
			this.outcome = outcome;
			this.parentObjective = parentObjective;
			this.options = options;
			SetFields(outcome);
			MissionObjectives objectives = MissionManager.Objectives;
			deleteText.text = ((parentObjective != null) ? ("Remove from " + parentObjective.SavedObjective.UniqueName) : $"Delete (References:{SaveHelper.CountUsedBy(objectives, outcome)})");
			deleteButton.gameObject.SetActive(options.Context == PanelDrawContext.Standard);
			DrawData();
		}

		private void DrawData()
		{
			DataDrawer dataDrawer = new DataDrawer(dataContent, prefabs);
			dataDrawer.Width = dataWidth;
			dataDrawer.Options = options;
			outcome.DrawData(dataDrawer);
			dataPlaceholder.SetActive(!dataDrawer.Success);
		}

		private void SetFields(Outcome outcome)
		{
			SavedOutcome savedOutcome = outcome.SavedOutcome;
			typeText.text = outcome.SavedOutcome.OutcomeTypeEnum.ToNicifyString();
			uniqueNameText.SetTextWithoutNotify(savedOutcome.UniqueName);
		}

		private void CloseClicked()
		{
			Panel.Destroy();
		}

		private void DeleteClicked()
		{
			if (parentObjective != null)
			{
				int num = parentObjective.Outcomes.IndexOf(outcome);
				if (num != -1)
				{
					parentObjective.Outcomes.RemoveAt(num);
				}
			}
			else
			{
				MissionManager.Objectives.RemoveOutcome(outcome);
			}
			Panel.Destroy();
			Panel.Parent?.Refresh();
		}

		void ISidePanel.PanelRefresh()
		{
		}
	}
}
