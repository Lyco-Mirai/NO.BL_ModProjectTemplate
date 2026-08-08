using System;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public struct PositionRotation_V5_OLD
	{
		public GlobalPosition Position;

		public Quaternion Rotation;
	}
}
