namespace NuclearOption.SavedMission.ConvertVersions
{
	public static class MissionVersionUpgrade
	{
		public static int LatestVersion = 6;

		public static Mission FromV5(string json, string missionName = null)
		{
			return MissionVersionUpgradeInner.FromV5Inner(json, missionName);
		}

		public static void Upgrade(Mission mission)
		{
		}
	}
}
