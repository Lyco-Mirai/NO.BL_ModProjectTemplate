using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class RevealUnitSavedOutcome : SavedOutcome
	{
		public List<string> UnitsToReveal = new List<string>();

		public override OutcomeType OutcomeTypeEnum => OutcomeType.RevealUnit;
	}
}
