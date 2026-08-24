using UnityEngine;
using UnityEngine.UI;

public class AudioMenu : MonoBehaviour
{
	[SerializeField]
	private Slider masterVolumeSlider;

	[SerializeField]
	private Text masterVolumeValue;

	[SerializeField]
	private Slider musicVolumeSlider;

	[SerializeField]
	private Text musicVolumeValue;

	[SerializeField]
	private Slider interfaceVolumeSlider;

	[SerializeField]
	private Text interfaceVolumeValue;

	[SerializeField]
	private Slider effectsVolumeSlider;

	[SerializeField]
	private Text effectsVolumeValue;

	[SerializeField]
	private Slider menuVolumeSlider;

	[SerializeField]
	private Text menuVolumeValue;

	[SerializeField]
	private Slider radarWarningVolumeSlider;

	[SerializeField]
	private Text radarWarningVolumeValue;

	[SerializeField]
	private Slider missileAlertVolumeSlider;

	[SerializeField]
	private Text missileAlertVolumeValue;

	[SerializeField]
	private Slider jammedNoiseVolumeSlider;

	[SerializeField]
	private Text jammedNoiseVolumeValue;

	public void Start()
	{
		SetupSlider(masterVolumeSlider, masterVolumeValue, AudioMixerVolume.Master);
		SetupSlider(musicVolumeSlider, musicVolumeValue, AudioMixerVolume.Music);
		SetupSlider(interfaceVolumeSlider, interfaceVolumeValue, AudioMixerVolume.Interface);
		SetupSlider(effectsVolumeSlider, effectsVolumeValue, AudioMixerVolume.Effects);
		SetupSlider(menuVolumeSlider, menuVolumeValue, AudioMixerVolume.Menu);
		SetupSlider(radarWarningVolumeSlider, radarWarningVolumeValue, AudioMixerVolume.RadarWarning);
		SetupSlider(missileAlertVolumeSlider, missileAlertVolumeValue, AudioMixerVolume.MissileAlert);
		SetupSlider(jammedNoiseVolumeSlider, jammedNoiseVolumeValue, AudioMixerVolume.JammedNoise);
	}

	private static void SetupSlider(Slider slider, Text text, string channel)
	{
		float pref = AudioMixerVolume.GetPref(channel);
		slider.SetValueWithoutNotify(pref);
		SetFormattedText(text, pref);
		slider.onValueChanged.AddListener(delegate(float newValue)
		{
			AudioMixerVolume.SetValue(channel, newValue);
			SetFormattedText(text, newValue);
		});
	}

	private static void SetFormattedText(Text text, float value)
	{
		text.text = (value * 100f).ToString("F0") + "%";
	}
}
