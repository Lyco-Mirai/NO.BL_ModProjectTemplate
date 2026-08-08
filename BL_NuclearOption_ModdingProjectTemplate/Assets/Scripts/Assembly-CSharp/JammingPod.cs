using NuclearOption.Networking;
using UnityEngine;

public class JammingPod : Weapon
{
	[SerializeField]
	private float power;

	[SerializeField]
	private float effectiveness;

	[SerializeField]
	private AnimationCurve rangeFalloff;

	[SerializeField]
	private Transform directionTransform;

	private PowerSupply powerSupply;

	private float lastJammingTick;

	private float previousLastFired;

	private float rewardAmount;

	private float rewardCount;

	private float rewardThreshold = 60f;

	public override void AttachToUnit(Unit unit)
	{
		base.AttachToUnit(unit);
		lastJammingTick = Time.timeSinceLevelLoad;
		powerSupply = unit.GetPowerSupply();
		base.enabled = false;
	}

	public override void SetTarget(Unit target)
	{
		currentTarget = target;
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		_ = base.enabled;
		base.enabled = true;
		lastFired = Time.timeSinceLevelLoad;
		weaponStation.LastFiredTime = Time.timeSinceLevelLoad;
	}

	private void LateUpdate()
	{
		if (lastFired == previousLastFired)
		{
			base.enabled = false;
		}
		previousLastFired = lastFired;
	}

	private void FixedUpdate()
	{
		Vector3 forward = ((currentTarget != null) ? (currentTarget.transform.position - base.transform.position) : base.transform.forward);
		directionTransform.rotation = Quaternion.LookRotation(forward);
		float num = powerSupply.DrawPower(power) / power;
		if (currentTarget == null || !attachedUnit.NetworkHQ.IsTargetBeingTracked(currentTarget))
		{
			return;
		}
		Transform transform = ((currentTarget.radar != null) ? currentTarget.radar.GetScanPoint() : currentTarget.transform);
		if (Physics.Linecast(directionTransform.position, transform.position, out var _, PhysicsLayers.StaticsMask))
		{
			return;
		}
		float magnitude = forward.magnitude;
		if (!(Time.timeSinceLevelLoad - lastJammingTick > 0.2f) || !NetworkManagerNuclearOption.i.Server.Active)
		{
			return;
		}
		float num2 = rangeFalloff.Evaluate(magnitude) * num;
		lastJammingTick = Time.timeSinceLevelLoad;
		currentTarget.Jam(new Unit.JamEventArgs
		{
			jamAmount = num2,
			jammingUnit = attachedUnit
		});
		if (attachedUnit is Aircraft aircraft && currentTarget.HasRadarEmission() && aircraft.Player != null && currentTarget.NetworkHQ != null && currentTarget.NetworkHQ != aircraft.NetworkHQ)
		{
			rewardCount += num2 * 0.2f;
			rewardAmount += 0.0001f * num2 * Mathf.Sqrt(currentTarget.definition.value);
			if (rewardCount > rewardThreshold)
			{
				aircraft.NetworkHQ.ReportJammingAction(aircraft.Player, currentTarget, rewardAmount);
				rewardAmount = 0f;
				rewardCount = 0f;
			}
		}
	}
}
