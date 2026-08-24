using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BasicFlightInstruments : HUDApp
{
	private Aircraft aircraft;

	[SerializeField]
	private TextMeshProUGUI airspeedDisplay;

	[SerializeField]
	private TextMeshProUGUI altitudeDisplay;

	[SerializeField]
	private TextMeshProUGUI climbRateDisplay;

	[SerializeField]
	private TextMeshProUGUI headingDisplay;

	[SerializeField]
	private Image horizonDisplay;

	[SerializeField]
	private Image skyDisplay;

	[SerializeField]
	private Image verticalSpeedIndicator;

	[SerializeField]
	private Image AoAIndicator;

	private float lastUpdate;

	public override void Initialize(Aircraft aircraft)
	{
		if (!(aircraft == null))
		{
			this.aircraft = aircraft;
		}
	}

	public override void Refresh()
	{
		if (!(aircraft == null) && !(Time.timeSinceLevelLoad - lastUpdate < 0.05f))
		{
			lastUpdate = Time.timeSinceLevelLoad;
			airspeedDisplay.text = UnitConverter.SpeedReading(aircraft.speed);
			altitudeDisplay.text = UnitConverter.AltitudeReading(aircraft.radarAlt) ?? "";
			float num = Vector3.Dot(aircraft.CockpitRB().velocity, Vector3.up);
			climbRateDisplay.text = UnitConverter.ClimbRateReading(num);
			verticalSpeedIndicator.transform.localPosition = new Vector3(0f, 1f * Mathf.Clamp(num, -50f, 50f), 0f);
			headingDisplay.text = $"{aircraft.transform.eulerAngles.y:F0}°";
			horizonDisplay.transform.localEulerAngles = new Vector3(0f, 0f, 0f - aircraft.transform.eulerAngles.z);
			float num2 = aircraft.transform.eulerAngles.x;
			if (num2 > 180f)
			{
				num2 -= 360f;
			}
			horizonDisplay.fillAmount = Mathf.Clamp(0.5f + num2 / 180f, 0f, 1f);
			skyDisplay.fillAmount = Mathf.Clamp(0.5f - num2 / 180f, 0f, 1f);
			if (aircraft.speed > 10f)
			{
				Vector3 vector = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity);
				float value = Mathf.Atan2(vector.y, vector.z) * -57.29578f;
				AoAIndicator.transform.localPosition = new Vector3(0f, 1.6f * Mathf.Clamp(value, -30f, 30f), 0f);
			}
			else
			{
				AoAIndicator.transform.localPosition = Vector3.zero;
			}
		}
	}
}
