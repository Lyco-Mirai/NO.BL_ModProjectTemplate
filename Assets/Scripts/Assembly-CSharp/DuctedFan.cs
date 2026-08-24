using System;
using UnityEngine;

public class DuctedFan : MonoBehaviour, IEngine, IThrustSource, IReportDamage, IPowerOutput, IPowerSource
{
	private Aircraft aircraft;

	private ControlInputs controlInputs;

	[SerializeField]
	private Transmission transmission;

	[SerializeField]
	private UnitPart unitPart;

	[SerializeField]
	private float minThrust;

	[SerializeField]
	private float spoolRate;

	[SerializeField]
	private float yawCoef;

	[SerializeField]
	private float bladeDrag;

	[SerializeField]
	private float responseTime;

	[SerializeField]
	private float maxRPM;

	[SerializeField]
	private float gearing;

	[SerializeField]
	private float nominalPower;

	[SerializeField]
	private float maxPower;

	[SerializeField]
	private float area;

	[SerializeField]
	private float idleThresholdPositive;

	[SerializeField]
	private float idleThresholdNegative;

	private float rpm;

	[SerializeField]
	private Transform thrustVector;

	[SerializeField]
	private Transform rotator;

	private GameObject thrustDebug;

	[SerializeField]
	private RotorShaft rotorShaft;

	private float trimThrust;

	private float availableThrust;

	private float maxThrust;

	private float currentThrust;

	private float availablePower;

	private float currentThrustSmoothVel;

	[SerializeField]
	private float IRMin;

	[SerializeField]
	private float IRMax;

	[SerializeField]
	private Transform IRTransform;

	[SerializeField]
	private bool isHeatSource;

	private IRSource heatSource;

	private bool inoperable;

	private bool reverseThrust;

	[SerializeField]
	private UnitPart[] criticalParts;

	[SerializeField]
	private float criticalDamageThreshold;

	[SerializeField]
	private MeshFilter meshFilter;

	[SerializeField]
	private MeshRenderer meshRenderer;

	[SerializeField]
	private Mesh slowMesh;

	[SerializeField]
	private Mesh fastMesh;

	[SerializeField]
	private Material slowMaterial;

	[SerializeField]
	private Material fastMaterial;

	[SerializeField]
	private float rpmThreshold;

	[SerializeField]
	private string failureMessage;

	[SerializeField]
	private AudioClip failureMessageAudio;

	[Header("Audio")]
	[SerializeField]
	private AudioSource fanSource;

	[SerializeField]
	private AudioClip exteriorSound;

	[SerializeField]
	private AudioClip interiorSound;

	[SerializeField]
	private float unthrottledVolume = 0.5f;

	[SerializeField]
	private float throttledVolume = 1f;

	[SerializeField]
	private float pitch = 1f;

	Transform IEngine.transform => base.transform;

	public event Action OnEngineDisable;

	public event Action OnEngineDamage;

	public event Action<OnReportDamage> onReportDamage;

	private void OnEnable()
	{
		maxThrust = Mathf.Pow(2.4f * area * (nominalPower * nominalPower), 0.3333f);
		aircraft = unitPart.parentUnit as Aircraft;
		controlInputs = aircraft.GetInputs();
		UnitPart[] array = criticalParts;
		foreach (UnitPart obj in array)
		{
			obj.onApplyDamage += DuctedFan_OnDamage;
			obj.onPartDetached += DuctedFan_OnDetached;
		}
		if (PlayerSettings.debugVis)
		{
			thrustDebug = UnityEngine.Object.Instantiate(GameAssets.i.debugArrow, thrustVector);
			thrustDebug.transform.localPosition = Vector3.zero;
		}
		if (rotorShaft == null)
		{
			controlInputs.customAxis1 = (idleThresholdNegative + idleThresholdPositive) * 0.5f;
		}
		if (isHeatSource)
		{
			heatSource = new IRSource(IRTransform, 0f, flare: false);
			aircraft.AddIRSource(heatSource);
		}
		aircraft.engines.Add(this);
	}

	private void Update()
	{
		rotator.localEulerAngles += Vector3.forward * rpm * 6f * Time.deltaTime;
		if (aircraft.displayDetail > 1f)
		{
			Animate();
		}
	}

	public void SetDesiredBaseThrust(float thrust)
	{
		trimThrust = thrust;
	}

	public float GetMaxThrust()
	{
		return maxThrust;
	}

	public float GetThrust()
	{
		return currentThrust;
	}

	public void SendPower(float power)
	{
		availablePower = Mathf.Min(power, maxPower);
	}

	public float GetRPM()
	{
		return rpm;
	}

