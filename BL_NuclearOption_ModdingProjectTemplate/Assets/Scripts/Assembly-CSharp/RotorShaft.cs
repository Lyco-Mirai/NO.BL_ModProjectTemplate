using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RotorShaft : MonoBehaviour, IEngine, IThrustSource, IPowerSource, IReportDamage, IPowerOutput
{
	[Serializable]
	private class RotorHinge
	{
		[SerializeField]
		private Transform hinge;

		[SerializeField]
		private Vector3 foldAngle;

		private float foldSpeed;

		private float foldAmount;

		public void Initialize(float foldSpeed, float foldAmount)
		{
			this.foldAmount = foldAmount;
			this.foldSpeed = UnityEngine.Random.Range(foldSpeed * 0.8f, foldSpeed * 1.2f);
			hinge.localEulerAngles = foldAngle * Mathf.Clamp01(foldAmount);
		}

		public bool Unfold()
		{
			foldAmount -= foldSpeed * Time.deltaTime;
			hinge.localEulerAngles = foldAngle * Mathf.SmoothStep(0f, 1f, foldAmount);
			return foldAmount > 0f;
		}
	}

	public enum TurnDirection
	{
		clockwise = 0,
		anticlockwise = 1
	}

	public Aircraft aircraft;

	[SerializeField]
	private Transmission transmission;

	[SerializeField]
	private Transform hubRotator;

	[SerializeField]
	private UnitPart unitPart;

	[Header("Rotors")]
	[SerializeField]
	private SwashRotor[] rotors;

	[SerializeField]
	private float bladeLength;

	[SerializeField]
	private float bladeMass;

	[SerializeField]
	private float flapSpringUp;

	[SerializeField]
	private float flapSpringDown;

	[SerializeField]
	private float flapDamp;

	[SerializeField]
	private float liftNumber;

	[SerializeField]
	private float stallAngle;

	[SerializeField]
	private float dragBase;

	[SerializeField]
	private float dragExponent;

	[SerializeField]
	private float washout;

	[SerializeField]
	private float foldSpeed;

	[SerializeField]
	private bool detachOnEject;

	[SerializeField]
	private RotorHinge[] rotorHinges;

	[Header("SwashPlate")]
	[SerializeField]
	private float hubRadius;

	[SerializeField]
	private float cyclicTravel = 5f;

	[SerializeField]
	private float collectiveTravel = 8f;

	[SerializeField]
	private float collectiveMin;

	[SerializeField]
	private float phaseLag;

	[SerializeField]
	private float yawCollective = 4f;

	private Transform swashPlate;

	[Header("Physics")]
	public bool debug;

	[SerializeField]
	private TurnDirection turnDirection;

	[SerializeField]
	private int frameSamples = 4;

	[SerializeField]
	private int radialSamples = 3;

	[SerializeField]
	private float torqueLimit;

	[SerializeField]
	private float shaftFriction;

	[SerializeField]
	private float nominalPower = 2000f;

	[SerializeField]
	private float nominalRPM = 280f;

	[SerializeField]
	private float maxInputRate = 20f;

	[SerializeField]
	private float VRSThreshold = 4f;

	[SerializeField]
	private float VRSStrength = 1f;

	private ForceAndTorque forceAndTorque;

	[Header("Audio")]
	[SerializeField]
	private AudioSource rotorSource;

	[SerializeField]
	private AudioClip exteriorSound;

	[SerializeField]
	private AudioClip interiorSound;

	[SerializeField]
	private float volume = 1f;

	[SerializeField]
	private float pitch = 1f;

	private bool disengaged;

	private bool unfolded;

	private bool detached;

	private bool damageReported;

	private int directionMult = 1;

	private float angularPosition;

	private float angularSpeed;

	private float angularSpeedNominal;

	private float angularSpeedLimit;

	private float angularSpeedRatio;

	private float availablePower;

	private float torqueFromEngine;

	private float torqueFromRotors;

	private float momentOfInertia;

	private float currentThrust;

	private float downdraft;

	private float groundEffect;

	private float VRSSmoothed;

	private float rotorDiskArea;

	private float condition = 1f;

	private float swashPlatePitch;

	private float swashPlateRoll;

	private float swashPlateCollective;

	private float startupProgress;

	private ControlInputs inputs;

	[SerializeField]
	private string failureMessage;

	[SerializeField]
	private AudioClip failureMessageAudio;

	[SerializeField]
	private AudioClip rotorStrikeSound;

	private AudioSource strikeSource;

	private Transform xform;

	Transform IEngine.transform => base.transform;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	public event Action<OnReportDamage> onReportDamage;

	private void Awake()
	{
		if (turnDirection == TurnDirection.anticlockwise)
		{
			directionMult = -1;
		}
		swashPlate = new GameObject("swashPlate").transform;
		swashPlate.transform.SetParent(base.transform);
		swashPlate.transform.localPosition = Vector3.zero;
		swashPlate.transform.localEulerAngles = new Vector3(0f, phaseLag, 0f);
		SwashRotor[] array = rotors;
		foreach (SwashRotor swashRotor in array)
		{
			swashRotor.Setup(this, aircraft.RegisterDamageable(swashRotor), directionMult, bladeLength, bladeMass, flapSpringUp, flapSpringDown, flapDamp, cyclicTravel, collectiveTravel, washout, radialSamples);
		}
		xform = base.transform;
		float distanceFromAxis = rotors[0].GetDistanceFromAxis();
		aircraft.onInitialize += RotorShaft_OnSpawnedInPosition;
		if (detachOnEject)
		{
			aircraft.onEject += RotorShaft_OnEject;
		}
		inputs = aircraft.GetInputs();
		rotorDiskArea = MathF.PI * bladeLength * bladeLength;
		unitPart.onParentDetached += RotorShaft_OnDetach;
		rotorSource.time = UnityEngine.Random.Range(0f, rotorSource.clip.length);
		angularSpeedNominal = nominalRPM * 0.10472f;
		angularSpeedLimit = angularSpeedNominal * 1.06f;
		float num = bladeLength + distanceFromAxis;
		momentOfInertia = 0.33333f * (float)rotors.Length * bladeMass * (num * num);
		angularPosition = hubRotator.transform.localEulerAngles.y * (MathF.PI / 180f);
		aircraft.engines.Add(this);
	}

	public float GetRPM()
	{
		return angularSpeed * 9.55414f;
	}

	public float GetRPMRatio()
	{
		return angularSpeed / angularSpeedNominal;
	}

	public float GetMaxRPM()
	{
		return nominalRPM * 1.06f;
	}

	public float GetVRSFactor()
	{
		return VRSSmoothed;
	}

	public float GetTorque()
	{
		return torqueFromEngine;
	}

	public float GetMaxThrust()
	{
		return 0f;
	}

	public float GetThrust()
	{
		return currentThrust;
	}

	public float GetPower()
	{
		return availablePower;
	}

	public float GetMaxPower()
	{
		return nominalPower;
	}

	public void Throttle(float throttle)
	{
	}

	public void SendPower(float power)
	{
		availablePower = power * condition;
	}

	public UnitPart GetUnitPart()
	{
		return unitPart;
	}

	public void ReportDamage()
	{
		if (!damageReported)
		{
			damageReported = true;
			this.onReportDamage?.Invoke(new OnReportDamage
			{
				failureMessage = failureMessage,
				audioReport = failureMessageAudio
			});
		}
	}

	private void RotorShaft_OnEject()
	{
		aircraft.onEject -= RotorShaft_OnEject;
		SwashRotor[] array = rotors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ShatterRotor();
		}
	}

	public void SetInteriorSounds(bool useInteriorSound)
	{
		if (rotorSource.isPlaying)
		{
			if (useInteriorSound)
			{
				rotorSource.Stop();
				rotorSource.clip = interiorSound;
				rotorSource.time = UnityEngine.Random.Range(0f, rotorSource.clip.length);
				rotorSource.Play();
			}
			else
			{
				rotorSource.Stop();
				rotorSource.clip = exteriorSound;
				rotorSource.time = UnityEngine.Random.Range(0f, rotorSource.clip.length);
				rotorSource.Play();
			}
		}
		else if (useInteriorSound)
		{
			rotorSource.clip = interiorSound;
		}
		else
		{
			rotorSource.clip = exteriorSound;
		}
	}

	private void RotorShaft_OnSpawnedInPosition()
	{
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			angularSpeed = angularSpeedLimit * 1.1f;
			unfolded = true;
			startupProgress = 1f;
			RotorHinge[] array = rotorHinges;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Initialize(foldSpeed, 0f);
			}
		}
		else
		{
			startupProgress = 0f;
			unfolded = false;
			RotorHinge[] array = rotorHinges;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Initialize(foldSpeed, 1f);
			}
			UnfoldRotors().Forget();
		}
	}

	private void RotorShaft_OnDetach(UnitPart part)
	{
		detached = true;
		rotorSource.Stop();
		condition = 0f;
		shaftFriction *= 10f;
	}

	private void AnimateRotor()
	{
		rotorSource.pitch = (detached ? 0f : (angularSpeedRatio * pitch));
		rotorSource.volume = (detached ? 0f : (angularSpeedRatio * volume * condition));
		SwashRotor[] array = rotors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BendSegments(angularSpeedRatio);
		}
		hubRotator.Rotate(new Vector3(0f, angularSpeed * (float)directionMult * 57.29578f * Time.deltaTime, 0f), Space.Self);
	}

	private async UniTask UnfoldRotors()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(1000);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		if (rotorHinges.Length == 0)
		{
			unfolded = true;
			return;
		}
		while (aircraft.rb.velocity.y > 1f)
		{
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		bool hingesMoving = true;
		while (hingesMoving)
		{
			hingesMoving = false;
			RotorHinge[] array = rotorHinges;
			foreach (RotorHinge rotorHinge in array)
			{
				hingesMoving = rotorHinge.Unfold() || hingesMoving;
			}
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		unfolded = true;
	}

	public void RotorStrike(float impactTorque)
	{
		ReportDamage();
		float f = angularSpeed;
		angularSpeed -= Mathf.Sign(angularSpeed) * impactTorque / momentOfInertia;
		if (Mathf.Sign(f) != Mathf.Sign(angularSpeed))
		{
			angularSpeed = 0f;
		}
		if (!(Mathf.Abs(angularSpeed) < 4f))
		{
			if (aircraft.LocalSim)
			{
				condition = Mathf.Max(condition - 0.5f, 0f);
			}
			if (strikeSource == null)
			{
				strikeSource = base.gameObject.AddComponent<AudioSource>();
				strikeSource.outputAudioMixerGroup = SoundManager.i.HeavyEffectsMixer;
				strikeSource.bypassListenerEffects = true;
				strikeSource.bypassEffects = true;
				strikeSource.spatialBlend = 1f;
				strikeSource.dopplerLevel = 1f;
				strikeSource.spread = 5f;
				strikeSource.maxDistance = 500f;
				strikeSource.minDistance = 10f;
				strikeSource.volume = 1f;
				strikeSource.priority = 128;
				strikeSource.clip = rotorStrikeSound;
			}
			strikeSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f) * GetRPM() / nominalRPM;
			strikeSource.volume = Mathf.Clamp01(strikeSource.pitch);
			strikeSource.Play();
		}
	}

	private void RotorPhysics()
	{
		if (!aircraft.disabled)
		{
			swashPlatePitch += Mathf.Clamp(inputs.pitch - swashPlatePitch, (0f - maxInputRate) * Time.fixedDeltaTime, maxInputRate * Time.fixedDeltaTime);
			swashPlatePitch = Mathf.Clamp(swashPlatePitch, -1f, 1f);
			swashPlateRoll += Mathf.Clamp(inputs.roll - swashPlateRoll, (0f - maxInputRate) * Time.fixedDeltaTime, maxInputRate * Time.fixedDeltaTime);
			swashPlateRoll = Mathf.Clamp(swashPlateRoll, -1f, 1f);
			float num = inputs.throttle + inputs.yaw * yawCollective;
			if (!unfolded)
			{
				num = 0f;
			}
			swashPlateCollective += Mathf.Clamp(num - swashPlateCollective, (0f - maxInputRate) * Time.fixedDeltaTime, maxInputRate * Time.fixedDeltaTime);
			swashPlateCollective = Mathf.Clamp(swashPlateCollective + collectiveMin, collectiveMin, 1.1f);
		}
		forceAndTorque.Clear();
		Vector3 vector = ((NetworkSceneSingleton<LevelInfo>.i != null) ? NetworkSceneSingleton<LevelInfo>.i.GetWind(unitPart.xform.GlobalPosition()) : Vector3.zero);
		Vector3 vector2 = vector + unitPart.rb.velocity;
		vector -= downdraft * xform.up;
		vector -= VRSSmoothed * VRSStrength * xform.up;
		float num2 = Time.fixedDeltaTime / (float)frameSamples;
		torqueFromRotors = 0f;
		angularPosition += angularSpeed * (float)directionMult * Time.fixedDeltaTime;
		if (Mathf.Abs(angularPosition) > MathF.PI * 2f)
		{
			angularPosition -= MathF.PI * 2f * Mathf.Sign(angularPosition);
		}
		hubRotator.transform.localEulerAngles = new Vector3(0f, angularPosition * 57.29578f, 0f);
		float num3 = 0f;
		for (int i = 0; i < frameSamples; i++)
		{
			hubRotator.Rotate(new Vector3(0f, angularSpeed * (float)directionMult * 57.29578f * num2, 0f), Space.Self);
			SwashRotor[] array = rotors;
			foreach (SwashRotor swashRotor in array)
			{
				swashRotor.SetPitch(swashPlate, swashPlatePitch, swashPlateRoll, swashPlateCollective, directionMult);
				forceAndTorque.Add(swashRotor.SampleForces(unitPart.rb, angularSpeed, directionMult, liftNumber, stallAngle, dragBase, dragExponent, vector, num2, out var angleOfAttack));
				num3 += angleOfAttack;
			}
		}
		if (unfolded)
		{
			SwashRotor[] array = rotors;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].CheckCollisions();
			}
		}
		num3 /= (float)(frameSamples * radialSamples);
		Vector3 vector3 = forceAndTorque.force / frameSamples;
		Vector3 vector4 = forceAndTorque.torque / frameSamples;
		torqueFromRotors = Vector3.Dot(vector4, xform.up);
		vector4 -= torqueFromRotors * xform.up;
		torqueFromRotors *= directionMult;
		currentThrust = Vector3.Dot(vector3, xform.up);
		float num4 = Mathf.Abs(Vector3.Dot(xform.forward, vector2)) + Mathf.Abs(Vector3.Dot(xform.right, vector2));
		groundEffect = Mathf.Lerp(groundEffect, Mathf.Clamp01(bladeLength / (aircraft.radarAlt * bladeLength)), Time.fixedDeltaTime);
		downdraft = Mathf.Lerp(downdraft, currentThrust * 0.04f / (rotorDiskArea * (1f + num4 * 0.1f)), Time.fixedDeltaTime);
		downdraft *= Mathf.Clamp01(1f - groundEffect * 0.15f);
		float b = Mathf.Clamp01(Mathf.Min(Vector3.Dot(vector2, -xform.up), 10f) - (VRSThreshold + num4 * 0.4f));
		VRSSmoothed = Mathf.Lerp(VRSSmoothed, b, 0.25f * Time.fixedDeltaTime);
		if (VRSSmoothed > 0.1f)
		{
			aircraft.ShakeAircraft(VRSSmoothed * 0.02f, 0f);
		}
		if (!aircraft.remoteSim && !detached)
		{
			Vector3 vector5 = unitPart.rb.transform.InverseTransformDirection(unitPart.rb.angularVelocity);
			vector4 += angularSpeed * 0.5f * momentOfInertia * ((0f - vector5.x) * unitPart.rb.transform.right - vector5.z * unitPart.rb.transform.forward);
			unitPart.rb.AddForce(vector3);
			unitPart.rb.AddTorque(vector4);
		}
	}

	private void Update()
	{
		if (Time.timeScale > 0f)
		{
			AnimateRotor();
		}
	}

	private void FixedUpdate()
	{
		angularSpeedRatio = Mathf.Max(angularSpeed / angularSpeedLimit, 0f);
		float num = torqueLimit;
		if (startupProgress < 1f)
		{
			num *= Mathf.Clamp01(startupProgress);
			if (availablePower > nominalPower * 0.1f)
			{
				startupProgress += ((angularSpeedRatio < 0.9f) ? (0.03f * Time.fixedDeltaTime) : (0.2f * Time.fixedDeltaTime));
			}
		}
		torqueFromEngine = Mathf.Min(condition * availablePower / Mathf.Max(angularSpeed, 1f), num);
		if (disengaged || !unfolded)
		{
			torqueFromEngine = 0f;
		}
		float num2 = torqueFromEngine + torqueFromRotors;
		num2 -= shaftFriction * Mathf.Clamp(angularSpeed * 20f, -1f, 1f);
		angularSpeed += num2 * Time.fixedDeltaTime / momentOfInertia;
		if (aircraft.LocalSim && !detached)
		{
			unitPart.rb.AddTorque(torqueFromEngine * (float)directionMult * -base.transform.up);
		}
		float num3 = (angularSpeedNominal - angularSpeed) / angularSpeedNominal;
		float powerRequested = Mathf.Clamp(nominalPower * (1f + num3 * 30f), 0f, nominalPower * 1.1f);
		if (disengaged)
		{
			powerRequested = 0f;
		}
		transmission.RequestPower(this, powerRequested);
		RotorPhysics();
	}
}
