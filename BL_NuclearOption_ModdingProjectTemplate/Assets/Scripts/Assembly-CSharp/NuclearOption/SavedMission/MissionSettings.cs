using System;
using System.Collections.Generic;
using RoadPathfinding;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public class MissionSettings
	{
		public string description = "";

		public bool allowEventContent;

		public List<MissionTag> Tags = new List<MissionTag>();

		public PlayerMode playerMode;

		public bool allowRespawn;

		public int playerStartingRank;

		public float rankMultiplier = 1f;

		public float successfulSortieBonus = 0.25f;

		public float nuclearEscalationThreshold;

		public float strategicEscalationThreshold;

		public int minRankTacticalWarhead;

		public int minRankStrategicWarhead;

		public Override<PositionRotation> cameraStartPosition;

		public RoadNetwork missionRoads = new RoadNetwork();

		public RoadNetwork missionSeaLanes = new RoadNetwork();

		public int wrecksMaxNumber;

		public float wrecksDecayTime;

		public void SetMinimumStartingRank(int rank)
		{
			playerStartingRank = Mathf.Max(playerStartingRank, rank);
		}
	}
}
