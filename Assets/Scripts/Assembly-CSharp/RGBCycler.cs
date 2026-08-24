using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RGBCycler : MonoBehaviour
{
	private List<Image> images = new List<Image>();

	private Text[] texts;

	[SerializeField]
	private float duration = 200f;

	[SerializeField]
	private float cycleSpeed = 1f;

	[SerializeField]
	private float flashSpeed = 2f;

	[SerializeField]
	private float cameraShakeAmount = 0.5f;

	private Color[] imageColors;

	private Color[] textColors;

	private Color currentColor;

	private float startTime;

	private float lastBeat;

	private float beatTimer;

	private Light light;

	private Aircraft aircraft;

	private bool started;

	private void Start()
	{
	}

	private void StartPlaying()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		started = true;
		startTime = Time.timeSinceLevelLoad;
		StoreColors();
		light = aircraft.cockpit.gameObject.AddComponent<Light>();
		CycleColors();
	}

	private void StoreColors()
	{
		Image[] componentsInChildren = base.gameObject.GetComponentsInChildren<Image>();
		foreach (Image image in componentsInChildren)
		{
			if (image.color.g > 0.25f)
			{
				images.Add(image);
			}
		}
		texts = base.gameObject.GetComponentsInChildren<Text>();
		imageColors = new Color[images.Count];
		textColors = new Color[texts.Length];
		for (int j = 0; j < images.Count; j++)
		{
			imageColors[j] = images[j].color;
		}
		for (int k = 0; k < texts.Length; k++)
		{
			textColors[k] = texts[k].color;
		}
	}

	private void Beat()
	{
		if (started)
		{
			beatTimer += Time.deltaTime;
			if (beatTimer > 1f / flashSpeed)
			{
				beatTimer -= 1f / flashSpeed;
				CycleColors();
			}
			if (aircraft != null && (aircraft.disabled || Time.timeSinceLevelLoad - startTime > duration))
			{
				Object.Destroy(this);
			}
		}
	}

	private void CycleColors()
	{
		currentColor = Color.HSVToRGB(0.5f * (Mathf.Sin(Time.timeSinceLevelLoad * cycleSpeed) + 1f), 1f, 1f);
		light.color = currentColor;
		light.intensity = ((NetworkSceneSingleton<LevelInfo>.i.timeOfDay < 6f || NetworkSceneSingleton<LevelInfo>.i.timeOfDay > 18f) ? 0.1f : 1f);
		lastBeat = Time.timeSinceLevelLoad;
		SceneSingleton<CameraStateManager>.i.ShakeCamera(0f, 1f);
	}

	private void UpdateColors()
	{
		Color color = Color.white * Mathf.Max(1f - (Time.timeSinceLevelLoad - lastBeat) * 4f, 0f);
		for (int i = 0; i < images.Count; i++)
		{
			images[i].color = currentColor.WithAlpha(imageColors[i].a) + color * 0.5f;
		}
		for (int j = 0; j < texts.Length; j++)
		{
			texts[j].color = currentColor.WithAlpha(textColors[j].a) + color * 0.5f;
		}
	}

	private void RestoreColors()
	{
		for (int i = 0; i < images.Count; i++)
		{
			images[i].color = imageColors[i];
		}
		for (int j = 0; j < texts.Length; j++)
		{
			texts[j].color = textColors[j];
		}
	}

	private void OnDestroy()
	{
		if (started)
		{
			RestoreColors();
			light.intensity = 0f;
		}
	}

	private void SetAircraft()
	{
		aircraft = SceneSingleton<CombatHUD>.i.aircraft;
		if (aircraft != null && MusicManager.i.HasPlayedMusic(aircraft.definition.aircraftParameters.takeoffMusic))
		{
			Object.Destroy(this);
		}
	}

	private void Update()
	{
		if (!started)
		{
			if (aircraft == null)
			{
				SetAircraft();
				return;
			}
			if (!(aircraft.radarAlt > 1f))
			{
				return;
			}
			StartPlaying();
		}
		Beat();
		UpdateColors();
	}
}
