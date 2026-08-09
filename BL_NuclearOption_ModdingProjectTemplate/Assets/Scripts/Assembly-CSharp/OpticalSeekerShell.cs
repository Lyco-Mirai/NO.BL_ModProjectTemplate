using UnityEngine;

public class OpticalSeekerShell : MissileSeeker
{
	[SerializeField]
	private float searchRadius = 50f;

	[SerializeField]
	private float maxTargetSpeed = 30f;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 measuredWind;

	private bool hasVisual;

	private float lastVisualCheck;

	private float timeToTarget;

	private float trajectoryError = 1f;

	private Transform targetTransform;

	private GameObject aimpointDebug;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		float num = Kinematics.FallTime(missile.GlobalPosition().y, missile.rb.velocity.y);
		Vector3 vector = new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z);
		GlobalPosition globalPosition = missile.GlobalPosition() + vector * num;
		globalPosition.y = 0f;
		knownPos = globalPosition;
		base.transform.rotation = Quaternion.LookRotation(missile.rb.velocity);
		missile.DeployFins();
		missile.NetworkseekerMode = Missile.SeekerMode.passive;
		if (UnitRegistry.TryGetUnit(missile.targetID, out targetUnit))
		{
			targetTransform = target.GetRandomPart();
			if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				knownPos = knownPosition;
			}
		}
		if (PlayerSettings.debugVis)
		{
			aimpointDebug = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
			aimpointDebug.transform.localPosition = knownPos.AsVector3();
			aimpointDebug.transform.localScale = Vector3.one * 3f;
		}
		this.StartSlowUpdateDelayed(0.5f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (!missile.disabled)
		{
			if (missile.speed < 1f && missile.IsArmed())
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: true);
			}
			if (!missile.IsTangible() && missile.timeSinceSpawn > 0.9f)
			{
				missile.SetTangible(tangible: true);
			}
			measuredWind = NetworkSceneSingleton<LevelInfo>.i.GetWind();
			missile.UpdateRadarAlt();
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
		GlobalPosition ballisticAimPoint = Kinematics.GetBallisticAimPoint(missile, knownPos, timeToTarget, maxTargetSpeed, trajectoryError, knownVel);
		ballisticAimPoint -= timeToTarget * 0.5f * measuredWind;
		if (PlayerSettings.debugVis && aimpointDebug != null)
		{
			aimpointDebug.transform.localPosition = ballisticAimPoint.AsVector3();
		}
		timeToTarget -= Time.fixedDeltaTime;
		if (!missile.IsTangible() && missile.owner != null && !FastMath.InRange(missile.owner.GlobalPosition(), missile.GlobalPosition(), 15f))
		{
			missile.SetTangible(tangible: true);
		}
		missile.SetAimpoint(ballisticAimPoint, knownVel);
	}

	public override void Seek()
	{
		GetTargetParameters();
		SendTargetInfo();
	}
}
