using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class CaptureAirbaseObjective : CompleteOrderObjectiveWithPositions<SavedAirbase>, IObjectiveWithPosition
	{
		public override bool NeedsFaction => true;

		protected override bool AllCompleteAtOnce => true;

		public CaptureAirbaseSavedObjective Saved => (CaptureAirbaseSavedObjective)SavedObjective;

		public CaptureAirbaseObjective(CaptureAirbaseSavedObjective savedObjective)
			: base((SavedObjective)savedObjective)
		{
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			completeOrder = Saved.completeOrder;
			completeSomePercent.SetValue(Saved.completeSomePercent, this);
			allItems = new List<SavedAirbase>();
			foreach (string targetAirbasis in Saved.targetAirbases)
			{
				if (lookups.Airbases.TryGetValue(targetAirbasis, out var value))
				{
					allItems.Add(value);
				}
				else
				{
					lookups.LoadErrors.AddWarn("'" + targetAirbasis + "' was not found in lookup for SavedAirbase");
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.completeOrder = completeOrder;
			Saved.completeSomePercent = completeSomePercent.Value;
			Saved.targetAirbases.Clear();
			foreach (SavedAirbase allItem in allItems)
			{
				Saved.targetAirbases.Add(allItem.GetNameSavedCheckDestroyed());
			}
		}

		protected override void DataReferenceDestroyed(ISaveableReference reference)
		{
			allItems.RemoveAll((SavedAirbase x) => x.SavedReferenceEquals(reference));
		}

		public override void ReferenceReplaced(ISaveableReference oldRef, ISaveableReference newRef)
		{
			if (oldRef is SavedAirbase item && newRef is SavedAirbase value)
			{
				int num = allItems.IndexOf(item);
				if (num != -1)
				{
					allItems[num] = value;
				}
			}
		}

		protected override bool CheckComplete(SavedAirbase item)
		{
			if (!FactionRegistry.airbaseLookup.TryGetValue(item.UniqueName, out var value))
			{
				Debug.LogWarning("Could not find airbase with name " + item.UniqueName);
				return true;
			}
			return value.CurrentHQ == base.FactionHQ;
		}

		protected override bool TryGetPosition(SavedAirbase item, out ObjectivePosition position)
		{
			position = new ObjectivePosition(item.Center, null);
			return true;
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (allItems == null)
			{
				allItems = new List<SavedAirbase>();
			}
			CompleteOrderPercentWrapper.Create(drawer, completeOrder, delegate(CompleteOrder v)
			{
				completeOrder = v;
			}, completeSomePercent);
			drawer.Space(10);
			drawer.DrawList(300, allItems).SelectExistingDropdown.FilterSet.Apply("NotAttached", FilterNotAttached);
		}

		public override void AddPins(GraphNodeData data)
		{
			base.AddPins(data);
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Airbases Count"),
				DisplayName = "Airbases Count",
				GetText = () => (allItems == null) ? "0" : allItems.Count.ToString()
			});
		}

		private static bool FilterNotAttached(object obj)
		{
			Airbase airbase = ((SavedAirbase)obj).Airbase;
			if (airbase != null && airbase.AttachedAirbase)
			{
				return false;
			}
			return true;
		}
	}
}
