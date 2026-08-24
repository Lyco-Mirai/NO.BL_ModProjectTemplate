using System;
using System.Collections.Generic;
using Mirage.Serialization;
using NuclearOption.SceneLoading;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public struct MissionQuickLoad
	{
		public int JsonVersion;

		[WeaverIgnore]
		[Obsolete("WorkshopId has been moved to workshop.json", true)]
		public ulong WorkshopId;

		public MapKey MapKey;

		public MissionSettings missionSettings;

		public MissionEnvironment environment;

		public List<QuickLoadMissionFaction> factions;

		[NonSerialized]
		public string Name;

		[NonSerialized]
		public MissionKey? LoadKey;

		public void AfterLoad(MissionKey key)
		{
			LoadKey = key;
			Name = key.Name;
		}

		public static MissionQuickLoad FromMission(Mission mission)
		{
			MissionQuickLoad result = new MissionQuickLoad
			{
				JsonVersion = mission.JsonVersion,
				MapKey = mission.MapKey,
				missionSettings = mission.missionSettings,
				environment = mission.environment,
				Name = mission.Name,
				LoadKey = mission.LoadKey,
				factions = new List<QuickLoadMissionFaction>()
			};
			if (mission.factions != null)
			{
				foreach (MissionFaction faction in mission.factions)
				{
					result.factions.Add(new QuickLoadMissionFaction
					{
						factionName = faction.factionName,
						preventJoin = faction.preventJoin
					});
				}
			}
			return result;
		}
	}
}
