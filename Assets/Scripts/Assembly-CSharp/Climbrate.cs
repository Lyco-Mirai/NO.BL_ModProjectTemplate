using TMPro;
using UnityEngine;

public class Climbrate : HUDApp
{
	[SerializeField]
	private TextMeshProUGUI climbRate;

	[SerializeField]
	private Aircraft aircraft;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		climbRate.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			float speed = Vector3.Dot(aircraft.CockpitRB().velocity, Vector3.up);
			climbRate.text = UnitConverter.ClimbRateReading(speed);
		}
	}
}
