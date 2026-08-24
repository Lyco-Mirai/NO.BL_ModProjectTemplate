using System;
using UnityEngine;

public class BallisticMissileGuidance : MissileSeeker
{
	[Serializable]
	private class RCS
	{
		public bool enabled;

		private float lastFired;

		public void CorrectTrajectory(float airDensity, GlobalPosition targetPosition, Vector3 targetKnownVel, Rigidbody rb)
		{
			if (!(airDensity > 0.1f) && !(Time.timeSinceLevelLoad - lastFired < 5f))
			{
				lastFired = Time.timeSinceLevelLoad;
				float num = Kinematics.FallTime(rb.transform.GlobalPosition().y - targetPosition.y, rb.velocity.y);
				targetPosition += num * targetKnownVel;
				Vector3 vector = targetPosition - rb.transform.GlobalPosition();
				Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
				Vector3 vector3 = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
				Vector3 force = vector2 / num - vector3;
				force.x = Mathf.Clamp(force.x, -1f, 1f);
				force.z = Mathf.Clamp(force.z, -1f, 1f);
				rb.AddForce(force, ForceMode.VelocityChange);
			}
		}
	}

	[SerializeField]
	private float airburstHeight;

	[SerializeField]
	private float armDelay = 2f;

	[SerializeField]
	private float tangibleDelay = 2f;

	[SerializeField]
	private float circularError;

	[SerializeField]
	private float maxTargetSpeed = 20f;

	[SerializeField]
	private RCS rcs;

	[SerializeField]
	private PIDFactors trajectoryPIDFactors;

	private GlobalPosition knownPos;

	private PID trajectoryPID;

	private Vector3 knownVel;

	private Vector3 errorOffset;

	private Vector3 desiredVector;

	private float lastTargetUpdate;

	private float launchTime;

	private float targetAngle;

	private void Awake()
	{
		launchTime = Time.timeSinceLevelLoad;
		errorOffset = UnityEngine.Random.insideUnitSphere * circularError;
		trajectoryPID = new PID(trajectoryPIDFactors);
	}

	public override void Initialize(Unit target, GlobalPosition aimpoint)
	{
		missile.NetworkseekerMode = Missile.SeekerMode.passive;
		targetAngle = 45f;
		if (UnitRegistry.TryGetUnit(missile.targetID, out target))
		{
			targetUnit = target;
			knownPos = missile.NetworkHQ.GetKnownPosition(target) ?? (missile.GlobalPosition() + missile.transform.forward * 10000f);
			knownVel = ((target.rb != null) ? Vector3.ClampMagnitude(target.rb.velocity, maxTargetSpeed) : Vector3.zero);
		}
		else
		{
			Vector3 forward = missile.transform.forward;
			forward.y = 0f;
			knownPos = missile.GlobalPosition() + forward.normalized * 100000f;
			knownPos.y = 0f;
		}
		Vector3 vector = knownPos - missile.GlobalPosition();
		vector.y = 0f;
		float magnitude = vector.magnitude;
		float remainingDeltaV = missile.GetRemainingDeltaV();
		float num = Mathf.Asin(magnitude * 9.81f / (remainingDeltaV * remainingDeltaV)) * 0.5f * 57.29578f;
		targetAngle = ((num > 45f) ? num : (90f - num));
		if (float.IsNaN(targetAngle))
		{
			targetAngle = 45f;
		}
		missile.SetAimpoint(knownPos, knownVel);
		this.StartSlowUpdateDelayed(1f, SlowChecks);
		missile.DeployFins();
	}

	private void SlowChecks()
	{
		if (!missile.disabled)
		{
			if (!missile.IsArmed() && missile.timeSinceSpawn > armDelay)
			{
				missile.Arm();
			}
			if (missile.timeSinceSpawn > 4f && missile.speed < 50f && missile.GlobalPosition().y < 3000f)
			{
				missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
			}
			if (!missile.IsTangible() && missile.timeSinceSpawn > tangibleDelay)
			{
				missile.SetTangible(tangible: true);
			}
		}
	}

	public override string GetSeekerType()
	{
		return "INS";
	}

