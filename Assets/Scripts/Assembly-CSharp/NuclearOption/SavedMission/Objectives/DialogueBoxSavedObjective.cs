using System;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class DialogueBoxSavedObjective : SavedObjective
	{
		public string title = "";

		public string body = "";

		public string button = "ok";

		public bool factionOnly;

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.DialogueBox;

		public DialogueBoxSavedObjective()
		{
		}

		public DialogueBoxSavedObjective(string name)
			: base(name)
		{
		}
	}
}
