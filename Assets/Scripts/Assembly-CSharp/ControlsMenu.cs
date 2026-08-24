using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlsMenu : MonoBehaviour
{
	public enum BindType
	{
		Keyboard = 0,
		Button = 1,
		Axis = 2
	}

	private GameObject parentMenu;

	public static BindType bindType;

	[SerializeField]
	private Toggle useVirtualJoystick;

	[SerializeField]
	private Toggle invertPitch;

	[SerializeField]
	private Toggle viewInvertPitch;

	[SerializeField]
	private Toggle throttleUseNegative;

	[SerializeField]
	private Toggle throttleUseRelative;

	[SerializeField]
	private Toggle controllerMenuNavigation;

	[SerializeField]
	private Toggle menuWeaponSafety;

	[SerializeField]
	private Toggle invertCollective;

	[SerializeField]
	private Slider sensitivitySlider;

	[SerializeField]
	private Slider centeringSlider;

	[SerializeField]
	private Text sensitivityLabel;

	[SerializeField]
	private Text centeringLabel;

	[SerializeField]
	private Slider viewSensitivitySlider;

	[SerializeField]
	private Text viewSensitivityLabel;

	[SerializeField]
	private Slider viewSmoothingSlider;

	[SerializeField]
	private Text viewSmoothingLabel;

	[SerializeField]
	public Toggle useHeadTracker;

	[SerializeField]
	private TMP_Dropdown selectedHeadTracker;

	[SerializeField]
	private Button tobiiEyeTrackerSettingsButton;

	[SerializeField]
	private Slider clickDelaySlider;

	[SerializeField]
	private Slider pressDelaySlider;

	[SerializeField]
	private Text clickDelayLabel;

	[SerializeField]
	private Text pressDelayLabel;

	private Button selectedTab;

	private void OnEnable()
	{
	}

	public void EditBindings()
	{
		if (SceneSingleton<GameplayUI>.i != null)
		{
			SceneSingleton<GameplayUI>.i.menuCanvas.enabled = false;
		}
		GameManager.controlMapper.Open();
	}

	public void EditTobiiSettings()
	{
		if (SceneSingleton<GameplayUI>.i != null)
		{
			SceneSingleton<GameplayUI>.i.menuCanvas.enabled = false;
		}
		GameManager.tobiiSettingsUI.Open();
	}

	private void Start()
	{
		PlayerSettings.LoadPrefs();
		InitializeStates();
		if (useHeadTracker != null)
		{
			useHeadTracker.onValueChanged.AddListener(OnHeadTrackerSettingsChanged);
		}
		if (selectedHeadTracker != null)
		{
			selectedHeadTracker.onValueChanged.AddListener(OnHeadTrackerDropdownChanged);
		}
	}

	private void OnHeadTrackerSettingsChanged(bool isOn)
	{
		UpdateTobiiButtonVisibility();
	}

	private void OnHeadTrackerDropdownChanged(int value)
	{
		UpdateTobiiButtonVisibility();
	}

	private void UpdateTobiiButtonVisibility()
	{
	}

	public void SavePlayerSettings()
	{
		PlayerPrefs.SetInt("VirtualJoystickEnabled", useVirtualJoystick.isOn ? 1 : 0);
		PlayerPrefs.SetInt("VirtualJoystickInvertPitch", invertPitch.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ViewInvertPitch", viewInvertPitch.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ThrottleUseNegative", throttleUseNegative.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ThrottleUseRelative", throttleUseRelative.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ControllerMenuNavigation", controllerMenuNavigation.isOn ? 1 : 0);
		PlayerPrefs.SetInt("MenuWeaponSafety", menuWeaponSafety.isOn ? 1 : 0);
		PlayerPrefs.SetInt("InvertCollective", invertCollective.isOn ? 1 : 0);
		PlayerPrefs.SetFloat("VirtualJoystickSensitivity", sensitivitySlider.value);
		PlayerPrefs.SetFloat("VirtualJoystickCentering", centeringSlider.value);
		PlayerPrefs.SetFloat("ViewSensitivity", viewSensitivitySlider.value);
		PlayerPrefs.SetFloat("ViewSmoothing", viewSmoothingSlider.value);
		PlayerPrefs.SetFloat("PressDelay", pressDelaySlider.value);
		PlayerPrefs.SetFloat("ClickDelay", clickDelaySlider.value);
		PlayerPrefs.SetInt("UseTrackIR", useHeadTracker.isOn ? 1 : 0);
		PlayerPrefs.SetInt("HeadTrackerType", selectedHeadTracker.value);
		PlayerSettings.LoadPrefs();
		PlayerSettings.ApplyPrefs();
	}

	public void ResetSlidersToggles()
	{
		PlayerPrefs.SetInt("VirtualJoystickEnabled", 0);
		PlayerPrefs.SetInt("VirtualJoystickInvertPitch", 0);
		PlayerPrefs.SetInt("ViewInvertPitch", 0);
		PlayerPrefs.SetInt("ThrottleUseNegative", 1);
		PlayerPrefs.SetInt("ThrottleUseRelative", 0);
		PlayerPrefs.SetInt("ControllerMenuNavigation", 1);
		PlayerPrefs.SetInt("MenuWeaponSafety", 1);
		PlayerPrefs.SetInt("InvertCollective", 0);
		PlayerPrefs.SetFloat("VirtualJoystickSensitivity", 0.25f);
		PlayerPrefs.SetFloat("VirtualJoystickCentering", 0f);
		PlayerPrefs.SetFloat("ViewSensitivity", 0.5f);
		PlayerPrefs.SetFloat("ViewSmoothing", 0.5f);
		PlayerPrefs.SetFloat("PressDelay", 0.2f);
		PlayerPrefs.SetFloat("ClickDelay", 0.1f);
		PlayerSettings.LoadPrefs();
		InitializeStates();
	}

	private void InitializeStates()
	{
		useVirtualJoystick.SetIsOnWithoutNotify(PlayerSettings.virtualJoystickEnabled);
		invertPitch.SetIsOnWithoutNotify(PlayerSettings.virtualJoystickInvertPitch);
		viewInvertPitch.SetIsOnWithoutNotify(PlayerSettings.viewInvertPitch);
		throttleUseNegative.SetIsOnWithoutNotify(PlayerSettings.throttleUseNegative);
		throttleUseRelative.SetIsOnWithoutNotify(PlayerSettings.throttleUseRelative);
		controllerMenuNavigation.SetIsOnWithoutNotify(PlayerSettings.controllerMenuNavigation);
		menuWeaponSafety.SetIsOnWithoutNotify(PlayerSettings.menuWeaponSafety);
		invertCollective.SetIsOnWithoutNotify(PlayerSettings.invertCollective);
		sensitivitySlider.SetValueWithoutNotify(PlayerSettings.virtualJoystickSensitivity);
		centeringSlider.SetValueWithoutNotify(PlayerSettings.virtualJoystickCentering);
		viewSensitivitySlider.SetValueWithoutNotify(PlayerSettings.viewSensitivity);
		viewSmoothingSlider.SetValueWithoutNotify(PlayerSettings.viewSmoothing);
		pressDelaySlider.SetValueWithoutNotify(PlayerSettings.pressDelay);
		clickDelaySlider.SetValueWithoutNotify(PlayerSettings.clickDelay);
		useHeadTracker.SetIsOnWithoutNotify(PlayerSettings.useTrackIR);
		selectedHeadTracker.SetValueWithoutNotify((int)PlayerSettings.headTrackerType);
		UpdateLabels();
	}

	public void UpdateLabels()
	{
		sensitivityLabel.text = "Virtual Joystick Sensitivity (" + sensitivitySlider.value.ToString("F1") + ")";
		centeringLabel.text = "Virtual Joystick Centering Force (" + centeringSlider.value.ToString("F1") + ")";
		viewSensitivityLabel.text = "View Motion Sensitivity (" + viewSensitivitySlider.value.ToString("F1") + ")";
		viewSmoothingLabel.text = "View Smoothing (" + viewSmoothingSlider.value.ToString("F1") + ")";
		clickDelayLabel.text = "Button Click Delay (" + clickDelaySlider.value.ToString("F2") + ")";
		pressDelayLabel.text = "Button Hold Delay (" + pressDelaySlider.value.ToString("F2") + ")";
	}

	public void OnDestroy()
	{
		PlayerSettings.LoadPrefs();
	}

	public void CloseControlsMenu()
	{
		parentMenu.SetActive(value: true);
		Object.Destroy(base.gameObject);
		SceneSingleton<GameplayUI>.i.gameplayCanvas.enabled = true;
	}

	private void Update()
	{
	}
}
