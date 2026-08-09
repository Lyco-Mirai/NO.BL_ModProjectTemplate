using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.Networking.Lobbies;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public struct MissionTag : IEquatable<MissionTag>, IEquatable<string>
	{
		public string Tag;

		public Color Color;

		public int SortOrder;

		private const float SATURATION = 0.35f;

		private const float BRIGHTNESS = 0.8f;

		public static readonly MissionTag SinglePlayer = new MissionTag("Single Player", 0.17f, 1);

		public static readonly MissionTag Multiplayer = new MissionTag("Multiplayer", 0.67f, 2);

		public static readonly MissionTag PVE = new MissionTag("PvE", 0.33f, 3);

		public static readonly MissionTag PVP = new MissionTag("PvP", 0f, 4);

		public static readonly MissionTag Dawn = new MissionTag("Dawn", 0.4f, 5);

		public static readonly MissionTag Day = new MissionTag("Day", 0.55f, 6);

		public static readonly MissionTag Dusk = new MissionTag("Dusk", 0.75f, 7);

		public static readonly MissionTag Night = new MissionTag("Night", 0.83f, 8);

		public static readonly MissionTag[] InternalTags = new MissionTag[8] { SinglePlayer, Multiplayer, PVE, PVP, Dawn, Day, Dusk, Night };

		public override int GetHashCode()
		{
			return Tag?.GetHashCode() ?? 0;
		}

		public override bool Equals(object obj)
		{
			if (obj is MissionTag)
			{
				return Equals((MissionTag)obj);
			}
			return false;
		}

		public bool Equals(MissionTag other)
		{
			return Tag == other.Tag;
		}

		public bool Equals(string other)
		{
			return Tag == other;
		}

		private MissionTag(string tag, float hue, int order)
			: this(tag, Color.HSVToRGB(hue, 0.35f, 0.8f), order)
		{
		}

		public MissionTag(string tag, Color color, int order)
		{
			Tag = tag;
			Color = color;
			SortOrder = order;
		}

		public static void AddAutoTags(Mission mission)
		{
			MissionQuickLoad mission2 = MissionQuickLoad.FromMission(mission);
			AddAutoTags(mission2);
			List<MissionTag> tags = mission.missionSettings.Tags;
			foreach (MissionTag tag in mission2.missionSettings.Tags)
			{
				AddSet(tags, tag);
			}
		}

		public static void AddAutoTags(MissionQuickLoad mission)
		{
			MissionSettings missionSettings = mission.missionSettings;
			if (missionSettings.Tags == null)
			{
				missionSettings.Tags = new List<MissionTag>();
			}
			List<MissionTag> tags = mission.missionSettings.Tags;
			PlayerMode playerMode = mission.missionSettings.playerMode;
			if (playerMode == PlayerMode.SingleAndMultiplayer || playerMode == PlayerMode.Singleplayer)
			{
				AddSet(tags, SinglePlayer);
			}
			if (playerMode == PlayerMode.SingleAndMultiplayer || playerMode == PlayerMode.Multiplayer)
			{
				AddSet(tags, Multiplayer);
			}
			float timeOfDay = mission.environment.timeOfDay;
			if (timeOfDay < 5f)
			{
				AddSet(tags, Night);
			}
			else if (timeOfDay < 8f)
			{
				AddSet(tags, Dawn);
			}
			else if (timeOfDay < 16.5f)
			{
				AddSet(tags, Day);
			}
			else if (timeOfDay < 18.5f)
			{
				AddSet(tags, Dusk);
			}
			else
			{
				AddSet(tags, Night);
			}
			if (GetPvpType(mission) == MissionPvpType.Pve)
			{
				AddSet(tags, PVE);
			}
			else
			{
				AddSet(tags, PVP);
			}
		}

		private static void AddSet(List<MissionTag> allTags, MissionTag newTag)
		{
			if (!allTags.Any((MissionTag x) => x.Tag == newTag.Tag))
			{
				allTags.Add(newTag);
			}
		}

		public static MissionPvpType GetPvpType(MissionQuickLoad mission)
		{
			foreach (QuickLoadMissionFaction faction in mission.factions)
			{
				if (faction.preventJoin)
				{
					return MissionPvpType.Pve;
				}
			}
			return MissionPvpType.Pvp;
		}

		public static MissionPvpType GetPvpType(Mission mission)
		{
			foreach (MissionFaction faction in mission.factions)
			{
				if (faction.preventJoin)
				{
					return MissionPvpType.Pve;
				}
			}
			return MissionPvpType.Pvp;
		}

		public static string GetPvpTypeLobbyString(Mission mission)
		{
			return GetPvpTypeLobbyString(GetPvpType(mission));
		}

		public static string GetPvpTypeLobbyString(MissionPvpType lobbyType)
		{
			int num = (int)lobbyType;
			return num.ToString();
		}
	}
}
