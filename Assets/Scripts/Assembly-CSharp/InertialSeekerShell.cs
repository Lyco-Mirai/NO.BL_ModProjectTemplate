using UnityEngine;

public class InertialSeekerShell : MissileSeeker
{
	[SerializeField]
	private float maxTargetSpeed = 30f;

	[SerializeField]
	private float CEP = 50f;

	[SerializeField]
	private bool useDatalink = true;

	[SerializeField]
	private bool useLaser;

	private GlobalPosition knownPos;

	private GlobalPosition aimPos;

	private Vector3 knownVel;

	private Vector3 measuredWind;

	private Vector3 error;

	private float errorAccumulation;

	private float timeToTarget;

	private float trajectoryError = 1f;

	private Transform targetTransform;

	private GameObject aimpointDebug;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		float num = Kinematics.FallTime(missile.GlobalPosition().y, missile.rb.velocity.y);
		Vector3 vector = new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z);
		timeToTarget = 10f;
		aimPos = missile.GlobalPosition() + vector * num;
		aimPos.y = 0f;
		knownPos = aimPos;
		error = GaussianRandom.NormalRandomSpherized(1f) * CEP * 1.5f;
		missile.DeployFins();
		missile.NetworkseekerMode = Missile.SeekerMode.passive;
		if (UnitRegistry.TryGetUnit(missile.targetID, out targetUnit))
		{
			targetTransform = target.GetRandomPart();
			if (missile.NetworkHQ != null && missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				knownPos = knownPosition;
				knownVel = ((target.rb != null) ? target.rb.velocity : Vector3.zero);
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
		if (missile.disabled)
		{
			return;
		}
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
		Vector3 to = knownPos - missile.GlobalPosition();
		float num = Kinematics.FallTime(0f - to.y, missile.rb.velocity.y);
		Vector3 vector = new Vector3(to.x, 0f, to.z);
		float magnitude = vector.magnitude;
		float a = Vector3.Dot(vector.normalized, new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z));
		timeToTarget = Mathf.Max(magnitude, 10f) / Mathf.Max(a, 10f);
		trajectoryError = Mathf.Clamp(timeToTarget / num, 0.5f, 1.5f);
		errorAccumulation += 0.005f;
		errorAccumulation = Mathf.Min(errorAccumulation, 1f);
		if (Vector3.Angle(missile.transform.forward, to) < 10f)
		{
			if (useDatalink && missile.NetworkHQ.IsTargetBeingTracked(targetUnit))
			{
				errorAccumulation = Mathf.Min(errorAccumulation, 0.2f);
			}
			if (useLaser && missile.NetworkHQ.IsTargetLased(targetUnit))
			{
				errorAccumulation = 0f;
			}
		}
	}

	public override string GetSeekerType()
	{
		return "INS";
	}

	private void UpdateTargetPosition()
	{
		if (useDatalink && missile.NetworkHQ != null && missile.NetworkHQ.IsTargetBeingTracked(targetUnit))
		{
			knownPos = targetUnit.GlobalPosition();
			knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
		}
	}

	private void UpdateTrajectory()
	{
		GlobalPosition ballisticAimPoint = Kinematics.GetBallisticAimPoint(missile, knownPos, timeToTarget, maxTargetSpeed, trajectoryError, knownVel);
		ballisticAimPoint -= Mathf.Min(timeToTarget, 10f) * 0.5f * measuredWind;
		ballisticAimPoint += errorAccumulation * error;
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
		UpdateTargetPosition();
		UpdateTrajectory();
	}
}
