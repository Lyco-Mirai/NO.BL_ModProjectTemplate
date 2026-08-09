using System.Collections.Generic;
using UnityEngine;

public class LaserSeeker : MissileSeeker
{
	[SerializeField]
	private float errorRate = 5f;

	[SerializeField]
	private float maxSeekerAngle = 10f;

	[SerializeField]
	private float maxTargetSpeed = 30f;

	[SerializeField]
	private float selfDestructAtSpeed = 200f;

	[SerializeField]
	private float guidanceDelay = 0.5f;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 positionError;

	private Vector3 positionErrorChange;

	private bool hasLock;

	private float lastTrack;

	private float timeToTarget;

	private float targetDist;

	private float topSpeed;

	private LaserDesignator laserDesignator;

	private List<Unit> targetList;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		if (missile.owner is Aircraft aircraft)
		{
			laserDesignator = aircraft.GetLaserDesignator();
			targetList = aircraft.weaponManager.GetTargetList();
		}
		topSpeed = missile.GetWeaponInfo().GetMaxSpeed();
		positionErrorChange = Random.insideUnitSphere * errorRate;
		knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
		if (UnitRegistry.TryGetUnit(missile.targetID, out target) && missile.NetworkHQ != null && missile.NetworkHQ.IsTargetLased(target))
		{
			lastTrack = -100f;
			targetUnit = target;
			missile.NetworkseekerMode = Missile.SeekerMode.passive;
		}
		else if (missile.NetworkHQ != null)
		{
			if (missile.NetworkHQ.TryGetLasedTargetInView(base.transform, maxSeekerAngle, 15000f, out var lasedTarget))
			{
				targetUnit = lasedTarget;
			}
			lastTrack = -100f;
			missile.NetworkseekerMode = Missile.SeekerMode.passive;
		}
		missile.DeployFins();
		missile.SetAimpoint(knownPos, knownVel);
		this.StartSlowUpdateDelayed(1f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (!missile.disabled)
		{
			if (!missile.IsTangible() && missile.timeSinceSpawn > 0.9f)
			{
				missile.SetTangible(tangible: true);
			}
			if (!missile.EngineOn() && (missile.LosingGround() || missile.MissedTarget() || missile.speed < selfDestructAtSpeed))
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
		}
	}

	public override string GetSeekerType()
	{
		return "Laser";
	}

	private bool TrackLaser()
	{
		lastTrack = Time.timeSinceLevelLoad;
		if (missile.NetworkHQ == null || !missile.NetworkHQ.IsTargetLased(targetUnit))
		{
			return false;
		}
		if (Vector3.Angle(targetUnit.transform.position - base.transform.position, base.transform.forward) > maxSeekerAngle)
		{
			return false;
		}
		if (!targetUnit.LineOfSight(missile.transform.position, 1000f))
		{
			return false;
		}
		return true;
	}

	private void GetTargetParameters()
	{
		if (targetUnit == null)
		{
			positionError += positionErrorChange * Time.fixedDeltaTime;
			return;
		}
		if (Time.timeSinceLevelLoad - lastTrack > 0.1f)
		{
			hasLock = TrackLaser();
		}
		if (hasLock)
		{
			positionError = Vector3.zero;
			TargetCalc.GetLeadFromMaxTargetSpeed(targetUnit, targetUnit.transform, base.transform, knownPos, maxTargetSpeed, out knownPos, out knownVel);
		}
		else
		{
			knownPos += knownVel * Time.fixedDeltaTime;
			positionError += positionErrorChange * Time.fixedDeltaTime;
		}
	}

	private void SendTargetInfo()
	{
		if (hasLock && missile.targetID != targetUnit.persistentID)
		{
			missile.SetTarget(targetUnit);
		}
		if (!hasLock && missile.targetID.IsValid)
		{
			missile.SetTarget(null);
		}
		if (!(missile.timeSinceSpawn < guidanceDelay))
		{
			Vector3 targetVel = knownVel;
			Vector3 platformVel = (missile.EngineOn() ? (missile.transform.forward * topSpeed) : missile.rb.velocity);
			Vector3 leadVector = TargetCalc.GetLeadVector(knownPos, missile.GlobalPosition(), targetVel, platformVel, 10f);
			if (targetUnit != null)
			{
				targetDist = FastMath.Distance(knownPos, missile.GlobalPosition());
				float a = (missile.EngineOn() ? topSpeed : missile.speed);
				timeToTarget = targetDist / Mathf.Max(a, 10f);
				float num = Mathf.Min(timeToTarget * timeToTarget, 4f) * 4.905f;
				leadVector += num * Vector3.up;
			}
			missile.SetAimpoint(knownPos + positionError + leadVector, knownVel);
		}
	}

	public override void Seek()
	{
		GetTargetParameters();
		SendTargetInfo();
		if (!missile.IsTangible() && missile.owner != null && !FastMath.InRange(missile.GlobalPosition(), missile.owner.GlobalPosition(), 20f))
		{
			missile.SetTangible(tangible: true);
		}
	}
}
