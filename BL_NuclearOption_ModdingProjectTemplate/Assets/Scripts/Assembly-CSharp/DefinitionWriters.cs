using System;
using Mirage.Serialization;
using UnityEngine;

public static class DefinitionWriters
{
	public static void WriteNetworkDefinition(this NetworkWriter writer, INetworkDefinition item)
	{
		uint value = ((item != null) ? checked((uint)(GetIndex(item) + 1)) : 0u);
		writer.WritePackedUInt32(value);
	}

	private static int GetIndex(INetworkDefinition item)
	{
		if (item.LookupIndex.HasValue)
		{
			return item.LookupIndex.Value;
		}
		Debug.LogWarning("Index was not assigned to LookupIndex in Encyclopedia.AfterLoad");
		int num = Encyclopedia.i.IndexLookup.IndexOf(item);
		if (num == -1)
		{
			throw new Exception($"Trying to write {item} but it was not in Encyclopedia lookup");
		}
		item.LookupIndex = num;
		return num;
	}

	public static T ReadNetworkDefinition<T>(this NetworkReader reader) where T : ScriptableObject, INetworkDefinition
	{
		uint num = reader.ReadPackedUInt32();
		if (num == 0)
		{
			return null;
		}
		int index = checked((int)num - 1);
		return (T)Encyclopedia.i.IndexLookup[index];
	}

	public static void WriteUnitDefinition(this NetworkWriter writer, UnitDefinition definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static void WriteAircraftDefinition(this NetworkWriter writer, AircraftDefinition definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static void WriteVehicleDefinition(this NetworkWriter writer, VehicleDefinition definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static void WriteMissileDefinition(this NetworkWriter writer, MissileDefinition definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static void WriteBuildingDefinition(this NetworkWriter writer, BuildingDefinition definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static void WriteShipDefinition(this NetworkWriter writer, ShipDefinition definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static void WriteWeaponMount(this NetworkWriter writer, WeaponMount definition)
	{
		writer.WriteNetworkDefinition(definition);
	}

	public static UnitDefinition ReadUnitDefinition(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<UnitDefinition>();
	}

	public static AircraftDefinition ReadAircraftDefinition(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<AircraftDefinition>();
	}

	public static VehicleDefinition ReadVehicleDefinition(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<VehicleDefinition>();
	}

	public static MissileDefinition ReadMissileDefinition(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<MissileDefinition>();
	}

	public static BuildingDefinition ReadBuildingDefinition(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<BuildingDefinition>();
	}

	public static ShipDefinition ReadShipDefinition(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<ShipDefinition>();
	}

	public static WeaponMount ReadWeaponMount(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<WeaponMount>();
	}

	public static void WriteFaction(this NetworkWriter writer, Faction faction)
	{
		writer.WriteNetworkDefinition(faction);
	}

	public static Faction ReadFaction(this NetworkReader reader)
	{
		return reader.ReadNetworkDefinition<Faction>();
	}
}
