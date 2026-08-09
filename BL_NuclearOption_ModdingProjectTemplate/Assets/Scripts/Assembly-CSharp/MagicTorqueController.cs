using UnityEngine;

public class MagicTorqueController : MonoBehaviour
{
	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Vector3 gain;

	[SerializeField]
	private Vector3 slipDamping;

	[SerializeField]
	private Vector3 slipDampingForceLimit;

	private ControlInputs inputs;

	[SerializeField]
	private UnitPart[] actingParts;

	[SerializeField]
	private float powerConsumption;

	private float powerRatio;

	private PowerSupply powerSupply;

	private void Awake()
	{
		inputs = aircraft.GetInputs();
		powerSupply = aircraft.GetPowerSupply();
		powerSupply.AddUser();
	}

	private void FixedUpdate()
	{
		if (aircraft.disabled)
		{
			return;
		}
		Vector3 vector = aircraft.cockpit.xform.InverseTransformVector(aircraft.cockpit.rb.velocity);
		float num = 1f - inputs.throttle;
		vector.z *= num * num * num;
		vector.y *= aircraft.speed * 0.005f;
		Vector3 vector2 = Vector3.Scale(slipDamping, -vector);
		vector2 = new Vector3(Mathf.Clamp(vector2.x, 0f - slipDampingForceLimit.x, slipDampingForceLimit.x), Mathf.Clamp(vector2.y, 0f - slipDampingForceLimit.y, slipDampingForceLimit.y), Mathf.Clamp(vector2.z, 0f - slipDampingForceLimit.z, slipDampingForceLimit.z));
		float num2 = 0f;
		UnitPart[] array = actingParts;
		foreach (UnitPart unitPart in array)
		{
			if (!unitPart.IsDetached())
			{
				unitPart.rb.AddRelativeTorque(inputs.pitch * gain.x * powerRatio, inputs.yaw * gain.y * powerRatio, inputs.roll * gain.z * powerRatio);
				num2 += (Mathf.Abs(inputs.pitch * gain.x) + Mathf.Abs(inputs.yaw * gain.y) + Mathf.Abs(inputs.roll * gain.z)) * powerConsumption;
				unitPart.rb.AddRelativeForce(vector2 * powerRatio);
				num2 += (Mathf.Abs(vector2.x) + Mathf.Abs(vector2.y) + Mathf.Abs(vector2.z)) * powerConsumption;
			}
		}
		powerRatio = Mathf.Clamp01(powerSupply.DrawPower(num2) / Mathf.Max(num2, 0.1f));
	}
}
