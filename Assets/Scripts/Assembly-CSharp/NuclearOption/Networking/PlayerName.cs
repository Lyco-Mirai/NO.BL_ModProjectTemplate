using Steamworks;

namespace NuclearOption.Networking
{
	public class PlayerName
	{
		public readonly string RawSteamName;

		public readonly string SanitizedName;

		public readonly string ClientReportedSteamName;

		private string _censoredName;

		private string _chatOrLeaderboardName;

		private string _otherName;

		public PlayerName(string rawSteamName, string sanitizedName, string clientReportedSteamName = null)
		{
			RawSteamName = rawSteamName;
			SanitizedName = sanitizedName;
			ClientReportedSteamName = clientReportedSteamName;
			RebuildCachedNames(0, null);
		}

		private string GetCensoredName()
		{
			if (_censoredName == null)
			{
				_censoredName = SanitizedName.ProfanityFilter();
			}
			return _censoredName;
		}

		public string GetDisplayName(PlayerNameContext context)
		{
			if (context == PlayerNameContext.ChatOrLeaderboard)
			{
				return _chatOrLeaderboardName;
			}
			return _otherName;
		}

		public void RebuildCachedNames(int playerIndex, string serverTag)
		{
			string baseName = (PlayerSettings.chatFilter ? GetCensoredName() : SanitizedName);
			bool flag = playerIndex > 0;
			if (flag)
			{
				flag = PlayerSettings.playerIndexDisplay switch
				{
					PlayerNameDisplayScope.InChatAndLeaderboard => true, 
					PlayerNameDisplayScope.Everywhere => true, 
					_ => false, 
				};
			}
			bool showIndex = flag;
			flag = !string.IsNullOrEmpty(serverTag);
			if (flag)
			{
				flag = PlayerSettings.serverTagDisplay switch
				{
					PlayerNameDisplayScope.InChatAndLeaderboard => true, 
					PlayerNameDisplayScope.Everywhere => true, 
					_ => false, 
				};
			}
			bool showTag = flag;
			_chatOrLeaderboardName = FormatName(baseName, playerIndex, serverTag, showIndex, showTag);
			bool showIndex2 = playerIndex > 0 && PlayerSettings.playerIndexDisplay == PlayerNameDisplayScope.Everywhere;
			bool showTag2 = !string.IsNullOrEmpty(serverTag) && PlayerSettings.serverTagDisplay == PlayerNameDisplayScope.Everywhere;
			_otherName = FormatName(baseName, playerIndex, serverTag, showIndex2, showTag2);
		}

		private static string FormatName(string baseName, int playerIndex, string serverTag, bool showIndex, bool showTag)
		{
			if (showIndex && showTag)
			{
				return $"[{playerIndex}][{serverTag}] {baseName}";
			}
			if (showIndex)
			{
				return $"[{playerIndex}] {baseName}";
			}
			if (showTag)
			{
				return "[" + serverTag + "] " + baseName;
			}
			return baseName;
		}

		public static PlayerName FallbackNoSteamId(string clientReportedName)
		{
			return new PlayerName("Player", "Player", clientReportedName)
			{
				_censoredName = "Player"
			};
		}

		public static PlayerName FallbackSteamID(CSteamID steamId)
		{
			string text = $"ID: {steamId}";
			return new PlayerName(text, text)
			{
				_censoredName = text
			};
		}

		public static void RebuildAllPlayerNameCaches()
		{
			foreach (Player value in UnitRegistry.playerLookup.Values)
			{
				value.RebuildNameCache();
			}
		}
	}
}
