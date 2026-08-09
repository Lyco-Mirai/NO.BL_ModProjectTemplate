using UnityEngine;
using UnityEngine.UI;

public class GameplayMenu : MonoBehaviour
{
	[SerializeField]
	private Dropdown unitSystemDropdown;

	[SerializeField]
	private Slider cockpitCameraInertiaSlider;

	[SerializeField]
	private Slider FoVSlider;

	[SerializeField]
	private Slider externalFoVSlider;

	[SerializeField]
	private Slider LandingCamSlider;

	[SerializeField]
	private Toggle zoomOnBoresightToggle;

	[SerializeField]
	private Text cockpitCameraInertiaLabel;

	[SerializeField]
	private Text FoVSliderLabel;

	[SerializeField]
	private Text externalFoVSliderLabel;

	[SerializeField]
	private Slider radialMenuControl;

	[SerializeField]
	private Toggle padlockToggle;

	[SerializeField]
	private Toggle tacScreenIRToggle;

	[SerializeField]
	private Toggle cameraAutoNVGToggle;

	[SerializeField]
	private Slider killFeedMunitionSlider;

	[SerializeField]
	private Slider killFeedAircraftSlider;

	[SerializeField]
	private Slider killFeedVehicleSlider;

	[SerializeField]
	private Slider killFeedBuildingSlider;

	[SerializeField]
	private Slider killFeedShipSlider;

	[SerializeField]
	private Slider killFeedMinValueSlider;

	[SerializeField]
	private Slider killFeedNbLinesSlider;

	[SerializeField]
	private Text killFeedMinValueText;

	[SerializeField]
	private Text killFeedNbLinesText;

	[SerializeField]
	private Toggle hitMarkersToggle;

	[SerializeField]
	private Toggle aircraftDeployedMessageToggle;

	private void Start()
	{
		UpdateLabels();
	}

	private void UpdateLabels()
	{
		unitSystemDropdown.SetValueWithoutNotify((int)PlayerSettings.unitSystem);
		cockpitCameraInertiaSlider.SetValueWithoutNotify(PlayerSettings.cockpitCamInertia);
		FoVSlider.SetValueWithoutNotify(PlayerSettings.defaultFoV);
		externalFoVSlider.SetValueWithoutNotify(PlayerSettings.defaultExternalFoV);
		zoomOnBoresightToggle.SetIsOnWithoutNotify(PlayerSettings.zoomOnBoresight);
		padlockToggle.SetIsOnWithoutNotify(PlayerSettings.padLockTarget);
		tacScreenIRToggle.SetIsOnWithoutNotify(PlayerSettings.tacScreenIR);
		cameraAutoNVGToggle.SetIsOnWithoutNotify(PlayerSettings.cameraAutoNVG);
		cockpitCameraInertiaLabel.text = $"{cockpitCameraInertiaSlider.value * 100f:F0}%";
		FoVSliderLabel.text = $"{FoVSlider.value:F0}°";
		externalFoVSliderLabel.text = $"{externalFoVSlider.value:F0}°";
		LandingCamSlider.SetValueWithoutNotify(PlayerSettings.landingCam);
		radialMenuControl.SetValueWithoutNotify(PlayerSettings.radialControl);
		killFeedNbLinesSlider.SetValueWithoutNotify(PlayerSettings.killFeedNbLines);
		killFeedMinValueSlider.SetValueWithoutNotify((int)PlayerSettings.killFeedMinValue);
		killFeedMunitionSlider.SetValueWithoutNotify((float)PlayerSettings.killFeedMunition);
		killFeedAircraftSlider.SetValueWithoutNotify((float)PlayerSettings.killFeedAircraft);
		killFeedShipSlider.SetValueWithoutNotify((float)PlayerSettings.killFeedShip);
		killFeedBuildingSlider.SetValueWithoutNotify((float)PlayerSettings.killFeedBuilding);
		killFeedVehicleSlider.SetValueWithoutNotify((float)PlayerSettings.killFeedVehicle);
		killFeedMinValueText.text = UnitConverter.ValueReading(killFeedMinValueSlider.value);
		killFeedNbLinesText.text = $"{5f * killFeedNbLinesSlider.value}";
		hitMarkersToggle.SetIsOnWithoutNotify(PlayerSettings.showHitMarkers);
		aircraftDeployedMessageToggle.SetIsOnWithoutNotify(PlayerSettings.showAicraftDeployedMessage);
	}

	private void OnDestroy()
	{
		PlayerSettings.LoadPrefs();
	}

	public void ApplySettings()
	{
		PlayerPrefs.SetInt("UnitSystem", unitSystemDropdown.value);
		PlayerPrefs.SetFloat("CockpitCamInertia", cockpitCameraInertiaSlider.value);
		cockpitCameraInertiaLabel.text = $"{cockpitCameraInertiaSlider.value * 100f:F0}%";
		PlayerPrefs.SetFloat("DefaultFoV", FoVSlider.value);
		FoVSliderLabel.text = $"{FoVSlider.value:F0}°";
		PlayerPrefs.SetFloat("DefaultExternalFoV", externalFoVSlider.value);
		externalFoVSliderLabel.text = $"{externalFoVSlider.value:F0}°";
		PlayerPrefs.SetInt("ZoomOnBoresight", zoomOnBoresightToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("PadLockTarget", padlockToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("TacScreenIR", tacScreenIRToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("CameraAutoNVG", cameraAutoNVGToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("LandingCam", (int)LandingCamSlider.value);
		PlayerPrefs.SetInt("RadialControl", (int)radialMenuControl.value);
		PlayerPrefs.SetInt("KillFeedMunition", (int)killFeedMunitionSlider.value);
		PlayerPrefs.SetInt("KillFeedAircraft", (int)killFeedAircraftSlider.value);
		PlayerPrefs.SetInt("KillFeedBuilding", (int)killFeedBuildingSlider.value);
		PlayerPrefs.SetInt("KillFeedShip", (int)killFeedShipSlider.value);
		PlayerPrefs.SetInt("KillFeedVehicle", (int)killFeedVehicleSlider.value);
		PlayerPrefs.SetFloat("KillFeedMinValue", killFeedMinValueSlider.value);
		killFeedMinValueText.text = UnitConverter.ValueReading(killFeedMinValueSlider.value);
		PlayerPrefs.SetInt("KillFeedNbLines", (int)killFeedNbLinesSlider.value);
		killFeedNbLinesText.text = $"{5f * killFeedNbLinesSlider.value}";
		PlayerPrefs.SetInt("ShowHitMarkers", hitMarkersToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ShowAircraftDeployedMessage", aircraftDeployedMessageToggle.isOn ? 1 : 0);
		if (SceneSingleton<CameraStateManager>.i != null)
		{
			CameraStateManager i = SceneSingleton<CameraStateManager>.i;
			float fOVTarget = ((i.currentState == i.cockpitState) ? FoVSlider.value : externalFoVSlider.value);
			i.SetDesiredFoV(fOVTarget, 0f);
		}
		PlayerSettings.ApplyPrefs();
	}
}
