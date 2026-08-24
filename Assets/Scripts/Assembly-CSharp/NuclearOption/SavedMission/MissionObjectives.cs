using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	public class MissionObjectives
	{
		public Objective StartObjective;

		public readonly List<Objective> AllObjectives;

		public readonly List<Outcome> AllOutcomes;

		public MissionObjectives(List<Objective> objectives, List<Outcome> outcomes, Objective startObjective)
		{
			AllObjectives = objectives;
			AllOutcomes = outcomes;
			StartObjective = startObjective;
		}

		public void AddNewObjective(Objective obj)
		{
			obj.ThrowIfDestroyed();
			if (AllObjectives.Contains(obj))
			{
				throw new ArgumentException($"Objective already in list: {obj}");
			}
			this.MakeUnique(obj);
			AllObjectives.Add(obj);
		}

		public void AddNewOutcome(Outcome outcome, Objective parent = null)
		{
			outcome.ThrowIfDestroyed();
			if (AllOutcomes.Contains(outcome))
			{
				throw new ArgumentException($"Outcome already in list: {outcome}");
			}
			this.MakeUnique(outcome);
			AllOutcomes.Add(outcome);
			parent?.Outcomes.Add(outcome);
		}

		public void AddExistingOutcome(Outcome outcome, Objective parent)
		{
			outcome.ThrowIfDestroyed();
			if (!AllOutcomes.Contains(outcome))
			{
				throw new ArgumentException($"Outcome was NOT in list: {outcome}");
			}
			parent.Outcomes.Add(outcome);
		}

		public void RemoveObjectiveAt(int index)
		{
			Objective reference = AllObjectives[index];
			AllObjectives.RemoveAt(index);
			ReferenceDestroyed(reference);
		}

		public void RemoveObjective(Objective objective)
		{
			int num = AllObjectives.IndexOf(objective);
			if (num >= 0)
			{
				RemoveObjectiveAt(num);
			}
		}

		public void RemoveOutcome(Outcome outcome)
		{
			int num = AllOutcomes.IndexOf(outcome);
			if (num >= 0)
			{
				RemoveOutcomeAt(num);
			}
		}

		public void RemoveOutcomeAt(int index)
		{
			Outcome outcome = AllOutcomes[index];
			AllOutcomes.RemoveAt(index);
			foreach (Objective allObjective in AllObjectives)
			{
				allObjective.Outcomes.Remove(outcome);
			}
			ReferenceDestroyed(outcome);
		}

		public void ReferenceDestroyed(ISaveableReference reference)
		{
			reference.Destroyed = true;
			foreach (Objective allObjective in AllObjectives)
			{
				allObjective.ReferenceDestroyed(reference);
			}
			foreach (Outcome allOutcome in AllOutcomes)
			{
				allOutcome.ReferenceDestroyed(reference);
			}
		}

		public Objective GetObjective(string name)
		{
			foreach (Objective allObjective in AllObjectives)
			{
				if (allObjective.SavedObjective.UniqueName == name)
				{
					return allObjective;
				}
			}
			throw new KeyNotFoundException("Could not find mission with name " + name);
		}
	}
}
