using UnityEngine;
using UnityEngine.UI;

public class ControlsFilterTuner : MonoBehaviour
{
	[SerializeField]
	private Slider basePitchDampingSlider;

	[SerializeField]
	private Slider velocityPitchDampingSlider;

	[SerializeField]
	private Slider maxResponseAirspeedSlider;

	[SerializeField]
	private Slider maxResponseRateSlider;

	[SerializeField]
	private Slider pitchTrimRateSlider;

	[SerializeField]
	private Slider pitchTrimLimitSlider;

	[SerializeField]
	private Slider gearTrimSlider;

	[SerializeField]
	private Text basePitchDampingValue;

	[SerializeField]
	private Text velocityPitchDampingValue;

	[SerializeField]
	private Text maxResponseAirspeedValue;

	[SerializeField]
	private Text maxResponseRateValue;

	[SerializeField]
	private Text pitchTrimRateValue;

	[SerializeField]
	private Text pitchTrimLimitValue;

	[SerializeField]
	private Text gearTrimValue;

	private ControlsFilter controlsFilter;

	public void SetControlsFilter(ControlsFilter filter, float basePitchDamping, float velocityPitchDamping, float maxResponseAirspeed, float maxResponseRate, float pitchTrimRate, float pitchTrimLimit, float gearTrim)
	{
		controlsFilter = filter;
		basePitchDampingSlider.SetValueWithoutNotify(basePitchDamping);
		velocityPitchDampingSlider.SetValueWithoutNotify(velocityPitchDamping);
		maxResponseAirspeedSlider.SetValueWithoutNotify(maxResponseAirspeed);
		maxResponseRateSlider.SetValueWithoutNotify(maxResponseRate);
		pitchTrimRateSlider.SetValueWithoutNotify(pitchTrimRate);
		pitchTrimLimitSlider.SetValueWithoutNotify(pitchTrimLimit);
		gearTrimSlider.SetValueWithoutNotify(gearTrim);
		ApplyValues();
	}

	public void ApplyValues()
	{
		_ = controlsFilter != null;
		basePitchDampingValue.text = basePitchDampingSlider.value.ToString("F2");
		velocityPitchDampingValue.text = velocityPitchDampingSlider.value.ToString("F3");
		maxResponseAirspeedValue.text = maxResponseAirspeedSlider.value.ToString("F0");
		maxResponseRateValue.text = maxResponseRateSlider.value.ToString("F2");
		pitchTrimRateValue.text = pitchTrimRateSlider.value.ToString("F2");
		pitchTrimLimitValue.text = pitchTrimLimitSlider.value.ToString("F2");
		gearTrimValue.text = gearTrimSlider.value.ToString("F2");
	}
}
