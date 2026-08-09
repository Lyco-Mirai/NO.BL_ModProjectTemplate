using System;
using UnityEngine;

[Serializable]
public class AimSolver
{
	[SerializeField]
	private bool correctShots;

	[SerializeField]
	private bool artillery;

	[SerializeField]
	private bool allowHighTrajectory;

	[SerializeField]
	private float simulationInterval = 2f;

	[SerializeField]
	private float artilleryDragCoef = 1f;

	[SerializeField]
	private float rakeAmount = 0.1f;

	[SerializeField]
	private float rakeFrequency = 1f;

	private float targetDist;

	private float lastSim;

	private Vector3 targetVelPrev;

	private Vector3 targetAccelSmoothed;

	private Vector3 targetAccelSmoothingVel;

	private Vector3 simCorrection;

	private Vector3 lastSimCorrection;

	private Vector3 simCorrectionRate;

	private Vector3 simCorrectionSmoothed;

	private Vector3 correctionSmoothingVel;

	private Vector3 aimCorrection;

	private Vector3 observedBulletVel;

	private Unit attachedUnit;

	private Unit currentTarget;

	private Transform firingTransform;

	private WeaponInfo weaponInfo;

	private BulletSim.Bullet observedBullet;

	public void SetTarget(Unit attachedUnit, Unit target, Transform firingTransform, WeaponInfo weaponInfo)
	{
		this.attachedUnit = attachedUnit;
		this.firingTransform = firingTransform;
		if (weaponInfo != this.weaponInfo || target != currentTarget)
		{
			currentTarget = target;
			this.weaponInfo = weaponInfo;
			observedBullet = null;
			aimCorrection = Vector3.zero;
			simCorrection = Vector3.zero;
			simCorrectionSmoothed = Vector3.zero;
			targetAccelSmoothed = Vector3.zero;
			lastSim = -100f;
		}
	}

	public void SetObservedBullet(BulletSim.Bullet bullet)
	{
		if (correctShots && observedBullet == null)
		{
			observedBullet = bullet;
		}
	}

	public void ObserveBullet()
	{
		int num;
		if (!observedBullet.impacted)
		{
			num = (observedBullet.active ? 1 : 0);
			if (num != 0)
			{
				observedBulletVel = observedBullet.velocity;
			}
		}
		else
		{
			num = 0;
		}
		Vector3 vector = observedBullet.position - currentTarget.GlobalPosition();
		if (num != 0 && !(Vector3.Dot(observedBullet.velocity, -vector) < 0f))
		{
			return;
		}
		if (vector.magnitude < Vector3.Distance(currentTarget.transform.position, firingTransform.position) * 0.5f)
		{
			Vector3 vector2 = Vector3.ProjectOnPlane(vector, observedBulletVel);
			aimCorrection -= vector2 * 0.5f;
			if (PlayerSettings.debugVis)
			{
				GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrowGreen, currentTarget.transform);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.rotation = Quaternion.LookRotation(vector2);
				gameObject.transform.localScale = new Vector3(2f, 2f, vector2.magnitude);
			}
		}
		else
		{
			aimCorrection = Vector3.zero;
		}
		observedBullet = null;
	}

	private void RunSim(GlobalPosition muzzlePosition, GlobalPosition targetPosition, Vector3 simpleLead, Vector3 targetVel, float estimatedTimeToTarget)
	{
		if (!(Time.timeSinceLevelLoad - lastSim < simulationInterval))
		{
			lastSim = Time.timeSinceLevelLoad;
			Vector3 initialVelocity = ((attachedUnit.speed > 1f) ? attachedUnit.rb.GetPointVelocity(firingTransform.position) : Vector3.zero) + simpleLead.normalized * weaponInfo.muzzleVelocity;
			Kinematics.TrajectorySim(PlayerSettings.debugVis && FastMath.InRange(attachedUnit.GlobalPosition(), SceneSingleton<CameraStateManager>.i.transform.GlobalPosition(), 100f), weaponInfo, initialVelocity, muzzlePosition, targetPosition, targetVel, targetAccelSmoothed, 0.1f, out var missVector, out var _);
			simCorrection = missVector * -1f;
			if (simCorrectionSmoothed == Vector3.zero)
			{
				simCorrectionSmoothed = simCorrection;
			}
		}
	}

	public Vector3 GetAimVector(out float targetRange)
	{
		GlobalPosition globalPosition = firingTransform.GlobalPosition();
		GlobalPosition globalPosition2 = currentTarget.GlobalPosition();
		targetRange = FastMath.Distance(globalPosition2, globalPosition);
		Vector3 vector = ((currentTarget.speed < 1f) ? Vector3.zero : currentTarget.rb.velocity);
		Vector3 vector2 = ((attachedUnit.speed < 1f) ? Vector3.zero : attachedUnit.rb.velocity);
		if (weaponInfo.missile)
		{
			float num = weaponInfo.GetMaxSpeed() * 0.7f;
			float a = targetRange / num;
			Vector3 vector3 = vector * Mathf.Min(a, 10f);
			return globalPosition2 + vector3 - globalPosition;
		}
		float maxSpeed = weaponInfo.GetMaxSpeed();
		float num2 = targetRange / weaponInfo.targetRequirements.maxRange;
		if (artillery && num2 > 0.2f)
		{
			float num3 = Mathf.Lerp(targetRange * (1f + 0.5f * artilleryDragCoef), targetRange * (1f + 1.5f * artilleryDragCoef), num2);
			Vector3 vector4 = globalPosition2 - globalPosition;
			vector4.y = 0f;
			if (Mathf.Abs(globalPosition.y - globalPosition2.y) / targetRange < 0.1f)
			{
				float num4 = 0.5f * Mathf.Asin(Mathf.Min(num3 * 9.81f / (maxSpeed * maxSpeed), 1f));
				if (allowHighTrajectory && num4 > MathF.PI / 6f && Mathf.Sin(NetworkSceneSingleton<MissionManager>.i.MissionTime / weaponInfo.fireInterval) > 0f)
				{
					num4 = MathF.PI / 2f - num4;
				}
				return vector4.normalized + Vector3.up * Mathf.Tan(num4);
			}
		}
		float num5 = Vector3.Dot((globalPosition2 - globalPosition).normalized, vector2 - vector);
		float num6 = targetRange / (maxSpeed * 0.9f + num5);
		if (correctShots && observedBullet != null)
		{
			ObserveBullet();
		}
		if (weaponInfo.muzzleVelocity == 0f)
		{
			return globalPosition2 + num6 * vector - globalPosition;
		}
		Vector3 target = (vector - targetVelPrev) / Time.fixedDeltaTime;
		targetVelPrev = vector;
		vector *= 1f + Mathf.Cos(Time.timeSinceLevelLoad * MathF.PI * 2f * rakeFrequency) * rakeAmount;
		targetAccelSmoothed = Vector3.SmoothDamp(targetAccelSmoothed, target, ref targetAccelSmoothingVel, 0.5f);
		Vector3 vector5 = num6 * vector + 0.5f * num6 * num6 * targetAccelSmoothed;
		vector5 -= num6 * vector2;
		Vector3 vector6 = num6 * num6 * 4.905f * weaponInfo.gravMult * Vector3.up;
		Vector3 vector7 = globalPosition2 + vector5 + vector6 - globalPosition;
		RunSim(globalPosition, globalPosition2, vector7, vector, num6);
		simCorrectionSmoothed = Vector3.SmoothDamp(simCorrectionSmoothed, simCorrection, ref correctionSmoothingVel, 0.15f);
		return vector7 + simCorrection + aimCorrection;
	}
}
