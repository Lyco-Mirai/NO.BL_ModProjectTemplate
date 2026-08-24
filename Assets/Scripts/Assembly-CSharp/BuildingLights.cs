using System.Linq;
using UnityEngine;

public class BuildingLights : MonoBehaviour
{
	[SerializeField]
	private bool daylightToggle = true;

	[SerializeField]
	private Renderer[] renderers;

	[SerializeField]
	private Light[] lights;

	[SerializeField]
	private UnitPart[] dependentParts;

	private void Awake()
	{
	}

	private void Start()
	{
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			return;
		}
		if (dependentParts != null && dependentParts.Count() > 0)
		{
			UnitPart[] array = dependentParts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].onApplyDamage += BuildingLights_OnPartDamage;
			}
		}
		if (daylightToggle)
		{
			NetworkSceneSingleton<LevelInfo>.i.onDaylightChange += BuildingLights_onDaylightChange;
			BuildingLights_onDaylightChange();
		}
	}

	private void BuildingLights_OnPartDamage(UnitPart.OnApplyDamage e)
	{
		if (!(e.hitPoints < 50f))
		{
			return;
		}
		if (renderers != null)
		{
			Renderer[] array = renderers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
		if (lights != null)
		{
			Light[] array2 = lights;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = false;
			}
		}
		UnitPart[] array3 = dependentParts;
		foreach (UnitPart unitPart in array3)
		{
			if (unitPart != null)
			{
				unitPart.onApplyDamage -= BuildingLights_OnPartDamage;
			}
		}
		if (daylightToggle)
		{
			NetworkSceneSingleton<LevelInfo>.i.onDaylightChange -= BuildingLights_onDaylightChange;
		}
		Object.Destroy(this);
	}

	private void BuildingLights_onDaylightChange()
	{
		bool flag = !NetworkSceneSingleton<LevelInfo>.i.isDayLight;
		if (renderers != null)
		{
			Renderer[] array = renderers;
			foreach (Renderer renderer in array)
			{
				if (renderer != null)
				{
					renderer.enabled = flag;
				}
			}
		}
		if (lights == null)
		{
			return;
		}
		Light[] array2 = lights;
		foreach (Light light in array2)
		{
			if (light != null)
			{
				light.enabled = flag;
			}
		}
	}
}
