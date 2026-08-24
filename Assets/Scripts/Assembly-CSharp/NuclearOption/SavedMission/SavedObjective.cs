using System;
using System.Collections.Generic;
using System.ComponentModel;
using NuclearOption.SavedMission.Objectives;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public abstract class SavedObjective
	{
		public string UniqueName = "";

		public string Faction = "";

		public string DisplayName = "";

		public bool Hidden;

		public List<string> Outcomes = new List<string>();

		public abstract ObjectiveType ObjectiveTypeEnum { get; }

		protected SavedObjective()
		{
		}

		public SavedObjective(string name)
		{
			UniqueName = name;
			DisplayName = name;
		}

		public static Objective Create(SavedObjective savedObjective)
		{
			if (!(savedObjective is NoSavedObjective savedObjective2))
			{
				if (!(savedObjective is DestroyUnitSavedObjective savedObjective3))
				{
					if (!(savedObjective is ReachUnitsSavedObjective savedObjective4))
					{
						if (!(savedObjective is ReachWaypointsSavedObjective savedObjective5))
						{
							if (!(savedObjective is WaitTimeSavedObjective savedObjective6))
							{
								if (!(savedObjective is DialogueBoxSavedObjective savedObjective7))
								{
									if (!(savedObjective is CompleteOtherObjectiveSavedObjective savedObjective8))
									{
										if (!(savedObjective is SpotUnitSavedObjective savedObjective9))
										{
											if (!(savedObjective is CaptureAirbaseSavedObjective savedObjective10))
											{
												if (!(savedObjective is CrashAircraftSavedObjective savedObjective11))
												{
													if (savedObjective is SuccessfulSortieSavedObjective savedObjective12)
													{
														return new SuccessfulSortieObjective(savedObjective12);
													}
													throw new ArgumentException("Unknown saved objective type " + savedObjective?.GetType().Name, "savedObjective");
												}
												return new CrashAircraftObjective(savedObjective11);
											}
											return new CaptureAirbaseObjective(savedObjective10);
										}
										return new SpotUnitObjective(savedObjective9);
									}
									return new CompleteOtherObjectiveObjective(savedObjective8);
								}
								return new DialogueBoxObjective(savedObjective7);
							}
							return new WaitTimeObjective(savedObjective6);
						}
						return new ReachWaypointsObjective(savedObjective5);
					}
					return new ReachUnitsObjective(savedObjective4);
				}
				return new DestroyUnitObjective(savedObjective3);
			}
			return new NoObjective(savedObjective2);
		}

		public static SavedObjective CreateSavedObjective(ObjectiveType type, string uniqueName)
		{
			return type switch
			{
				ObjectiveType.None => new NoSavedObjective(uniqueName), 
				ObjectiveType.DestroyUnits => new DestroyUnitSavedObjective(uniqueName), 
				ObjectiveType.ReachUnits => new ReachUnitsSavedObjective(uniqueName), 
				ObjectiveType.ReachWaypoints => new ReachWaypointsSavedObjective(uniqueName), 
				ObjectiveType.WaitSeconds => new WaitTimeSavedObjective(uniqueName), 
				ObjectiveType.DialogueBox => new DialogueBoxSavedObjective(uniqueName), 
				ObjectiveType.CompleteOtherObjective => new CompleteOtherObjectiveSavedObjective(uniqueName), 
				ObjectiveType.SpotUnit => new SpotUnitSavedObjective(uniqueName), 
				ObjectiveType.CaptureAirbase => new CaptureAirbaseSavedObjective(uniqueName), 
				ObjectiveType.CrashAircraft => new CrashAircraftSavedObjective(uniqueName), 
				ObjectiveType.SuccessfulSortie => new SuccessfulSortieSavedObjective(uniqueName), 
				_ => throw new InvalidEnumArgumentException($"Invalid objective type {type}"), 
			};
		}
	}
}
