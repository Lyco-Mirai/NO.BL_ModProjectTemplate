using System;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class ModifyAirbaseSavedOutcome : SavedOutcome
	{
		public string airbase = "";

		public Override<string> faction = new Override<string>(isOverride: false, "");

		public Override<bool> disabled;

		public Override<bool> capturable;

		public Override<float> captureDefense;

		public override OutcomeType OutcomeTypeEnum => OutcomeType.ModifyAirbase;
	}
}
