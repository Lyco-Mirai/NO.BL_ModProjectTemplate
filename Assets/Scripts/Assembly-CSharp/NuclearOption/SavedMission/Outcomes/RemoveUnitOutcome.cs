using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.NodeGraph;
using UnityEngine;

namespace NuclearOption.SavedMission.Outcomes
{
	internal class RemoveUnitOutcome : Outcome
	{
		public List<SavedUnit> UnitsToRemove;

		public RemoveUnitSavedOutcome Saved => (RemoveUnitSavedOutcome)SavedOutcome;

		public RemoveUnitOutcome(RemoveUnitSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			RemoveUnitOutcome removeUnitOutcome = (RemoveUnitOutcome)original;
			if (removeUnitOutcome.UnitsToRemove != null)
			{
				UnitsToRemove = new List<SavedUnit>(removeUnitOutcome.UnitsToRemove);
			}
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			UnitsToRemove = new List<SavedUnit>();
			foreach (string item in Saved.UnitsToRemove)
			{
				if (lookups.SavedUnits.TryGetValue(item, out var value))
				{
					UnitsToRemove.Add(value);
				}
				else
				{
					lookups.LoadErrors.AddWarn("'" + item + "' was not found in lookup for SavedUnit");
				}
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.UnitsToRemove.Clear();
			foreach (SavedUnit item in UnitsToRemove)
			{
				Saved.UnitsToRemove.Add(item.GetNameSavedCheckDestroyed());
			}
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
			UnitsToRemove.RemoveAll((SavedUnit x) => x.SavedReferenceEquals(reference));
		}

		public override void Complete(Objective completedObjective)
		{
			foreach (SavedUnit item in UnitsToRemove)
			{
				if (item.HasSpawned)
				{
					RemoveUnit(item);
				}
			}
		}

		private void RemoveUnit(SavedUnit savedUnit)
		{
			if (UnitRegistry.customIDLookup.TryGetValue(savedUnit.UniqueName, out var value) && value != null)
			{
				if (value is Aircraft aircraft && aircraft.GetPlayer() != null)
				{
					aircraft.StartEjectionSequence();
					return;
				}
				value.DisableUnit();
				Object.Destroy(value.gameObject, 2f);
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (UnitsToRemove == null)
			{
				UnitsToRemove = new List<SavedUnit>();
			}
			drawer.DrawList(300, UnitsToRemove, includeBuiltIn: true);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Units Count"),
				DisplayName = "Units Count",
				GetText = () => (UnitsToRemove == null) ? "0" : UnitsToRemove.Count.ToString()
			});
		}
	}
}
