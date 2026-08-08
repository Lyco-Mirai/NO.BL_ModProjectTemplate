using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.MissionEditorScripts.ObjectiveGraph;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	public class StartObjectiveOutcome : Outcome
	{
		public List<Objective> objectivesToStart;

		public StartObjectiveSavedOutcome Saved => (StartObjectiveSavedOutcome)SavedOutcome;

		public StartObjectiveOutcome(StartObjectiveSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			objectivesToStart = new List<Objective>();
			foreach (string item in Saved.objectivesToStart)
			{
				if (lookups.Objectives.TryGetValue(item, out var value))
				{
					objectivesToStart.Add(value);
				}
				else
				{
					lookups.LoadErrors.AddWarn("'" + item + "' was not found in lookup for Objective");
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.objectivesToStart.Clear();
			foreach (Objective item in objectivesToStart)
			{
				Saved.objectivesToStart.Add(item.GetNameSavedCheckDestroyed());
			}
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
			objectivesToStart.RemoveAll((Objective x) => x.SavedReferenceEquals(reference));
		}

		public override void Complete(Objective completedObjective)
		{
			foreach (Objective item in objectivesToStart)
			{
				MissionManager.Runner.StartObjective(item);
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (objectivesToStart == null)
			{
				objectivesToStart = new List<Objective>();
			}
			drawer.DrawList(300, objectivesToStart);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.OutputElements.Add(new GraphPinData
			{
				PinId = ObjectiveOutcomeGraph.Ids.StartOutcomeOutput,
				DisplayName = "Start",
				PinType = ObjectiveOutcomeGraph.Ids.StartOutcomeOutputType,
				AllowedConnectionTypes = new List<PinType> { ObjectiveOutcomeGraph.Ids.StartObjectiveInputType },
				AllowMultipleConnections = true
			});
		}
	}
}
