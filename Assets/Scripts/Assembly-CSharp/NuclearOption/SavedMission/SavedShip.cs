using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedShip : SavedUnit
	{
		public bool holdPosition;

		public float skill = 0.7f;

		public List<VehicleWaypoint> waypoints = new List<VehicleWaypoint>();

		public SavedShip()
		{
		}

		public SavedShip(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