	public float GetRPMRatio()
	{
		return rpm / maxRPM;
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

	public void SetInteriorSounds(bool useInteriorSound)
	{
		if (!(fanSource != null))
		{
			return;
		}
		if (fanSource.isPlaying)
		{
			if (useInteriorSound)
			{
				fanSource.Stop();
				fanSource.clip = interiorSound;
				fanSource.time = UnityEngine.Random.Range(0f, fanSource.clip.length);
				fanSource.Play();
			}
			else
			{
				fanSource.Stop();
				fanSource.clip = exteriorSound;
				fanSource.time = UnityEngine.Random.Range(0f, fanSource.clip.length);
				fanSource.Play();
			}
		}
		else if (useInteriorSound)
		{
			fanSource.clip = interiorSound;
		}
		else
		{
			fanSource.clip = exteriorSound;
		}
	}

	private void DuctedFan_OnDetached(UnitPart unitPart)
	{
		UnitPart[] array = criticalParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onPartDetached -= DuctedFan_OnDetached;
		}
		inoperable = true;
		rpm *= 0.1f;
	}

	private void DuctedFan_OnDamage(UnitPart.OnApplyDamage e)
	{
		if (inoperable)
		{
			return;
		}
		this.OnEngineDamage?.Invoke();
		if (e.detached || e.hitPoints < criticalDamageThreshold)
		{
			this.onReportDamage?.Invoke(new OnReportDamage
			{
				failureMessage = failureMessage,
				audioReport = failureMessageAudio
			});
			this.OnEngineDisable?.Invoke();
			inoperable = true;
			UnitPart[] array = criticalParts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].onApplyDamage -= DuctedFan_OnDamage;
			}
			aircraft.RemoveIRSource(heatSource);
		}
	}

	private void Animate()
	{
		meshFilter.mesh = ((rpm * Time.timeScale < rpmThreshold) ? slowMesh : fastMesh);
		meshRenderer.material = ((rpm * Time.timeScale < rpmThreshold) ? slowMaterial : fastMaterial);
		if (fanSource != null)
		{
			fanSource.pitch = rpm / maxRPM * pitch;
			fanSource.volume = rpm / maxRPM * (unthrottledVolume + Mathf.Abs(currentThrust) / maxThrust * throttledVolume);
		}
	}

	private void FixedUpdate()
	{
		if (inoperable)
		{
			rpm -= rpm * bladeDrag * Mathf.Abs(currentThrust / maxThrust) * Time.fixedDeltaTime;
			availableThrust = 0f;
			availablePower = 0f;
			return;
		}
		reverseThrust = false;
		if (rotorShaft != null)
		{
			float num = trimThrust + controlInputs.yaw * yawCoef;
			if (num < 0f)
			{
				reverseThrust = true;
				num = Mathf.Abs(num);
			}
			float num2 = Mathf.Abs(num);
			float num3 = Mathf.Sqrt(num2 * num2 * num2 / (2f * aircraft.airDensity * area));
			if (num3 > 0f)
			{
				transmission.RequestPower(this, Mathf.Min(num3, nominalPower));
			}
			availableThrust = Mathf.Min(num, maxThrust);
			rpm = rotorShaft.GetRPM() * gearing;
		}
		else
		{
			float value = (controlInputs.customAxis1 - idleThresholdPositive) / (1f - idleThresholdPositive);
			float value2 = (idleThresholdNegative - controlInputs.customAxis1) / idleThresholdNegative;
			float num4 = Mathf.Clamp01(value) - Mathf.Clamp01(value2);
			num4 += controlInputs.yaw * yawCoef;
			float value3 = nominalPower * Mathf.Max(Mathf.Abs(num4), 0.025f);
			transmission.RequestPower(this, Mathf.Clamp(value3, 0f, maxPower));
			if (availablePower > maxPower * 0.001f)
			{
				rpm += spoolRate * Time.fixedDeltaTime;
			}
			else
			{
				rpm -= (rpm + 20f) * bladeDrag * (0.3f + Mathf.Abs(currentThrust / maxThrust)) * Time.fixedDeltaTime;
			}
			availableThrust = Mathf.Pow(2f * aircraft.airDensity * area * (availablePower * availablePower), 0.3333f);
			if (num4 < 0f)
			{
				availableThrust *= -1f;
			}
			if (Mathf.Abs(num4) < 0.025f)
			{
				availableThrust = 0f;
			}
		}
		if (reverseThrust)
		{
			availableThrust *= -1f;
		}
		currentThrust = Mathf.SmoothDamp(currentThrust, availableThrust, ref currentThrustSmoothVel, responseTime);
		rpm = Mathf.Clamp(rpm, 0f, maxRPM);
		float num5 = rpm * rpm / (maxRPM * maxRPM);
		if (isHeatSource)
		{
			heatSource.intensity = Mathf.Lerp(IRMin, IRMax, num5);
		}
		if (aircraft.LocalSim)
		{
			unitPart.rb.AddForceAtPosition(thrustVector.forward * currentThrust * num5, thrustVector.position);
		}
		if (thrustDebug != null)
		{
			thrustDebug.transform.rotation = Quaternion.LookRotation(thrustVector.forward);
			thrustDebug.transform.localScale = Vector3.up + Vector3.right + Vector3.forward * currentThrust * 0.001f;
		}
	}
}
