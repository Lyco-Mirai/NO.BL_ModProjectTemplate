using System;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public abstract class SavedUnit_V5_OLD
	{
		public string type;

		public string faction;

		public string UniqueName;

		public GlobalPosition globalPosition;

		public Quaternion rotation;

		public Override_V5_OLD<float> CaptureStrength;

		public Override_V5_OLD<float> CaptureDefense;

		[Obsolete("Use UniqueName instead", true)]
		public string unitCustomID;

		[Obsolete("Use SpawnUnit Outcome instead", true)]
		public string spawnTiming;
	}
}
