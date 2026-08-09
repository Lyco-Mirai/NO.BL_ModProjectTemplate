using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class Restrictions_V5_OLD
	{
		public List<string> aircraft = new List<string>();

		public List<string> weapons = new List<string>();
	}
}
