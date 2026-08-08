using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FactionRegistry
{
	public static readonly List<Faction> factions = new List<Faction>();

	public static readonly Dictionary<string, Faction> factionLookup = new Dictionary<string, Faction>();

	public static readonly Dictionary<Faction, FactionHQ> HQLookup = new Dictionary<Faction, FactionHQ>();

	public static readonly Dictionary<string, Airbase> airbaseLookup = new Dictionary<string, Airbase>();

	public static void RegisterAirbase(string key, Airbase airbase)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentNullException("key", "airbase name was null");
		}
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			ColorLog<Airbase>.Info("RegisterAirbase " + key);
			airbaseLookup.Add(key, airbase);
		}
	}

	public static void UnregisterAirbase(Airbase airbase)
	{
		string uniqueName = airbase.SavedAirbase.UniqueName;
		if (string.IsNullOrEmpty(uniqueName))
		{
			Debug.LogError("Airbase name empty when UnregisterAirbase was called");
		}
		ColorLog<Airbase>.Info("UnregisterAirbase " + uniqueName);
		airbaseLookup.Remove(uniqueName);
	}

	public static void ChangeAirbaseName(Airbase airbase, string newName)
	{
		airbaseLookup.Remove(airbase.SavedAirbase.UniqueName);
		airbaseLookup.Add(newName, airbase);
	}

	public static void RegisterFaction(Faction faction, FactionHQ HQ)
	{
		if (!factions.Contains(faction))
		{
			factions.Add(faction);
		}
		if (!factionLookup.TryGetValue(faction.factionName, out var _))
		{
			factionLookup.Add(faction.factionName, faction);
		}
		if (!HQLookup.TryGetValue(faction, out var _))
		{
			HQLookup.Add(faction, HQ);
		}
	}

	public static Dictionary<Faction, FactionHQ>.ValueCollection GetAllHQs()
	{
		return HQLookup.Values;
	}

	public static FactionHQ[] GetSortedHQs()
	{
		FactionHQ[] array = GetAllHQs().ToArray();
		Array.Sort(array, (FactionHQ x, FactionHQ y) => x.faction.LeaderboardOrder.CompareTo(y.faction.LeaderboardOrder));
		return array;
	}

	public static void Clear()
	{
		factions.Clear();
		factionLookup.Clear();
		airbaseLookup.Clear();
		HQLookup.Clear();
	}

	public static FactionHQ HQFromFaction(Faction faction)
	{
		if (faction == null)
		{
			return null;
		}
		if (HQLookup.ContainsKey(faction))
		{
			return HQLookup[faction];
		}
		return null;
	}

	public static FactionHQ HqFromName(string factionName)
	{
		Faction faction = FactionFromName(factionName);
		if (faction != null)
		{
			return HQLookup[faction];
		}
		return null;
	}

	public static Faction FactionFromName(string factionName)
	{
		if (string.IsNullOrEmpty(factionName))
		{
			return null;
		}
		if (factionLookup.TryGetValue(factionName, out var value))
		{
			return value;
		}
		return null;
	}
}
