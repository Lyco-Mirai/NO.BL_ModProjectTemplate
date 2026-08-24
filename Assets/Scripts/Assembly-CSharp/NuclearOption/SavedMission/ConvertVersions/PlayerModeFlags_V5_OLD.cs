using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Flags]
	[Obsolete("V5", true)]
	public enum PlayerModeFlags_V5_OLD
	{
		SinglePlayer = 1,
		Multiplayer = 2,
		SingleAndMultiplayer = 3
	}
}
