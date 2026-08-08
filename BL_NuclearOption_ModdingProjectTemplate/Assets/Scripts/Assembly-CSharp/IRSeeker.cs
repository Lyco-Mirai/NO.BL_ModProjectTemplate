using UnityEngine;

public class IRSeeker : MissileSeeker
{
	[SerializeField]
	private float flareRejection;

	private IRSource IRTarget;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 knownVelPrev;

	private Vector3 knownAccel;

	private Vector3 driftError;

	private Vector3 errorOffset;

	[SerializeField]
	private float positionalError;

	[SerializeField]
	private float driftRate;

	[SerializeField]
	private float guidanceDelay = 0.25f;

	[SerializeField]
	private float tangibleDelay = 0.25f;

	[SerializeField]
	private float selfDestructAtSpeed = 200f;

	[SerializeField]
	private float maxLead = 5f;

	[SerializeField]
	private AnimationCurve rangeFactor;

	private float dazzleAmount;

	private float lastEvaluated;

	private float topSpeed;

	private bool guidance;

	private bool achievedLock;

	private bool targetOnLaunch;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		targetUnit = target;
		errorOffset = Random.insideUnitSphere;
		missile.NetworkseekerMode = Missile.SeekerMode.passive;
		topSpeed = missile.GetWeaponInfo().GetMaxSpeed();
		GlobalPosition? globalPosition = ((targetUnit != null) ? missile.NetworkHQ.GetKnownPosition(targetUnit) : ((GlobalPosition?)null));
		knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
		missile.SetAimpoint(knownPos, Vector3.zero);
		if (proximityFuse && target != null)
		{
			missile.SetProxyFuse(target.GetRandomPart().transform, target.rb);
		}
		if (targetUnit == null || !targetUnit.HasIRSignature() || !globalPosition.HasValue || FastMath.OutOfRange(targetUnit.GlobalPosition(), globalPosition.Value, 500f) || !targetUnit.LineOfSight(base.transform.position, 1000f))
		{
			LoseLock();
		}
		else
		{
			targetOnLaunch = true;
			IRTarget = targetUnit.GetIRSource();
			if (IRTarget == null || IRTarget.flare)
			{
				LoseLock();
			}
			else
			{
				targetUnit.onAddIRSource += IRSeeker_OnTargetFlare;
			}
		}
		lastEvaluated = Time.timeSinceLevelLoad;
		missile.onDisableUnit += IRSeeker_OnMissileDestroyed;
		this.StartSlowUpdateDelayed(0.5f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (!missile.disabled && !missile.EngineOn() && (missile.LosingGround() || missile.MissedTarget() || missile.speed < selfDestructAtSpeed || (targetOnLaunch && targetUnit == null)))
		{
			missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
		}
	}

	public override string GetSeekerType()
	{
		return "IR";
	}

