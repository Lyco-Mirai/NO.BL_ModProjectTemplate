using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Obsolete("V5", true)]
	public enum CompleteOrder_V5_OLD
	{
		CompleteAny = 0,
		CompleteAll = 1,
		InOrder = 2,
		CompleteSome = 3
	}
}
