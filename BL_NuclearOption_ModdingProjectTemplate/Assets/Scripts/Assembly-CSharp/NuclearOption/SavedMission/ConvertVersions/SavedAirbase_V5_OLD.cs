using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class SavedAirbase_V5_OLD
	{
		public bool IsOverride;

		public string faction;

		public string UniqueName;

		public string DisplayName;

		public bool Disabled;

		public bool Capturable = true;

		public float CaptureDefense = 10f;

		public float CaptureRange = 1000f;

		public GlobalPosition Center;

		public GlobalPosition SelectionPosition;

		public string Tower;

		public List<GlobalPosition> VerticalLandingPoints = new List<GlobalPosition>();

		public List<GlobalPosition> ServicePoints = new List<GlobalPosition>();

		public RoadNetwork_V5_OLD roads = new RoadNetwork_V5_OLD();

		public List<SavedRunway_V5_OLD> runways = new List<SavedRunway_V5_OLD>();
	}
}
