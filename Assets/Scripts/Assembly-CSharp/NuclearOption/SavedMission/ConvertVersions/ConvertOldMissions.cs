using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Obsolete("V5", true)]
	public static class ConvertOldMissions
	{
		[Obsolete("V5", true)]
		private class AllObjectives_V5_OLD
		{
			public List<SavedObjective_V5_OLD> objectives = new List<SavedObjective_V5_OLD>();

			public List<SavedOutcome_V5_OLD> outcomes = new List<SavedOutcome_V5_OLD>();

			private bool hasStart;

			public SavedMissionObjectives_V5_OLD ToMissionObjectives()
			{
				return new SavedMissionObjectives_V5_OLD
				{
					Objectives = objectives,
					Outcomes = outcomes
				};
			}

			public void Add(ref SavedObjective_V5_OLD objective)
			{
				bool num = hasStart && objective.UniqueName == MissionObjectivesFactory.MissionStartName;
				SaveHelper.MakeUnique(ref objective.UniqueName, objectives.Select((SavedObjective_V5_OLD x) => x.UniqueName));
				objectives.Add(objective);
				if (num)
				{
					SavedOutcome_V5_OLD outcome = new SavedOutcome_V5_OLD
					{
						UniqueName = "Start_" + objective.UniqueName,
						Type = OutcomeType_V5_OLD.StartObjective,
						Data = new List<ObjectiveData_V5_OLD>
						{
							new ObjectiveData_V5_OLD
							{
								StringValue = objective.UniqueName
							}
						}
					};
					Add(ref outcome);
					objectives[0].Outcomes.Add(outcome.UniqueName);
				}
			}

			public void Add(ref SavedOutcome_V5_OLD outcome)
			{
				SaveHelper.MakeUnique(ref outcome.UniqueName, outcomes.Select((SavedOutcome_V5_OLD x) => x.UniqueName));
				outcomes.Add(outcome);
			}

			internal void SetStartMission(SavedObjective_V5_OLD start)
			{
				Add(ref start);
				hasStart = true;
			}
		}

		public static SavedMissionObjectives_V5_OLD ConvertObjective(List<MissionFaction_V5_OLD> factions, IReadOnlyList<SavedUnit_V5_OLD> units)
		{
			AllObjectives_V5_OLD allObjectives_V5_OLD = new AllObjectives_V5_OLD();
			SavedObjective_V5_OLD startMission = new SavedObjective_V5_OLD
			{
				UniqueName = "Mission Start",
				Type = ObjectiveType_V5_OLD.None,
				Faction = "",
				Outcomes = new List<string>(),
				Hidden = true
			};
			allObjectives_V5_OLD.SetStartMission(startMission);
			FixUnitNames(units);
			foreach (MissionFaction_V5_OLD faction in factions)
			{
				Convert(faction.factionName, faction.objectives, allObjectives_V5_OLD, units);
			}
			return allObjectives_V5_OLD.ToMissionObjectives();
		}

		private static void FixUnitNames(IReadOnlyList<SavedUnit_V5_OLD> units)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (SavedUnit_V5_OLD unit in units)
			{
				if (!string.IsNullOrEmpty(unit.UniqueName))
				{
					SaveHelper.MakeUnique(ref unit.UniqueName, hashSet);
					hashSet.Add(unit.UniqueName);
				}
				else if (!string.IsNullOrEmpty(unit.unitCustomID))
				{
					unit.UniqueName = unit.unitCustomID;
					SaveHelper.MakeUnique(ref unit.UniqueName, hashSet);
					hashSet.Add(unit.UniqueName);
				}
			}
			foreach (SavedUnit_V5_OLD unit2 in units)
			{
				if (string.IsNullOrEmpty(unit2.UniqueName))
				{
					unit2.UniqueName = unit2.type;
					SaveHelper.MakeUnique(ref unit2.UniqueName, hashSet, warn: false);
					hashSet.Add(unit2.UniqueName);
				}
			}
		}

		private static void Convert(string faction, List<MissionObjective_V5_OLD> old, AllObjectives_V5_OLD allObjectives, IReadOnlyList<SavedUnit_V5_OLD> units)
		{
			SavedOutcome_V5_OLD outcome = default(SavedOutcome_V5_OLD);
			if (old.Any((MissionObjective_V5_OLD x) => x.victoryObjective))
			{
				outcome = new SavedOutcome_V5_OLD
				{
					UniqueName = "Victory Outcome",
					Type = OutcomeType_V5_OLD.EndGame
				};
				allObjectives.Add(ref outcome);
			}
			List<string>[] array = new List<string>[old.Count - 1];
			for (int num = 0; num < array.Length; num++)
			{
				array[num] = new List<string>();
			}
			for (int num2 = 0; num2 < old.Count; num2++)
			{
				MissionObjective_V5_OLD missionObjective_V5_OLD = old[num2];
				SavedObjective_V5_OLD newObjective = CreateFromOld(missionObjective_V5_OLD, faction);
				if (!string.IsNullOrEmpty(missionObjective_V5_OLD.message))
				{
					SavedOutcome_V5_OLD outcome2 = AddMessageOutcome(missionObjective_V5_OLD);
					allObjectives.Add(ref outcome2);
					newObjective.Outcomes.Add(outcome2.UniqueName);
				}
				SavedUnit_V5_OLD[] array2 = units.Where((SavedUnit_V5_OLD x) => x.spawnTiming == newObjective.UniqueName).ToArray();
				if (array2.Length != 0)
				{
					SavedOutcome_V5_OLD outcome3 = AddSpawnUnitOutcome(missionObjective_V5_OLD, array2);
					allObjectives.Add(ref outcome3);
					newObjective.Outcomes.Add(outcome3.UniqueName);
				}
				if (missionObjective_V5_OLD.victoryObjective)
				{
					newObjective.Outcomes.Add(outcome.UniqueName);
				}
				if (num2 < old.Count - 1)
				{
					SavedOutcome_V5_OLD outcome4 = new SavedOutcome_V5_OLD
					{
						UniqueName = "StartNext_" + old[num2 + 1].objectiveName,
						Type = OutcomeType_V5_OLD.StartObjective,
						Data = new List<ObjectiveData_V5_OLD>
						{
							new ObjectiveData_V5_OLD
							{
								StringValue = "NEXT_OUTCOME_PLACEHOLDER",
								FloatValue = num2
							}
						}
					};
					allObjectives.Add(ref outcome4);
					newObjective.Outcomes.Add(outcome4.UniqueName);
				}
				bool flag = missionObjective_V5_OLD.positionTrigger && missionObjective_V5_OLD.triggerRange > 0f;
				bool flag2 = missionObjective_V5_OLD.targetUnits.Count > 0;
				if (flag && flag2)
				{
					newObjective.Type = ObjectiveType_V5_OLD.CompleteOtherObjective;
					newObjective.Hidden = true;
					SavedObjective_V5_OLD objective = CreateFromOld(missionObjective_V5_OLD, faction);
					ReachWaypoint(ref objective, missionObjective_V5_OLD, setName: true);
					allObjectives.Add(ref objective);
					if (num2 > 0)
					{
						array[num2 - 1].Add(objective.UniqueName);
					}
					newObjective.Data.Add(new ObjectiveData_V5_OLD
					{
						StringValue = objective.UniqueName
					});
					SavedOutcome_V5_OLD outcome5 = new SavedOutcome_V5_OLD
					{
						UniqueName = "Complete_" + objective.UniqueName,
						Type = OutcomeType_V5_OLD.StopOrCompleteObjective,
						Data = new List<ObjectiveData_V5_OLD>
						{
							new ObjectiveData_V5_OLD
							{
								FloatValue = 1f
							},
							new ObjectiveData_V5_OLD
							{
								StringValue = objective.UniqueName
							}
						}
					};
					allObjectives.Add(ref outcome5);
					newObjective.Outcomes.Add(outcome5.UniqueName);
					SavedObjective_V5_OLD objective2 = CreateFromOld(missionObjective_V5_OLD, faction);
					DestroyUnits(ref objective2, missionObjective_V5_OLD, setName: true);
					allObjectives.Add(ref objective2);
					if (num2 > 0)
					{
						array[num2 - 1].Add(objective2.UniqueName);
					}
					newObjective.Data.Add(new ObjectiveData_V5_OLD
					{
						StringValue = objective2.UniqueName
					});
					SavedOutcome_V5_OLD outcome6 = new SavedOutcome_V5_OLD
					{
						UniqueName = "Complete_" + objective2.UniqueName,
						Type = OutcomeType_V5_OLD.StopOrCompleteObjective,
						Data = new List<ObjectiveData_V5_OLD>
						{
							new ObjectiveData_V5_OLD
							{
								FloatValue = 1f
							},
							new ObjectiveData_V5_OLD
							{
								StringValue = objective2.UniqueName
							}
						}
					};
					allObjectives.Add(ref outcome6);
					newObjective.Outcomes.Add(outcome6.UniqueName);
					newObjective.Data.Insert(0, new ObjectiveData_V5_OLD
					{
						FloatValue = 0f
					});
				}
				else if (flag)
				{
					ReachWaypoint(ref newObjective, missionObjective_V5_OLD, setName: false);
				}
				else if (flag2)
				{
					DestroyUnits(ref newObjective, missionObjective_V5_OLD, setName: false);
				}
				else if (missionObjective_V5_OLD.objectiveName != "Mission Start")
				{
					Debug.LogWarning("Objective '" + missionObjective_V5_OLD.objectiveName + "' could not be converted and will be left as type None");
				}
				allObjectives.Add(ref newObjective);
				if (num2 > 0)
				{
					array[num2 - 1].Add(newObjective.UniqueName);
				}
			}
			for (int num3 = 0; num3 < allObjectives.outcomes.Count; num3++)
			{
				if (!allObjectives.outcomes[num3].UniqueName.StartsWith("StartNext_"))
				{
					continue;
				}
				SavedOutcome_V5_OLD value = allObjectives.outcomes[num3];
				if (!(value.Data[0].StringValue != "NEXT_OUTCOME_PLACEHOLDER"))
				{
					int num4 = (int)value.Data[0].FloatValue;
					List<string> source = array[num4];
					value.Data = source.Select((string x) => new ObjectiveData_V5_OLD
					{
						StringValue = x
					}).ToList();
					allObjectives.outcomes[num3] = value;
				}
			}
		}

		private static SavedObjective_V5_OLD CreateFromOld(MissionObjective_V5_OLD old, string faction)
		{
			return new SavedObjective_V5_OLD
			{
				UniqueName = old.objectiveName,
				DisplayName = old.objectiveName,
				Hidden = false,
				Data = new List<ObjectiveData_V5_OLD>(),
				Outcomes = new List<string>(),
				Type = ObjectiveType_V5_OLD.None,
				Faction = faction
			};
		}

		private static void DestroyUnits(ref SavedObjective_V5_OLD objective, MissionObjective_V5_OLD old, bool setName)
		{
			if (setName)
			{
				objective.UniqueName += "_DestroyUnit";
			}
			objective.Type = ObjectiveType_V5_OLD.DestroyUnits;
			objective.Hidden = false;
			objective.Data = old.targetUnits.Select((string x) => new ObjectiveData_V5_OLD
			{
				StringValue = x
			}).ToList();
			objective.Data.Insert(0, new ObjectiveData_V5_OLD
			{
				FloatValue = 1f
			});
		}

		private static void ReachWaypoint(ref SavedObjective_V5_OLD objective, MissionObjective_V5_OLD old, bool setName)
		{
			if (setName)
			{
				objective.UniqueName += "_ReachWaypoints";
			}
			objective.Type = ObjectiveType_V5_OLD.ReachWaypoints;
			objective.Hidden = false;
			objective.Data = new List<ObjectiveData_V5_OLD>
			{
				new ObjectiveData_V5_OLD
				{
					VectorValue = old.position,
					FloatValue = old.triggerRange
				}
			};
			objective.Data.Insert(0, new ObjectiveData_V5_OLD
			{
				FloatValue = 0f
			});
		}

		private static SavedOutcome_V5_OLD AddMessageOutcome(MissionObjective_V5_OLD old)
		{
			return new SavedOutcome_V5_OLD
			{
				UniqueName = "Message_" + old.objectiveName,
				Type = OutcomeType_V5_OLD.ShowMessage,
				TypeName = OutcomeType_V5_OLD.ShowMessage.ToString(),
				Data = new List<ObjectiveData_V5_OLD>
				{
					new ObjectiveData_V5_OLD
					{
						StringValue = old.message
					},
					new ObjectiveData_V5_OLD
					{
						FloatValue = 1f
					},
					new ObjectiveData_V5_OLD
					{
						FloatValue = 1f
					}
				}
			};
		}

		private static SavedOutcome_V5_OLD AddSpawnUnitOutcome(MissionObjective_V5_OLD old, SavedUnit_V5_OLD[] units)
		{
			List<ObjectiveData_V5_OLD> list = new List<ObjectiveData_V5_OLD>(units.Length);
			foreach (SavedUnit_V5_OLD savedUnit_V5_OLD in units)
			{
				list.Add(new ObjectiveData_V5_OLD
				{
					StringValue = savedUnit_V5_OLD.UniqueName
				});
			}
			return new SavedOutcome_V5_OLD
			{
				UniqueName = "SpawnUnits_" + old.objectiveName,
				Type = OutcomeType_V5_OLD.SpawnUnit,
				TypeName = OutcomeType_V5_OLD.SpawnUnit.ToString(),
				Data = list
			};
		}
	}
}
