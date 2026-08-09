using UnityEngine;

public class FriendlyAircraftProximity
{
	private float moraleBoost;

	private float lastMoraleCheck;

	public float CheckMorale(Aircraft tracker)
	{
		if (Time.timeSinceLevelLoad - lastMoraleCheck < 5f)
		{
			return moraleBoost;
		}
		lastMoraleCheck = Time.timeSinceLevelLoad;
		moraleBoost = 0f;
		foreach (Unit item in BattlefieldGrid.GetUnitsInRangeEnumerable(tracker.transform.position.ToGlobalPosition(), 5000f))
		{
			if (item.NetworkHQ == tracker.NetworkHQ && item != tracker && item.radarAlt > 10f && item is Aircraft aircraft)
			{
				moraleBoost += 1000f / Mathf.Max(Vector3.Distance(aircraft.transform.position, tracker.transform.position), 1000f);
			}
		}
		return moraleBoost;
	}
}
