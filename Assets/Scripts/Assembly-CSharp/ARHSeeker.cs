using UnityEngine;

public class ARHSeeker : MissileSeeker
{
	[SerializeField]
	private float lockPerseverance = 2f;

	[SerializeField]
	private bool homeOnJam;

	[SerializeField]
	private float homingLockDelay;

	[SerializeField]
	private float maxDatalinkAngle = 90f;

	[SerializeField]
	private float minReacquireRange = 2000f;

	[SerializeField]
	private float datalinkPositionalError;

	[SerializeField]
	private float maxTrackingAngle;

	[SerializeField]
	private float armDelay = 1f;

	[SerializeField]
	private float guidanceDelay = 1f;

	[SerializeField]
	private float terminalRange = 12000f;

	[SerializeField]
	private float maxLead = 5f;

	[SerializeField]
	private float selfDestructAtSpeed = 200f;

	[SerializeField]
	private float loftAmount = 0.2f;

	[SerializeField]
	private RadarParams radarParameters;

	[Range(0f, 1f)]
	[SerializeField]
	private float jamTolerance;

	private GlobalPosition knownPos;

	private Vector3 knownVel;

	private Vector3 knownVelPrev;

	private Vector3 knownAccel;

	private float lastActiveTrackAttempt;

	private float lastDatalinkTrackAttempt;

	private float timeWithoutReturn;

	private float returnStrength;

	private float homingLockTime;

	private float jamAccumulation;

	private float topSpeed;

	private float targetDist;

	private float timeToTarget;

	private Vector3 positionalErrorVector;

	private bool armed;

	private bool guidance;

	private bool isJammed;

	private bool radarLockEstablished;

	private bool achievedLock;

	private bool multipleInbound;

