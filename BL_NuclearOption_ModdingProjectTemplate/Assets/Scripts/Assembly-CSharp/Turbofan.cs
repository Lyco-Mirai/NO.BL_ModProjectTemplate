using System;
using UnityEngine;

public class Turbofan : MonoBehaviour, IEngine, IThrustSource, IDamageable, IReportDamage
{
	[Serializable]
	private class CriticalPart
	{
		[SerializeField]
		private UnitPart part;

		[SerializeField]
		private float threshold;

		[SerializeField]
		private float weight;

		private Turbofan turbofan;

		public void Initialize(Turbofan turbofan)
		{
			this.turbofan = turbofan;
			part.onApplyDamage += CriticalPart_OnApplyDamage;
			part.onParentDetached += CriticalPart_OnPartDetached;
		}

		public void Remove()
		{
			part.onApplyDamage -= CriticalPart_OnApplyDamage;
			part.onParentDetached -= CriticalPart_OnPartDetached;
		}

		private void CriticalPart_OnApplyDamage(UnitPart.OnApplyDamage e)
		{
			if (!(e.hitPoints > threshold))
			{
				float hitPoints = Mathf.Max(Mathf.Min(e.pierceDamage + e.blastDamage + e.fireDamage + e.impactDamage, threshold - e.hitPoints) * weight, 0f);
				turbofan.ApplyPartDamage(hitPoints);
			}
		}

		private void CriticalPart_OnPartDetached(UnitPart part)
		{
			turbofan.ApplyPartDamage(100f * weight);
		}
	}

	public float staticThrust;

	private Vector3 nozzleAngles;

	[SerializeField]
	private AudioSource turbineAudio;

	[SerializeField]
	private Vector3 thrustVectoring;

	[SerializeField]
	private Vector3 thrustVectoringGain = new Vector3(1f, 1f, 4f);

	[SerializeField]
	private Vector2 throttleRemap = new Vector2(0f, 1f);

	[SerializeField]
	private float thrustVectoringMaxAirspeed;

	[SerializeField]
	private float minDensity;

	[SerializeField]
	private float splitThrustFactor;

	[SerializeField]
	private AnimationCurve altitudeThrust;

	[SerializeField]
	private AnimationCurve speedThrust;

	private float dynamicThrustFactor;

	private float dynamicThrustFactorSmoothed;

	private float dynamicThrustFactorSmoothingVel;

	private float currentThrust;

	private float parasiticThrustLoss;

	private bool operable = true;

	private float currentRPM;

	[SerializeField]
	private UnitPart part;

	[SerializeField]
	private JetNozzle[] nozzles;

	[SerializeField]
	private Transform[] vectoringTransforms;

	[SerializeField]
	private float turbineMaxPitch;

	[SerializeField]
	private float minRPM;

	[SerializeField]
	private float maxRPM;

	[SerializeField]
	private float spoolRate;

	[SerializeField]
	private float startupRate;

	[SerializeField]
	private float fuelConsumptionMin;

	[SerializeField]
	private float fuelConsumptionMax;

	[SerializeField]
	private float damageThreshold = 80f;

	[SerializeField]
	private CriticalPart[] criticalParts;

	[SerializeField]
	private ArmorProperties armorProperties;

	[SerializeField]
	private string failureMessage;

	[SerializeField]
	private AudioClip failureMessageAudio;

	private ControlInputs controlInputs;

	private float lastFuelCheck;

	private float spoolRatio;

	private bool hasFuel;

	private Aircraft aircraft;

	private float condition = 1f;

	private float hitPoints = 100f;

	private byte damageIndex;

	Transform IEngine.transform => base.transform;

