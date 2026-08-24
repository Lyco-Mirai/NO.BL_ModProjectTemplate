using System.Collections.Generic;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetListSelector : SceneSingleton<TargetListSelector>
{
	public MFDScreen screen;

	[SerializeField]
	private List<TargetListSelector_UnitItem> listItems = new List<TargetListSelector_UnitItem>();

	[SerializeField]
	private GameObject unitPrefab;

	[SerializeField]
	private GameObject buttonPrefab;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private Transform buttonContainer;

	[SerializeField]
	private Image resetButtonImage;

	[SerializeField]
	private TextMeshProUGUI resetButtonLabel;

	[SerializeField]
	private Image clearButtonImage;

	[SerializeField]
	private TextMeshProUGUI clearButtonLabel;

	public TargetListSelector_ToggleButton toggleFollowHUD;

	public TargetListSelector_ToggleButton toggleLaser;

	public List<TargetListSelector_ToggleButton> toggleFactionItems;

	public List<TargetListSelector_ToggleButton> toggleUnitTypesItems;

	public List<TargetListSelector_ToggleButton> toggleVehicleTypesItems;

	private Color resetColor = Color.yellow;

	private Color clearColor = Color.red;

	private float lastRefresh;

	private float refreshDelay = 1f;

	private bool needUpdateIcon;

	private void Start()
	{
		toggleFollowHUD.Set(arg: false);
		toggleLaser.Set(arg: false);
		SceneSingleton<DynamicMap>.i.onUnitSelected += AddItem;
		SceneSingleton<DynamicMap>.i.onUnitDeselected += RemoveItem;
		SceneSingleton<DynamicMap>.i.onAllDeselected += RemoveAll;
		SceneSingleton<HUDOptions>.i.OnApplyOptions += SetFilters;
		toggleFollowHUD.OnToggle += OnToggleFollowHUD;
		TargetListSelector_OnThemeGroupChanged();
		ThemeManager.ThemeGroupChanged += TargetListSelector_OnThemeGroupChanged;
		resetButtonImage.color = Color.gray;
		resetButtonLabel.color = Color.gray;
		clearButtonImage.color = Color.gray;
		clearButtonLabel.color = Color.gray;
		SetupList();
		needUpdateIcon = true;
	}

	private void OnDestroy()
	{
		if (SceneSingleton<DynamicMap>.i != null)
		{
			SceneSingleton<DynamicMap>.i.onUnitSelected -= AddItem;
			SceneSingleton<DynamicMap>.i.onUnitDeselected -= RemoveItem;
			SceneSingleton<DynamicMap>.i.onAllDeselected -= RemoveAll;
		}
		if (SceneSingleton<HUDOptions>.i != null)
		{
			SceneSingleton<HUDOptions>.i.OnApplyOptions -= SetFilters;
		}
		ThemeManager.ThemeGroupChanged -= TargetListSelector_OnThemeGroupChanged;
	}

	private void SetupList()
	{
		if (toggleVehicleTypesItems.Count > 0)
		{
			toggleVehicleTypesItems.Clear();
			foreach (Transform item in buttonContainer)
			{
				Object.Destroy(item.gameObject);
			}
		}
		for (int i = 0; i < Encyclopedia.i.vehicleTypes.Count; i++)
		{
			GameObject obj = Object.Instantiate(buttonPrefab, buttonContainer);
			obj.name = "Item_" + Encyclopedia.i.vehicleTypes[i].typeName;
			TargetListSelector_ToggleButton component = obj.GetComponent<TargetListSelector_ToggleButton>();
			component.SetTextIcon(Encyclopedia.i.vehicleTypes[i].typeName, Encyclopedia.i.vehicleTypes[i].typeSprite);
			for (int j = 0; j < Encyclopedia.i.vehicles.Count; j++)
			{
				if (Encyclopedia.i.vehicles[j].vehicleType.ToString() == Encyclopedia.i.vehicleTypes[i].typeName)
				{
					component.AddDefinition(Encyclopedia.i.vehicles[j]);
				}
			}
			toggleVehicleTypesItems.Add(component);
		}
	}

	private void Update()
	{
		if (needUpdateIcon && Time.timeSinceLevelLoad > lastRefresh + refreshDelay)
		{
			if (SceneSingleton<CombatHUD>.i.aircraft == null && toggleFollowHUD.status)
			{
				toggleFollowHUD.Set(arg: false);
			}
			lastRefresh = Time.timeSinceLevelLoad;
			CheckAllExclusions();
			needUpdateIcon = false;
		}
	}

	public void AddItem(Unit u)
	{
		if (listItems.Find((TargetListSelector_UnitItem item) => item.unit == u) == null)
		{
			TargetListSelector_UnitItem component = Object.Instantiate(unitPrefab, container).GetComponent<TargetListSelector_UnitItem>();
			component.SetUnit(u);
			component.selector = this;
			u.onDisableUnit += RemoveItem;
			listItems.Add(component);
			clearButtonImage.color = clearColor;
			clearButtonLabel.color = clearColor;
		}
	}

	public void RemoveItem(Unit u)
	{
		if (listItems.Find((TargetListSelector_UnitItem item) => item.unit == u) != null)
		{
			TargetListSelector_UnitItem targetListSelector_UnitItem = listItems.Find((TargetListSelector_UnitItem item) => item.unit == u);
			listItems.Remove(targetListSelector_UnitItem);
			u.onDisableUnit -= RemoveItem;
			Object.Destroy(targetListSelector_UnitItem.gameObject);
		}
		if (listItems.Count == 0)
		{
			clearButtonImage.color = Color.gray;
			clearButtonLabel.color = Color.gray;
		}
	}

	public void RemoveAll()
	{
		listItems.Clear();
		if (container.childCount <= 0)
		{
			return;
		}
		foreach (Transform item in container)
		{
			Object.Destroy(item.gameObject);
		}
	}

	public void CheckAllExclusions()
	{
		List<Unit> list = new List<Unit>();
		foreach (MapIcon selectedIcon in SceneSingleton<DynamicMap>.i.selectedIcons)
		{
			if (selectedIcon is UnitMapIcon unitMapIcon && CheckExclusions(unitMapIcon.unit))
			{
				list.Add(unitMapIcon.unit);
			}
		}
		foreach (Unit item in list)
		{
			ForceDeselect(item);
		}
		list.Clear();
		foreach (MapIcon mapIcon in SceneSingleton<DynamicMap>.i.mapIcons)
		{
			if (mapIcon is UnitMapIcon unitMapIcon2)
			{
				unitMapIcon2.UnitMapIcon_UpdateColor();
			}
		}
	}

	public void ForceDeselect(Unit unit)
	{
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			SceneSingleton<CombatHUD>.i.DeSelectUnit(unit);
		}
		else
		{
			SceneSingleton<DynamicMap>.i.DeselectIcon(unit);
		}
	}

	public bool CheckExclusions(Unit u)
	{
		for (int i = 0; i < toggleFactionItems.Count; i++)
		{
			if (toggleFactionItems[i].CheckFactions(u))
			{
				return true;
			}
		}
		for (int j = 0; j < toggleUnitTypesItems.Count; j++)
		{
			if (toggleUnitTypesItems[j].CheckUnitTypes(u))
			{
				return true;
			}
		}
		for (int k = 0; k < toggleVehicleTypesItems.Count; k++)
		{
			if (toggleVehicleTypesItems[k].CheckDefinitions(u))
			{
				return true;
			}
		}
		if (toggleLaser.status && SceneSingleton<DynamicMap>.i.HQ != null && !SceneSingleton<DynamicMap>.i.HQ.IsTargetLased(u))
		{
			return true;
		}
		return false;
	}

	public void SetOnlyItem(TargetListSelector_ToggleButton item)
	{
		if (toggleFactionItems.Contains(item))
		{
			foreach (TargetListSelector_ToggleButton toggleFactionItem in toggleFactionItems)
			{
				toggleFactionItem.Set(arg: false);
			}
		}
		else if (toggleUnitTypesItems.Contains(item))
		{
			foreach (TargetListSelector_ToggleButton toggleUnitTypesItem in toggleUnitTypesItems)
			{
				toggleUnitTypesItem.Set(arg: false);
			}
		}
		else if (toggleVehicleTypesItems.Contains(item))
		{
			foreach (TargetListSelector_ToggleButton toggleVehicleTypesItem in toggleVehicleTypesItems)
			{
				toggleVehicleTypesItem.Set(arg: false);
			}
			foreach (TargetListSelector_ToggleButton toggleUnitTypesItem2 in toggleUnitTypesItems)
			{
				toggleUnitTypesItem2.Set(item.listDefinitions[0].GetType() == toggleUnitTypesItem2.listUnitTypes[0].GetType());
			}
		}
		item.Set(arg: true);
	}

	public void DeselectAll()
	{
		if (SceneSingleton<CombatHUD>.i.aircraft != null)
		{
			SceneSingleton<CombatHUD>.i.DeselectAll();
		}
		else
		{
			SceneSingleton<DynamicMap>.i.UnselectAll();
		}
		clearButtonImage.color = Color.gray;
		clearButtonLabel.color = Color.gray;
	}

	public void ResetFilters()
	{
		toggleLaser.Set(arg: false);
		foreach (TargetListSelector_ToggleButton toggleFactionItem in toggleFactionItems)
		{
			toggleFactionItem.Set(arg: true);
		}
		foreach (TargetListSelector_ToggleButton toggleUnitTypesItem in toggleUnitTypesItems)
		{
			toggleUnitTypesItem.Set(arg: true);
		}
		foreach (TargetListSelector_ToggleButton toggleVehicleTypesItem in toggleVehicleTypesItems)
		{
			toggleVehicleTypesItem.Set(arg: true);
		}
		resetButtonImage.color = Color.gray;
		resetButtonLabel.color = Color.gray;
	}

	public void SetFilters()
	{
		if (!toggleFollowHUD.status)
		{
			return;
		}
		HUDOptions.HUDMode currentMode = SceneSingleton<HUDOptions>.i.currentMode;
		List<HUDOptions_Priorities.Setting> listCategories = SceneSingleton<HUDOptions>.i.listModes[(int)currentMode].settings.listCategories;
		List<HUDOptions_Priorities.Setting> listVehicles = SceneSingleton<HUDOptions>.i.listModes[(int)currentMode].settings.listVehicles;
		for (int i = 0; i < listCategories.Count; i++)
		{
			bool typePriority = listCategories[i].typePriority;
			if (i < 2)
			{
				toggleFactionItems[i].Set(typePriority);
			}
			else
			{
				toggleUnitTypesItems[i - 2].Set(typePriority);
			}
		}
		for (int j = 0; j < listVehicles.Count; j++)
		{
			toggleVehicleTypesItems[j].Set(listVehicles[j].typePriority);
		}
	}

	public void OnToggleFollowHUD()
	{
		if (toggleFollowHUD.status)
		{
			SetFilters();
		}
		else
		{
			ResetFilters();
		}
	}

	public void NeedUpdateIcons()
	{
		resetButtonImage.color = resetColor;
		resetButtonLabel.color = resetColor;
		needUpdateIcon = true;
	}

	private void TargetListSelector_OnThemeGroupChanged()
	{
		resetColor = ThemeManager.Active.ColorTheme.Warning;
		clearColor = ThemeManager.Active.ColorTheme.Alert;
		if (clearButtonImage.color != Color.gray || clearButtonLabel.color != Color.gray)
		{
			clearButtonImage.color = clearColor;
			clearButtonLabel.color = clearColor;
		}
	}
}
