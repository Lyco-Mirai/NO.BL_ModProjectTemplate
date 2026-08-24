using System;
using UnityEngine;

[Serializable]
public class HangarLighting
{
	public Renderer lightRenderer;

	public Light light;

	public bool onlyAtNight = true;

	public void Enable(bool state)
	{
		bool flag = state;
		if (onlyAtNight)
		{
			flag = flag && !NetworkSceneSingleton<LevelInfo>.i.isDayLight;
		}
		if (light != null)
		{
			light.enabled = flag;
		}
		if (lightRenderer != null)
		{
			lightRenderer.enabled = flag;
		}
	}

	public void OnUnitDisabled()
	{
		if (light != null)
		{
			light.enabled = false;
		}
		if (lightRenderer != null)
		{
			lightRenderer.enabled = false;
		}
	}
}
