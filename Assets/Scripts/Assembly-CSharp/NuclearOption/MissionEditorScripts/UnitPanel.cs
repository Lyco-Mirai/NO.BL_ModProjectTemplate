using System.Collections.Generic;
using System.Linq;
using Mirage;
using NuclearOption.MissionEditorScripts.MultiSelect;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Outcomes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class UnitPanel : MonoBehaviour
	{
		[Header("Buttons")]
		[SerializeField]
		private Button copyButton;

		[SerializeField]
		private Button pasteButton;

		[SerializeField]
		private Button removeButton;

		[SerializeField]
		private TextMeshProUGUI removeButtonText;

		[Header("Title")]
		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI builtInNotice;

		[SerializeField]
		private TextMeshProUGUI typeText;

		[Header("Name")]
		[SerializeField]
		private TMP_InputField uniqueNameInput;

		[SerializeField]
		private GameObject uniqueNamePanel;

		[Header("Faction")]
		[SerializeField]
		private GameObject factionPanel;

		[SerializeField]
		private TMP_Dropdown factionDropdown;

		[SerializeField]
		private GameObject factionDropdownArrow;

		[SerializeField]
		private TextMeshProUGUI factionDropdownLabel;

		[SerializeField]
		private Color factionDropdownLabelNormalColor;

		[SerializeField]
		private Color factionDropdownLabelInactiveColor;

		[Header("Airbase")]
		[SerializeField]
		private GameObject airbasePanel;

		[SerializeField]
		private Button airbaseButton;

		[SerializeField]
		private ReferenceDataField airbaseReference;

		[Header("Spawning")]
		[SerializeField]
		private GameObject spawnTimingPanel;

		[SerializeField]
		private TextMeshProUGUI spawnTiming;

		[Header("Position")]
		[SerializeField]
		private GameObject positionMultipleWarning;

		[SerializeField]
		private Vector3DataField positionField;

		[SerializeField]
		private Vector3DataField rotationField;

		[Header("Capture")]
		[SerializeField]
		private GameObject capturePanel;

		[SerializeField]
		private FloatDataField.FloatSlider captureSliderMinMax = new FloatDataField.FloatSlider(0f, 10f);

		[SerializeField]
		private float captureSliderSteps = 0.1f;

		[SerializeField]
		private string captureSliderTextFormat = "0.0";

		[SerializeField]
		private string captureSliderDifferentValueTextFormat = "-";

		[SerializeField]
		private GameObject captureStrengthDifferentWarning;

		[SerializeField]
		private OverrideDataField overrideCaptureStrengthField;

		[SerializeField]
		private FloatDataField captureStrengthField;

		[SerializeField]
		private GameObject captureDefenseDifferentWarning;

		[SerializeField]
		private OverrideDataField overrideCaptureDefenseField;

		[SerializeField]
		private FloatDataField captureDefenseField;

		[Header("Inventory")]
		[SerializeField]
		private Button inventoryButton;

		[SerializeField]
		private InventoryInspector inventoryPanel;

		[Header("Unit Options")]
		[SerializeField]
		private AircraftOptions aircraftOptions;

		[SerializeField]
		private BuildingOptions buildingOptions;

		[SerializeField]
		private MissileOptions missileOptions;

		[SerializeField]
		private SceneryOptions sceneryOptions;

		[SerializeField]
		private ShipOptions shipOptions;

		[SerializeField]
		private VehicleOptions vehicleOptions;

		private UnitPanelOptions options;

		private readonly MultiSelect<SavedUnit> targets = new MultiSelect<SavedUnit>();

		private ValueWrapperOverride<float> captureStrengthWrapper;

		private ValueWrapperOverride<float> captureDefenseWrapper;

		private IValueWrapper<GlobalPosition> multiPositionWrapper;

		private FloatDataField.FloatSettings captureSliderSettings => new FloatDataField.FloatSettings
		{
			Slider = captureSliderMinMax,
			Steps = captureSliderSteps,
			TextFormat = captureSliderTextFormat
		};

		public static bool CanCapture(SavedUnit unit)
		{
			if (!(unit is SavedPilot))
			{
				if (!(unit is SavedMissile))
				{
					if (unit is SavedScenery)
					{
						return false;
					}
					return true;
				}
				return false;
			}
			return false;
		}

		public static bool CanHaveFaction(SavedUnit unit)
		{
			if (unit is SavedScenery)
			{
				return false;
			}
			return true;
		}

		public static void SetUnitFaction(SavedUnit saved, string factionName)
		{
			if (!(saved is SavedBuilding { AirbaseRef: not null }))
			{
				saved.faction = factionName;
				Unit unit = saved.Unit;
				unit.NetworkHQ = FactionRegistry.HqFromName(factionName);
				if (unit.TryGetComponent<Airbase>(out var component))
				{
					component.EditorSetFaction(factionName, setAttachedAirbaseFaction: true);
				}
			}
		}

		private void Awake()
		{
			copyButton.onClick.AddListener(CopyToClipboard);
			pasteButton.onClick.AddListener(PasteFromClipboard);
			removeButton.onClick.AddListener(RemoveUnit);
			airbaseButton.onClick.AddListener(AirbaseButtonClicked);
			inventoryButton.onClick.AddListener(OpenInventoryPanel);
			uniqueNameInput.onEndEdit.AddListener(ChangeUniqueName);
			List<string> factionOptions = FactionHelper.GetFactionsAndNeutral();
			factionDropdown.options.Clear();
			factionDropdown.AddOptions(factionOptions);
			targets.AddAndInvokeChanged(factionDropdown, delegate
			{
				foreach (SavedUnit target in targets.Targets)
				{
					if (target is SavedBuilding { AirbaseRef: not null } savedBuilding)
					{
						savedBuilding.faction = savedBuilding.AirbaseRef.faction;
					}
				}
				string sameValue;
				int valueWithoutNotify = ((!MultiSelect<SavedUnit>.TryGetSameValue(targets.Targets.Where(CanHaveFaction), (SavedUnit x) => x.faction, out sameValue)) ? (-1) : ((!FactionHelper.EmptyOrNoFactionOrNeutral(sameValue)) ? factionOptions.IndexOf(sameValue) : 0));
				factionDropdown.SetValueWithoutNotify(valueWithoutNotify);
			});
			factionDropdown.onValueChanged.AddListener(delegate(int index)
			{
				foreach (SavedUnit target2 in targets.Targets)
				{
					if (CanHaveFaction(target2))
					{
						if (target2 is SavedBuilding { AirbaseRef: not null })
						{
							break;
						}
						CheckOverride(target2);
						string factionName = factionOptions[index];
						SetUnitFaction(target2, factionName);
					}
				}
			});
			captureStrengthWrapper = ValueWrapper.FromCallback<ValueWrapperOverride<float>, Override<float>>(this, default(Override<float>), delegate(Override<float> value)
			{
				foreach (SavedUnit target3 in targets.Targets)
				{
					if (CanCapture(target3))
					{
						target3.CaptureStrength = value;
					}
				}
			});
			captureStrengthWrapper.RegisterOnChange((object)this, (ValueWrapper<Override<float>>.OnChangeDelegate)delegate
			{
				CheckAllOverrides();
			});
			captureStrengthField.Setup("Capture Power", captureStrengthWrapper, captureSliderSettings);
			overrideCaptureStrengthField.Setup(captureStrengthWrapper, captureStrengthField);
			captureDefenseWrapper = ValueWrapper.FromCallback<ValueWrapperOverride<float>, Override<float>>(this, default(Override<float>), delegate(Override<float> value)
			{
				foreach (SavedUnit target4 in targets.Targets)
				{
					if (CanCapture(target4))
					{
						target4.CaptureDefense = value;
					}
				}
			});
			captureDefenseWrapper.RegisterOnChange((object)this, (ValueWrapper<Override<float>>.OnChangeDelegate)delegate
			{
				CheckAllOverrides();
			});
			captureDefenseField.Setup("Capture Defense", captureDefenseWrapper, captureSliderSettings);
			overrideCaptureDefenseField.Setup(captureDefenseWrapper, captureDefenseField);
			targets.AddAndInvokeChanged(this, OnTargetsChanged);
			SceneSingleton<UnitSelection>.i.OnSelect += UnitMenu_OnSelect;
		}

		private void OnTargetsChanged()
		{
			inventoryPanel.gameObject.SetActive(value: false);
			int count = targets.Targets.Count;
			if (count == 0)
			{
				return;
			}
			bool flag = count == 1;
			SavedUnit firstSaved = targets.Targets[0];
			Unit unit = firstSaved.Unit;
			if (flag)
			{
				if (unit.BuiltIn)
				{
					titleText.text = unit.unitName + " [Map Object]";
				}
				else
				{
					titleText.text = unit.unitName;
				}
			}
			else
			{
				titleText.text = $"{count} units selected";
			}
			factionPanel.SetActive(targets.Any(CanHaveFaction));
			UpdateFactionInteractable();
			builtInNotice.gameObject.SetActive(targets.Any((SavedUnit savedUnit) => savedUnit.Unit.BuiltIn));
			typeText.text = targets.GetSameLabel((SavedUnit savedUnit) => savedUnit.type);
			uniqueNamePanel.gameObject.SetActive(flag);
			uniqueNameInput.interactable = flag && firstSaved.PlacementType != PlacementType.BuiltIn;
			uniqueNameInput.SetTextWithoutNotify(flag ? firstSaved.UniqueName : "-");
			copyButton.interactable = flag;
			pasteButton.interactable = SceneSingleton<UnitCopyPaste>.i.Clipboard != null;
			removeButton.interactable = targets.Any((SavedUnit x) => x.PlacementType != PlacementType.BuiltIn);
			if (flag)
			{
				removeButtonText.text = ((firstSaved.PlacementType == PlacementType.Override) ? "Remove Override" : "Remove Unit");
			}
			else if (targets.All((SavedUnit saved) => saved.PlacementType == PlacementType.Override))
			{
				removeButtonText.text = $"Remove {count} Overrides";
			}
			else if (targets.All((SavedUnit saved) => saved.PlacementType == PlacementType.Custom))
			{
				removeButtonText.text = $"Remove {count} Units";
			}
			else if (targets.All((SavedUnit saved) => saved.PlacementType == PlacementType.BuiltIn))
			{
				removeButtonText.text = "Cant Remove";
			}
			else
			{
				removeButtonText.text = $"Remove {count} Units and Overrides";
			}
			Airbase component;
			bool flag2 = flag && unit.TryGetComponent<Airbase>(out component);
			bool flag3 = targets.All((SavedUnit x) => x is SavedBuilding);
			airbasePanel.SetActive(flag2 || flag3);
			airbaseButton.gameObject.SetActive(flag2);
			airbaseReference.gameObject.SetActive(flag3);
			if (flag3)
			{
				using AutoPool<List<SavedAirbase>>.Wrapper wrapper = AutoPool<List<SavedAirbase>>.Take();
				List<SavedAirbase> item = wrapper.Item;
				MissionManager.GetAllSavedAirbaseNonAlloc(item);
				SavedAirbase differentValuePlaceholder = null;
				SavedAirbase current;
				if (targets.TryGetSameValue(GetAirbaseRef, out var sameValue))
				{
					current = sameValue;
				}
				else
				{
					differentValuePlaceholder = new SavedAirbase
					{
						UniqueName = "-",
						DisplayName = "-"
					};
					current = differentValuePlaceholder;
				}
				List<SavedAirbase> list = new List<SavedAirbase>();
				if (differentValuePlaceholder != null)
				{
					list.Add(differentValuePlaceholder);
				}
				for (int num = 0; num < item.Count; num++)
				{
					SavedAirbase savedAirbase = item[num];
					if (!savedAirbase.IsAttached())
					{
						list.Add(savedAirbase);
					}
				}
				airbaseReference.Setup("Airbase", list, current, delegate(SavedAirbase value)
				{
					if (differentValuePlaceholder == null || !(value.UniqueName == differentValuePlaceholder.UniqueName))
					{
						CheckAllOverrides();
						foreach (SavedBuilding target in targets.Targets)
						{
							target.SetOrRemoveAirbase(value);
						}
						UpdateFactionInteractable();
					}
				});
			}
			spawnTimingPanel.SetActive(flag);
			if (flag)
			{
				IEnumerable<string> values = from outcome in MissionManager.CurrentMission.RuntimeObjectives.AllOutcomes.OfType<SpawnUnitOutcome>()
					where outcome.UnitsToSpawn.Contains(firstSaved)
					select "- " + outcome.SavedOutcome.UniqueName;
				string text = string.Join("\n", values);
				if (text.Length == 0)
				{
					text = "<i>Mission Start</i>";
				}
				spawnTiming.text = text;
			}
			bool flag4 = targets.Any(CanCapture);
			capturePanel.SetActive(flag4);
			if (flag4)
			{
				Override<float> sameValue2;
				bool flag5 = MultiSelect<SavedUnit>.TryGetSameValue(targets.Targets.Where(CanCapture), (SavedUnit x) => x.CaptureStrength, out sameValue2);
				captureStrengthDifferentWarning.SetActive(!flag5);
				captureStrengthField.TextFormat = (flag5 ? captureSliderSettings.TextFormat : captureSliderDifferentValueTextFormat);
				captureStrengthWrapper.SetValue(flag5 ? sameValue2 : default(Override<float>), this);
				Override<float> sameValue3;
				bool flag6 = MultiSelect<SavedUnit>.TryGetSameValue(targets.Targets.Where(CanCapture), (SavedUnit x) => x.CaptureDefense, out sameValue3);
				captureDefenseDifferentWarning.SetActive(!flag6);
				captureDefenseField.TextFormat = (flag6 ? captureSliderSettings.TextFormat : captureSliderDifferentValueTextFormat);
				captureDefenseWrapper.SetValue(flag6 ? sameValue3 : default(Override<float>), this);
			}
			positionMultipleWarning.gameObject.SetActive(!flag);
			if (flag)
			{
				if (firstSaved.PlacementType == PlacementType.Custom)
				{
					positionField.Setup("Position", firstSaved.PositionWrapper);
					rotationField.Setup("Rotation", firstSaved.RotationWrapper);
				}
				else
				{
					unit.transform.GetPositionAndRotation(out var position, out var rotation);
					positionField.SetupReadOnly("Position", position.ToGlobalPosition());
					rotationField.SetupReadOnly("Rotation", rotation.eulerAngles);
				}
			}
			else
			{
				RefreshMultiTargetPosition();
			}
			inventoryButton.gameObject.SetActive(targets.Any((SavedUnit saved) => saved.Unit.TryGetComponent<UnitStorage>(out var _)));
			UnitPanelOptions optionsPanel = GetOptionsPanel();
			if (options?.GetType() != optionsPanel?.GetType())
			{
				if (options != null)
				{
					options.gameObject.SetActive(value: false);
					options.Cleanup();
					Object.Destroy(options.gameObject);
					options = null;
				}
				if (optionsPanel != null)
				{
					options = Object.Instantiate(optionsPanel, base.transform);
					options.Setup(this, targets);
				}
			}
			if (options != null)
			{
				options.OnTargetsChanged();
			}
			FixLayout.RebuildRoot(base.gameObject);
			static SavedAirbase GetAirbaseRef(SavedUnit savedUnit)
			{
				SavedBuilding savedBuilding = (SavedBuilding)savedUnit;
				if (savedUnit.PlacementType == PlacementType.BuiltIn)
				{
					Airbase mapAirbase = savedUnit.Unit.MapAirbase;
					if (!(mapAirbase != null))
					{
						return null;
					}
					return mapAirbase.SavedAirbase;
				}
				return savedBuilding.AirbaseRef;
			}
		}

		private void RefreshMultiTargetPosition()
		{
			int count = targets.Targets.Count;
			Vector3 value = default(Vector3);
			foreach (SavedUnit target in targets.Targets)
			{
				value += ((target.PlacementType == PlacementType.Custom) ? target.globalPosition : target.Unit.GlobalPosition()).AsVector3() / count;
			}
			positionField.SetupReadOnly("Position", value);
			rotationField.SetupNonValue("Rotation");
		}

		private void UpdateFactionInteractable()
		{
			bool flag = targets.All((SavedUnit saved) => !(saved is SavedBuilding { AirbaseRef: not null }));
			factionDropdown.interactable = flag;
			factionDropdownLabel.color = (flag ? factionDropdownLabelNormalColor : factionDropdownLabelInactiveColor);
			factionDropdownArrow.gameObject.SetActive(flag);
		}

		private UnitPanelOptions GetOptionsPanel()
		{
			if (!targets.TryGetSameValue((SavedUnit x) => x.GetType(), out var sameValue))
			{
				return null;
			}
			if (sameValue == typeof(SavedAircraft))
			{
				return aircraftOptions;
			}
			if (sameValue == typeof(SavedBuilding))
			{
				return buildingOptions;
			}
			if (sameValue == typeof(SavedMissile))
			{
				return missileOptions;
			}
			if (sameValue == typeof(SavedShip))
			{
				return shipOptions;
			}
			if (sameValue == typeof(SavedVehicle))
			{
				return vehicleOptions;
			}
			if (sameValue == typeof(SavedScenery))
			{
				return sceneryOptions;
			}
			if (sameValue == typeof(SavedPilot))
			{
				return null;
			}
			if (sameValue == typeof(SavedContainer))
			{
				return null;
			}
			Debug.LogError($"No unit Options for type {sameValue}");
			return null;
		}

		public void Start()
		{
			Setup(SceneSingleton<UnitSelection>.i.SelectionDetails);
		}

		public void SelectedRefreshed()
		{
			Setup(SceneSingleton<UnitSelection>.i.SelectionDetails);
		}

		public void Setup(SelectionDetails details)
		{
			multiPositionWrapper?.UnregisterOnChange(this);
			multiPositionWrapper = null;
			if (details is UnitSelectionDetails unitSelectionDetails)
			{
				if (unitSelectionDetails.Unit.SavedUnit == null)
				{
					Debug.LogError("UnitPanel unit did not have a SavedUnit");
				}
				else
				{
					targets.ReplaceTargets(unitSelectionDetails.Unit.SavedUnit);
				}
			}
			else if (details is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				multiPositionWrapper = multiSelectSelectionDetails.PositionWrapper;
				multiPositionWrapper.RegisterOnChange(this, RefreshMultiTargetPosition);
				List<SavedUnit> list = new List<SavedUnit>();
				foreach (SingleSelectionDetails item in multiSelectSelectionDetails.Items)
				{
					if (!(item is UnitSelectionDetails unitSelectionDetails2))
					{
						Debug.LogError("Unit Panel should not be open while MultiSelectSelectionDetails is not Unit");
						return;
					}
					if (unitSelectionDetails2.Unit.SavedUnit == null)
					{
						Debug.LogError("UnitPanel unit did not have a SavedUnit");
					}
					else
					{
						list.Add(unitSelectionDetails2.Unit.SavedUnit);
					}
				}
				targets.ReplaceTargets(list);
			}
			else
			{
				Debug.LogError("UnitPanel was given a null details");
			}
		}

		public void OpenInventoryPanel()
		{
			if (inventoryPanel.gameObject.activeSelf)
			{
				return;
			}
			List<(SavedUnit, UnitStorage)> list = new List<(SavedUnit, UnitStorage)>();
			foreach (SavedUnit target in targets.Targets)
			{
				if (target.Unit.TryGetComponent<UnitStorage>(out var component))
				{
					list.Add((target, component));
				}
			}
			if (list.Count == 0)
			{
				Debug.LogError("OpenInventoryPanel called but no units had UnitStorage");
				return;
			}
			inventoryPanel.gameObject.SetActive(value: true);
			inventoryPanel.transform.SetParent(SceneSingleton<MissionEditor>.i.transform);
			inventoryPanel.transform.localPosition = Vector3.zero;
			inventoryPanel.UnitPanelTargetsChanged(list);
		}

		private void OnDestroy()
		{
			if (inventoryPanel != null)
			{
				Object.Destroy(inventoryPanel.gameObject);
			}
			SceneSingleton<UnitSelection>.i.OnSelect -= UnitMenu_OnSelect;
		}

		private void UnitMenu_OnSelect(SelectionDetails selectionDetails)
		{
			if (selectionDetails == null)
			{
				Object.Destroy(base.gameObject);
			}
			if (!(selectionDetails is UnitSelectionDetails) && selectionDetails is MultiSelectSelectionDetails multiSelectSelectionDetails)
			{
				_ = multiSelectSelectionDetails.SelectionType == typeof(UnitSelectionDetails);
			}
		}

		public void CopyToClipboard()
		{
			if (targets.Targets.Count == 1)
			{
				SceneSingleton<UnitCopyPaste>.i.Clipboard = targets.Targets.First();
				pasteButton.interactable = true;
			}
			else
			{
				Debug.LogError("Can't copy when more than 1 unit is selected");
			}
		}

		public void PasteFromClipboard()
		{
			SavedUnit clipboard = SceneSingleton<UnitCopyPaste>.i.Clipboard;
			if (clipboard == null)
			{
				Debug.LogWarning("Can't paste because clipboard unit was null");
				return;
			}
			foreach (SavedUnit target in targets.Targets)
			{
				CheckOverride(target);
				UnitCopyPaste.CopyPaste(MissionManager.CurrentMission, clipboard, target.Unit, target);
				target.Unit.NetworkHQ = FactionRegistry.HqFromName(target.faction);
			}
			targets.ReplaceTargets(targets.Targets);
			SceneSingleton<UnitSelection>.i.RefreshSelected();
		}

		public void RemoveUnit()
		{
			List<Unit> list = new List<Unit>();
			foreach (SavedUnit item in targets.Targets.ToList())
			{
				if (item.PlacementType == PlacementType.Override || item.PlacementType == PlacementType.Custom)
				{
					SceneSingleton<MissionEditor>.i.RemoveUnit(item.Unit);
				}
				if (item.PlacementType == PlacementType.BuiltIn || item.PlacementType == PlacementType.Override)
				{
					list.Add(item.Unit);
				}
			}
			SceneSingleton<UnitSelection>.i.ReplaceSelection(list);
		}

		public void ChangeUniqueName(string newValue)
		{
			if (targets.Targets.Count != 1)
			{
				Debug.LogError("Can't set unique name unless only 1 unit is selected");
				return;
			}
			SavedUnit savedUnit = targets.Targets.First();
			string text = uniqueNameInput.text;
			if (string.IsNullOrEmpty(text))
			{
				text = savedUnit.type;
			}
			using (AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take())
			{
				List<SavedUnit> item = wrapper.Item;
				MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: true);
				SaveHelper.MakeUnique(ref text, savedUnit, item);
			}
			savedUnit.Rename(text);
			savedUnit.Unit.NetworkUniqueName = savedUnit.UniqueName;
			uniqueNameInput.SetTextWithoutNotify(savedUnit.UniqueName);
			if (savedUnit.Unit.TryGetComponent<Airbase>(out var component))
			{
				string newName = "<UNIT_AIRBASE>++" + savedUnit.UniqueName;
				FactionRegistry.ChangeAirbaseName(component, newName);
				component.SavedAirbase.Rename(newName);
			}
		}

		private void AirbaseButtonClicked()
		{
			Airbase component;
			if (targets.Targets.Count != 1)
			{
				Debug.LogError("Can't set unique name unless only 1 unit is selected");
			}
			else if (targets.Targets.First().Unit.TryGetComponent<Airbase>(out component))
			{
				SceneSingleton<UnitSelection>.i.SetSelection(component);
			}
		}

		public void CheckAllOverrides()
		{
			foreach (SavedUnit target in targets.Targets)
			{
				CheckOverride(target);
			}
		}

		public void CheckOverride(SavedUnit savedUnit)
		{
			if (savedUnit.PlacementType == PlacementType.BuiltIn)
			{
				CreateOverride(savedUnit);
			}
		}

		private void CreateOverride(SavedUnit savedUnit)
		{
			string uniqueName = savedUnit.UniqueName;
			_ = MissionManager.CurrentMission;
			SavedUnit savedUnit2;
			using (AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take())
			{
				List<SavedUnit> item = wrapper.Item;
				MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: false);
				savedUnit2 = item.FirstOrDefault((SavedUnit x) => x.UniqueName == uniqueName);
			}
			if (savedUnit2 != null)
			{
				Debug.LogError("Unit with name " + uniqueName + " already in mission but trying to create a new override for it");
			}
			else
			{
				savedUnit.PlacementType = PlacementType.Override;
				SceneSingleton<MissionEditor>.i.AddUnitOverride(savedUnit.Unit, savedUnit);
			}
			removeButton.interactable = true;
		}
	}
}
