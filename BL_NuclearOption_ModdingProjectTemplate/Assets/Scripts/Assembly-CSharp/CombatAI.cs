using System.Collections.Generic;
using UnityEngine;

public static class CombatAI
{
	private struct TargetCandidate
	{
		public readonly Unit unit;

		public readonly TrackingInfo trackingInfo;

		public readonly float range;

		public TargetCandidate(TrackingInfo trackingInfo, float range)
		{
			unit = trackingInfo.GetUnit();
			this.trackingInfo = trackingInfo;
			this.range = range;
		}
	}

	private class TargetAttack
	{
		public readonly Unit unit;

		public int attacks;

		public TargetAttack(Unit unit, int attacks)
		{
			this.unit = unit;
			this.attacks = attacks;
		}
	}

	public struct TargetSearchResults
	{
		public readonly Unit target;

		public readonly WeaponStation chosenWeaponStation;

		public readonly float opportunity;

		public readonly bool outOfAmmo;

		public TargetSearchResults(Unit target, WeaponStation chosenWeaponStation, float opportunity, bool outOfAmmo)
		{
			this.target = target;
			this.chosenWeaponStation = chosenWeaponStation;
			this.opportunity = opportunity;
			this.outOfAmmo = outOfAmmo;
		}
	}

	private static List<TargetCandidate> targetCandidates = new List<TargetCandidate>();

	private static Dictionary<WeaponInfo, float> exclusionRadiusLookup = new Dictionary<WeaponInfo, float>();

	private static List<Unit> unitsInBlastRadius;

	private static List<TargetAttack> targetsAttacked;

	public static float GetExclusionRadius(WeaponInfo weaponInfo)
	{
		if (exclusionRadiusLookup.TryGetValue(weaponInfo, out var value))
		{
			return value;
		}
		float num = Mathf.Pow(weaponInfo.weaponPrefab.GetComponent<Missile>().GetYield(), 0.3333f) * 13f;
		exclusionRadiusLookup.Add(weaponInfo, num);
		return num;
	}

