using System;
using UnityEngine;

public class NavLights : MonoBehaviour
{
	private enum ControlledState
	{
		Never = 0,
		Auto = 1,
		ForceOn = 2
	}

	[Serializable]
	private class NavLight
	{
		[SerializeField]
		private Renderer renderer;

		[SerializeField]
		private GameObject[] objects;

		[SerializeField]
		private Material litMaterial;

		[SerializeField]
		private UnitPart part;

		[SerializeField]
		private LandingGear gear;

		[SerializeField]
		private ControlledState controlState;

		private Material baseMaterial;

		private Aircraft aircraft;

		private NavLights manager;

		private bool isOn;

		public void Initialize(Aircraft aircraft, NavLights navLights)
		{
			this.aircraft = aircraft;
			manager = navLights;
			baseMaterial = renderer.material;
			aircraft.onSetGear += NavLight_OnSetGear;
			if (part != null)
			{
				part.onParentDetached += NavLight_OnDetachedFromUnit;
			}
			if (gear != null)
			{
				gear.onGearBreak += NavLight_OnGearBreak;
			}
			isOn = false;
			Toggle(isOn);
		}

		private void NavLight_OnSetGear(Aircraft.OnSetGear e)
		{
			bool enabled = e.gearState == LandingGear.GearState.LockedExtended || e.gearState == LandingGear.GearState.Extending;
			Toggle(enabled);
		}

		private void Toggle(bool enabled)
		{
			isOn = enabled || controlState == ControlledState.ForceOn;
			renderer.material = (isOn ? litMaterial : baseMaterial);
			GameObject[] array = objects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(isOn);
			}
			manager.SetState(isOn);
		}

		private void NavLight_OnGearBreak(LandingGear landingGear)
		{
			controlState = ControlledState.Auto;
			Toggle(enabled: false);
			gear.onGearBreak -= NavLight_OnGearBreak;
			if (part != null)
			{
				part.onParentDetached -= NavLight_OnDetachedFromUnit;
			}
			aircraft.onSetGear -= NavLight_OnSetGear;
		}

		private void NavLight_OnDetachedFromUnit(UnitPart parentPart)
		{
			controlState = ControlledState.Auto;
			Toggle(enabled: false);
			aircraft.onSetGear -= NavLight_OnSetGear;
			part.onParentDetached -= NavLight_OnDetachedFromUnit;
			if (gear != null)
			{
				gear.onGearBreak -= NavLight_OnGearBreak;
			}
		}

		public void ToggleState()
		{
			if (controlState != ControlledState.Never)
			{
				if (controlState == ControlledState.Auto)
				{
					controlState = ControlledState.ForceOn;
				}
				else
				{
					controlState = ControlledState.Auto;
				}
				if (aircraft.gearState == LandingGear.GearState.LockedRetracted)
				{
					Toggle(!isOn);
				}
			}
		}
	}

	[SerializeField]
	private NavLight[] navLights;

	[SerializeField]
	private Aircraft aircraft;

	private bool isOn;

	private void Awake()
	{
		NavLight[] array = navLights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(aircraft, this);
		}
	}

	public void ToggleNavLights()
	{
		NavLight[] array = navLights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ToggleState();
		}
	}

	public void SetState(bool newState)
	{
		if (SceneSingleton<CameraStateManager>.i.currentState != SceneSingleton<CameraStateManager>.i.selectionState && SceneSingleton<CameraStateManager>.i.currentState != SceneSingleton<CameraStateManager>.i.encyclopediaState && newState != isOn)
		{
			isOn = newState;
			if (SceneSingleton<CombatHUD>.i != null && aircraft == SceneSingleton<CombatHUD>.i.aircraft)
			{
				string report = (isOn ? "Nav Lights <b>On</b>" : "Nav Lights <b>Off</b>");
				SceneSingleton<AircraftActionsReport>.i.ReportText(report, 5f);
			}
		}
	}
}
