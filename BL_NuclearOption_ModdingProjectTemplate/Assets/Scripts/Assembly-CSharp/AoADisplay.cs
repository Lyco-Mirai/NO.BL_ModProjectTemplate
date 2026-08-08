using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;

public class AoADisplay : HUDApp
{
	[SerializeField]
	private TextMeshProUGUI AoAText;

	[SerializeField]
	private TextMeshProUGUI stallText;

	[SerializeField]
	private AudioClip stallHorn;

	[SerializeField]
	private AudioClip stallVoice;

	[SerializeField]
	private float hornVolume;

	[SerializeField]
	private float hornThreshold;

	[SerializeField]
	private float velocityThreshold = 10f;

	[SerializeField]
	private float gradientMax;

	private Aircraft aircraft;

	private AudioSource hornSource;

	private float hornLastPlayed;

	private Gradient greenToRedGradient;

	private void Awake()
	{
		stallText.enabled = false;
	}

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		hornSource = aircraft.gameObject.AddComponent<AudioSource>();
		hornSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
		hornSource.clip = stallHorn;
		hornSource.loop = true;
		hornSource.volume = hornVolume;
		hornSource.spatialBlend = 0f;
		hornSource.dopplerLevel = 0f;
		hornSource.bypassEffects = true;
		AoADisplay_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += AoADisplay_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		if (hornSource != null)
		{
			hornSource.Stop();
		}
		ThemeManager.ThemeGroupChanged -= AoADisplay_OnThemeGroupChanged;
	}

	private void OnDisable()
	{
		if (hornSource != null)
		{
			hornSource.Stop();
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		AoAText.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (aircraft == null)
		{
			return;
		}
		Vector3 vector = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity);
		float num = Mathf.Atan2(vector.y, vector.z) * -57.29578f;
		AoAText.enabled = aircraft.speed > velocityThreshold;
		if (AoAText.enabled)
		{
			AoAText.color = greenToRedGradient.Evaluate(num / gradientMax);
			AoAText.text = $"{num:F1}°";
		}
		if (num > hornThreshold && AoAText.enabled)
		{
			stallText.enabled = Mathf.Sin(Time.timeSinceLevelLoad * 16f) > 0f;
			stallText.color = AoAText.color;
			if (!hornSource.isPlaying)
			{
				hornSource.Play();
				if (Time.timeSinceLevelLoad - hornLastPlayed > 5f)
				{
					SoundManager.PlayInterfaceOneShot(stallVoice);
				}
			}
			hornLastPlayed = Time.timeSinceLevelLoad;
		}
		else
		{
			stallText.enabled = false;
			if (hornSource.isPlaying)
			{
				hornSource.Stop();
			}
		}
	}

	private void AoADisplay_OnThemeGroupChanged()
	{
		greenToRedGradient = ThemeManager.Active.ColorTheme.Gradient(new List<float> { 1f, 0.5f, 0f }, new List<float> { 0.5f, 0.75f, 1f });
	}
}
