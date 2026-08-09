using System;
using Mirage;
using Mirage.Collections;
using Mirage.Serialization;
using NuclearOption.Networking;

public class MissionStatsTracker : NetworkBehaviour
{
	public struct Stat
	{
		public float total;

		public float current;

		public float spent;

		public float lost;

		public static readonly Stat Default = new Stat
		{
			total = 0f,
			current = 0f,
			spent = 0f,
			lost = 0f
		};
	}

	public struct TypeStat
	{
		public Stat total;

		public Stat buildings;

		public Stat ships;

		public Stat vehicles;

		public Stat aircraft;
	}

	public FactionHQ hq;

	public readonly SyncDictionary<UnitDefinition, int> currentUnits = new SyncDictionary<UnitDefinition, int>();

	public readonly SyncDictionary<UnitDefinition, int> lostUnits = new SyncDictionary<UnitDefinition, int>();

	[SyncVar]
	public TypeStat manpower;

	[SyncVar]
	public TypeStat value;

	[SyncVar]
	public TypeStat units;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 3;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public TypeStat Networkmanpower
	{
		get
		{
			return manpower;
		}
		set
		{
			if (!SyncVarEqual(value, manpower))
			{
				TypeStat typeStat = manpower;
				manpower = value;
				SetDirtyBit(1uL);
			}
		}
	}

	public TypeStat Networkvalue
	{
		get
		{
			return value;
		}
		set
		{
			if (!SyncVarEqual(value, this.value))
			{
				TypeStat typeStat = this.value;
				this.value = value;
				SetDirtyBit(2uL);
			}
		}
	}

	public TypeStat Networkunits
	{
		get
		{
			return units;
		}
		set
		{
			if (!SyncVarEqual(value, units))
			{
				TypeStat typeStat = units;
				units = value;
				SetDirtyBit(4uL);
			}
		}
	}

	public void Start()
	{
		hq.onRemoveUnit += LostUnit;
		hq.onRegisterUnit += NewUnit;
	}

