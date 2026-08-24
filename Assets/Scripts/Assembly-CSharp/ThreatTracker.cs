using System.Collections.Generic;
using UnityEngine;

public class ThreatTracker
{
	private class UnitThreatInfo
	{
		public Unit unit;

		public TrackingInfo trackingInfo;

		private float individualThreat;

		private float combinedThreat;

		public UnitThreatInfo(TrackingInfo trackingInfo, TypeIdentity typeIdentity)
		{
			this.trackingInfo = trackingInfo;
			unit = trackingInfo.GetUnit();
			individualThreat = typeIdentity.ThreatPosedBy(unit.definition.roleIdentity);
		}

		public void ResetCombinedThreat()
		{
			combinedThreat = 0f;
		}

		public void AddCombinedThreat(TrackingInfo otherUnit, float otherThreat)
		{
			if (!(otherThreat < 0.4f) && otherUnit != trackingInfo)
			{
				float num = Mathf.Max(1f, 0.001f * FastMath.Distance(otherUnit.lastKnownPosition, trackingInfo.lastKnownPosition));
				combinedThreat += otherThreat / num;
			}
		}

		public float GetIndividualThreat()
		{
			return individualThreat;
		}

		public float GetCombinedThreat()
		{
			return individualThreat + combinedThreat;
		}
	}

	private FactionHQ hq;

	private TypeIdentity typeIdentity;

	private int checkIndex;

	private float lastCheck;

	private float checkInterval;

	private List<UnitThreatInfo> threatList;

	private Dictionary<PersistentID, UnitThreatInfo> threatLookup;

	public ThreatTracker(FactionHQ hq, float checkInterval, TypeIdentity typeIdentity)
	{
		this.hq = hq;
		threatList = new List<UnitThreatInfo>();
		threatLookup = new Dictionary<PersistentID, UnitThreatInfo>();
		this.typeIdentity = typeIdentity;
		this.checkInterval = checkInterval;
		hq.onDiscoverUnit += ThreatTracker_OnDiscoverUnit;
		hq.onForgetUnit += ThreatTracker_OnForgetUnit;
	}

	private void ThreatTracker_OnDiscoverUnit(PersistentID id)
	{
		UnitThreatInfo unitThreatInfo = new UnitThreatInfo(hq.trackingDatabase[id], typeIdentity);
		threatList.Add(unitThreatInfo);
		threatLookup.Add(id, unitThreatInfo);
	}

	private void ThreatTracker_OnForgetUnit(PersistentID id)
	{
		if (threatLookup.Remove(id, out var value))
		{
			threatList.Remove(value);
		}
	}

	public void CheckThreats()
	{
		if (Time.timeSinceLevelLoad - lastCheck < checkInterval || threatList.Count == 0)
		{
			return;
		}
		if (checkIndex >= threatList.Count)
		{
			checkIndex = 0;
		}
		UnitThreatInfo unitThreatInfo = threatList[checkIndex];
		unitThreatInfo.ResetCombinedThreat();
		TrackingInfo trackingInfo = unitThreatInfo.trackingInfo;
		foreach (UnitThreatInfo threat in threatList)
		{
			unitThreatInfo.AddCombinedThreat(trackingInfo, threat.GetIndividualThreat());
		}
		checkIndex++;
	}

	public float GetThreat(PersistentID id)
	{
		if (threatLookup.TryGetValue(id, out var value))
		{
			return value.GetCombinedThreat();
		}
		Debug.LogError($"unit {id} was not found in {hq}'s threat lookup");
		return 0f;
	}
}
