using TMPro;
using UnityEngine;

public class MachIndicator : HUDApp
{
	private Aircraft aircraft;

	private AircraftParameters aircraftParameters;

	[SerializeField]
	private TextMeshProUGUI machDisplay;

	public override void Initialize(Aircraft aircraft)
	{
		if (!(aircraft == null))
		{
			this.aircraft = aircraft;
			aircraftParameters = aircraft.definition.aircraftParameters;
		}
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		machDisplay.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			float speed = aircraft.speed;
			float speedOfSound = LevelInfo.GetSpeedOfSound(aircraft.GlobalPosition().y);
			machDisplay.text = $"{speed / speedOfSound:F2}";
		}
	}
}
