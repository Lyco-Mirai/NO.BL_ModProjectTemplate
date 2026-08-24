using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class NewObjectivePanel : NewPanelBase<ObjectiveType>
	{
		[SerializeField]
		private EditObjectivePanel editObjectivePanelPrefab;

		protected override void CreateItem(ObjectiveType type)
		{
			string text = nameField.text;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = type.ToString();
			}
			Objective objective = SavedObjective.Create(SavedObjective.CreateSavedObjective(type, text));
			MissionManager.Objectives.AddNewObjective(objective);
			base.Panel.Parent?.Refresh();
			base.Panel.Create(editObjectivePanelPrefab).SetObjective(objective);
		}
	}
}
