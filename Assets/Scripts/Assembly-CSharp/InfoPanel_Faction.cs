using System.Collections.Generic;
using System.Linq;
using NuclearOption.Networking;
using NuclearOption.UIStyleSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel_Faction : MonoBehaviour
{
	public enum SelectFaction
	{
		Current = 0,
		Other = 1
	}

	public enum DisplayType
	{
		Forces = 0,
		Players = 1,
		Airbases = 2,
		Reserves = 3,
		Losses = 4,
		Value = 5,
		Manpower = 6
	}

	public MFDScreen screen;

	public SelectFaction selectFaction;

	public DisplayType currentDisplay;

	public bool Initialized;

	[Header("Faction")]
	[SerializeField]
	private TextMeshProUGUI factionName;

	[SerializeField]
	private Image factionImage;

	[SerializeField]
	private TextMeshProUGUI funds;

	[SerializeField]
	private TextMeshProUGUI score;

	[SerializeField]
	private TextMeshProUGUI warheads;

	[Header("Button")]
	[SerializeField]
	private TMP_Dropdown currentDisplayDropdown;

	[Header("Airbases")]
	[SerializeField]
	private GameObject airbase_container;

	[SerializeField]
	private InfoPanel_ItemPrefab airbases;

	[SerializeField]
	private Transform airbases_list;

	public List<Airbase> listAirbases;

	[Header("Value items")]
	[SerializeField]
	private GameObject total_value_container;

	[SerializeField]
	private Transform total_value_parent;

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_total_value = new List<InfoPanel_ItemPrefab>();

	[Header("Buildings")]
	[SerializeField]
	private GameObject buildings_container;

	[SerializeField]
	private Transform buildings_item_parent;

	[SerializeField]
	private Transform buildings_value_parent;

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_buildings = new List<InfoPanel_ItemPrefab>();

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_buildings_value = new List<InfoPanel_ItemPrefab>();

	[Header("Vehicles")]
	[SerializeField]
	private GameObject vehicles_container;

	[SerializeField]
	private Transform vehicles_item_parent;

	[SerializeField]
	private Transform vehicles_value_parent;

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_vehicles = new List<InfoPanel_ItemPrefab>();

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_vehicles_value = new List<InfoPanel_ItemPrefab>();

	[Header("Ships")]
	[SerializeField]
	private GameObject ships_container;

	[SerializeField]
	private Transform ships_item_parent;

	[SerializeField]
	private Transform ships_value_parent;

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_ships = new List<InfoPanel_ItemPrefab>();

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_ships_value = new List<InfoPanel_ItemPrefab>();

	[Header("Aircraft")]
	[SerializeField]
	private GameObject aircraft_container;

	[SerializeField]
	private Transform aircraft_item_parent;

	[SerializeField]
	private Transform aircraft_value_parent;

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_aircraft = new List<InfoPanel_ItemPrefab>();

	[SerializeField]
	private List<InfoPanel_ItemPrefab> items_aircraft_value = new List<InfoPanel_ItemPrefab>();

	[Header("Players")]
	[SerializeField]
	private GameObject players_container;

	[SerializeField]
	private InfoPanel_ItemPrefab players;

	[SerializeField]
	private Transform players_list;

	public List<Player> listPlayers;

	private float refreshRate = 1f;

	private float lastRefresh;

	private Gradient fundsGradient;

	[Header("Prefabs")]
	[SerializeField]
	private GameObject playerEntry_prefab;

	[SerializeField]
	private GameObject airbaseEntry_prefab;

	[SerializeField]
	private GameObject airbaseHeader_prefab;

	[SerializeField]
	private GameObject carrierEntry_prefab;

	[SerializeField]
	private GameObject itemEntry_prefab;

	[SerializeField]
	private GameObject spacer_prefab;

	public FactionHQ factionHQ { get; private set; }

	private void Start()
	{
		fundsGradient = ThemeManager.Active.ColorTheme.Gradient();
		score.color = ThemeManager.Active.ColorTheme.AllClear;
		warheads.color = ThemeManager.Active.ColorTheme.AllClear;
		ThemeManager.ThemeGroupChanged += InfoPanel_Faction_OnThemeGroupChanged;
		SceneSingleton<DynamicMap>.i.onMapGenerated += Initialize;
		Initialized = false;
	}

	private void OnDestroy()
	{
		ThemeManager.ThemeGroupChanged -= InfoPanel_Faction_OnThemeGroupChanged;
	}

	private void Update()
	{
		if (Initialized && screen.isActive && Time.timeSinceLevelLoad > lastRefresh + refreshRate)
		{
			funds.text = UnitConverter.ValueReading(factionHQ.factionFunds);
			funds.color = fundsGradient.Evaluate(factionHQ.factionFunds / 100f);
			score.text = factionHQ.factionScore.ToString("N1") ?? "";
			warheads.text = factionHQ.GetWarheadStockpile().ToString() ?? "";
			airbases.Refresh(factionHQ.GetAirbases().Count(), isLoss: false, isValue: false);
			switch (currentDisplay)
			{
			case DisplayType.Forces:
				UpdateDisplayForces();
				break;
			case DisplayType.Players:
				UpdateDisplayPlayers();
				break;
			case DisplayType.Airbases:
				UpdateAirbaseList();
				break;
			case DisplayType.Reserves:
				UpdateDisplayReserves();
				break;
			case DisplayType.Losses:
				UpdateDisplayLosses();
				break;
			case DisplayType.Value:
				UpdateDisplayValue();
				break;
			case DisplayType.Manpower:
				UpdateDisplayManpower();
				break;
			}
			lastRefresh = Time.timeSinceLevelLoad;
		}
	}

	private void AddAirbase(Airbase airbase)
	{
		if (!listAirbases.Contains(airbase))
		{
			AddAirbaseEntry(airbase);
		}
	}

	private void UpdateAirbaseList()
	{
		List<Airbase> list = factionHQ.GetAirbases().ToList();
		if (list.Count == 0 || list.Count == listAirbases.Count)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (!listAirbases.Contains(list[i]))
			{
				AddAirbase(list[i]);
			}
		}
	}

	public void Initialize()
	{
		ResetLists();
		listPlayers = new List<Player>();
		listAirbases = new List<Airbase>();
		string text = "Boscali";
		string text2 = "Primeva";
		if (SceneSingleton<DynamicMap>.i.HQ != null)
		{
			text = SceneSingleton<DynamicMap>.i.HQ.faction.name;
			text2 = ((!(text == "Boscali")) ? "Boscali" : "Primeva");
			UpdateAirbaseList();
		}
		if (selectFaction == SelectFaction.Current)
		{
			factionHQ = FactionRegistry.HqFromName(text);
		}
		else
		{
			factionHQ = FactionRegistry.HqFromName(text2);
		}
		screen.shortName = factionHQ.faction.factionTag;
		screen.virtualMFD.SetupButtons();
		factionHQ.onAirbaseAdded += AddAirbase;
		factionImage.sprite = factionHQ.faction.factionColorLogo;
		factionName.text = factionHQ.faction.factionName.ToUpper();
		lastRefresh = Time.timeSinceLevelLoad;
		if (!Initialized)
		{
			SetupList("Total_Value");
			SetupList("Buildings");
			SetupList("Vehicles");
			SetupList("Ships");
			SetupList("Aircraft");
			Initialized = true;
		}
	}

	private void SetupList(string typeName)
	{
		GameObject gameObject = null;
		Transform parent = null;
		Transform transform = null;
		List<Encyclopedia.UnitType> list = new List<Encyclopedia.UnitType>();
		List<UnitDefinition> list2 = new List<UnitDefinition>();
		List<InfoPanel_ItemPrefab> list3 = new List<InfoPanel_ItemPrefab>();
		List<InfoPanel_ItemPrefab> list4 = new List<InfoPanel_ItemPrefab>();
		switch (typeName)
		{
		case "Total_Value":
			gameObject = total_value_container;
			transform = total_value_parent;
			list3 = null;
			list4 = items_total_value;
			list = null;
			list2 = null;
			break;
		case "Buildings":
			gameObject = buildings_container;
			parent = buildings_item_parent;
			transform = buildings_value_parent;
			list3 = items_buildings;
			list4 = items_buildings_value;
			list = Encyclopedia.i.buildingTypes;
			list2.AddRange(Encyclopedia.i.buildings);
			break;
		case "Vehicles":
			gameObject = vehicles_container;
			parent = vehicles_item_parent;
			transform = vehicles_value_parent;
			list3 = items_vehicles;
			list4 = items_vehicles_value;
			list = Encyclopedia.i.vehicleTypes;
			list2.AddRange(Encyclopedia.i.vehicles);
			break;
		case "Ships":
			gameObject = ships_container;
			parent = ships_item_parent;
			transform = ships_value_parent;
			list3 = items_ships;
			list4 = items_ships_value;
			list = Encyclopedia.i.shipTypes;
			list2.AddRange(Encyclopedia.i.ships);
			break;
		case "Aircraft":
			gameObject = aircraft_container;
			parent = aircraft_item_parent;
			transform = aircraft_value_parent;
			list3 = items_aircraft;
			list4 = items_aircraft_value;
			list = null;
			foreach (AircraftDefinition item in Encyclopedia.i.aircraft)
			{
				if (item.IsAllowed(MissionManager.AllowEventContent))
				{
					list2.Add(item);
				}
			}
			break;
		}
		GameObject obj = Object.Instantiate(itemEntry_prefab, gameObject.transform);
		obj.name = "Header_" + typeName;
		InfoPanel_ItemPrefab component = obj.GetComponent<InfoPanel_ItemPrefab>();
		component.SetFaction(this);
		component.SetTextIcon(typeName.ToUpper(), null);
		obj.transform.SetAsFirstSibling();
		list3?.Add(component);
		list4?.Add(component);
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				GameObject obj2 = Object.Instantiate(itemEntry_prefab, parent);
				obj2.name = "Item_" + list[i].typeName;
				InfoPanel_ItemPrefab component2 = obj2.GetComponent<InfoPanel_ItemPrefab>();
				component2.SetFaction(this);
				component2.SetTextIcon((typeName != "Ships") ? list[i].typeName : "", (typeName == "Ships") ? list[i].typeSprite : null, fit: true);
				for (int j = 0; j < list2.Count; j++)
				{
					string text = "";
					switch (typeName)
					{
					case "Buildings":
						text = (list2[j] as BuildingDefinition).buildingType.ToString();
						break;
					case "Vehicles":
						text = (list2[j] as VehicleDefinition).vehicleType.ToString();
						break;
					case "Ships":
						text = (list2[j] as ShipDefinition).shipType.ToString();
						break;
					}
					if (text == list[i].typeName)
					{
						component2.AddDefinition(list2[j]);
					}
				}
				list3.Add(component2);
			}
		}
		else if (list2 != null)
		{
			for (int k = 0; k < list2.Count; k++)
			{
				GameObject obj3 = Object.Instantiate(itemEntry_prefab, aircraft_item_parent);
				obj3.name = "Item_" + list2[k].code;
				InfoPanel_ItemPrefab component3 = obj3.GetComponent<InfoPanel_ItemPrefab>();
				component3.SetFaction(this);
				component3.SetTextIcon("", list2[k].mapIcon);
				component3.AddDefinition(list2[k]);
				items_aircraft.Add(component3);
			}
		}
		for (int l = 0; l < 3; l++)
		{
			GameObject gameObject2 = Object.Instantiate(itemEntry_prefab, transform);
			InfoPanel_ItemPrefab component4 = gameObject2.GetComponent<InfoPanel_ItemPrefab>();
			switch (l)
			{
			case 0:
				gameObject2.name = "Item_" + typeName + "_Current";
				component4.SetTextIcon("Current", null);
				break;
			case 1:
				gameObject2.name = "Item_" + typeName + "_Spent";
				component4.SetTextIcon("Spent", null);
				break;
			case 2:
				gameObject2.name = "Item_" + typeName + "_Losses";
				component4.SetTextIcon("Losses", null);
				break;
			}
			component4.SetFaction(this);
			list4.Add(component4);
		}
		transform.gameObject.SetActive(value: false);
	}

	public void DeselectPlayers()
	{
		foreach (Transform item in players_list)
		{
			item.GetComponent<InfoPanel_PlayerEntry>().Deselect();
		}
	}

	public bool CheckIfPlayerSelected()
	{
		bool result = false;
		foreach (Transform item in players_list)
		{
			if (item.GetComponent<InfoPanel_PlayerEntry>().IsSelected())
			{
				result = true;
			}
		}
		return result;
	}

	public void AddPlayerEntry(Player p)
	{
		Object.Instantiate(playerEntry_prefab, players_list).GetComponent<InfoPanel_PlayerEntry>().SetPlayer(p, this);
	}

	public void RemoveNullPlayers()
	{
		for (int i = 0; i < listPlayers.Count; i++)
		{
			if (listPlayers[i] == null)
			{
				listPlayers.RemoveAt(i);
			}
		}
	}

	public void AddAirbaseEntry(Airbase a)
	{
		if (airbases_list.childCount == 0)
		{
			Object.Instantiate(airbaseHeader_prefab, airbases_list);
		}
		a.TryGetAttachedUnit(out var attachedUnit);
		GameObject gameObject = ((!(attachedUnit != null)) ? Object.Instantiate(airbaseEntry_prefab, airbases_list) : Object.Instantiate(carrierEntry_prefab, airbases_list));
		gameObject.GetComponent<InfoPanel_AirbaseEntry>().SetAirbase(a, this);
		listAirbases.Add(a);
	}

	public void SetDisplayForces()
	{
		currentDisplay = DisplayType.Forces;
		players_container.SetActive(value: false);
		airbase_container.SetActive(value: false);
		total_value_container.SetActive(value: false);
		total_value_parent.gameObject.SetActive(value: false);
		buildings_container.SetActive(value: true);
		buildings_item_parent.gameObject.SetActive(value: true);
		buildings_value_parent.gameObject.SetActive(value: false);
		vehicles_container.SetActive(value: true);
		vehicles_item_parent.gameObject.SetActive(value: true);
		vehicles_value_parent.gameObject.SetActive(value: false);
		ships_container.SetActive(value: true);
		ships_item_parent.gameObject.SetActive(value: true);
		ships_value_parent.gameObject.SetActive(value: false);
		aircraft_container.SetActive(value: true);
		aircraft_item_parent.gameObject.SetActive(value: true);
		aircraft_value_parent.gameObject.SetActive(value: false);
	}

	public void UpdateDisplayForces()
	{
		for (int i = 0; i < items_buildings.Count; i++)
		{
			if (i == 0)
			{
				items_buildings[i].Refresh((int)factionHQ.missionStatsTracker.units.buildings.current, isLoss: false, isValue: false);
			}
			else
			{
				items_buildings[i].RefreshDefinition(currentDisplay);
			}
		}
		for (int j = 0; j < items_vehicles.Count; j++)
		{
			if (j == 0)
			{
				items_vehicles[j].Refresh((int)factionHQ.missionStatsTracker.units.vehicles.current, isLoss: false, isValue: false);
			}
			else
			{
				items_vehicles[j].RefreshDefinition(currentDisplay);
			}
		}
		for (int k = 0; k < items_ships.Count; k++)
		{
			if (k == 0)
			{
				items_ships[k].Refresh((int)factionHQ.missionStatsTracker.units.ships.current, isLoss: false, isValue: false);
			}
			else
			{
				items_ships[k].RefreshDefinition(currentDisplay);
			}
		}
		for (int l = 0; l < items_aircraft.Count; l++)
		{
			if (l == 0)
			{
				items_aircraft[l].Refresh((int)factionHQ.missionStatsTracker.units.aircraft.current, isLoss: false, isValue: false);
			}
			else
			{
				items_aircraft[l].RefreshDefinition(currentDisplay);
			}
		}
	}

	public void SetDisplayReserves()
	{
		currentDisplay = DisplayType.Reserves;
		players_container.SetActive(value: false);
		airbase_container.SetActive(value: false);
		total_value_container.SetActive(value: false);
		total_value_parent.gameObject.SetActive(value: false);
		buildings_container.SetActive(value: true);
		buildings_item_parent.gameObject.SetActive(value: true);
		buildings_value_parent.gameObject.SetActive(value: false);
		vehicles_container.SetActive(value: true);
		vehicles_item_parent.gameObject.SetActive(value: true);
		vehicles_value_parent.gameObject.SetActive(value: false);
		ships_container.SetActive(value: true);
		ships_item_parent.gameObject.SetActive(value: true);
		ships_value_parent.gameObject.SetActive(value: false);
		aircraft_container.SetActive(value: true);
		aircraft_item_parent.gameObject.SetActive(value: true);
		aircraft_value_parent.gameObject.SetActive(value: false);
	}

	public void UpdateDisplayReserves()
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < items_ships.Count; i++)
		{
			items_ships[i].Refresh(0f, isLoss: false, isValue: false);
		}
		for (int j = 0; j < items_buildings.Count; j++)
		{
			items_buildings[j].Refresh(0f, isLoss: false, isValue: false);
		}
		for (int k = 0; k < items_vehicles.Count; k++)
		{
			if (k > 0)
			{
				items_vehicles[k].RefreshDefinition(currentDisplay);
				num += items_vehicles[k].GetValue();
			}
		}
		items_vehicles[0].Refresh(num, isLoss: false, isValue: false);
		for (int l = 0; l < items_aircraft.Count; l++)
		{
			if (l > 0)
			{
				items_aircraft[l].RefreshDefinition(currentDisplay);
				num2 += items_aircraft[l].GetValue();
			}
		}
		items_aircraft[0].Refresh(num2, isLoss: false, isValue: false);
	}

	public void SetDisplayLosses()
	{
		currentDisplay = DisplayType.Losses;
		players_container.SetActive(value: false);
		airbase_container.SetActive(value: false);
		total_value_container.SetActive(value: false);
		total_value_parent.gameObject.SetActive(value: false);
		buildings_container.SetActive(value: true);
		buildings_item_parent.gameObject.SetActive(value: true);
		buildings_value_parent.gameObject.SetActive(value: false);
		vehicles_container.SetActive(value: true);
		vehicles_item_parent.gameObject.SetActive(value: true);
		vehicles_value_parent.gameObject.SetActive(value: false);
		ships_container.SetActive(value: true);
		ships_item_parent.gameObject.SetActive(value: true);
		ships_value_parent.gameObject.SetActive(value: false);
		aircraft_container.SetActive(value: true);
		aircraft_item_parent.gameObject.SetActive(value: true);
		aircraft_value_parent.gameObject.SetActive(value: false);
	}

	public void UpdateDisplayLosses()
	{
		for (int i = 0; i < items_buildings.Count; i++)
		{
			if (i == 0)
			{
				items_buildings[i].Refresh((int)factionHQ.missionStatsTracker.units.buildings.lost, isLoss: true, isValue: false);
			}
			else
			{
				items_buildings[i].RefreshDefinition(currentDisplay);
			}
		}
		for (int j = 0; j < items_vehicles.Count; j++)
		{
			if (j == 0)
			{
				items_vehicles[j].Refresh((int)factionHQ.missionStatsTracker.units.vehicles.lost, isLoss: true, isValue: false);
			}
			else
			{
				items_vehicles[j].RefreshDefinition(currentDisplay);
			}
		}
		for (int k = 0; k < items_ships.Count; k++)
		{
			if (k == 0)
			{
				items_ships[k].Refresh((int)factionHQ.missionStatsTracker.units.ships.lost, isLoss: true, isValue: false);
			}
			else
			{
				items_ships[k].RefreshDefinition(currentDisplay);
			}
		}
		for (int l = 0; l < items_aircraft.Count; l++)
		{
			if (l == 0)
			{
				items_aircraft[l].Refresh((int)factionHQ.missionStatsTracker.units.aircraft.lost, isLoss: true, isValue: false);
			}
			else
			{
				items_aircraft[l].RefreshDefinition(currentDisplay);
			}
		}
	}

	public void SetDisplayValue()
	{
		currentDisplay = DisplayType.Value;
		players_container.SetActive(value: false);
		airbase_container.SetActive(value: false);
		total_value_container.SetActive(value: true);
		total_value_parent.gameObject.SetActive(value: true);
		items_total_value[2].gameObject.SetActive(value: true);
		buildings_container.SetActive(value: true);
		buildings_item_parent.gameObject.SetActive(value: false);
		buildings_value_parent.gameObject.SetActive(value: true);
		items_buildings_value[2].gameObject.SetActive(value: true);
		vehicles_container.SetActive(value: true);
		vehicles_item_parent.gameObject.SetActive(value: false);
		vehicles_value_parent.gameObject.SetActive(value: true);
		items_vehicles_value[2].gameObject.SetActive(value: true);
		ships_container.SetActive(value: true);
		ships_item_parent.gameObject.SetActive(value: false);
		ships_value_parent.gameObject.SetActive(value: true);
		items_ships_value[2].gameObject.SetActive(value: true);
		aircraft_container.SetActive(value: true);
		aircraft_item_parent.gameObject.SetActive(value: false);
		aircraft_value_parent.gameObject.SetActive(value: true);
		items_aircraft_value[2].gameObject.SetActive(value: true);
	}

	public void UpdateDisplayValue()
	{
		List<InfoPanel_ItemPrefab>[] array = new List<InfoPanel_ItemPrefab>[5] { items_total_value, items_buildings_value, items_vehicles_value, items_ships_value, items_aircraft_value };
		for (int i = 0; i < 5; i++)
		{
			MissionStatsTracker.Stat stat = factionHQ.missionStatsTracker.value.total;
			switch (i)
			{
			case 1:
				stat = factionHQ.missionStatsTracker.value.buildings;
				break;
			case 2:
				stat = factionHQ.missionStatsTracker.value.vehicles;
				break;
			case 3:
				stat = factionHQ.missionStatsTracker.value.ships;
				break;
			case 4:
				stat = factionHQ.missionStatsTracker.value.aircraft;
				break;
			}
			array[i][0].Refresh(stat.total, isLoss: false, isValue: true);
			array[i][1].Refresh(stat.current, isLoss: false, isValue: true);
			array[i][2].Refresh(stat.spent, isLoss: false, isValue: true);
			array[i][3].Refresh(stat.lost, isLoss: true, isValue: true);
		}
	}

	public void SetDisplayManpower()
	{
		currentDisplay = DisplayType.Manpower;
		players_container.SetActive(value: false);
		airbase_container.SetActive(value: false);
		total_value_container.SetActive(value: true);
		total_value_parent.gameObject.SetActive(value: true);
		items_total_value[2].gameObject.SetActive(value: false);
		buildings_container.SetActive(value: true);
		buildings_item_parent.gameObject.SetActive(value: false);
		buildings_value_parent.gameObject.SetActive(value: true);
		items_buildings_value[2].gameObject.SetActive(value: false);
		vehicles_container.SetActive(value: true);
		vehicles_item_parent.gameObject.SetActive(value: false);
		vehicles_value_parent.gameObject.SetActive(value: true);
		items_vehicles_value[2].gameObject.SetActive(value: false);
		ships_container.SetActive(value: true);
		ships_item_parent.gameObject.SetActive(value: false);
		ships_value_parent.gameObject.SetActive(value: true);
		items_ships_value[2].gameObject.SetActive(value: false);
		aircraft_container.SetActive(value: true);
		aircraft_item_parent.gameObject.SetActive(value: false);
		aircraft_value_parent.gameObject.SetActive(value: true);
		items_aircraft_value[2].gameObject.SetActive(value: false);
	}

	public void UpdateDisplayManpower()
	{
		List<InfoPanel_ItemPrefab>[] array = new List<InfoPanel_ItemPrefab>[5] { items_total_value, items_buildings_value, items_vehicles_value, items_ships_value, items_aircraft_value };
		for (int i = 0; i < 5; i++)
		{
			MissionStatsTracker.Stat stat = factionHQ.missionStatsTracker.manpower.total;
			switch (i)
			{
			case 1:
				stat = factionHQ.missionStatsTracker.manpower.buildings;
				break;
			case 2:
				stat = factionHQ.missionStatsTracker.manpower.vehicles;
				break;
			case 3:
				stat = factionHQ.missionStatsTracker.manpower.ships;
				break;
			case 4:
				stat = factionHQ.missionStatsTracker.manpower.aircraft;
				break;
			}
			array[i][0].Refresh((int)stat.total, isLoss: false, isValue: false);
			array[i][1].Refresh((int)stat.current, isLoss: false, isValue: false);
			array[i][2].Refresh((int)stat.spent, isLoss: false, isValue: false);
			array[i][3].Refresh((int)stat.lost, isLoss: true, isValue: false);
		}
	}

	public void SetDisplayPlayers()
	{
		currentDisplay = DisplayType.Players;
		players_container.SetActive(value: true);
		airbase_container.SetActive(value: false);
		total_value_container.SetActive(value: false);
		vehicles_container.SetActive(value: false);
		ships_container.SetActive(value: false);
		buildings_container.SetActive(value: false);
		aircraft_container.SetActive(value: false);
	}

	public void UpdateDisplayPlayers()
	{
		players.Refresh(factionHQ.factionPlayers.Count, isLoss: false, isValue: false);
		if (factionHQ.factionPlayers.Count == 0)
		{
			return;
		}
		for (int i = 0; i < factionHQ.factionPlayers.Count; i++)
		{
			if (!listPlayers.Contains(factionHQ.factionPlayers[i].Player))
			{
				listPlayers.Add(factionHQ.factionPlayers[i].Player);
				AddPlayerEntry(factionHQ.factionPlayers[i].Player);
			}
		}
	}

	public void SetDisplayAirbases()
	{
		currentDisplay = DisplayType.Players;
		players_container.SetActive(value: false);
		airbase_container.SetActive(value: true);
		total_value_container.SetActive(value: false);
		vehicles_container.SetActive(value: false);
		ships_container.SetActive(value: false);
		buildings_container.SetActive(value: false);
		aircraft_container.SetActive(value: false);
		UpdateAirbaseList();
	}

	public void ToggleDisplayType()
	{
		currentDisplay = (DisplayType)currentDisplayDropdown.value;
		switch (currentDisplay)
		{
		case DisplayType.Forces:
			SetDisplayForces();
			break;
		case DisplayType.Players:
			SetDisplayPlayers();
			break;
		case DisplayType.Airbases:
			SetDisplayAirbases();
			break;
		case DisplayType.Reserves:
			SetDisplayReserves();
			break;
		case DisplayType.Losses:
			SetDisplayLosses();
			break;
		case DisplayType.Value:
			SetDisplayValue();
			break;
		case DisplayType.Manpower:
			SetDisplayManpower();
			break;
		}
	}

	public void ResetLists()
	{
		if (airbases_list.childCount > 0)
		{
			for (int i = 1; i < airbases_list.childCount; i++)
			{
				Object.Destroy(airbases_list.GetChild(i).gameObject);
			}
		}
		if (players_list.childCount > 0)
		{
			foreach (Transform item in players_list)
			{
				Object.Destroy(item.gameObject);
			}
		}
		listAirbases.Clear();
		listPlayers.Clear();
	}

	private void InfoPanel_Faction_OnThemeGroupChanged()
	{
		fundsGradient = ThemeManager.Active.ColorTheme.Gradient();
		score.color = ThemeManager.Active.ColorTheme.AllClear;
		warheads.color = ThemeManager.Active.ColorTheme.AllClear;
	}
}
