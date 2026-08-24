using System;
using UnityEngine;

public class TrackingInfo
{
	private const float LAST_SPOTTED_EXTRA_TIME = 4f;

	public GlobalPosition lastKnownPosition;

	public float lastSpottedTime;

	public PersistentID id;

	public sbyte attackers;

	public sbyte missileAttacks;

	private Unit unit;

	public event Action OnSpotted;

	public TrackingInfo(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit", "Null unit passed to TrackingInfo");
		}
		this.unit = unit;
		lastKnownPosition = unit.GlobalPosition();
		lastSpottedTime = Time.timeSinceLevelLoad;
		id = unit.persistentID;
		attackers = 0;
		missileAttacks = 0;
	}

	public TrackingInfo(PersistentID id, GlobalPosition lastKnownPosition, float lastSpottedTime)
	{
		this.id = id;
		this.lastKnownPosition = lastKnownPosition;
		this.lastSpottedTime = lastSpottedTime;
	}

	[Obsolete("Use TryGetUnit instead")]
	public Unit GetUnit()
	{
		TryGetUnit(out var _);
		return this.unit;
	}

	public float GetStrategicPriority()
	{
		if (unit == null || unit.disabled)
		{
			return 0f;
		}
		return unit.definition.typeIdentity.strategic / (float)(1 + missileAttacks + attackers);
	}

	public bool TryGetUnit(out Unit unit)
	{
		if (this.unit != null || UnitRegistry.TryGetUnit(id, out this.unit))
		{
			unit = this.unit;
			return true;
		}
		unit = null;
		return false;
	}

	public void UpdateInfo(GlobalPosition position)
	{
		lastKnownPosition = position;
		lastSpottedTime = Time.timeSinceLevelLoad;
		this.OnSpotted?.Invoke();
	}

	public bool Observed()
	{
		return Time.timeSinceLevelLoad - lastSpottedTime < 4f;
	}

	public GlobalPosition GetPosition()
	{
		if (Time.timeSinceLevelLoad - lastSpottedTime < 4f && TryGetUnit(out var unit))
		{
			lastKnownPosition = unit.GlobalPosition();
		}
		return lastKnownPosition;
	}
}
