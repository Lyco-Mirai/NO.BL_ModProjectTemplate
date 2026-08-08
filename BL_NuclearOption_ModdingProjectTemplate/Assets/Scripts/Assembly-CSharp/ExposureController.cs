using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class ExposureController
{
	private static ColorAdjustments colorAdjustments;

	private static List<Light> brightLights = new List<Light>();

	private static float exposure;

	private static float targetExposure;

	private static float adjustmentSpeed;

	private static float lastExposureUpdate;

	public static void SetColorAdjustments(ColorAdjustments colorAdjustments)
	{
		brightLights = new List<Light>();
		ExposureController.colorAdjustments = colorAdjustments;
	}

	public static void RegisterBrightLight(Light light)
	{
		brightLights.Add(light);
	}

	public static bool BrightLightsExist()
	{
		return brightLights.Count > 1;
	}

	public static void UpdateExposure()
	{
		if (colorAdjustments == null)
		{
			return;
		}
		if (Time.unscaledTime - lastExposureUpdate > 0.1f)
		{
			lastExposureUpdate = Time.unscaledTime;
			targetExposure = Mathf.LerpUnclamped(SceneSingleton<CameraStateManager>.i.maxExposure, SceneSingleton<CameraStateManager>.i.minExposure, NetworkSceneSingleton<LevelInfo>.i.GetAmbientLight() / SceneSingleton<CameraStateManager>.i.lightSensitivity);
			float num = 0f;
			for (int num2 = brightLights.Count - 1; num2 >= 0; num2--)
			{
				Light light = brightLights[num2];
				if (light == null)
				{
					brightLights.RemoveAt(num2);
				}
				else
				{
					Vector3 vector = light.transform.position - SceneSingleton<CameraStateManager>.i.transform.position;
					float num3 = light.intensity * 2f;
					if (light.type == LightType.Directional)
					{
						vector = -light.transform.forward;
					}
					else
					{
						num3 = light.intensity / vector.sqrMagnitude;
					}
					float num4 = Vector3.Dot(vector.normalized, SceneSingleton<CameraStateManager>.i.transform.forward) * 2f - 1f;
					if (!(num4 < 0f) && !Physics.Linecast(SceneSingleton<CameraStateManager>.i.transform.position, light.transform.position, PhysicsLayers.StaticsMask))
					{
						num += 0.05f * num4 * num3;
					}
				}
			}
			num = Mathf.Clamp(Mathf.Sqrt(num), 0f, 5f);
			targetExposure -= num;
		}
		float smoothTime = ((targetExposure < exposure) ? 0.5f : 4f);
		colorAdjustments.postExposure.value = Mathf.SmoothDamp(colorAdjustments.postExposure.value, targetExposure, ref adjustmentSpeed, smoothTime, 10f, Time.unscaledDeltaTime);
	}
}
