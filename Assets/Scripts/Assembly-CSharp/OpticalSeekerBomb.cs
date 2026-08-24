using UnityEngine;

public class OpticalSeekerBomb : MissileSeeker
{
	[SerializeField]
	private float searchRadius;

	[SerializeField]
	private float maxTargetSpeed = 30f;

	[SerializeField]
	private float tangibleDelay = 0.25f;

	[SerializeField]
	private float guidanceDelay = 0.5f;

	[SerializeField]
	private float finDelay = 0.2f;

	[SerializeField]
	private float altitudeFuseHeight;

	[SerializeField]
	private float armDelay;

	private GlobalPosition knownPos;

	private GlobalPosition aimPos;

	private Vector3 knownVel;

	private Vector3 measuredWind;

	private bool hasVisual;

	private bool guidance;

	private bool deployedFins;

	private bool armTriggered;

	private float lastVisualCheck;

	private float timeToTarget;

	private float trajectoryError = 1f;

	private float airburstHeight;

	private Transform targetTransform;

	private GameObject aimpointDebug;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		float num = Kinematics.FallTime(missile.GlobalPosition().y, missile.rb.velocity.y);
		Vector3 vector = new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z);
		aimPos = missile.GlobalPosition() + vector * num;
		aimPos.y = 0f;
		knownPos = aimPos;
		missile.NetworkseekerMode = Missile.SeekerMode.passive;
		airburstHeight = missile.GetWeaponInfo().airburstHeight;
		if (UnitRegistry.TryGetUnit(missile.targetID, out targetUnit))
		{
			targetTransform = ((targetUnit.maxRadius > 20f) ? target.GetRandomPart() : targetUnit.transform);
			if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				knownPos = knownPosition;
			}
		}
		this.StartSlowUpdateDelayed(0.5f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (!missile.disabled)
		{
			if (!missile.IsTangible() && missile.timeSinceSpawn > tangibleDelay)
			{
				missile.SetTangible(tangible: true);
			}
			if (missile.IsArmed() && missile.speed < 30f && missile.radarAlt < 30f)
			{
				missile.Detonate(Vector3.up, hitArmor: false, hitTerrain: true);
			}
			missile.UpdateRadarAlt();
			measuredWind = NetworkSceneSingleton<LevelInfo>.i.GetWind();
			Vector3 vector = knownPos - missile.GlobalPosition();
			float num = Kinematics.FallTime(0f - vector.y, missile.rb.velocity.y);
			Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
			float magnitude = vector2.magnitude;
			float a = Vector3.Dot(vector2.normalized, new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z));
			timeToTarget = Mathf.Max(magnitude, 10f) / Mathf.Max(a, 10f);
			trajectoryError = Mathf.Clamp(timeToTarget / num, 0.5f, 1.5f);
		}
	}

	public override string GetSeekerType()
	{
		return "Optical";
	}

	private bool TrackVisual()
	{
		lastVisualCheck = Time.timeSinceLevelLoad;
		if (FastMath.InRange(targetUnit.GlobalPosition(), knownPos, searchRadius + targetUnit.maxRadius))
		{
			return targetUnit.LineOfSight(base.transform.position, 1000f);
		}
		return false;
	}

	private void GetTargetParameters()
	{
		if (!(targetUnit == null) && !(targetTransform == null))
		{
			if (Time.timeSinceLevelLoad - lastVisualCheck > 0.25f)
			{
				hasVisual = TrackVisual();
			}
			if (hasVisual)
			{
				knownPos = targetTransform.GlobalPosition();
				knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
			}
			else
			{
				knownPos += knownVel * Time.fixedDeltaTime;
			}
		}
	}

	private void SendTargetInfo()
	{
		if (hasVisual && missile.targetID.NotValid)
		{
			missile.SetTarget(targetUnit);
		}
		if (!hasVisual && missile.targetID.IsValid)
		{
			missile.SetTarget(null);
		}
		if (!guidance)
		{
			return;
		}
		GlobalPosition ballisticAimPoint = Kinematics.GetBallisticAimPoint(missile, knownPos, timeToTarget, maxTargetSpeed, trajectoryError, knownVel);
		if (airburstHeight > 0f)
		{
			float num = armDelay - missile.timeSinceSpawn;
			if (timeToTarget > num)
			{
				ballisticAimPoint += Vector3.up * airburstHeight;
			}
		}
		ballisticAimPoint -= timeToTarget * 0.5f * measuredWind;
		timeToTarget -= Time.fixedDeltaTime;
		if (altitudeFuseHeight > 0f)
		{
			missile.UpdateRadarAlt();
		}
		if (!armTriggered && missile.timeSinceSpawn > armDelay && (altitudeFuseHeight == 0f || missile.radarAlt < altitudeFuseHeight))
		{
			missile.Arm();
			armTriggered = true;
		}
		if (armTriggered && altitudeFuseHeight > 0f && missile.radarAlt < altitudeFuseHeight)
		{
			missile.Detonate(Vector3.up, hitArmor: false, missile.radarAlt < 2f);
		}
		missile.SetAimpoint(ballisticAimPoint, knownVel);
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
}
