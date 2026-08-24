using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class SavedVehicle : SavedUnit
	{
		public bool holdPosition;

		public float skill = 0.7f;

		public List<VehicleWaypoint> waypoints = new List<VehicleWaypoint>();

		public SavedVehicle()
		{
		}

		public SavedVehicle(string uniqueName)
			: base(uniqueName)
		{
		}
	}
}
