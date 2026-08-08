using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class GLOC : MonoBehaviour
{
	[SerializeField]
	private float bloodPumpRate = 0.28f;

	private float stamina = 1f;

	private float staminaRecoveryRate = 0.18f;

	private float bloodPressure = 1f;

	private bool conscious = true;

	private ColorAdjustments colorAdjustments;

	private Vignette vignette;

	private Image blackoutImage;

	private void OnEnable()
	{
		if (!SceneSingleton<CameraStateManager>.i.GetPostProcessVolume().profile.TryGet<ColorAdjustments>(out colorAdjustments))
		{
			throw new NullReferenceException("colorAdjustments");
		}
		if (!SceneSingleton<CameraStateManager>.i.GetPostProcessVolume().profile.TryGet<Vignette>(out vignette))
		{
			throw new NullReferenceException("colorAdjustments");
		}
		blackoutImage = SceneSingleton<CameraStateManager>.i.GetBlackoutImage();
		SceneSingleton<CameraStateManager>.i.onSwitchCamera += GLOC_OnSwitchCamera;
	}

	public void ResetGLOC()
	{
		blackoutImage.color = Color.clear;
		colorAdjustments.saturation.value = 0f;
		vignette.intensity.value = 0.4f;
	}

	public float SimulateGLOC(float gForce)
	{
		gForce = Mathf.Abs(gForce);
		if (conscious)
		{
			float num = bloodPumpRate;
			num -= gForce * 0.04f;
			if (bloodPressure < 0.55f)
			{
				num += stamina * 0.25f;
			}
			if (bloodPressure < 0.6f && !blackoutImage.enabled)
			{
				blackoutImage.enabled = true;
			}
			else if (bloodPressure >= 0.6f && blackoutImage.enabled)
			{
				blackoutImage.enabled = false;
			}
			stamina += (staminaRecoveryRate - gForce * 0.04f) * Time.deltaTime;
			stamina = Mathf.Clamp01(stamina);
			bloodPressure += num * Time.deltaTime;
			bloodPressure = Mathf.Clamp(bloodPressure, 0f, 1f);
			if (bloodPressure < 0.2f)
			{
				LOC().Forget();
				conscious = false;
			}
		}
		if (CameraStateManager.cameraMode == CameraMode.cockpit)
		{
			float num2 = (bloodPressure - 0.2f) / 0.4f;
			float num3 = (bloodPressure - 0.3f) / 0.4f;
			blackoutImage.color = Color.Lerp(Color.black, Color.clear, num2);
			colorAdjustments.saturation.value = Mathf.Lerp(-100f, 0f, num3);
			vignette.intensity.value = Mathf.Lerp(1f, 0.4f, num2);
			AudioMixerVolume.SetMasterAudioFilterStrength(Mathf.Lerp(250f, 11000f, Mathf.Clamp01(num2)) + 11000f * Mathf.Clamp01(num3));
		}
		return bloodPressure;
	}

	private void GLOC_OnSwitchCamera()
	{
		if (CameraStateManager.cameraMode != CameraMode.cockpit)
		{
			blackoutImage.color = Color.clear;
			colorAdjustments.saturation.value = 0f;
			vignette.intensity.value = 0.4f;
			AudioMixerVolume.SetMasterAudioFilterStrength(22000f);
		}
	}

	private void OnDestroy()
	{
		SceneSingleton<CameraStateManager>.i.onSwitchCamera -= GLOC_OnSwitchCamera;
		blackoutImage.color = Color.clear;
		colorAdjustments.saturation.value = 0f;
		vignette.intensity.value = 0.4f;
	}

	private async UniTask LOC()
	{
		blackoutImage.enabled = true;
		blackoutImage.color = new Color(0f, 0f, 0f, 1f);
		await UniTask.Delay((int)(UnityEngine.Random.Range(3f, 6f) * 1000f));
		bloodPressure = 0.2f;
		conscious = true;
	}
}
