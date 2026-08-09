using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct SavedMissionObjectives_V5_OLD
	{
		public List<SavedObjective_V5_OLD> Objectives;

		public List<SavedOutcome_V5_OLD> Outcomes;
	}
}
