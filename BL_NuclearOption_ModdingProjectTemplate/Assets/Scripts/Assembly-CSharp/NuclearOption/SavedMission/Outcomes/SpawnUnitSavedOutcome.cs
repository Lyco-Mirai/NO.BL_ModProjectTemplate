using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class SpawnUnitSavedOutcome : SavedOutcome
	{
		public List<string> UnitsToSpawn = new List<string>();

		public override OutcomeType OutcomeTypeEnum => OutcomeType.SpawnUnit;
	}
}