	[SerializeField]
	private JinkEvasion jinkEvasion;

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		missile.NetworkseekerMode = Missile.SeekerMode.passive;
		positionalErrorVector = Random.insideUnitSphere * datalinkPositionalError;
		missile.onJam += ARHSeeker_OnJam;
		lastActiveTrackAttempt = Time.timeSinceLevelLoad - 0.9f;
		topSpeed = missile.GetTopSpeed(0f, 0f);
		targetUnit = target;
		if (target is IRadarReturn)
		{
			target.onAddRadarChaff += ARHSeeker_OnChaff;
		}
		knownPos = missile.GlobalPosition() + missile.transform.forward * 100000f;
		if (targetUnit != null && missile.NetworkHQ != null)
		{
			missile.NetworkHQ.TryGetKnownPosition(targetUnit, out knownPos);
			knownPos += positionalErrorVector;
			if (proximityFuse)
			{
				missile.SetProxyFuse(target.GetRandomPart().transform, target.rb);
			}
		}
		missile.SetAimpoint(knownPos, Vector3.zero);
		this.StartSlowUpdate(1f, SlowChecks);
	}

	private void SlowChecks()
	{
		if (missile.disabled)
		{
			return;
		}
		if ((missile.timeSinceSpawn > 10f || (!missile.EngineOn() && missile.timeSinceSpawn > 2f)) && (missile.LosingGround() || missile.MissedTarget() || missile.speed < selfDestructAtSpeed || targetUnit == null))
		{
			missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
		}
		if (missile.NetworkHQ != null && targetUnit != null && !targetUnit.disabled && targetUnit.NetworkHQ != missile.NetworkHQ)
		{
			TrackingInfo trackingData = missile.NetworkHQ.GetTrackingData(targetUnit.persistentID);
			multipleInbound = targetUnit is Aircraft && trackingData.missileAttacks > 1;
		}
		if (loftAmount > 0f)
		{
			Vector3 vector = knownPos - missile.GlobalPosition();
			float a = Vector3.Dot(vector.normalized, missile.rb.velocity);
			if (targetUnit != null && missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				vector = knownPosition - missile.GlobalPosition();
			}
			targetDist = vector.magnitude;
			timeToTarget = targetDist / Mathf.Max(a, 10f);
		}
	}

	public override string GetSeekerType()
	{
		return "ARH";
	}

	private void ARHSeeker_OnJam(Unit.JamEventArgs e)
	{
		if (!(Vector3.Angle(e.jammingUnit.GlobalPosition() - missile.GlobalPosition(), missile.transform.forward) > maxTrackingAngle))
		{
			jamAccumulation += e.jamAmount;
			missile.RecordDamage(e.jammingUnit.persistentID, 0.01f);
			if (!(jamAccumulation < jamTolerance) && homeOnJam)
			{
				missile.SetTarget(e.jammingUnit);
				targetUnit = e.jammingUnit;
				knownPos = e.jammingUnit.GlobalPosition();
				knownVel = e.jammingUnit.rb.velocity;
				radarLockEstablished = false;
			}
		}
	}

	private void ARHSeeker_OnChaff(RadarChaff source)
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
			Debug.Log($"Target chaff angle: {num2:F2}, range coeff: {num:F2}, dazzle : {num3:F2} jam: {jamAccumulation:F2} success {jamAccumulation > jamTolerance}");
		}
	}

	private float RangeCoef(float targetDistance)
	{
		float maxRange = radarParameters.maxRange;
		float num = targetDistance / maxRange;
		return Mathf.Clamp01(1f - num);
	}

	private float GetRadarReturn()
	{
		if (Time.timeSinceLevelLoad - lastActiveTrackAttempt < 0.5f)
		{
			return returnStrength;
		}
		lastActiveTrackAttempt = Time.timeSinceLevelLoad;
		if (!(targetUnit is IRadarReturn radarReturn))
		{
			return 0f;
		}
		if (isJammed && !homeOnJam)
		{
			return 0f;
		}
		if (returnStrength == 0f && targetDist < 5000f && radarReturn.GetECMIntensity() > 2f)
		{
			return 0f;
		}
		GlobalPosition globalPosition = missile.GlobalPosition();
		GlobalPosition globalPosition2 = targetUnit.GlobalPosition();
		Vector3 vector = globalPosition2 - globalPosition;
		vector.y = 0f;
		float num = FastMath.Distance(globalPosition, globalPosition2);
		float magnitude = vector.magnitude;
		float num2 = Mathf.Sqrt(12742000f * globalPosition.y);
		float num3 = Mathf.Sqrt(12742000f * globalPosition2.y);
		if (num2 + num3 < num || !TargetCalc.LineOfSight(base.transform, targetUnit.transform, 10f))
		{
			return -1f;
		}
		if (num > radarParameters.maxRange)
		{
			return 0f;
		}
		if (returnStrength < radarParameters.minSignal && num < minReacquireRange)
		{
			return 0f;
		}
		if (Vector3.Angle(base.transform.forward, targetUnit.transform.position - base.transform.position) > maxTrackingAngle)
		{
			return 0f;
		}
		float num4 = 0f;
		if (magnitude < num2 && globalPosition2.y < globalPosition.y * (1f - magnitude / num2))
		{
			float num5 = num * targetUnit.radarAlt / (globalPosition.y - globalPosition2.y);
			num4 += Mathf.Min(num, 1000f) / num5;
		}
		num4 += targetUnit.maxRadius * targetUnit.maxRadius * 2f / (targetUnit.radarAlt * targetUnit.radarAlt);
		return radarReturn.GetRadarReturn(missile.transform.position, null, missile, num, num4, radarParameters, triggerWarning: true);
	}

	public override void Seek()
	{
		if (missile.targetID.NotValid)
		{
			knownPos += knownVel * Time.fixedDeltaTime;
			missile.SetAimpoint(knownPos, Vector3.zero);
			return;
		}
		if (!armed && missile.timeSinceSpawn > armDelay)
		{
			armed = true;
			missile.Arm();
			missile.SetTangible(tangible: true);
		}
		if (!guidance && missile.timeSinceSpawn > guidanceDelay)
		{
			guidance = true;
			missile.DeployFins();
		}
		jamAccumulation -= Mathf.Max(jamAccumulation, 0.2f) * Mathf.Max(jamTolerance, 0.1f) * Time.deltaTime;
		jamAccumulation = Mathf.Clamp01(jamAccumulation);
		isJammed = jamAccumulation > jamTolerance;
		if (targetUnit == null)
		{
			missile.SetTarget(null);
			missile.SetAimpoint(missile.GlobalPosition() + missile.transform.forward * 10000f, Vector3.zero);
			return;
		}
		if (!guidance)
		{
			missile.SetAimpoint(missile.GlobalPosition() + missile.transform.forward * 10000f, Vector3.zero);
			return;
		}
		if (!radarLockEstablished)
		{
			DatalinkMode();
			knownPos += knownVel * Time.fixedDeltaTime;
		}
		else
		{
			TerminalMode();
		}
		Vector3 platformVel = ((missile.timeSinceSpawn < 3f) ? (missile.transform.forward * topSpeed) : missile.rb.velocity);
		Vector3 leadVectorWithAccel = TargetCalc.GetLeadVectorWithAccel(knownPos, missile.GlobalPosition(), knownVel, platformVel, knownAccel, maxLead);
		if (loftAmount > 0f)
		{
			if (missile.timeSinceSpawn < 3f)
			{
				timeToTarget = targetDist / topSpeed;
			}
			float num = Mathf.Min(timeToTarget * timeToTarget * 4.905f * loftAmount, targetDist * loftAmount);
			leadVectorWithAccel += num * Vector3.up;
			timeToTarget -= Time.fixedDeltaTime;
		}
		GlobalPosition globalPosition = knownPos + leadVectorWithAccel;
		float num2 = Mathf.InverseLerp(3f, 10f, missile.timeSinceSpawn);
		if (jinkEvasion.amount > 0f && multipleInbound && targetDist > terminalRange)
		{
			globalPosition += num2 * jinkEvasion.ApplyJink(base.transform.GlobalPosition(), knownPos, missile.speed, targetDist);
		}
		globalPosition.y = Mathf.Max(globalPosition.y, 0f);
		missile.SetAimpoint(globalPosition, knownVel);
		if (PlayerSettings.debugVis)
		{
			GameObject obj = Object.Instantiate(GameAssets.i.debugArrowGreen, missile.transform);
			obj.transform.rotation = Quaternion.LookRotation(globalPosition - missile.GlobalPosition());
			obj.transform.localScale = new Vector3(1f, 1f, (globalPosition - missile.GlobalPosition()).magnitude);
			Object.Destroy(obj, 0.05f);
		}
	}

	private void DatalinkMode()
	{
		if (Time.timeSinceLevelLoad - lastDatalinkTrackAttempt < 1f)
		{
			return;
		}
		lastDatalinkTrackAttempt = Time.timeSinceLevelLoad;
		if (FastMath.Distance(knownPos, missile.GlobalPosition()) < terminalRange)
		{
			returnStrength = GetRadarReturn();
			Missile.SeekerMode seekerMode = ((returnStrength > radarParameters.minSignal) ? Missile.SeekerMode.activeLock : Missile.SeekerMode.activeSearch);
			if (missile.seekerMode != seekerMode)
			{
				missile.NetworkseekerMode = seekerMode;
			}
		}
		if (returnStrength > radarParameters.minSignal)
		{
			knownPos = targetUnit.GlobalPosition();
			knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
			radarLockEstablished = true;
			if (!achievedLock && targetUnit is Aircraft aircraft)
			{
				aircraft.RecordDamage(missile.ownerID, 0.001f);
				achievedLock = true;
			}
			return;
		}
		if (missile.NetworkHQ.IsTargetBeingTracked(targetUnit))
		{
			knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
		}
		if (missile.NetworkHQ.IsTargetPositionAccurate(targetUnit, 2000f))
		{
			knownPos = missile.NetworkHQ.GetKnownPosition(targetUnit).Value;
			knownPos += positionalErrorVector;
		}
		if (maxDatalinkAngle < 180f && Vector3.Angle(missile.transform.forward, FastMath.Direction(missile.GlobalPosition(), knownPos)) > maxDatalinkAngle)
		{
			knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
		}
		if (!FastMath.InRange(knownPos, targetUnit.GlobalPosition(), 2000f))
		{
			missile.SetTarget(null);
			targetUnit = null;
		}
	}

	private void TerminalMode()
	{
		returnStrength = GetRadarReturn();
		Missile.SeekerMode seekerMode = ((returnStrength > radarParameters.minSignal) ? Missile.SeekerMode.activeLock : Missile.SeekerMode.activeSearch);
		if (missile.seekerMode != seekerMode)
		{
			missile.NetworkseekerMode = seekerMode;
		}
		if (returnStrength < radarParameters.minSignal)
		{
			if (returnStrength == -1f)
			{
				missile.SetTarget(null);
			}
			homingLockTime = 0f;
			timeWithoutReturn += Time.deltaTime;
			if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
			{
				knownPos = knownPosition + positionalErrorVector;
			}
			if (Vector3.Angle(knownPos - missile.GlobalPosition(), missile.transform.forward) > maxTrackingAngle)
			{
				knownPos = missile.GlobalPosition() + missile.transform.forward * 1000f;
			}
			else
			{
				knownPos += knownVel * Time.fixedDeltaTime;
			}
			if (timeWithoutReturn > lockPerseverance)
			{
				missile.SetTarget(null);
			}
		}
		else
		{
			homingLockTime += Time.fixedDeltaTime;
			timeWithoutReturn = 0f;
			if (homingLockTime > homingLockDelay)
			{
				knownPos = targetUnit.GlobalPosition();
				knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
				knownAccel = (knownVel - knownVelPrev) / Time.fixedDeltaTime;
				knownVelPrev = knownVel;
			}
			missile.SetTarget(targetUnit);
		}
	}

	public RadarParams GetRadarParams()
	{
		return radarParameters;
	}
}
