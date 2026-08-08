using System;

namespace NuclearOption.DedicatedServer
{
	[Serializable]
	public enum RotationType
	{
		Sequence = 0,
		PureRandom = 1,
		RandomQueue = 2
	}
}
