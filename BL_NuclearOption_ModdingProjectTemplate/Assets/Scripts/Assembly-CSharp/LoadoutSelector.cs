using System;
using System.Collections.Generic;
using NuclearOption.AddressableScripts;
using NuclearOption.SavedMission;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutSelector : MonoBehaviour
{
	[SerializeField]
	private Transform backgroundTransform;

	[SerializeField]
	private GameObject hardpointSelectorPrefab;

	[SerializeField]
	private Slider fuelLevel;

	[SerializeField]
	private TMP_Dropdown liveryDropdown;

	[SerializeField]
	private TMP_Text fuelPercentage;

	private readonly List<(LiveryKey key, string label)> liveryOptions = new List<(LiveryKey, string)>();

	private List<WeaponSelector> weaponSelectors = new List<WeaponSelector>();

	private Aircraft aircraft;

	private FactionHQ hq;

	private Airbase airbase;

	public LiveryKey CurrentLivery => liveryOptions[liveryDropdown.value].key;

	public event Action onLoadoutChange;

	public event Action<WeaponInfo> OnWeaponInfoInspected;

	public void AssignAircraft(Aircraft aircraft, FactionHQ hq, Airbase airbase)
	{
		this.aircraft = aircraft;
		this.hq = hq;
		this.airbase = airbase;
		RefreshLiveryOptions(airbase, selectRandom: true);
		ShowHardpoints();
		LoadDefaults();
		aircraft.weaponManager.InitializeWeaponManager();
	}

	public void ShowHardpoints()
	{
		foreach (WeaponSelector weaponSelector in weaponSelectors)
		{
			UnityEngine.Object.Destroy(weaponSelector.gameObject);
		}
		weaponSelectors.Clear();
		if (aircraft == null || !(aircraft.weaponManager != null))
		{
			return;
		}
		for (int i = 0; i < aircraft.weaponManager.hardpointSets.Length; i++)
		{
			HardpointSet hardpointSet = aircraft.weaponManager.hardpointSets[i];
			WeaponSelector component = UnityEngine.Object.Instantiate(hardpointSelectorPrefab, backgroundTransform).GetComponent<WeaponSelector>();
			component.Initialize(aircraft, hardpointSet, hq, airbase);
			component.OnWeaponSelected += delegate
			{
				UpdateWeapons(respawnWeapons: true);
				SaveDefaults();
			};
			component.OnHover += delegate(WeaponMount mount)
			{
				ShowMountInfo(mount);
			};
			if (hardpointSet.SymmetryWithPrev)
			{
				List<WeaponSelector> list = weaponSelectors;
				list[list.Count - 1].LinkSymmetry(component);
			}
			weaponSelectors.Add(component);
		}
	}

	public void ShowMountInfo(WeaponMount mount)
	{
		WeaponInfo obj = ((mount != null) ? mount.info : null);
		this.OnWeaponInfoInspected?.Invoke(obj);
	}

	public void SaveDefaults()
	{
		if (!(aircraft == null))
		{
			if (!GameManager.aircraftCustomization.ContainsKey(aircraft.definition))
			{
				AircraftCustomization value = new AircraftCustomization(GenerateLoadoutFromDropdowns(), fuelLevel.value, liveryDropdown.value);
				GameManager.aircraftCustomization.Add(aircraft.definition, value);
			}
			else
			{
				GameManager.aircraftCustomization[aircraft.definition].Update(GenerateLoadoutFromDropdowns(), fuelLevel.value, liveryDropdown.value);
			}
		}
	}

	public void LoadDefaults()
	{
		if (!(aircraft == null))
		{
			if (!GameManager.aircraftCustomization.TryGetValue(aircraft.definition, out var value))
			{
				int livery = Mathf.FloorToInt(UnityEngine.Random.Range(0, liveryOptions.Count));
				float defaultFuelLevel = aircraft.definition.aircraftParameters.DefaultFuelLevel;
				value = new AircraftCustomization(aircraft.definition.aircraftParameters.loadouts[1], defaultFuelLevel, livery);
			}
			for (int i = 0; i < value.loadout.weapons.Count; i++)
			{
				WeaponMount value2 = value.loadout.weapons[i];
				weaponSelectors[i].SetValue(value2);
			}
			UpdateWeapons(respawnWeapons: false);
			liveryDropdown.SetValueWithoutNotify(value.livery);
			aircraft.SetLiveryKey(CurrentLivery, loadIfUnspawned: true);
			fuelLevel.SetValueWithoutNotify(value.fuelLevel);
			ChangeFuelLevel();
		}
	}

	public void RefreshLiveryOptions(Airbase airbase, bool selectRandom)
	{
		string factionName = airbase.CurrentHQ.faction.factionName;
		RefreshLiveryOptions(liveryDropdown, liveryOptions, aircraft.definition, factionName, allowFactionLivery: true, selectRandom);
	}

	public static void RefreshLiveryOptions(TMP_Dropdown liveryDropdown, List<(LiveryKey key, string label)> liveryOptions, AircraftDefinition aircraft, string aircraftFaction, bool allowFactionLivery, bool selectRandom)
	{
		GetLiveryOptions(liveryOptions, aircraft, aircraftFaction, allowFactionLivery);
		liveryDropdown.ClearOptions();
		foreach (var liveryOption in liveryOptions)
		{
			liveryDropdown.options.Add(new TMP_Dropdown.OptionData(liveryOption.label));
		}
		if (liveryOptions.Count > 0 && selectRandom)
		{
			liveryDropdown.SetValueWithoutNotify(0);
			liveryDropdown.RefreshShownValue();
		}
	}

	public static void GetLiveryOptions(List<(LiveryKey key, string label)> resultsList, AircraftDefinition aircraft, string aircraftFaction, bool allowFactionLivery)
	{
		resultsList.Clear();
		AircraftParameters aircraftParameters = aircraft.aircraftParameters;
		for (int i = 0; i < aircraftParameters.liveries.Count; i++)
		{
			AircraftParameters.Livery livery = aircraftParameters.liveries[i];
			if (ValidFaction(livery.faction?.factionName))
			{
				resultsList.Add((new LiveryKey(i), livery.name));
			}
		}
		if (!ModLoadCache.HasSkinMetaData || GameManager.gameState == GameState.Editor)
		{
			ModLoadCache.SkinMetaData.Clear();
			ModLoadCache.SkinMetaData.AddRange(ModFolders.AppDataSkins.ListMetaData());
			ModLoadCache.SkinMetaData.AddRange(ModFolders.WorkshopSkins.ListMetaData());
			ModLoadCache.HasSkinMetaData = true;
		}
		foreach (LiveryMetaData skinMetaDatum in ModLoadCache.SkinMetaData)
		{
			if (skinMetaDatum.CheckAircraft(aircraft) && ValidFaction(skinMetaDatum.Faction))
			{
				bool flag = skinMetaDatum.Id != PublishedFileId_t.Invalid;
				string text = (flag ? " (workshop)" : " (app data)");
				resultsList.Add((new LiveryKey(skinMetaDatum, flag), skinMetaDatum.DisplayName + text));
			}
		}
		bool ValidFaction(string liveryFaction)
		{
			if (FactionHelper.EmptyOrNoFactionOrNeutral(liveryFaction))
			{
				return true;
			}
			if (!allowFactionLivery)
			{
				return false;
			}
			if (FactionHelper.EmptyOrNoFactionOrNeutral(aircraftFaction))
			{
				return true;
			}
			return liveryFaction == aircraftFaction;
		}
	}

	public void SelectLivery()
	{
		aircraft.SetLiveryKey(CurrentLivery, loadIfUnspawned: true);
	}

	public Loadout GenerateLoadoutFromDropdowns()
	{
		Loadout loadout = new Loadout();
		for (int i = 0; i < weaponSelectors.Count; i++)
		{
			loadout.weapons.Add(weaponSelectors[i].GetValue());
		}
		return loadout;
	}

	public void ChangeFuelLevel()
	{
		if (aircraft != null)
		{
			aircraft.NetworkfuelLevel = fuelLevel.value;
			aircraft.Refuel(null);
		}
		fuelPercentage.text = $"{fuelLevel.value * 100f:F0}%";
		this.onLoadoutChange?.Invoke();
	}

	public void UpdateWeapons(bool respawnWeapons)
	{
		aircraft.Networkloadout = GenerateLoadoutFromDropdowns();
		if (respawnWeapons)
		{
			aircraft.weaponManager.SpawnWeapons();
		}
		foreach (WeaponSelector weaponSelector in weaponSelectors)
		{
			weaponSelector.SetInteractable(aircraft.loadout);
		}
		this.onLoadoutChange?.Invoke();
	}
}
