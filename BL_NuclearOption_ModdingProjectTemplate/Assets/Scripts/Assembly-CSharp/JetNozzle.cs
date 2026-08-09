using System;
using UnityEngine;

public class JetNozzle : MonoBehaviour
{
	[Serializable]
	private class Afterburner
	{
		[SerializeField]
		private Renderer flameRenderer;

		[SerializeField]
		private Renderer nozzleGlowRenderer;

		[SerializeField]
		private Transform thrustDirection;

		[SerializeField]
		private AudioSource source;

		[SerializeField]
		private float smoothing;

		[SerializeField]
		private float throttleStart = 99.8f;

		[SerializeField]
		private float throttleEnd = 100f;

		[SerializeField]
		private float thrust;

		[SerializeField]
		private float fuelConsumption;

		[SerializeField]
		private float flameBrightness;

		[SerializeField]
		private float nozzleGlowBrightness;

		[SerializeField]
		private float temperature;

		[SerializeField]
		private float IRIntensity = 1f;

		public float afterburnerAmount;

		public void Run(float throttleAmount, float camFacing, float directionalVolumeMult)
		{
			float b = Mathf.Clamp01((throttleAmount - throttleStart) / (throttleEnd - throttleStart));
			afterburnerAmount = Mathf.Lerp(afterburnerAmount, b, smoothing * Time.deltaTime);
			nozzleGlowRenderer.material.SetColor("_EmissionColor", Color.white * afterburnerAmount * nozzleGlowBrightness);
			if (afterburnerAmount < 0.01f)
			{
				if (flameRenderer.enabled)
				{
					flameRenderer.enabled = false;
				}
				source.volume = 0f;
				return;
			}
			if (!flameRenderer.enabled)
			{
				flameRenderer.enabled = true;
			}
			flameRenderer.material.SetFloat("_Brightness", Mathf.Lerp(0f, flameBrightness, afterburnerAmount));
			flameRenderer.material.SetFloat("_Temperature", temperature * afterburnerAmount);
			flameRenderer.transform.localScale = new Vector3(1f, 1f, 1f + Mathf.PerlinNoise1D(Time.timeSinceLevelLoad * 15f) * 0.05f);
			Audio(camFacing, directionalVolumeMult);
		}

		private void Audio(float camFacing, float directionalVolumeMult)
		{
			source.volume = afterburnerAmount * directionalVolumeMult;
			if (source.dopplerLevel > 0f)
			{
				source.dopplerLevel = Mathf.Max(1f - camFacing * 2f, 0.01f);
			}
		}

		public float GetIRIntensity()
		{
			return IRIntensity * afterburnerAmount;
		}

		public float GetMaxIRIntensity()
		{
			return IRIntensity;
		}

		public void DisableAudio()
		{
			if (source.isPlaying)
			{
				source.Stop();
			}
		}

		public void EnableAudio()
		{
			if (!source.isPlaying && afterburnerAmount > 0.01f)
			{
				source.Play();
			}
		}

		public float GetFuelConsumption()
		{
			return afterburnerAmount * fuelConsumption;
		}

		public float GetMaxThrust()
		{
			return thrust;
		}

		public float GetThrust()
		{
			return afterburnerAmount * thrust;
		}
	}

	[Serializable]
	private struct JetParticleParameters
	{
		public ParticleSystem system;

		private ParticleSystem.MainModule main;

		private ParticleSystem.EmissionModule emit;

		[SerializeField]
		private float sizeMin;

		[SerializeField]
		private float sizeMax;

		[SerializeField]
		private float speedMin;

		[SerializeField]
		private float speedMax;

		[SerializeField]
		private float opacityMin;

		[SerializeField]
		private float opacityMax;

		[SerializeField]
		private float lifeMin;

		[SerializeField]
		private float lifeMax;

		[SerializeField]
		private float airspeedLifeFactor;

		[SerializeField]
		private float rateMin;

		[SerializeField]
		private float rateMax;

		public void Initialize()
		{
			main = system.main;
			emit = system.emission;
		}

		public void UpdateParticles(float thrustRatio, float rpmRatio, float airspeed)
		{
			if (rpmRatio < 0.2f && system.isPlaying)
			{
				system.Stop();
			}
			if (rpmRatio > 0.2f && !system.isPlaying)
			{
				system.Play();
			}
			main.startColor = new Color(1f, 1f, 1f, Mathf.Lerp(opacityMin, opacityMax, thrustRatio));
			main.startSize = Mathf.Lerp(sizeMin, sizeMax, thrustRatio);
			main.startLifetime = Mathf.Lerp(lifeMin, lifeMax, thrustRatio) / (1f + airspeed * airspeedLifeFactor);
			main.startSpeed = Mathf.Lerp(speedMin, speedMax, thrustRatio);
			emit.rateOverTime = Mathf.Lerp(rateMin, rateMax, thrustRatio);
		}
	}

	[SerializeField]
	private UnitPart part;

	[SerializeField]
	private Turbojet turbojet;

	[SerializeField]
	private GameObject engine;

	[SerializeField]
	private GameObject failureEffect;

	[SerializeField]
	private Transform thrustTransform;

	[SerializeField]
	private JetParticleParameters heatHaze;

	[SerializeField]
	private float thrustProportion;

	[SerializeField]
	private float pitchThrust;

	[SerializeField]
	private float rollThrust;

	[SerializeField]
	private float thrustMaxVolume;

