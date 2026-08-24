using System;
using NuclearOption.SavedMission.Outcomes;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public abstract class SavedOutcome
	{
		public string UniqueName = "";

		public abstract OutcomeType OutcomeTypeEnum { get; }

		public static Outcome Create(SavedOutcome savedOutcome)
		{
			if (!(savedOutcome is NoSavedOutcome savedOutcome2))
			{
				if (!(savedOutcome is StartObjectiveSavedOutcome savedOutcome3))
				{
					if (!(savedOutcome is CompleteObjectiveSavedOutcome savedOutcome4))
					{
						if (!(savedOutcome is ShowMessageSavedOutcome savedOutcome5))
						{
							if (!(savedOutcome is GiveScoreSavedOutcome savedOutcome6))
							{
								if (!(savedOutcome is SpawnUnitSavedOutcome savedOutcome7))
								{
									if (!(savedOutcome is RemoveUnitSavedOutcome savedOutcome8))
									{
										if (!(savedOutcome is RevealUnitSavedOutcome savedOutcome9))
										{
											if (!(savedOutcome is EndGameSavedOutcome savedOutcome10))
											{
												if (!(savedOutcome is ModifyAirbaseSavedOutcome savedOutcome11))
												{
													if (!(savedOutcome is ModifyEnvironmentSavedOutcome savedOutcome12))
													{
														if (!(savedOutcome is ModifyFactionSavedOutcome savedOutcome13))
														{
															if (savedOutcome is RestrictionSavedOutcome savedOutcome14)
															{
																return new RestrictionOutcome(savedOutcome14);
															}
															throw new ArgumentException("Unknown saved outcome type " + savedOutcome?.GetType().Name, "savedOutcome");
														}
														return new ModifyFactionOutcome(savedOutcome13);
													}
													return new ModifyEnvironmentOutcome(savedOutcome12);
												}
												return new ModifyAirbaseOutcome(savedOutcome11);
											}
											return new EndGameOutcome(savedOutcome10);
										}
										return new RevealUnitOutcome(savedOutcome9);
									}
									return new RemoveUnitOutcome(savedOutcome8);
								}
								return new SpawnUnitOutcome(savedOutcome7);
							}
							return new GiveScoreOutcome(savedOutcome6);
						}
						return new ShowMessageOutcome(savedOutcome5);
					}
					return new CompleteObjectiveOutcome(savedOutcome4);
				}
				return new StartObjectiveOutcome(savedOutcome3);
			}
			return new NoOutcome(savedOutcome2);
		}

		public static SavedOutcome CreateSaved(OutcomeType type, string uniqueName)
		{
			SavedOutcome savedOutcome = type switch
			{
				OutcomeType.None => new NoSavedOutcome(), 
				OutcomeType.StartObjective => new StartObjectiveSavedOutcome(), 
				OutcomeType.StopOrCompleteObjective => new CompleteObjectiveSavedOutcome(), 
				OutcomeType.ShowMessage => new ShowMessageSavedOutcome(), 
				OutcomeType.GiveScore => new GiveScoreSavedOutcome(), 
				OutcomeType.SpawnUnit => new SpawnUnitSavedOutcome(), 
				OutcomeType.RemoveUnit => new RemoveUnitSavedOutcome(), 
				OutcomeType.RevealUnit => new RevealUnitSavedOutcome(), 
				OutcomeType.EndGame => new EndGameSavedOutcome(), 
				OutcomeType.ModifyAirbase => new ModifyAirbaseSavedOutcome(), 
				OutcomeType.ModifyEnvironment => new ModifyEnvironmentSavedOutcome(), 
				OutcomeType.ModifyFaction => new ModifyFactionSavedOutcome(), 
				_ => throw new ArgumentOutOfRangeException("type", type, null), 
			};
			savedOutcome.UniqueName = uniqueName;
			return savedOutcome;
		}
	}
}
