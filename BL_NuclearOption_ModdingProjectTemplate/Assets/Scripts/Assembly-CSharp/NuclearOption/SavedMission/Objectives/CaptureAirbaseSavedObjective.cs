using System;
using System.Collections.Generic;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class CaptureAirbaseSavedObjective : SavedObjective
	{
		public CompleteOrder completeOrder = CompleteOrder.CompleteAll;

		public float completeSomePercent = 0.5f;

		public List<string> targetAirbases = new List<string>();

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.CaptureAirbase;

		public CaptureAirbaseSavedObjective()
		{
		}

		public CaptureAirbaseSavedObjective(string name)
			: base(name)
		{
		}
	}
}
