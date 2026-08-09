using System;
using System.Collections.Generic;
using System.Linq;
using Mirage;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts.Buttons
{
	public class NewUnitPanel : MonoBehaviour, IPlacingMenu
	{
		private enum PlaceType
		{
			None = 0,
			Unit = 1,
			Airbase = 2
		}

		private class UnitOptionProvider
		{
			public enum SortMode
			{
				Name = 0,
				Value = 1
			}

			private readonly UnitDefinition[] noEvent;

			private readonly UnitDefinition[] allowEvent;

			public int StickyOption;

			public UnitDefinition[] GetUnitDefinitions()
			{
				return GetUnits(MissionManager.AllowEventContent);
			}

			public UnitDefinition[] GetUnits(bool allowEventContent)
			{
				if (!allowEventContent)
				{
					return noEvent;
				}
				return allowEvent;
			}

			private UnitOptionProvider(UnitDefinition[] noEvent, UnitDefinition[] allowEvent)
			{
				this.noEvent = noEvent;
				this.allowEvent = allowEvent;
				StickyOption = 0;
			}

			public static UnitOptionProvider Create<T>(List<T> definitionOptions, SortMode sortMode = SortMode.Name) where T : UnitDefinition
			{
				UnitDefinition[] array = CreateUnitArray(definitionOptions, allowEventContent: false, sortMode);
				UnitDefinition[] array2 = CreateUnitArray(definitionOptions, allowEventContent: true, sortMode);
				return new UnitOptionProvider(array, array2);
			}

			private static UnitDefinition[] CreateUnitArray<T>(List<T> definitionOptions, bool allowEventContent, SortMode sortMode) where T : UnitDefinition
			{
				List<UnitDefinition> list = new List<UnitDefinition>(definitionOptions.Count);
				foreach (T definitionOption in definitionOptions)
				{
					if (definitionOption.IsAllowed(allowEventContent))
					{
						list.Add(definitionOption);
					}
				}
				if (sortMode == SortMode.Name || sortMode != SortMode.Value)
				{
					list.Sort((UnitDefinition x, UnitDefinition y) => x.unitName.CompareTo(y.unitName));
				}
				else
				{
					list.Sort((UnitDefinition x, UnitDefinition y) => x.value.CompareTo(y.value));
				}
				return list.ToArray();
			}
		}

		private static readonly Dictionary<string, UnitOptionProvider> unitProviders = new Dictionary<string, UnitOptionProvider>();

		private static string stickyTab;

		private static bool stickyPlaceMore;

		[SerializeField]
		private Button aircraft;

		[SerializeField]
		private Button vehicles;

		[SerializeField]
		private Button buildings;

		[SerializeField]
		private Button ships;

		[SerializeField]
		private Button scenery;

		[SerializeField]
		private Button missiles;

		[SerializeField]
		private Button otherUnits;

		[SerializeField]
		private Button airbase;

		[SerializeField]
		private Color highlight;

		[SerializeField]
		private Color normal;

		[SerializeField]
		private TMP_Dropdown typeDropdown;

		[SerializeField]
		private TMP_Dropdown factionDropdown;

		[SerializeField]
		private Vector3DataField positionField;

		[SerializeField]
		private SliderToggle placeMoreToggle;

		[SerializeField]
		private TextMeshProUGUI placeTextHint;

		[SerializeField]
		private float placeFlagSize = 5f;

		private Button activeButton;

		private List<string> factionOptions;

		private PlaceType placeType;

		private UnitOptionProvider activeProvider;

		private UnitDefinition placingDefinition;

		private GameObject placingObject;

		private AirbaseEditorFlag placingFlag;

		private ValueWrapperGlobalPosition positionWrapper;

		private LiveryKey aircraftLiver;

		private UnitDefinition[] UnitDefinitions => activeProvider.GetUnitDefinitions();

		private void Awake()
		{
			if (unitProviders.Count == 0)
			{
				unitProviders.Add("aircraft", UnitOptionProvider.Create(Encyclopedia.i.aircraft, UnitOptionProvider.SortMode.Value));
				unitProviders.Add("vehicles", UnitOptionProvider.Create(Encyclopedia.i.vehicles));
				unitProviders.Add("buildings", UnitOptionProvider.Create(Encyclopedia.i.buildings));
				unitProviders.Add("ships", UnitOptionProvider.Create(Encyclopedia.i.ships));
				unitProviders.Add("scenery", UnitOptionProvider.Create(Encyclopedia.i.scenery));
				unitProviders.Add("missiles", UnitOptionProvider.Create(Encyclopedia.i.missiles));
				unitProviders.Add("otherUnits", UnitOptionProvider.Create(Encyclopedia.i.otherUnits));
			}
			aircraft.onClick.AddListener(delegate
			{
				TabClickedUnit(aircraft, "aircraft");
			});
			vehicles.onClick.AddListener(delegate
			{
				TabClickedUnit(vehicles, "vehicles");
			});
			buildings.onClick.AddListener(delegate
			{
				TabClickedUnit(buildings, "buildings");
			});
			ships.onClick.AddListener(delegate
			{
				TabClickedUnit(ships, "ships");
			});
			scenery.onClick.AddListener(delegate
			{
				TabClickedUnit(scenery, "scenery");
			});
			missiles.onClick.AddListener(delegate
			{
				TabClickedUnit(missiles, "missiles");
			});
			otherUnits.onClick.AddListener(delegate
			{
				TabClickedUnit(otherUnits, "otherUnits");
			});
			airbase.onClick.AddListener(delegate
			{
				TabClickedAirbase(airbase);
			});
			typeDropdown.onValueChanged.AddListener(UnitTypeChanged);
			factionDropdown.onValueChanged.AddListener(FactionChanged);
			factionOptions = FactionHelper.GetFactionsAndNeutral();
			factionDropdown.ClearOptions();
			factionDropdown.AddOptions(factionOptions);
			int valueWithoutNotify = 0;
			if (SceneSingleton<MissionEditor>.i.stickyFaction != null)
			{
				valueWithoutNotify = ((!FactionHelper.EmptyOrNoFactionOrNeutral(SceneSingleton<MissionEditor>.i.stickyFaction)) ? factionOptions.IndexOf(SceneSingleton<MissionEditor>.i.stickyFaction) : 0);
			}
			factionDropdown.SetValueWithoutNotify(valueWithoutNotify);
			placeMoreToggle.onValueChanged.AddListener(PlaceMoreChanged);
			positionWrapper = new ValueWrapperGlobalPosition();
			positionField.Setup("Position", positionWrapper);
			positionField.Interactable = false;
			positionWrapper.SetValue(default(GlobalPosition), this);
			ClearTypeDropDown();
			Button button = ButtonFromSticky();
			if (button != null)
			{
				button.onClick.Invoke();
			}
			placeMoreToggle.isOn = stickyPlaceMore;
		}

		private Button ButtonFromSticky()
		{
			return stickyTab switch
			{
				"aircraft" => aircraft, 
				"vehicles" => vehicles, 
				"buildings" => buildings, 
				"ships" => ships, 
				"scenery" => scenery, 
				"missiles" => missiles, 
				"otherUnits" => otherUnits, 
				"airbase" => airbase, 
				_ => null, 
			};
		}

		private void ClearTypeDropDown()
		{
			typeDropdown.options.Clear();
			typeDropdown.options.Add(new TMP_Dropdown.OptionData("N/A"));
			typeDropdown.interactable = false;
			typeDropdown.SetValueWithoutNotify(0);
			typeDropdown.RefreshShownValue();
		}

		private void SetActiveButton(Button newButton)
		{
			if (activeButton != null)
			{
				activeButton.image.color = normal;
			}
			activeButton = newButton;
			if (activeButton != null)
			{
				activeButton.image.color = highlight;
			}
		}

		private void UnitTypeChanged(int index)
		{
			activeProvider.StickyOption = index;
			SpawnUnit(UnitDefinitions[index]);
		}

		private void FactionChanged(int index)
		{
			SceneSingleton<MissionEditor>.i.stickyFaction = factionOptions[index];
			if (placingFlag != null)
			{
				placingFlag.SetColor(FactionColor());
			}
			SetLiveryIfPlacingAircraft();
		}

		private void PlaceMoreChanged(bool toggle)
		{
			stickyPlaceMore = placeMoreToggle.isOn;
			placeTextHint.text = (toggle ? "Place multiple units" : "Select unit after placing (shift click to place more)");
		}

		private void TabClickedUnit(Button button, string unitType)
		{
			SetActiveButton(button);
			activeProvider = unitProviders[unitType];
			stickyTab = unitType;
			typeDropdown.options.Clear();
			UnitDefinition[] unitDefinitions = UnitDefinitions;
			foreach (UnitDefinition unitDefinition in unitDefinitions)
			{
				typeDropdown.options.Add(new TMP_Dropdown.OptionData(unitDefinition.unitName));
			}
			typeDropdown.interactable = true;
			typeDropdown.SetValueWithoutNotify(activeProvider.StickyOption);
			typeDropdown.RefreshShownValue();
			SpawnUnit(UnitDefinitions[activeProvider.StickyOption]);
		}

		private void SetLiveryIfPlacingAircraft()
		{
			if (placingObject.TryGetComponent<Aircraft>(out var component))
			{
				List<(LiveryKey, string)> list = new List<(LiveryKey, string)>();
				LoadoutSelector.GetLiveryOptions(list, component.definition, SceneSingleton<MissionEditor>.i.stickyFaction, allowFactionLivery: true);
				aircraftLiver = list.FirstOrDefault().Item1;
				component.SetLiveryKey(aircraftLiver, loadIfUnspawned: true);
			}
		}

		private void SpawnUnit(UnitDefinition unitDefinition)
		{
			CancelPlace();
			placeType = PlaceType.Unit;
			placingDefinition = unitDefinition;
			placingObject = UnityEngine.Object.Instantiate(unitDefinition.unitPrefab);
			SetLiveryIfPlacingAircraft();
			Collider[] componentsInChildren = placingObject.GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			SceneSingleton<UnitSelection>.i.StartPlaceUnit(this);
		}

		private void TabClickedAirbase(Button button)
		{
			SetActiveButton(button);
			CancelPlace();
			ClearTypeDropDown();
			placeType = PlaceType.Airbase;
			placingFlag = MissionEditor.CreateFlag(FactionColor(), placeFlagSize);
			placingObject = placingFlag.gameObject;
			SceneSingleton<UnitSelection>.i.StartPlaceUnit(this);
		}

		private Color FactionColor()
		{
			Faction faction = FactionRegistry.FactionFromName(SceneSingleton<MissionEditor>.i.stickyFaction);
			if (!(faction != null))
			{
				return Color.white;
			}
			return faction.color;
		}

		private void OnDestroy()
		{
			CancelPlace();
		}

		public void CancelPlace()
		{
			if (placingObject != null)
			{
				UnityEngine.Object.Destroy(placingObject);
			}
			if (placeType != PlaceType.None)
			{
				SceneSingleton<UnitSelection>.i.StopPlacingUnit(this);
			}
			placingObject = null;
			placingFlag = null;
			placeType = PlaceType.None;
		}

		void IPlacingMenu.CancelPlace()
		{
			CancelPlace();
			ClearTypeDropDown();
			positionWrapper.SetValue(default(GlobalPosition), this);
			SetActiveButton(null);
		}

		(bool placeMore, IEditorSelectable placedObject) IPlacingMenu.Place(bool shift)
		{
			return placeType switch
			{
				PlaceType.Unit => PlaceUnit(shift), 
				PlaceType.Airbase => PlaceAirbase(shift), 
				_ => throw new Exception("Place should not be called when no object is being placed"), 
			};
		}

		private (bool placeMore, IEditorSelectable placedObject) PlaceUnit(bool shift)
		{
			placingObject.transform.GetPositionAndRotation(out var position, out var rotation);
			GlobalPosition position2 = position.ToGlobalPosition();
			FactionHQ factionHq = FactionRegistry.HqFromName(SceneSingleton<MissionEditor>.i.stickyFaction);
			string jsonKey = placingDefinition.jsonKey;
			using (AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take())
			{
				List<SavedUnit> item = wrapper.Item;
				MissionManager.GetAllSavedUnitsNonAlloc(item, includeBuiltIn: true);
				SaveHelper.MakeUnique(ref jsonKey, item, warn: false);
			}
			Unit unit = NetworkSceneSingleton<Spawner>.i.SpawnFromUnitDefinitionInEditor(placingDefinition, position2, rotation, factionHq, jsonKey);
			Physics.SyncTransforms();
			SavedUnit savedUnit = SceneSingleton<MissionEditor>.i.RegisterNewUnit(unit, jsonKey);
			if (unit.TryGetComponent<Aircraft>(out var component))
			{
				((SavedAircraft)savedUnit).liveryKey = aircraftLiver;
				component.SetLiveryKey(aircraftLiver, loadIfUnspawned: true);
			}
			if (unit.TryGetComponent<Airbase>(out var component2))
			{
				MissionEditor.CreateFlagForAirbase(component2);
			}
			int num;
			if (!shift)
			{
				num = (placeMoreToggle.isOn ? 1 : 0);
				if (num == 0)
				{
					CancelPlace();
				}
			}
			else
			{
				num = 1;
			}
			return (placeMore: (byte)num != 0, placedObject: unit);
		}

		private (bool placeMore, IEditorSelectable placedObject) PlaceAirbase(bool shift)
		{
			Mission currentMission = MissionManager.CurrentMission;
			SavedAirbase savedAirbase = new SavedAirbase();
			savedAirbase.SavedInMission = true;
			savedAirbase.UniqueName = AirbasePanel.NewAirbaseUniqueName(null);
			savedAirbase.DisplayName = savedAirbase.UniqueName;
			savedAirbase.faction = SceneSingleton<MissionEditor>.i.stickyFaction;
			currentMission.airbases.Add(savedAirbase);
			Airbase item = currentMission.SpawnCustomAirbase(savedAirbase, NetworkManagerNuclearOption.i.ServerObjectManager);
			MissionEditor.CreateFlagForAirbase(item);
			GlobalPosition value = SceneSingleton<UnitSelection>.i.placementTransform.GlobalPosition();
			savedAirbase.CenterWrapper.SetValue(value, this);
			savedAirbase.SelectionPositionWrapper.SetValue(value, this);
			int num;
			if (!shift)
			{
				num = (placeMoreToggle.isOn ? 1 : 0);
				if (num == 0)
				{
					CancelPlace();
				}
			}
			else
			{
				num = 1;
			}
			return (placeMore: (byte)num != 0, placedObject: item);
		}

		void IPlacingMenu.MoveCursor(Transform placementTransform)
		{
			switch (placeType)
			{
			case PlaceType.Unit:
			{
				Vector3 position2 = placementTransform.position + placementTransform.up * placingDefinition.spawnOffset.y;
				placingObject.transform.SetPositionAndRotation(position2, placementTransform.rotation);
				positionWrapper.SetValue(position2.ToGlobalPosition(), this);
				break;
			}
			case PlaceType.Airbase:
			{
				Vector3 position = placementTransform.position;
				placingObject.transform.position = position;
				positionWrapper.SetValue(position.ToGlobalPosition(), this);
				break;
			}
			}
		}
	}
}
