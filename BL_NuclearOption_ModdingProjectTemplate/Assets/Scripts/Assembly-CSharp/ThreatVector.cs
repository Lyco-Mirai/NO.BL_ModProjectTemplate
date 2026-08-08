using UnityEngine;

public class ThreatVector
{
	private FactionHQ hq;

	private int index;

	private Unit owner;

	public Vector3 threatVector;

	private Vector3 constructThreatVector;

	public ThreatVector(Unit owner)
	{
		this.owner = owner;
		hq = owner.NetworkHQ;
	}

	private void IterateThreats(Unit currentTarget)
	{
		index++;
		if (index >= UnitRegistry.allUnits.Count)
		{
			index = 0;
			threatVector = constructThreatVector;
			threatVector.y = 0f;
			constructThreatVector = Vector3.zero;
			if (!hq.TryGetNearestAirbase(owner.transform.position, out var nearestAirbase) || Vector3.Dot(-threatVector, owner.transform.position - nearestAirbase.center.position) < 0f)
			{
				threatVector = Vector3.zero;
			}
			return;
		}
		Unit unit = UnitRegistry.allUnits[index];
		if (!(unit == null) && !unit.disabled && !(unit.NetworkHQ == null) && !(unit.NetworkHQ == owner.NetworkHQ) && !(unit == currentTarget) && !(unit is Missile) && hq.IsTargetPositionAccurate(unit, 1000f))
		{
			float num = owner.definition.ThreatPosedBy(unit.definition.roleIdentity);
			Vector3 vector = unit.GlobalPosition() - owner.GlobalPosition();
			float magnitude = vector.magnitude;
			if (!(magnitude < 3000f))
			{
				constructThreatVector += vector.normalized * num * 2000f / magnitude;
			}
		}
	}

	public Vector3 CheckThreats(Unit currentTarget)
	{
		IterateThreats(currentTarget);
		return threatVector;
	}
}
