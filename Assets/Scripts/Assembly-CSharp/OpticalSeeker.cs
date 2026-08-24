using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class OpticalSeeker : MissileSeeker
{
	[SerializeField]
	private bool useDatalink;

	[SerializeField]
	private bool terrainAvoidance;

	[SerializeField]
	private bool aimVelocity;

	[SerializeField]
	private float searchRadius;

	[SerializeField]
	private float loftAmount;

	[SerializeField]
	private float topAttackAngle;

	[SerializeField]
	private float magnification = 1000f;

	[SerializeField]
	private float searchAngle = 90f;

	[SerializeField]
	private float maxTargetSpeed = 30f;

	[SerializeField]
	private float selfDestructAtSpeed = 200f;

	[SerializeField]
	private float tangibleDelay = 0.25f;

	[SerializeField]
	private float guidanceDelay = 0.5f;

	[SerializeField]
	private float finDelay = 0.2f;

	[SerializeField]
	private float armDelay;

	[SerializeField]
	private float timeFuse;

	[SerializeField]
	private float aimVelocitySpeed = 300f;

	[SerializeField]
	private JinkEvasion jinkEvasion;

	[SerializeField]
	private TopAttack topAttack;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 measuredWind;

	private bool hasVisual;

	private bool guidance;

	private bool deployedFins;

	private bool targetObstructed;

	private bool tooFast;

	private bool armTriggered;

	private float lastOpticalCheck;

	private float timeToTarget;

	private float targetDist;

	private float topSpeed;

	private Transform targetTransform;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		knownPos = aimpoint;
		topSpeed = missile.GetTopSpeed(0f, 0f);
		timeToTarget = 0f;
		if (UnitRegistry.TryGetUnit(missile.targetID, out target))
		{
			lastOpticalCheck = -100f;
			targetUnit = target;
			targetTransform = ((targetUnit.maxRadius > 20f) ? target.GetRandomPart() : targetUnit.transform);
			if (proximityFuse)
			{
				missile.SetProxyFuse(targetTransform, target.rb);
			}
			if (!missile.NetworkHQ.TryGetKnownPosition(target, out knownPos))
			{
				knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
			}
			knownVel = ((target.rb != null) ? target.rb.velocity : Vector3.zero);
			if (topAttack.Amount > 0f && (!topAttack.ShouldUseTopAttack(target) || FastMath.InRange(knownPos, missile.GlobalPosition(), topAttack.TooCloseRange)))
			{
				topAttack.Amount = 0f;
			}
			missile.NetworkseekerMode = Missile.SeekerMode.passive;
		}
		else
		{
			knownPos = missile.GlobalPosition() + missile.transform.forward * 100000f;
			knownVel = Vector3.zero;
		}
		if (timeFuse > 0f)
		{
			TimeFuse().Forget();
		}
		missile.SetAimpoint(knownPos, knownVel);
		CalcDistAndTimeToTarget();
		this.StartSlowUpdate(1f, SlowChecks);
	}

	private async UniTask TimeFuse()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(1000f * timeFuse));
		if (!cancel.IsCancellationRequested && !(missile == null) && !missile.disabled)
		{
			missile.Arm();
			missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
		}
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
		}
		measuredWind = NetworkSceneSingleton<LevelInfo>.i.GetWind();
		if (!missile.EngineOn() && missile.timeSinceSpawn > 1f)
		{
			if (missile.LosingGround() || missile.MissedTarget() || (missile.speed < selfDestructAtSpeed && missile.timeSinceSpawn > 5f))
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
			if (timeToTarget < 5f)
			{
				loftAmount = 1f;
			}
		}
		if (loftAmount > 0f && targetUnit != null && missile.timeSinceSpawn >= guidanceDelay)
		{
			CalcDistAndTimeToTarget();
		}
	}

	private void CalcDistAndTimeToTarget()
	{
		Vector3 vector = knownPos - missile.GlobalPosition();
		vector.y = 0f;
		targetDist = vector.magnitude;
		float a = Vector3.Dot(vector.normalized, missile.rb.velocity);
		timeToTarget = targetDist / Mathf.Max(a, selfDestructAtSpeed);
	}

	public override string GetSeekerType()
	{
		return "Optical";
	}

	private void OpticalCheck()
	{
		lastOpticalCheck = Time.timeSinceLevelLoad;
		if (!hasVisual && useDatalink && missile.NetworkHQ.IsTargetBeingTracked(targetUnit))
		{
			knownPos = targetUnit.GlobalPosition();
		}
		bool flag = FastMath.InRange(targetUnit.GlobalPosition(), knownPos, searchRadius + targetUnit.maxRadius);
		if (!flag)
		{
			targetObstructed = false;
			hasVisual = false;
		}
		else
		{
			bool flag2 = targetUnit.LineOfSight(missile.transform.position, 1000f);
			hasVisual = flag && flag2 && Vector3.Angle(targetUnit.transform.position - base.transform.position, base.transform.forward) < searchAngle;
			targetObstructed = !flag2;
		}
	}

	private void GetTargetParameters()
	{
		if (!(targetUnit == null) && !(targetTransform == null))
		{
			if (Time.timeSinceLevelLoad - lastOpticalCheck > 0.25f)
			{
				OpticalCheck();
				missile.SetTarget(hasVisual ? targetUnit : null);
			}
			if (hasVisual)
			{
				TargetCalc.GetLeadFromMaxTargetSpeed(targetUnit, targetTransform, base.transform, knownPos, maxTargetSpeed, out knownPos, out knownVel);
			}
			else
			{
				knownPos += knownVel * Time.fixedDeltaTime;
			}
		}
	}

	private void SendTargetInfo()
	{
		if (!guidance)
		{
			return;
		}
		Vector3 platformVel = ((missile.timeSinceSpawn < 3f) ? (missile.transform.forward * topSpeed) : missile.rb.velocity);
		Vector3 leadVector = TargetCalc.GetLeadVector(knownPos, missile.GlobalPosition(), knownVel, platformVel, 10f);
		if (loftAmount > 0f && targetUnit != null && !topAttack.Active)
		{
			float a = Mathf.Min(timeToTarget * timeToTarget * 4.905f * loftAmount, targetDist * loftAmount);
			float b = Mathf.Min(timeToTarget * timeToTarget, 25f) * 4.905f;
			leadVector += Mathf.Max(a, b) * Vector3.up;
			timeToTarget -= Time.fixedDeltaTime;
		}
		if (hasVisual && jinkEvasion.amount > 0f)
		{
			leadVector += jinkEvasion.ApplyJink(base.transform.GlobalPosition(), knownPos, missile.speed, targetDist);
		}
		if (topAttack.Amount > 0f)
		{
			leadVector += topAttack.ApplyTopAttack(missile.GlobalPosition(), knownPos, missile.speed);
		}
		leadVector -= timeToTarget * 0.5f * measuredWind;
		if (terrainAvoidance && targetObstructed)
		{
			Vector3 vector = FastMath.Direction(missile.GlobalPosition(), knownPos);
			vector.y = 0f;
			missile.SetAimpoint(knownPos + leadVector + vector.magnitude * (0.25f * Vector3.up), knownVel);
			return;
		}
		if (!armTriggered && missile.timeSinceSpawn > armDelay)
		{
			missile.Arm();
			armTriggered = true;
		}
		missile.SetAimpoint(knownPos + leadVector, knownVel);
	}

	public override void Seek()
	{
		GetTargetParameters();
		SendTargetInfo();
		if (!deployedFins && missile.timeSinceSpawn > finDelay)
		{
			missile.DeployFins();
			deployedFins = true;
		}
		if (!missile.IsTangible() && missile.timeSinceSpawn > tangibleDelay)
		{
			missile.SetTangible(tangible: true);
		}
		if (!guidance && missile.timeSinceSpawn > guidanceDelay)
		{
			guidance = true;
		}
	}

	public override float GetMinSpeed()
	{
		return selfDestructAtSpeed;
	}
}
