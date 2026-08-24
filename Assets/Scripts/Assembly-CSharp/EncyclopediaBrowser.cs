using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.SceneLoading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EncyclopediaBrowser : SceneSingleton<EncyclopediaBrowser>
{
	[Serializable]
	private class WeaponStationDisplay
	{
		[SerializeField]
		private GameObject panel;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private TMP_Text ammoText;

		private List<WeaponStation> weaponStations;

		private WeaponInfo weaponInfo;

		public void Hide()
		{
			panel.SetActive(value: false);
			if (weaponStations != null)
			{
				weaponStations.Clear();
			}
			weaponInfo = null;
		}

		public void Show(Unit unit, WeaponInfo weaponInfo)
		{
			this.weaponInfo = weaponInfo;
			if (weaponStations == null)
			{
				weaponStations = new List<WeaponStation>();
			}
			weaponStations.Clear();
			foreach (WeaponStation weaponStation in unit.weaponStations)
			{
				if (weaponStation.WeaponInfo == weaponInfo)
				{
					weaponStations.Add(weaponStation);
				}
			}
			panel.SetActive(value: true);
			UpdateText();
		}

		private void UpdateText()
		{
			int num = 0;
			foreach (WeaponStation weaponStation in weaponStations)
			{
				num += weaponStation.FullAmmo;
			}
			nameText.text = weaponInfo.weaponName ?? "";
			ammoText.text = $"x {num}";
		}
	}

	public enum EncyclopediaMode
	{
		Aircraft = 0,
		Vehicle = 1,
		Ship = 2,
		Building = 3,
		Missile = 4
	}

	private enum BrowserMode
	{
		Aircraft = 0,
		Vehicle = 1,
		Building = 2,
		Ship = 3,
		Missile = 4
	}

	public EncyclopediaMode mode = EncyclopediaMode.Vehicle;

	private List<UnitDefinition> browseList = new List<UnitDefinition>();

	[SerializeField]
	private MapLoader mapLoader;

	[SerializeField]
	private Button vehiclesTab;

	[SerializeField]
	private Button aircraftTab;

	[SerializeField]
	private Button shipsTab;

	[SerializeField]
	private Button buildingsTab;

	[SerializeField]
	private Button missilesTab;

	[SerializeField]
	private Transform[] spawnTransforms;

	private Transform spawnTransform;

	[SerializeField]
	private TMP_Text unitName;

	[SerializeField]
	private TMP_Text unitDescription;

	[SerializeField]
	private GameObject massPanel;

	[SerializeField]
	private GameObject topSpeedPanel;

	[SerializeField]
	private GameObject emptyWeightPanel;

	[SerializeField]
	private GameObject stallSpeedPanel;

	[SerializeField]
	private GameObject maneuverabilityPanel;

	[SerializeField]
	private GameObject guidancePanel;

	[SerializeField]
	private GameObject yieldPanel;

	[SerializeField]
	private GameObject burnTimePanel;

	[SerializeField]
	private GameObject deltaVPanel;

	[SerializeField]
	private GameObject rangePanel;

	[SerializeField]
	private GameObject rcsPanel;

	[SerializeField]
	private GameObject weaponPanel;

	[SerializeField]
	private TMP_Text length;

	[SerializeField]
	private TMP_Text width;

	[SerializeField]
	private TMP_Text height;

	[SerializeField]
	private TMP_Text cost;

	[SerializeField]
	private TMP_Text mass;

	[SerializeField]
	private TMP_Text topSpeed;

	[SerializeField]
	private TMP_Text emptyWeight;

	[SerializeField]
	private TMP_Text stallSpeed;

	[SerializeField]
	private TMP_Text maneuverability;

	[SerializeField]
	private TMP_Text guidance;

	[SerializeField]
	private TMP_Text yield;

	[SerializeField]
	private TMP_Text burnTime;

	[SerializeField]
	private TMP_Text deltaV;

	[SerializeField]
	private TMP_Text range;

	[SerializeField]
	private TMP_Text rcs;

	[SerializeField]
	private GameObject backdropGround;

	[SerializeField]
	private GameObject backdropWater;

	[SerializeField]
	private RectTransform descriptionPanel;

	[SerializeField]
	private Material waterMaterial;

	[SerializeField]
	private MapKey mapKey;

	[SerializeField]
	private MapSettings mapSettings;

	[SerializeField]
	private WeaponStationDisplay[] weaponStationDisplays;

	private List<WeaponInfo> weaponInfoToShow = new List<WeaponInfo>();

	public GameObject spawnedUnitObject;

	private Unit spawnedUnit;

	private Transform[] spawnedParts;

	private float cameraDistance = 5f;

	private float cameraBaseHeight;

	private float targetDistance;

	private float cameraSmoothingVel;

	private bool initialized;

	private int index;

	private void SpawnUnit(UnitDefinition definition)
	{
		if (NetworkSceneSingleton<Spawner>.i == null)
		{
			Debug.LogError("Error: Spawner was not initialized");
		}
		GlobalPosition globalPosition = spawnTransform.GlobalPosition() + Vector3.up * definition.spawnOffset.y;
		Debug.Log($"Spawning {definition.unitName} at global position {globalPosition.x:F0}, {globalPosition.y:F0}, {globalPosition.z:F0}");
		if (mode == EncyclopediaMode.Vehicle)
		{
			spawnedUnitObject = NetworkSceneSingleton<Spawner>.i.SpawnVehicle(definition.unitPrefab, globalPosition, spawnTransform.rotation, Vector3.zero, null, definition.unitName, 0f, holdPosition: true, null).gameObject;
		}
		if (mode == EncyclopediaMode.Aircraft)
		{
			spawnedUnitObject = NetworkSceneSingleton<Spawner>.i.SpawnAircraft(null, definition.unitPrefab, null, 0f, default(LiveryKey), globalPosition, spawnTransform.rotation * Quaternion.Euler((definition as AircraftDefinition).restRotation), Vector3.zero, null, null, definition.unitName, 0f, 0f).gameObject;
		}
		if (mode == EncyclopediaMode.Missile)
		{
			spawnedUnitObject = NetworkSceneSingleton<Spawner>.i.SpawnMissileEncyclopedia(definition as MissileDefinition, spawnTransform).gameObject;
		}
		if (mode == EncyclopediaMode.Ship)
		{
			spawnedUnitObject = NetworkSceneSingleton<Spawner>.i.SpawnShip(definition.unitPrefab, globalPosition, spawnTransform.rotation, null, definition.unitName, 0f, holdPosition: true).gameObject;
		}
		if (mode == EncyclopediaMode.Building)
		{
			spawnedUnitObject = NetworkSceneSingleton<Spawner>.i.SpawnBuilding(definition.unitPrefab, globalPosition, spawnTransform.rotation, null, null, definition.unitName, capturable: false, null).gameObject;
		}
		cameraBaseHeight = definition.spawnOffset.y;
		spawnedParts = spawnedUnitObject.GetComponentsInChildren<Transform>();
		spawnedUnit = spawnedUnitObject.GetComponent<Unit>();
		unitName.text = definition.unitName;
		targetDistance = (Mathf.Max(definition.length, definition.width * 0.7f) + Mathf.Max(definition.width, definition.length * 0.7f)) * 0.5f;
		if (definition.height > definition.length && definition.height > definition.width)
		{
			targetDistance = definition.height * 1.2f;
		}
		unitDescription.text = definition.description;
		float value = Mathf.Pow(targetDistance * 0.1f, 0.2f);
		value = Mathf.Clamp(value, 0.6f, 1.5f);
		targetDistance /= value;
		DisplayUnitInfo(definition);
		UpdateWeaponDisplay(spawnedUnit);
		LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionPanel);
	}

	private void DisplayUnitInfo(UnitDefinition definition)
	{
		length.text = UnitConverter.DimensionReading(definition.length);
		width.text = UnitConverter.DimensionReading(definition.width);
		height.text = UnitConverter.DimensionReading(definition.height);
		cost.text = UnitConverter.ValueReading(definition.value);
		if (definition is BuildingDefinition)
		{
			massPanel.SetActive(value: false);
			topSpeedPanel.SetActive(value: false);
			emptyWeightPanel.SetActive(value: false);
			stallSpeedPanel.SetActive(value: false);
			maneuverabilityPanel.SetActive(value: false);
			deltaVPanel.SetActive(value: false);
			rangePanel.SetActive(value: false);
			guidancePanel.SetActive(value: false);
			yieldPanel.SetActive(value: false);
			burnTimePanel.SetActive(value: false);
			rcsPanel.SetActive(value: false);
		}
		if (definition is VehicleDefinition)
		{
			massPanel.SetActive(value: true);
			topSpeedPanel.SetActive(value: true);
			emptyWeightPanel.SetActive(value: false);
			stallSpeedPanel.SetActive(value: false);
			maneuverabilityPanel.SetActive(value: false);
			deltaVPanel.SetActive(value: false);
			rangePanel.SetActive(value: false);
			guidancePanel.SetActive(value: false);
			yieldPanel.SetActive(value: false);
			burnTimePanel.SetActive(value: false);
			rcsPanel.SetActive(value: false);
			VehicleDefinition vehicleDefinition = definition as VehicleDefinition;
			mass.text = UnitConverter.WeightReading(vehicleDefinition.mass);
			GroundVehicle component = definition.unitPrefab.GetComponent<GroundVehicle>();
			topSpeed.text = UnitConverter.SpeedReadingGround(component.GetTopSpeed() / 3.6f);
		}
		if (definition is ShipDefinition)
		{
			massPanel.SetActive(value: true);
			topSpeedPanel.SetActive(value: true);
			emptyWeightPanel.SetActive(value: false);
			stallSpeedPanel.SetActive(value: false);
			maneuverabilityPanel.SetActive(value: false);
			deltaVPanel.SetActive(value: false);
			rangePanel.SetActive(value: false);
			guidancePanel.SetActive(value: false);
			yieldPanel.SetActive(value: false);
			burnTimePanel.SetActive(value: false);
			rcsPanel.SetActive(value: false);
			ShipDefinition shipDefinition = definition as ShipDefinition;
			mass.text = UnitConverter.WeightReading(shipDefinition.mass);
			topSpeed.text = UnitConverter.SpeedReading(shipDefinition.shipInfo.topSpeed / 3.6f);
		}
		if (definition is AircraftDefinition)
		{
			massPanel.SetActive(value: false);
			emptyWeightPanel.SetActive(value: true);
			stallSpeedPanel.SetActive(value: true);
			maneuverabilityPanel.SetActive(value: true);
			deltaVPanel.SetActive(value: false);
			rangePanel.SetActive(value: false);
			guidancePanel.SetActive(value: false);
			yieldPanel.SetActive(value: false);
			burnTimePanel.SetActive(value: false);
			rcsPanel.SetActive(value: true);
			AircraftDefinition aircraftDefinition = definition as AircraftDefinition;
			Aircraft component2 = spawnedUnitObject.GetComponent<Aircraft>();
			float num = 0f;
			foreach (UnitPart allPart in component2.GetAllParts())
			{
				num += allPart.mass;
			}
			emptyWeight.text = UnitConverter.WeightReading(num);
			topSpeed.text = UnitConverter.SpeedReading(aircraftDefinition.aircraftInfo.maxSpeed / 3.6f);
			stallSpeed.text = UnitConverter.SpeedReading(aircraftDefinition.aircraftInfo.stallSpeed / 3.6f);
			maneuverability.text = aircraftDefinition.aircraftInfo.maneuverability.ToString("F1") + "g";
			rcs.text = $"{aircraftDefinition.radarSize}";
		}
		if (definition is MissileDefinition)
		{
			massPanel.SetActive(value: true);
			emptyWeightPanel.SetActive(value: false);
			stallSpeedPanel.SetActive(value: false);
			maneuverabilityPanel.SetActive(value: false);
			guidancePanel.SetActive(value: true);
			yieldPanel.SetActive(value: true);
			rcsPanel.SetActive(value: true);
			MissileDefinition missileDefinition = definition as MissileDefinition;
			Missile componentInChildren = definition.unitPrefab.GetComponentInChildren<Missile>();
			WeaponInfo weaponInfo = componentInChildren.GetWeaponInfo();
			cost.text = UnitConverter.ValueReading(weaponInfo.costPerRound);
			unitDescription.text = definition.description;
			float num2 = componentInChildren.CalcDeltaV();
			float num3 = componentInChildren.GetTopSpeed(0f, 0f);
			Debug.Log($"Calculated deltaV: {deltaV}, topSpeed: {topSpeed}");
			componentInChildren.CalcRange(0f, 0f, 0f, 10000f, 0f, out var _);
			mass.text = UnitConverter.WeightReading(missileDefinition.GetMass());
			if (num2 > 0f)
			{
				range.text = UnitConverter.DistanceReading(weaponInfo.targetRequirements.maxRange);
				burnTime.text = $"{componentInChildren.GetTotalBurnTime()}s";
				rangePanel.SetActive(value: true);
				burnTimePanel.SetActive(value: true);
			}
			else
			{
				rangePanel.SetActive(value: false);
				burnTimePanel.SetActive(value: false);
			}
			if (num3 >= num2)
			{
				deltaVPanel.SetActive(value: true);
				topSpeedPanel.SetActive(value: false);
				deltaV.text = UnitConverter.SpeedReading(num2);
			}
			else
			{
				deltaVPanel.SetActive(value: false);
				topSpeedPanel.SetActive(value: true);
				topSpeed.text = UnitConverter.SpeedReading(num3);
			}
			guidance.text = componentInChildren.GetComponent<MissileSeeker>().GetSeekerType() ?? "";
			yield.text = UnitConverter.YieldReading(componentInChildren.GetYield()) + " TNT";
			rcs.text = $"{missileDefinition.radarSize}";
		}
	}

	private void UpdateWeaponDisplay(Unit unit)
	{
		weaponPanel.SetActive(value: false);
		WeaponStationDisplay[] array = weaponStationDisplays;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Hide();
		}
		weaponInfoToShow.Clear();
		foreach (WeaponStation weaponStation in unit.weaponStations)
		{
			if (!weaponInfoToShow.Contains(weaponStation.WeaponInfo))
			{
				weaponInfoToShow.Add(weaponStation.WeaponInfo);
			}
		}
		weaponPanel.SetActive(weaponInfoToShow.Count > 0);
		weaponInfoToShow.Sort((WeaponInfo a, WeaponInfo b) => a.costPerRound.CompareTo(b.costPerRound));
		for (int num = 0; num < weaponInfoToShow.Count; num++)
		{
			weaponStationDisplays[num].Show(unit, weaponInfoToShow[num]);
		}
	}

	private void RemoveUnit()
	{
		if (spawnedParts == null)
		{
			return;
		}
		Transform[] array = spawnedParts;
		foreach (Transform transform in array)
		{
			if (transform != null)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
	}

	public void BackToMenu()
	{
		TimeScaleManager.Scale = 1f;
		NetworkManagerNuclearOption.i.Stop(setDisconnectReason: true);
	}

	protected override void Awake()
	{
		base.Awake();
		GameManager.SetGameState(GameState.Encyclopedia);
	}

	private void Start()
	{
		NetworkSceneSingleton<LevelInfo>.i.ApplyMapSettings(mapSettings);
	}

	public Unit GetSpawnedUnit()
	{
		return spawnedUnit;
	}

	private async UniTask Initialize()
	{
		await UniTask.DelayFrame(1);
		SelectVehicles();
	}

	public void SelectAircraft()
	{
		spawnTransform = spawnTransforms[0];
		mode = EncyclopediaMode.Aircraft;
		RemoveUnit();
		index = 0;
		browseList.Clear();
		foreach (AircraftDefinition item in Encyclopedia.i.aircraft)
		{
			if (item.IsAllowed(includeEventContent: false))
			{
				browseList.Add(item);
			}
		}
		browseList.Sort((UnitDefinition a, UnitDefinition b) => a.value.CompareTo(b.value));
		SpawnAircraft(browseList[index]);
	}

	public void SelectVehicles()
	{
		spawnTransform = spawnTransforms[1];
		mode = EncyclopediaMode.Vehicle;
		RemoveUnit();
		index = 0;
		browseList.Clear();
		foreach (VehicleDefinition vehicle in Encyclopedia.i.vehicles)
		{
			if (vehicle.IsAllowed(includeEventContent: false))
			{
				browseList.Add(vehicle);
			}
		}
		browseList.Sort((UnitDefinition a, UnitDefinition b) => a.value.CompareTo(b.value));
		SpawnUnit(browseList[index]);
	}

	public void SelectShips()
	{
		spawnTransform = spawnTransforms[2];
		mode = EncyclopediaMode.Ship;
		RemoveUnit();
		index = 0;
		browseList.Clear();
		browseList.AddRange(Encyclopedia.i.ships);
		browseList.Sort((UnitDefinition a, UnitDefinition b) => a.value.CompareTo(b.value));
		waterMaterial.SetVector("_OriginOffset", Vector2.zero);
		SpawnUnit(browseList[index]);
	}

	public void SelectBuildings()
	{
		spawnTransform = spawnTransforms[3];
		mode = EncyclopediaMode.Building;
		RemoveUnit();
		index = 0;
		browseList.Clear();
		browseList.AddRange(Encyclopedia.i.buildings);
		browseList.Sort((UnitDefinition a, UnitDefinition b) => a.value.CompareTo(b.value));
		SpawnUnit(browseList[index]);
	}

	public void SelectMissiles()
	{
		spawnTransform = spawnTransforms[4];
		mode = EncyclopediaMode.Missile;
		RemoveUnit();
		index = 0;
		browseList.Clear();
		foreach (MissileDefinition missile in Encyclopedia.i.missiles)
		{
			if (missile.IsAllowed(includeEventContent: false))
			{
				browseList.Add(missile);
			}
		}
		browseList.Sort((UnitDefinition a, UnitDefinition b) => a.value.CompareTo(b.value));
		SpawnUnit(browseList[index]);
	}

	public void SpawnAircraft(UnitDefinition definition)
	{
		SpawnUnit(definition);
		Aircraft componentInChildren = spawnedUnitObject.GetComponentInChildren<Aircraft>();
		componentInChildren.networked = false;
		componentInChildren.SetGear(LandingGear.GearState.LockedExtended);
	}

	private void Update()
	{
		if (!initialized && NetworkSceneSingleton<Spawner>.i != null && NetworkSceneSingleton<Spawner>.i.NetId != 0)
		{
			initialized = true;
			PlayerSettings.LoadPrefs();
			aircraftTab.Select();
			SelectAircraft();
		}
		_ = spawnedUnitObject == null;
	}

	public void NextUnit(bool backwards)
	{
		int num = index;
		index += ((!backwards) ? 1 : (-1));
		if (index < 0)
		{
			index = browseList.Count - 1;
		}
		if (index >= browseList.Count)
		{
			index = 0;
		}
		while (browseList[index].NotAllowed(includeEventContent: false))
		{
			index += ((!backwards) ? 1 : (-1));
			if (index < 0)
			{
				index = browseList.Count - 1;
			}
			if (index >= browseList.Count)
			{
				index = 0;
			}
		}
		if (index != num)
		{
			RemoveUnit();
			if (mode == EncyclopediaMode.Vehicle)
			{
				SpawnUnit(browseList[index]);
			}
			if (mode == EncyclopediaMode.Aircraft)
			{
				SpawnAircraft(browseList[index]);
			}
			if (mode == EncyclopediaMode.Ship)
			{
				SpawnUnit(browseList[index]);
			}
			if (mode == EncyclopediaMode.Missile)
			{
				SpawnUnit(browseList[index]);
			}
			if (mode == EncyclopediaMode.Building)
			{
				SpawnUnit(browseList[index]);
			}
		}
	}
}
