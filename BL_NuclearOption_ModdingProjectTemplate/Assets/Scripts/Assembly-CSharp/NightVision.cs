using System;
using NuclearOption.MissionEditorScripts;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NightVision : MonoBehaviour
{
	[SerializeField]
	private Volume postProcessing;

	public static NightVision i;

	private bool nightVisSelected;

	private bool nightVisActive;

	[SerializeField]
	private float gainMin;

	[SerializeField]
	private float gainMax;

	[SerializeField]
	private float bloomThresholdMin;

	[SerializeField]
	private float bloomThresholdMax;

	private ColorAdjustments colorAdjustments;

	private Bloom bloom;

	private float gainLastUpdated;

	private void Awake()
	{
		i = this;
	}

	private void Start()
	{
		SceneSingleton<CameraStateManager>.i.onSwitchCamera += NightVis_OnSwitchCam;
		if (!postProcessing.profile.TryGet<ColorAdjustments>(out colorAdjustments))
		{
			throw new NullReferenceException("colorAdjustments");
		}
		if (!postProcessing.profile.TryGet<Bloom>(out bloom))
		{
			throw new NullReferenceException("bloom");
		}
	}

	private void NightVis_OnSwitchCam()
	{
		if (PlayerSettings.cameraAutoNVG && (NetworkSceneSingleton<LevelInfo>.i.timeOfDay < 5.6f || NetworkSceneSingleton<LevelInfo>.i.timeOfDay > 18.4f))
		{
			nightVisSelected = true;
		}
	}

	private void UpdateGain()
	{
		if (!(Time.unscaledTime - gainLastUpdated < 1f))
		{
			gainLastUpdated = Time.unscaledTime;
			float t = Mathf.InverseLerp(0.01f, 0.4f, NetworkSceneSingleton<LevelInfo>.i.GetAmbientLight());
			float value = Mathf.Lerp(gainMax, gainMin, t);
			float value2 = Mathf.Lerp(bloomThresholdMin, bloomThresholdMax, t);
			colorAdjustments.postExposure.value = value;
			bloom.threshold.value = value2;
		}
	}

	private bool BlockToggle()
	{
		if (InputFieldChecker.InsideInputField)
		{
			return true;
		}
		CursorFlags flags = CursorManager.GetFlags();
		if (GameManager.gameState == GameState.Editor)
		{
			return CursorManager.GetFlags() != CursorFlags.NotInGame;
		}
		return flags != CursorFlags.None;
	}

	public static void Toggle()
	{
		if (!(i == null) && !i.BlockToggle())
		{
			i.nightVisSelected = !i.nightVisSelected;
		}
	}

	private void Update()
	{
		if (BlockToggle())
		{
			return;
		}
		if (nightVisSelected)
		{
			if (!nightVisActive)
			{
				nightVisActive = true;
				postProcessing.enabled = true;
				NetworkSceneSingleton<LevelInfo>.i.PostProcessing.enabled = false;
			}
		}
		else if (nightVisActive)
		{
			nightVisActive = false;
			postProcessing.enabled = false;
			NetworkSceneSingleton<LevelInfo>.i.PostProcessing.enabled = true;
		}
		if (nightVisActive)
		{
			UpdateGain();
		}
		if (GameManager.playerInput.GetButtonDown("Night Vis"))
		{
			Toggle();
		}
	}
}
