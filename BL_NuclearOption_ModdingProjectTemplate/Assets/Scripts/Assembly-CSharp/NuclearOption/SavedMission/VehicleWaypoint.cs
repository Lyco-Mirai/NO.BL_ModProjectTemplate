using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class VehicleWaypoint : ICloneable, IEquatable<VehicleWaypoint>
	{
		public GlobalPosition position;

		public string objective = "";

		public object Clone()
		{
			return new VehicleWaypoint
			{
				position = position,
				objective = objective
			};
		}

		public bool Equals(VehicleWaypoint other)
		{
			if (position == other.position)
			{
				return objective == other.objective;
			}
			return false;
		}
	}
}
