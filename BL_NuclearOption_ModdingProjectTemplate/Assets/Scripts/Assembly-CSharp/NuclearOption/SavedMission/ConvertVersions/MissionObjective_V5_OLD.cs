using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5: Use V2 instead", true)]
	public class MissionObjective_V5_OLD
	{
		public string objectiveName;

		public string message;

		public bool positionTrigger;

		public bool victoryObjective;

		public bool nonSequentialObjective;

		public float triggerRange;

		public Vector3 position;

		public List<string> targetUnits = new List<string>();
	}
}
