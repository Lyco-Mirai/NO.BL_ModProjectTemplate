using System;
using System.Collections.Generic;
using NuclearOption.MissionEditorScripts;

namespace NuclearOption.SavedMission.Outcomes
{
	public class RestrictionOutcome : Outcome
	{
		public enum Change
		{
			Add = 0,
			Remove = 1
		}

		private Change endType;

		private List<string> restrictionNames = new List<string>();

		public RestrictionSavedOutcome Saved => (RestrictionSavedOutcome)SavedOutcome;

		public RestrictionOutcome(RestrictionSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void Complete(Objective completedObjective)
		{
			throw new NotImplementedException();
		}

		public override void CopyFrom(Outcome original)
		{
			base.CopyFrom(original);
			RestrictionOutcome restrictionOutcome = (RestrictionOutcome)original;
			endType = restrictionOutcome.endType;
			if (restrictionOutcome.restrictionNames != null)
			{
				restrictionNames = new List<string>(restrictionOutcome.restrictionNames);
			}
		}

		public override void Load(MissionLookups lookups)
		{
			base.Load(lookups);
			endType = Saved.endType;
			if (Saved.restrictionNames != null)
			{
				restrictionNames = new List<string>(Saved.restrictionNames);
			}
		}

		public override void Save()
		{
			base.Save();
			Saved.endType = endType;
			if (restrictionNames != null)
			{
				Saved.restrictionNames = new List<string>(restrictionNames);
			}
		}

		public override void ReferenceDestroyed(ISaveableReference reference)
		{
		}

		public override void DrawData(DataDrawer drawer)
		{
			drawer.DrawEnum<Change>("End Type", (int)endType, delegate(int v)
			{
				endType = (Change)v;
			});
		}
	}
}
