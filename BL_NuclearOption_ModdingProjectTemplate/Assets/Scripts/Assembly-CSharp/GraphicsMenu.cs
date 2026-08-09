using System.Collections.Generic;
using NuclearOption.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsMenu : MonoBehaviour
{
	[Header("Display Settings")]
	[SerializeField]
	private TMP_Dropdown resolutionDropdown;

	[SerializeField]
	private Slider fpsLimitSlider;

	[SerializeField]
	private TextMeshProUGUI fpsLimitSliderText;

	[SerializeField]
	private Toggle windowedToggle;

	[SerializeField]
	private Toggle vsyncToggle;

	[SerializeField]
	private Toggle cinematicModeToggle;

	[Header("Quality Settings")]
	[SerializeField]
	private TMP_Dropdown aaDropdown;

	[SerializeField]
	private TMP_Dropdown mipmapLevelDropdown;

	[SerializeField]
	private TMP_Dropdown anisotropicDropdown;

	[SerializeField]
	private Slider lodBiasSlider;

	[SerializeField]
	private TextMeshProUGUI lodBiasSliderText;

	[SerializeField]
	private TMP_Dropdown lightsDropdown;

	[SerializeField]
	private Toggle debugVisToggle;

	[Header("Environment & Lighting")]
	[SerializeField]
	private TMP_Dropdown shadowDetailDropdown;

	[SerializeField]
	private Slider shadowDistanceSlider;

	[SerializeField]
	private TextMeshProUGUI shadowDistanceSliderText;

	[SerializeField]
	private Toggle softShaderToggle;

	[SerializeField]
	private Slider cloudsDetailSlider;

	[SerializeField]
	private TextMeshProUGUI cloudsDetailSliderText;

	[SerializeField]
	private Toggle grassToggle;

	[SerializeField]
	private Slider treeDistanceSlider;

	[SerializeField]
	private TextMeshProUGUI treeDistanceSliderText;

	[Header("Actions")]
	[SerializeField]
	private Button resetButton;

	private GraphicsHelper settings => PlayerSettings.graphics;

	private ScreenResolutionHelper screenResolution => PlayerSettings.screenResolutionHelper;

	private void Start()
	{
		mipmapLevelDropdown.ClearOptions();
		mipmapLevelDropdown.AddOptions(GraphicsHelper.MipmapLimitOptions);
		shadowDetailDropdown.ClearOptions();
		shadowDetailDropdown.AddOptions(GraphicsHelper.ShadowQualityOptions);
		aaDropdown.ClearOptions();
		aaDropdown.AddOptions(GraphicsHelper.AAOptions);
		anisotropicDropdown.ClearOptions();
		anisotropicDropdown.AddOptions(GraphicsHelper.AnisotropicOptions);
		lightsDropdown.ClearOptions();
		lightsDropdown.AddOptions(GraphicsHelper.MaxLightsOptions);
		List<string> optionStrings = screenResolution.OptionStrings;
		resolutionDropdown.ClearOptions();
		resolutionDropdown.AddOptions(optionStrings);
		fpsLimitSlider.maxValue = Mathf.RoundToInt((float)screenResolution.Current.refreshRateRatio.value / 5f) + 1;
		RefreshUI();
		mipmapLevelDropdown.onValueChanged.AddListener(OnMipmapLevelChanged);
		shadowDetailDropdown.onValueChanged.AddListener(OnShadowDetailChanged);
		shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceSliderChanged);
		softShaderToggle.onValueChanged.AddListener(OnSoftShaderToggleChanged);
		aaDropdown.onValueChanged.AddListener(OnAADropdownChanged);
		lodBiasSlider.onValueChanged.AddListener(OnLodBiasSliderChanged);
		lightsDropdown.onValueChanged.AddListener(OnLightsDropdownChanged);
		anisotropicDropdown.onValueChanged.AddListener(OnAnisotropicDropdownChanged);
		resolutionDropdown.onValueChanged.AddListener(ResolutionDropdownChanged);
		fpsLimitSlider.onValueChanged.AddListener(FPSLimitChanged);
		windowedToggle.onValueChanged.AddListener(OnWindowedChanged);
		vsyncToggle.onValueChanged.AddListener(OnVsyncChanged);
		cloudsDetailSlider.onValueChanged.AddListener(OnCloudDetailChanged);
		cinematicModeToggle.onValueChanged.AddListener(OnCinematicChanged);
		debugVisToggle.onValueChanged.AddListener(OnDebugVisChanged);
		resetButton.onClick.AddListener(ResetGraphicsSettings);
		grassToggle.onValueChanged.AddListener(OnGrassToggleChanged);
		treeDistanceSlider.onValueChanged.AddListener(OnTreeDistanceSliderChanged);
	}

	private void RefreshUI()
	{
		mipmapLevelDropdown.SetValueWithoutNotify(settings.MipmapLevel);
		shadowDetailDropdown.SetValueWithoutNotify(settings.ShadowQuality);
		shadowDistanceSlider.minValue = 500f;
		shadowDistanceSlider.maxValue = 10000f;
		shadowDistanceSlider.wholeNumbers = true;
		shadowDistanceSlider.SetValueWithoutNotify(settings.ShadowDistance);
		shadowDistanceSliderText.text = $"{settings.ShadowDistance:0}m";
		softShaderToggle.SetIsOnWithoutNotify(settings.SoftShadows);
		aaDropdown.SetValueWithoutNotify(settings.AntiAliasing);
		lodBiasSlider.minValue = 1f;
		lodBiasSlider.maxValue = 4f;
		lodBiasSlider.wholeNumbers = false;
		lodBiasSlider.SetValueWithoutNotify(settings.LodBias);
		lodBiasSliderText.text = $"{settings.LodBias:0.0}";
		anisotropicDropdown.SetValueWithoutNotify(settings.AnisotropicFiltering);
		lightsDropdown.SetValueWithoutNotify(settings.MaxLights);
		int valueWithoutNotify = screenResolution.OptionStrings.IndexOf(screenResolution.CurrentString);
		resolutionDropdown.SetValueWithoutNotify(valueWithoutNotify);
		fpsLimitSlider.SetValueWithoutNotify((settings.FpsLimit > -1) ? ((float)settings.FpsLimit / 5f) : fpsLimitSlider.maxValue);
		fpsLimitSliderText.text = ((settings.FpsLimit > -1) ? $"{settings.FpsLimit} FPS" : "Unlimited");
		windowedToggle.SetIsOnWithoutNotify(!screenResolution.FullScreen);
		vsyncToggle.SetIsOnWithoutNotify(settings.Vsync);
		cloudsDetailSlider.minValue = 0f;
		cloudsDetailSlider.maxValue = 1f;
		cloudsDetailSlider.wholeNumbers = false;
		cloudsDetailSlider.SetValueWithoutNotify(settings.CloudDetail);
		cloudsDetailSliderText.text = $"{settings.CloudDetail * 100f:F0}%";
		cinematicModeToggle.SetIsOnWithoutNotify(PlayerSettings.cinematicMode);
		debugVisToggle.SetIsOnWithoutNotify(PlayerSettings.debugVis);
		grassToggle.SetIsOnWithoutNotify(PlayerSettings.DetailSettings.GrassEnabled);
		treeDistanceSlider.minValue = DetailSettings.TreeRangeMultiplierMin;
		treeDistanceSlider.maxValue = DetailSettings.TreeRangeMultiplierMax;
		treeDistanceSlider.SetValueWithoutNotify(PlayerSettings.DetailSettings.TreeRangeMultiplier);
		treeDistanceSliderText.text = $"{PlayerSettings.DetailSettings.TreeRangeMultiplier:0.0}";
	}

	private void ResetGraphicsSettings()
	{
		PlayerSettings.graphics.Clear();
		PlayerSettings.DetailSettings.Clear();
		RefreshUI();
	}

	private void OnMipmapLevelChanged(int value)
	{
		settings.MipmapLevel = value;
	}

	private void OnShadowDetailChanged(int value)
	{
		settings.ShadowQuality = value;
	}

	private void OnShadowDistanceSliderChanged(float rawValue)
	{
		int value = (int)(Mathf.Round(rawValue / 500f) * 500f);
		value = Mathf.Clamp(value, 500, 10000);
		shadowDistanceSlider.SetValueWithoutNotify(value);
		shadowDistanceSliderText.text = $"{value:0} m";
		settings.ShadowDistance = value;
	}

	private void OnSoftShaderToggleChanged(bool value)
	{
		settings.SoftShadows = value;
	}

	private void OnAADropdownChanged(int value)
	{
		settings.AntiAliasing = value;
	}

	private void OnAnisotropicDropdownChanged(int value)
	{
		settings.AnisotropicFiltering = value;
	}

	private void OnVsyncChanged(bool isOn)
	{
		settings.Vsync = isOn;
		if (isOn)
		{
			fpsLimitSlider.value = fpsLimitSlider.maxValue;
		}
	}

	private void OnLodBiasSliderChanged(float value)
	{
		lodBiasSliderText.text = $"{value:0.0}";
		settings.LodBias = value;
	}

	private void OnLightsDropdownChanged(int value)
	{
		settings.MaxLights = value;
	}

	private void OnCloudDetailChanged(float value)
	{
		cloudsDetailSliderText.text = $"{value * 100f:F0}%";
		settings.CloudDetail = value;
	}

	private void ResolutionDropdownChanged(int index)
	{
		string resolutionStr = screenResolution.OptionStrings[index];
		screenResolution.SetValues(resolutionStr, screenResolution.FullScreen);
	}

	private void FPSLimitChanged(float value)
	{
		int num = (int)value * 5;
		int num2 = (int)fpsLimitSlider.maxValue * 5;
		settings.FpsLimit = ((num >= num2) ? (-1) : num);
		fpsLimitSliderText.text = ((num < num2) ? $"{num} FPS" : "Unlimited");
		if (num < num2)
		{
			vsyncToggle.isOn = false;
		}
		GraphicsHelper.SetFPSLimit(num);
	}

	private void OnWindowedChanged(bool isOn)
	{
		screenResolution.SetValues(screenResolution.CurrentString, !isOn);
	}

	private void OnCinematicChanged(bool isOn)
	{
		PlayerSettings.cinematicMode = isOn;
		PlayerPrefs.SetInt("CinematicMode", isOn ? 1 : 0);
	}

	private void OnDebugVisChanged(bool isOn)
	{
		PlayerSettings.debugVis = isOn;
		PlayerPrefs.SetInt("DebugVis", isOn ? 1 : 0);
	}

	private void OnGrassToggleChanged(bool isOn)
	{
		PlayerSettings.DetailSettings.GrassEnabled = isOn;
	}

	private void OnTreeDistanceSliderChanged(float value)
	{
		treeDistanceSliderText.text = $"{value:0.0}";
		PlayerSettings.DetailSettings.TreeRangeMultiplier = value;
	}
}
