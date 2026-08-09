using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;

namespace NuclearOption.SavedMission.Outcomes
{
	internal class RevealUnitOutcome : Outcome
	{
		public List<SavedUnit> UnitsToReveal;

		public RevealUnitSavedOutcome Saved => (RevealUnitSavedOutcome)SavedOutcome;

		public RevealUnitOutcome(RevealUnitSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			RevealUnitOutcome revealUnitOutcome = (RevealUnitOutcome)original;
			if (revealUnitOutcome.UnitsToReveal != null)
			{
				UnitsToReveal = new List<SavedUnit>(revealUnitOutcome.UnitsToReveal);
			}
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			UnitsToReveal = new List<SavedUnit>();
			foreach (string item in Saved.UnitsToReveal)
			{
				if (lookups.SavedUnits.TryGetValue(item, out var value))
				{
					UnitsToReveal.Add(value);
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
			Saved.UnitsToReveal.Clear();
			foreach (SavedUnit item in UnitsToReveal)
			{
				Saved.UnitsToReveal.Add(item.GetNameSavedCheckDestroyed());
			}
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
			UnitsToReveal.RemoveAll((SavedUnit x) => x.SavedReferenceEquals(reference));
		}

		public override void Complete(Objective completedObjective)
		{
			foreach (SavedUnit item in UnitsToReveal)
			{
				if (item.HasSpawned)
				{
					completedObjective.FactionHQ.RpcUpdateTrackingInfo(item.Unit.persistentID);
				}
			}
		}

		public override void DrawData(DataDrawer drawer)
		{
			if (UnitsToReveal == null)
			{
				UnitsToReveal = new List<SavedUnit>();
			}
			drawer.DrawList(300, UnitsToReveal, includeBuiltIn: false);
		}

		public override void AddPins(GraphNodeData data)
		{
			data.InputElements.Add(new GraphReadOnlyFieldData
			{
				PinId = new PinId("Units Count"),
				DisplayName = "Units Count",
				GetText = () => (UnitsToReveal == null) ? "0" : UnitsToReveal.Count.ToString()
			});
		}
	}
}
