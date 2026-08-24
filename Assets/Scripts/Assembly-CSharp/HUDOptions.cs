using System;
using System.Collections.Generic;
using UnityEngine;

public class HUDOptions : SceneSingleton<HUDOptions>
{
	public enum HUDMode
	{
		NAV = 0,
		GUN = 1,
		A2A = 2,
		A2G = 3,
		EW = 4,
		LOG = 5
	}

	public HUDMode currentMode;

	public MFDScreen screen;

	public List<HUDOptions_ToggleButton> listModes = new List<HUDOptions_ToggleButton>();

	public List<HUDOptions_ToggleButton> listVehicleTypes = new List<HUDOptions_ToggleButton>();

	public List<HUDOptions_ToggleButton> listBuildingTypes = new List<HUDOptions_ToggleButton>();

	public List<HUDOptions_Category> listCategories = new List<HUDOptions_Category>();

	private float lastRefresh;

	private float refreshDelay = 1f;

	private bool needUpdateIcon;

	public GameObject buttonPrefab;

	public Transform vehiclesButtonContainer;

	public Transform buidlingsButtonContainer;

	private HUDOptions_Priorities currentSetting;

	public event Action OnApplyOptions;

	private void Start()
	{
		SetupList("Buildings", Encyclopedia.i.buildingTypes, buidlingsButtonContainer, listBuildingTypes);
		SetupList("Vehicles", Encyclopedia.i.vehicleTypes, vehiclesButtonContainer, listVehicleTypes);
		LoadValues();
		needUpdateIcon = true;
	}

