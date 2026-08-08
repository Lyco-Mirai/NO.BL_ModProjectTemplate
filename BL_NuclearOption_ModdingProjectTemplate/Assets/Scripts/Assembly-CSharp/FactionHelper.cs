using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

public static class FactionHelper
{
	public const string NO_FACTION = "None";

	public const string NEUTRAL_FACTION = "Neutral";

	public const string Boscali = "Boscali";

	public const string Primeva = "Primeva";

	public static bool EmptyOrNoFaction(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			return name == "None";
		}
		return true;
	}

	public static bool EmptyOrNoFactionOrNeutral(string name)
	{
		if (!string.IsNullOrEmpty(name) && !(name == "None"))
		{
			return name == "Neutral";
		}
		return true;
	}

	public static bool BelongsToFaction(this IHasFaction hasFaction, string faction)
	{
		string factionName = hasFaction.FactionName;
		if (faction == factionName)
		{
			return true;
		}
		if (EmptyOrNoFaction(faction))
		{
			return EmptyOrNoFaction(factionName);
		}
		return false;
	}

	public static FactionHQ FindHQ(this IHasFaction hasFaction)
	{
		string factionName = hasFaction.FactionName;
		if (EmptyOrNoFactionOrNeutral(factionName))
		{
			return null;
		}
		FactionHQ factionHQ = FactionRegistry.HqFromName(factionName);
		if (factionHQ == null)
		{
			Debug.LogError("Faction HQ for '" + factionName + "' was not found in FactionRegistry");
		}
		return factionHQ;
	}

	public static Color GetColorOrGray(this FactionHQ hq)
	{
		if (!(hq != null))
		{
			return Color.gray;
		}
		return hq.faction.color;
	}

	public static List<string> GetFactionsAndNeutral()
	{
		List<MissionFaction> factions = MissionManager.CurrentMission.factions;
		List<string> list = new List<string>(factions.Count + 1) { "Neutral" };
		foreach (MissionFaction item in factions)
		{
			list.Add(item.factionName);
		}
		return list;
	}

	public static string ToUIString(FactionHQ hq)
	{
		if (hq == null)
		{
			return "None";
		}
		Faction faction = hq.faction;
		if (faction == null)
		{
			return "None";
		}
		return faction.factionName.AddColor(faction.color);
	}

	public static string ToUIString(string factionName)
	{
		if (EmptyOrNoFaction(factionName))
		{
			return "None";
		}
		if (factionName == "Neutral")
		{
			return "Neutral";
		}
		Faction faction = FactionRegistry.FactionFromName(factionName);
		if (faction == null)
		{
			Debug.LogError("Faction '" + factionName + "' was not found in FactionRegistry");
			return factionName;
		}
		return faction.factionName.AddColor(faction.color);
	}
}
