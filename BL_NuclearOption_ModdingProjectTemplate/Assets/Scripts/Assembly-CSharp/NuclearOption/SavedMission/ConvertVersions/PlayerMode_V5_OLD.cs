using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public enum PlayerMode_V5_OLD
	{
		SingleAndMultiplayer = 0,
		Singleplayer = 1,
		Multiplayer = 2
	}
}
