using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Encyclopedia", menuName = "ScriptableObjects/Encyclopedia", order = 999)]
public class Encyclopedia : ScriptableObject
{
	[Serializable]
	public class UnitType
	{
		public string typeName;

		public Sprite typeSprite;
	}

	private static readonly ResourcesAsyncLoader<Encyclopedia> loader = ResourcesAsyncLoader.Create<Encyclopedia>("Encyclopedia", AfterLoad);

	public List<AircraftDefinition> aircraft;

	[SerializeField]
	public List<UnitType> vehicleTypes;

	public List<VehicleDefinition> vehicles;

	public List<MissileDefinition> missiles;

	[SerializeField]
	public List<UnitType> buildingTypes;

	public List<BuildingDefinition> buildings;

	[SerializeField]
	public List<UnitType> shipTypes;

	public List<ShipDefinition> ships;

	public List<SceneryDefinition> scenery;

	public List<UnitDefinition> otherUnits;

	public List<WeaponMount> weaponMounts;

	public List<Faction> factions;

	public static Dictionary<string, UnitDefinition> Lookup;

	public static Dictionary<string, WeaponMount> WeaponLookup;

	private static Dictionary<UnitDefinition, float> massLookup;

	public List<INetworkDefinition> IndexLookup;

	[NonSerialized]
	private List<UnitDefinition> aircraftAndVehiclesCache;

	public static Encyclopedia i => loader.Get();

	public static async UniTask Preload(CancellationToken cancel)
	{
		await loader.Load(cancel);
	}

	private static void AfterLoad(Encyclopedia instance)
	{
		instance.AfterLoad();
	}

	public void SortByValue()
	{
		aircraft.Sort((AircraftDefinition a, AircraftDefinition b) => a.value.CompareTo(b.value));
		vehicles.Sort((VehicleDefinition a, VehicleDefinition b) => a.value.CompareTo(b.value));
	}

	private void AfterLoad()
	{
		aircraftAndVehiclesCache = null;
		if (Lookup == null)
		{
			Lookup = new Dictionary<string, UnitDefinition>();
		}
		Lookup.Clear();
		if (WeaponLookup == null)
		{
			WeaponLookup = new Dictionary<string, WeaponMount>();
		}
		WeaponLookup.Clear();
		if (IndexLookup == null)
		{
			IndexLookup = new List<INetworkDefinition>();
		}
		IndexLookup.Clear();
		if (massLookup == null)
		{
			massLookup = new Dictionary<UnitDefinition, float>();
		}
		massLookup.Clear();
		foreach (AircraftDefinition item in aircraft)
		{
			item.CacheMass();
		}
		foreach (VehicleDefinition vehicle in vehicles)
		{
			vehicle.CacheMass();
		}
		foreach (MissileDefinition missile in missiles)
		{
			missile.CacheMass();
		}
		foreach (ShipDefinition ship in ships)
		{
			ship.CacheMass();
		}
		foreach (UnitDefinition otherUnit in otherUnits)
		{
			otherUnit.CacheMass();
		}
		foreach (WeaponMount weaponMount in weaponMounts)
		{
			weaponMount.Initialize();
		}
		SortByValue();
		AddList<AircraftDefinition, UnitDefinition>(Lookup, aircraft);
		AddList<VehicleDefinition, UnitDefinition>(Lookup, vehicles);
		AddList<MissileDefinition, UnitDefinition>(Lookup, missiles);
		AddList<BuildingDefinition, UnitDefinition>(Lookup, buildings);
		AddList<ShipDefinition, UnitDefinition>(Lookup, ships);
		AddList<SceneryDefinition, UnitDefinition>(Lookup, scenery);
		AddList<UnitDefinition, UnitDefinition>(Lookup, otherUnits);
		AddList<WeaponMount, WeaponMount>(WeaponLookup, weaponMounts);
		AddWithIndex<AircraftDefinition>(IndexLookup, aircraft);
		AddWithIndex<VehicleDefinition>(IndexLookup, vehicles);
		AddWithIndex<MissileDefinition>(IndexLookup, missiles);
		AddWithIndex<BuildingDefinition>(IndexLookup, buildings);
		AddWithIndex<ShipDefinition>(IndexLookup, ships);
		AddWithIndex<SceneryDefinition>(IndexLookup, scenery);
		AddWithIndex<UnitDefinition>(IndexLookup, otherUnits);
		AddWithIndex<WeaponMount>(IndexLookup, weaponMounts);
		AddWithIndex<Faction>(IndexLookup, factions);
		static void AddList<TItem, TBase>(Dictionary<string, TBase> lookup, List<TItem> items) where TItem : TBase where TBase : ScriptableObject, IHasJsonKey
		{
			foreach (TItem item2 in items)
			{
				if (item2 == null)
				{
					Debug.LogError($"Null found in Encyclopedia list for, {typeof(TItem)}");
				}
				else
				{
					lookup.Add(item2.JsonKey, (TBase)item2);
				}
			}
		}
		static void AddWithIndex<T>(List<INetworkDefinition> lookup, List<T> items) where T : ScriptableObject, INetworkDefinition
		{
			foreach (T item3 in items)
			{
				if (!(item3 == null))
				{
					item3.LookupIndex = lookup.Count;
					lookup.Add(item3);
				}
			}
		}
	}

