using System;
using UnityEngine;

public class TiltWingController : MonoBehaviour, IWingAngleGauge
{
	[Serializable]
	private class RotatorLinkage
	{
		[SerializeField]
		private bool stretch;

		[SerializeField]
		private Transform linkage;

		[SerializeField]
		private Transform stretchTo;

		public void Animate()
		{
			Vector3 forward = stretchTo.transform.position - linkage.transform.position;
			linkage.transform.rotation = Quaternion.LookRotation(forward, linkage.transform.up);
			if (stretch)
			{
				linkage.transform.localScale = new Vector3(1f, 1f, forward.magnitude);
			}
		}

		public void Remove()
		{
			linkage.gameObject.GetComponent<Renderer>().enabled = false;
		}
	}

	[Serializable]
	private class RotatorInput
	{
		[SerializeField]
		private AeroPart part;

		[SerializeField]
		private AeroPart[] connectedParts;

		[SerializeField]
		private RotatorLinkage[] links;

		[SerializeField]
		private float minAngle;

		[SerializeField]
		private float maxAngle;

		[SerializeField]
		private float minSpeed;

		[SerializeField]
		private float maxSpeed;

		[SerializeField]
		private float rotationSpeed;

		[SerializeField]
		private float yawFactor;

		[SerializeField]
		private float customAxis1Factor;

		[SerializeField]
		private float pitchFactor;

		[SerializeField]
		private float spring;

		[SerializeField]
		private float damp;

		[SerializeField]
		private float breakStrength;

		[SerializeField]
		private float currentAngle = 0.18f;

		private float baseAngle;

		public (float, float) GetAngleLimits()
		{
			return (minAngle, maxAngle);
		}

		public float GetAngle()
		{
			return currentAngle;
		}

		public void Setup()
		{
			baseAngle = part.transform.localEulerAngles.x;
			part.onPartDetached += RotatorInput_OnDetached;
		}

		private void RotatorInput_OnDetached(UnitPart unitPart)
		{
			RotatorLinkage[] array = links;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Remove();
			}
			part.onPartDetached -= RotatorInput_OnDetached;
		}

		public void Animate(ControlInputs inputs, float speed, float tiltCorrection)
		{
			float num = Mathf.Clamp01(1f - currentAngle * 0.03f);
			float num2 = currentAngle / maxAngle;
			int num3 = ((num2 > 0.2f && num2 < 0.7f) ? 1 : 0);
			float t = yawFactor * inputs.yaw * num + pitchFactor * (float)num3 * inputs.pitch + tiltCorrection;
			float num4 = Mathf.Lerp(minAngle, maxAngle, t) - currentAngle;
			currentAngle += Mathf.Clamp(num4, (0f - rotationSpeed) * Time.deltaTime, rotationSpeed * Time.deltaTime);
			currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
			for (int i = 0; i < connectedParts.Length; i++)
			{
				part.SetHingeJoint(i, connectedParts[i], spring, damp, currentAngle, breakStrength, baseAngle, Vector3.right);
			}
			if (Mathf.Abs(num4) > 0.01f)
			{
				RotatorLinkage[] array = links;
				for (int j = 0; j < array.Length; j++)
				{
					array[j].Animate();
				}
			}
		}
	}

	[SerializeField]
	private RotatorInput[] rotatingJoints;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private float forwardLockSpeed;

	[SerializeField]
	private float hoverLockSpeed;

	[SerializeField]
	private float tiltTransitionRate;

	[SerializeField]
	private Vector3 autoTiltPIDFactors;

	[SerializeField]
	private AnimationCurve tiltAtSpeed;

	private ControlInputs inputs;

	private AircraftParameters aircraftParameters;

	private float tiltCorrectionPosition;

	private bool autoTilt;

	private PID autoTiltPID;

	public (float, float) GetAngleLimits()
	{
		return rotatingJoints[0].GetAngleLimits();
	}

	public float GetLowerAngleLimit()
	{
		return GetAngleLimits().Item1;
	}

	public float GetUpperAngleLimit()
	{
		return GetAngleLimits().Item2;
	}

	public float GetAverageAngle()
	{
		return rotatingJoints[0].GetAngle();
	}

	public float GetWingAngle()
	{
		return GetAverageAngle();
	}

	private void Awake()
	{
		autoTiltPID = new PID(autoTiltPIDFactors);
		inputs = aircraft.GetInputs();
		aircraftParameters = aircraft.GetAircraftParameters();
		aircraft.onSetFlightAssist += TiltWingController_OnSetFlightAssist;
		RotatorInput[] array = rotatingJoints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Setup();
		}
	}

	private void TiltWingController_OnSetFlightAssist(Aircraft.OnFlightAssistToggle e)
	{
		autoTilt = e.enabled;
	}

	private void FixedUpdate()
	{
		float speed = Vector3.Dot(aircraft.rb.velocity, aircraft.transform.forward);
		if ((aircraft.LocalSim && autoTilt) || !aircraft.networked)
		{
			_ = Vector3.Dot(aircraft.transform.forward, -Vector3.up) * 2f + 0.2f;
			_ = inputs.pitch;
			float num = 0.18f;
			float value = -0.04f * TargetCalc.GetAngleOnAxis(aircraft.transform.up, Vector3.up, aircraft.transform.right);
			num += Mathf.Clamp(value, -0.18f, 0.18f);
			float b = 1f;
			float num2 = tiltAtSpeed.Evaluate(aircraft.speed);
			float a = aircraft.speed / aircraftParameters.maxSpeed;
			float num3 = Mathf.Clamp(inputs.throttle - 0.8f, -0.3f, 0.1f) / Mathf.Max(a, 0.3f);
			num2 += num3 * 0.5f;
			inputs.customAxis1 = Mathf.Lerp(num, b, num2);
			if (!aircraft.networked || (aircraft.radarAlt < 1f && Mathf.Abs(inputs.pitch) < 0.2f))
			{
				inputs.customAxis1 = Mathf.Lerp(inputs.customAxis1, 0.18f, Time.fixedDeltaTime);
			}
		}
		if (aircraft.IsAutoHoverEnabled())
		{
			inputs.customAxis1 = 0.18f;
		}
		tiltCorrectionPosition += Mathf.Clamp(inputs.customAxis1 - tiltCorrectionPosition, (0f - tiltTransitionRate) * Time.fixedDeltaTime, tiltTransitionRate * Time.fixedDeltaTime);
		RotatorInput[] array = rotatingJoints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Animate(inputs, speed, tiltCorrectionPosition);
		}
	}
}
