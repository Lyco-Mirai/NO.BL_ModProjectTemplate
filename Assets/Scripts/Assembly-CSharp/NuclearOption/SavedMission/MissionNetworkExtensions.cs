using System;
using Mirage.Serialization;
using NuclearOption.SavedMission.Objectives;
using NuclearOption.SavedMission.Outcomes;

namespace NuclearOption.SavedMission
{
	public static class MissionNetworkExtensions
	{
		public static void WriteSavedObjective(this NetworkWriter writer, SavedObjective obj)
		{
			if (obj == null)
			{
				writer.WritePackedInt32(0);
				return;
			}
			writer.WritePackedInt32((int)(obj.ObjectiveTypeEnum + 1));
			if (!(obj is NoSavedObjective value))
			{
				if (!(obj is DestroyUnitSavedObjective value2))
				{
					if (!(obj is ReachUnitsSavedObjective value3))
					{
						if (!(obj is ReachWaypointsSavedObjective value4))
						{
							if (!(obj is WaitTimeSavedObjective value5))
							{
								if (!(obj is CaptureAirbaseSavedObjective value6))
								{
									if (!(obj is DialogueBoxSavedObjective value7))
									{
										if (!(obj is CompleteOtherObjectiveSavedObjective value8))
										{
											if (!(obj is SpotUnitSavedObjective value9))
											{
												if (!(obj is CrashAircraftSavedObjective value10))
												{
													if (!(obj is SuccessfulSortieSavedObjective value11))
													{
														throw new ArgumentOutOfRangeException();
													}
													writer.Write(value11);
												}
												else
												{
													writer.Write(value10);
												}
											}
											else
											{
												writer.Write(value9);
											}
										}
										else
										{
											writer.Write(value8);
										}
									}
									else
									{
										writer.Write(value7);
									}
								}
								else
								{
									writer.Write(value6);
								}
							}
							else
							{
								writer.Write(value5);
							}
						}
						else
						{
							writer.Write(value4);
						}
					}
					else
					{
						writer.Write(value3);
					}
				}
				else
				{
					writer.Write(value2);
				}
			}
			else
			{
				writer.Write(value);
			}
		}

		public static SavedObjective ReadSavedObjective(this NetworkReader reader)
		{
			return reader.ReadPackedInt32() switch
			{
				0 => null, 
				1 => reader.Read<NoSavedObjective>(), 
				2 => reader.Read<DestroyUnitSavedObjective>(), 
				3 => reader.Read<ReachUnitsSavedObjective>(), 
				4 => reader.Read<ReachWaypointsSavedObjective>(), 
				5 => reader.Read<WaitTimeSavedObjective>(), 
				7 => reader.Read<CaptureAirbaseSavedObjective>(), 
				8 => reader.Read<DialogueBoxSavedObjective>(), 
				9 => reader.Read<CompleteOtherObjectiveSavedObjective>(), 
				10 => reader.Read<SpotUnitSavedObjective>(), 
				11 => reader.Read<CrashAircraftSavedObjective>(), 
				12 => reader.Read<SuccessfulSortieSavedObjective>(), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}

		public static void WriteSavedOutcome(this NetworkWriter writer, SavedOutcome obj)
		{
			if (obj == null)
			{
				writer.WritePackedInt32(0);
				return;
			}
			writer.WritePackedInt32((int)(obj.OutcomeTypeEnum + 1));
			if (!(obj is NoSavedOutcome value))
			{
				if (!(obj is StartObjectiveSavedOutcome value2))
				{
					if (!(obj is CompleteObjectiveSavedOutcome value3))
					{
						if (!(obj is ShowMessageSavedOutcome value4))
						{
							if (!(obj is GiveScoreSavedOutcome value5))
							{
								if (!(obj is SpawnUnitSavedOutcome value6))
								{
									if (!(obj is RemoveUnitSavedOutcome value7))
									{
										if (!(obj is RevealUnitSavedOutcome value8))
										{
											if (!(obj is EndGameSavedOutcome value9))
											{
												if (!(obj is ModifyAirbaseSavedOutcome value10))
												{
													if (!(obj is ModifyEnvironmentSavedOutcome value11))
													{
														if (!(obj is ModifyFactionSavedOutcome value12))
														{
															throw new ArgumentOutOfRangeException();
														}
														writer.Write(value12);
													}
													else
													{
														writer.Write(value11);
													}
												}
												else
												{
													writer.Write(value10);
												}
											}
											else
											{
												writer.Write(value9);
											}
										}
										else
										{
											writer.Write(value8);
										}
									}
									else
									{
										writer.Write(value7);
									}
								}
								else
								{
									writer.Write(value6);
								}
							}
							else
							{
								writer.Write(value5);
							}
						}
						else
						{
							writer.Write(value4);
						}
					}
					else
					{
						writer.Write(value3);
					}
				}
				else
				{
					writer.Write(value2);
				}
			}
			else
			{
				writer.Write(value);
			}
		}

		public static SavedOutcome ReadSavedOutcome(this NetworkReader reader)
		{
			return reader.ReadPackedInt32() switch
			{
				0 => null, 
				1 => reader.Read<NoSavedOutcome>(), 
				2 => reader.Read<StartObjectiveSavedOutcome>(), 
				3 => reader.Read<CompleteObjectiveSavedOutcome>(), 
				4 => reader.Read<ShowMessageSavedOutcome>(), 
				5 => reader.Read<GiveScoreSavedOutcome>(), 
				6 => reader.Read<SpawnUnitSavedOutcome>(), 
				7 => reader.Read<RemoveUnitSavedOutcome>(), 
				8 => reader.Read<RevealUnitSavedOutcome>(), 
				9 => reader.Read<EndGameSavedOutcome>(), 
				10 => reader.Read<ModifyAirbaseSavedOutcome>(), 
				11 => reader.Read<ModifyEnvironmentSavedOutcome>(), 
				12 => reader.Read<ModifyFactionSavedOutcome>(), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
	}
}
