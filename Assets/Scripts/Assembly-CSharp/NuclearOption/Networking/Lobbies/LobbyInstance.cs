using System;
using System.Globalization;
using Steamworks;

namespace NuclearOption.Networking.Lobbies
{
	public abstract class LobbyInstance
	{
		public const string TAG_YES = "1";

		public const string TAG_NO = "0";

		public const string GAME_ENDED_NAME = "Game Ended";

		public const string HOST_ADDRESS_KEY = "HostAddress";

		public const string HOST_LOCATION = "HostPing";

		public const string HOST_VERSION_KEY = "version";

		public const string MODDED_KEY = "modded_server";

		public const string LOBBY_NAME = "name";

		public const string UDP_ADDRESS = "UDP_Address";

		public const string UDP_PORT = "UDP_Port";

		public const string HAS_PASSWORD = "has_password";

		public const string SHORT_PASSWORD = "short_password";

		public const string MISSION_NAME_KEY = "mission_name";

		public const string MAP_NAME_KEY = "map_name";

		public const string MISSION_DESCRIPTION_KEY = "mission_description";

		public const string MISSION_PVP_TYPE = "mission_pvp_type";

		public const string START_TIME_KEY = "start_time";

		public const string START_TIME_NO_GAME = "no-game";

		public const string OPEN_MEMBER_SPOTS_KEY = "open_member_spots";

		public const string MAX_MEMBERS_KEY = "max_members";

		public const string MISSION_WORKSHOP_ID_KEY = "mission_workshop_id";

		public const int MAX_LOBBY_NAME_LENGTH = 128;

		public const int MAX_MISSION_NAME_LENGTH = 128;

		public const int MAX_MAP_NAME_LENGTH = 64;

		public const int MAX_MISSION_DESCRIPTION_LENGTH = 1000;

		public bool InList;

		public abstract CSteamID LobbyId { get; }

		public abstract string HostAddress { get; }

		public abstract string UdpAddress { get; }

		public abstract string UdpPort { get; }

		public abstract bool DedicatedServer { get; }

		public string HostVersion => GetData("version");

		public bool ModdedServer => GetData("modded_server") == "1";

		public DateTime? StartTime
		{
			get
			{
				string data = GetData("start_time");
				if (data == "no-game")
				{
					return null;
				}
				if (!DateTime.TryParse(data, null, DateTimeStyles.AdjustToUniversal, out var result))
				{
					return null;
				}
				return result;
			}
		}

		protected virtual string LobbyName => GetData("name");

		public string LobbyNameSanitized => LobbyName.ProfanityFilter().SanitizeRichText(128);

		public string MissionNameRaw => GetData("mission_name");

		public string MissionNameSanitized => GetData("mission_name").ProfanityFilter().SanitizeRichText(128);

		public string MapNameSanitized => GetData("map_name").ProfanityFilter().SanitizeRichText(64);

		public virtual string MissionDescriptionSanitized => GetData("mission_description").ProfanityFilter().SanitizeRichText(1000);

		public MissionPvpType MissionPvpType
		{
			get
			{
				if (!int.TryParse(GetData("mission_pvp_type"), out var result))
				{
					return MissionPvpType.All;
				}
				return (MissionPvpType)result;
			}
		}

		public PublishedFileId_t MissionWorkshopId
		{
			get
			{
				if (!ulong.TryParse(GetData("mission_workshop_id"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
				{
					return PublishedFileId_t.Invalid;
				}
				return new PublishedFileId_t(result);
			}
		}

		public static string BoolToTag(bool value)
		{
			if (!value)
			{
				return "0";
			}
			return "1";
		}

		public static bool TagToBool(string value)
		{
			return value == "1";
		}

		protected abstract string GetData(string key);

		public abstract int? CalculatePing();

		public abstract bool GetPlayerCounts(out int current, out int max);

		public abstract bool IsPasswordProtected(out string shortPassword);

		public static bool ValidName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			if (name == "Game Ended")
			{
				return false;
			}
			return true;
		}

		public static string CreateStartTime()
		{
			return DateTime.UtcNow.ToString("o");
		}

		public static string TimeSpanString(DateTime? startTime, bool includeSeconds)
		{
			TimeSpan? timespan = null;
			if (startTime.HasValue)
			{
				DateTime utcNow = DateTime.UtcNow;
				timespan = utcNow - startTime.Value;
			}
			return TimeSpanString(timespan, includeSeconds);
		}

		public static string TimeSpanString(TimeSpan? timespan, bool includeSeconds)
		{
			if (timespan.HasValue)
			{
				TimeSpan value = timespan.Value;
				if (includeSeconds)
				{
					return $"{(int)value.TotalHours}:{value.Minutes:D2}:{value.Seconds:D2}";
				}
				return $"{(int)value.TotalHours}:{value.Minutes:D2}";
			}
			return "";
		}
	}
}
