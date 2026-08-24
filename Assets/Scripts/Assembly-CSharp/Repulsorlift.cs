using System;
using UnityEngine;

public class Repulsorlift : MonoBehaviour
{
	[Serializable]
	private class RepulsorliftProjector
	{
		[SerializeField]
		private Transform castTransform;

		[SerializeField]
		private UnitPart unitPart;

		[SerializeField]
		private float fastSpeed;

		[SerializeField]
		private float distanceSlow;

		[SerializeField]
		private float distanceFast;

		[SerializeField]
		private float spring;

		[SerializeField]
		private float damp;

		[SerializeField]
		private float maxForce;

		[SerializeField]
		private float hoverForce;

		private float compressionPrev;

		private float rangeSmoothed;

		private float rangeSmoothingVel;

		private float outputSmoothed;

		private float outputSmoothingVel;

		public void Initialize()
		{
			rangeSmoothed = distanceSlow;
		}

		public void Run(float powerRatio, float speed, Vector3 velocity, float alt, float customAxis1, bool groundRepel, out float requestedOutput)
		{
			float num = Mathf.Max(speed / fastSpeed - 0.2f, 0f);
			float num2 = Mathf.Lerp(distanceSlow, distanceFast, num);
			num2 += Mathf.Max((0f - velocity.y) * 2f, 0f);
			rangeSmoothed = Mathf.SmoothDamp(rangeSmoothed, num2, ref rangeSmoothingVel, 0.5f);
			float num3 = 1f - num * 0.5f;
			float num4 = 0f;
			if (num3 > 0f && groundRepel && Physics.Linecast(castTransform.position, castTransform.position - castTransform.up * rangeSmoothed, out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				float num5 = (rangeSmoothed - hitInfo.distance) / rangeSmoothed;
				float num6 = (num5 - compressionPrev) / Time.fixedDeltaTime;
				compressionPrev = num5;
				float num7 = Mathf.Clamp(num6 * damp, 0f, maxForce);
				num4 = Mathf.Clamp(num5 * spring + num7, 0f, maxForce) * num3;
			}
			outputSmoothed = ((num2 > 10f) ? Mathf.SmoothDamp(outputSmoothed, num4, ref outputSmoothingVel, Mathf.Min(num2 * 0.01f, 0.1f)) : num4);
			float num8 = customAxis1 * hoverForce;
			requestedOutput = outputSmoothed + num8;
			if (requestedOutput > 0f)
			{
				unitPart.rb.AddForceAtPosition(castTransform.up * requestedOutput * powerRatio, castTransform.position);
			}
		}
	}

	[SerializeField]
	private RepulsorliftProjector[] projectors;

	[SerializeField]
	private PowerSupply powerSupply;

	[SerializeField]
	private Aircraft aircraft;

	private ControlInputs inputs;

	private float availablePower;

	private float powerRatio;

	[SerializeField]
	private float maxActiveAlt = 10f;

	[SerializeField]
	private float powerConsumption = 0.01f;

	private void Awake()
	{
		RepulsorliftProjector[] array = projectors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize();
		}
		powerSupply = aircraft.GetPowerSupply();
		powerSupply.AddUser();
		powerSupply.SetFullyCharged();
		inputs = aircraft.GetInputs();
		inputs.customAxis1 = 0.5f;
	}

	private void OnDestroy()
	{
		if (powerSupply != null)
		{
			powerSupply.RemoveUser();
		}
	}

	private void FixedUpdate()
	{
		if (!aircraft.disabled)
		{
			float num = 0f;
			RepulsorliftProjector[] array = projectors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Run(powerRatio, aircraft.speed, aircraft.rb.velocity, aircraft.radarAlt, inputs.customAxis1, aircraft.gearDeployed || !aircraft.networked, out var requestedOutput);
				num += requestedOutput;
			}
			if (!aircraft.networked)
			{
				powerSupply.SetFullyCharged();
			}
			availablePower = powerSupply.DrawPower(num * powerConsumption);
			powerRatio = Mathf.Clamp01(availablePower / Mathf.Max(num * powerConsumption, 0.1f));
		}
	}
}
