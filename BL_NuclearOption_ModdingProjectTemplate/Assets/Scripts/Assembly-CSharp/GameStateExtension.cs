public static class GameStateExtension
{
	public static bool IsSingleOrMultiplayer(this GameState state)
	{
		if (state != GameState.SinglePlayer)
		{
			return state == GameState.Multiplayer;
		}
		return true;
	}
}