	[SerializeField]
	private float IRMin;

	[SerializeField]
	private float IRMax;

	[SerializeField]
	private ParticleSystem glow;

	[SerializeField]
	private Transform[] vectorTransforms;

	[SerializeField]
	private Afterburner[] afterburners;

	[SerializeField]
	private AudioSource thrustAudio;

	private Aircraft aircraft;

	private float totalThrust;

	private float thrustRatio;

	private float rpmRatio;

	private float directionalVolumeMult;

	[HideInInspector]
	public float priority;

	private float fuelConsumption;

	private IRSource irSource;

	private DamageParticles damageParticles;

	private float camFacing;

	private float camDistance;

	private void Awake()
	{
		if (heatHaze.system != null)
		{
			heatHaze.Initialize();
		}
		thrustAudio.time = UnityEngine.Random.Range(0, 2);
		aircraft = part.parentUnit as Aircraft;
		CreateIRSource();
		if (turbojet != null)
		{
			turbojet.OnEngineDisable += JetNozzle_OnEngineFail;
		}
		this.StartSlowUpdate(0.1f, SlowUpdate);
		if (engine != null)
		{
			engine.GetComponent<IReportDamage>().onReportDamage += EngineDamageInterface_onReportDamage;
		}
	}

	private void EngineDamageInterface_onReportDamage(OnReportDamage obj)
	{
		JetNozzle_OnEngineFail();
	}

	public void CreateIRSource()
	{
		irSource = new IRSource(thrustTransform, 0f, flare: false);
		aircraft.AddIRSource(irSource);
	}

	public float GetPriority(ControlInputs inputs)
	{
		priority = Mathf.Max(thrustProportion + pitchThrust * inputs.pitch + rollThrust * inputs.roll, 0f);
		return priority;
	}

	public float GetIRMin()
	{
		return IRMin;
	}

	public float GetIRMax()
	{
		float num = IRMax;
		Afterburner[] array = afterburners;
		foreach (Afterburner afterburner in array)
		{
			num += afterburner.GetMaxIRIntensity();
		}
		return num;
	}

	public float GetMaxThrust()
	{
		float num = 0f;
		Afterburner[] array = afterburners;
		foreach (Afterburner afterburner in array)
		{
			num += afterburner.GetMaxThrust();
		}
		return num;
	}

	public float GetTotalThrust()
	{
		float num = 0f;
		Afterburner[] array = afterburners;
		foreach (Afterburner afterburner in array)
		{
			num += afterburner.GetThrust();
		}
		return num;
	}

	public float GetFuelConsumption()
	{
		return fuelConsumption;
	}

	public void SlowUpdate()
	{
		if (heatHaze.system != null)
		{
			heatHaze.UpdateParticles(thrustRatio, rpmRatio, aircraft.speed);
		}
		if (aircraft != null && aircraft.speed > LevelInfo.GetSpeedOfSound(aircraft.GlobalPosition().y))
		{
			SonicBoomManager.RegisterUnit(aircraft);
		}
	}

	public void AudioEffects()
	{
		Vector3 lhs = FastMath.NormalizedDirection(SceneSingleton<CameraStateManager>.i.transform.GlobalPosition(), base.transform.GlobalPosition());
		camFacing = Vector3.Dot(lhs, thrustTransform.forward);
		directionalVolumeMult = Mathf.Lerp(0.5f, 2f, camFacing);
		thrustAudio.volume = thrustRatio * thrustMaxVolume * directionalVolumeMult;
		if (thrustAudio.dopplerLevel > 0f)
		{
			thrustAudio.dopplerLevel = Mathf.Max(1f - camFacing * 2f, 0.01f);
		}
	}

	private void JetNozzle_OnEngineFail()
	{
		if (!(thrustProportion <= 0f))
		{
			thrustProportion = 0f;
			FailureEffect();
		}
	}

	public void FailureEffect()
	{
		GameObject obj = UnityEngine.Object.Instantiate(failureEffect, thrustTransform);
		obj.transform.SetParent(thrustTransform);
		obj.transform.localPosition = Vector3.zero;
		irSource.intensity = 0.2f;
	}

	public void CullDamageParticles()
	{
		if (damageParticles != null)
		{
			damageParticles.ParentObjectCulled();
		}
	}

	public void Thrust(float thrustAmount, float rpmRatio, float thrustRatio, float throttle, bool allowAfterburner)
	{
		totalThrust = thrustAmount;
		irSource.intensity = Mathf.Lerp(IRMin, IRMax, thrustRatio);
		fuelConsumption = 0f;
		this.thrustRatio = thrustRatio;
		this.rpmRatio = rpmRatio;
		AudioEffects();
		Afterburner[] array = afterburners;
		foreach (Afterburner afterburner in array)
		{
			afterburner.Run(allowAfterburner ? (throttle * (float)((rpmRatio > 0.85f) ? 1 : 0)) : 0f, camFacing, directionalVolumeMult);
			fuelConsumption += afterburner.GetFuelConsumption();
			totalThrust += afterburner.GetThrust() * Mathf.Clamp(aircraft.airDensity, 0.4f, 1f);
			irSource.intensity += afterburner.GetIRIntensity();
		}
		if (aircraft.LocalSim && thrustProportion > 0f)
		{
			part.rb.AddForceAtPosition(thrustTransform.forward * totalThrust, thrustTransform.position);
		}
	}
}
