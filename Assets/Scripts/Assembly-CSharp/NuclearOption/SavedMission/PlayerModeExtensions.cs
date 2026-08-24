namespace NuclearOption.SavedMission
{
	public static class PlayerModeExtensions
	{
		public static PlayerModeFlags ToFlags(this PlayerMode mode)
		{
			return mode switch
			{
				PlayerMode.Singleplayer => PlayerModeFlags.SinglePlayer, 
				PlayerMode.Multiplayer => PlayerModeFlags.Multiplayer, 
				PlayerMode.SingleAndMultiplayer => PlayerModeFlags.SingleAndMultiplayer, 
				_ => (PlayerModeFlags)0, 
			};
		}

		public static PlayerMode ToMode(this PlayerModeFlags flags)
		{
			return flags switch
			{
				PlayerModeFlags.SinglePlayer => PlayerMode.Singleplayer, 
				PlayerModeFlags.Multiplayer => PlayerMode.Multiplayer, 
				PlayerModeFlags.SingleAndMultiplayer => PlayerMode.SingleAndMultiplayer, 
				_ => PlayerMode.SingleAndMultiplayer, 
			};
		}
	}
}
