using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public class Road_V5_OLD
	{
		public Bounds bounds;

		public bool bridge;

		public List<GlobalPosition> points = new List<GlobalPosition>();

		public float length;
	}
}
