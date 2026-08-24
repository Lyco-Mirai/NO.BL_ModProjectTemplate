using UnityEngine;
using UnityEngine.UI;

public class HUDSettings : MonoBehaviour
{
	[SerializeField]
	private Toggle lagPipToggle;

	[SerializeField]
	private Text lagPipLabel;

	[SerializeField]
	private Toggle rangeCircleToggle;

	[SerializeField]
	private Text rangeCircleLabel;

	[SerializeField]
	private Slider HUDTimeSlider;

	[SerializeField]
	private Toggle gaugesToggle;

	[SerializeField]
	private Text gaugesLabel;

	[SerializeField]
	private Toggle HUDWeaponsToggle;

	[SerializeField]
	private Text HUDWeaponsLabel;

	[SerializeField]
	private Slider HMDWidthSlider;

	[SerializeField]
	private Text HMDWidthLabel;

	[SerializeField]
	private Slider HMDHeightSlider;

	[SerializeField]
	private Text HMDHeightLabel;

	[SerializeField]
	private Slider HMDSideDistSlider;

	[SerializeField]
	private Text HMDSideDistLabel;

	[SerializeField]
	private Slider HMDSideAngleSlider;

	[SerializeField]
	private Text HMDSideAngleLabel;

	[SerializeField]
	private Slider HMDTopHeightSlider;

	[SerializeField]
	private Text HMDTopHeightLabel;

	[SerializeField]
	private Slider HMDHideDistSlider;

	[SerializeField]
	private Text HMDHideDistLabel;

	[SerializeField]
	private Slider HMDIconSizeSlider;

	[SerializeField]
	private Text HMDIconSizeLabel;

	[SerializeField]
	private Slider HUDTextSizeSlider;

	[SerializeField]
	private Text HUDTextSizeLabel;

	[SerializeField]
	private Slider HMDTextSizeSlider;

	[SerializeField]
	private Text HMDTextSizeLabel;

	[SerializeField]
	private Slider OverlayTextSizeSlider;

	[SerializeField]
	private Text OverlayTextSizeLabel;

	[SerializeField]
	private Slider ColorRSlider;

	[SerializeField]
	private Text ColorRLabel;

	[SerializeField]
	private Slider ColorGSlider;

	[SerializeField]
	private Text ColorGLabel;

	[SerializeField]
	private Slider ColorBSlider;

	[SerializeField]
	private Text ColorBLabel;

	[SerializeField]
	private Image colorExample;

	private void Start()
	{
		float num = (float)Screen.width / (float)Screen.height;
		HMDWidthSlider.maxValue = num * 1080f;
		HMDHeightSlider.maxValue = 1080f;
		UpdateLabels();
	}

	private void UpdateLabels()
	{
		lagPipToggle.SetIsOnWithoutNotify(PlayerSettings.lagPip);
		lagPipLabel.text = (lagPipToggle.isOn ? "Lag" : "Lead");
		rangeCircleToggle.SetIsOnWithoutNotify(PlayerSettings.rangeCircle);
		rangeCircleLabel.text = (rangeCircleToggle.isOn ? "Circle" : "Ladder");
		HUDTimeSlider.SetValueWithoutNotify(PlayerSettings.hudTime);
		gaugesToggle.SetIsOnWithoutNotify(PlayerSettings.gauges);
		gaugesLabel.text = (gaugesToggle.isOn ? "Show" : "Hide");
		HUDWeaponsToggle.SetIsOnWithoutNotify(PlayerSettings.hudWeapons);
		HUDWeaponsLabel.text = (HUDWeaponsToggle.isOn ? "Show" : "Hide");
		HMDWidthSlider.SetValueWithoutNotify(PlayerSettings.hmdWidth);
		HMDWidthLabel.text = $"{HMDWidthSlider.value:F0}px";
		HMDHeightSlider.SetValueWithoutNotify(PlayerSettings.hmdHeight);
		HMDHeightLabel.text = $"{HMDHeightSlider.value:F0}px";
		HMDSideDistSlider.SetValueWithoutNotify(PlayerSettings.hmdSideDist);
		HMDSideDistLabel.text = $"{HMDSideDistSlider.value:F0}px";
		HMDSideAngleSlider.SetValueWithoutNotify(PlayerSettings.hmdSideAngle);
		HMDSideAngleLabel.text = $"{HMDSideAngleSlider.value:F0}°";
		HMDTopHeightSlider.SetValueWithoutNotify(PlayerSettings.hmdTopHeight);
		HMDTopHeightLabel.text = $"{HMDTopHeightSlider.value:F0}px";
		HMDHideDistSlider.SetValueWithoutNotify(PlayerSettings.hmdHideDist);
		HMDHideDistLabel.text = $"{100f * HMDHideDistSlider.value:F0}%";
		HMDIconSizeSlider.SetValueWithoutNotify(PlayerSettings.hmdIconSize);
		HMDIconSizeLabel.text = $"{HMDIconSizeSlider.value:F0}";
		HUDTextSizeSlider.SetValueWithoutNotify(PlayerSettings.hudTextSize);
		HUDTextSizeLabel.text = $"{HUDTextSizeSlider.value:F0}";
		HMDTextSizeSlider.SetValueWithoutNotify(PlayerSettings.hmdTextSize);
		HMDTextSizeLabel.text = $"{HMDTextSizeSlider.value:F0}";
		ColorRSlider.SetValueWithoutNotify(PlayerSettings.hudColorR);
		ColorRLabel.text = $"R {ColorRSlider.value:F0}";
		ColorGSlider.SetValueWithoutNotify(PlayerSettings.hudColorG);
		ColorGLabel.text = $"R {ColorGSlider.value:F0}";
		ColorBSlider.SetValueWithoutNotify(PlayerSettings.hudColorB);
		ColorBLabel.text = $"R {ColorBSlider.value:F0}";
		colorExample.color = new Color(ColorRSlider.value, ColorGSlider.value, ColorBSlider.value);
		OverlayTextSizeSlider.SetValueWithoutNotify(PlayerSettings.overlayTextSize);
		OverlayTextSizeLabel.text = $"{OverlayTextSizeSlider.value:F0}";
	}