	private void SetTrajectory()
	{
		if (Time.timeSinceLevelLoad - lastTargetUpdate < 0.5f)
		{
			return;
		}
		lastTargetUpdate = Time.timeSinceLevelLoad;
		if (targetUnit != null && !targetUnit.disabled && missile.NetworkHQ.TryGetKnownPosition(targetUnit, out knownPos) && missile.NetworkHQ.IsTargetBeingTracked(targetUnit))
		{
			knownVel = ((targetUnit.rb == null) ? Vector3.zero : targetUnit.rb.velocity);
		}
		Vector3 to = knownPos - missile.GlobalPosition();
		Vector3 current = new Vector3(to.x, 0f, to.z);
		float magnitude = current.magnitude;
		float a = Vector3.Dot(missile.rb.velocity, current.normalized);
		float num = magnitude / Mathf.Max(a, 100f);
		float num2 = Kinematics.FallTime(missile.GlobalPosition().y - knownPos.y, missile.rb.velocity.y);
		GlobalPosition globalPosition = knownPos + knownVel * num2 + num * num * 4.905f * Vector3.up;
		if (missile.EngineOn())
		{
			SimulateTrajectorySimple(out var overshoot);
			targetAngle += trajectoryPID.GetOutput(overshoot * 0.001f, 0.5f);
			targetAngle = Mathf.Clamp(targetAngle, 45f, 90f);
			if (float.IsNaN(targetAngle))
			{
				targetAngle = 45f;
			}
			desiredVector = Vector3.RotateTowards(current, Vector3.up, targetAngle * (MathF.PI / 180f), 0f);
			globalPosition = base.transform.GlobalPosition() + desiredVector * 1000f;
		}
		else if (Vector3.Angle(base.transform.forward, to) > 30f || missile.airDensity < 0.1f)
		{
			globalPosition = base.transform.GlobalPosition() + missile.rb.velocity * 10f;
		}
		Vector3 leadVector = TargetCalc.GetLeadVector(globalPosition, missile.GlobalPosition(), knownVel, missile.rb.velocity, 30f);
		missile.SetAimpoint(globalPosition + leadVector + errorOffset, knownVel);
	}

	private void SimulateTrajectorySimple(out float overshoot)
	{
		Vector3 vector = knownPos - missile.GlobalPosition();
		Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
		float magnitude = vector2.magnitude;
		float remainingDeltaV = missile.GetRemainingDeltaV();
		float remainingBurnTime = missile.GetRemainingBurnTime();
		Vector3 normalized = (vector2 + Mathf.Tan(targetAngle * (MathF.PI / 180f)) * magnitude * Vector3.up).normalized;
		if (Vector3.Angle(normalized, missile.rb.velocity) > 5f)
		{
			overshoot = 0f;
			return;
		}
		Vector3 vector3 = missile.rb.velocity + remainingDeltaV * normalized;
		GlobalPosition globalPosition = missile.GlobalPosition() + remainingBurnTime * 0.5f * (missile.rb.velocity + vector3);
		magnitude = new Vector3(knownPos.x - globalPosition.x, 0f, knownPos.z - globalPosition.z).magnitude;
		float num = Vector3.Dot(vector3, vector2.normalized);
		float initialVerticalVelocity = Vector3.Dot(vector3, Vector3.up);
		float num2 = Kinematics.FallTime(globalPosition.y, initialVerticalVelocity);
		float num3 = magnitude / num;
		overshoot = (num2 - num3) * num;
	}

	private void Fusing()
	{
		if (missile.IsTangible() && airburstHeight > 0f && missile.timeSinceSpawn > 30f && !missile.IsArmed() && missile.GlobalPosition().y - knownPos.y < airburstHeight)
		{
			missile.Arm();
			missile.Detonate(missile.rb.velocity, hitArmor: false, hitTerrain: false);
		}
	}

	public override void Seek()
	{
		if (missile.LocalSim)
		{
			SetTrajectory();
			rcs.CorrectTrajectory(missile.airDensity, knownPos, knownVel, missile.rb);
			Fusing();
		}
	}
}
