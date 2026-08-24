using System;
using UnityEngine;

public class ShipPropulsion : MonoBehaviour
{
	[Serializable]
	private class Rotator
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private float minSpeed;

		[SerializeField]
		private float maxSpeed;

		public void Animate(float power)
		{
			transform.localEulerAngles += Vector3.forward * Mathf.Lerp(minSpeed, maxSpeed, power) * Time.deltaTime;
		}
	}

	[SerializeField]
	private ShipPart part;

	[SerializeField]
	private ShipPart[] criticalParts;

	[SerializeField]
	private float thrust;

	[SerializeField]
	private float steeringThrust;

	[SerializeField]
	private float momentumFactor = 0.05f;

	[SerializeField]
	private float damageThreshold;

	[SerializeField]
	private float inputSmoothing;

	[SerializeField]
	private ParticleSystem[] particles;

	[SerializeField]
	private AudioSource thrustSound;

	[SerializeField]
	private AudioSource engineSound;

	[SerializeField]
	private Transform thrustTransform;

	[SerializeField]
	private bool underwater = true;

	[SerializeField]
	private Rotator[] rotators;

	private float thrustInputSmoothed;

	private float steeringInputSmoothed;

	private float thrustSmoothSpeed;

	private float steeringSmoothSpeed;

	private ShipInputs inputs;

	private Ship ship;

	protected void Awake()
	{
		part.onDetachFromParent += ShipPropulsion_OnPartDetach;
		ShipPart[] array = criticalParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onApplyDamage += ShipPropulsion_OnApplyDamage;
		}
		part.parentUnit.onDisableUnit += ShipPropulsion_OnDisabled;
		ship = part.parentUnit as Ship;
		inputs = ship.GetInputs();
	}

	private void ShipPropulsion_OnPartDetach(ShipPart shipPart)
	{
		DisablePropulsion();
	}

	private void ShipPropulsion_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
		if (e.hitPoints < damageThreshold)
		{
			DisablePropulsion();
		}
	}

	private void ShipPropulsion_OnDisabled(Unit unit)
	{
		DisablePropulsion();
	}

	private void DisablePropulsion()
	{
		thrust = 0f;
		ParticleSystem[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
		if (thrustSound != null)
		{
			thrustSound.Stop();
		}
		if (engineSound != null)
		{
			engineSound.Stop();
		}
		base.enabled = false;
	}

	private void FixedUpdate()
	{
		if (thrust == 0f)
		{
			base.enabled = false;
			return;
		}
		Rotator[] array = rotators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Animate(inputs.throttle);
		}
		if (!ship.LocalSim)
		{
			return;
		}
		int num = ((!underwater || thrustTransform.position.y < Datum.LocalSeaY) ? 1 : 0);
		float throttle = inputs.throttle;
		float target = (1f + ship.speed * momentumFactor) * inputs.steering;
		thrustInputSmoothed = FastMath.SmoothDamp(thrustInputSmoothed, throttle, ref thrustSmoothSpeed, inputSmoothing);
		steeringInputSmoothed = FastMath.SmoothDamp(steeringInputSmoothed, target, ref steeringSmoothSpeed, inputSmoothing);
		steeringInputSmoothed *= ship.AllowedSteerRate;
		part.rb.AddForceAtPosition((float)num * thrustInputSmoothed * thrust * base.transform.forward + (float)num * steeringInputSmoothed * steeringThrust * base.transform.right, thrustTransform.position);
		if (engineSound != null)
		{
			engineSound.pitch = Mathf.Lerp(engineSound.pitch, Mathf.Min(0.5f + ship.speed * 0.005f + Mathf.Abs(thrustInputSmoothed) * 0.35f + Mathf.Abs(steeringInputSmoothed) * 0.25f, 1.5f), Time.deltaTime);
			engineSound.volume = engineSound.pitch;
		}
		if (throttle > 0.1f && num > 0)
		{
			if (particles.Length != 0 && !particles[0].isPlaying)
			{
				ParticleSystem[] array2 = particles;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].Play();
				}
			}
			if (thrustSound != null)
			{
				if (!thrustSound.isPlaying)
				{
					thrustSound.Play();
				}
				if (thrustSound.volume < 1f)
				{
					thrustSound.volume += Time.deltaTime;
				}
			}
		}
		if (!(throttle < 0.1f) && num >= 0)
		{
			return;
		}
		if (particles.Length != 0 && particles[0].isPlaying)
		{
			ParticleSystem[] array2 = particles;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Stop();
			}
		}
		if (thrustSound != null && thrustSound.isPlaying)
		{
			if (thrustSound.volume > 0f)
			{
				thrustSound.volume -= Time.deltaTime;
			}
			else
			{
				thrustSound.Stop();
			}
		}
	}
}