	public static float GetSafeStandoffDist(GlobalPosition fromPosition, FactionHQ hq)
	{
		float num = 0f;
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in hq.trackingDatabase)
		{
			if (item.Value.TryGetUnit(out var unit) && unit.radarAlt < 10f && !(unit is Aircraft))
			{
				float num2 = unit.GetMaxRange() * 1.2f;
				if (num2 != 0f && FastMath.InRange(item.Value.lastKnownPosition, fromPosition, num2))
				{
					num = Mathf.Max(num, num2 - FastMath.Distance(item.Value.lastKnownPosition, fromPosition));
				}
			}
		}
		return num;
	}

	public static float InterceptViability(Unit target, Unit analyzer, WeaponStation weaponStation, float maxRange, float targetDist, float targetMaxSpeed)
	{
		Vector3 vector = FastMath.NormalizedDirection(analyzer.GlobalPosition(), target.GlobalPosition());
		Vector3 rhs = target.rb.velocity + Mathf.Max(target.speed, targetMaxSpeed) * vector;
		float num = Vector3.Dot(-vector, rhs) / weaponStation.WeaponInfo.GetMaxSpeed();
		return Mathf.Min(maxRange * (1f + num) / targetDist - 1f, 1f);
	}

	public static OpportunityThreat AnalyzeTarget(WeaponStation weaponStation, Unit analyzer, TrackingInfo trackingInfo, float armorTierOptimism = 0f, float targetDistance = -1f, float maxRangeMultiplier = 1f)
	{
		if (!trackingInfo.TryGetUnit(out var unit))
		{
			return new OpportunityThreat(0f, 0f);
		}
		TargetRequirements targetRequirements = weaponStation.WeaponInfo.targetRequirements;
		OpportunityThreat opportunityThreat = weaponStation.CalcOpportunityThreat(unit.definition, analyzer);
		float num = opportunityThreat.opportunity;
		float threat = opportunityThreat.threat;
		if (targetDistance < 0f)
		{
			targetDistance = FastMath.Distance(analyzer.GlobalPosition(), trackingInfo.GetPosition());
		}
		if (num == 0f || (float)trackingInfo.missileAttacks > weaponStation.WeaponInfo.CalcAttacksNeeded(unit))
		{
			return new OpportunityThreat(0f, threat);
		}
		threat /= (float)(1 + 2 * Mathf.Max(trackingInfo.missileAttacks + trackingInfo.attackers, 0));
		float num2 = targetRequirements.minAltitude * targetDistance / targetRequirements.maxRange;
		if (weaponStation.WeaponInfo.nuclear)
		{
			if (trackingInfo.attackers > 0)
			{
				return new OpportunityThreat(0f, threat);
			}
			num *= unit.definition.typeIdentity.strategic;
		}
		if (unit.radarAlt < num2 || unit.radarAlt > targetRequirements.maxAltitude)
		{
			return new OpportunityThreat(0f, threat);
		}
		if (unit.speed > targetRequirements.maxSpeed)
		{
			return new OpportunityThreat(0f, threat);
		}
		if (targetRequirements.minIR > 0f && !unit.HasIRSignature())
		{
			return new OpportunityThreat(0f, threat);
		}
		if (targetRequirements.minRadar > 0f && !unit.HasRadarEmission())
		{
			return new OpportunityThreat(0f, threat);
		}
		if (weaponStation.WeaponInfo.armorTierEffectiveness + armorTierOptimism < unit.definition.armorTier)
		{
			return new OpportunityThreat(0f, threat);
		}
		float costPerRound = weaponStation.WeaponInfo.costPerRound;
		if (unit.radarAlt > 1f)
		{
			float num3 = 0f;
			bool flag = false;
			if (unit is Missile missile)
			{
				num3 = missile.GetWeaponInfo().GetMaxSpeed() * 0.67f;
				if (missile.targetID == analyzer.persistentID)
				{
					threat *= 3f;
					num *= 3f;
					flag = true;
				}
				else
				{
					num *= missile.InterceptPriority(analyzer, 5000f, costPerRound);
				}
			}
			if (!flag && num3 > 0f)
			{
				num *= InterceptViability(unit, analyzer, weaponStation, targetRequirements.maxRange, targetDistance, num3);
			}
		}
		num *= Mathf.Lerp(1f, 0f, targetDistance / (targetRequirements.maxRange * maxRangeMultiplier));
		num *= (float)((targetRequirements.lineOfSight || !(targetRequirements.maxRange > 10000f)) ? 1 : 100);
		num *= 1f + Mathf.Sqrt(unit.definition.value) * 0.01f;
		return new OpportunityThreat(num, threat);
	}

	public static int LookForMissileTargets(Aircraft aircraft, Unit currentTarget, WeaponStation weaponStation, List<Unit> outTargets)
	{
		int num = 0;
		int num2 = 1000;
		float minAlignment = weaponStation.WeaponInfo.targetRequirements.minAlignment;
		float maxRange = weaponStation.WeaponInfo.targetRequirements.maxRange;
		if (targetsAttacked == null)
		{
			targetsAttacked = new List<TargetAttack>();
		}
		targetsAttacked.Clear();
		GlobalPosition a = aircraft.GlobalPosition();
		int num3 = (weaponStation.WeaponInfo.laserGuided ? aircraft.GetLaserDesignator().GetMaxTargets() : 16);
		if (!aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition))
		{
			return 0;
		}
		int attacks = Mathf.Max(Mathf.FloorToInt(weaponStation.WeaponInfo.CalcAttacksNeeded(currentTarget)), 1);
		targetsAttacked.Add(new TargetAttack(currentTarget, attacks));
		num++;
		foreach (Unit item in BattlefieldGrid.GetUnitsInRangeEnumerable(knownPosition, num2))
		{
			TrackingInfo trackingData = aircraft.NetworkHQ.GetTrackingData(item.persistentID);
			if (item.NetworkHQ == null || item.NetworkHQ == aircraft.NetworkHQ || trackingData == null || item == currentTarget || !(Vector3.Angle(item.transform.position - aircraft.transform.position, aircraft.transform.forward) < minAlignment) || !(AnalyzeTarget(weaponStation, aircraft, trackingData).opportunity > 0f) || !FastMath.InRange(trackingData.lastKnownPosition, item.GlobalPosition(), 20f) || !FastMath.InRange(a, trackingData.lastKnownPosition, maxRange) || (weaponStation.WeaponInfo.targetRequirements.lineOfSight && !item.LineOfSight(aircraft.transform.position, 1000f)))
			{
				continue;
			}
			int num4 = Mathf.Max(Mathf.FloorToInt(weaponStation.WeaponInfo.CalcAttacksNeeded(item)), 1);
			num4 -= trackingData.missileAttacks;
			if (num4 > 0)
			{
				targetsAttacked.Add(new TargetAttack(item, num4));
				num++;
				if (num >= num3)
				{
					break;
				}
			}
		}
		DistributeTargets(aircraft, targetsAttacked, outTargets);
		return num;
	}

	public static int LookForJammingTargets(Aircraft aircraft, Unit currentTarget, WeaponStation weaponStation, List<Unit> outTargets)
	{
		if (targetsAttacked == null)
		{
			targetsAttacked = new List<TargetAttack>();
		}
		targetsAttacked.Clear();
		targetCandidates.Clear();
		GlobalPosition b = aircraft.GlobalPosition();
		int count = weaponStation.Weapons.Count;
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in aircraft.NetworkHQ.trackingDatabase)
		{
			TrackingInfo value = item.Value;
			if (value.TryGetUnit(out var unit) && unit.NetworkHQ != null && unit.NetworkHQ != aircraft.NetworkHQ && unit.HasRadarEmission() && FastMath.InRange(value.GetPosition(), b, weaponStation.WeaponInfo.targetRequirements.maxRange) && unit.LineOfSight(aircraft.transform.position, 1000f))
			{
				targetCandidates.Add(new TargetCandidate(value, FastMath.Distance(value.lastKnownPosition, aircraft.GlobalPosition())));
			}
		}
		targetCandidates.Sort((TargetCandidate a, TargetCandidate targetCandidate) => targetCandidate.trackingInfo.missileAttacks.CompareTo(a.trackingInfo.missileAttacks));
		for (int num = 0; num < targetCandidates.Count && num < count; num++)
		{
			outTargets.Add(targetCandidates[num].unit);
		}
		return outTargets.Count;
	}

	public static TargetSearchResults ChooseHQTarget(Unit searcher, float bravery, List<WeaponStation> stationList)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		bool outOfAmmo = stationList.Count == 0;
		Unit unit = null;
		WeaponStation weaponStation = null;
		if (targetCandidates == null)
		{
			targetCandidates = new List<TargetCandidate>();
		}
		else
		{
			targetCandidates.Clear();
		}
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in searcher.NetworkHQ.trackingDatabase)
		{
			TrackingInfo value = item.Value;
			if (value.TryGetUnit(out var unit2) && unit2.NetworkHQ != null && unit2.NetworkHQ != searcher.NetworkHQ)
			{
				float range = FastMath.Distance(value.GetPosition(), searcher.GlobalPosition());
				targetCandidates.Add(new TargetCandidate(value, range));
			}
		}
		foreach (WeaponStation station in stationList)
		{
			if (station.Ammo <= 0 || (station.WeaponInfo.energy && searcher.GetPowerSupply().GetCharge() < 0.6f))
			{
				outOfAmmo = true;
			}
			else
			{
				if (station.Cargo)
				{
					continue;
				}
				_ = station.WeaponInfo.targetRequirements;
				foreach (TargetCandidate targetCandidate in targetCandidates)
				{
					OpportunityThreat opportunityThreat = AnalyzeTarget(station, searcher, targetCandidate.trackingInfo, 0f, targetCandidate.range, 100f);
					num = Mathf.Max(num, opportunityThreat.opportunity);
					float num4 = opportunityThreat.opportunity * (1f + opportunityThreat.threat) / targetCandidate.range;
					if (targetCandidate.range > station.WeaponInfo.targetRequirements.maxRange * 1.2f)
					{
						num4 *= 0.5f;
					}
					if (num4 > num3 && searcher.NetworkHQ.IsTargetPositionAccurate(targetCandidate.unit, 1000f))
					{
						unit = targetCandidate.unit;
						num3 = num4;
						weaponStation = station;
						num2 = targetCandidate.range;
					}
				}
			}
		}
		if (unit != null && num * bravery * 2f < 0.35f && searcher.NetworkHQ.GetAircraftThreat(unit.persistentID) > num * bravery * 2f && num2 > weaponStation.WeaponInfo.targetRequirements.maxRange * 2f)
		{
			unit = null;
			num = 0f;
		}
		return new TargetSearchResults(unit, weaponStation, num, outOfAmmo);
	}

	public static List<Unit> FilterTargetsWithinCone(List<Unit> unitList, Transform firingCone, float maxDegrees)
	{
		for (int num = unitList.Count - 1; num >= 0; num--)
		{
			if (Vector3.Angle(unitList[num].transform.position - firingCone.position, firingCone.forward) > maxDegrees)
			{
				unitList.RemoveAt(num);
			}
		}
		return unitList;
	}

	private static void DistributeTargets(Aircraft aircraft, List<TargetAttack> targetsAttacks, List<Unit> outTargets)
	{
		targetsAttacks.Sort((TargetAttack a, TargetAttack b) => aircraft.definition.ThreatPosedBy(b.unit.definition.roleIdentity).CompareTo(aircraft.definition.ThreatPosedBy(a.unit.definition.roleIdentity)));
		while (targetsAttacks.Count > 0)
		{
			for (int num = targetsAttacks.Count - 1; num >= 0; num--)
			{
				if (outTargets.Count >= 128)
				{
					targetsAttacks.Clear();
					break;
				}
				outTargets.Add(targetsAttacks[num].unit);
				targetsAttacks[num].attacks--;
				if (targetsAttacks[num].attacks <= 0)
				{
					targetsAttacks.RemoveAt(num);
				}
			}
		}
		outTargets.Reverse();
	}

	public static int LookForBombingTargets(Aircraft aircraft, Unit currentTarget, WeaponStation weaponStation, List<Unit> outTargets)
	{
		int num = 0;
		float range = 200f + aircraft.radarAlt;
		if (targetsAttacked == null)
		{
			targetsAttacked = new List<TargetAttack>();
		}
		targetsAttacked.Clear();
		if (!aircraft.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition))
		{
			return 0;
		}
		foreach (Unit item in BattlefieldGrid.GetUnitsInRangeEnumerable(knownPosition, range))
		{
			TrackingInfo trackingData = aircraft.NetworkHQ.GetTrackingData(item.persistentID);
			if (!(item.NetworkHQ == null) && !(item.NetworkHQ == aircraft.NetworkHQ) && trackingData != null && !FastMath.OutOfRange(item.transform.position, currentTarget.transform.position, range) && aircraft.NetworkHQ.IsTargetPositionAccurate(item, 50f) && AnalyzeTarget(weaponStation, aircraft, trackingData, 0f, 1000f, 100f).opportunity > 0f)
			{
				int attacks = Mathf.Max(Mathf.CeilToInt(weaponStation.WeaponInfo.CalcAttacksNeeded(item)), 1);
				targetsAttacked.Add(new TargetAttack(item, attacks));
				num++;
			}
		}
		if (targetsAttacked.Count == 0)
		{
			targetsAttacked.Add(new TargetAttack(currentTarget, 1));
		}
		DistributeTargets(aircraft, targetsAttacked, outTargets);
		return num;
	}
}
