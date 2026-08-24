using System.Collections.Generic;
using UnityEngine;

public class VRSWarning : HUDApp
{
	[SerializeField]
	private string[] sourceNames;

	[SerializeField]
	private GameObject warningObject;

	[SerializeField]
	private AudioClip warningSound;

	[SerializeField]
	private float warningVolume;

	[SerializeField]
	private float warningThreshold;

	private Aircraft aircraft;

	private List<RotorShaft> sources;

	private float VRSFactor;

	private bool inVRS;

	private AudioSource warningSource;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
		sources = new List<RotorShaft>();
		string[] array = sourceNames;
		foreach (string text in array)
		{
			foreach (UnitPart item in aircraft.partLookup)
			{
				if (item != null && item.gameObject.name == text)
				{
					sources.Add(item.gameObject.GetComponent<RotorShaft>());
					break;
				}
			}
		}
		warningObject.SetActive(value: false);
		warningSource = aircraft.gameObject.AddComponent<AudioSource>();
		warningSource.outputAudioMixerGroup = SoundManager.i.InterfaceMixer;
		warningSource.clip = warningSound;
		warningSource.loop = false;
		warningSource.volume = warningVolume;
		warningSource.spatialBlend = 0f;
		warningSource.dopplerLevel = 0f;
		warningSource.bypassEffects = true;
	}

	private void OnDestroy()
	{
		if (warningSource != null)
		{
			warningSource.Stop();
		}
	}

	public override void Refresh()
	{
		VRSFactor = 0f;
		foreach (RotorShaft source in sources)
		{
			VRSFactor += source.GetVRSFactor();
		}
		VRSFactor /= sources.Count;
		if (VRSFactor > warningThreshold && aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f)
		{
			if (!inVRS)
			{
				warningObject.SetActive(value: true);
				if (!warningSource.isPlaying)
				{
					warningSource.Play();
				}
				inVRS = true;
			}
		}
		else if (inVRS)
		{
			warningObject.SetActive(value: false);
			inVRS = false;
		}
	}
}
