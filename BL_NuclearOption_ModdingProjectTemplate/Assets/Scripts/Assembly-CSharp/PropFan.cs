using System;
using UnityEngine;

public class PropFan : MonoBehaviour, IEngine, IPowerOutput, IReportDamage
{
	[Serializable]
	private class Prop
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private bool antiClockwise;

		[SerializeField]
		private Renderer propRenderer;

		[SerializeField]
		private Renderer diskRenderer;

		[SerializeField]
		private MeshFilter propMeshFilter;

		[SerializeField]
		private MeshFilter diskMeshFilter;

		[SerializeField]
		private Mesh[] diskMeshes;

		[SerializeField]
		private Mesh damageMesh;

		private bool damaged;

		public void Animate(float rpm, float rpmRatio)
		{
			if (TimeScaleManager.Scale == 0f)
			{
				return;
			}
			float num = rpm * 0.01666667f * 360f * (antiClockwise ? 1f : (-1f));
			transform.Rotate(num * Time.deltaTime * Vector3.forward);
			if (!damaged)
			{
				bool flag = rpmRatio * Time.timeScale > 0.1f;
				propRenderer.enabled = !flag;
				diskRenderer.enabled = flag;
				if (flag)
				{
					diskMeshFilter.mesh = ((rpmRatio > 0.55f) ? diskMeshes[1] : diskMeshes[0]);
				}
			}
		}

		public void Damage()
		{
			propRenderer.enabled = true;
			diskRenderer.enabled = false;
			propMeshFilter.mesh = damageMesh;
			damaged = true;
		}
	}

	[SerializeField]
	private Transmission transmission;

	[SerializeField]
	private AnimationCurve efficiencyCurve;

	[SerializeField]
	private UnitPart part;

	[SerializeField]
	private AudioSource source;

	[SerializeField]
	private AudioClip interiorSound;

	[SerializeField]
	private AudioClip exteriorSound;

	[SerializeField]
	private float volumeBase = 0.2f;

	[SerializeField]
	private float rpmVolumePortion = 0.5f;

	[SerializeField]
	private float aoaVolumePortion = 0.3f;

	[SerializeField]
	private float pitchMultiplier = 1f;

	[SerializeField]
	private float rpmPitchPortion = 0.9f;

	[SerializeField]
	private float aoaPitchPortion = 0.1f;

	[SerializeField]
	private Prop[] props;

	[SerializeField]
	private PropStrikeDetector propStrike;

	private ControlInputs inputs;

	[SerializeField]
	private Transform thrustTransform;

	[SerializeField]
	private float area;

	[SerializeField]
	private float nominalRPM;

	[SerializeField]
	private float startTime;

	[SerializeField]
	private string failureMessage;

	[SerializeField]
	private AudioClip failureMessageAudio;

	private Aircraft aircraft;

	private float availablePower;

	private float nominalPower;

	private float rpmRatio;

	private float powerRatio;

	private float currentRPM;

	private float condition;

	private float thrust;

	private bool damageReported;

	Transform IEngine.transform => base.transform;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	public event Action<OnReportDamage> onReportDamage;

	private void Awake()
	{
		aircraft = part.parentUnit as Aircraft;
		inputs = aircraft.GetInputs();
		aircraft.onInitialize += PropFan_OnInitialize;
		condition = 1f;
		if (propStrike != null)
		{
			propStrike.OnStrike += PropFan_OnPropStrike;
		}
		aircraft.engines.Add(this);
	}

	private void PropFan_OnPropStrike(float distance)
	{
		currentRPM *= 0.5f;
		condition = 0f;
		if (!damageReported && !part.IsDetached())
		{
			damageReported = true;
			Prop[] array = props;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Damage();
			}
			this.onReportDamage?.Invoke(new OnReportDamage
			{
				failureMessage = failureMessage,
				audioReport = failureMessageAudio
			});
		}
	}

	private void PropFan_OnInitialize()
	{
		if (aircraft.radarAlt > 1f)
		{
			currentRPM = nominalRPM;
		}
	}

	private void Start()
	{
		nominalPower = transmission.GetMaxPower();
	}

	public void SendPower(float power)
	{
		availablePower += power;
	}

	public float GetMaxThrust()
	{
		return 0f;
	}

	public float GetThrust()
	{
		return thrust;
	}

	public float GetRPM()
	{
		return currentRPM;
	}

	public float GetRPMRatio()
	{
		return rpmRatio;
	}

	public void SetInteriorSounds(bool useInteriorSound)
	{
		if (source.isPlaying)
		{
			if (useInteriorSound)
			{
				source.Stop();
				source.clip = interiorSound;
				source.time = UnityEngine.Random.Range(0f, source.clip.length);
				source.Play();
			}
			else
			{
				source.Stop();
				source.clip = exteriorSound;
				source.time = UnityEngine.Random.Range(0f, source.clip.length);
				source.Play();
			}
		}
		else if (useInteriorSound)
		{
			source.clip = interiorSound;
		}
		else
		{
			source.clip = exteriorSound;
		}
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

	private void Update()
	{
		Prop[] array = props;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Animate(currentRPM, rpmRatio);
		}
	}

	private void FixedUpdate()
	{
		Vector3 vector = aircraft.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
		float powerRequested = nominalPower * Mathf.Max(inputs.throttle, 0.1f) * condition;
		float num = efficiencyCurve.Evaluate(vector.magnitude);
		rpmRatio = currentRPM / nominalRPM;
		availablePower *= condition;
		powerRatio = availablePower / nominalPower;
		if (currentRPM < nominalRPM && availablePower > 0f)
		{
			powerRequested = nominalPower;
			float num2 = Mathf.Clamp(1f - Mathf.Abs(rpmRatio - 0.5f) * 2f, 0.2f, 2f);
			currentRPM += nominalRPM / startTime * num2 * powerRatio * Time.fixedDeltaTime;
			currentRPM = Mathf.Min(currentRPM, nominalRPM);
		}
		if (availablePower == 0f)
		{
			float num3 = Mathf.Lerp(1f, 0.1f, condition);
			currentRPM -= Mathf.Max(Mathf.Sqrt(rpmRatio), 0.005f) * 3f * num3 * (nominalRPM / startTime) * Time.fixedDeltaTime;
			currentRPM = Mathf.Max(currentRPM, 0f);
		}
		float num4 = Mathf.Min(powerRatio, inputs.throttle);
		float airDensity = aircraft.GetAirDensity();
		thrust = Mathf.Pow(2f * airDensity * area * availablePower * availablePower, 0.33333f) * num * num4;
		if (aircraft.LocalSim)
		{
			part.rb.AddForceAtPosition(thrust * rpmRatio * thrustTransform.forward, thrustTransform.position);
		}
		source.volume = volumeBase + rpmVolumePortion * rpmRatio + aoaVolumePortion * num4 * rpmRatio;
		source.pitch = pitchMultiplier * (rpmPitchPortion * rpmRatio + aoaPitchPortion * num4 * rpmRatio);
		transmission.RequestPower(this, powerRequested);
		availablePower = 0f;
	}
}
