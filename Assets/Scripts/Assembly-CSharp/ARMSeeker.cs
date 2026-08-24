using System.Collections.Generic;
using UnityEngine;

public class ARMSeeker : MissileSeeker
{
	private struct RadarReturn
	{
		public readonly Unit emitter;
	}

	[SerializeField]
	private float maxTargetAngle;

	[SerializeField]
	private float inertialDrift;

	[SerializeField]
	private float guidanceDelay = 2f;

	[SerializeField]
	private float finDelay = 0.5f;

	[SerializeField]
	private float tangibleDelay = 2f;

	[SerializeField]
	private float maxTargetSpeed = 100f;

	[SerializeField]
	private float selfDestructAtSpeed = 200f;

	[SerializeField]
	private float loftAmount = 0.2f;

	[SerializeField]
	private JinkEvasion jinkEvasion;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 targetDrift;

	private List<Radar> recentReturns;

	private float lastEvaluation;

	private float lastLOSCheck;

	private float targetDist;

	private float timeToTarget;

	private Radar targetedRadar;

	private bool guidance;

	private bool finsDeployed;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		recentReturns = new List<Radar>();
		lastEvaluation = Time.timeSinceLevelLoad;
		knownPos = ((target != null && missile.NetworkHQ.TryGetKnownPosition(target, out var knownPosition)) ? knownPosition : (missile.GlobalPosition() + base.transform.forward * 100000f));
		knownVel = ((target != null && target.rb != null && missile.NetworkHQ.IsTargetBeingTracked(target)) ? target.rb.velocity : Vector3.zero);
		if (target != null)
		{
			targetedRadar = target.radar as Radar;
		}
		if (missile.NetworkHQ != null)
		{
			missile.onRadarPing += ARMSeeker_OnRadarPing;
		}
		if (targetedRadar != null && proximityFuse)
		{
			missile.SetProxyFuse(targetedRadar.GetScanPoint(), targetedRadar.GetAttachedUnit().rb);
		}
		missile.SetAimpoint(missile.GlobalPosition() + base.transform.forward * 100000f, Vector3.zero);
		this.StartSlowUpdateDelayed(1f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (!missile.disabled)
		{
			if (!missile.EngineOn() && (missile.LosingGround() || missile.MissedTarget() || missile.speed < selfDestructAtSpeed))
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
			if (loftAmount > 0f)
			{
				Vector3 vector = knownPos - missile.GlobalPosition();
				float a = Vector3.Dot(vector.normalized, missile.rb.velocity);
				targetDist = vector.magnitude;
				timeToTarget = targetDist / Mathf.Max(a, 10f);
			}
		}
	}

	public override string GetSeekerType()
	{
		return "ARAD";
	}

	public Radar TrackCurrentTarget()
	{
		if (targetedRadar == null || !targetedRadar.activated)
		{
			return null;
		}
		if (Time.timeSinceLevelLoad - lastLOSCheck > 0.25f)
		{
			lastLOSCheck = Time.timeSinceLevelLoad;
			if (!targetedRadar.GetAttachedUnit().LineOfSight(base.transform.position, 10000f))
			{
				return null;
			}
		}
		return targetedRadar;
	}

	public override void Seek()
	{
		if (!missile.LocalSim)
		{
			return;
		}
		if (!finsDeployed && missile.timeSinceSpawn > finDelay)
		{
			finsDeployed = true;
			missile.DeployFins();
		}
		if (!missile.IsTangible() && missile.timeSinceSpawn > tangibleDelay)
		{
			missile.SetTangible(tangible: true);
		}
		if (!guidance && missile.timeSinceSpawn > guidanceDelay)
		{
			guidance = true;
		}
		Radar radar = targetedRadar;
		targetedRadar = TrackCurrentTarget();
		if (Time.timeSinceLevelLoad - lastEvaluation > 4.5f && missile.NetworkHQ != null)
		{
			if (targetedRadar == null)
			{
				targetedRadar = EvaluateRadarSources();
			}
			recentReturns.Clear();
		}
		if (targetedRadar == null)
		{
			targetDrift += Random.insideUnitSphere * (inertialDrift * Time.deltaTime / 2f);
			missile.SetTarget(null);
		}
		else
		{
			if (targetedRadar != radar)
			{
				missile.SetTarget((targetedRadar != null) ? targetedRadar.GetAttachedUnit() : null);
				if (targetedRadar != null && proximityFuse)
				{
					missile.SetProxyFuse(targetedRadar.GetScanPoint(), targetedRadar.GetAttachedUnit().rb);
				}
			}
			Transform scanPoint = targetedRadar.GetScanPoint();
			knownPos = scanPoint.position.ToGlobalPosition();
			knownVel = targetedRadar.GetVelocity();
			targetDrift = Vector3.zero;
		}
		if (guidance)
		{
			Vector3 targetVel = ((maxTargetSpeed < 1000f) ? Vector3.ClampMagnitude(knownVel, maxTargetSpeed) : knownVel);
			Vector3 leadVector = TargetCalc.GetLeadVector(knownPos, missile.GlobalPosition(), targetVel, missile.rb.velocity, 10f);
			if (loftAmount > 0f && targetedRadar != null)
			{
				float num = Mathf.Min(timeToTarget * timeToTarget * 4.905f * loftAmount, targetDist * loftAmount);
				leadVector += num * Vector3.up;
				timeToTarget -= Time.fixedDeltaTime;
			}
			if (jinkEvasion.amount > 0f)
			{
				leadVector += jinkEvasion.ApplyJink(base.transform.GlobalPosition(), knownPos, missile.speed, targetDist);
			}
			missile.SetAimpoint(knownPos + targetDrift + leadVector, knownVel);
		}
	}

	public Radar EvaluateRadarSources()
	{
		lastEvaluation = Time.timeSinceLevelLoad;
		Radar result = null;
		float num = 0f;
		if (missile.targetID.IsValid)
		{
			TrackingInfo trackingData = missile.NetworkHQ.GetTrackingData(missile.targetID);
			if (trackingData != null)
			{
				trackingData.missileAttacks--;
			}
		}
		foreach (Radar recentReturn in recentReturns)
		{
			if (recentReturn == null || !recentReturn.activated)
			{
				continue;
			}
			float num2 = Vector3.Distance(recentReturn.transform.position, missile.transform.position);
			float num3 = Vector3.Angle(recentReturn.transform.position - missile.transform.position, knownPos - missile.GlobalPosition());
			if (!(num3 > maxTargetAngle))
			{
				float num4 = 10000f / (num2 * (num3 + 1f));
				TrackingInfo trackingData2 = missile.NetworkHQ.GetTrackingData(recentReturn.GetAttachedUnit().persistentID);
				if (trackingData2 != null)
				{
					num4 /= (float)(trackingData2.missileAttacks + 1);
				}
				if (num4 > num)
				{
					num = num4;
					result = recentReturn;
				}
			}
		}
		if (missile.targetID.IsValid)
		{
			TrackingInfo trackingData3 = missile.NetworkHQ.GetTrackingData(missile.targetID);
			if (trackingData3 != null)
			{
				trackingData3.missileAttacks++;
			}
		}
		return result;
	}

	public void ARMSeeker_OnRadarPing(Aircraft.OnRadarWarning source)
	{
		Vector3 vector = ((source.emitter.rb != null) ? source.emitter.rb.velocity : Vector3.zero);
		if (Vector3.Angle(source.emitter.transform.position - base.transform.position, missile.rb.velocity - vector) < maxTargetAngle && !recentReturns.Contains(source.radar))
		{
			recentReturns.Add(source.radar);
		}
	}
}
