using UnityEngine;
using UnityEngine.UI;

public class AutopilotIndicator : HUDApp
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private float maxSpeed = 27f;

	private Aircraft aircraft;

	public override void Initialize(Aircraft aircraft)
	{
		aircraft.GetControlsFilter().OnSetAutoHover += AutopilotIndicator_OnSetAutopilot;
		icon.enabled = aircraft.GetControlsFilter().IsAutoHoverEnabled();
		this.aircraft = aircraft;
	}

	public new void RefreshSettings()
	{
	}

	public override void Refresh()
	{
		if (icon.enabled)
		{
			icon.color = ((aircraft.speed > maxSpeed) ? (Color.green * 0.5f + Color.red) : Color.green);
		}
	}

	private void OnDestroy()
	{
		if (aircraft != null)
		{
			aircraft.GetControlsFilter().OnSetAutoHover -= AutopilotIndicator_OnSetAutopilot;
		}
	}

	private void AutopilotIndicator_OnSetAutopilot()
	{
		icon.enabled = aircraft.GetControlsFilter().IsAutoHoverEnabled();
	}
}
