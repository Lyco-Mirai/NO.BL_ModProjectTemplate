using System;
using UnityEngine;

public class TurbineEngine : MonoBehaviour, IEngine, IPowerSource, IReportDamage
{
	public float maxRPM;

	public float minRPM = 1000f;

	public float maxPower;

	private ControlInputs controlInputs;

	public float spoolUpTime;

	[SerializeField]
	private float startupTime = 10f;

	public float currentPower;

	public float powerRatio;

	public float RPMRatio;

	public float startedAmount;

	private bool operable = true;

	private float currentRPM;

	private float condition = 1f;

	public float throttle;

	private AudioSource turbineAudio;

	[SerializeField]
	private float pitch = 1f;

	[SerializeField]
	private float volume = 1f;

	[SerializeField]
	private AudioSource startupAudio;

	public Aircraft aircraft;

	[SerializeField]
	private ParticleSystem[] heatHazeParticles;

	[SerializeField]
	private float maxFuelConsumption;

	[SerializeField]
	private float IRMin;

	[SerializeField]
	private float IRMax;

	private float lastFuelCheck;

	private bool hasFuel;

	private IRSource heatSource;

	[SerializeField]
	private Transform IRSourceTransform;

	[SerializeField]
	private UnitPart[] criticalParts;

	[SerializeField]
	private float criticalDamageThreshold;

	[SerializeField]
	private GameObject failureEffect;

	[SerializeField]
	private Transform failureTransform;

	[SerializeField]
	private string failureMessage;

	[SerializeField]
	private AudioClip failureMessageAudio;

	Transform IEngine.transform => base.transform;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	public event Action<OnReportDamage> onReportDamage;

	private void OnEnable()
	{
		turbineAudio = GetComponent<AudioSource>();
		turbineAudio.volume = 0f;
		turbineAudio.loop = true;
		turbineAudio.time = UnityEngine.Random.Range(0f, turbineAudio.clip.length);
		controlInputs = aircraft.GetInputs();
		aircraft.engineStates.Add(this);
		aircraft.onSpawnedInPosition += TurbineEngine_OnInitialize;
		heatSource = new IRSource(new GameObject(base.gameObject.name + "_IRSource").transform, 0f, flare: false);
		heatSource.transform.SetParent(IRSourceTransform);
		heatSource.transform.localPosition = Vector3.zero;
		aircraft.AddIRSource(heatSource);
		UnitPart[] array = criticalParts;
		foreach (UnitPart obj in array)
		{
			obj.onApplyDamage += TurbineEngine_OnDamage;
			obj.onPartDetached += TurbineEngine_OnDetach;
		}
	}

