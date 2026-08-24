using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Objectives
{
	public class CompleteOtherObjectiveObjective : CompleteOrderObjective<Objective>
	{
		private static readonly List<CompleteOrder> completeOrderOptions = new List<CompleteOrder>
		{
			CompleteOrder.CompleteAny,
			CompleteOrder.CompleteAll,
			CompleteOrder.CompleteSome
		};

		public CompleteOtherObjectiveSavedObjective Saved => (CompleteOtherObjectiveSavedObjective)SavedObjective;

		public CompleteOtherObjectiveObjective(CompleteOtherObjectiveSavedObjective savedObjective)
			: base((SavedObjective)savedObjective)
		{
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			completeOrder = Saved.completeOrder;
			completeSomePercent.SetValue(Saved.completeSomePercent, this);
			allItems = new List<Objective>();
			foreach (string targetObjective in Saved.targetObjectives)
			{
				if (lookups.Objectives.TryGetValue(targetObjective, out var value))
				{
					allItems.Add(value);
				}
				else
				{
					lookups.LoadErrors.AddWarn("'" + targetObjective + "' was not found in lookup for Objective");
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.completeOrder = completeOrder;
			Saved.completeSomePercent = completeSomePercent.Value;
			Saved.targetObjectives.Clear();
			foreach (Objective allItem in allItems)
			{
				Saved.targetObjectives.Add(allItem.GetNameSavedCheckDestroyed());
			}
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
			allItems.RemoveAll((Objective x) => x.SavedReferenceEquals(reference));
		}

		public override void ClientOnlyUpdate()
		{
		}

		protected override bool CheckComplete(Objective item)
		{
			return item.Status == ObjectiveStatus.Complete;
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (allItems == null)
			{
				allItems = new List<Objective>();
			}
			CompleteOrderPercentWrapper.Create(drawer, completeOrder, delegate(CompleteOrder v)
			{
				completeOrder = v;
			}, completeSomePercent, completeOrderOptions);
			drawer.Space(10);
			drawer.DrawList(300, allItems);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Objectives Count"),
				DisplayName = "Objectives Count",
				GetText = () => (allItems == null) ? "0" : allItems.Count.ToString()
			});
		}
	}
}
