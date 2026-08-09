using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.MissionEditorScripts.ObjectiveGraph;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	public class CompleteObjectiveOutcome : Outcome
	{
		public enum Options
		{
			Stop = 0,
			Complete = 1
		}

		public List<Objective> objectivesToStart;

		private Options options;

		private readonly ValueWrapperEnum<Options> optionsWrapper;

		public CompleteObjectiveSavedOutcome Saved => (CompleteObjectiveSavedOutcome)SavedOutcome;

		public CompleteObjectiveOutcome(CompleteObjectiveSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
			optionsWrapper = new ValueWrapperEnum<Options>(this, () => options, delegate(Options v)
			{
				options = v;
			});
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			CompleteObjectiveOutcome completeObjectiveOutcome = (CompleteObjectiveOutcome)original;
			options = completeObjectiveOutcome.options;
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			options = Saved.options;
			optionsWrapper.SetValue((int)options, this);
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
			Saved.options = options;
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
			if (options == Options.Stop)
			{
				foreach (Objective item in objectivesToStart)
				{
					MissionManager.Runner.StopObjective(item);
				}
				return;
			}
			foreach (Objective item2 in objectivesToStart)
			{
				MissionManager.Runner.CompleteObjective(item2);
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (objectivesToStart == null)
			{
				objectivesToStart = new List<Objective>();
			}
			drawer.DrawEnum<Options>("Stop Mode", (int)options, delegate(int v)
			{
				options = (Options)v;
			});
			drawer.DrawList(300, objectivesToStart);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.OutputElements.Add(new GraphPinData
			{
				PinId = ObjectiveOutcomeGraph.Ids.CompleteOutcomeOutput,
				DisplayName = "Complete",
				PinType = ObjectiveOutcomeGraph.Ids.CompleteOutcomeOutputType,
				AllowedConnectionTypes = new List<PinType> { ObjectiveOutcomeGraph.Ids.CompleteObjectiveInputType },
				AllowMultipleConnections = true
			});
			data.InputElements.Add(new GraphDropdownFieldData
			{
				PinId = new PinId("Stop Mode"),
				DisplayName = "Stop Mode",
				Options = new List<string> { "Stop", "Complete" },
				ValueWrapper = optionsWrapper
			});
		}
	}
}
