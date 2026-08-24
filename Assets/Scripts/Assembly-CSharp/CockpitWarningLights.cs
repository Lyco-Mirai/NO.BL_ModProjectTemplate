using System.Collections.Generic;
using UnityEngine;

public class CockpitWarningLights : MonoBehaviour
{
	[SerializeField]
	private Renderer[] lightRenderers;

	[SerializeField]
	private Aircraft aircraft;

	private MissileWarning missileWarning;

	private List<Missile> knownMissiles;

	private void Awake()
	{
		aircraft.onInitialize += CockpitWarningLight_OnInitialize;
		base.enabled = false;
	}

	public void CockpitWarningLight_OnInitialize()
	{
		Renderer[] array = lightRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		if (GameManager.IsLocalAircraft(aircraft))
		{
			missileWarning = aircraft.GetMissileWarningSystem();
			knownMissiles = missileWarning.knownMissiles;
			missileWarning.onMissileWarning += CockpitWarningLights_OnMissileWarning;
			aircraft.onDisableUnit += CockpitWarningLights_OnDisable;
		}
		else
		{
			Object.Destroy(this);
		}
	}

	private void CockpitWarningLights_OnDisable(Unit unit)
	{
		missileWarning.onMissileWarning -= CockpitWarningLights_OnMissileWarning;
		base.enabled = false;
	}

	private void CockpitWarningLights_OnMissileWarning(MissileWarning.OnMissileWarning _)
	{
		base.enabled = true;
	}

	private void Update()
	{
		if (knownMissiles.Count > 0)
		{
			bool flag = Mathf.Sin(Time.timeSinceLevelLoad * 20f) > 0f;
			Renderer[] array = lightRenderers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = flag;
			}
		}
		else
		{
			base.enabled = false;
			Renderer[] array = lightRenderers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
		}
	}
}
