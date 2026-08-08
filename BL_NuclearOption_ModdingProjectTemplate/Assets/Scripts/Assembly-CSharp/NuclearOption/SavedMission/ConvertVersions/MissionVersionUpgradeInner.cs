using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Obsolete("V5", false)]
	internal class MissionVersionUpgradeInner
	{
		private MissionVersionUpgradeInner()
		{
		}

		public static Mission FromV5Inner(string json, string missionName = null)
		{
			return Upgrade(JsonUtility.FromJson<Mission_V5_OLD>(json), missionName);
		}

		private static Mission Upgrade(Mission_V5_OLD oldMission, string missionName = null)
		{
			string text = (string.IsNullOrEmpty(missionName) ? "" : ("'" + missionName + "' "));
			if (oldMission.JsonVersion == 0)
			{
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Mission had no version, setting to V1");
				oldMission.JsonVersion = 1;
			}
			if (oldMission.JsonVersion == 1)
			{
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Converting V1 Mission to V2");
				Convert_v1_to_v2(oldMission);
				oldMission.JsonVersion = 2;
			}
			if (oldMission.JsonVersion == 2)
			{
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Converting V2 Mission to V3");
				Convert_v2_to_v3(oldMission);
				oldMission.JsonVersion = 3;
			}
			if (oldMission.JsonVersion == 3)
			{
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Converting V3 Mission to V4");
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Converting Buildings to Scenery");
				Convert_v3_to_v4(oldMission);
				oldMission.JsonVersion = 4;
			}
			if (oldMission.JsonVersion == 4)
			{
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Converting V4 Mission to V5");
				ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Objectives for Complete Some");
				Convert_v4_to_v5(oldMission);
				oldMission.JsonVersion = 5;
			}
			ColorLog<MissionVersionUpgradeInner>.InfoWarn(text + "Converting V5 Mission to V6");
			var (result, loadErrors) = MissionConverter_V5_to_V6.Convert(oldMission);
			if (loadErrors != null && loadErrors.AnyMessages())
			{
				loadErrors.LogAllErrors(missionName);
			}
			return result;
		}

		private static void Convert_v1_to_v2(Mission_V5_OLD mission)
		{
			mission.objectives = ConvertOldMissions.ConvertObjective(mission.factions, GetAllSavedUnits(mission));
		}

		private static IReadOnlyList<SavedUnit_V5_OLD> GetAllSavedUnits(Mission_V5_OLD mission)
		{
			List<SavedUnit_V5_OLD> list = new List<SavedUnit_V5_OLD>();
			list.AddRange(mission.aircraft);
			list.AddRange(mission.vehicles);
			list.AddRange(mission.ships);
			list.AddRange(mission.buildings);
			list.AddRange(mission.scenery);
			list.AddRange(mission.containers);
			list.AddRange(mission.missiles);
			list.AddRange(mission.pilots);
			return list;
		}

		private static void Convert_v2_to_v3(Mission_V5_OLD mission)
		{
			V2LoadoutMap v2LoadoutMap = V2LoadoutMap.Load();
			foreach (SavedAircraft_V5_OLD savedAircraft in mission.aircraft)
			{
				V2LoadoutMap.UnitLoadout unitLoadout = v2LoadoutMap.Units.FirstOrDefault((V2LoadoutMap.UnitLoadout x) => x.UnitKey == savedAircraft.type);
				if (unitLoadout == null)
				{
					Debug.LogWarning("Could not find unit with name " + savedAircraft.type);
					continue;
				}
				SavedLoadout_V5_OLD savedLoadout_V5_OLD = new SavedLoadout_V5_OLD();
				savedAircraft.savedLoadout = savedLoadout_V5_OLD;
				LoadoutOld_V5_OLD loadout = savedAircraft.loadout;
				if (loadout.weaponSelections == null)
				{
					continue;
				}
				int num = Math.Min(loadout.weaponSelections.Count, unitLoadout.HardPoints.Count);
				for (int num2 = 0; num2 < num; num2++)
				{
					byte b = loadout.weaponSelections[num2];
					V2LoadoutMap.HardPoint hardPoint = unitLoadout.HardPoints[num2];
					if (b < hardPoint.Options.Count)
					{
						string key = hardPoint.Options[b];
						savedLoadout_V5_OLD.Selected.Add(new SavedLoadout_V5_OLD.SelectedMount_V5_OLD
						{
							Key = key
						});
						continue;
					}
					Debug.LogWarning($"Old weapon index was out of range for {savedAircraft.type}, hardpoint index {num2}. Old index:{b} V2 option count: {hardPoint.Options.Count}");
					savedLoadout_V5_OLD.Selected.Add(new SavedLoadout_V5_OLD.SelectedMount_V5_OLD
					{
						Key = null
					});
				}
			}
		}

		private static void Convert_v3_to_v4(Mission_V5_OLD mission)
		{
			HashSet<string> hashSet = new HashSet<string>(Encyclopedia.i.buildings.Select((BuildingDefinition x) => x.jsonKey));
			HashSet<string> hashSet2 = new HashSet<string>(Encyclopedia.i.scenery.Select((SceneryDefinition x) => x.jsonKey));
			List<SavedBuilding_V5_OLD> list = new List<SavedBuilding_V5_OLD>();
			foreach (SavedBuilding_V5_OLD building in mission.buildings)
			{
				if (!hashSet.Contains(building.type))
				{
					if (hashSet2.Contains(building.type))
					{
						list.Add(building);
					}
					else
					{
						Debug.LogError("building now found in building or scenery list: " + building.type);
					}
				}
			}
			foreach (SavedBuilding_V5_OLD item2 in list)
			{
				ColorLog<SavedScenery_V5_OLD>.Info("Converting " + item2.UniqueName + " (" + item2.type + ") to Scenery");
				SavedScenery_V5_OLD item = new SavedScenery_V5_OLD
				{
					type = item2.type,
					UniqueName = item2.UniqueName,
					globalPosition = item2.globalPosition,
					rotation = item2.rotation
				};
				mission.scenery.Add(item);
				mission.buildings.Remove(item2);
			}
		}

		private static void Convert_v4_to_v5(Mission_V5_OLD mission)
		{
			for (int i = 0; i < mission.objectives.Objectives.Count; i++)
			{
				SavedObjective_V5_OLD objective = mission.objectives.Objectives[i];
				Convert_v4_to_v5_old(ref objective);
				mission.objectives.Objectives[i] = objective;
			}
		}

		private static void Convert_v4_to_v5_old(ref SavedObjective_V5_OLD objective)
		{
			switch (objective.Type)
			{
			case ObjectiveType_V5_OLD.DestroyUnits:
			case ObjectiveType_V5_OLD.ReachUnits:
			case ObjectiveType_V5_OLD.ReachWaypoints:
			case ObjectiveType_V5_OLD.CaptureAirbase:
			case ObjectiveType_V5_OLD.CompleteOtherObjective:
			case ObjectiveType_V5_OLD.SpotUnit:
			{
				List<ObjectiveData_V5_OLD> data = objective.Data;
				if (data.Count > 0)
				{
					ObjectiveData_V5_OLD objectiveData_V5_OLD = data.Last();
					if (string.IsNullOrEmpty(objectiveData_V5_OLD.StringValue) && objectiveData_V5_OLD.VectorValue == Vector3.zero && Mathf.Approximately(objectiveData_V5_OLD.FloatValue, 0.5f))
					{
						data.RemoveAt(data.Count - 1);
					}
				}
				ObjectiveData_V5_OLD item = new ObjectiveData_V5_OLD
				{
					FloatValue = 0.5f
				};
				if (data.Count >= 1)
				{
					data.Insert(1, item);
				}
				else
				{
					data.Add(item);
				}
				break;
			}
			case ObjectiveType_V5_OLD.WaitSeconds:
			case (ObjectiveType_V5_OLD)5:
			case ObjectiveType_V5_OLD.DialogueBox:
				break;
			}
		}
	}
}