	public event Action<OnReportDamage> onReportDamage;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	private void Awake()
	{
		aircraft = part.parentUnit as Aircraft;
		aircraft.engineStates.Add(this);
		damageIndex = aircraft.RegisterDamageable(this);
		aircraft.onInitialize += Turbofan_OnInitialize;
		controlInputs = aircraft.GetInputs();
		CriticalPart[] array = criticalParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(this);
		}
		this.StartSlowUpdate(1f, SlowUpdate);
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID dealerID)
	{
		if (!aircraft.IsServer)
		{
			Debug.LogWarning($"TakeDamage called on {this} but it is not spawned on server");
			return;
		}
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / Mathf.Max(armorProperties.pierceTolerance, 0.01f);
		float num2 = blastDamage * amountAffected / Mathf.Max(armorProperties.blastTolerance, 0.01f);
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / Mathf.Max(armorProperties.fireTolerance, 0.01f);
		float num4 = num + num2 + num3 + impactDamage;
		if (!(num4 <= 0f) && !(aircraft == null))
		{
			if (dealerID.IsValid && dealerID != aircraft.persistentID)
			{
				aircraft.RecordDamage(dealerID, num4);
			}
			aircraft.RpcDamage(damageIndex, new DamageInfo(num, num2, num3, impactDamage));
		}
	}

	public void ApplyDamage(float netPierceDamage, float netBlastDamage, float netFireDamage, float netImpactDamage)
	{
		hitPoints -= netPierceDamage + netBlastDamage + netFireDamage + netImpactDamage;
		InvokeDamage();
	}

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	public Unit GetUnit()
	{
		return aircraft;
	}

	public void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
	}

	public void Detach(Vector3 _1, Vector3 _2)
	{
	}

	public float GetMass()
	{
		return part.rb.mass;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	private void ApplyPartDamage(float hitPoints)
	{
		this.hitPoints -= hitPoints;
		InvokeDamage();
	}

	private void SlowUpdate()
	{
		if (operable)
		{
			float y = aircraft.GlobalPosition().y;
			float num = altitudeThrust.Evaluate(y);
			float num2 = speedThrust.Evaluate(aircraft.speed);
			dynamicThrustFactor = num * num2;
		}
	}

	public float GetSpoolPercentage()
	{
		return spoolRatio;
	}

	public float GetMaxThrust()
	{
		float num = staticThrust;
		JetNozzle[] array = nozzles;
		foreach (JetNozzle jetNozzle in array)
		{
			num += jetNozzle.GetMaxThrust();
		}
		return num;
	}

	public float GetThrust()
	{
		float num = currentThrust;
		JetNozzle[] array = nozzles;
		foreach (JetNozzle jetNozzle in array)
		{
			num += jetNozzle.GetTotalThrust();
		}
		return num;
	}

	public float GetThrustRatio()
	{
		return currentThrust / staticThrust;
	}

	public float GetRPM()
	{
		return currentRPM;
	}

	public float GetMinRPM()
	{
		return minRPM;
	}

	public float GetMaxRPM()
	{
		return maxRPM;
	}

	public float GetRPMRatio()
	{
		return currentRPM / maxRPM;
	}

	public void SetParasiticLoss(float loss)
	{
		parasiticThrustLoss = loss;
	}

	public void SetInteriorSounds(bool useInteriorSound)
	{
	}

	private void Turbofan_OnInitialize()
	{
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			currentRPM = maxRPM;
		}
	}

	private void InvokeDamage()
	{
		if (operable && !(hitPoints > damageThreshold))
		{
			condition = Mathf.Clamp01(1f - (damageThreshold - hitPoints) / damageThreshold);
			this.OnEngineDamage?.Invoke();
			if (condition <= 0f)
			{
				KillEngine();
			}
		}
	}

	private void KillEngine()
	{
		operable = false;
		CriticalPart[] array = criticalParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Remove();
		}
		this.OnEngineDisable?.Invoke();
		this.onReportDamage?.Invoke(new OnReportDamage
		{
			failureMessage = failureMessage,
			audioReport = failureMessageAudio
		});
	}

	private void Animate()
	{
		if (currentRPM > 0f && !turbineAudio.isPlaying)
		{
			turbineAudio.Play();
		}
		turbineAudio.dopplerLevel = 1f;
		turbineAudio.pitch = currentRPM / maxRPM * turbineMaxPitch;
	}

	private void UseFuel(float fuelConsumption)
	{
		lastFuelCheck = Time.timeSinceLevelLoad;
		hasFuel = aircraft.UseFuel(fuelConsumption);
	}

	private void FixedUpdate()
	{
		Animate();
		bool flag = aircraft.Ignition && operable && aircraft.airDensity > minDensity && hasFuel;
		JetNozzle[] array;
		if (base.transform.position.y < Datum.LocalSeaY && operable && flag)
		{
			hitPoints = 0f;
			InvokeDamage();
			array = nozzles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].CullDamageParticles();
			}
		}
		float num = controlInputs.throttle;
		float num2 = 0f;
		currentThrust = 0f;
		float num3 = 0f;
		if (Mathf.Abs(splitThrustFactor) > 0f)
		{
			num = Mathf.Clamp01(num + controlInputs.yaw * splitThrustFactor);
		}
		if (flag)
		{
			float t = (num - throttleRemap.x) / (throttleRemap.y - throttleRemap.x);
			num2 = Mathf.Lerp(minRPM, minRPM + (maxRPM - minRPM) * (condition * 0.5f + 0.5f), t);
			num3 = Mathf.Lerp(fuelConsumptionMin, fuelConsumptionMax, spoolRatio);
		}
		float num4 = Mathf.Clamp(num2 - currentRPM, 0f - spoolRate, spoolRate);
		if (currentRPM < minRPM)
		{
			num4 = Mathf.Min(num4, startupRate);
		}
		currentRPM += num4 * Time.deltaTime;
		spoolRatio = (flag ? Mathf.Clamp01((currentRPM - minRPM * 0.85f) / (maxRPM - minRPM)) : 0f);
		dynamicThrustFactorSmoothed = Mathf.SmoothDamp(dynamicThrustFactorSmoothed, dynamicThrustFactor, ref dynamicThrustFactorSmoothingVel, 1f);
		currentThrust = Mathf.Max((staticThrust * dynamicThrustFactorSmoothed - parasiticThrustLoss) * spoolRatio, 0f);
		float num5 = (0f - Mathf.Clamp(controlInputs.pitch * thrustVectoringGain.x, -1f, 1f)) * thrustVectoring.x;
		float num6 = Mathf.Clamp(0f - controlInputs.yaw, -1f, 1f) * thrustVectoring.y;
		num5 -= Mathf.Clamp(controlInputs.roll * thrustVectoringGain.z, -1f, 1f) * thrustVectoring.z;
		if (aircraft.radarAlt < 3f)
		{
			num6 = 0f;
		}
		nozzleAngles.x += Mathf.Clamp(num5 - nozzleAngles.x, -70f * Time.deltaTime, 70f * Time.deltaTime);
		nozzleAngles.y += Mathf.Clamp(num6 - nozzleAngles.y, -70f * Time.deltaTime, 70f * Time.deltaTime);
		if (aircraft.speed > thrustVectoringMaxAirspeed)
		{
			nozzleAngles = Vector3.zero;
		}
		for (int j = 0; j < vectoringTransforms.Length; j++)
		{
			vectoringTransforms[j].transform.localEulerAngles = new Vector3(Mathf.Clamp(nozzleAngles.x + nozzleAngles.z, -20f, 20f), nozzleAngles.y, 0f);
		}
		float rpmRatio = currentRPM / maxRPM;
		array = nozzles;
		foreach (JetNozzle jetNozzle in array)
		{
			bool allowAfterburner = parasiticThrustLoss <= 0f || controlInputs.customAxis1 > 0.3f;
			jetNozzle.Thrust(currentThrust, rpmRatio, spoolRatio, controlInputs.throttle, allowAfterburner);
			num3 += jetNozzle.GetFuelConsumption();
		}
		if (Time.timeSinceLevelLoad - lastFuelCheck > 1f)
		{
			UseFuel(num3);
		}
	}
}
