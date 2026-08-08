using System.Collections.Generic;
using UnityEngine;

public class RadarJammer : Countermeasure
{
	[SerializeField]
	private float powerUsage;

	[SerializeField]
	private float capacitance = 100f;

	[SerializeField]
	private float jammingIntensity;

	[SerializeField]
	private float dischargeVolume;

	[SerializeField]
	private AudioClip dischargeSound;

	[SerializeField]
	[Range(0f, 1f)]
	private float volumeMultiplier;

	private float lastActivated;

	private float jamIntensityPrev;

	private float jamIntensityCurrent;

	private AudioSource dischargeSource;

	private PowerSupply powerSupply;

	protected override void Awake()
	{
		ammo = 1;
		if (aircraft != null)
		{
			powerSupply = aircraft.GetPowerSupply();
			powerSupply.AddUser();
			powerSupply.ModifyCapacitance(capacitance);
		}
		base.Awake();
	}

	public override List<string> GetThreatTypes()
	{
		if (threatTypes == null)
		{
			threatTypes = new List<string> { "ARH", "SARH" };
		}
		return threatTypes;
	}

	public override void AttachToUnit(Aircraft aircraft)
	{
		powerSupply = aircraft.GetPowerSupply();
		base.AttachToUnit(aircraft);
		powerSupply.AddUser();
	}

	public override void Fire()
	{
		base.enabled = true;
		lastActivated = Time.timeSinceLevelLoad;
		float num = powerSupply.DrawPower(powerUsage);
		jamIntensityCurrent = jammingIntensity * num / powerUsage;
		aircraft.AddECMIntensity(jamIntensityCurrent - jamIntensityPrev);
		jamIntensityPrev = jamIntensityCurrent;
		if (dischargeSource == null)
		{
			dischargeSource = base.gameObject.AddComponent<AudioSource>();
			dischargeSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
			dischargeSource.clip = dischargeSound;
			dischargeSource.spatialBlend = 1f;
			dischargeSource.dopplerLevel = 0f;
			dischargeSource.spread = 5f;
			dischargeSource.maxDistance = 40f;
			dischargeSource.minDistance = 5f;
		}
		dischargeSource.pitch = num / powerUsage;
		dischargeSource.volume = num / powerUsage * volumeMultiplier;
		if (!dischargeSource.isPlaying)
		{
			dischargeSource.Play();
		}
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - lastActivated > 0.1f && aircraft != null)
		{
			aircraft.AddECMIntensity(0f - jamIntensityPrev);
			jamIntensityPrev = 0f;
			base.enabled = false;
			if (SceneSingleton<CombatHUD>.i != null && aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				UpdateHUD();
			}
		}
	}

	public override void UpdateHUD()
	{
		SceneSingleton<CombatHUD>.i.DisplayCountermeasures(displayName, displayImage, Time.timeSinceLevelLoad - lastActivated < 0.05f);
		if (dischargeSource != null && dischargeSource.isPlaying && Time.timeSinceLevelLoad - lastActivated > 0.05f)
		{
			dischargeSource.Stop();
		}
	}

	public float GetMaxJammingIntensity()
	{
		return jammingIntensity;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (powerSupply != null)
		{
			powerSupply.RemoveUser();
			powerSupply.ModifyCapacitance(0f - capacitance);
		}
	}
}
