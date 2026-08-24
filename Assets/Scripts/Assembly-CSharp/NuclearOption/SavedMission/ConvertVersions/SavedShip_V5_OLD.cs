using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedShip_V5_OLD : SavedUnit_V5_OLD
	{
		public bool holdPosition;

		public float skill = 0.7f;

		public List<VehicleWaypoint_V5_OLD> waypoints = new List<VehicleWaypoint_V5_OLD>();
	}
}