	public override void Seek()
	{
		if (!missile.IsTangible() && missile.timeSinceSpawn > tangibleDelay && (missile.owner == null || (FastMath.OutOfRange(missile.owner.GlobalPosition(), missile.GlobalPosition(), 50f) && Vector3.Dot(missile.owner.GlobalPosition() - missile.GlobalPosition(), missile.rb.velocity) < 0f)))
		{
			missile.SetTangible(tangible: true);
		}
		if (!guidance && missile.timeSinceSpawn > guidanceDelay)
		{
			missile.DeployFins();
			guidance = true;
		}
		if (Time.timeSinceLevelLoad - lastEvaluated > 0.25f && !IRLockCheck())
		{
			IRTarget = null;
		}
		if (guidance)
		{
			if (IRTarget != null && IRTarget.transform != null)
			{
				knownPos = IRTarget.transform.GlobalPosition();
				knownVel = ((targetUnit != null && targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
				knownAccel = (knownVel - knownVelPrev) / Time.fixedDeltaTime;
				knownVelPrev = knownVel;
				driftError = Vector3.zero;
			}
			else
			{
				driftError += Random.insideUnitSphere * (driftRate * Time.deltaTime / 2f);
			}
		}
		float num = maxLead;
		Vector3 platformVel = FastMath.NormalizedDirection(missile.GlobalPosition(), knownPos) * ((missile.timeSinceSpawn < 3f) ? topSpeed : missile.speed);
		Vector3 leadVectorWithAccel = TargetCalc.GetLeadVectorWithAccel(knownPos, missile.GlobalPosition(), knownVel, platformVel, knownAccel, num);
		leadVectorWithAccel += driftError + errorOffset * (dazzleAmount + positionalError);
		GlobalPosition globalPosition = knownPos + leadVectorWithAccel;
		globalPosition.y = Mathf.Max(globalPosition.y, 0f);
		if (PlayerSettings.debugVis)
		{
			GameObject obj = Object.Instantiate(GameAssets.i.debugArrowGreen, missile.transform);
			obj.transform.rotation = Quaternion.LookRotation(globalPosition - missile.GlobalPosition());
			obj.transform.localScale = new Vector3(1f, 1f, (globalPosition - missile.GlobalPosition()).magnitude);
			Object.Destroy(obj, 0.05f);
		}
		missile.SetAimpoint(globalPosition, knownVel);
	}

	private bool IRLockCheck()
	{
		lastEvaluated = Time.timeSinceLevelLoad;
		if (IRTarget == null || IRTarget.transform == null)
		{
			return false;
		}
		if (Physics.Linecast(base.transform.position, IRTarget.transform.position, out var _, PhysicsLayers.StaticsMask))
		{
			LoseLock();
			IRTarget = null;
			return false;
		}
		if (!achievedLock && targetUnit is Aircraft aircraft)
		{
			aircraft.RecordDamage(missile.ownerID, 0.001f);
			achievedLock = true;
		}
		return true;
	}

	private void IRSeeker_OnTargetFlare(IRSource source)
	{
		if (!missile.targetID.NotValid)
		{
			float targetDistance = FastMath.Distance(base.transform.position, IRTarget.transform.position);
			float num = RangeCoef(targetDistance);
			Vector3 vector = FastMath.NormalizedDirection(base.transform.position, IRTarget.transform.position);
			Vector3 rhs = FastMath.NormalizedDirection(source.transform.position, IRTarget.transform.position);
			float num2 = Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(vector, rhs)));
			float num3 = AspectCoef(vector);
			float num4 = Mathf.Clamp01(BackgroundBrightness(vector)) * 2f;
			float num5 = IRTarget.intensity * (1f + num3) / (num + num4);
			dazzleAmount += (1f + num2) / flareRejection;
			if (dazzleAmount > num5)
			{
				LoseLock();
				IRTarget = source;
			}
		}
	}

	private float AspectCoef(Vector3 targetVector)
	{
		float num = Mathf.Clamp01(Vector3.Dot(-IRTarget.transform.forward, targetVector));
		float num2 = Mathf.Clamp01(Vector3.Dot(IRTarget.transform.forward, targetVector));
		return num * 0.5f + num2 * 2f;
	}

	private float RangeCoef(float targetDistance)
	{
		float maxRange = missile.GetWeaponInfo().targetRequirements.maxRange;
		float time = targetDistance / maxRange;
		return rangeFactor.Evaluate(time);
	}

	private float BackgroundBrightness(Vector3 targetVector)
	{
		float cloudOcclusion = NetworkSceneSingleton<LevelInfo>.i.GetCloudOcclusion(base.transform.position);
		float b = NetworkSceneSingleton<LevelInfo>.i.sun.color.b;
		return Mathf.Clamp01(Vector3.Dot(targetVector, -NetworkSceneSingleton<LevelInfo>.i.sun.transform.forward)) * (1f - cloudOcclusion) * b;
	}

	private void LoseLock()
	{
		if (targetUnit != null)
		{
			targetUnit.onAddIRSource -= IRSeeker_OnTargetFlare;
		}
		if (!missile.disabled)
		{
			missile.SetTarget(null);
		}
	}

	private void IRSeeker_OnMissileDestroyed(Unit unit)
	{
		LoseLock();
	}
}