	[Server]
	public void LostUnit(Unit unit)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'LostUnit' called when server not active");
		}
		if (!NetworkManagerNuclearOption.i.Server.Active || !unit.disabled)
		{
			return;
		}
		if (currentUnits.TryGetValue(unit.definition, out var num))
		{
			int num2 = num - 1;
			currentUnits[unit.definition] = ((num2 > 0) ? num2 : 0);
		}
		if (unit.unitState != Unit.UnitState.Returned)
		{
			if (lostUnits.TryGetValue(unit.definition, out var num3))
			{
				int num4 = num3 + 1;
				lostUnits[unit.definition] = num4;
			}
			else
			{
				lostUnits.Add(unit.definition, 1);
			}
			if (!(unit is Missile))
			{
				value.total.current -= unit.definition.value;
				value.total.lost += unit.definition.value;
				if (!(unit is Aircraft))
				{
					manpower.total.current -= unit.definition.manpower;
					manpower.total.lost += unit.definition.manpower;
				}
			}
		}
		if (unit is Building)
		{
			units.buildings.current -= 1f;
			units.buildings.lost += 1f;
			value.buildings.current -= unit.definition.value;
			value.buildings.lost += unit.definition.value;
			manpower.buildings.current -= unit.definition.manpower;
			manpower.buildings.lost += unit.definition.manpower;
		}
		else if (unit is Ship)
		{
			units.ships.current -= 1f;
			units.ships.lost += 1f;
			value.ships.current -= unit.definition.value;
			value.ships.lost += unit.definition.value;
			manpower.ships.current -= unit.definition.manpower;
			manpower.ships.lost += unit.definition.manpower;
		}
		else if (unit is GroundVehicle)
		{
			units.vehicles.current -= 1f;
			units.vehicles.lost += 1f;
			value.vehicles.current -= unit.definition.value;
			value.vehicles.lost += unit.definition.value;
			manpower.vehicles.current -= unit.definition.manpower;
			manpower.vehicles.lost += unit.definition.manpower;
		}
		else if (unit is Aircraft aircraft)
		{
			units.aircraft.current -= 1f;
			value.aircraft.current -= unit.definition.value;
			if (aircraft.unitState != Unit.UnitState.Returned)
			{
				units.aircraft.lost += 1f;
				value.aircraft.lost += unit.definition.value;
			}
			Pilot[] pilots = aircraft.pilots;
			for (int i = 0; i < pilots.Length; i++)
			{
				if (pilots[i].dead)
				{
					manpower.total.current -= 1f;
					manpower.total.lost += 1f;
					manpower.aircraft.current -= 1f;
					manpower.aircraft.lost += 1f;
				}
			}
		}
		else if (unit is PilotDismounted)
		{
			if (unit.unitState == Unit.UnitState.Destroyed)
			{
				manpower.aircraft.current -= 1f;
				manpower.aircraft.lost += 1f;
			}
			else if (unit.unitState == Unit.UnitState.Returned)
			{
				manpower.total.total -= 1f;
				manpower.total.current -= 1f;
				manpower.aircraft.total -= 1f;
				manpower.aircraft.current -= 1f;
			}
		}
	}

	[Server]
	public void NewUnit(Unit unit)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'NewUnit' called when server not active");
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			if (currentUnits.TryGetValue(unit.definition, out var num))
			{
				int num2 = num + 1;
				currentUnits[unit.definition] = num2;
			}
			else
			{
				currentUnits.Add(unit.definition, 1);
			}
			if (!(unit is Missile) && !(unit is PilotDismounted))
			{
				value.total.total += unit.definition.value;
				value.total.current += unit.definition.value;
				manpower.total.total += unit.definition.manpower;
				manpower.total.current += unit.definition.manpower;
			}
			if (unit is Building)
			{
				units.buildings.total += 1f;
				units.buildings.current += 1f;
				value.buildings.total += unit.definition.value;
				value.buildings.current += unit.definition.value;
				manpower.buildings.total += unit.definition.manpower;
				manpower.buildings.current += unit.definition.manpower;
			}
			else if (unit is Ship)
			{
				units.ships.total += 1f;
				units.ships.current += 1f;
				value.ships.total += unit.definition.value;
				value.ships.current += unit.definition.value;
				manpower.ships.total += unit.definition.manpower;
				manpower.ships.current += unit.definition.manpower;
			}
			else if (unit is GroundVehicle)
			{
				units.vehicles.total += 1f;
				units.vehicles.current += 1f;
				value.vehicles.total += unit.definition.value;
				value.vehicles.current += unit.definition.value;
				manpower.vehicles.total += unit.definition.manpower;
				manpower.vehicles.current += unit.definition.manpower;
			}
			else if (unit is Aircraft)
			{
				units.aircraft.total += 1f;
				units.aircraft.current += 1f;
				value.aircraft.total += unit.definition.value;
				value.aircraft.current += unit.definition.value;
				manpower.aircraft.total += unit.definition.manpower;
				manpower.aircraft.current += unit.definition.manpower;
			}
		}
	}

	[Server]
	public void MunitionCost(Unit unit, float cost)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'MunitionCost' called when server not active");
		}
		value.total.total += cost;
		value.total.spent += cost;
		if (unit is Ship)
		{
			value.ships.total += cost;
			value.ships.spent += cost;
		}
		else if (unit is Building)
		{
			value.buildings.total += cost;
			value.buildings.spent += cost;
		}
		else if (unit is GroundVehicle)
		{
			value.vehicles.total += cost;
			value.vehicles.spent += cost;
		}
		else if (unit is Aircraft)
		{
			value.aircraft.total += cost;
			value.aircraft.spent += cost;
		}
	}

	public int GetLostUnits(UnitDefinition definition)
	{
		if (lostUnits.TryGetValue(definition, out var result))
		{
			return result;
		}
		return 0;
	}

	public int GetCurrentUnits(UnitDefinition definition)
	{
		if (currentUnits.TryGetValue(definition, out var result))
		{
			return result;
		}
		return 0;
	}

	public MissionStatsTracker()
	{
		InitSyncObject(currentUnits);
		InitSyncObject(lostUnits);
	}

	private void MirageProcessed()
	{
	}

	public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
	{
		ulong syncVarDirtyBits = base.SyncVarDirtyBits;
		bool result = base.SerializeSyncVars(writer, initialize);
		if (initialize)
		{
			GeneratedNetworkCode._Write_MissionStatsTracker_002FTypeStat(writer, manpower);
			GeneratedNetworkCode._Write_MissionStatsTracker_002FTypeStat(writer, value);
			GeneratedNetworkCode._Write_MissionStatsTracker_002FTypeStat(writer, units);
			return true;
		}
		writer.Write(syncVarDirtyBits, 3);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			GeneratedNetworkCode._Write_MissionStatsTracker_002FTypeStat(writer, manpower);
			result = true;
		}
		if ((syncVarDirtyBits & 2L) != 0L)
		{
			GeneratedNetworkCode._Write_MissionStatsTracker_002FTypeStat(writer, value);
			result = true;
		}
		if ((syncVarDirtyBits & 4L) != 0L)
		{
			GeneratedNetworkCode._Write_MissionStatsTracker_002FTypeStat(writer, units);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			manpower = GeneratedNetworkCode._Read_MissionStatsTracker_002FTypeStat(reader);
			value = GeneratedNetworkCode._Read_MissionStatsTracker_002FTypeStat(reader);
			units = GeneratedNetworkCode._Read_MissionStatsTracker_002FTypeStat(reader);
			return;
		}
		ulong num = reader.Read(3);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			manpower = GeneratedNetworkCode._Read_MissionStatsTracker_002FTypeStat(reader);
		}
		if ((num & 2L) != 0L)
		{
			value = GeneratedNetworkCode._Read_MissionStatsTracker_002FTypeStat(reader);
		}
		if ((num & 4L) != 0L)
		{
			units = GeneratedNetworkCode._Read_MissionStatsTracker_002FTypeStat(reader);
		}
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
