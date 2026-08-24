using System;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct MissionTag_V5_OLD
	{
		public string Tag;

		public Color Color;

		public int SortOrder;
	}
}
