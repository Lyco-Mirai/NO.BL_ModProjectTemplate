using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class MissionSettings_V5_OLD
	{
		public string description;

		public bool allowEventContent;

		public List<MissionTag_V5_OLD> Tags = new List<MissionTag_V5_OLD>();

		public PlayerMode_V5_OLD playerMode;

		public bool allowRespawn;

		public int playerStartingRank;

		public float rankMultiplier = 1f;

		public float successfulSortieBonus = 0.25f;

		public float nuclearEscalationThreshold;

		public float strategicEscalationThreshold;

		public int minRankTacticalWarhead;

		public int minRankStrategicWarhead;

		public Override_V5_OLD<PositionRotation_V5_OLD> cameraStartPosition;

		public RoadNetwork_V5_OLD missionRoads = new RoadNetwork_V5_OLD();

		public RoadNetwork_V5_OLD missionSeaLanes = new RoadNetwork_V5_OLD();

		public int wrecksMaxNumber;

		public float wrecksDecayTime;
	}
}
