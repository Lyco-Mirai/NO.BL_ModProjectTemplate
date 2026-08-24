using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class ShowMessageSavedOutcome : SavedOutcome
	{
		public string Message = "";

		public bool PlaySound;

		public bool ObjectiveFactionOnly;

		public override OutcomeType OutcomeTypeEnum => OutcomeType.ShowMessage;
	}
}
