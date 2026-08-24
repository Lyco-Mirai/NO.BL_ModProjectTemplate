using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerVolume : MonoBehaviour
{
	public static readonly string Master = "MasterVolume";

	public static readonly string Effects = "EffectsVolume";

	public static readonly string Interface = "InterfaceVolume";

	public static readonly string Menu = "MenuVolume";

	public static readonly string Music = "MusicVolume";

	public static readonly string RadarWarning = "RadarWarningVolume";

	public static readonly string MissileAlert = "MissileAlertVolume";

	public static readonly string JammedNoise = "JammedNoiseVolume";

	public static readonly string EffectLowPassCutoff = "EffectLowPassCutoff";

	public static readonly string EffectChorusDryMix = "EffectChorusDryMix";

	public static readonly string MasterLowPassCutoff = "MasterLowPassCutoff";

	[SerializeField]
	private AudioMixer _mixer;

	public static void SetValue(string channel, float value)
	{
		PlayerPrefs.SetFloat(channel, value);
		SoundManager.i.Volumes.ChangeMixerVolume(channel, value);
	}

	public static float GetPref(string channel)
	{
		return PlayerPrefs.GetFloat(channel, 0.75f);
	}

	private void Start()
	{
		LoadPref(Master);
		LoadPref(Effects);
		LoadPref(Interface);
		LoadPref(Menu);
		LoadPref(Music);
		LoadPref(RadarWarning);
		LoadPref(MissileAlert);
		LoadPref(JammedNoise);
	}

	private void LoadPref(string channel)
	{
		float pref = GetPref(channel);
		ChangeMixerVolume(channel, pref);
	}

	private void ChangeMixerVolume(string channel, float volume)
	{
		if (Application.isPlaying)
		{
			float value = AudioHelper.LinearToDecibel(volume);
			_mixer.SetFloat(channel, value);
		}
	}

	public static void SetEffectsAudioFilterStrength(float cutoff, float volume)
	{
		AudioMixerVolume volumes = SoundManager.i.Volumes;
		volumes._mixer.SetFloat(EffectLowPassCutoff, cutoff);
		volumes._mixer.SetFloat(EffectChorusDryMix, volume);
	}

	public static void SetMasterAudioFilterStrength(float cutoff)
	{
		SoundManager.i.Volumes._mixer.SetFloat(MasterLowPassCutoff, cutoff);
	}
}
