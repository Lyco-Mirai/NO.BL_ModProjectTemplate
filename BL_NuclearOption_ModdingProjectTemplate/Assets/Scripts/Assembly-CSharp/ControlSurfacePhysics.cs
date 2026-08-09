using UnityEngine;

public class ControlSurfacePhysics : MonoBehaviour
{
	[SerializeField]
	private AeroPart[] connectedParts;

	[SerializeField]
	private float pitchRange;

	[SerializeField]
	private float rollRange;

	[SerializeField]
	private float yawRange;

	[SerializeField]
	private float servoSpeed;

	[SerializeField]
	private float spring;

	[SerializeField]
	private float damp;

	[SerializeField]
	private float breakStrength;

	[SerializeField]
	private AeroPart part;

	private float currentAngle;

	private ControlInputs inputs;

	private void Awake()
	{
		if (part.parentUnit != null && part.parentUnit is Aircraft aircraft)
		{
			inputs = aircraft.GetInputs();
		}
	}

	private void FixedUpdate()
	{
		float num = inputs.pitch * pitchRange + inputs.yaw * yawRange + inputs.roll * rollRange;
		currentAngle += Mathf.Clamp(num - currentAngle, (0f - servoSpeed) * Time.fixedDeltaTime, servoSpeed * Time.fixedDeltaTime);
		for (int i = 0; i < connectedParts.Length; i++)
		{
			part.SetHingeJoint(i, connectedParts[i], spring, damp, currentAngle, breakStrength, 0f, Vector3.right);
		}
	}
}
