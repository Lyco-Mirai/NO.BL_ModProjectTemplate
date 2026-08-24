using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Obsolete("V5", true)]
	public enum PlacementType_V5_OLD
	{
		NotSet = 0,
		BuiltIn = 1,
		Override = 2,
		Attached = 3,
		Custom = 4
	}
}
