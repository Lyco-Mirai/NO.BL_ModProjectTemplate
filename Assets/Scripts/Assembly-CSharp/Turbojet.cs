using System;
using UnityEngine;

public class Turbojet : MonoBehaviour, IEngine, IThrustSource, IReportDamage
{
	public float maxThrust;

	private Vector3 nozzleAngles;

	public float damageFactor;

	private float thrust;

	private float parasiticThrustLoss;

	public bool engineFire;

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

	private bool operable = true;

	private float rpm;

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
	private float maxSpeed = 522f;

	[SerializeField]
	private float spoolRate;

	[SerializeField]
	private float startupRate;

	[SerializeField]
	private float fuelConsumptionMin;

	[SerializeField]
	private float fuelConsumptionMax;

	[SerializeField]
	private float damageThreshold;

	[SerializeField]
	private UnitPart[] criticalParts;

	[SerializeField]
	private string failureMessage;

	[SerializeField]
	private AudioClip failureMessageAudio;

	private ControlInputs controlInputs;

	private float lastFuelCheck;

	private float thrustRatio;

	private bool hasFuel;

	private bool afterburnerOn;

	private Aircraft aircraft;

	private float condition = 1f;

	private bool outOfSoundCone;

	Transform IEngine.transform => base.transform;

	public event Action<OnReportDamage> onReportDamage;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	private void Awake()
	{
		aircraft = criticalParts[0].parentUnit as Aircraft;
		aircraft.engineStates.Add(this);
		aircraft.onInitialize += Turbojet_OnInitialize;
		controlInputs = aircraft.GetInputs();
		UnitPart[] array = criticalParts;
		foreach (UnitPart obj in array)
		{
			obj.onApplyDamage += Turbojet_OnApplyDamage;
			obj.onParentDetached += Turbojet_OnPartDetach;
		}
	}

	public float GetSpoolPercentage()
	{
		return thrustRatio;
	}

	public float GetMaxThrust()
	{
		float num = maxThrust;
		JetNozzle[] array = nozzles;
		foreach (JetNozzle jetNozzle in array)
		{
			num += jetNozzle.GetMaxThrust();
		}
		return num;
	}

	public float GetThrust()
	{
		float num = thrust;
		JetNozzle[] array = nozzles;
		foreach (JetNozzle jetNozzle in array)
		{
			num += jetNozzle.GetTotalThrust();
		}
		return num;
	}

	public float GetThrustRatio()
	{
		return thrust / maxThrust;
	}

	public float GetRPM()
	{
		return rpm;
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
		return rpm / maxRPM;
	}

	public void SetParasiticLoss(float loss)
	{
		parasiticThrustLoss = loss;
	}

	public void SetInteriorSounds(bool useInteriorSound)
	{
	}

	private void Turbojet_OnInitialize()
	{
		if (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			rpm = maxRPM;
		}
	}

	private void Turbojet_OnPartDetach(UnitPart part)
	{
		if (operable)
		{
			KillEngine();
		}
	}

	private void Turbojet_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
		if (operable)
		{
			this.OnEngineDamage?.Invoke();
			condition = Mathf.Clamp((e.hitPoints - damageThreshold) / (100f - damageThreshold), 0f, condition);
			if (condition <= 0f)
			{
				KillEngine();
			}
		}
	}

	private void KillEngine()
	{
		operable = false;
		UnitPart[] array = criticalParts;
		foreach (UnitPart unitPart in array)
		{
			if (!(unitPart == null))
			{
				unitPart.onApplyDamage -= Turbojet_OnApplyDamage;
				unitPart.onParentDetached -= Turbojet_OnPartDetach;
			}
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
		if (rpm > 0f && !turbineAudio.isPlaying && !outOfSoundCone)
		{
			turbineAudio.Play();
		}
		turbineAudio.dopplerLevel = 1f;
		turbineAudio.pitch = rpm / maxRPM * turbineMaxPitch;
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
			KillEngine();
			array = nozzles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].CullDamageParticles();
			}
		}
		float num = controlInputs.throttle;
		float num2 = 0f;
		thrust = 0f;
		float num3 = 0f;
		if (Mathf.Abs(splitThrustFactor) > 0f)
		{
			num = Mathf.Clamp01(num + controlInputs.yaw * splitThrustFactor);
		}
		if (flag)
		{
			float t = (num - throttleRemap.x) / (throttleRemap.y - throttleRemap.x);
			num2 = Mathf.Lerp(minRPM, minRPM + (maxRPM - minRPM) * (condition * 0.5f + 0.5f), t);
			num3 = Mathf.Lerp(fuelConsumptionMin, fuelConsumptionMax, thrustRatio);
		}
		float num4 = Mathf.Clamp(num2 - rpm, 0f - spoolRate, spoolRate);
		if (rpm < minRPM)
		{
			num4 = Mathf.Min(num4, startupRate);
		}
		rpm += num4 * Time.deltaTime;
		thrustRatio = (flag ? Mathf.Clamp((rpm - minRPM * 0.85f) / (maxRPM - minRPM), 0f, 1f) : 0f);
		thrust = Mathf.Max((maxThrust - parasiticThrustLoss) * thrustRatio, 0f);
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
		thrust *= altitudeThrust.Evaluate(base.transform.position.y - Datum.LocalSeaY);
		if (aircraft.speed > maxSpeed)
		{
			thrust *= Mathf.Max(1f - 5f * (aircraft.speed - maxSpeed) / maxSpeed, 0f);
		}
		float rpmRatio = rpm / maxRPM;
		array = nozzles;
		foreach (JetNozzle jetNozzle in array)
		{
			bool allowAfterburner = parasiticThrustLoss <= 0f || controlInputs.customAxis1 > 0.3f;
			jetNozzle.Thrust(thrust, rpmRatio, thrustRatio, controlInputs.throttle, allowAfterburner);
			num3 += jetNozzle.GetFuelConsumption();
		}
		if (Time.timeSinceLevelLoad - lastFuelCheck > 1f)
		{
			UseFuel(num3);
		}
	}
}
