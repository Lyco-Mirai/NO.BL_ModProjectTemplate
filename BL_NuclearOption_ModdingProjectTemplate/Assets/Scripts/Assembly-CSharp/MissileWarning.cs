using System;
using System.Collections.Generic;
using UnityEngine;

public class MissileWarning : MonoBehaviour
{
	public struct OnMissileWarning
	{
		public Missile missile;
	}

	public struct OffMissileWarning
	{
		public Missile missile;
	}

	private List<Missile> unknownMissiles = new List<Missile>();

	public List<Missile> knownMissiles = new List<Missile>();

	[SerializeField]
	private float detectionRange = 5000f;

	private float lastDetectionCheck;

	private Aircraft aircraft;

	public event Action<OnMissileWarning> onMissileWarning;

	public event Action<OffMissileWarning> offMissileWarning;

	public void LockedByMissile(Aircraft aircraft, Missile missile)
	{
		base.enabled = true;
		this.aircraft = aircraft;
		unknownMissiles.Add(missile);
	}

	public bool IsWarning()
	{
		return knownMissiles.Count > 0;
	}

	public bool TryGetNearestIncoming(out Missile nearestIncoming)
	{
		nearestIncoming = null;
		if (knownMissiles.Count == 0)
		{
			return false;
		}
		float num = float.MaxValue;
		foreach (Missile knownMissile in knownMissiles)
		{
			float num2 = FastMath.SquareDistance(knownMissile.GlobalPosition(), aircraft.GlobalPosition());
			if (num2 < num)
			{
				nearestIncoming = knownMissile;
				num = num2;
			}
		}
		return nearestIncoming != null;
	}

	private void Update()
	{
		if ((unknownMissiles.Count == 0 && knownMissiles.Count == 0) || aircraft.disabled)
		{
			base.enabled = false;
		}
		if (Time.timeSinceLevelLoad - lastDetectionCheck < 0.2f)
		{
			return;
		}
		lastDetectionCheck = Time.timeSinceLevelLoad;
		for (int num = unknownMissiles.Count - 1; num >= 0; num--)
		{
			if (UnitRegistry.TryGetUnit((PersistentID?)unknownMissiles[num].persistentID, out Missile unit))
			{
				if (unit.disabled || unit.targetID != aircraft.persistentID)
				{
					unknownMissiles.RemoveAt(num);
				}
				else if (FastMath.InRange(unit.transform.position, base.transform.position, detectionRange) && unit.LineOfSight(aircraft.transform.position, 1000f))
				{
					knownMissiles.Add(unit);
					aircraft.NetworkHQ.CmdUpdateTrackingInfo(unit.persistentID);
					this.onMissileWarning?.Invoke(new OnMissileWarning
					{
						missile = unit
					});
					unknownMissiles.RemoveAt(num);
				}
				else if ((int)unit.seekerMode < 3 && (aircraft.NetworkHQ.GetTrackingData(unit.persistentID) != null || !(unit.NetworkHQ != aircraft.NetworkHQ)))
				{
					knownMissiles.Add(unit);
					this.onMissileWarning?.Invoke(new OnMissileWarning
					{
						missile = unit
					});
					unknownMissiles.RemoveAt(num);
				}
			}
		}
		for (int num2 = knownMissiles.Count - 1; num2 >= 0; num2--)
		{
			Missile missile = knownMissiles[num2];
			if (missile == null || missile.disabled || missile.targetID != aircraft.persistentID)
			{
				knownMissiles.RemoveAt(num2);
				if (missile != null)
				{
					this.offMissileWarning?.Invoke(new OffMissileWarning
					{
						missile = missile
					});
				}
			}
		}
	}
}
