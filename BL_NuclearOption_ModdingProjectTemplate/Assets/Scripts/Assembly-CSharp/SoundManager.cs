using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
	private static readonly ResourcesAsyncLoader<SoundManager> loader = ResourcesAsyncLoader.Create("MusicAndVolume", (Func<GameObject, SoundManager>)null);

	[Header("References")]
	[SerializeField]
	private MusicManager musicManager;

	[SerializeField]
	private AudioMixerVolume mixerVolume;

	[Header("Oneshot Sources")]
	[SerializeField]
	private AudioSource effectsSource;

	[SerializeField]
	private AudioSource interfaceSource;

	[SerializeField]
	private AudioSource radarWarningSource;

	[SerializeField]
	private AudioSource menuSource;

	[Header("Mixers")]
	[SerializeField]
	public AudioMixerGroup EffectsMixer;

	[SerializeField]
	public AudioMixerGroup HeavyEffectsMixer;

	[SerializeField]
	public AudioMixerGroup InterfaceMixer;

	[SerializeField]
	public AudioMixerGroup RadarWarningMixer;

	[SerializeField]
	public AudioMixerGroup MissileAlertMixer;

	[SerializeField]
	public AudioMixerGroup JammedNoiseMixer;

	[SerializeField]
	public AudioMixerGroup MenuMixer;

	[SerializeField]
	public AudioMixerGroup MusicMixer;

	public static SoundManager i => loader.Get();

	public MusicManager Music => musicManager;

	public AudioMixerVolume Volumes => mixerVolume;

	public static async UniTask Preload(CancellationToken cancel)
	{
		await loader.Load(cancel);
	}

	public static void PlayEffectOneShot(AudioClip audioClip)
	{
		i.effectsSource.PlayOneShot(audioClip);
	}

	public static void PlayInterfaceOneShot(AudioClip audioClip)
	{
		i.interfaceSource.PlayOneShot(audioClip);
	}

	public static void PlayRadarWarningOneShot(AudioClip audioClip)
	{
		i.radarWarningSource.PlayOneShot(audioClip);
	}

	public static void PlayMenuOneShot(AudioClip audioClip)
	{
		i.menuSource.PlayOneShot(audioClip);
	}

	public static void PlayEffect(AudioClip audioClip)
	{
		PlaySound(i.effectsSource, audioClip);
	}

	public static void PlayInterface(AudioClip audioClip)
	{
		PlaySound(i.interfaceSource, audioClip);
	}

	public static void PlayMenu(AudioClip audioClip)
	{
		PlaySound(i.menuSource, audioClip);
	}

	public static void PlaySound(AudioSource source, AudioClip audioClip)
	{
		if (!(source == i.interfaceSource) || !PlayerSettings.cinematicMode)
		{
			if (source.isPlaying)
			{
				source.Stop();
			}
			source.time = 0f;
			source.clip = audioClip;
			source.Play();
		}
	}
}
