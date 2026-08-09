using System;
using System.Collections.Generic;
using Mirage;
using NuclearOption.NodeGraph;
using NuclearOption.SavedMission.ConvertVersions;
using NuclearOption.SceneLoading;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	[Serializable]
	[NetworkMessage]
	public class Mission
	{
		public static readonly Mission NullMission = new Mission("NULL", skipAfterLoad: true);

		public int JsonVersion;

		public MapKey MapKey;

		public MissionSettings missionSettings = new MissionSettings();

		public MissionEnvironment environment = new MissionEnvironment();

		public List<SavedAircraft> aircraft = new List<SavedAircraft>();

		public List<SavedVehicle> vehicles = new List<SavedVehicle>();

		public List<SavedShip> ships = new List<SavedShip>();

		public List<SavedBuilding> buildings = new List<SavedBuilding>();

		public List<SavedScenery> scenery = new List<SavedScenery>();

		public List<SavedContainer> containers = new List<SavedContainer>();

		public List<SavedMissile> missiles = new List<SavedMissile>();

		public List<SavedPilot> pilots = new List<SavedPilot>();

		public List<MissionFaction> factions = new List<MissionFaction>();

		public List<SavedAirbase> airbases = new List<SavedAirbase>();

		public List<SavedObjective> objectives = new List<SavedObjective>();

		public List<SavedOutcome> outcomes = new List<SavedOutcome>();

		[NonSerialized]
		public string Name;

		[NonSerialized]
		public MissionObjectives RuntimeObjectives;

		[NonSerialized]
		public MissionKey? LoadKey;

		[NonSerialized]
		public NewMissionConfig? NewMissionConfig;

		[NonSerialized]
		public LoadErrors LoadErrors;

		[NonSerialized]
		public LoadErrors SaveErrors;

		[NonSerialized]
		public GraphLayoutJson ObjectiveGraphLayout;

		public bool HasFullLoaded => RuntimeObjectives != null;

		public Mission()
		{
		}

		public Mission(string name, bool skipAfterLoad = false)
		{
			JsonVersion = MissionVersionUpgrade.LatestVersion;
			Name = name;
			SetupNewMission(this);
			if (!skipAfterLoad)
			{
				AfterLoad(name);
			}
		}

		private static void SetupNewMission(Mission mission)
		{
			Mission mission2 = mission;
			if (mission2.objectives == null)
			{
				mission2.objectives = new List<SavedObjective>();
			}
			mission2 = mission;
			if (mission2.outcomes == null)
			{
				mission2.outcomes = new List<SavedOutcome>();
			}
			if (mission.objectives.Count == 0)
			{
				SavedObjective savedObjective = SavedObjective.CreateSavedObjective(ObjectiveType.None, MissionObjectivesFactory.MissionStartName);
				savedObjective.Hidden = true;
				mission.objectives.Add(savedObjective);
			}
		}

		public void AfterLoad(string name)
		{
			LoadKey = null;
			Name = name;
			AfterLoadInner();
		}

		public void AfterLoad(MissionKey key)
		{
			LoadKey = key;
			Name = key.Name;
			AfterLoadInner();
		}

		public void ReferenceReplaced(ISaveableReference oldRef, ISaveableReference newRef)
		{
			foreach (Objective allObjective in RuntimeObjectives.AllObjectives)
			{
				allObjective.ReferenceReplaced(oldRef, newRef);
			}
			foreach (Outcome allOutcome in RuntimeObjectives.AllOutcomes)
			{
				allOutcome.ReferenceReplaced(oldRef, newRef);
			}
			foreach (SavedBuilding building in buildings)
			{
				building.ReferenceReplaced(oldRef, newRef);
			}
		}

		private void AfterLoadInner()
		{
			MissionVersionUpgrade.Upgrade(this);
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(this, item, includeBuiltIn: false);
			foreach (SavedUnit item2 in item)
			{
				if (item2.rotation.Equals(default(Quaternion)))
				{
					item2.rotation = Quaternion.identity;
				}
			}
			ColorLog<MissionManager>.Info("AfterLoad for mission " + (Name ?? "NULL"));
		}

		public void OnSceneLoaded(MissionManager manager)
		{
			ColorLog<MissionManager>.Info("OnSceneLoaded for mission " + (Name ?? "NULL"));
			if (LoadErrors == null)
			{
				LoadErrors = new LoadErrors();
			}
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(this, item, includeBuiltIn: false);
			foreach (SavedUnit item3 in item)
			{
				if (UnitRegistry.customIDLookup.TryGetValue(item3.UniqueName, out var value))
				{
					if (value.BuiltIn)
					{
						item3.PlacementType = PlacementType.Override;
					}
					else
					{
						item3.PlacementType = PlacementType.Custom;
					}
					value.LinkSavedUnit(item3);
				}
				else
				{
					item3.PlacementType = PlacementType.Custom;
				}
			}
			if (GameManager.gameState == GameState.Editor)
			{
				foreach (Unit allUnit in UnitRegistry.allUnits)
				{
					if (allUnit.SavedUnit == null)
					{
						allUnit.EditorMapLoaded();
					}
				}
				foreach (Airbase value2 in FactionRegistry.airbaseLookup.Values)
				{
					value2.SavedAirbase.AfterLoadEditor(value2, null);
				}
				if (airbases.Count > 0)
				{
					using AutoPool<List<SavedBuilding>>.Wrapper wrapper2 = AutoPool<List<SavedBuilding>>.Take();
					List<SavedBuilding> item2 = wrapper2.Item;
					MissionManager.GetAllSavedBuildingsNonAlloc(this, item2, includeBuiltIn: true);
					foreach (SavedAirbase airbasis in airbases)
					{
						airbasis.AfterLoadEditor(null, item2);
					}
				}
			}
			RuntimeObjectives = MissionObjectivesFactory.Load(this);
			foreach (Objective allObjective in RuntimeObjectives.AllObjectives)
			{
				allObjective.MissionManager = manager;
			}
			foreach (FactionHQ allHQ in FactionRegistry.GetAllHQs())
			{
				string factionName = allHQ.faction.factionName;
				foreach (Objective allObjective2 in RuntimeObjectives.AllObjectives)
				{
					if (allObjective2.SavedObjective.Faction == factionName)
					{
						allObjective2.FactionHQ = allHQ;
					}
				}
			}
			SetupAirbase(manager.ServerObjectManager);
			if (manager.IsServer)
			{
				foreach (SavedBuilding building in buildings)
				{
					building.LoadAirbaseRef();
				}
				{
					foreach (Unit allUnit2 in UnitRegistry.allUnits)
					{
						ServerSetupBuiltInBuildings(allUnit2);
					}
					return;
				}
			}
			if (manager.IsClient)
			{
				ClientAddBuildingsToAirbase();
			}
		}

		public static void ClientAddBuildingsToAirbase()
		{
			foreach (Unit allUnit in UnitRegistry.allUnits)
			{
				if (allUnit is Building building)
				{
					building.ClientAddBuildingToAirbase();
				}
			}
		}

		private void ServerSetupBuiltInBuildings(Unit unit)
		{
			if (!unit.BuiltIn)
			{
				Debug.LogError("Only OnSceneLoaded units should exist in OnSceneLoaded");
			}
			else
			{
				if (!(unit is Building building))
				{
					return;
				}
				if (unit.SavedUnit is SavedBuilding { PlacementType: not PlacementType.BuiltIn } savedBuilding)
				{
					if (savedBuilding.PlacementType == PlacementType.Override)
					{
						if (savedBuilding.AirbaseRef != null)
						{
							building.SetAirbase(savedBuilding.AirbaseRef.Airbase);
						}
						else
						{
							building.NetworkHQ = savedBuilding.FindHQ();
						}
						building.capturable = savedBuilding.capturable;
						if (savedBuilding.factoryOptions != null && building.TryGetComponent<Factory>(out var component))
						{
							component.SetFactory(savedBuilding.factoryOptions);
						}
					}
					else
					{
						Debug.LogError($"Building should not have placement type {savedBuilding.PlacementType} before units have spawned");
					}
				}
				else if (building.MapAirbase != null)
				{
					building.SetAirbase(building.MapAirbase);
				}
				else if (building.MapHQ != null)
				{
					building.NetworkHQ = building.MapHQ;
				}
			}
		}

		public void BeforeSave()
		{
			SaveErrors = new LoadErrors();
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(this, item, includeBuiltIn: true);
			foreach (SavedUnit item2 in item)
			{
				SaveHelper.MakeUnique(item2, item);
			}
			foreach (SavedAirbase airbasis in airbases)
			{
				this.MakeUnique(airbasis);
			}
			if (RuntimeObjectives != null)
			{
				foreach (Outcome allOutcome in RuntimeObjectives.AllOutcomes)
				{
					RuntimeObjectives.MakeUnique(allOutcome);
				}
				foreach (Objective allObjective in RuntimeObjectives.AllObjectives)
				{
					RuntimeObjectives.MakeUnique(allObjective);
				}
				MissionObjectivesFactory.Save(this, RuntimeObjectives);
			}
			foreach (SavedBuilding building in buildings)
			{
				building.SaveAirbaseString();
			}
			foreach (SavedAirbase airbasis2 in airbases)
			{
				airbasis2.BeforeSave();
			}
		}

		public void EnsureFactionExists(Faction faction, out MissionFaction missionFaction)
		{
			if (!TryGetFaction(faction.factionName, out missionFaction))
			{
				missionFaction = new MissionFaction(faction.factionName);
				factions.Add(missionFaction);
			}
		}

		public void GetFactionFromHq(FactionHQ factionHQ, out MissionFaction missionFaction)
		{
			EnsureFactionExists(factionHQ.faction, out missionFaction);
			missionFaction.FactionHQ = factionHQ;
		}

		public bool TryGetFaction(string name, out MissionFaction faction)
		{
			foreach (MissionFaction faction2 in factions)
			{
				if (faction2.factionName == name)
				{
					faction = faction2;
					return true;
				}
			}
			faction = null;
			return false;
		}

		private void SetupAirbase(ServerObjectManager som)
		{
			Dictionary<string, Airbase> airbaseLookup = FactionRegistry.airbaseLookup;
			bool flag = som != null;
			List<SavedAirbase> list = new List<SavedAirbase>();
			foreach (Airbase value2 in airbaseLookup.Values)
			{
				value2.UnlinkSavedAirbase();
			}
			foreach (SavedAirbase airbasis in airbases)
			{
				airbasis.SavedInMission = true;
				if (airbaseLookup.TryGetValue(airbasis.UniqueName, out var value))
				{
					ColorLog<Mission>.Info("Found " + airbasis.UniqueName + " in scene");
					value.LinkSavedAirbase(airbasis, value.IsCustom);
					using AutoPool<List<SavedBuilding>>.Wrapper wrapper = AutoPool<List<SavedBuilding>>.Take();
					List<SavedBuilding> item = wrapper.Item;
					MissionManager.GetAllSavedBuildingsNonAlloc(this, item, includeBuiltIn: true);
					foreach (SavedBuilding item2 in item)
					{
						if (item2.Airbase == airbasis.UniqueName)
						{
							item2.SetAirbase(airbasis);
						}
					}
				}
				else
				{
					if (!flag)
					{
						continue;
					}
					if (airbasis.IsOverride)
					{
						if (airbasis.IsAttached())
						{
							ColorLog<Mission>.Info("Attached airbase " + airbasis.UniqueName + " not in scene yet, waiting for unit to spawn");
							continue;
						}
						LoadErrors.AddWarn(airbasis.UniqueName + " not found in scene but is Override but not Attached");
						ColorLog<Mission>.LogError(airbasis.UniqueName + " not found in scene but is Override but not Attached");
					}
					else
					{
						ColorLog<Mission>.Info("Custom Airbase " + airbasis.UniqueName + " not in scene yet, adding to spawn list");
						list.Add(airbasis);
					}
				}
			}
			foreach (Airbase value3 in airbaseLookup.Values)
			{
				if (!value3.SavedAirbase.SavedInMission)
				{
					value3.LinkSavedAirbase(value3.SavedAirbase, customAirbase: false);
				}
			}
			if (flag)
			{
				SpawnAllCustomAirbase(list, som);
			}
		}

		private void SpawnAllCustomAirbase(List<SavedAirbase> airbaseToSpawn, ServerObjectManager som)
		{
			foreach (SavedAirbase item in airbaseToSpawn)
			{
				SpawnCustomAirbase(item, som);
			}
		}

		public Airbase SpawnCustomAirbase(SavedAirbase saved, ServerObjectManager som)
		{
			ColorLog<Airbase>.Info("Spawn custom airbase " + saved.UniqueName);
			GameObject gameObject = UnityEngine.Object.Instantiate(GameAssets.i.airbasePrefab, Datum.origin);
			gameObject.name = saved.UniqueName;
			Airbase component = gameObject.GetComponent<Airbase>();
			component.SetupCustomAirbase(saved);
			som.Spawn(component.Identity);
			return component;
		}
	}
}