	public bool TryGetPrefab(string key, out GameObject prefab)
	{
		if (Lookup.TryGetValue(key, out var value))
		{
			prefab = value.unitPrefab;
			return true;
		}
		Debug.LogError("Prefab with key '" + key + "' not found in Encyclopedia");
		prefab = null;
		return false;
	}

	public IReadOnlyList<UnitDefinition> GetAircraftAndVehicles()
	{
		if (aircraftAndVehiclesCache == null)
		{
			aircraftAndVehiclesCache = new List<UnitDefinition>();
			aircraftAndVehiclesCache.AddRange(aircraft);
			aircraftAndVehiclesCache.AddRange(vehicles);
		}
		return aircraftAndVehiclesCache;
	}

	private void OnValidate()
	{
		Dictionary<string, IHasJsonKey> allKeys = new Dictionary<string, IHasJsonKey>(200);
		Validate<AircraftDefinition>(aircraft);
		Validate<VehicleDefinition>(vehicles);
		Validate<MissileDefinition>(missiles);
		Validate<BuildingDefinition>(buildings);
		Validate<ShipDefinition>(ships);
		Validate<SceneryDefinition>(scenery);
		Validate<UnitDefinition>(otherUnits);
		Validate<WeaponMount>(weaponMounts);
		void Validate<T>(List<T> assets) where T : UnityEngine.Object, IHasJsonKey
		{
			foreach (T asset in assets)
			{
				if (asset == null)
				{
					Debug.LogWarning("Null item count in Encyclopedia lists");
				}
				else if (string.IsNullOrEmpty(asset.JsonKey))
				{
					asset.JsonKey = asset.name;
					Debug.LogWarning($"Asset in Encyclopedia has no JsonKey, setting key to be asset name: {asset}", asset);
				}
				else if (!allKeys.TryAdd(asset.JsonKey, asset))
				{
					Debug.LogError($"Two or more assets had the same JsonKey, old:{allKeys[asset.JsonKey]}, new:{asset}");
				}
			}
		}
	}

	[ContextMenu("Log All Aircraft Names")]
	public void LogAllAircraftNames()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (AircraftDefinition item in aircraft.OrderBy((AircraftDefinition x) => x.value))
		{
			stringBuilder.AppendLine("\"" + item.jsonKey + "\",\"" + item.unitName + "\",");
			stringBuilder2.AppendLine("\"" + item.unitName + "\",");
		}
		Debug.Log(stringBuilder);
		Debug.Log(stringBuilder2);
	}
}
