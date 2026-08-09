using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class VehicleWaypoint_V5_OLD
	{
		public GlobalPosition position;

		public string objective;
	}
}
