using System;
using System.Collections.Generic;
using NuclearOption.SavedMission.Objectives;
using NuclearOption.SavedMission.Outcomes;
using NuclearOption.SceneLoading;
using RoadPathfinding;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Obsolete("V5", true)]
	public class MissionConverter_V5_to_V6
	{
		private ref struct ObjectiveDataReader
		{
			private readonly List<ObjectiveData_V5_OLD> data;

			private int dataIndex;

			public bool HasMore
			{
				get
				{
					if (data != null)
					{
						return dataIndex < data.Count;
					}
					return false;
				}
			}

			public ObjectiveDataReader(List<ObjectiveData_V5_OLD> data)
			{
				this.data = data;
				dataIndex = 0;
			}

			public float ReadFloat(float defaultValue = 0f)
			{
				if (data != null && dataIndex < data.Count)
				{
					return data[dataIndex++].FloatValue;
				}
				return defaultValue;
			}

			public string ReadString(string defaultValue = "")
			{
				if (data != null && dataIndex < data.Count)
				{
					return data[dataIndex++].StringValue;
				}
				return defaultValue;
			}

			public int ReadInt(int defaultValue = 0)
			{
				if (data != null && dataIndex < data.Count)
				{
					return (int)data[dataIndex++].FloatValue;
				}
				return defaultValue;
			}

			public bool ReadBool(bool defaultValue = false)
			{
				if (data != null && dataIndex < data.Count)
				{
					return data[dataIndex++].FloatValue == 1f;
				}
				return defaultValue;
			}

			public Vector3 ReadVector3(Vector3 defaultValue = default(Vector3))
			{
				if (data != null && dataIndex < data.Count)
				{
					return data[dataIndex++].VectorValue;
				}
				return defaultValue;
			}

			public unsafe T ReadEnum<T>(T defaultValue = default(T)) where T : unmanaged, Enum
			{
				if (data != null && dataIndex < data.Count)
				{
					int num = (int)data[dataIndex++].FloatValue;
					return *(T*)(&num);
				}
				return defaultValue;
			}

			public List<string> ReadStringList()
			{
				List<string> list = new List<string>();
				while (HasMore)
				{
					list.Add(data[dataIndex++].StringValue);
				}
				return list;
			}

			public ObjectiveData_V5_OLD ReadRaw()
			{
				if (data != null && dataIndex < data.Count)
				{
					return data[dataIndex++];
				}
				return default(ObjectiveData_V5_OLD);
			}

			public Override<float> ReadFloatOverride()
			{
				bool num = ReadFloat() == 1f;
				float value = (num ? ReadFloat() : 0f);
				return new Override<float>(num, value);
			}

			public Override<string> ReadStringOverride()
			{
				bool num = ReadFloat() == 1f;
				string value = (num ? ReadString() : null);
				return new Override<string>(num, value);
			}

			public Override<bool> ReadBoolOverride()
			{
				bool num = ReadFloat() == 1f;
				bool value = num && ReadBool();
				return new Override<bool>(num, value);
			}
		}

		public readonly LoadErrors LoadErrors = new LoadErrors();

		public static (Mission Mission, LoadErrors Errors) Convert(Mission_V5_OLD old)
		{
			MissionConverter_V5_to_V6 missionConverter_V5_to_V = new MissionConverter_V5_to_V6();
			return (Mission: missionConverter_V5_to_V.ConvertInstance(old), Errors: missionConverter_V5_to_V.LoadErrors);
		}

		public Mission ConvertInstance(Mission_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			Mission mission = new Mission();
			mission.JsonVersion = 6;
			mission.LoadErrors = LoadErrors;
			try
			{
				mission.MapKey = ConvertMapKey(old.MapKey);
			}
			catch (Exception e)
			{
				LoadErrors.AddException(e, "Failed to convert MapKey");
			}
			try
			{
				mission.missionSettings = ConvertMissionSettings(old.missionSettings);
			}
			catch (Exception e2)
			{
				LoadErrors.AddException(e2, "Failed to convert missionSettings");
			}
			try
			{
				mission.environment = ConvertMissionEnvironment(old.environment);
			}
			catch (Exception e3)
			{
				LoadErrors.AddException(e3, "Failed to convert environment");
			}
			if (old.aircraft != null)
			{
				foreach (SavedAircraft_V5_OLD item in old.aircraft)
				{
					try
					{
						if (item != null)
						{
							mission.aircraft.Add(ConvertAircraft(item));
						}
					}
					catch (Exception e4)
					{
						LoadErrors.AddException(e4, "Failed to convert aircraft '" + (item?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.vehicles != null)
			{
				foreach (SavedVehicle_V5_OLD vehicle in old.vehicles)
				{
					try
					{
						if (vehicle != null)
						{
							mission.vehicles.Add(ConvertVehicle(vehicle));
						}
					}
					catch (Exception e5)
					{
						LoadErrors.AddException(e5, "Failed to convert vehicle '" + (vehicle?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.ships != null)
			{
				foreach (SavedShip_V5_OLD ship in old.ships)
				{
					try
					{
						if (ship != null)
						{
							mission.ships.Add(ConvertShip(ship));
						}
					}
					catch (Exception e6)
					{
						LoadErrors.AddException(e6, "Failed to convert ship '" + (ship?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.buildings != null)
			{
				foreach (SavedBuilding_V5_OLD building in old.buildings)
				{
					try
					{
						if (building != null)
						{
							mission.buildings.Add(ConvertBuilding(building));
						}
					}
					catch (Exception e7)
					{
						LoadErrors.AddException(e7, "Failed to convert building '" + (building?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.scenery != null)
			{
				foreach (SavedScenery_V5_OLD item2 in old.scenery)
				{
					try
					{
						if (item2 != null)
						{
							mission.scenery.Add(ConvertScenery(item2));
						}
					}
					catch (Exception e8)
					{
						LoadErrors.AddException(e8, "Failed to convert scenery '" + (item2?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.containers != null)
			{
				foreach (SavedContainer_V5_OLD container in old.containers)
				{
					try
					{
						if (container != null)
						{
							mission.containers.Add(ConvertContainer(container));
						}
					}
					catch (Exception e9)
					{
						LoadErrors.AddException(e9, "Failed to convert container '" + (container?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.missiles != null)
			{
				foreach (SavedMissile_V5_OLD missile in old.missiles)
				{
					try
					{
						if (missile != null)
						{
							mission.missiles.Add(ConvertMissile(missile));
						}
					}
					catch (Exception e10)
					{
						LoadErrors.AddException(e10, "Failed to convert missile '" + (missile?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.pilots != null)
			{
				foreach (SavedPilot_V5_OLD pilot in old.pilots)
				{
					try
					{
						if (pilot != null)
						{
							mission.pilots.Add(ConvertPilot(pilot));
						}
					}
					catch (Exception e11)
					{
						LoadErrors.AddException(e11, "Failed to convert pilot '" + (pilot?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.factions != null)
			{
				foreach (MissionFaction_V5_OLD faction in old.factions)
				{
					try
					{
						if (faction != null)
						{
							mission.factions.Add(ConvertFaction(faction));
						}
					}
					catch (Exception e12)
					{
						LoadErrors.AddException(e12, "Failed to convert faction '" + (faction?.factionName ?? "unknown") + "'");
					}
				}
			}
			if (old.airbases != null)
			{
				foreach (SavedAirbase_V5_OLD airbasis in old.airbases)
				{
					try
					{
						if (airbasis != null)
						{
							mission.airbases.Add(ConvertAirbase(airbasis));
						}
					}
					catch (Exception e13)
					{
						LoadErrors.AddException(e13, "Failed to convert airbase '" + (airbasis?.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.unitInventories != null)
			{
				foreach (UnitInventory_V5_OLD unitInventory in old.unitInventories)
				{
					try
					{
						if (unitInventory != null && !string.IsNullOrEmpty(unitInventory.AttachedUnitUniqueName))
						{
							SavedUnit savedUnit = FindSavedUnit(mission, unitInventory.AttachedUnitUniqueName);
							if (savedUnit != null)
							{
								savedUnit.inventory = ConvertUnitInventory(unitInventory);
							}
							else
							{
								LoadErrors.AddWarn("Unit inventory attached to '" + unitInventory.AttachedUnitUniqueName + "' could not be matched to any unit in mission.");
							}
						}
					}
					catch (Exception e14)
					{
						LoadErrors.AddException(e14, "Failed to convert unit inventory for '" + (unitInventory?.AttachedUnitUniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.objectives.Objectives != null)
			{
				foreach (SavedObjective_V5_OLD objective in old.objectives.Objectives)
				{
					try
					{
						SavedObjective savedObjective = ParseObjective(objective);
						if (savedObjective != null)
						{
							mission.objectives.Add(savedObjective);
						}
					}
					catch (Exception e15)
					{
						LoadErrors.AddException(e15, "Failed to parse objective '" + (objective.UniqueName ?? "unknown") + "'");
					}
				}
			}
			if (old.objectives.Outcomes != null)
			{
				foreach (SavedOutcome_V5_OLD outcome in old.objectives.Outcomes)
				{
					try
					{
						SavedOutcome savedOutcome = ParseOutcome(outcome);
						if (savedOutcome != null)
						{
							mission.outcomes.Add(savedOutcome);
						}
					}
					catch (Exception e16)
					{
						LoadErrors.AddException(e16, "Failed to parse outcome '" + (outcome.UniqueName ?? "unknown") + "'");
					}
				}
			}
			return mission;
		}

		private static MapKey ConvertMapKey(MapKey_V5_OLD old)
		{
			return new MapKey
			{
				Type = (MapKey.KeyType)old.Type,
				Path = old.Path
			};
		}

		private static Override<T> ConvertOverride<T>(Override_V5_OLD<T> old) where T : IEquatable<T>
		{
			return new Override<T>(old.IsOverride, old.Value);
		}

		private static Override<PositionRotation> ConvertOverridePosRot(Override_V5_OLD<PositionRotation_V5_OLD> old)
		{
			return new Override<PositionRotation>(old.IsOverride, new PositionRotation
			{
				Position = old.Value.Position,
				Rotation = old.Value.Rotation
			});
		}

		private static MissionEnvironment ConvertMissionEnvironment(MissionEnvironment_V5_OLD old)
		{
			if (old == null)
			{
				return new MissionEnvironment();
			}
			return new MissionEnvironment
			{
				timeOfDay = old.timeOfDay,
				timeFactor = old.timeFactor,
				weatherIntensity = old.weatherIntensity,
				cloudAltitude = old.cloudAltitude,
				windSpeed = old.windSpeed,
				windTurbulence = old.windTurbulence,
				windHeading = old.windHeading,
				windRandomHeading = old.windRandomHeading,
				moonPhase = old.moonPhase
			};
		}

		private static void ConvertSavedUnitBase(SavedUnit_V5_OLD old, SavedUnit newUnit)
		{
			newUnit.type = old.type;
			newUnit.faction = old.faction;
			newUnit.Rename(old.UniqueName);
			newUnit.globalPosition = old.globalPosition;
			newUnit.rotation = old.rotation;
			newUnit.CaptureStrength = ConvertOverride(old.CaptureStrength);
			newUnit.CaptureDefense = ConvertOverride(old.CaptureDefense);
		}

		private static SavedAircraft ConvertAircraft(SavedAircraft_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedAircraft savedAircraft = new SavedAircraft();
			ConvertSavedUnitBase(old, savedAircraft);
			savedAircraft.playerControlled = old.playerControlled;
			savedAircraft.playerControlledPriority = old.playerControlledPriority;
			savedAircraft.savedLoadout = ConvertSavedLoadout(old.savedLoadout);
			savedAircraft.livery = old.livery;
			savedAircraft.liveryType = old.liveryType;
			savedAircraft.liveryName = old.liveryName;
			savedAircraft.fuel = old.fuel;
			savedAircraft.skill = old.skill;
			savedAircraft.bravery = old.bravery;
			savedAircraft.startingSpeed = old.startingSpeed;
			return savedAircraft;
		}

		private static SavedLoadout ConvertSavedLoadout(SavedLoadout_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedLoadout savedLoadout = new SavedLoadout();
			if (old.Selected != null)
			{
				foreach (SavedLoadout_V5_OLD.SelectedMount_V5_OLD item in old.Selected)
				{
					savedLoadout.Selected.Add(new SavedLoadout.SelectedMount
					{
						Key = item.Key
					});
				}
			}
			return savedLoadout;
		}

		private static SavedVehicle ConvertVehicle(SavedVehicle_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedVehicle savedVehicle = new SavedVehicle();
			ConvertSavedUnitBase(old, savedVehicle);
			savedVehicle.holdPosition = old.holdPosition;
			savedVehicle.skill = old.skill;
			if (old.waypoints != null)
			{
				foreach (VehicleWaypoint_V5_OLD waypoint in old.waypoints)
				{
					savedVehicle.waypoints.Add(new VehicleWaypoint
					{
						position = waypoint.position,
						objective = waypoint.objective
					});
				}
			}
			return savedVehicle;
		}

		private static SavedShip ConvertShip(SavedShip_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedShip savedShip = new SavedShip();
			ConvertSavedUnitBase(old, savedShip);
			savedShip.holdPosition = old.holdPosition;
			savedShip.skill = old.skill;
			if (old.waypoints != null)
			{
				foreach (VehicleWaypoint_V5_OLD waypoint in old.waypoints)
				{
					savedShip.waypoints.Add(new VehicleWaypoint
					{
						position = waypoint.position,
						objective = waypoint.objective
					});
				}
			}
			return savedShip;
		}

		private static SavedBuilding ConvertBuilding(SavedBuilding_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedBuilding savedBuilding = new SavedBuilding();
			ConvertSavedUnitBase(old, savedBuilding);
			savedBuilding.capturable = old.capturable;
			savedBuilding.Airbase = old.Airbase;
			if (old.factoryOptions != null)
			{
				savedBuilding.factoryOptions = new SavedBuilding.FactoryOptions
				{
					productionType = old.factoryOptions.productionType,
					productionTime = old.factoryOptions.productionTime
				};
			}
			if (old.placementOffset != 0f)
			{
				savedBuilding.globalPosition += Vector3.up * old.placementOffset;
			}
			return savedBuilding;
		}

		private static SavedScenery ConvertScenery(SavedScenery_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedScenery savedScenery = new SavedScenery();
			ConvertSavedUnitBase(old, savedScenery);
			savedScenery.indestructible = old.indestructible;
			return savedScenery;
		}

		private static SavedContainer ConvertContainer(SavedContainer_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedContainer savedContainer = new SavedContainer();
			ConvertSavedUnitBase(old, savedContainer);
			return savedContainer;
		}

		private static SavedMissile ConvertMissile(SavedMissile_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedMissile savedMissile = new SavedMissile();
			ConvertSavedUnitBase(old, savedMissile);
			savedMissile.startingSpeed = old.startingSpeed;
			savedMissile.targetUnitName = old.targetUnitName;
			savedMissile.guidingUnit = old.guidingUnit;
			return savedMissile;
		}

		private static SavedPilot ConvertPilot(SavedPilot_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedPilot savedPilot = new SavedPilot();
			ConvertSavedUnitBase(old, savedPilot);
			return savedPilot;
		}

		private static MissionFaction ConvertFaction(MissionFaction_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			MissionFaction missionFaction = new MissionFaction();
			missionFaction.factionName = old.factionName;
			missionFaction.preventJoin = old.preventJoin;
			missionFaction.preventDonation = old.preventDonation;
			if (old.supplies != null)
			{
				foreach (FactionSupply_V5_OLD supply in old.supplies)
				{
					missionFaction.supplies.Add(new UnitCount(supply.unitType, supply.count));
				}
			}
			missionFaction.startingBalance = old.startingBalance;
			missionFaction.playerJoinAllowance = old.playerJoinAllowance;
			missionFaction.playerTaxRate = old.playerTaxRate;
			missionFaction.regularIncome = old.regularIncome;
			missionFaction.excessFundsDistributePercent = old.excessFundsDistributePercent;
			missionFaction.killReward = old.killReward;
			missionFaction.airSkillMultiplier = old.airSkillMultiplier;
			missionFaction.surfaceSkillMultiplier = old.surfaceSkillMultiplier;
			missionFaction.startingWarheads = old.startingWarheads;
			missionFaction.reserveWarheads = old.reserveWarheads;
			missionFaction.reserveAirframes = old.reserveAirframes;
			missionFaction.extraReservesPerPlayer = old.extraReservesPerPlayer;
			missionFaction.AIAircraftLimit = old.AIAircraftLimit;
			missionFaction.reduceAIPerFriendlyPlayer = old.reduceAIPerFriendlyPlayer;
			missionFaction.addAIPerEnemyPlayer = old.addAIPerEnemyPlayer;
			if (old.restrictions != null)
			{
				missionFaction.restrictions = new Restrictions();
				if (old.restrictions.aircraft != null)
				{
					missionFaction.restrictions.aircraft.AddRange(old.restrictions.aircraft);
				}
				if (old.restrictions.weapons != null)
				{
					missionFaction.restrictions.weapons.AddRange(old.restrictions.weapons);
				}
			}
			missionFaction.cameraStartPosition = ConvertOverridePosRot(old.cameraStartPosition);
			return missionFaction;
		}

		private static SavedAirbase ConvertAirbase(SavedAirbase_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedAirbase savedAirbase = new SavedAirbase();
			savedAirbase.IsOverride = old.IsOverride;
			savedAirbase.faction = old.faction;
			savedAirbase.UniqueName = old.UniqueName;
			savedAirbase.DisplayName = old.DisplayName;
			savedAirbase.Disabled = old.Disabled;
			savedAirbase.Capturable = old.Capturable;
			savedAirbase.CaptureDefense = old.CaptureDefense;
			savedAirbase.CaptureRange = old.CaptureRange;
			savedAirbase.Center = old.Center;
			savedAirbase.SelectionPosition = old.SelectionPosition;
			savedAirbase.Tower = old.Tower;
			if (old.VerticalLandingPoints != null)
			{
				savedAirbase.VerticalLandingPoints = new List<GlobalPosition>(old.VerticalLandingPoints);
			}
			if (old.ServicePoints != null)
			{
				savedAirbase.ServicePoints = new List<GlobalPosition>(old.ServicePoints);
			}
			savedAirbase.roads = ConvertRoadNetwork(old.roads);
			if (old.runways != null)
			{
				foreach (SavedRunway_V5_OLD runway in old.runways)
				{
					savedAirbase.runways.Add(new SavedRunway
					{
						Name = runway.Name,
						Reversable = runway.Reversable,
						Takeoff = runway.Takeoff,
						Landing = runway.Landing,
						Arrestor = runway.Arrestor,
						SkiJump = runway.SkiJump,
						Width = runway.Width,
						Start = runway.Start,
						End = runway.End,
						exitPoints = ((runway.exitPoints != null) ? ((GlobalPosition[])runway.exitPoints.Clone()) : new GlobalPosition[0])
					});
				}
			}
			return savedAirbase;
		}

		private static RoadNetwork ConvertRoadNetwork(RoadNetwork_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			RoadNetwork roadNetwork = new RoadNetwork();
			if (old.roads != null)
			{
				foreach (Road_V5_OLD road2 in old.roads)
				{
					if (road2 != null)
					{
						Road road = new Road();
						road.points = ((road2.points != null) ? new List<GlobalPosition>(road2.points) : new List<GlobalPosition>());
						road.length = road2.length;
						road.SetBridge(road2.bridge);
						road.UpdateBB();
						roadNetwork.roads.Add(road);
					}
				}
			}
			return roadNetwork;
		}

		private static SavedUnit FindSavedUnit(Mission mission, string uniqueName)
		{
			foreach (SavedAircraft item in mission.aircraft)
			{
				if (item.UniqueName == uniqueName)
				{
					return item;
				}
			}
			foreach (SavedVehicle vehicle in mission.vehicles)
			{
				if (vehicle.UniqueName == uniqueName)
				{
					return vehicle;
				}
			}
			foreach (SavedShip ship in mission.ships)
			{
				if (ship.UniqueName == uniqueName)
				{
					return ship;
				}
			}
			foreach (SavedBuilding building in mission.buildings)
			{
				if (building.UniqueName == uniqueName)
				{
					return building;
				}
			}
			foreach (SavedScenery item2 in mission.scenery)
			{
				if (item2.UniqueName == uniqueName)
				{
					return item2;
				}
			}
			foreach (SavedContainer container in mission.containers)
			{
				if (container.UniqueName == uniqueName)
				{
					return container;
				}
			}
			foreach (SavedMissile missile in mission.missiles)
			{
				if (missile.UniqueName == uniqueName)
				{
					return missile;
				}
			}
			foreach (SavedPilot pilot in mission.pilots)
			{
				if (pilot.UniqueName == uniqueName)
				{
					return pilot;
				}
			}
			return null;
		}

		private static SavedInventory ConvertUnitInventory(UnitInventory_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			SavedInventory savedInventory = new SavedInventory();
			if (old.StoredList != null)
			{
				foreach (StoredUnitCount_V5_OLD stored in old.StoredList)
				{
					savedInventory.StoredList.Add(new UnitCount
					{
						UnitType = stored.UnitType,
						Count = stored.Count
					});
				}
			}
			if (savedInventory.StoredList.Count == 0)
			{
				return null;
			}
			return savedInventory;
		}

		private static MissionSettings ConvertMissionSettings(MissionSettings_V5_OLD old)
		{
			if (old == null)
			{
				return null;
			}
			MissionSettings missionSettings = new MissionSettings();
			missionSettings.description = old.description;
			missionSettings.allowEventContent = old.allowEventContent;
			if (old.Tags != null)
			{
				foreach (MissionTag_V5_OLD tag in old.Tags)
				{
					missionSettings.Tags.Add(new MissionTag(tag.Tag, tag.Color, tag.SortOrder));
				}
			}
			missionSettings.playerMode = (PlayerMode)old.playerMode;
			missionSettings.allowRespawn = old.allowRespawn;
			missionSettings.playerStartingRank = old.playerStartingRank;
			missionSettings.rankMultiplier = old.rankMultiplier;
			missionSettings.successfulSortieBonus = old.successfulSortieBonus;
			missionSettings.nuclearEscalationThreshold = old.nuclearEscalationThreshold;
			missionSettings.strategicEscalationThreshold = old.strategicEscalationThreshold;
			missionSettings.minRankTacticalWarhead = old.minRankTacticalWarhead;
			missionSettings.minRankStrategicWarhead = old.minRankStrategicWarhead;
			missionSettings.cameraStartPosition = ConvertOverridePosRot(old.cameraStartPosition);
			missionSettings.missionRoads = ConvertRoadNetwork(old.missionRoads);
			missionSettings.missionSeaLanes = ConvertRoadNetwork(old.missionSeaLanes);
			missionSettings.wrecksMaxNumber = old.wrecksMaxNumber;
			missionSettings.wrecksDecayTime = old.wrecksDecayTime;
			return missionSettings;
		}

		private SavedObjective ParseObjective(SavedObjective_V5_OLD old)
		{
			ObjectiveDataReader objectiveDataReader = new ObjectiveDataReader(old.Data);
			SavedObjective savedObjective;
			switch ((ObjectiveType)old.Type)
			{
			case ObjectiveType.DestroyUnits:
				savedObjective = new DestroyUnitSavedObjective
				{
					completeOrder = objectiveDataReader.ReadEnum(CompleteOrder.CompleteAll),
					completeSomePercent = objectiveDataReader.ReadFloat(0.5f),
					targetUnits = objectiveDataReader.ReadStringList()
				};
				break;
			case ObjectiveType.ReachUnits:
			{
				ReachUnitsSavedObjective reachUnitsSavedObjective = new ReachUnitsSavedObjective();
				reachUnitsSavedObjective.completeOrder = objectiveDataReader.ReadEnum(CompleteOrder.InOrder);
				reachUnitsSavedObjective.completeSomePercent = objectiveDataReader.ReadFloat(0.5f);
				while (objectiveDataReader.HasMore)
				{
					ObjectiveData_V5_OLD objectiveData_V5_OLD2 = objectiveDataReader.ReadRaw();
					reachUnitsSavedObjective.targets.Add(new SavedReachUnitData
					{
						Range = objectiveData_V5_OLD2.FloatValue,
						TargetUnit = objectiveData_V5_OLD2.StringValue
					});
				}
				savedObjective = reachUnitsSavedObjective;
				break;
			}
			case ObjectiveType.ReachWaypoints:
			{
				ReachWaypointsSavedObjective reachWaypointsSavedObjective = new ReachWaypointsSavedObjective();
				reachWaypointsSavedObjective.completeOrder = objectiveDataReader.ReadEnum(CompleteOrder.InOrder);
				reachWaypointsSavedObjective.completeSomePercent = objectiveDataReader.ReadFloat(0.5f);
				while (objectiveDataReader.HasMore)
				{
					ObjectiveData_V5_OLD objectiveData_V5_OLD = objectiveDataReader.ReadRaw();
					reachWaypointsSavedObjective.waypoints.Add(new SavedWaypoint
					{
						Range = objectiveData_V5_OLD.FloatValue,
						Position = objectiveData_V5_OLD.VectorValue
					});
				}
				savedObjective = reachWaypointsSavedObjective;
				break;
			}
			case ObjectiveType.WaitSeconds:
				savedObjective = new WaitTimeSavedObjective
				{
					seconds = objectiveDataReader.ReadFloat(5f)
				};
				break;
			case ObjectiveType.CaptureAirbase:
				savedObjective = new CaptureAirbaseSavedObjective
				{
					completeOrder = objectiveDataReader.ReadEnum(CompleteOrder.CompleteAll),
					completeSomePercent = objectiveDataReader.ReadFloat(0.5f),
					targetAirbases = objectiveDataReader.ReadStringList()
				};
				break;
			case ObjectiveType.DialogueBox:
				savedObjective = new DialogueBoxSavedObjective
				{
					title = objectiveDataReader.ReadString(),
					body = objectiveDataReader.ReadString(),
					button = objectiveDataReader.ReadString("ok"),
					factionOnly = objectiveDataReader.ReadBool()
				};
				break;
			case ObjectiveType.CompleteOtherObjective:
				savedObjective = new CompleteOtherObjectiveSavedObjective
				{
					completeOrder = objectiveDataReader.ReadEnum(CompleteOrder.CompleteAll),
					completeSomePercent = objectiveDataReader.ReadFloat(0.5f),
					targetObjectives = objectiveDataReader.ReadStringList()
				};
				break;
			case ObjectiveType.SpotUnit:
				savedObjective = new SpotUnitSavedObjective
				{
					completeOrder = objectiveDataReader.ReadEnum(CompleteOrder.InOrder),
					completeSomePercent = objectiveDataReader.ReadFloat(0.5f),
					targetUnits = objectiveDataReader.ReadStringList()
				};
				break;
			case ObjectiveType.CrashAircraft:
				savedObjective = new CrashAircraftSavedObjective
				{
					livesPerPlayer = objectiveDataReader.ReadInt(),
					extraLives = objectiveDataReader.ReadInt(1),
					includeDestroy = objectiveDataReader.ReadBool(defaultValue: true),
					includeEject = objectiveDataReader.ReadBool(defaultValue: true)
				};
				break;
			case ObjectiveType.SuccessfulSortie:
				savedObjective = new SuccessfulSortieSavedObjective
				{
					minimumScore = objectiveDataReader.ReadFloat()
				};
				break;
			case ObjectiveType.None:
				savedObjective = new NoSavedObjective();
				break;
			default:
				LoadErrors.AddWarn($"Unknown or unsupported objective type '{(int)old.Type}' (TypeName: '{old.TypeName}', UniqueName: '{old.UniqueName}'). Skipping objective.");
				savedObjective = new NoSavedObjective();
				break;
			}
			savedObjective.UniqueName = old.UniqueName;
			savedObjective.Faction = old.Faction;
			savedObjective.DisplayName = old.DisplayName;
			savedObjective.Hidden = old.Hidden;
			savedObjective.Outcomes = ((old.Outcomes != null) ? new List<string>(old.Outcomes) : new List<string>());
			return savedObjective;
		}

		private SavedOutcome ParseOutcome(SavedOutcome_V5_OLD old)
		{
			ObjectiveDataReader objectiveDataReader = new ObjectiveDataReader(old.Data);
			SavedOutcome savedOutcome;
			switch ((OutcomeType)old.Type)
			{
			case OutcomeType.StartObjective:
				savedOutcome = new StartObjectiveSavedOutcome
				{
					objectivesToStart = objectiveDataReader.ReadStringList()
				};
				break;
			case OutcomeType.StopOrCompleteObjective:
				savedOutcome = new CompleteObjectiveSavedOutcome
				{
					options = objectiveDataReader.ReadEnum(CompleteObjectiveOutcome.Options.Stop),
					objectivesToStart = objectiveDataReader.ReadStringList()
				};
				break;
			case OutcomeType.ShowMessage:
				savedOutcome = new ShowMessageSavedOutcome
				{
					Message = objectiveDataReader.ReadString(),
					PlaySound = objectiveDataReader.ReadBool(),
					ObjectiveFactionOnly = objectiveDataReader.ReadBool()
				};
				break;
			case OutcomeType.GiveScore:
				savedOutcome = new GiveScoreSavedOutcome
				{
					bothFactions = objectiveDataReader.ReadBool(),
					playerFundsType = objectiveDataReader.ReadEnum(ChangeType.Add),
					playerFunds = objectiveDataReader.ReadFloat(),
					factionFundsType = objectiveDataReader.ReadEnum(ChangeType.Add),
					factionFunds = objectiveDataReader.ReadFloat(),
					playerScoreType = objectiveDataReader.ReadEnum(ChangeType.Add),
					playerScore = objectiveDataReader.ReadFloat(),
					rankType = objectiveDataReader.ReadEnum(ChangeType.Add),
					rank = objectiveDataReader.ReadInt(),
					factionScoreType = objectiveDataReader.ReadEnum(ChangeType.Add),
					factionScore = objectiveDataReader.ReadFloat()
				};
				break;
			case OutcomeType.SpawnUnit:
				savedOutcome = new SpawnUnitSavedOutcome
				{
					UnitsToSpawn = objectiveDataReader.ReadStringList()
				};
				break;
			case OutcomeType.RemoveUnit:
				savedOutcome = new RemoveUnitSavedOutcome
				{
					UnitsToRemove = objectiveDataReader.ReadStringList()
				};
				break;
			case OutcomeType.RevealUnit:
				savedOutcome = new RevealUnitSavedOutcome
				{
					UnitsToReveal = objectiveDataReader.ReadStringList()
				};
				break;
			case OutcomeType.EndGame:
				savedOutcome = new EndGameSavedOutcome
				{
					endType = objectiveDataReader.ReadEnum(EndType.Victory),
					endDelay = objectiveDataReader.ReadFloat()
				};
				break;
			case OutcomeType.ModifyAirbase:
				savedOutcome = new ModifyAirbaseSavedOutcome
				{
					airbase = objectiveDataReader.ReadString(),
					faction = objectiveDataReader.ReadStringOverride(),
					disabled = objectiveDataReader.ReadBoolOverride(),
					capturable = objectiveDataReader.ReadBoolOverride(),
					captureDefense = objectiveDataReader.ReadFloatOverride()
				};
				break;
			case OutcomeType.ModifyEnvironment:
				savedOutcome = new ModifyEnvironmentSavedOutcome
				{
					timeOfDay = objectiveDataReader.ReadFloatOverride(),
					weather = objectiveDataReader.ReadFloatOverride(),
					cloudAltitude = objectiveDataReader.ReadFloatOverride(),
					windSpeed = objectiveDataReader.ReadFloatOverride(),
					windTurbulence = objectiveDataReader.ReadFloatOverride(),
					windHeading = objectiveDataReader.ReadFloatOverride()
				};
				break;
			case OutcomeType.ModifyFaction:
				savedOutcome = new ModifyFactionSavedOutcome
				{
					bothFactions = objectiveDataReader.ReadBool(),
					excessFundsThreshold = objectiveDataReader.ReadFloatOverride(),
					playerJoinAllowance = objectiveDataReader.ReadFloatOverride(),
					playerTaxRate = objectiveDataReader.ReadFloatOverride(),
					regularIncome = objectiveDataReader.ReadFloatOverride(),
					killReward = objectiveDataReader.ReadFloatOverride(),
					preventDonation = objectiveDataReader.ReadBoolOverride(),
					aiAircraftLimit = objectiveDataReader.ReadFloatOverride(),
					reduceAIPerFriendlyPlayer = objectiveDataReader.ReadFloatOverride(),
					addAIPerEnemyPlayer = objectiveDataReader.ReadFloatOverride(),
					warheadsReserve = objectiveDataReader.ReadFloatOverride(),
					reserveAirframes = objectiveDataReader.ReadFloatOverride(),
					extraReservesPerPlayer = objectiveDataReader.ReadFloatOverride(),
					excessFundsDistributePercent = objectiveDataReader.ReadFloatOverride(),
					preventJoin = objectiveDataReader.ReadBoolOverride()
				};
				break;
			default:
				LoadErrors.AddWarn($"Unknown or unsupported outcome type '{(int)old.Type}' (TypeName: '{old.TypeName}', UniqueName: '{old.UniqueName}'). Skipping outcome.");
				savedOutcome = new NoSavedOutcome();
				break;
			}
			savedOutcome.UniqueName = old.UniqueName;
			return savedOutcome;
		}
	}
}
