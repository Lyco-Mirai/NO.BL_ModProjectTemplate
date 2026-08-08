using System;
using UnityEngine;

public class HeloControlsFilter : ControlsFilter
{
	[Serializable]
	private class HeloFlyByWire
	{
		public bool Enabled;

		[SerializeField]
		private float gLimit = 3f;

		[SerializeField]
		private Vector3 directControlFactor = new Vector3(0.7f, 0.7f, 0.7f);

		[SerializeField]
		private Vector3 maxAngularVel = new Vector3(1f, 2f, 2f);

		[SerializeField]
		private Vector3 pFactor = new Vector3(2f, 2f, 2f);

		[SerializeField]
		private Vector3 dFactor = new Vector3(0.01f, 0.01f, 0.01f);

		[SerializeField]
		private float yawWeathervaneStrength = 0.4f;

		[SerializeField]
		private float yawWeathervaneMinSpeed = 40f;

		[SerializeField]
		private float yawWeathervaneMaxSpeed = 60f;

		private Vector3 pPrev;

		private Vector3 compensator;

		public void Filter(Aircraft aircraft, ControlInputs inputs, Vector3 localAngularVelocity)
		{
			float num = gLimit * 9.81f / Mathf.Max(aircraft.speed, 10f);
			Vector3 vector = new Vector3(Mathf.Clamp(inputs.pitch * maxAngularVel.x, 0f - num, num), inputs.yaw * maxAngularVel.y, (0f - inputs.roll) * maxAngularVel.z);
			Vector3 vector2 = localAngularVelocity - vector;
			Vector3 a = (vector2 - pPrev) / Time.fixedDeltaTime;
			pPrev = vector2;
			if (yawWeathervaneStrength > 0f && aircraft.speed > yawWeathervaneMinSpeed)
			{
				float num2 = Mathf.Clamp01((aircraft.speed - yawWeathervaneMinSpeed) / (yawWeathervaneMaxSpeed - yawWeathervaneMinSpeed));
				float num3 = TargetCalc.GetAngleOnAxis(aircraft.rb.velocity, aircraft.cockpit.xform.forward, aircraft.cockpit.xform.up) * 0.1f;
				vector2.y += num3 * yawWeathervaneStrength * num2;
			}
			compensator += -(Vector3.Scale(vector2, pFactor) + Vector3.Scale(a, dFactor)) * Time.fixedDeltaTime;
			compensator.x = Mathf.Clamp(compensator.x, -1f, 1f);
			compensator.y = Mathf.Clamp(compensator.y, -1f, 1f);
			compensator.z = Mathf.Clamp(compensator.z, -1f, 1f);
			if (aircraft.radarAlt < 0.5f)
			{
				compensator = Vector3.zero;
			}
			inputs.pitch = Mathf.Clamp((0f - vector2.x) * directControlFactor.x + compensator.x, -1f, 1f);
			inputs.yaw = Mathf.Clamp((0f - vector2.y) * directControlFactor.y + compensator.y, -1f, 1f);
			inputs.roll = 0f - Mathf.Clamp((0f - vector2.z) * directControlFactor.z + compensator.z, -1f, 1f);
		}
	}

	[SerializeField]
	private HeloFlyByWire heloFlyByWire;

	[SerializeField]
	private RotorShaft rotorShaft;

	[SerializeField]
	private DuctedFan tailRotor;

	[SerializeField]
	private float tailRotorDist;

	public override void Filter(ControlInputs inputs, Vector3 rawInputs, Rigidbody rb, float gForce, bool flightAssist)
	{
		gearDownSmoothed = FastMath.SmoothDamp(gearDownSmoothed, aircraft.gearDeployed ? 1 : 0, ref gearDownSmoothingVel, 2f);
		Vector3 localAngularVelocity = rb.transform.InverseTransformDirection(rb.angularVelocity);
		if (tailRotor != null)
		{
			tailRotor.SetDesiredBaseThrust(rotorShaft.GetTorque() / tailRotorDist);
		}
		autoHover.Hover(inputs, aircraft);
		if (heloFlyByWire.Enabled)
		{
			heloFlyByWire.Filter(aircraft, inputs, localAngularVelocity);
		}
	}

	public RotorShaft GetRotorShaft()
	{
		return rotorShaft;
	}
}
