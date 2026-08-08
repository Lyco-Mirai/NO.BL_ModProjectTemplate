using System;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public enum ObjectiveType
	{
		None = 0,
		DestroyUnits = 1,
		ReachUnits = 2,
		ReachWaypoints = 3,
		WaitSeconds = 4,
		CaptureAirbase = 6,
		DialogueBox = 7,
		CompleteOtherObjective = 8,
		SpotUnit = 9,
		CrashAircraft = 10,
		SuccessfulSortie = 11
	}
}
