using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class NewOutcomePanel : NewPanelBase<OutcomeType>
	{
		[SerializeField]
		private EditOutcomePanel editOutcomePanelPrefab;

		private Objective parentObjective;

		public void Setup(Objective parentObjective)
		{
			this.parentObjective = parentObjective;
		}

		protected override void CreateItem(OutcomeType type)
		{
			string text = nameField.text;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = type.ToString();
			}
			Outcome outcome = SavedOutcome.Create(SavedOutcome.CreateSaved(type, text));
			MissionManager.Objectives.AddNewOutcome(outcome, parentObjective);
			base.Panel.Parent?.Refresh();
			base.Panel.Create(editOutcomePanelPrefab).SetOutcome(outcome, parentObjective);
		}
	}
}
