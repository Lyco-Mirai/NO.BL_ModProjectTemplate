using System;

namespace NuclearOption.NetworkTransforms
{
	[Flags]
	public enum RejectMask : uint
	{
		Accepted = 0u,
		AccelerationForward = 1u,
		AccelerationPerpendicular = 2u,
		AccelerationBackwards = 4u,
		Position = 8u,
		SnapshotPosition = 0x10u,
		AveragePosition = 0x20u,
		UnderTerrain = 0x40u
	}
}
