using UnityEngine;

public class SpecialSmokeEjector : Weapon
{
	[SerializeField]
	private SmokeEmitter[] emitters;

	private bool triggerPulled;

	[SerializeField]
	private float ejectionInterval;

	private float lastEjectionTime;

	private float emitCounter;

	private ParticleSystem.EmitParams emitParams;

	private Aircraft aircraft;

	private Vector3 velocity;

	private float airspeed;

	public override void AttachToUnit(Unit unit)
	{
		base.AttachToUnit(unit);
		base.enabled = true;
		aircraft = attachedUnit as Aircraft;
		for (int i = 0; i < emitters.Length; i++)
		{
			emitters[i].Initialize();
		}
	}

	public override void SetTarget(Unit target)
	{
	}

	private void Update()
	{
		airspeed = aircraft.speed;
		velocity = aircraft.rb.velocity;
		Emit(airspeed, velocity);
	}

	public override void Fire(Unit firingUnit, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (attachedUnit.disabled || Time.timeSinceLevelLoad - lastEjectionTime < ejectionInterval)
		{
			return;
		}
		lastEjectionTime = Time.timeSinceLevelLoad;
		if (hardpoint != null)
		{
			if (hardpoint.part.IsDetached())
			{
				return;
			}
			if (info.useWeaponDoors)
			{
				hardpoint.SpringOpenBayDoors();
			}
		}
		triggerPulled = !triggerPulled;
		Debug.Log($"FIRE PRESSED {triggerPulled}");
		base.weaponStation = weaponStation;
		base.enabled = true;
	}

	public void Emit(float airspeed, Vector3 velocity)
	{
		for (int i = 0; i < emitters.Length; i++)
		{
			emitters[i].Emit(triggerPulled, airspeed, velocity);
		}
	}
}
