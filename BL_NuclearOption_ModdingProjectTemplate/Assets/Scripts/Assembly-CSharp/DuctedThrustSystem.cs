using System;
using Rewired;
using UnityEngine;

public class DuctedThrustSystem : MonoBehaviour, INozzleGauge
{
	private enum DuctedThrustMode
	{
		Forward = 0,
		Takeoff = 1,
		Hover = 2,
		Reverse = 3,
		Manual = 4
	}

	[Serializable]
	private class IntakeDoor
	{
		private bool opening;

		private bool closing;

		[SerializeField]
		private Transform hinge;

		[SerializeField]
		private float speed;

		[SerializeField]
		private Vector3 openAngle;

		[SerializeField]
		private AudioSource intakeSound;

		[SerializeField]
		private float pitchMin;

		[SerializeField]
		private float pitchMax;

		[SerializeField]
		[Range(0f, 2f)]
		private float volumeMultiplier;

		private float openAmount;

		public void Open()
		{
			opening = true;
			closing = false;
		}

		public void Close()
		{
			opening = false;
			closing = true;
		}

		public void Animate(float thrustRatio)
		{
			if (intakeSound != null)
			{
				if (openAmount > 0f)
				{
					intakeSound.volume = openAmount * thrustRatio * volumeMultiplier;
					intakeSound.pitch = Mathf.Lerp(pitchMin, pitchMax, openAmount * thrustRatio);
					if (!intakeSound.isPlaying)
					{
						intakeSound.Play();
					}
				}
				else if (intakeSound.isPlaying)
				{
					intakeSound.Stop();
				}
			}
			if (opening || closing)
			{
				openAmount += (opening ? (speed * Time.deltaTime) : ((0f - speed) * Time.deltaTime));
				if (openAmount > 1f || openAmount < 0f)
				{
					opening = false;
					closing = false;
					openAmount = Mathf.Clamp01(openAmount);
				}
				hinge.localEulerAngles = openAngle * openAmount;
			}
		}
	}

	[Serializable]
	private class SwivelTransform
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private UnitPart part;

		[SerializeField]
		private float swivelRate;

		[SerializeField]
		private float yawSwivel;

		[SerializeField]
		private float swivelDegrees;

		[SerializeField]
		private float swivelGain = 1f;

		[Header("Audio")]
		[SerializeField]
		private AudioSource swivelAudio;

		[SerializeField]
		private float swivelPitchBase;

		[SerializeField]
		private float swivelPitchFactor;

		[SerializeField]
		[Range(0f, 1f)]
		private float volumeMultiplier;

		private float currentSwivel;

		private float swivelVolume;

		private float smoothVel;

