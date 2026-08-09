using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Altitude : HUDApp
{
	[SerializeField]
	private TextMeshProUGUI radarAlt;

	[SerializeField]
	private TextMeshProUGUI absAlt;

	[SerializeField]
	private Aircraft aircraft;

	[SerializeField]
	private Image border;

	public override void Initialize(Aircraft aircraft)
	{
		this.aircraft = aircraft;
	}

	public override void RefreshSettings()
	{
		base.RefreshSettings();
		radarAlt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
		absAlt.fontSize = (int)((float)fontSize * fontSizeMultiplier);
	}

	public override void Refresh()
	{
		if (!(aircraft == null))
		{
			radarAlt.text = "R[" + UnitConverter.AltitudeReading(aircraft.radarAlt) + "]";
			absAlt.text = UnitConverter.AltitudeReading(aircraft.transform.position.GlobalY());
		}
	}
}
