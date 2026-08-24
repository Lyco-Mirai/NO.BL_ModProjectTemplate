using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class Airbrake : MonoBehaviour
{
	[SerializeField]
	private Transform[] transforms;

	[SerializeField]
	private float dragAmount;

	[SerializeField]
	private float maxAngle;

	[SerializeField]
	private float openSpeed;

	[SerializeField]
	private UnitPart part;

	[SerializeField]
	private AudioSource airbrakeSound;

	[SerializeField]
	private AimConstraint[] constraints;

	[SerializeField]
	[Range(0f, 2f)]
	private float volumeMultiplier;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private float maxVolumeSpeed = 340f;

	[SerializeField]
	private float pitchMin = 0.5f;

	[SerializeField]
	private float pitchMax = 2f;

	private float openAmount;

	private bool active;

	private List<Vector3> baseAngles = new List<Vector3>();

	private ControlInputs controlInputs;

	private Aircraft attachedAircraft;

	private void Start()
	{
		attachedAircraft = part.parentUnit as Aircraft;
		controlInputs = attachedAircraft.GetInputs();
		for (int i = 0; i < transforms.Length; i++)
		{
			baseAngles.Add(transforms[i].localEulerAngles);
		}
	}

	private void Update()
	{
		openAmount += ((controlInputs.throttle == 0f) ? (openSpeed * Time.deltaTime) : ((0f - openSpeed) * Time.deltaTime));
		openAmount = Mathf.Clamp01(openAmount);
		bool flag = openAmount > 0f;
		if (!active)
		{
			if (flag)
			{
				active = true;
				AimConstraint[] array = constraints;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].enabled = true;
				}
			}
		}
		else if (!flag)
		{
			AimConstraint[] array = constraints;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
		active = flag;
		if (airbrakeSound != null)
		{
			if (openAmount > 0f)
			{
				NetworkFloatHelper.LogNaN(aircraft.speed, "aircraft.speed");
				float num = Mathf.Clamp01((aircraft.speed - 5f) / maxVolumeSpeed);
				airbrakeSound.volume = Mathf.Sqrt(openAmount * num) * volumeMultiplier;
				airbrakeSound.pitch = Mathf.Lerp(pitchMin, pitchMax, num);
				if (!airbrakeSound.isPlaying)
				{
					airbrakeSound.Play();
				}
			}
			else if (airbrakeSound.isPlaying)
			{
				airbrakeSound.Stop();
			}
		}
		for (int j = 0; j < transforms.Length; j++)
		{
			transforms[j].localEulerAngles = baseAngles[j];
			transforms[j].Rotate(new Vector3(openAmount * maxAngle, 0f, 0f), Space.Self);
		}
	}

	private void FixedUpdate()
	{
		if (openAmount > 0f && !attachedAircraft.remoteSim)
		{
			float num = dragAmount * part.parentUnit.airDensity * part.rb.velocity.sqrMagnitude;
			part.rb.AddForce(openAmount * -part.rb.velocity.normalized * num);
		}
	}
}
