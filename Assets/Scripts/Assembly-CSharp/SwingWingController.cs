using System;
using Rewired;
using UnityEngine;

public class SwingWingController : MonoBehaviour, IWingAngleGauge
{
	public enum SwingWingMode
	{
		Auto = 0,
		Manual = 1
	}

	[Serializable]
	private class RotatorInput
	{
		[Serializable]
		private class SweepAeroProperties
		{
			[SerializeField]
			private AeroPart part;

			[SerializeField]
			private float swingAreaChange;

			[SerializeField]
			private float swingDragChange;

			public void UpdateAeroProperties(float change)
			{
				part.ModifyDrag(change * swingDragChange);
				part.ModifyWingArea(change * swingAreaChange);
			}
		}

		[SerializeField]
		private AeroPart part;

		[SerializeField]
		private AeroPart[] criticalParts;

		[SerializeField]
		private Transform anchor;

		[SerializeField]
		private Transform[] counterRotators;

		[SerializeField]
		private AeroPart[] connectedParts;

		[SerializeField]
		private int solverIterations = 12;

		[SerializeField]
		private float minAngle;

		[SerializeField]
		private float maxAngle;

		[SerializeField]
		private float rotationSpeed;

		[SerializeField]
		private float customAxis1Factor;

		[SerializeField]
		private float spring;

		[SerializeField]
		private float damp;

		[SerializeField]
		private float breakStrength;

		[SerializeField]
		private float currentAngle;

		[SerializeField]
		private float damageTolerance = 1f;

		[SerializeField]
		private SweepAeroProperties[] sweepAeroProperties;

		private float baseAngle;

		private float condition = 1f;

		public (float, float) GetAngleLimits()
		{
			return (minAngle, maxAngle);
		}

		public float GetAngle()
		{
			return currentAngle;
		}

		public float GetSwingAmount()
		{
			return Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
		}

