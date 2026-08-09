using System;
using UnityEngine;

public class PowerSupply : MonoBehaviour
{
	[SerializeField]
	private GameObject[] powerSources;

	private IEngine[] engineInterfaces;

	private float charge;

	private float powerRequested;

	private float powerDrawn;

	[SerializeField]
	private float maxCharge;

	[SerializeField]
	private float maxPower;

	[SerializeField]
	private float chargePerRPM;

	[SerializeField]
	private float pitchMin;

	[SerializeField]
	private float pitchMax;

	[SerializeField]
	[Range(0f, 2f)]
	private float volumeMultiplier;

	[SerializeField]
	private AnimationCurve supplyAtCharge;

	[SerializeField]
	private AudioSource source;

	[SerializeField]
	private Aircraft aircraft;

	public int Users { get; private set; }

	public event Action<PowerSupply> onChargeChanged;

	private void Awake()
	{
		engineInterfaces = new IEngine[powerSources.Length];
		aircraft.onInitialize += PowerSupply_OnSpawnedInPosition;
		for (int i = 0; i < powerSources.Length; i++)
		{
			engineInterfaces[i] = powerSources[i].GetComponent<IEngine>();
		}
	}

	public void AddUser()
	{
		Users++;
	}

	public void RemoveUser()
	{
		Users--;
	}

	public void ModifyCapacitance(float capacitance)
	{
		maxCharge += capacitance;
	}

	private void FixedUpdate()
	{
		if (charge >= maxCharge && powerDrawn == 0f)
		{
			base.enabled = false;
			source.Stop();
			return;
		}
		if (!source.isPlaying)
		{
			source.Play();
		}
		IEngine[] array = engineInterfaces;
		foreach (IEngine engine in array)
		{
			float num = chargePerRPM * engine.GetRPM() * Time.deltaTime;
			charge += num;
		}
		charge = Mathf.Clamp(charge, 0f, maxCharge);
		this.onChargeChanged?.Invoke(this);
		source.pitch = Mathf.Lerp(pitchMin, pitchMax, powerDrawn / Mathf.Max(powerRequested, 0.01f));
		source.volume = Mathf.Sqrt(powerDrawn / maxPower) * volumeMultiplier;
		powerRequested = 0f;
		powerDrawn = 0f;
	}

	private void PowerSupply_OnSpawnedInPosition()
	{
		charge = Mathf.Max(charge, (aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f) ? maxCharge : 0f);
	}

	public float GetCharge()
	{
		return charge / maxCharge;
	}

	public float GetPowerSupplied()
	{
		return powerDrawn / maxPower;
	}

	public float GetPowerAvailable()
	{
		return supplyAtCharge.Evaluate(charge / maxCharge);
	}

	public void SetFullyCharged()
	{
		charge = maxCharge;
	}

	public float GetChargeKJ()
	{
		return charge;
	}

	public float DrawPower(float powerRequested)
	{
		base.enabled = true;
		if (!source.isPlaying)
		{
			source.Play();
		}
		this.powerRequested += powerRequested;
		float num = ((charge > 0f) ? (powerRequested * supplyAtCharge.Evaluate(charge / maxCharge)) : 0f);
		charge -= num * Time.deltaTime;
		charge = Mathf.Max(charge, 0f);
		powerDrawn += num;
		return num;
	}
}
