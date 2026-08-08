using UnityEngine;

public class SARHSeeker : MissileSeeker
{
	[SerializeField]
	private float lockPersistence = 2f;

	[SerializeField]
	private float tangibleDelay = 2f;

	[SerializeField]
	private float armDelay = 2f;

	[SerializeField]
	private float guidanceDelay;

	[SerializeField]
	private float selfDestructAtSpeed = 200f;

	[SerializeField]
	private float seekerAngle = 45f;

	private RadarParams radarParams;

	[Range(0f, 1f)]
	[SerializeField]
	private float jamTolerance;

	private Radar radarSource;

	private Transform radarSourcePoint;

	private Transform targetTransform;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 knownVelPrev;

	private Vector3 knownAccel;

	private float lastTrackingCheck;

	private float timeWithoutTrack;

	private float topSpeed;

	private float trackingStrength;

	private float jamAccumulation;

	private bool isJammed;

	private bool guidance;

	private Unit detectorUnit;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		Unit unit = null;
		if (missile.owner != null)
		{
			unit = missile.owner.radar.GetAttachedUnit();
		}
		topSpeed = missile.GetWeaponInfo().maxSpeed;
		if (unit != null)
		{
			radarSource = unit.radar as Radar;
			missile.radar = radarSource;
			radarParams = radarSource.RadarParameters;
			detectorUnit = missile.owner;
			if (radarSource != null)
			{
				radarSourcePoint = radarSource.GetScanPoint();
				detectorUnit = radarSource.GetAttachedUnit();
				radarSource.AddGuidedMissile(missile);
			}
			missile.onJam += SARHSeeker_OnJam;
			missile.onDisableUnit += SARHSeeker_OnDisable;
			targetUnit = target;
			if (target is IRadarReturn)
			{
				target.onAddRadarChaff += SARHSeeker_OnChaff;
			}
			if (targetUnit != null && missile.NetworkHQ != null && missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				knownPos = knownPosition;
				missile.SetAimpoint(knownPos, Vector3.zero);
			}
			lastTrackingCheck = Time.timeSinceLevelLoad;
			targetTransform = ((target != null) ? target.GetRandomPart() : null);
			if (proximityFuse && targetTransform != null)
			{
				missile.SetProxyFuse(targetTransform, target.rb);
			}
			if (guidanceDelay > 0f)
			{
				knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
				missile.SetAimpoint(knownPos, Vector3.zero);
			}
			missile.DeployFins();
			this.StartSlowUpdateDelayed(1f, SlowChecks);
		}
	}

	public override GlobalPosition GetEvasionPoint()
	{
		if (missile.radar != null)
		{
			return missile.radar.GetScanPoint().GlobalPosition();
		}
		return missile.GlobalPosition();
	}

	public override string GetSeekerType()
	{
		return "SARH";
	}

	private void SlowChecks()
	{
		if (missile.disabled)
		{
			return;
		}
		if (!missile.IsTangible() && missile.timeSinceSpawn > tangibleDelay)
		{
			missile.SetTangible(tangible: true);
			if (radarSource != null)
			{
				missile.RpcAssignRadar(radarSource.GetAttachedUnit());
			}
		}
		if (!missile.EngineOn() && missile.IsArmed() && (missile.LosingGround() || missile.MissedTarget() || missile.speed < selfDestructAtSpeed))
		{
			missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
		}
	}

	private void SARHSeeker_OnJam(Unit.JamEventArgs e)
	{
		if (!(Vector3.Angle(e.jammingUnit.GlobalPosition() - missile.GlobalPosition(), missile.transform.forward) > seekerAngle))
		{
			jamAccumulation += e.jamAmount / Mathf.Max(jamTolerance, 0.1f);
			missile.RecordDamage(e.jammingUnit.persistentID, 0.01f);
		}
	}

	private void SARHSeeker_OnChaff(RadarChaff source)
	{
		if (!missile.targetID.NotValid && missile.seekerMode == Missile.SeekerMode.activeLock)
		{
			float targetDistance = FastMath.Distance(base.transform.position, targetUnit.transform.position);
			float num = RangeCoef(targetDistance);
			Vector3 lhs = FastMath.NormalizedDirection(base.transform.position, targetUnit.transform.position);
			Vector3 rhs = FastMath.NormalizedDirection(source.transform.position, targetUnit.transform.position);
			float num2 = Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(lhs, rhs)));
			float num3 = num * num2 / (1f + jamTolerance);
			jamAccumulation += num3;
			Debug.Log($"Target chaff angle: {num2:F2}, range coeff: {num:F2}, dazzle: {num3:F2} jam: {jamAccumulation:F2} success {jamAccumulation > jamTolerance}");
		}
	}

	private void SARHSeeker_OnDisable(Unit unit)
	{
		radarSource.RemoveGuidedMissile(missile);
		missile.onDisableUnit -= SARHSeeker_OnDisable;
	}

	private float RangeCoef(float targetDistance)
	{
		float maxRange = radarParams.maxRange;
		float num = targetDistance / maxRange;
		return Mathf.Clamp01(1f - num);
	}

	private float GetTrackingStrength(Unit targetUnit)
	{
		if (Time.timeSinceLevelLoad - lastTrackingCheck < 0.2f)
		{
			return trackingStrength;
		}
		lastTrackingCheck = Time.timeSinceLevelLoad;
		if (targetUnit == null || !(targetUnit is IRadarReturn radarReturn))
		{
			trackingStrength = 0f;
			return 0f;
		}
		GlobalPosition globalPosition = radarSourcePoint.GlobalPosition();
		GlobalPosition globalPosition2 = targetUnit.GlobalPosition();
		Vector3 vector = globalPosition2 - globalPosition;
		vector.y = 0f;
		float num = FastMath.Distance(globalPosition, globalPosition2);
		float magnitude = vector.magnitude;
		float num2 = Mathf.Sqrt(12742000f * globalPosition.y);
		float num3 = Mathf.Sqrt(12742000f * globalPosition2.y);
		if (!(num2 + num3 > num) || !TargetCalc.LineOfSight(radarSourcePoint, targetUnit.transform, 10f))
		{
			trackingStrength = 0f;
			missile.SetTarget(null);
			return 0f;
		}
		isJammed = jamAccumulation > jamTolerance || radarSource.IsJammed();
		if (isJammed || radarSource == null || radarSourcePoint == null || detectorUnit == null || detectorUnit.disabled)
		{
			trackingStrength = 0f;
			return 0f;
		}
		float num4 = 0f;
		if (magnitude < num2 && globalPosition2.y < globalPosition.y * (1f - magnitude / num2))
		{
			float num5 = num * targetUnit.radarAlt / (globalPosition.y - globalPosition2.y);
			num4 += Mathf.Min(num, 1000f) / num5;
		}
		num4 += targetUnit.maxRadius * targetUnit.maxRadius * 2f / (targetUnit.radarAlt * targetUnit.radarAlt);
		trackingStrength = radarReturn.GetRadarReturn(radarSourcePoint.position, null, missile, num, num4, radarParams, triggerWarning: false);
		return trackingStrength;
	}

	private void SearchMode()
	{
		if (missile.seekerMode != Missile.SeekerMode.activeSearch)
		{
			missile.NetworkseekerMode = Missile.SeekerMode.activeSearch;
		}
		if (timeWithoutTrack > lockPersistence)
		{
			targetTransform = null;
			missile.SetTarget(null);
			missile.onJam -= SARHSeeker_OnJam;
		}
		else
		{
			timeWithoutTrack += Time.fixedDeltaTime;
			knownPos += knownVel * Time.fixedDeltaTime;
			knownAccel = Vector3.zero;
		}
	}

	private void LockedMode()
	{
		if (missile.seekerMode != Missile.SeekerMode.activeLock)
		{
			missile.NetworkseekerMode = Missile.SeekerMode.activeLock;
		}
		knownPos = targetTransform.GlobalPosition();
		knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
		knownAccel = (knownVel - knownVelPrev) / Time.fixedDeltaTime;
		knownVelPrev = knownVel;
	}

	private void SendTargetInfo()
	{
		Vector3 platformVel = ((missile.timeSinceSpawn < 3f) ? (missile.transform.forward * topSpeed) : missile.rb.velocity);
		Vector3 vector = (missile.IsArmed() ? TargetCalc.GetLeadVectorWithAccel(knownPos, missile.GlobalPosition(), knownVel, platformVel, knownAccel, 10f) : Vector3.zero);
		GlobalPosition aimPoint = knownPos + vector;
		aimPoint.y = Mathf.Max(aimPoint.y, 0f);
		missile.SetAimpoint(aimPoint, knownVel);
	}

	public override void Seek()
	{
		if (!missile.IsArmed() && missile.timeSinceSpawn > armDelay)
		{
			missile.Arm();
		}
		if (!guidance && missile.timeSinceSpawn > guidanceDelay)
		{
			guidance = true;
			if (targetUnit != null && missile.NetworkHQ != null && missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				knownPos = knownPosition;
			}
			return;
		}
		if (targetTransform != null && targetUnit != null)
		{
			jamAccumulation -= Mathf.Max(jamAccumulation, 0.2f) * Mathf.Max(jamTolerance, 0.1f) * Time.deltaTime;
			jamAccumulation = Mathf.Clamp01(jamAccumulation);
			if (GetTrackingStrength(targetUnit) >= radarParams.minSignal)
			{
				missile.SetTarget(targetUnit);
				timeWithoutTrack = 0f;
				LockedMode();
			}
			else
			{
				SearchMode();
			}
		}
		else
		{
			knownPos += knownVel * Time.fixedDeltaTime;
			if (targetUnit == null && missile.IsArmed())
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
		}
		SendTargetInfo();
	}
}
