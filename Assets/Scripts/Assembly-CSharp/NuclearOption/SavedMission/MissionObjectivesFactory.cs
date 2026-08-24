using System;
using System.Collections.Generic;
using System.Linq;
using Mirage;
using NuclearOption.SavedMission.Outcomes;

namespace NuclearOption.SavedMission
{
	public class MissionObjectivesFactory
	{
		public static string MissionStartName => "Mission Start";

		public static string MissionStartSpawnUnitName => "Mission Start Default Spawn Units";

		public static void AssertSceneAirbaseRegistered()
		{
		}

		public static MissionObjectives Load(Mission mission)
		{
			LoadErrors loadErrors = mission.LoadErrors;
			MissionLookups missionLookups = new MissionLookups(loadErrors);
			Objective[] array = new Objective[mission.objectives.Count];
			Outcome[] array2 = new Outcome[mission.outcomes.Count];
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(mission, item, includeBuiltIn: true);
			foreach (SavedUnit item2 in item)
			{
				if (!string.IsNullOrEmpty(item2.UniqueName))
				{
					missionLookups.SavedUnits.Add(item2.UniqueName, item2);
				}
			}
			foreach (SavedAirbase airbasis in mission.airbases)
			{
				if (!string.IsNullOrEmpty(airbasis.UniqueName))
				{
					missionLookups.Airbases.Add(airbasis.UniqueName, airbasis);
				}
			}
			AssertSceneAirbaseRegistered();
			foreach (KeyValuePair<string, Airbase> item3 in FactionRegistry.airbaseLookup)
			{
				string key = item3.Key;
				Airbase value = item3.Value;
				missionLookups.Airbases.TryAdd(key, value.SavedAirbase);
			}
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = SavedObjective.Create(mission.objectives[i]);
				missionLookups.Objectives.Add(mission.objectives[i].UniqueName, array[i]);
			}
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = SavedOutcome.Create(mission.outcomes[j]);
				missionLookups.Outcomes.Add(mission.outcomes[j].UniqueName, array2[j]);
			}
			for (int k = 0; k < array.Length; k++)
			{
				try
				{
					array[k].Load(missionLookups);
				}
				catch (Exception e)
				{
					loadErrors.AddException(e, "Error loading objective '" + array[k]?.SavedObjective?.UniqueName + "'");
				}
			}
			for (int l = 0; l < array2.Length; l++)
			{
				try
				{
					array2[l].Load(missionLookups);
				}
				catch (Exception e2)
				{
					loadErrors.AddException(e2, "Error loading outcome '" + array2[l]?.SavedOutcome?.UniqueName + "'");
				}
			}
			if (!missionLookups.Objectives.TryGetValue(MissionStartName, out var value2))
			{
				loadErrors.AddError("Mission must have objective with name " + MissionStartName);
			}
			if (loadErrors.Exceptions.Count > 0 || loadErrors.Errors.Count > 0)
			{
				throw new MissionLoadException(loadErrors);
			}
			return new MissionObjectives(array.ToList(), array2.ToList(), value2);
		}

		public static void AddStartingUnits(MissionObjectives missionObjectives, IReadOnlyList<SavedUnit> allUnits)
		{
			AddStartingUnits(missionObjectives.StartObjective, missionObjectives.AllOutcomes, allUnits);
		}

		private static void AddStartingUnits(Objective startObjective, IReadOnlyList<Outcome> outcomes, IReadOnlyList<SavedUnit> allUnits)
		{
			HashSet<SavedUnit> unspawnedUnits = GetUnspawnedUnits(outcomes, allUnits);
			if (unspawnedUnits.Count == 0)
			{
				return;
			}
			SpawnUnitOutcome spawnUnitOutcome = new SpawnUnitOutcome(new SpawnUnitSavedOutcome
			{
				UniqueName = MissionStartSpawnUnitName
			})
			{
				UnitsToSpawn = new List<SavedUnit>(unspawnedUnits)
			};
			spawnUnitOutcome.UnitsToSpawn.Sort(delegate(SavedUnit x, SavedUnit y)
			{
				if (!(x is SavedAircraft x2))
				{
					return 1;
				}
				return (!(y is SavedAircraft y2)) ? (-1) : Spawner.SortControlledAircraft(x2, y2);
			});
			startObjective.Outcomes.Add(spawnUnitOutcome);
		}

		private static HashSet<SavedUnit> GetUnspawnedUnits(IReadOnlyList<Outcome> outcomes, IReadOnlyList<SavedUnit> allUnits)
		{
			HashSet<SavedUnit> hashSet = new HashSet<SavedUnit>(allUnits.Where((SavedUnit x) => x.PlacementType == PlacementType.Custom));
			foreach (Outcome outcome in outcomes)
			{
				if (!(outcome is SpawnUnitOutcome spawnUnitOutcome))
				{
					continue;
				}
				foreach (SavedUnit item in spawnUnitOutcome.UnitsToSpawn)
				{
					hashSet.Remove(item);
				}
			}
			return hashSet;
		}

		public static void Save(Mission mission, MissionObjectives runtime)
		{
			mission.SaveErrors = new LoadErrors();
			Dictionary<string, Objective> dictionary = new Dictionary<string, Objective>();
			Dictionary<string, Outcome> dictionary2 = new Dictionary<string, Outcome>();
			foreach (Outcome allOutcome in runtime.AllOutcomes)
			{
				string uniqueName = allOutcome.SavedOutcome.UniqueName;
				if (!dictionary2.TryAdd(uniqueName, allOutcome))
				{
					mission.SaveErrors.AddError("Duplicate Outcome UniqueName '" + uniqueName + "' during Save. Skipping duplicate.");
				}
			}
			foreach (Objective allObjective in runtime.AllObjectives)
			{
				string uniqueName2 = allObjective.SavedObjective.UniqueName;
				if (!dictionary.TryAdd(uniqueName2, allObjective))
				{
					mission.SaveErrors.AddError("Duplicate Objective UniqueName '" + uniqueName2 + "' during Save. Skipping duplicate.");
					continue;
				}
				foreach (Outcome outcome in allObjective.Outcomes)
				{
					if (dictionary2.TryGetValue(outcome.SavedOutcome.UniqueName, out var value))
					{
						if (value != outcome)
						{
							mission.SaveErrors.AddError("Multiple Outcomes with same UniqueName '" + outcome.SavedOutcome.UniqueName + "'");
						}
					}
					else
					{
						mission.SaveErrors.AddError("Outcome was in AllObjectives list but not in AllOutcomes");
					}
				}
			}
			foreach (Objective value3 in dictionary.Values)
			{
				try
				{
					value3.Save();
				}
				catch (Exception e)
				{
					mission.SaveErrors.AddException(e, "Error saving objective '" + value3.SavedObjective?.UniqueName + "'");
				}
			}
			foreach (Outcome value4 in dictionary2.Values)
			{
				try
				{
					value4.Save();
				}
				catch (Exception e2)
				{
					mission.SaveErrors.AddException(e2, "Error saving outcome '" + value4.SavedOutcome?.UniqueName + "'");
				}
			}
			mission.objectives.Clear();
			mission.outcomes.Clear();
			if (dictionary.TryGetValue(MissionStartName, out var value2))
			{
				value2.SavedObjective.Outcomes.Remove(MissionStartSpawnUnitName);
			}
			dictionary2.Remove(MissionStartSpawnUnitName);
			mission.objectives.AddRange(dictionary.Values.Select((Objective x) => x.SavedObjective));
			mission.outcomes.AddRange(dictionary2.Values.Select((Outcome x) => x.SavedOutcome));
		}
	}
}
