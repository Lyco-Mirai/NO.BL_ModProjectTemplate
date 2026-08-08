using System.Collections.Generic;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.Social;
using Steamworks;
using UnityEngine;

public static class UnitRegistry
{
	public static readonly Dictionary<PersistentID, PersistentUnit> persistentUnitLookup = new Dictionary<PersistentID, PersistentUnit>();

	public static readonly Dictionary<string, Unit> customIDLookup = new Dictionary<string, Unit>();

	public static readonly Dictionary<PlayerRef, Player> playerLookup = new Dictionary<PlayerRef, Player>();

	public static readonly Dictionary<CSteamID, PlayerName> cachedPlayerNames = new Dictionary<CSteamID, PlayerName>();

	public static readonly List<Unit> allUnits = new List<Unit>();

	public static readonly List<Aircraft> allAircraft = new List<Aircraft>();

	private static uint nextIndex;

	private static void Registry_OnUnitDisable(Unit unit)
	{
		allUnits.Remove(unit);
		if (unit is Aircraft item)
		{
			allAircraft.Remove(item);
		}
		unit.onDisableUnit -= Registry_OnUnitDisable;
	}

	public static bool TryGetNearestUnit(GlobalPosition fromPosition, out Unit nearestUnit, float maxDist)
	{
		nearestUnit = null;
		float range = maxDist;
		foreach (Unit allUnit in allUnits)
		{
			if (FastMath.InRange(allUnit.GlobalPosition(), fromPosition, range))
			{
				range = FastMath.Distance(fromPosition, allUnit.GlobalPosition());
				nearestUnit = allUnit;
			}
		}
		return nearestUnit != null;
	}

	public static bool TryGetNearestAircraft(Unit fromUnit, bool playersOnly, FactionHQ faction, out Aircraft nearestAircraft, out float nearestDistance)
	{
		nearestAircraft = null;
		nearestDistance = float.MaxValue;
		foreach (Aircraft item in allAircraft)
		{
			if (item.persistentID != fromUnit.persistentID && (!playersOnly || !(item.Player == null)) && (!(faction != null) || !(item.NetworkHQ != faction)))
			{
				float num = FastMath.SquareDistance(item.GlobalPosition(), fromUnit.GlobalPosition());
				if (num < nearestDistance)
				{
					nearestAircraft = item;
					nearestDistance = num;
				}
			}
		}
		return nearestAircraft != null;
	}

	public static PersistentID GetNextIndex()
	{
		nextIndex++;
		return new PersistentID
		{
			Id = nextIndex
		};
	}

	public static void Clear()
	{
		Debug.LogWarning("UnitRegistry.Reinitialize Called");
		customIDLookup.Clear();
		persistentUnitLookup.Clear();
		playerLookup.Clear();
		cachedPlayerNames.Clear();
		allUnits.Clear();
		allAircraft.Clear();
		nextIndex = 0u;
	}

	public static void AddPlayer(PlayerRef playerRef, Player player)
	{
		if (!playerLookup.ContainsKey(playerRef))
		{
			playerLookup.Add(playerRef, player);
			RichPresenceManager.SetPlayerCount(playerLookup.Count);
		}
	}

	public static void RemovePlayer(PlayerRef playerRef)
	{
		if (playerLookup.Remove(playerRef))
		{
			RichPresenceManager.SetPlayerCount(playerLookup.Count);
		}
	}

	public static void RegisterCustomID(string customID, Unit unit)
	{
		if (customIDLookup.TryGetValue(customID, out var value))
		{
			if (unit != value)
			{
				Debug.LogError($"2 different units had the same name {customID}, unit1:{value}, unit2:{unit}");
			}
			else if (!unit.BuiltIn)
			{
				Debug.LogWarning("Unit with name " + customID + " already registered");
			}
		}
		else
		{
			customIDLookup.Add(customID, unit);
		}
	}

	public static void RegisterUnit(Unit unit, PersistentID id)
	{
		if (!persistentUnitLookup.ContainsKey(id))
		{
			persistentUnitLookup.Add(id, new PersistentUnit(unit, id));
		}
		unit.onDisableUnit += Registry_OnUnitDisable;
		allUnits.Add(unit);
		if (unit is Aircraft item)
		{
			allAircraft.Add(item);
		}
		if (unit.NetworkHQ == null)
		{
			SceneSingleton<DynamicMap>.i.AddIcon(unit.persistentID);
		}
	}

	public static void UnregisterUnit(Unit unit)
	{
		Registry_OnUnitDisable(unit);
	}

	public static bool TryGetPersistentUnit(PersistentID id, out PersistentUnit persistentUnit)
	{
		return persistentUnitLookup.TryGetValue(id, out persistentUnit);
	}

	public static bool TryGetUnit<TUnit>(PersistentID? id, out TUnit unit) where TUnit : Unit
	{
		if (TryGetUnit(id, out var unit2) && unit2 is TUnit val)
		{
			unit = val;
			return true;
		}
		unit = null;
		return false;
	}

	public static bool TryGetUnit(PersistentID? id, out Unit unit)
	{
		if (!id.HasValue)
		{
			unit = null;
			return false;
		}
		if (!persistentUnitLookup.TryGetValue(id.Value, out var value))
		{
			unit = null;
			return false;
		}
		if (value.unit != null)
		{
			unit = value.unit;
			return true;
		}
		unit = null;
		return false;
	}

	public static bool TryGetPosition(SavedUnit savedUnit, out GlobalPosition pos)
	{
		if (customIDLookup.TryGetValue(savedUnit.UniqueName, out var value) && value != null)
		{
			pos = value.GlobalPosition();
			return true;
		}
		pos = default(GlobalPosition);
		return false;
	}
}