		public void Aim(ControlInputs inputs, Vector3 aimDirection)
		{
			aimDirection += yawSwivel * inputs.yaw * -part.xform.forward;
			float num = Mathf.Clamp01((0f - TargetCalc.GetAngleOnAxis(aimDirection, -part.xform.forward, part.xform.right)) / swivelDegrees) - currentSwivel;
			currentSwivel += Mathf.Clamp(num, (0f - swivelRate) * Time.fixedDeltaTime, swivelRate * Time.fixedDeltaTime);
			if (swivelAudio != null)
			{
				int num2 = ((Mathf.Abs(num) > swivelRate * 0.9f * Time.fixedDeltaTime) ? 1 : 0);
				swivelVolume = FastMath.SmoothDamp(swivelVolume, num2, ref smoothVel, 0.1f);
				swivelAudio.volume = swivelVolume * volumeMultiplier;
				swivelAudio.pitch = swivelPitchBase + swivelVolume * swivelPitchFactor;
			}
			if (num != 0f)
			{
				transform.localEulerAngles = new Vector3(swivelDegrees * currentSwivel * swivelGain, 0f, 0f);
			}
		}
	}

	private DuctedThrustMode mode;

	[SerializeField]
	private AnimationCurve thrustAtAirspeed;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private JetNozzle[] nozzles;

	[SerializeField]
	private SwivelTransform[] swivelTransforms;

	[SerializeField]
	private Turbojet[] turbojets;

	[SerializeField]
	private Turbofan[] turbofans;

	[SerializeField]
	private float swivelRate;

	[SerializeField]
	private float swivelLimit = 120f;

	[SerializeField]
	private float minSpeedForForward = 60f;

	[SerializeField]
	private float maxReverseSpeed = 100f;

	[SerializeField]
	private IntakeDoor[] intakeDoors;

	[SerializeField]
	private float intakeDoorSpeedThreshold;

	[SerializeField]
	private bool autoReverseOnlyWhenGearDown;

	[HideInInspector]
	public float angle;

	private bool intakeDoorsOpen;

	private float lastAirborne = -100f;

	private float timeLanded;

	private float takeoffSpeed;

	private float angleTarget;

	private float thrustRatio;

	private float customAxis1Prev;

	private float lastAutoToggle;

	private ControlInputs inputs;

	private Player player;

	private void Awake()
	{
		inputs = aircraft.GetInputs();
		inputs.customAxis1 = 1f;
		takeoffSpeed = aircraft.GetAircraftParameters().takeoffSpeed;
		player = ReInput.players.GetPlayer(0);
		mode = DuctedThrustMode.Forward;
	}

	public float GetNozzleAngle()
	{
		return angle;
	}

	public float GetMaxThrust()
	{
		return thrustAtAirspeed.Evaluate(0f);
	}

	private void AutoAngle()
	{
		DuctedThrustMode num = mode;
		float num2 = aircraft.speed / takeoffSpeed;
		float num3 = aircraft.radarAlt - aircraft.definition.spawnOffset.y;
		if (num3 > 1f)
		{
			lastAirborne = Time.timeSinceLevelLoad;
		}
		if (aircraft.gearDeployed)
		{
			if (num2 < 0.1f && (num3 < 1f || inputs.throttle > 0.1f))
			{
				mode = DuctedThrustMode.Forward;
			}
			if (num2 > 0.35f && inputs.throttle > 0.9f)
			{
				mode = DuctedThrustMode.Takeoff;
			}
			if (num2 > 0.5f && inputs.throttle < 0.9f)
			{
				mode = DuctedThrustMode.Reverse;
			}
			if (num2 < 0.5f && Time.timeSinceLevelLoad - lastAirborne < 8f)
			{
				mode = DuctedThrustMode.Hover;
			}
		}
		else
		{
			if (inputs.throttle > 0.5f)
			{
				mode = DuctedThrustMode.Forward;
			}
			if (inputs.throttle < 0.5f)
			{
				mode = DuctedThrustMode.Reverse;
			}
		}
		if (num != mode && aircraft == SceneSingleton<CombatHUD>.i.aircraft)
		{
			SceneSingleton<AircraftActionsReport>.i.ReportText($"Vectoring Mode set to {mode}", 4f);
		}
		if (mode == DuctedThrustMode.Forward)
		{
			angleTarget = 0f;
		}
		if (mode == DuctedThrustMode.Takeoff)
		{
			angleTarget = 45f;
		}
		if (mode == DuctedThrustMode.Hover)
		{
			angleTarget = 90f;
		}
		if (mode == DuctedThrustMode.Reverse)
		{
			angleTarget = swivelLimit;
		}
		inputs.customAxis1 = 1f - angleTarget / swivelLimit;
	}

	private void Swivel()
	{
		Vector3 forward = aircraft.transform.forward;
		forward.y = 0f;
		angleTarget = (1f - inputs.customAxis1) * swivelLimit;
		if (aircraft.radarAlt > 1f && aircraft.speed < minSpeedForForward)
		{
			angleTarget = Mathf.Max(angleTarget, 45f);
		}
		angle += Mathf.Clamp(angleTarget - angle, -40f * Time.deltaTime, 40f * Time.deltaTime);
		angle = Mathf.Clamp(angle, 0f, swivelLimit);
		Vector3 aimDirection = -aircraft.transform.forward * Mathf.Cos(angle * (MathF.PI / 180f)) + -aircraft.transform.up * Mathf.Sin(angle * (MathF.PI / 180f));
		thrustRatio = 0f;
		float num = 0f;
		Turbojet[] array = turbojets;
		foreach (Turbojet turbojet in array)
		{
			thrustRatio += turbojet.GetThrustRatio();
			num += turbojet.GetRPMRatio();
		}
		Turbofan[] array2 = turbofans;
		foreach (Turbofan turbofan in array2)
		{
			thrustRatio += turbofan.GetThrustRatio();
			num += turbofan.GetRPMRatio();
		}
		if (turbojets.Length != 0)
		{
			thrustRatio /= turbojets.Length;
			num /= (float)turbojets.Length;
		}
		if (turbofans.Length != 0)
		{
			thrustRatio /= turbofans.Length;
			num /= (float)turbofans.Length;
		}
		float num2 = thrustRatio * thrustAtAirspeed.Evaluate(aircraft.speed);
		float num3 = 0f;
		JetNozzle[] array3 = nozzles;
		foreach (JetNozzle jetNozzle in array3)
		{
			num3 += jetNozzle.GetPriority(inputs);
		}
		SwivelTransform[] array4 = swivelTransforms;
		for (int i = 0; i < array4.Length; i++)
		{
			array4[i].Aim(inputs, aimDirection);
		}
		array3 = nozzles;
		foreach (JetNozzle jetNozzle2 in array3)
		{
			if (num3 != 0f)
			{
				float thrustAmount = jetNozzle2.priority / num3 * num2;
				jetNozzle2.Thrust(thrustAmount, num, thrustRatio, inputs.throttle, allowAfterburner: false);
			}
		}
	}

	private void SwitchMode(DuctedThrustMode newMode)
	{
		if (mode != newMode)
		{
			mode = newMode;
			if (GameManager.IsLocalAircraft(aircraft))
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText($"Vectoring Mode set to {mode}", 4f);
			}
		}
	}

	private void ForwardMode()
	{
		inputs.customAxis1 = 1f + inputs.pitch * 0.5f;
		float num = (aircraft.gearDeployed ? 0.9f : 0.5f);
		if (aircraft.radarAlt > 5f && inputs.throttle < num && aircraft.speed < maxReverseSpeed && (aircraft.gearDeployed || !autoReverseOnlyWhenGearDown))
		{
			SwitchMode(DuctedThrustMode.Reverse);
		}
		if (aircraft.radarAlt < 1f && aircraft.speed < takeoffSpeed * 1.5f)
		{
			float num2 = Mathf.Lerp(0f, 45f, (aircraft.speed - 20f) / (takeoffSpeed - 20f));
			inputs.customAxis1 = Mathf.Min(inputs.customAxis1, 1f - num2 / swivelLimit);
			if (inputs.throttle > 0.9f && aircraft.speed > takeoffSpeed)
			{
				SwitchMode(DuctedThrustMode.Takeoff);
			}
		}
	}

	private void ReverseMode()
	{
		inputs.customAxis1 = 0f;
		float num = (aircraft.gearDeployed ? 0.9f : 0.5f);
		if (inputs.throttle > num)
		{
			SwitchMode(DuctedThrustMode.Forward);
		}
		if ((aircraft.speed < minSpeedForForward) & (Vector3.Dot(aircraft.transform.forward, Vector3.up) > 0.05f))
		{
			SwitchMode(DuctedThrustMode.Hover);
		}
	}

	private void TakeoffMode()
	{
		inputs.customAxis1 = 1f - 45f / swivelLimit;
		if (!aircraft.gearDeployed && aircraft.radarAlt > 10f && aircraft.speed > minSpeedForForward * 1.2f && inputs.throttle > 0.99f)
		{
			SwitchMode(DuctedThrustMode.Forward);
		}
	}

	private void HoverMode()
	{
		inputs.customAxis1 = 1f - 90f / swivelLimit;
		if (aircraft.IsAutoHoverEnabled())
		{
			return;
		}
		if (inputs.throttle > 0.99f && Vector3.Dot(aircraft.transform.forward, -Vector3.up) > 0.05f)
		{
			timeLanded = 0f;
			SwitchMode(DuctedThrustMode.Takeoff);
		}
		if (aircraft.speed < 4f && aircraft.radarAlt < 0.1f)
		{
			timeLanded += Time.fixedDeltaTime;
			if (timeLanded > 2f)
			{
				SwitchMode(DuctedThrustMode.Forward);
				timeLanded = 0f;
			}
		}
	}

	private void ChooseOperatingMode()
	{
		if (!GameManager.IsLocalAircraft(aircraft))
		{
			return;
		}
		float num = Mathf.Clamp(player.GetAxisRaw("Custom Axis 1"), -1f, 1f);
		float num2 = Mathf.Clamp(player.GetAxisRawPrev("Custom Axis 1"), -1f, 1f);
		float num3 = Mathf.Abs(num - num2);
		bool flag = player.GetButton("Axis Modifier") && player.GetAxisRaw("Throttle") != 0f;
		bool flag2 = (num3 > 0f && num3 < 0.5f) || Mathf.Abs(num) > 0.5f || flag;
		bool flag3 = Mathf.Clamp01(customAxis1Prev) != Mathf.Clamp01(inputs.customAxis1);
		customAxis1Prev = inputs.customAxis1;
		if (mode == DuctedThrustMode.Manual)
		{
			if (flag2 && flag3 && Time.timeSinceLevelLoad - lastAutoToggle > 1f)
			{
				if (!aircraft.gearDeployed && inputs.customAxis1 == 1f && inputs.throttle > 0.9f)
				{
					SwitchMode(DuctedThrustMode.Forward);
					lastAutoToggle = Time.timeSinceLevelLoad;
				}
				if (aircraft.speed < 1f && aircraft.radarAlt < 1f && inputs.customAxis1 == 1f)
				{
					SwitchMode(DuctedThrustMode.Forward);
					lastAutoToggle = Time.timeSinceLevelLoad;
				}
			}
			return;
		}
		if (flag2 && flag3 && Time.timeSinceLevelLoad - lastAutoToggle > 1f)
		{
			lastAutoToggle = Time.timeSinceLevelLoad;
			SwitchMode(DuctedThrustMode.Manual);
			return;
		}
		switch (mode)
		{
		case DuctedThrustMode.Forward:
			ForwardMode();
			break;
		case DuctedThrustMode.Reverse:
			ReverseMode();
			break;
		case DuctedThrustMode.Takeoff:
			TakeoffMode();
			break;
		case DuctedThrustMode.Hover:
			HoverMode();
			break;
		}
	}

	private void Update()
	{
		IntakeDoor[] array;
		if (aircraft.speed < intakeDoorSpeedThreshold && !intakeDoorsOpen)
		{
			intakeDoorsOpen = true;
			array = intakeDoors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Open();
			}
		}
		if (aircraft.speed > intakeDoorSpeedThreshold && intakeDoorsOpen)
		{
			intakeDoorsOpen = false;
			array = intakeDoors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Close();
			}
		}
		array = intakeDoors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Animate(thrustRatio);
		}
	}

	private void FixedUpdate()
	{
		ChooseOperatingMode();
		Swivel();
	}
}
