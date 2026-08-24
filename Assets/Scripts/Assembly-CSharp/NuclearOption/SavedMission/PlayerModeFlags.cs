using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	[Flags]
	public enum PlayerModeFlags
	{
		SinglePlayer = 1,
		Multiplayer = 2,
		SingleAndMultiplayer = 3
	}
}
