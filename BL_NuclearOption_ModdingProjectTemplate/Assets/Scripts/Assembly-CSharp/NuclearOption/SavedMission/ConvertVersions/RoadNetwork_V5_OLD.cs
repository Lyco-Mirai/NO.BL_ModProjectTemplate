using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class RoadNetwork_V5_OLD
	{
		public List<Road_V5_OLD> roads = new List<Road_V5_OLD>();
	}
}