	private void TurbineEngine_OnInitialize()
	{
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			hasFuel = true;
			startedAmount = 1f;
			throttle = 1f;
			currentRPM = maxRPM;
		}
	}

	public float GetMaxThrust()
	{
		return 0f;
	}

	public float GetThrust()
	{
		return 0f;
	}

	public float GetMaxPower()
	{
		return maxPower;
	}

	public float GetPower()
	{
		return currentPower;
	}

	public float GetRPM()
	{
		return currentRPM;
	}

	public float GetRPMRatio()
	{
		return currentRPM / maxRPM;
	}

	public void Throttle(float throttle)
	{
		this.throttle = Mathf.Clamp01(throttle);
	}

	public float GetIRMin()
	{
		return IRMin;
	}

	public float GetIRMax()
	{
		return IRMax;
	}

	public void SetInteriorSounds(bool useInteriorSound)
	{
	}

	private void UseFuel(float fuelConsumption)
	{
		lastFuelCheck = Time.timeSinceLevelLoad;
		hasFuel = aircraft.UseFuel(fuelConsumption);
	}

	private void TurbineEngine_OnDamage(UnitPart.OnApplyDamage e)
	{
		if (operable)
		{
			this.OnEngineDamage?.Invoke();
			condition = Mathf.Clamp((e.hitPoints - criticalDamageThreshold) / (100f - criticalDamageThreshold), 0f, condition);
			if (e.detached || condition <= 0f)
			{
				KillEngine();
			}
		}
	}

	private void TurbineEngine_OnDetach(UnitPart e)
	{
		if (operable)
		{
			this.OnEngineDamage?.Invoke();
			condition = 0f;
			KillEngine();
		}
	}

	private void KillEngine()
	{
		this.onReportDamage?.Invoke(new OnReportDamage
		{
			failureMessage = failureMessage,
			audioReport = failureMessageAudio
		});
		this.OnEngineDisable?.Invoke();
		heatSource.transform.SetParent(aircraft.transform);
		operable = false;
		UnitPart[] array = criticalParts;
		foreach (UnitPart obj in array)
		{
			obj.onApplyDamage -= TurbineEngine_OnDamage;
			obj.onPartDetached -= TurbineEngine_OnDetach;
		}
		for (int j = 0; j < heatHazeParticles.Length; j++)
		{
			if (!(heatHazeParticles[j] == null) && heatHazeParticles[j].isPlaying)
			{
				heatHazeParticles[j].Stop();
			}
		}
		if (!(failureTransform == null) && !(base.transform.position.y < Datum.origin.position.y))
		{
			GameObject obj2 = UnityEngine.Object.Instantiate(failureEffect, failureTransform);
			obj2.transform.localRotation = Quaternion.identity;
			DamageParticles component = obj2.GetComponent<DamageParticles>();
			if (component != null)
			{
				aircraft.spawnedEffects.Add(component);
			}
			if (startupAudio != null && startupAudio.isPlaying)
			{
				startupAudio.Stop();
			}
		}
	}

	public bool IsOperable()
	{
		if (operable)
		{
			return hasFuel;
		}
		return false;
	}

	private void Animate(bool running)
	{
		if (!(aircraft.displayDetail < 1f))
		{
			turbineAudio.pitch = RPMRatio - powerRatio * pitch + pitch;
			if (running)
			{
				turbineAudio.volume = (RPMRatio * 0.25f + powerRatio * 0.75f) * volume;
			}
			else
			{
				turbineAudio.volume = RPMRatio * 0.5f * volume;
			}
		}
	}

	private void Update()
	{
		if (aircraft == null)
		{
			return;
		}
		bool flag = aircraft.Ignition && operable && hasFuel && aircraft.airDensity > 0.2f;
		if (RPMRatio < 0.3f && flag && startupAudio != null && !startupAudio.isPlaying)
		{
			startupAudio.Play();
		}
		if (flag && base.transform.position.y < Datum.origin.position.y)
		{
			KillEngine();
		}
		RPMRatio = currentRPM / maxRPM;
		if (flag)
		{
			float num = ((currentRPM < minRPM) ? (spoolUpTime * 2f) : spoolUpTime);
			currentRPM = Mathf.Lerp(currentRPM, maxRPM * (condition * 0.5f + 0.5f), Time.deltaTime / num);
			currentPower = currentRPM / maxRPM * maxPower * throttle;
			currentPower *= aircraft.airDensity / 1.225f;
			if (currentRPM < minRPM)
			{
				currentPower = 0f;
			}
			if (startedAmount < 1f)
			{
				startedAmount += Time.deltaTime / startupTime;
				currentPower *= startedAmount;
			}
			heatSource.intensity = Mathf.Lerp(IRMin, IRMax, currentPower / maxPower);
		}
		else
		{
			startedAmount = 0f;
			if (operable)
			{
				currentRPM -= maxRPM / spoolUpTime * Time.deltaTime * 0.1f;
			}
			else
			{
				currentRPM -= maxRPM / spoolUpTime * Time.deltaTime;
			}
			currentPower = 0f;
			heatSource.intensity = IRMin;
		}
		if (heatHazeParticles != null)
		{
			bool flag2 = aircraft.rb.velocity.sqrMagnitude > 2500f;
			for (int i = 0; i < heatHazeParticles.Length; i++)
			{
				if (!(heatHazeParticles[i] == null))
				{
					if (!flag2 && !heatHazeParticles[i].isPlaying && flag)
					{
						heatHazeParticles[i].Play();
					}
					if (heatHazeParticles[i].isPlaying && flag2)
					{
						heatHazeParticles[i].Stop();
					}
				}
			}
		}
		currentRPM = Mathf.Clamp(currentRPM, 0f, maxRPM);
		powerRatio = currentPower * currentPower / (maxPower * maxPower);
		if (Time.timeSinceLevelLoad - lastFuelCheck > 1f)
		{
			UseFuel(powerRatio * maxFuelConsumption);
		}
		Animate(flag);
	}
}
