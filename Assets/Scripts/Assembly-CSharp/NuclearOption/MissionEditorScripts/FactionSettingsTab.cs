using System.Collections.Generic;
using NuclearOption.MissionEditorScripts.MultiSelect;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class FactionSettingsTab : MonoBehaviour, IMissionTab
	{
		[SerializeField]
		private TMP_Dropdown factionSelect;

		[SerializeField]
		private TMP_Dropdown unitSelect;

		[SerializeField]
		private Button addUnitButton;

		[SerializeField]
		private Slider factionStartingBalanceSlider;

		[SerializeField]
		private Slider factionRegularIncomeSlider;

		[SerializeField]
		private Slider playerJoinAllowanceSlider;

		[SerializeField]
		private Slider killRewardSlider;

		[SerializeField]
		private Slider playerTaxRateSlider;

		[SerializeField]
		private Slider excessFundsDistributeSlider;

		[SerializeField]
		private Slider airSkillMultiplerSlider;

		[SerializeField]
		private Slider surfaceSkillMultiplerSlider;

		[SerializeField]
		private TextMeshProUGUI factionStartingBalanceLabel;

		[SerializeField]
		private TextMeshProUGUI factionRegularIncomeLabel;

		[SerializeField]
		private TextMeshProUGUI playerJoinAllowanceLabel;

		[SerializeField]
		private TextMeshProUGUI killRewardLabel;

		[SerializeField]
		private TextMeshProUGUI playerTaxRateLabel;

		[SerializeField]
		private TextMeshProUGUI excessFundsDistributeLabel;

		[SerializeField]
		private TextMeshProUGUI airSkillMultiplerLabel;

		[SerializeField]
		private TextMeshProUGUI surfaceSkillMultiplerLabel;

		[SerializeField]
		private Slider reserveAirframesSlider;

		[SerializeField]
		private Slider extraReservesPerPlayerSlider;

		[SerializeField]
		private Slider AIAircraftLimitSlider;

		[SerializeField]
		private Slider reduceAIPerFriendlyPlayerSlider;

		[SerializeField]
		private Slider addAIPerEnemyPlayerSlider;

		[SerializeField]
		private TextMeshProUGUI reserveAirframesLabel;

		[SerializeField]
		private TextMeshProUGUI extraReservesPerPlayerLabel;

		[SerializeField]
		private TextMeshProUGUI AIAircraftLimitLabel;

		[SerializeField]
		private TextMeshProUGUI reduceAIPerFriendlyPlayerLabel;

		[SerializeField]
		private TextMeshProUGUI addAIPerEnemyPlayerLabel;

		[SerializeField]
		private Slider startingWarheadsSlider;

		[SerializeField]
		private Slider reserveWarheadsSlider;

		[SerializeField]
		private TextMeshProUGUI startingWarheadsLabel;

		[SerializeField]
		private TextMeshProUGUI reserveWarheadsLabel;

		[SerializeField]
		private Toggle preventJoinToggle;

		[SerializeField]
		private GameObject preventJoinToggleDifferentValue;

		[SerializeField]
		private Transform inventoryPanel;

		[SerializeField]
		private GameObject supplyItemPrefab;

		[SerializeField]
		private GameObject supplyNotSameOverlay;

		private MultiSelect<MissionFaction> selectedFactions = new MultiSelect<MissionFaction>();

		private List<InventoryItem> inventoryItems = new List<InventoryItem>();

		private Mission mission;

		private const string ALL_FACTIONS = "Both";

		[SerializeField]
		private Toggle preventDonationToggle;

		[SerializeField]
		private GameObject preventDonationToggleDifferentValue;

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

		[SerializeField]
		private GameObject cameraPositionDifferentOverlay;

		private ValueWrapperOverride<PositionRotation> cameraOverrideWrapper;

		private ValueWrapperGlobalPosition cameraPositionWrapper;

		private ValueWrapperQuaternion cameraRotationWrapper;

		private DropdownKeyHelper<UnitDefinition> unitSupplyHelper;

		private void Start()
		{
			unitSupplyHelper = new DropdownKeyHelper<UnitDefinition>(unitSelect);
			factionSelect.onValueChanged.AddListener(delegate
			{
				SelectFaction();
			});
			selectedFactions.SetupSlider(factionStartingBalanceSlider, factionStartingBalanceLabel, (MissionFaction f) => ref f.startingBalance, (float v) => Mathf.Sqrt(v), (float v) => v * v, (float v) => UnitConverter.ValueReading(v) ?? "");
			selectedFactions.SetupSlider(factionRegularIncomeSlider, factionRegularIncomeLabel, (MissionFaction f) => ref f.regularIncome, (float v) => Mathf.Sqrt(v), (float v) => v * v, (float v) => UnitConverter.ValueReading(v) ?? "");
			selectedFactions.SetupSlider(excessFundsDistributeSlider, excessFundsDistributeLabel, (MissionFaction f) => ref f.excessFundsDistributePercent, (float v) => $"{v * 100f:0}%");
			selectedFactions.SetupSlider(playerJoinAllowanceSlider, playerJoinAllowanceLabel, (MissionFaction f) => ref f.playerJoinAllowance, (float v) => Mathf.Sqrt(v), (float v) => v * v, (float v) => UnitConverter.ValueReading(v) ?? "");
			selectedFactions.SetupSlider(killRewardSlider, killRewardLabel, (MissionFaction f) => ref f.killReward, (float v) => $"{v * 100f:F0}%");
			selectedFactions.SetupSlider(playerTaxRateSlider, playerTaxRateLabel, (MissionFaction f) => ref f.playerTaxRate, (float v) => $"{v * 100f:0}%");
			selectedFactions.SetupSlider(airSkillMultiplerSlider, airSkillMultiplerLabel, (MissionFaction f) => ref f.airSkillMultiplier, (float v) => $"{v:F1}");
			selectedFactions.SetupSlider(surfaceSkillMultiplerSlider, surfaceSkillMultiplerLabel, (MissionFaction f) => ref f.surfaceSkillMultiplier, (float v) => $"{v:F1}");
			selectedFactions.SetupSlider(startingWarheadsSlider, startingWarheadsLabel, (MissionFaction f) => ref f.startingWarheads, (int v) => v, (float v) => (int)v, (int v) => $"{v:0}");
			selectedFactions.SetupSlider(reserveWarheadsSlider, reserveWarheadsLabel, (MissionFaction f) => ref f.reserveWarheads, (int v) => v, (float v) => (int)v, (int v) => $"{v:0}");
			selectedFactions.SetupSlider(reserveAirframesSlider, reserveAirframesLabel, (MissionFaction f) => ref f.reserveAirframes, (int v) => v, (float v) => (int)v, (int v) => $"{v:0}");
			selectedFactions.SetupSlider(extraReservesPerPlayerSlider, extraReservesPerPlayerLabel, (MissionFaction f) => ref f.extraReservesPerPlayer, (int v) => v, (float v) => (int)v, (int v) => $"{v:0}");
			selectedFactions.SetupSlider(AIAircraftLimitSlider, AIAircraftLimitLabel, (MissionFaction f) => ref f.AIAircraftLimit, (int v) => v, (float v) => (int)v, (int v) => $"{v:0}");
			selectedFactions.SetupSlider(reduceAIPerFriendlyPlayerSlider, reduceAIPerFriendlyPlayerLabel, (MissionFaction f) => ref f.reduceAIPerFriendlyPlayer, (float v) => $"{v:0.0}");
			selectedFactions.SetupSlider(addAIPerEnemyPlayerSlider, addAIPerEnemyPlayerLabel, (MissionFaction f) => ref f.addAIPerEnemyPlayer, (float v) => $"{v:0.0}");
			selectedFactions.SetupToggle(preventJoinToggle, preventJoinToggleDifferentValue, (MissionFaction f) => ref f.preventJoin);
			selectedFactions.SetupToggle(preventDonationToggle, preventDonationToggleDifferentValue, (MissionFaction f) => ref f.preventDonation);
			SetupCameraPositionFields();
			selectedFactions.AddAndInvokeChanged(this, OnFactionsChanged);
		}

		public void SetMission(Mission mission)
		{
			this.mission = mission;
			factionSelect.options.Clear();
			factionSelect.options.Add(new TMP_Dropdown.OptionData("Both"));
			foreach (MissionFaction faction in mission.factions)
			{
				factionSelect.options.Add(new TMP_Dropdown.OptionData(faction.factionName));
			}
			factionSelect.SetValueWithoutNotify(0);
			SelectFaction();
		}

		private void SetupCameraPositionFields()
		{
			if (GameManager.gameState == GameState.Editor)
			{
				cameraPositionOverrideField.gameObject.SetActive(value: true);
				setCameraFromCurrentButton.onClick.AddListener(SetCameraPositionFromCurrent);
				moveCameraToSavedButton.onClick.AddListener(MoveCameraToSaved);
				cameraOverrideWrapper = ValueWrapper.FromCallback<ValueWrapperOverride<PositionRotation>, Override<PositionRotation>>(this, default(Override<PositionRotation>), selectedFactions.SetSameValueAction((MissionFaction f) => ref f.cameraStartPosition));
				cameraPositionWrapper = ValueWrapper.FromCallback<ValueWrapperGlobalPosition, GlobalPosition>(this, default(GlobalPosition), selectedFactions.SetSameValueAction((MissionFaction f) => ref f.cameraStartPosition.Value.Position));
				cameraRotationWrapper = ValueWrapper.FromCallback<ValueWrapperQuaternion, Quaternion>(this, default(Quaternion), selectedFactions.SetSameValueAction((MissionFaction f) => ref f.cameraStartPosition.Value.Rotation));
				cameraPositionOverrideField.Setup(cameraOverrideWrapper, cameraPositionField, cameraRotationField, moveCameraToSavedButton, setCameraFromCurrentButton);
				cameraPositionField.Setup("Position", cameraPositionWrapper);
				cameraRotationField.Setup("Rotation", cameraRotationWrapper);
			}
			else
			{
				cameraPositionOverrideField.gameObject.SetActive(value: false);
			}
		}

		private void SetCameraPositionFromCurrent()
		{
			SceneSingleton<CameraStateManager>.i.GetCameraPosition(out var positionRotation);
			cameraPositionWrapper.SetValue(positionRotation.Position, null);
			cameraRotationWrapper.SetValue(positionRotation.Rotation, null);
		}

		private void MoveCameraToSaved()
		{
			SceneSingleton<CameraStateManager>.i.SetCameraPosition(cameraOverrideWrapper.Value.Value);
		}

		public void SelectFaction()
		{
			string text = factionSelect.options[factionSelect.value].text;
			MissionFaction faction;
			if (text == "Both")
			{
				selectedFactions.ReplaceTargets(mission.factions);
			}
			else if (mission.TryGetFaction(text, out faction))
			{
				selectedFactions.ReplaceTargets(faction);
			}
			else
			{
				Debug.LogWarning("Failed to find faction with name: " + text);
			}
		}

		private void OnFactionsChanged()
		{
			if (cameraOverrideWrapper != null)
			{
				Override<PositionRotation> sameValue;
				bool flag = selectedFactions.TryGetSameValue((MissionFaction f) => f.cameraStartPosition, out sameValue);
				cameraPositionDifferentOverlay.SetActive(!flag);
				if (flag)
				{
					cameraOverrideWrapper.SetValue(sameValue, this);
					cameraPositionWrapper.SetValue(sameValue.Value.Position, this);
					cameraRotationWrapper.SetValue(sameValue.Value.Rotation, this);
				}
				else
				{
					cameraOverrideWrapper.SetValue(default(Override<PositionRotation>), this);
					cameraPositionWrapper.SetValue(default(GlobalPosition), this);
					cameraRotationWrapper.SetValue(default(Quaternion), this);
				}
			}
			GenerateUnitList();
		}

		private void GenerateUnitList()
		{
			List<string> list = new List<string>();
			foreach (InventoryItem inventoryItem in inventoryItems)
			{
				Object.Destroy(inventoryItem.gameObject);
			}
			inventoryItems.Clear();
			bool flag = selectedFactions.AllTheSame((MissionFaction x) => x.supplies, (UnitCount a, UnitCount b) => a.UnitType == b.UnitType && a.Count == b.Count);
			supplyNotSameOverlay.SetActive(!flag);
			if (!flag)
			{
				return;
			}
			IReadOnlyList<MissionFaction> targets = selectedFactions.Targets;
			for (int num = 0; num < targets[0].supplies.Count; num++)
			{
				UnitCount unitCount = targets[0].supplies[num];
				list.Add(unitCount.UnitType);
				InventoryItem component = Object.Instantiate(supplyItemPrefab, inventoryPanel).GetComponent<InventoryItem>();
				component.SetInventoryItem(num, unitCount, targets, this);
				inventoryItems.Add(component);
			}
			unitSupplyHelper.Clear();
			foreach (UnitDefinition aircraftAndVehicle in Encyclopedia.i.GetAircraftAndVehicles())
			{
				if (!aircraftAndVehicle.NotAllowed(MissionManager.AllowEventContent) && !list.Contains(aircraftAndVehicle.jsonKey))
				{
					unitSupplyHelper.Add(aircraftAndVehicle.unitName, aircraftAndVehicle);
				}
			}
			addUnitButton.interactable = unitSelect.options.Count > 0;
			unitSelect.SetValueWithoutNotify(0);
			unitSelect.RefreshShownValue();
		}

		public void RemoveSupplyEntry(InventoryItem inventoryItem)
		{
			inventoryItems.Remove(inventoryItem);
			GenerateUnitList();
		}

		public void AddSupplyUnit()
		{
			if (unitSelect.options.Count == 0)
			{
				return;
			}
			int value = unitSelect.value;
			UnitDefinition unitDefinition = unitSupplyHelper.keys[value];
			if (unitDefinition == null)
			{
				return;
			}
			foreach (MissionFaction target in selectedFactions.Targets)
			{
				MissionFaction current;
				MissionFaction missionFaction = (current = target);
				if (current.supplies == null)
				{
					current.supplies = new List<UnitCount>();
				}
				missionFaction.supplies.Add(new UnitCount(unitDefinition.jsonKey, 1));
			}
			GenerateUnitList();
		}
	}
}
