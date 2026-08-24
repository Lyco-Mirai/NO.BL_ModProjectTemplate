using System;
using UnityEngine;

public class ReactionControlSystem : MonoBehaviour
{
	[Serializable]
	private class Thruster
	{
		[SerializeField]
		private AeroPart part;

		[SerializeField]
		private Transform transform;

		[SerializeField]
		private float thrustMin;

		[SerializeField]
		private float thrustMax;

		[SerializeField]
		private Vector3 axisControlFactor;

		public void Thrust(ControlInputs inputs, float thrustRatio)
		{
			if (!part.IsDetached())
			{
				float num = inputs.pitch * axisControlFactor.x + inputs.yaw * axisControlFactor.y + inputs.roll * axisControlFactor.z;
				num = num * 0.5f + 0.5f;
				part.rb.AddForceAtPosition(Mathf.Lerp(thrustMin, thrustMax, num * thrustRatio) * transform.forward, transform.position);
			}
		}
	}

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Turbofan turbofan;

	[SerializeField]
	private float maxSpeed = 100f;

	[SerializeField]
	private Thruster[] thrusters;

	private ControlInputs inputs;

	private void Awake()
	{
		inputs = aircraft.GetInputs();
	}

	private void FixedUpdate()
	{
		if (!aircraft.LocalSim || aircraft.speed > maxSpeed)
		{
			return;
		}
		float thrustRatio = turbofan.GetThrustRatio();
		if (thrustRatio != 0f)
		{
			Thruster[] array = thrusters;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Thrust(inputs, thrustRatio);
			}
		}
	}
}
