using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class Restrictions
	{
		public List<string> aircraft = new List<string>();

		public List<string> weapons = new List<string>();
	}
}
