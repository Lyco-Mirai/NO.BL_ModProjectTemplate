using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct SavedObjective_V5_OLD
	{
		public string UniqueName;

		public string Faction;

		public string DisplayName;

		public bool Hidden;

		public ObjectiveType_V5_OLD Type;

		public string TypeName;

		public List<ObjectiveData_V5_OLD> Data;

		public List<string> Outcomes;
	}
}
