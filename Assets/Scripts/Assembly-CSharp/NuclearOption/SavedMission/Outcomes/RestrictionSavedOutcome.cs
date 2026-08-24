using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Outcomes
{
	[Serializable]
	[NetworkMessage]
	public class RestrictionSavedOutcome : SavedOutcome
	{
		public RestrictionOutcome.Change endType;

		public List<string> restrictionNames = new List<string>();

		public override OutcomeType OutcomeTypeEnum
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public RestrictionSavedOutcome()
		{
		}

		public RestrictionSavedOutcome(string name)
		{
			UniqueName = name;
		}
	}
}