		public void Setup()
		{
			baseAngle = part.transform.localEulerAngles.x;
			AeroPart[] array = criticalParts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].onApplyDamage += RotatorInput_OnPartDamage;
			}
		}

		private void RotatorInput_OnPartDamage(UnitPart.OnApplyDamage e)
		{
			float num = e.pierceDamage + e.blastDamage + e.fireDamage + e.impactDamage;
			condition -= num * 0.01f / damageTolerance;
		}

		public void Animate(float input, out float amountMoved)
		{
			amountMoved = 0f;
			if (condition <= 0f)
			{
				return;
			}
			float num = Mathf.Clamp(Mathf.Lerp(minAngle, maxAngle, input) - currentAngle, (0f - rotationSpeed) * Time.deltaTime, rotationSpeed * Time.deltaTime);
			currentAngle += num;
			if (Mathf.Abs(num) > 0.0001f)
			{
				amountMoved = Mathf.Abs(num) / (rotationSpeed * Time.deltaTime);
				Transform[] array = counterRotators;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].transform.Rotate(new Vector3(0f, 0f, 0f - num), Space.Self);
				}
				num /= maxAngle - minAngle;
				SweepAeroProperties[] array2 = sweepAeroProperties;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].UpdateAeroProperties(num);
				}
			}
			for (int j = 0; j < connectedParts.Length; j++)
			{
				part.SetHingeJoint(j, connectedParts[j], spring, damp, currentAngle, breakStrength, baseAngle, Vector3.up, anchor, solverIterations);
			}
		}
	}

	private SwingWingMode swingWingMode;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private RotatorInput[] rotators;

	[Tooltip("Control surfaces to lock when wings are swept")]
	[SerializeField]
	private ControlSurface[] lockingControlSurfaces;

	private ControlInputs inputs;

	[SerializeField]
	private float forwardMach = 0.5f;

	[SerializeField]
	private float sweptMach = 0.9f;

	[SerializeField]
	private float axisRemapLower;

	[SerializeField]
	private float axisRemapUpper = 1f;

	[SerializeField]
	private float displayAngleMin;

	[SerializeField]
	private float displayAngleMax;

	[SerializeField]
	private bool showHUDStatus = true;

	[Header("Audio")]
	[SerializeField]
	private AudioClip swingSound;

	[SerializeField]
	[Range(0f, 2f)]
	private float volumeMultiplier;

	[SerializeField]
	private Vector2 volumePitchMinMax = new Vector2(0.5f, 1.5f);

	private AudioSource swingSource;

	private float customAxis1Prev;

	private float lastAutoToggle;

	private Player player;

	private float positionPrev;

	private float currentVolume;

	public float GetLowerAngleLimit()
	{
		return displayAngleMin;
	}

	public float GetUpperAngleLimit()
	{
		return displayAngleMax;
	}

	public float GetWingAngle()
	{
		return Mathf.Lerp(displayAngleMin, displayAngleMax, rotators[0].GetSwingAmount());
	}

	private void CheckForManualInput()
	{
		if (aircraft.Player == null)
		{
			swingWingMode = SwingWingMode.Auto;
		}
		else
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
			bool flag3 = customAxis1Prev != inputs.customAxis1;
			customAxis1Prev = inputs.customAxis1;
			if (Time.timeSinceLevelLoad - lastAutoToggle < 1f)
			{
				return;
			}
			if (swingWingMode == SwingWingMode.Manual)
			{
				if (inputs.customAxis1 < 0.02f && aircraft.speed < forwardMach * LevelInfo.GetSpeedOfSound(aircraft.GlobalPosition().y))
				{
					swingWingMode = SwingWingMode.Auto;
					inputs.customAxis1 = 0f;
					customAxis1Prev = 0f;
					lastAutoToggle = Time.timeSinceLevelLoad;
					if (showHUDStatus)
					{
						SceneSingleton<AircraftActionsReport>.i.ReportText("Wing sweep set to Auto", 4f);
					}
				}
				if (inputs.customAxis1 > 0.98f && aircraft.speed > sweptMach * LevelInfo.GetSpeedOfSound(aircraft.GlobalPosition().y))
				{
					swingWingMode = SwingWingMode.Auto;
					inputs.customAxis1 = 1f;
					customAxis1Prev = 1f;
					lastAutoToggle = Time.timeSinceLevelLoad;
					if (showHUDStatus)
					{
						SceneSingleton<AircraftActionsReport>.i.ReportText("Wing sweep set to Auto", 4f);
					}
				}
			}
			else if (flag2 && flag3)
			{
				lastAutoToggle = Time.timeSinceLevelLoad;
				swingWingMode = SwingWingMode.Manual;
				if (showHUDStatus)
				{
					SceneSingleton<AircraftActionsReport>.i.ReportText("Wing sweep set to Manual", 4f);
				}
			}
		}
	}

	private void AutoSwing()
	{
		float value = aircraft.speed / LevelInfo.GetSpeedOfSound(aircraft.GlobalPosition().y);
		inputs.customAxis1 = Mathf.InverseLerp(forwardMach, sweptMach, value);
	}

	private void Awake()
	{
		inputs = aircraft.GetInputs();
		inputs.customAxis1 = 1f;
		swingWingMode = SwingWingMode.Auto;
		player = ReInput.players.GetPlayer(0);
		RotatorInput[] array = rotators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Setup();
		}
	}

	public float GetSwingPosition()
	{
		return positionPrev;
	}

	private void CreateAudioSource()
	{
		swingSource = base.gameObject.AddComponent<AudioSource>();
		swingSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		swingSource.bypassListenerEffects = true;
		swingSource.clip = swingSound;
		swingSource.dopplerLevel = 0f;
		swingSource.minDistance = 30f;
		swingSource.maxDistance = 50f;
		swingSource.spatialBlend = 1f;
		swingSource.loop = true;
		swingSource.Play();
		aircraft.RegisterDopplerSound(swingSource);
	}

	private void Audio(float amountMoved)
	{
		currentVolume = Mathf.Clamp(amountMoved - currentVolume, currentVolume - 4f * Time.fixedDeltaTime, currentVolume + 4f * Time.fixedDeltaTime);
		if (!(currentVolume > 0f) || !(SceneSingleton<CameraStateManager>.i.followingUnit == aircraft))
		{
			return;
		}
		if (swingSource == null)
		{
			CreateAudioSource();
		}
		if (currentVolume > 0.1f)
		{
			if (!swingSource.isPlaying)
			{
				swingSource.Play();
			}
			float num = Mathf.InverseLerp(0.1f, 1f, currentVolume);
			swingSource.volume = num * volumeMultiplier;
			swingSource.pitch = Mathf.Lerp(volumePitchMinMax.x, volumePitchMinMax.y, num);
		}
		else if (swingSource.isPlaying)
		{
			swingSource.Stop();
		}
	}

	private void FixedUpdate()
	{
		CheckForManualInput();
		if (swingWingMode == SwingWingMode.Auto && aircraft.LocalSim)
		{
			AutoSwing();
		}
		if (!aircraft.networked)
		{
			inputs.customAxis1 = 0f;
		}
		float num = Mathf.InverseLerp(axisRemapLower, axisRemapUpper, inputs.customAxis1);
		if (num != positionPrev)
		{
			positionPrev = num;
			ControlSurface[] array = lockingControlSurfaces;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetLocked(num > 0f);
			}
		}
		float num2 = 0f;
		RotatorInput[] array2 = rotators;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Animate(num, out var amountMoved);
			num2 += amountMoved / (float)rotators.Length;
		}
		if (swingSound != null)
		{
			Audio(num2);
		}
	}
}