	private void SetupList(string typeName, List<Encyclopedia.UnitType> listTypes, Transform container, List<HUDOptions_ToggleButton> listButtons)
	{
		if (listButtons.Count > 0)
		{
			listButtons.Clear();
			foreach (Transform item in container)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		for (int i = 0; i < listTypes.Count; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(buttonPrefab, container);
			obj.name = "Item_" + listTypes[i].typeName;
			HUDOptions_ToggleButton component = obj.GetComponent<HUDOptions_ToggleButton>();
			component.SetTextIcon(listTypes[i].typeName, listTypes[i].typeSprite);
			List<UnitDefinition> list = new List<UnitDefinition>();
			if (typeName == "Vehicles")
			{
				list.AddRange(Encyclopedia.i.vehicles);
			}
			else if (typeName == "Buildings")
			{
				list.AddRange(Encyclopedia.i.buildings);
			}
			for (int j = 0; j < list.Count; j++)
			{
				string text = "";
				if (typeName == "Vehicles")
				{
					text = (list[j] as VehicleDefinition).vehicleType.ToString();
				}
				else if (typeName == "Buildings")
				{
					text = (list[j] as BuildingDefinition).buildingType.ToString();
				}
				if (text == listTypes[i].typeName)
				{
					component.AddDefinition(list[j]);
				}
			}
			listButtons.Add(component);
		}
	}

	public void ApplyHUDSettings()
	{
		this.OnApplyOptions?.Invoke();
	}

	public void AutomaticToggle(WeaponStation newWeaponStation, bool safety)
	{
		if (newWeaponStation == null)
		{
			ToggleButtons(listModes[0]);
			return;
		}
		WeaponInfo weaponInfo = newWeaponStation.WeaponInfo;
		if (safety)
		{
			ToggleButtons(listModes[0]);
			currentMode = HUDMode.NAV;
		}
		else if (weaponInfo.effectiveness.antiAir == 0f && weaponInfo.effectiveness.antiSurface == 0f && weaponInfo.effectiveness.antiMissile == 0f && weaponInfo.effectiveness.antiRadar == 0f)
		{
			ToggleButtons(listModes[5]);
			currentMode = HUDMode.LOG;
		}
		else if (weaponInfo.gun)
		{
			ToggleButtons(listModes[1]);
			currentMode = HUDMode.GUN;
		}
		else if (weaponInfo.effectiveness.antiAir > weaponInfo.effectiveness.antiSurface)
		{
			ToggleButtons(listModes[2]);
			currentMode = HUDMode.A2A;
		}
		else if (weaponInfo.effectiveness.antiSurface > weaponInfo.effectiveness.antiAir)
		{
			ToggleButtons(listModes[3]);
			currentMode = HUDMode.A2G;
		}
		else if (weaponInfo.effectiveness.antiRadar > weaponInfo.effectiveness.antiSurface)
		{
			ToggleButtons(listModes[4]);
			currentMode = HUDMode.EW;
		}
		else
		{
			ToggleButtons(listModes[0]);
			currentMode = HUDMode.NAV;
		}
	}

	public void ToggleButtons(HUDOptions_ToggleButton button)
	{
		if (listModes.Contains(button))
		{
			for (int i = 0; i < listModes.Count; i++)
			{
				if (listModes[i] != button)
				{
					listModes[i].Set(arg: false);
				}
				else
				{
					listModes[i].Set(arg: true);
				}
			}
		}
		else if (listVehicleTypes.Contains(button))
		{
			for (int j = 0; j < listVehicleTypes.Count; j++)
			{
				if (listVehicleTypes[j] != button)
				{
					listVehicleTypes[j].Set(arg: false);
				}
				else
				{
					listVehicleTypes[j].Set(arg: true);
				}
			}
		}
		else if (listBuildingTypes.Contains(button))
		{
			for (int k = 0; k < listBuildingTypes.Count; k++)
			{
				if (listBuildingTypes[k] != button)
				{
					listBuildingTypes[k].Set(arg: false);
				}
				else
				{
					listBuildingTypes[k].Set(arg: true);
				}
			}
		}
		NeedUpdateIcons();
	}

	public void ApplySettings(HUDOptions_Priorities settings)
	{
		if (settings != null)
		{
			currentSetting = settings;
			for (int i = 0; i < listCategories.Count; i++)
			{
				listCategories[i].Set(settings.listCategories[i].typePriority);
			}
			for (int j = 0; j < listVehicleTypes.Count; j++)
			{
				listVehicleTypes[j].Set(settings.listVehicles[j].typePriority);
			}
			for (int k = 0; k < listBuildingTypes.Count; k++)
			{
				listBuildingTypes[k].Set(settings.listBuildings[k].typePriority);
			}
		}
		NeedUpdateIcons();
	}

	public void SaveSettings()
	{
		if (currentSetting != null)
		{
			for (int i = 0; i < listCategories.Count; i++)
			{
				currentSetting.listCategories[i].typePriority = listCategories[i].maximized;
			}
			for (int j = 0; j < listVehicleTypes.Count; j++)
			{
				currentSetting.listVehicles[j].typePriority = listVehicleTypes[j].status;
			}
			for (int k = 0; k < listBuildingTypes.Count; k++)
			{
				currentSetting.listBuildings[k].typePriority = listBuildingTypes[k].status;
			}
			currentSetting.SaveToJson();
		}
		NeedUpdateIcons();
	}

	private void Update()
	{
		if (needUpdateIcon && Time.timeSinceLevelLoad > lastRefresh + refreshDelay)
		{
			lastRefresh = Time.timeSinceLevelLoad;
			this.OnApplyOptions?.Invoke();
			needUpdateIcon = false;
		}
	}

	public void NeedUpdateIcons()
	{
		needUpdateIcon = true;
	}

	public float CheckMaximizeIcon(Unit unit)
	{
		bool flag = true;
		float num = 15000f;
		for (int i = 0; i < listCategories.Count; i++)
		{
			if (i < 2 && listCategories[i].CheckFaction(unit.NetworkHQ))
			{
				flag = listCategories[i].maximized;
				num *= (flag ? 10f : 0f);
			}
			else if (i >= 2 && listCategories[i].CheckType(unit.definition))
			{
				flag = listCategories[i].maximized;
				num *= (flag ? 1f : 0f);
			}
			if (!flag)
			{
				break;
			}
		}
		if (unit is GroundVehicle groundVehicle)
		{
			for (int j = 0; j < listVehicleTypes.Count; j++)
			{
				if (listVehicleTypes[j].CheckDefinition(groundVehicle.definition) && !listVehicleTypes[j].status)
				{
					num *= 0f;
				}
			}
		}
		else if (unit is Building building)
		{
			for (int k = 0; k < listBuildingTypes.Count; k++)
			{
				if (listBuildingTypes[k].CheckDefinition(building.definition) && !listBuildingTypes[k].status)
				{
					num *= 0f;
				}
			}
		}
		return num;
	}

	public void LoadValues()
	{
		foreach (HUDOptions_ToggleButton listMode in listModes)
		{
			listMode.LoadValues();
		}
	}

	public void SaveValues()
	{
		foreach (HUDOptions_ToggleButton listMode in listModes)
		{
			listMode.SaveValues();
		}
	}
}
