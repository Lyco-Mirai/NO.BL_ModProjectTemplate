using System;
using Mirage;
using Mirage.Serialization;
using NuclearOption.SavedMission;
using UnityEngine;

public class Factory : NetworkBehaviour
{
	public enum FactoryType
	{
		AircraftAndVehicles = 0,
		Nukes = 1
	}

	public Unit attachedUnit;

	[SyncVar]
	[SerializeField]
	public UnitDefinition productionUnit;

	[SyncVar]
	[SerializeField]
	public float productionInterval;

	[SyncVar]
	[SerializeField]
	public float lastProductionTime;

	[SerializeField]
	private bool aircraft;

	[SerializeField]
	private bool vehicles;

	[SerializeField]
	private bool weapons;

	[Obsolete("Unused")]
	[SerializeField]
	private bool warheads;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 3;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	public FactoryType factoryType
	{
		get
		{
			if (weapons)
			{
				return FactoryType.Nukes;
			}
			return FactoryType.AircraftAndVehicles;
		}
	}

	public float ProductionInterval => productionInterval;

	public UnitDefinition ProductionUnit => productionUnit;

	public UnitDefinition NetworkproductionUnit
	{
		get
		{
			return productionUnit;
		}
		set
		{
			if (!SyncVarEqual(value, productionUnit))
			{
				UnitDefinition unitDefinition = productionUnit;
				productionUnit = value;
				SetDirtyBit(1uL);
			}
		}
	}

	public float NetworkproductionInterval
	{
		get
		{
			return productionInterval;
		}
		set
		{
			if (!SyncVarEqual(value, productionInterval))
			{
				float num = productionInterval;
				productionInterval = value;
				SetDirtyBit(2uL);
			}
		}
	}

	public float NetworklastProductionTime
	{
		get
		{
			return lastProductionTime;
		}
		set
		{
			if (!SyncVarEqual(value, lastProductionTime))
			{
				float num = lastProductionTime;
				lastProductionTime = value;
				SetDirtyBit(4uL);
			}
		}
	}

	public void SetFactory(SavedBuilding.FactoryOptions factoryOptions)
	{
		SetFactory(factoryOptions.productionType, factoryOptions.productionTime);
	}

	public void SetFactory(string productionType, float productionInterval)
	{
		if (!string.IsNullOrEmpty(productionType))
		{
			NetworklastProductionTime = NetworkSceneSingleton<MissionManager>.i.MissionTime;
			if (Encyclopedia.Lookup.TryGetValue(productionType, out var value))
			{
				NetworkproductionUnit = value;
				NetworkproductionInterval = productionInterval;
				attachedUnit.NetworkunitName = value.code + " factory";
				this.StartSlowUpdateDelayed(productionInterval, ProduceUnit);
			}
			else if (productionType == "Nuclear Warhead")
			{
				warheads = true;
				NetworkproductionInterval = productionInterval;
				this.StartSlowUpdateDelayed(productionInterval, ProduceWarhead);
			}
		}
	}

	public string GetProduction()
	{
		string result = "";
		if (productionUnit != null)
		{
			result = productionUnit.code;
		}
		else if (productionInterval > 0f)
		{
			result = "Nuclear Warhead";
		}
		return result;
	}

	public float GetInterval()
	{
		float result = 0f;
		if (productionUnit != null || productionInterval > 0f)
		{
			result = productionInterval;
		}
		return result;
	}

	public float GetNextProduction(bool absolute)
	{
		float result = 0f;
		if (productionUnit != null || productionInterval > 0f)
		{
			if (absolute)
			{
				result = Mathf.RoundToInt(lastProductionTime + productionInterval - NetworkSceneSingleton<MissionManager>.i.MissionTime);
			}
			else if (productionInterval > 0f)
			{
				result = (lastProductionTime + productionInterval - NetworkSceneSingleton<MissionManager>.i.MissionTime) / productionInterval;
			}
		}
		return result;
	}

	private void ProduceUnit()
	{
		if (attachedUnit.NetworkHQ != null)
		{
			attachedUnit.NetworkHQ.AddSupplyUnit(productionUnit, 1);
			NetworklastProductionTime = NetworkSceneSingleton<MissionManager>.i.MissionTime;
		}
	}

	private void ProduceWarhead()
	{
		if (attachedUnit.NetworkHQ != null)
		{
			attachedUnit.NetworkHQ.AddWarheadStockpile(1);
		}
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
			writer.WriteUnitDefinition(productionUnit);
			writer.WriteSingleConverter(productionInterval);
			writer.WriteSingleConverter(lastProductionTime);
			return true;
		}
		writer.Write(syncVarDirtyBits, 3);
		if ((syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteUnitDefinition(productionUnit);
			result = true;
		}
		if ((syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteSingleConverter(productionInterval);
			result = true;
		}
		if ((syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteSingleConverter(lastProductionTime);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			productionUnit = reader.ReadUnitDefinition();
			productionInterval = reader.ReadSingleConverter();
			lastProductionTime = reader.ReadSingleConverter();
			return;
		}
		ulong num = reader.Read(3);
		SetDeserializeMask(num, 0);
		if ((num & 1L) != 0L)
		{
			productionUnit = reader.ReadUnitDefinition();
		}
		if ((num & 2L) != 0L)
		{
			productionInterval = reader.ReadSingleConverter();
		}
		if ((num & 4L) != 0L)
		{
			lastProductionTime = reader.ReadSingleConverter();
		}
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
