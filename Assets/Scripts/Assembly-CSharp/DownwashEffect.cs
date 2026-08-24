using UnityEngine;

public class DownwashEffect : MonoBehaviour
{
	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private ParticleSystem[] landEffects;

	[SerializeField]
	private ParticleSystem[] waterEffects;

	[SerializeField]
	private float range;

	[SerializeField]
	private float minSpeed;

	private ParticleSystem.MainModule[] waterEffectMains;

	private ParticleSystem.MainModule[] landEffectMains;

	private float lastCheck;

	private GlobalPosition effectPosition;

	private Vector3 velAlongSlope;

	private void Start()
	{
		waterEffectMains = new ParticleSystem.MainModule[waterEffects.Length];
		landEffectMains = new ParticleSystem.MainModule[landEffects.Length];
		for (int i = 0; i < waterEffects.Length; i++)
		{
			waterEffectMains[i] = waterEffects[i].main;
		}
		for (int j = 0; j < landEffects.Length; j++)
		{
			landEffectMains[j] = landEffects[j].main;
		}
		this.StartSlowUpdateDelayed(1f, SlowUpdate);
	}

	private void SetEffects(ParticleSystem[] systems, bool enabled)
	{
		if (enabled)
		{
			ParticleSystem[] array = systems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
		}
		else
		{
			ParticleSystem[] array = systems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
	}

	private void SlowUpdate()
	{
		if (base.enabled)
		{
			if (aircraft.radarAlt > range)
			{
				base.enabled = false;
				StopEffects();
			}
		}
		else if (aircraft.radarAlt < range)
		{
			base.enabled = true;
		}
	}

	private void StopEffects()
	{
		ParticleSystem[] array = waterEffects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
		array = landEffects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - lastCheck > 0.1f)
		{
			if (aircraft.displayDetail < 1f)
			{
				StopEffects();
				return;
			}
			velAlongSlope = new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z);
			lastCheck = Time.timeSinceLevelLoad;
			float distance = range;
			bool flag = false;
			bool flag2 = false;
			if (aircraft.speed >= minSpeed)
			{
				Ray ray = new Ray(aircraft.transform.position, Vector3.up * -340f - aircraft.rb.velocity);
				if (Physics.Raycast(ray, out var hitInfo, range, PhysicsLayers.StaticsMask))
				{
					distance = hitInfo.distance;
					velAlongSlope = Vector3.ProjectOnPlane(aircraft.rb.velocity, hitInfo.normal);
					base.transform.rotation = Quaternion.LookRotation(velAlongSlope);
					effectPosition = hitInfo.point.ToGlobalPosition();
					Color color = new Color(1f, 1f, 1f, 1f - distance / range);
					for (int i = 0; i < landEffectMains.Length; i++)
					{
						landEffectMains[i].emitterVelocity = velAlongSlope;
						landEffectMains[i].startColor = color;
					}
					flag = true;
				}
				if (Datum.WaterPlane().Raycast(ray, out var enter) && enter < distance)
				{
					base.transform.rotation = Quaternion.identity;
					effectPosition = aircraft.transform.GlobalPosition() + ray.direction * enter;
					Color color2 = new Color(1f, 1f, 1f, 1f - enter / range);
					for (int j = 0; j < waterEffectMains.Length; j++)
					{
						waterEffectMains[j].emitterVelocity = new Vector3(aircraft.rb.velocity.x, 0f, aircraft.rb.velocity.z);
						waterEffectMains[j].startColor = color2;
					}
					flag2 = true;
					flag = false;
				}
			}
			SetEffects(waterEffects, flag2);
			SetEffects(landEffects, flag);
		}
		base.transform.position = effectPosition.ToLocalPosition() + velAlongSlope * (Time.timeSinceLevelLoad - lastCheck);
	}
}
