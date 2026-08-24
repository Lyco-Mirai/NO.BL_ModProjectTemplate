using System;
using System.Collections.Generic;
using System.Linq;
using Mirage;
using NuclearOption.SavedMission.Outcomes;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public static class SaveHelper
	{
		public static string GetNameSavedCheckDestroyed<T>(this T reference) where T : ISaveableReference
		{
			reference.ThrowIfDestroyed();
			return reference.UniqueName;
		}

		public static void ThrowIfDestroyed(this ISaveableReference reference)
		{
			if (reference.Destroyed)
			{
				throw new InvalidOperationException($"Object Destroyed, {reference}");
			}
		}

		public static void ValidateEnum<T>(T value) where T : Enum
		{
			if (!Enum.IsDefined(typeof(T), value))
			{
				Debug.LogError($"Value '{value}' was not a valid {typeof(T).FullName}");
			}
		}

		public static int CountStartedBy(MissionObjectives missionObjectives, Objective objective)
		{
			int num = 0;
			foreach (Outcome allOutcome in missionObjectives.AllOutcomes)
			{
				if (allOutcome is StartObjectiveOutcome startObjectiveOutcome && startObjectiveOutcome.objectivesToStart.Contains(objective))
				{
					num++;
				}
			}
			return num;
		}

		public static int CountUsedBy(MissionObjectives missionObjectives, Outcome outcome)
		{
			int num = 0;
			foreach (Objective allObjective in missionObjectives.AllObjectives)
			{
				if (allObjective.Outcomes.Contains(outcome))
				{
					num++;
				}
			}
			return num;
		}

		public static int CountSpawnedBy(MissionObjectives missionObjectives, SavedUnit savedUnit)
		{
			int num = 0;
			foreach (Outcome allOutcome in missionObjectives.AllOutcomes)
			{
				if (allOutcome is SpawnUnitOutcome spawnUnitOutcome && spawnUnitOutcome.UnitsToSpawn.Contains(savedUnit))
				{
					num++;
				}
			}
			return num;
		}

		public static bool MakeUnique(this MissionObjectives mission, Objective objective)
		{
			string name = objective.SavedObjective.UniqueName;
			if (mission.MakeUnique(ref name, objective))
			{
				objective.Rename(name);
				return true;
			}
			return false;
		}

		public static bool MakeUnique(this MissionObjectives mission, ref string name, Objective objective)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = objective.SavedObjective.ObjectiveTypeEnum.ToString();
			}
			IEnumerable<string> others = from x in mission.AllObjectives
				where x != objective
				select x.SavedObjective.UniqueName;
			return MakeUnique(ref name, others);
		}

		public static bool MakeUnique(this MissionObjectives mission, Outcome outcome)
		{
			string name = outcome.SavedOutcome.UniqueName;
			if (mission.MakeUnique(ref name, outcome))
			{
				outcome.Rename(name);
				return true;
			}
			return false;
		}

		public static bool MakeUnique(this MissionObjectives mission, ref string name, Outcome outcome)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = outcome.SavedOutcome.OutcomeTypeEnum.ToString();
			}
			IEnumerable<string> others = from x in mission.AllOutcomes
				where x != outcome
				select x.SavedOutcome.UniqueName;
			return MakeUnique(ref name, others);
		}

		public static bool MakeUnique(this Mission mission, SavedUnit unit)
		{
			using AutoPool<List<SavedUnit>>.Wrapper wrapper = AutoPool<List<SavedUnit>>.Take();
			List<SavedUnit> item = wrapper.Item;
			MissionManager.GetAllSavedUnitsNonAlloc(mission, item, includeBuiltIn: true);
			return MakeUnique(unit, item);
		}

		public static bool MakeUnique(SavedUnit unit, IEnumerable<ISaveableReference> allUnits)
		{
			string name = unit.UniqueName;
			if (string.IsNullOrEmpty(name))
			{
				name = ((!string.IsNullOrEmpty(unit.type)) ? unit.type : "Unit");
			}
			if (MakeUnique(ref name, unit, allUnits))
			{
				unit.Rename(name);
				return true;
			}
			return false;
		}

		public static bool MakeUnique(this Mission mission, SavedAirbase airbase)
		{
			string name = airbase.UniqueName;
			if (string.IsNullOrEmpty(name))
			{
				name = "Airbase";
			}
			if (MakeUnique(ref name, airbase, mission.airbases))
			{
				airbase.Rename(name);
				return true;
			}
			return false;
		}

		public static bool MakeUnique(ref string name, ISaveableReference current, IEnumerable<ISaveableReference> others, bool warn = true)
		{
			IEnumerable<string> others2 = from x in others
				where x != current
				select x.UniqueName;
			return MakeUnique(ref name, others2, warn);
		}

		public static bool MakeUnique(ref string name, IEnumerable<ISaveableReference> others, bool warn = true)
		{
			IEnumerable<string> others2 = others.Select((ISaveableReference x) => x.UniqueName);
			return MakeUnique(ref name, others2, warn);
		}

		public static bool MakeUnique<T>(ref string name, Dictionary<string, T> others, bool warn = true)
		{
			return MakeUnique(ref name, others.ContainsKey, warn);
		}

		public static bool MakeUnique(ref string name, HashSet<string> others, bool warn = true)
		{
			return MakeUnique(ref name, others.Contains, warn);
		}

		public static bool MakeUnique(ref string name, IEnumerable<string> others, bool warn = true)
		{
			return MakeUnique(ref name, (string checkName) => others.Any((string x) => x == checkName), warn);
		}

		private static bool MakeUnique(ref string name, Func<string, bool> contains, bool warn = true)
		{
			bool flag = string.IsNullOrEmpty(name);
			if (flag)
			{
				name = "empty";
			}
			int num = 0;
			string text = "";
			while (true)
			{
				string arg = name + text;
				if (!contains(arg))
				{
					break;
				}
				if (warn)
				{
					Debug.LogWarning("non-unique name found " + name);
					warn = false;
				}
				num++;
				text = $"_{num}";
			}
			if (!string.IsNullOrEmpty(text) || flag)
			{
				name += text;
				return true;
			}
			return false;
		}

		public static bool SavedReferenceEquals<T>(this T field, ISaveableReference other) where T : class, ISaveableReference
		{
			if (field == null)
			{
				return other == null;
			}
			if (other is T val)
			{
				return field.UniqueName == val.UniqueName;
			}
			return false;
		}
	}
}
