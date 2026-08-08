using System.Collections.Generic;
using NuclearOption.SavedMission;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NuclearOption.MissionEditorScripts
{
	public class BuildingOptions : UnitPanelOptions
	{
		private const string WARHEAD_PRODUCTION_TYPE = "Nuclear Warhead";

		[Header("Capture")]
		[SerializeField]
		private Toggle captureToggle;

		[SerializeField]
		private GameObject captureToggleDifferentValue;

		[Header("Factory")]
		[SerializeField]
		private GameObject factoryOptionsPanel;

		[SerializeField]
		private TextMeshProUGUI factionHeader;

		[SerializeField]
		private GameObject productionTypeDifferentWarning;

		[SerializeField]
		private TextMeshProUGUI productionTimeLabel;

		[SerializeField]
		private Slider productionTimeSlider;

		[SerializeField]
		private TMP_Dropdown productionUnitDropdown;

		private List<(SavedUnit saved, SavedBuilding.FactoryOptions options, Factory factory)> factoryList = new List<(SavedUnit, SavedBuilding.FactoryOptions, Factory)>();

		private DropdownKeyHelper<string> productionUnitHelper;

		private void Awake()
		{
			productionUnitHelper = new DropdownKeyHelper<string>(productionUnitDropdown);
		}

		public override void Cleanup()
		{
			targets.RemoveChanged(captureToggle);
		}

		protected override void SetupInner()
		{
			productionUnitDropdown.onValueChanged.AddListener(ProductionUnitDropdownChanged);
			productionTimeSlider.onValueChanged.AddListener(ProductionTimeSliderChanged);
			targets.SetupToggle(captureToggle, captureToggleDifferentValue, (SavedUnit x) => ref ((SavedBuilding)x).capturable);
			captureToggle.onValueChanged.AddListener(delegate
			{
				unitMenu.CheckAllOverrides();
			});
		}

		public override void OnTargetsChanged()
		{
			factoryList.Clear();
			foreach (SavedUnit target in targets.Targets)
			{
				if (target.Unit.TryGetComponent<Factory>(out var component))
				{
					SavedBuilding savedBuilding = (SavedBuilding)target;
					SavedBuilding savedBuilding2 = savedBuilding;
					if (savedBuilding2.factoryOptions == null)
					{
						savedBuilding2.factoryOptions = new SavedBuilding.FactoryOptions();
					}
					factoryList.Add((savedBuilding, savedBuilding.factoryOptions, component));
				}
			}
			factoryOptionsPanel.SetActive(factoryList.Count > 0);
			if (factoryList.Count <= 0)
			{
				return;
			}
			factionHeader.text = ((targets.Targets.Count == 1) ? "Factory" : ((factoryList.Count == 1) ? "1 Factory" : $"{factoryList.Count} Factories"));
			bool flag = true;
			bool flag2 = true;
			float productionTime = factoryList[0].options.productionTime;
			string productionType = factoryList[0].options.productionType;
			Factory.FactoryType factoryType = factoryList[0].factory.factoryType;
			for (int i = 1; i < factoryList.Count; i++)
			{
				(SavedUnit saved, SavedBuilding.FactoryOptions options, Factory factory) tuple = factoryList[i];
				SavedBuilding.FactoryOptions item = tuple.options;
				Factory item2 = tuple.factory;
				if (factoryType != item2.factoryType)
				{
					flag2 = false;
					break;
				}
				if (productionTime != item.productionTime || productionType != item.productionType)
				{
					flag = false;
					break;
				}
			}
			productionTypeDifferentWarning.SetActive(!flag2);
			if (flag2)
			{
				int valueWithoutNotify = -1;
				if (factoryType == Factory.FactoryType.Nukes)
				{
					GenerateWeaponList();
					if (flag)
					{
						valueWithoutNotify = ((productionType == "Nuclear Warhead") ? 1 : 0);
					}
				}
				else
				{
					GenerateUnitList();
					if (flag)
					{
						valueWithoutNotify = productionUnitHelper.IndexOf(productionType);
					}
				}
				productionUnitDropdown.SetValueWithoutNotify(valueWithoutNotify);
				productionUnitDropdown.RefreshShownValue();
				SetProductionTimeSlider(flag ? new float?(productionTime) : ((float?)null));
			}
			else
			{
				productionUnitDropdown.SetValueWithoutNotify(-1);
				productionUnitDropdown.RefreshShownValue();
				SetProductionTimeSlider(null);
			}
		}

		private void GenerateUnitList()
		{
			productionUnitHelper.Clear();
			productionUnitHelper.Add("none", "");
			foreach (UnitDefinition aircraftAndVehicle in Encyclopedia.i.GetAircraftAndVehicles())
			{
				if (!aircraftAndVehicle.NotAllowed(MissionManager.AllowEventContent))
				{
					productionUnitHelper.Add(aircraftAndVehicle.unitName, aircraftAndVehicle.jsonKey);
				}
			}
		}

		private void GenerateWeaponList()
		{
			productionUnitHelper.Clear();
			productionUnitHelper.Add("none", "");
			productionUnitHelper.Add("Nuclear Warhead", "Nuclear Warhead");
		}

		private void ProductionUnitDropdownChanged(int index)
		{
			foreach (var factory in factoryList)
			{
				SavedUnit item = factory.saved;
				unitMenu.CheckOverride(item);
			}
			string productionType = productionUnitHelper.keys[index];
			foreach (var factory2 in factoryList)
			{
				factory2.options.productionType = productionType;
			}
		}

		private void SetProductionTimeSlider(float? productionTime)
		{
			if (productionTime.HasValue)
			{
				productionTimeSlider.SetValueWithoutNotify(productionTime.Value);
				productionTimeLabel.text = $"{productionTime.Value:F0}s";
			}
			else
			{
				productionTimeSlider.SetValueWithoutNotify(0f);
				productionTimeLabel.text = "-";
			}
		}

		private void ProductionTimeSliderChanged(float value)
		{
			foreach (var factory in factoryList)
			{
				SavedUnit item = factory.saved;
				unitMenu.CheckOverride(item);
			}
			foreach (var factory2 in factoryList)
			{
				factory2.options.productionTime = value;
			}
			SetProductionTimeSlider(value);
		}
	}
}
