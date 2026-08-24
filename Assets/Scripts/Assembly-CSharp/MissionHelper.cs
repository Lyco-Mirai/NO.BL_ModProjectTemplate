public static class MissionHelper
{
	public static bool CanRespawn => MissionManager.CurrentMission.missionSettings.allowRespawn;

	public static float RankMultiplier => MissionManager.CurrentMission.missionSettings.rankMultiplier;

	public static int StartingRank => MissionManager.CurrentMission.missionSettings.playerStartingRank;
}
