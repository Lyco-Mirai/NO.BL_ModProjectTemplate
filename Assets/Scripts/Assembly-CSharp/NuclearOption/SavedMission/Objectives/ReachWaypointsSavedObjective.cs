using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class ReachWaypointsSavedObjective : SavedObjective
	{
		public CompleteOrder completeOrder = CompleteOrder.InOrder;

		public float completeSomePercent = 0.5f;

		public bool completeOnEnterRange;

		public List<SavedWaypoint> waypoints = new List<SavedWaypoint>();

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.ReachWaypoints;

		public ReachWaypointsSavedObjective()
		{
		}

		public ReachWaypointsSavedObjective(string name)
			: base(name)
		{
		}
	}
}
