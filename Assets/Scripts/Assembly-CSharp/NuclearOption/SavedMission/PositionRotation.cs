using System;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public struct PositionRotation : IEquatable<PositionRotation>
	{
		public GlobalPosition Position;

		public Quaternion Rotation;

		public readonly bool Equals(PositionRotation other)
		{
			if (other.Position.Equals(Position))
			{
				return other.Rotation.Equals(Rotation);
			}
			return false;
		}
	}
}
