using System;
using System.Collections.Generic;
using System.Text;
using Steamworks;
using UnityEngine;

namespace NuclearOption.Networking.Lobbies
{
	public class DedicatedServerKeyValues
	{
		public const string SHORT_HOST_VERSION_KEY = "v";

		public const string SHORT_MODDED_KEY = "m";

		public const string SHORT_MISSION_PVP_TYPE = "t";

		public const string SHORT_HAS_PASSWORD = "p";

		public const string SHORT_START_TIME_KEY = "s";

		public const string SHORT_SHORT_PASSWORD = "p";

		public const string SHORT_MISSION_NAME_KEY = "mi";

		public const string SHORT_MISSION_DESCRIPTION_KEY = "d";

		public const int SHORT_MISSION_DESCRIPTION_KEY_CHUNKS = 4;

		public const string SHORT_MAP_NAME_KEY = "ma";

		public const string SHORT_MISSION_WORKSHOP_ID_KEY = "id";

		private readonly StringBuilder builder = new StringBuilder();

		private readonly Dictionary<string, string> keyValues = new Dictionary<string, string>();

		private readonly Dictionary<string, string> tags = new Dictionary<string, string>();

		private bool hasPassword;

		public void SetKeyValue(string key, string value)
		{
			switch (key)
			{
			default:
				throw new ArgumentException("Invalid server key " + key);
			case "version":
				tags["v"] = value;
				break;
			case "modded_server":
				tags["m"] = value;
				break;
			case "mission_pvp_type":
				tags["t"] = value;
				break;
			case "short_password":
				if (!string.IsNullOrEmpty(value))
				{
					hasPassword = true;
					keyValues["p"] = value;
					tags["p"] = "1";
				}
				else
				{
					hasPassword = false;
					tags["p"] = "0";
				}
				break;
			case "start_time":
				keyValues["s"] = value;
				break;
			case "mission_name":
				keyValues["mi"] = value;
				break;
			case "mission_description":
			{
				for (int i = 0; i < 4; i++)
				{
					keyValues.Remove(string.Format("{0}{1}", "d", i));
				}
				for (int j = 0; j < 4; j++)
				{
				}
				List<string> list = StringHelper.SplitStringByByteCount(value, includeEmptyLast: false, 125, 4);
				for (int k = 0; k < list.Count; k++)
				{
					keyValues[string.Format("{0}{1}", "d", k)] = list[k];
				}
				break;
			}
			case "map_name":
				keyValues["ma"] = value;
				break;
			case "mission_workshop_id":
				keyValues["id"] = value;
				break;
			}
		}

		public void ApplyValuesToSteam()
		{
			SteamGameServer.SetPasswordProtected(hasPassword);
			ApplyTags();
			ApplyMapName();
			ApplyKeyValue();
		}

		private void ApplyTags()
		{
			builder.Clear();
			foreach (KeyValuePair<string, string> tag in tags)
			{
				builder.Append(tag.Key);
				builder.Append("=");
				builder.Append(tag.Value);
				builder.Append(",");
			}
			string text = builder.ToString();
			int byteLength = StringHelper.GetByteLength(text);
			if (byteLength > 128)
			{
				Debug.LogError($"tags are {byteLength} bytes, but the limit is {128}");
			}
			ColorLog<DedicatedServerKeyValues>.Info("Settings steam tags to '" + text + "'");
			SteamGameServer.SetGameTags(text);
		}

		private void ApplyMapName()
		{
			keyValues.TryGetValue("ma", out var value);
			keyValues.TryGetValue("mi", out var value2);
			string mapName = (string.IsNullOrEmpty(value) ? (value2 ?? "") : ((!string.IsNullOrEmpty(value2)) ? (value + " | " + value2) : (value2 ?? "")));
			SteamGameServer.SetMapName(mapName);
		}

		private void ApplyKeyValue()
		{
			int num = 0;
			foreach (KeyValuePair<string, string> keyValue in keyValues)
			{
				int byteLength = StringHelper.GetByteLength(keyValue.Key);
				int byteLength2 = StringHelper.GetByteLength(keyValue.Value);
				int num2 = byteLength + byteLength2;
				if (num2 > 127)
				{
					Debug.LogError($"pair ({num2} bytes) is over 127 bytes and will be truncated, {keyValue.Key} = {keyValue.Value}");
					num2 = 127;
				}
				num += num2;
				if (num > 1300)
				{
					Debug.LogError("total SetKeyValue are over 1300 no more values will be set");
				}
				ColorLog<DedicatedServerKeyValues>.Info($"Settings pair ({num2}/{num} bytes) {keyValue.Key}={keyValue.Value}");
				SteamGameServer.SetKeyValue(keyValue.Key, keyValue.Value);
			}
		}

		public static void ParseTags(string rawTags, Dictionary<string, string> outRules)
		{
			string[] array = rawTags.Split(",");
			foreach (string text in array)
			{
				if (!string.IsNullOrEmpty(text))
				{
					string[] array2 = text.Split("=");
					if (array2.Length != 2)
					{
						Debug.LogError("Failed to parse tag '" + text + "', full tags '" + rawTags + "'");
						break;
					}
					if (TryParseTagKey(array2[0], out var fullKey))
					{
						outRules[fullKey] = array2[1];
					}
					else
					{
						ColorLog<DedicatedServerKeyValues>.InfoWarn("Unrecognized server tag key '" + array2[0] + "'");
					}
				}
			}
		}

		private static bool TryParseTagKey(string rawKey, out string fullKey)
		{
			switch (rawKey)
			{
			case "v":
				fullKey = "version";
				return true;
			case "m":
				fullKey = "modded_server";
				return true;
			case "t":
				fullKey = "mission_pvp_type";
				return true;
			case "p":
				fullKey = "has_password";
				return true;
			default:
				fullKey = null;
				return false;
			}
		}

		public static void ParseKeyValue(string shortKey, string value, Dictionary<string, string> outRules)
		{
			if (TryParseKeyValueKey(shortKey, out var fullKey))
			{
				outRules[fullKey] = value;
				return;
			}
			ColorLog<DedicatedServerKeyValues>.InfoWarn("Unrecognized server rule key '" + shortKey + "' with value '" + value + "'");
		}

		private static bool TryParseKeyValueKey(string rawKey, out string fullKey)
		{
			switch (rawKey)
			{
			case "p":
				fullKey = "short_password";
				return true;
			case "s":
				fullKey = "start_time";
				return true;
			case "mi":
				fullKey = "mission_name";
				return true;
			case "ma":
				fullKey = "map_name";
				return true;
			case "id":
				fullKey = "mission_workshop_id";
				return true;
			default:
			{
				for (int i = 0; i < 4; i++)
				{
					if (rawKey == string.Format("{0}{1}", "d", i))
					{
						fullKey = rawKey;
						return true;
					}
				}
				fullKey = null;
				return false;
			}
			}
		}

		public static string ParseDescription(Dictionary<string, string> rules)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string value;
			for (int i = 0; i < 4 && rules.TryGetValue(string.Format("{0}{1}", "d", i), out value); i++)
			{
				stringBuilder.Append(value);
			}
			return stringBuilder.ToString();
		}
	}
}
