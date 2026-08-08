using System;

namespace NuclearOption.SavedMission.ConvertVersions
{
	[Serializable]
	[Obsolete("V5", true)]
	public enum OutcomeType_V5_OLD
	{
		StartObjective = 1,
		StopOrCompleteObjective = 2,
		ShowMessage = 3,
		GiveScore = 4,
		SpawnUnit = 5,
		RemoveUnit = 6,
		RevealUnit = 7,
		EndGame = 8,
		ModifyAirbase = 9,
		ModifyEnvironment = 10,
		ModifyFaction = 11
	}
}
