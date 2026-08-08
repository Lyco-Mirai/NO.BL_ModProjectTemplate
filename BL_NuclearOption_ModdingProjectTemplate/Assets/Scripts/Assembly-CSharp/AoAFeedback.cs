using UnityEngine;

public static class AoAFeedback
{
	private static Aircraft _aircraft;

	private static AudioSource _source;

	private static AircraftParameters.OnboardAoAEffects aoaEffects;

	private static float volume;

	private static float volumeSmoothed;

	private static float shake;

	private static float shakeSmoothed;

	private static float lastUpdate;

	private static void SetupAircraft(Aircraft aircraft)
	{
		_aircraft = aircraft;
		lastUpdate = 0f;
		if (_source != null)
		{
			Object.Destroy(_source);
		}
		if (!(aircraft == null))
		{
			volumeSmoothed = 0f;
			shakeSmoothed = 0f;
			_source = aircraft.cockpit.gameObject.AddComponent<AudioSource>();
			_source.dopplerLevel = 0f;
			_source.spatialBlend = 0f;
			_source.minDistance = 4f;
			_source.maxDistance = 5f;
			_source.priority = 128;
			_source.loop = true;
			_source.volume = 0f;
			_source.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			_source.bypassListenerEffects = true;
			aoaEffects = aircraft.GetAircraftParameters().AoAEffects;
			_source.clip = aoaEffects.AudioClip;
			_source.Play();
		}
	}

	public static void RunAoAFeedback(Aircraft aircraft)
	{
		if (_aircraft != aircraft)
		{
			SetupAircraft(aircraft);
		}
		if (Time.timeSinceLevelLoad - lastUpdate < 0.1f)
		{
			volumeSmoothed = Mathf.Lerp(volumeSmoothed, volume, 8f * Time.fixedDeltaTime);
			shakeSmoothed = Mathf.Lerp(shakeSmoothed, shake, 8f * Time.fixedDeltaTime);
			_source.volume = volumeSmoothed;
			SceneSingleton<CameraStateManager>.i.ShakeCamera(0f, shakeSmoothed);
		}
		else if (!(aircraft == null))
		{
			lastUpdate = Time.timeSinceLevelLoad;
			Vector3 direction = aircraft.cockpit.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(aircraft.cockpit.xform.GlobalPosition());
			Vector3 vector = aircraft.cockpit.xform.InverseTransformDirection(direction);
			float f = Mathf.Atan2(vector.y, vector.z) * 57.29578f;
			float num = Mathf.Max(aircraft.speed - aoaEffects.OnsetSpeed, 0f) / (aoaEffects.FullVolumeSpeed - aoaEffects.OnsetSpeed);
			float num2 = Mathf.Max(Mathf.Abs(f) - aoaEffects.OnsetAlpha, 0f) / (aoaEffects.FullVolumeAlpha - aoaEffects.OnsetAlpha);
			volume = Mathf.Sqrt(num * num2);
			shake = aoaEffects.ShakeFactor * num * num2;
		}
	}
}
