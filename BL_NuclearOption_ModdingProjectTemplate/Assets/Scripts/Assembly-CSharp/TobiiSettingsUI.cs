using UnityEngine;
using UnityEngine.UI;

public class TobiiSettingsUI : MonoBehaviour
{
	[Header("UI References")]
	public Slider eyeVsHeadRatioSlider;

	public Slider eyeTrackingResponsivenessSlider;

	public Slider headSensitivityPitchYawSlider;

	public Slider centerStabilizationSlider;

	public Slider headSensitivityRollSlider;

	public Slider headSensitivityPositionSlider;

	public Toggle headTrackingAutoCenterToggle;

	[Header("UI Labels")]
	public Text eyeVsHeadRatioSliderLabel;

	public Text eyeTrackingResponsivenessSliderLabel;

	public Text headSensitivityPitchYawSliderLabel;

	public Text centerStabilizationSliderLabel;

	public Text headSensitivityRollSliderLabel;

	public Text headSensitivityPositionSliderLabel;

	[Header("Default Values")]
	public readonly float defaultEyeVsHeadRatio = 1f;

	public readonly float defaultEyeTrackingResponsiveness = 1f;

	public readonly float defaultHeadSensitivityPitchYaw = 2f;

	public readonly float defaultCenterStabilization = 1f;

	public readonly float defaultHeadSensitivityRoll = 1f;

	public readonly float defaultHeadSensitivityPosition = 1f;

	public readonly bool defaultHeadTrackingAutoCenter = true;

	private void Awake()
	{
		GameManager.tobiiSettingsUI = this;
	}

	private void Start()
	{
		InitializeUI();
	}