	private void OnDestroy()
	{
		PlayerSettings.ApplyPrefs();
		if (SceneSingleton<HUDOptions>.i != null)
		{
			SceneSingleton<HUDOptions>.i.ApplyHUDSettings();
		}
	}

	public void ApplySettings()
	{
		PlayerPrefs.SetInt("LagPip", lagPipToggle.isOn ? 1 : 0);
		lagPipLabel.text = (lagPipToggle.isOn ? "Lag" : "Lead");
		PlayerPrefs.SetInt("RangeCircle", rangeCircleToggle.isOn ? 1 : 0);
		rangeCircleLabel.text = (rangeCircleToggle.isOn ? "Circle" : "Ladder");
		PlayerPrefs.SetInt("HUDTime", (int)HUDTimeSlider.value);
		PlayerPrefs.SetInt("Gauges", gaugesToggle.isOn ? 1 : 0);
		gaugesLabel.text = (gaugesToggle.isOn ? "Show" : "Hide");
		PlayerPrefs.SetInt("HUDWeapons", HUDWeaponsToggle.isOn ? 1 : 0);
		HUDWeaponsLabel.text = (HUDWeaponsToggle.isOn ? "Show" : "Hide");
		PlayerPrefs.SetFloat("HMDWidth", 10f * (float)Mathf.RoundToInt(HMDWidthSlider.value / 10f));
		HMDWidthLabel.text = $"{10f * (float)Mathf.RoundToInt(HMDWidthSlider.value / 10f):F0}px";
		PlayerPrefs.SetFloat("HMDHeight", 10f * (float)Mathf.RoundToInt(HMDHeightSlider.value / 10f));
		HMDHeightLabel.text = $"{10f * (float)Mathf.RoundToInt(HMDHeightSlider.value / 10f):F0}px";
		HMDSideDistSlider.value = Mathf.Clamp(HMDSideDistSlider.value, 100f, 5f * (float)Mathf.RoundToInt(HMDWidthSlider.value / 10f));
		HMDSideDistSlider.value = 5f * (float)Mathf.RoundToInt(HMDSideDistSlider.value / 5f);
		PlayerPrefs.SetFloat("HMDSideDist", HMDSideDistSlider.value);
		HMDSideDistLabel.text = $"{HMDSideDistSlider.value:F0}px";
		PlayerPrefs.SetFloat("HMDSideAngle", HMDSideAngleSlider.value);
		HMDSideAngleLabel.text = $"{HMDSideAngleSlider.value:F0}°";
		HMDTopHeightSlider.value = Mathf.Clamp(HMDTopHeightSlider.value, -5f * (float)Mathf.RoundToInt(HMDHeightSlider.value / 10f), 5f * (float)Mathf.RoundToInt(HMDHeightSlider.value / 10f));
		HMDTopHeightSlider.value = 5f * (float)Mathf.RoundToInt(HMDTopHeightSlider.value / 5f);
		PlayerPrefs.SetFloat("HMDTopHeight", HMDTopHeightSlider.value);
		HMDTopHeightLabel.text = $"{HMDTopHeightSlider.value:F0}px";
		HMDHideDistSlider.value = Mathf.Max(HMDHideDistSlider.value, 0.2f);
		PlayerPrefs.SetFloat("HMDHideDist", HMDHideDistSlider.value);
		HMDHideDistLabel.text = $"{100f * HMDHideDistSlider.value:F0}%";
		HMDIconSizeSlider.value = 5f * (float)Mathf.RoundToInt(HMDIconSizeSlider.value / 5f);
		PlayerPrefs.SetFloat("HMDIconSize", HMDIconSizeSlider.value);
		HMDIconSizeLabel.text = $"{HMDIconSizeSlider.value:F0}";
		HUDTextSizeSlider.value = 2f * (float)Mathf.RoundToInt(HUDTextSizeSlider.value / 2f);
		PlayerPrefs.SetFloat("HUDTextSize", HUDTextSizeSlider.value);
		HUDTextSizeLabel.text = $"{HUDTextSizeSlider.value:F0}";
		HMDTextSizeSlider.value = 2f * (float)Mathf.RoundToInt(HMDTextSizeSlider.value / 2f);
		PlayerPrefs.SetFloat("HMDTextSize", HMDTextSizeSlider.value);
		HMDTextSizeLabel.text = $"{HMDTextSizeSlider.value:F0}";
		OverlayTextSizeSlider.value = 2f * (float)Mathf.RoundToInt(OverlayTextSizeSlider.value / 2f);
		PlayerPrefs.SetFloat("OverlayTextSize", OverlayTextSizeSlider.value);
		OverlayTextSizeLabel.text = $"{OverlayTextSizeSlider.value:F0}";
		PlayerPrefs.SetInt("HUDColorR", 0);
		PlayerPrefs.SetInt("HUDColorG", 255);
		PlayerPrefs.SetInt("HUDColorB", 0);
		PlayerSettings.LoadPrefs();
	}
}
