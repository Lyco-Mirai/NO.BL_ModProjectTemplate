using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct SavedOutcome_V5_OLD
	{
		public string UniqueName;

		public OutcomeType_V5_OLD Type;

		public string TypeName;

		public List<ObjectiveData_V5_OLD> Data;
	}
}