	public void Open()
	{
		base.gameObject.SetActive(value: true);
		Canvas componentInChildren = GetComponentInChildren<Canvas>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.gameObject.SetActive(value: true);
		}
		base.transform.SetAsLastSibling();
	}

	private void InitializeUI()
	{
		if (eyeTrackingResponsivenessSlider != null)
		{
			eyeTrackingResponsivenessSlider.SetValueWithoutNotify(PlayerSettings.tobiiEyeTrackingResponsiveness);
			eyeTrackingResponsivenessSlider.onValueChanged.AddListener(OnEyeTrackingResponsivenessChanged);
		}
		if (eyeVsHeadRatioSlider != null)
		{
			eyeVsHeadRatioSlider.SetValueWithoutNotify(PlayerSettings.tobiiEyeVsHeadRatio);
			eyeVsHeadRatioSlider.onValueChanged.AddListener(OnEyeVsHeadRatioChanged);
		}
		if (headSensitivityPitchYawSlider != null)
		{
			headSensitivityPitchYawSlider.SetValueWithoutNotify(PlayerSettings.tobiiHeadSensitivityPitchYaw);
			headSensitivityPitchYawSlider.onValueChanged.AddListener(OnHeadSensitivityPitchYawChanged);
		}
		if (centerStabilizationSlider != null)
		{
			centerStabilizationSlider.SetValueWithoutNotify(PlayerSettings.tobiiCenterStabilization);
			centerStabilizationSlider.onValueChanged.AddListener(OnCenterStabilizationChanged);
		}
		if (headSensitivityRollSlider != null)
		{
			headSensitivityRollSlider.SetValueWithoutNotify(PlayerSettings.tobiiHeadSensitivityRoll);
			headSensitivityRollSlider.onValueChanged.AddListener(OnHeadSensitivityRollChanged);
		}
		if (headSensitivityPositionSlider != null)
		{
			headSensitivityPositionSlider.SetValueWithoutNotify(PlayerSettings.tobiiHeadSensitivityPosition);
			headSensitivityPositionSlider.onValueChanged.AddListener(OnHeadSensitivityPositionChanged);
		}
		if (headTrackingAutoCenterToggle != null)
		{
			headTrackingAutoCenterToggle.SetIsOnWithoutNotify(PlayerSettings.tobiiHeadTrackingAutoCenter);
			headTrackingAutoCenterToggle.onValueChanged.AddListener(OnHeadTrackingAutoCenterChanged);
		}
		UpdateLabels();
	}

	public void OnEyeVsHeadRatioChanged(float value)
	{
		PlayerSettings.tobiiEyeVsHeadRatio = value;
		PlayerPrefs.SetFloat("Tobii_EyeVsHeadRatio", value);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void OnEyeTrackingResponsivenessChanged(float value)
	{
		PlayerSettings.tobiiEyeTrackingResponsiveness = value;
		PlayerPrefs.SetFloat("Tobii_EyeTrackingResponsiveness", value);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void OnHeadSensitivityPitchYawChanged(float value)
	{
		PlayerSettings.tobiiHeadSensitivityPitchYaw = value;
		PlayerPrefs.SetFloat("Tobii_HeadSensitivityPitchYaw", value);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void OnCenterStabilizationChanged(float value)
	{
		PlayerSettings.tobiiCenterStabilization = value;
		PlayerPrefs.SetFloat("Tobii_CenterStabilization", value);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void OnHeadSensitivityRollChanged(float value)
	{
		PlayerSettings.tobiiHeadSensitivityRoll = value;
		PlayerPrefs.SetFloat("Tobii_HeadSensitivityRoll", value);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void OnHeadSensitivityPositionChanged(float value)
	{
		PlayerSettings.tobiiHeadSensitivityPosition = value;
		PlayerPrefs.SetFloat("Tobii_HeadSensitivityPosition", value);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void OnHeadTrackingAutoCenterChanged(bool value)
	{
		PlayerSettings.tobiiHeadTrackingAutoCenter = value;
		PlayerPrefs.SetInt("Tobii_HeadTrackingAutoCenter", value ? 1 : 0);
		PlayerSettings.ApplyTobiiSettings();
		UpdateLabels();
	}

	public void UpdateLabels()
	{
		eyeVsHeadRatioSliderLabel.text = "Eye VS Head Tracking Ratio (" + eyeVsHeadRatioSlider.value.ToString("F2") + ")";
		eyeTrackingResponsivenessSliderLabel.text = "Eye Tracking Responsiveness (" + eyeTrackingResponsivenessSlider.value.ToString("F2") + ")";
		headSensitivityPitchYawSliderLabel.text = "Head Tracking Pitch & Yaw Sensitivity (" + headSensitivityPitchYawSlider.value.ToString("F2") + ")";
		centerStabilizationSliderLabel.text = "Center Stabilization (" + centerStabilizationSlider.value.ToString("F2") + ")";
		headSensitivityRollSliderLabel.text = "Head Tracking Roll Sensitivity (" + headSensitivityRollSlider.value.ToString("F2") + ")";
		headSensitivityPositionSliderLabel.text = "Head Tracking Position Sensitivity (" + headSensitivityPositionSlider.value.ToString("F2") + ")";
	}

	public void SaveAndClose()
	{
		if (eyeVsHeadRatioSlider != null)
		{
			PlayerSettings.tobiiEyeVsHeadRatio = eyeVsHeadRatioSlider.value;
			PlayerPrefs.SetFloat("Tobii_EyeVsHeadRatio", eyeVsHeadRatioSlider.value);
		}
		if (eyeTrackingResponsivenessSlider != null)
		{
			PlayerSettings.tobiiEyeTrackingResponsiveness = eyeTrackingResponsivenessSlider.value;
			PlayerPrefs.SetFloat("Tobii_EyeTrackingResponsiveness", eyeTrackingResponsivenessSlider.value);
		}
		if (headSensitivityPitchYawSlider != null)
		{
			PlayerSettings.tobiiHeadSensitivityPitchYaw = headSensitivityPitchYawSlider.value;
			PlayerPrefs.SetFloat("Tobii_HeadSensitivityPitchYaw", headSensitivityPitchYawSlider.value);
		}
		if (centerStabilizationSlider != null)
		{
			PlayerSettings.tobiiCenterStabilization = centerStabilizationSlider.value;
			PlayerPrefs.SetFloat("Tobii_CenterStabilization", centerStabilizationSlider.value);
		}
		if (headSensitivityRollSlider != null)
		{
			PlayerSettings.tobiiHeadSensitivityRoll = headSensitivityRollSlider.value;
			PlayerPrefs.SetFloat("Tobii_HeadSensitivityRoll", headSensitivityRollSlider.value);
		}
		if (headSensitivityPositionSlider != null)
		{
			PlayerSettings.tobiiHeadSensitivityPosition = headSensitivityPositionSlider.value;
			PlayerPrefs.SetFloat("Tobii_HeadSensitivityPosition", headSensitivityPositionSlider.value);
		}
		if (headTrackingAutoCenterToggle != null)
		{
			PlayerSettings.tobiiHeadTrackingAutoCenter = headTrackingAutoCenterToggle.isOn;
			PlayerPrefs.SetInt("Tobii_HeadTrackingAutoCenter", headTrackingAutoCenterToggle.isOn ? 1 : 0);
		}
		PlayerPrefs.Save();
		base.gameObject.SetActive(value: false);
		if (SceneSingleton<GameplayUI>.i != null)
		{
			SceneSingleton<GameplayUI>.i.menuCanvas.enabled = true;
		}
	}

	public void ResetToDefaults()
	{
		if (eyeVsHeadRatioSlider != null)
		{
			eyeVsHeadRatioSlider.value = defaultEyeVsHeadRatio;
		}
		if (eyeTrackingResponsivenessSlider != null)
		{
			eyeTrackingResponsivenessSlider.value = defaultEyeTrackingResponsiveness;
		}
		if (headSensitivityPitchYawSlider != null)
		{
			headSensitivityPitchYawSlider.value = defaultHeadSensitivityPitchYaw;
		}
		if (centerStabilizationSlider != null)
		{
			centerStabilizationSlider.value = defaultCenterStabilization;
		}
		if (headSensitivityRollSlider != null)
		{
			headSensitivityRollSlider.value = defaultHeadSensitivityRoll;
		}
		if (headSensitivityPositionSlider != null)
		{
			headSensitivityPositionSlider.value = defaultHeadSensitivityPosition;
		}
		if (headTrackingAutoCenterToggle != null)
		{
			headTrackingAutoCenterToggle.isOn = defaultHeadTrackingAutoCenter;
		}
		PlayerPrefs.Save();
	}
}
