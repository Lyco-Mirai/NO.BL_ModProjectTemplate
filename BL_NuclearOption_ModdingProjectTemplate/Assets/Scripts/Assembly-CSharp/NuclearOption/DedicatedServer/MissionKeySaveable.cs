using System;
using System.Collections.Generic;
using NuclearOption.SavedMission;
using Steamworks;
using UnityEngine;

namespace NuclearOption.DedicatedServer
{
	[Serializable]
	public struct MissionKeySaveable : IEquatable<MissionKeySaveable>
	{
		public static Dictionary<string, MissionGroup> TypeNameToGroup = new Dictionary<string, MissionGroup>(StringComparer.OrdinalIgnoreCase)
		{
			{
				"",
				MissionGroup.All
			},
			{
				"Default",
				MissionGroup.Default
			},
			{
				"Tutorial",
				MissionGroup.Tutorial
			},
			{
				"BuiltIn",
				MissionGroup.BuiltIn
			},
			{
				"User",
				MissionGroup.User
			},
			{
				"Workshop",
				MissionGroup.Workshop
			}
		};

		public string Group;

		public string Name;

		public bool TryGetKey(out MissionKey missionKey)
		{
			missionKey = default(MissionKey);
			if (string.IsNullOrEmpty(Name))
			{
				Debug.LogError("Mission name should not be empty");
				return false;
			}
			if (!TypeNameToGroup.TryGetValue(Group ?? "", out var value))
			{
				Debug.LogError("Group '" + Group + "' is invalid");
				return false;
			}
			if (value == MissionGroup.Workshop)
			{
				if (ulong.TryParse(Name, out var result))
				{
					missionKey = new MissionKey(Name, Name, new PublishedFileId_t(result), value);
					missionKey = MissionGroup.Workshop.ResolveKey(missionKey);
					ColorLog<DedicatedServerConfig>.Info($"Resolved workshop key: {missionKey}");
					return true;
				}
				Debug.LogError("Failed to parse " + Name + " as ulong SteamId");
				return false;
			}
			missionKey = new MissionKey(Name, value);
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj is MissionKeySaveable other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (17 * 23 + ((Group != null) ? StringComparer.OrdinalIgnoreCase.GetHashCode(Group) : 0)) * 23 + ((Name != null) ? Name.GetHashCode() : 0);
		}

		public bool Equals(MissionKeySaveable other)
		{
			if (string.Equals(Group, other.Group, StringComparison.OrdinalIgnoreCase))
			{
				return Name == other.Name;
			}
			return false;
		}

		public override string ToString()
		{
			return "(" + Group + "," + Name + ")";
		}
	}
}
