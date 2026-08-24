using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class MissionSettingsTab : MonoBehaviour, IMissionTab
	{
		[SerializeField]
		private TMP_InputField summaryInput;

		[SerializeField]
		private Toggle allowEventContentToggle;

		[SerializeField]
		private TMP_Dropdown playerModeDropdown;

		[SerializeField]
		private Slider startingRankSlider;

		[SerializeField]
		private Slider rankMultiplierSlider;

		[SerializeField]
		private Slider successfulSortieBonusSlider;

		[SerializeField]
		private Slider nuclearEscalationThresholdSlider;

		[SerializeField]
		private Slider strategicEscalationThresholdSlider;

		[SerializeField]
		private Slider minRankTacticalSlider;

		[SerializeField]
		private Slider minRankStrategicSlider;

		[SerializeField]
		private Toggle respawnToggle;

		[SerializeField]
		private TextMeshProUGUI startingRankLabel;

		[SerializeField]
		private TextMeshProUGUI rankMultiplierLabel;

		[SerializeField]
		private TextMeshProUGUI successfulSortieBonusLabel;

		[SerializeField]
		private TextMeshProUGUI nuclearEscalationThresholdLabel;

		[SerializeField]
		private TextMeshProUGUI strategicEscalationThresholdLabel;

		[SerializeField]
		private TextMeshProUGUI minRankTacticalLabel;

		[SerializeField]
		private TextMeshProUGUI minRankStrategicLabel;

		[Header("Camera Start Position")]
		[SerializeField]
		private OverrideDataField cameraPositionOverrideField;

		[SerializeField]
		private Vector3DataField cameraPositionField;

		[SerializeField]
		private Vector3DataField cameraRotationField;

		[SerializeField]
		private Button setCameraFromCurrentButton;

		[SerializeField]
		private Button moveCameraToSavedButton;

		[Space]
		[SerializeField]
		private Slider wrecksMaxNumberSlider;

		[SerializeField]
		private Slider wrecksDecayTimeSlider;

		[SerializeField]
		private TextMeshProUGUI wrecksMaxNumberLabel;

		[SerializeField]
		private TextMeshProUGUI wrecksDecayTimeLabel;

		private MissionSettings missionSettings;

		private ValueWrapperOverride<PositionRotation> overrideWrapper;

		private ValueWrapperGlobalPosition cameraPositionWrapper;

		private ValueWrapperQuaternion cameraRotationWrapper;

		private void Awake()
		{
			playerModeDropdown.ClearOptions();
			playerModeDropdown.AddOptions(EnumNames<PlayerMode>.GetNames());
			playerModeDropdown.onValueChanged.AddListener(ApplySettings);
			allowEventContentToggle.onValueChanged.AddListener(ApplySettings);
			summaryInput.onEndEdit.AddListener(ApplySettings);
			startingRankSlider.onValueChanged.AddListener(ApplySettings);
			rankMultiplierSlider.onValueChanged.AddListener(ApplySettings);
			successfulSortieBonusSlider.onValueChanged.AddListener(ApplySettings);
			nuclearEscalationThresholdSlider.onValueChanged.AddListener(ApplySettings);
			strategicEscalationThresholdSlider.onValueChanged.AddListener(ApplySettings);
			respawnToggle.onValueChanged.AddListener(ApplySettings);
			wrecksMaxNumberSlider.onValueChanged.AddListener(ApplySettings);
			wrecksDecayTimeSlider.onValueChanged.AddListener(ApplySettings);
			SetupCameraPositionFields();
		}

		public void SetMission(Mission mission)
		{
			missionSettings = mission.missionSettings;
			summaryInput.SetTextWithoutNotify(missionSettings.description);
			allowEventContentToggle.SetIsOnWithoutNotify(missionSettings.allowEventContent);
			playerModeDropdown.SetValueWithoutNotify((int)missionSettings.playerMode);
			startingRankSlider.SetValueWithoutNotify(missionSettings.playerStartingRank);
			rankMultiplierSlider.SetValueWithoutNotify(missionSettings.rankMultiplier);
			respawnToggle.SetIsOnWithoutNotify(missionSettings.allowRespawn);
			successfulSortieBonusSlider.SetValueWithoutNotify(missionSettings.successfulSortieBonus);
			nuclearEscalationThresholdSlider.SetValueWithoutNotify(missionSettings.nuclearEscalationThreshold);
			strategicEscalationThresholdSlider.SetValueWithoutNotify(missionSettings.strategicEscalationThreshold);
			minRankTacticalSlider.SetValueWithoutNotify(missionSettings.minRankTacticalWarhead);
			minRankStrategicSlider.SetValueWithoutNotify(missionSettings.minRankStrategicWarhead);
			wrecksMaxNumberSlider.SetValueWithoutNotify((float)missionSettings.wrecksMaxNumber * 0.1f);
			wrecksDecayTimeSlider.SetValueWithoutNotify(missionSettings.wrecksDecayTime);
			if (overrideWrapper != null)
			{
				overrideWrapper.SetValue(missionSettings.cameraStartPosition, this);
				cameraPositionWrapper.SetValue(missionSettings.cameraStartPosition.Value.Position, this);
				cameraRotationWrapper.SetValue(missionSettings.cameraStartPosition.Value.Rotation, this);
			}
			UpdateLabels();
		}

		private void SetupCameraPositionFields()
		{
			if (GameManager.gameState == GameState.Editor)
			{
				cameraPositionOverrideField.gameObject.SetActive(value: true);
				setCameraFromCurrentButton.onClick.AddListener(SetCameraPositionFromCurrent);
				moveCameraToSavedButton.onClick.AddListener(MoveCameraToSaved);
				overrideWrapper = ValueWrapper.FromCallback<ValueWrapperOverride<PositionRotation>, Override<PositionRotation>>(this, default(Override<PositionRotation>), delegate(Override<PositionRotation> newValue)
				{
					missionSettings.cameraStartPosition = newValue;
				});
				cameraPositionWrapper = ValueWrapper.FromCallback<ValueWrapperGlobalPosition, GlobalPosition>(this, default(GlobalPosition), delegate(GlobalPosition newValue)
				{
					missionSettings.cameraStartPosition.Value.Position = newValue;
				});
				cameraRotationWrapper = ValueWrapper.FromCallback<ValueWrapperQuaternion, Quaternion>(this, default(Quaternion), delegate(Quaternion newValue)
				{
					missionSettings.cameraStartPosition.Value.Rotation = newValue;
				});
				cameraPositionOverrideField.Setup(overrideWrapper, cameraPositionField, cameraRotationField, moveCameraToSavedButton, setCameraFromCurrentButton);
				cameraPositionField.Setup("Position", cameraPositionWrapper);
				cameraRotationField.Setup("Rotation", cameraRotationWrapper);
			}
			else
			{
				cameraPositionOverrideField.gameObject.SetActive(value: false);
			}
		}

		private void ApplySettings(string arg0)
		{
			ApplySettings();
		}

		private void ApplySettings(int arg0)
		{
			ApplySettings();
		}

		private void ApplySettings(float arg0)
		{
			ApplySettings();
		}

		private void ApplySettings(bool arg0)
		{
			ApplySettings();
		}

		public void ApplySettings()
		{
			missionSettings.description = summaryInput.text;
			missionSettings.allowEventContent = allowEventContentToggle.isOn;
			missionSettings.playerMode = (PlayerMode)playerModeDropdown.value;
			missionSettings.playerStartingRank = (int)startingRankSlider.value;
			missionSettings.rankMultiplier = rankMultiplierSlider.value;
			missionSettings.allowRespawn = respawnToggle.isOn;
			missionSettings.successfulSortieBonus = successfulSortieBonusSlider.value;
			if (strategicEscalationThresholdSlider.value < nuclearEscalationThresholdSlider.value)
			{
				strategicEscalationThresholdSlider.SetValueWithoutNotify(nuclearEscalationThresholdSlider.value);
			}
			missionSettings.nuclearEscalationThreshold = nuclearEscalationThresholdSlider.value;
			missionSettings.strategicEscalationThreshold = strategicEscalationThresholdSlider.value;
			if (minRankStrategicSlider.value < minRankTacticalSlider.value)
			{
				minRankStrategicSlider.SetValueWithoutNotify(minRankTacticalSlider.value);
			}
			missionSettings.minRankTacticalWarhead = (int)minRankTacticalSlider.value;
			missionSettings.minRankStrategicWarhead = (int)minRankStrategicSlider.value;
			missionSettings.wrecksMaxNumber = (int)wrecksMaxNumberSlider.value * 10;
			missionSettings.wrecksDecayTime = wrecksDecayTimeSlider.value;
			UpdateLabels();
		}

		private void UpdateLabels()
		{
			startingRankLabel.text = startingRankSlider.value.ToString("F0");
			rankMultiplierLabel.text = (rankMultiplierSlider.value * 100f).ToString("F0") + "%";
			successfulSortieBonusLabel.text = (successfulSortieBonusSlider.value * 100f).ToString("F0") + "%";
			nuclearEscalationThresholdLabel.text = $"{nuclearEscalationThresholdSlider.value}";
			strategicEscalationThresholdLabel.text = $"{strategicEscalationThresholdSlider.value}";
			minRankTacticalLabel.text = $"{minRankTacticalSlider.value}";
			minRankStrategicLabel.text = $"{minRankStrategicSlider.value}";
			wrecksMaxNumberLabel.text = ((wrecksMaxNumberSlider.value > 0f) ? $"{10f * wrecksMaxNumberSlider.value}" : "No despawn");
			wrecksDecayTimeLabel.text = ((wrecksDecayTimeSlider.value > 0f) ? $"{wrecksDecayTimeSlider.value} minutes" : "No decay");
		}

		private void SetCameraPositionFromCurrent()
		{
			SceneSingleton<CameraStateManager>.i.GetCameraPosition(out var positionRotation);
			cameraPositionWrapper.SetValue(positionRotation.Position, null);
			cameraRotationWrapper.SetValue(positionRotation.Rotation, null);
		}

		private void MoveCameraToSaved()
		{
			SceneSingleton<CameraStateManager>.i.SetCameraPosition(overrideWrapper.Value.Value);
		}
	}
}
