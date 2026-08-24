using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RPMGauge : HUDApp
{
	[SerializeField]
	private string[] sourceNames;

	[SerializeField]
	private TextMeshProUGUI rpmText;

	[SerializeField]
	private Image textBox;

	[SerializeField]
	private AudioClip warningSound;

	[SerializeField]
	private GameObject warningObject;

	[SerializeField]
	private float warningVolume;

	[SerializeField]
	private float warningThreshold;

	[SerializeField]
	private float maxRPM;

	private Aircraft aircraft;

	private List<IEngine> sources;

	private AudioSource warningSource;

	private float displayedRPM;

	private Gradient orangeToGreenGradient;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		sources = new List<IEngine>();
		string[] array = sourceNames;
		foreach (string text in array)
		{
			foreach (UnitPart item in aircraft.partLookup)
			{
				if (item != null && item.gameObject.name == text)
				{
					sources.Add(item.gameObject.GetComponent<IEngine>());
					break;
				}
			}
		}
		warningSource = aircraft.gameObject.AddComponent<AudioSource>();
		warningSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
		warningSource.clip = warningSound;
		warningSource.loop = true;
		warningSource.volume = warningVolume;
		warningSource.spatialBlend = 0f;
		warningSource.dopplerLevel = 0f;
		warningSource.bypassEffects = true;
		UpdateGradient();
		ThemeManager.ThemeGroupChanged += RPMGauge_OnThemeGroupChanged;
	}

	private void OnDestroy()
	{
		if (warningSource != null)
		{
			warningSource.Stop();
		}
		ThemeManager.ThemeGroupChanged -= RPMGauge_OnThemeGroupChanged;
	}

	public override void Refresh()
	{
		displayedRPM = 0f;
		foreach (IEngine source in sources)
		{
			displayedRPM += source.GetRPM();
		}
		displayedRPM /= sources.Count;
		rpmText.text = $"RPM {displayedRPM:F0}";
		if (warningSource != null)
		{
			if (displayedRPM < warningThreshold && aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
			{
				if (!warningSource.isPlaying)
				{
					warningSource.Play();
				}
				warningSource.pitch = displayedRPM / warningThreshold;
			}
			else if (warningSource.isPlaying)
			{
				warningSource.Stop();
			}
		}
		if (warningObject != null)
		{
			warningObject.SetActive(displayedRPM < warningThreshold);
		}
		Color color = orangeToGreenGradient.Evaluate((displayedRPM - warningThreshold) / (maxRPM - warningThreshold));
		rpmText.color = color;
		textBox.color = color;
	}

	private void UpdateGradient()
	{
		orangeToGreenGradient = ThemeManager.Active.ColorTheme.Gradient(new List<float> { 0.1135f, 0.34f, 1f }, new List<float> { 0f, 0.014f, 1f });
	}

	private void RPMGauge_OnThemeGroupChanged()
	{
		UpdateGradient();
		Refresh();
	}
}
