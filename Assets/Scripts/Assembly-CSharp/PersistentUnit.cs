using NuclearOption.Networking;

public class PersistentUnit
{
	public Unit unit;

	public PersistentID id;

	private FactionHQ _hq;

	public string unitName;

	public Player player;

	public UnitDefinition definition;

	public PersistentUnit(Unit unit, PersistentID id)
	{
		this.unit = unit;
		this.id = id;
		_hq = unit.NetworkHQ;
		unitName = unit.unitName;
		definition = unit.definition;
		if (unit is GroundVehicle groundVehicle && groundVehicle.Networkowner != null)
		{
			player = groundVehicle.Networkowner;
		}
	}

	public void SetHQ(FactionHQ value)
	{
		_hq = value;
	}

	public FactionHQ GetHQ()
	{
		if (unit != null)
		{
			_hq = unit.NetworkHQ;
		}
		return _hq;
	}

	public bool HasHQ(out FactionHQ hq)
	{
		hq = GetHQ();
		return hq != null;
	}

	public Faction GetFaction()
	{
		FactionHQ hQ = GetHQ();
		if (!(hQ != null))
		{
			return null;
		}
		return hQ.faction;
	}
}
