using System;
using System.Collections.Generic;
using UnityEngine;

public class Cockpit : MonoBehaviour
{
	[Serializable]
	private class Joystick
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private float range;

		public void Animate(ControlInputs inputs)
		{
			transform.localEulerAngles = new Vector3(inputs.pitch * range, 0f, (0f - inputs.roll) * range);
		}
	}

	[Serializable]
	private class Throttle
	{
		[SerializeField]
		private bool rotation;

		[SerializeField]
		private bool motion;

		[SerializeField]
		private Transform transform;

		[SerializeField]
		private float range;

		public void Animate(ControlInputs inputs)
		{
			if (rotation)
			{
				transform.localEulerAngles = new Vector3(inputs.throttle * range, 0f, 0f);
			}
			if (motion)
			{
				transform.localPosition = new Vector3(0f, 0f, inputs.throttle * range);
			}
		}
	}

	[SerializeField]
	private Renderer tacScreenRender;

	[SerializeField]
	private GameObject tacScreenUIPrefab;

	[SerializeField]
	private Aircraft aircraft;

	private TacScreen tacScreen;

	[SerializeField]
	private GameObject[] engineSources;

	private List<IEngine> engineStates = new List<IEngine>();

	[SerializeField]
	private Joystick[] joysticks;

	[SerializeField]
	private Throttle[] throttles;

	private ControlInputs inputs;

	private void Awake()
	{
		aircraft.onInitialize += Cockpit_OnAircraftInitialize;
		for (int i = 0; i < engineSources.Length; i++)
		{
			engineStates.Add(engineSources[i].GetComponent<IEngine>());
		}
		inputs = aircraft.GetInputs();
	}

	private void Update()
	{
		Joystick[] array = joysticks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Animate(inputs);
		}
		Throttle[] array2 = throttles;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Animate(inputs);
		}
	}

	public List<IEngine> GetEngineStates()
	{
		return engineStates;
	}

	private void Cockpit_OnAircraftInitialize()
	{
		if (SceneSingleton<CombatHUD>.i != null && SceneSingleton<CombatHUD>.i.aircraft != null && SceneSingleton<CombatHUD>.i.aircraft == aircraft)
		{
			tacScreen = UnityEngine.Object.Instantiate(tacScreenUIPrefab, base.transform).GetComponent<TacScreen>();
			tacScreen.Initialize(aircraft, this);
			aircraft.onDisableUnit += Cockpit_OnAircraftDisable;
			base.enabled = true;
		}
		else
		{
			base.enabled = false;
		}
	}

	private void Cockpit_OnAircraftDisable(Unit unit)
	{
		UnityEngine.Object.Destroy(tacScreen.gameObject);
	}
}
