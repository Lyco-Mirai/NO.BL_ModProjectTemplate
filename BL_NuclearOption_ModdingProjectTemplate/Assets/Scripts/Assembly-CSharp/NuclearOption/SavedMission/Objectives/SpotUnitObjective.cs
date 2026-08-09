using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Objectives
{
	public class SpotUnitObjective : CompleteOrderObjective<SavedUnit>
	{
		private static readonly List<CompleteOrder> completeOrderOptions = new List<CompleteOrder>
		{
			CompleteOrder.CompleteAny,
			CompleteOrder.CompleteAll,
			CompleteOrder.CompleteSome
		};

		public override bool NeedsFaction => true;

		private new CompleteOrderList<SavedUnit> completeList => (CompleteOrderList<SavedUnit>)base.completeList;

		public SpotUnitSavedObjective Saved => (SpotUnitSavedObjective)SavedObjective;

		public SpotUnitObjective(SpotUnitSavedObjective savedObjective)
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

		public override void OnStart()
		{
			base.OnStart();
			base.FactionHQ.onDiscoverUnit += FactionHQ_onDiscoverUnit;
			CheckExistingTracking();
		}

		public override void Cleanup()
		{
			base.FactionHQ.onDiscoverUnit -= FactionHQ_onDiscoverUnit;
		}

		private void CheckExistingTracking()
		{
			CompleteOrderList<SavedUnit>.CheckItem[] array = completeList.ToCheck.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				CompleteOrderList<SavedUnit>.CheckItem toCheck = array[i];
				string uniqueName = toCheck.Item.UniqueName;
				if (UnitRegistry.customIDLookup.TryGetValue(uniqueName, out var value) && base.FactionHQ.trackingDatabase.TryGetValue(value.persistentID, out var _))
				{
					completeList.MarkCompleted(toCheck);
				}
			}
		}

		protected override bool CheckComplete(SavedUnit toCheck)
		{
			return false;
		}

		private void FactionHQ_onDiscoverUnit(PersistentID id)
		{
			if (!UnitRegistry.TryGetUnit(id, out var unit))
			{
				return;
			}
			foreach (CompleteOrderList<SavedUnit>.CheckItem item in completeList.ToCheck)
			{
				if (item.Item.UniqueName == unit.UniqueName)
				{
					completeList.MarkCompleted(item);
					break;
				}
			}
		}

		public override void ClientOnlyUpdate()
		{
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
			}, completeSomePercent, completeOrderOptions);
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
