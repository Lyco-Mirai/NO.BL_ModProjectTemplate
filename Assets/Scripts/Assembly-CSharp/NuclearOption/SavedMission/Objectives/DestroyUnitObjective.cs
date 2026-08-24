using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Objectives
{
	public class DestroyUnitObjective : CompleteOrderObjectiveWithPositions<SavedUnit>, IObjectiveWithPosition
	{
		public DestroyUnitSavedObjective Saved => (DestroyUnitSavedObjective)SavedObjective;

		public DestroyUnitObjective(DestroyUnitSavedObjective savedObjective)
			: base((SavedObjective)savedObjective)
		{
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			completeOrder = Saved.completeOrder;
			completeSomePercent.SetValue(Saved.completeSomePercent, this);
			allItems = new List<SavedUnit>();
			foreach (string targetUnit in Saved.targetUnits)
			{
				if (lookups.SavedUnits.TryGetValue(targetUnit, out var value))
				{
					allItems.Add(value);
				}
				else
				{
					lookups.LoadErrors.AddWarn("'" + targetUnit + "' was not found in lookup for SavedUnit");
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.completeOrder = completeOrder;
			Saved.completeSomePercent = completeSomePercent.Value;
			Saved.targetUnits.Clear();
			foreach (SavedUnit allItem in allItems)
			{
				Saved.targetUnits.Add(allItem.GetNameSavedCheckDestroyed());
			}
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
			allItems.RemoveAll((SavedUnit x) => x.SavedReferenceEquals(reference));
		}

		protected override bool CheckComplete(SavedUnit item)
		{
			if (UnitRegistry.customIDLookup.TryGetValue(item.UniqueName, out var value) && (value == null || value.disabled))
			{
				return true;
			}
			return false;
		}

		protected override bool TryGetPosition(SavedUnit item, out ObjectivePosition position)
		{
			GlobalPosition pos;
			bool result = UnitRegistry.TryGetPosition(item, out pos);
			position = new ObjectivePosition(pos, null);
			return result;
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (allItems == null)
			{
				allItems = new List<SavedUnit>();
			}
			CompleteOrderPercentWrapper.Create(drawer, completeOrder, delegate(CompleteOrder v)
			{
				completeOrder = v;
			}, completeSomePercent);
			drawer.Space(10);
			drawer.DrawList(300, allItems, includeBuiltIn: true);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Units Count"),
				DisplayName = "Units Count",
				GetText = () => (allItems == null) ? "0" : allItems.Count.ToString()
			});
		}
	}
}
