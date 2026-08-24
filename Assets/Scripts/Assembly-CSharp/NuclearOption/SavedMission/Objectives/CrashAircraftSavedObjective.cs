using System;
using Mirage;

namespace NuclearOption.SavedMission.Objectives
{
	[Serializable]
	[NetworkMessage]
	public class CrashAircraftSavedObjective : SavedObjective
	{
		public int livesPerPlayer;

		public int extraLives = 1;

		public bool includeDestroy = true;

		public bool includeEject = true;

		public override ObjectiveType ObjectiveTypeEnum => ObjectiveType.CrashAircraft;

		public CrashAircraftSavedObjective()
		{
		}

		public CrashAircraftSavedObjective(string name)
			: base(name)
		{
		}
	}
}
