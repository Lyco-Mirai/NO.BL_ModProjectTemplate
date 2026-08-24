using System;
using System.Collections.Generic;
using Mirage.Serialization;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Objectives;
using NuclearOption.SavedMission.Outcomes;
using UnityEngine;

namespace NuclearOption.BuildScripts.DebugTests
{
	public class ObjectiveSerializationTests
	{
		private ObjectiveSerializationTests()
		{
		}

		public static void Run()
		{
			TestObjective(new NoSavedObjective("no_obj")
			{
				Faction = "Alpha",
				DisplayName = "No Objective",
				Hidden = true,
				Outcomes = new List<string> { "out1", "out2" }
			}, delegate
			{
			});
			TestObjective(new DestroyUnitSavedObjective("destroy_obj")
			{
				Faction = "Beta",
				DisplayName = "Destroy Unit",
				Hidden = false,
				Outcomes = new List<string> { "out1" },
				completeOrder = CompleteOrder.InOrder,
				completeSomePercent = 0.75f,
				targetUnits = new List<string> { "unit1", "unit2" }
			}, delegate(DestroyUnitSavedObjective o, DestroyUnitSavedObjective d)
			{
				if (d.completeOrder != o.completeOrder)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("DestroyUnitSavedObjective.completeOrder mismatch");
				}
				if (d.completeSomePercent != o.completeSomePercent)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("DestroyUnitSavedObjective.completeSomePercent mismatch");
				}
				CompareLists(o.targetUnits, d.targetUnits, "DestroyUnitSavedObjective.targetUnits");
			});
			TestObjective(new ReachUnitsSavedObjective("reach_obj")
			{
				completeOrder = CompleteOrder.CompleteSome,
				completeSomePercent = 0.5f,
				targets = new List<SavedReachUnitData>
				{
					new SavedReachUnitData
					{
						TargetUnit = "u1",
						Range = 100f
					}
				}
			}, delegate(ReachUnitsSavedObjective o, ReachUnitsSavedObjective d)
			{
				if (d.completeOrder != o.completeOrder)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachUnitsSavedObjective.completeOrder mismatch");
				}
				if (d.completeSomePercent != o.completeSomePercent)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachUnitsSavedObjective.completeSomePercent mismatch");
				}
				if (d.targets.Count != o.targets.Count)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachUnitsSavedObjective.targets count mismatch");
				}
				else if (d.targets[0].TargetUnit != o.targets[0].TargetUnit || d.targets[0].Range != o.targets[0].Range)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachUnitsSavedObjective.targets item mismatch");
				}
			});
			TestObjective(new ReachWaypointsSavedObjective("waypoints_obj")
			{
				completeOrder = CompleteOrder.CompleteAll,
				completeSomePercent = 1f,
				waypoints = new List<SavedWaypoint>
				{
					new SavedWaypoint
					{
						Position = new Vector3(1f, 2f, 3f),
						Range = 50f
					}
				}
			}, delegate(ReachWaypointsSavedObjective o, ReachWaypointsSavedObjective d)
			{
				if (d.completeOrder != o.completeOrder)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachWaypointsSavedObjective.completeOrder mismatch");
				}
				if (d.completeSomePercent != o.completeSomePercent)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachWaypointsSavedObjective.completeSomePercent mismatch");
				}
				if (d.waypoints.Count != o.waypoints.Count)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachWaypointsSavedObjective.waypoints count mismatch");
				}
				else if (d.waypoints[0].Position != o.waypoints[0].Position || d.waypoints[0].Range != o.waypoints[0].Range)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ReachWaypointsSavedObjective.waypoints item mismatch");
				}
			});
			TestObjective(new WaitTimeSavedObjective("wait_obj")
			{
				seconds = 45.5f
			}, delegate(WaitTimeSavedObjective o, WaitTimeSavedObjective d)
			{
				if (d.seconds != o.seconds)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("WaitTimeSavedObjective.seconds mismatch");
				}
			});
			TestObjective(new CaptureAirbaseSavedObjective("capture_obj")
			{
				completeOrder = CompleteOrder.CompleteAny,
				completeSomePercent = 0f,
				targetAirbases = new List<string> { "base1" }
			}, delegate(CaptureAirbaseSavedObjective o, CaptureAirbaseSavedObjective d)
			{
				if (d.completeOrder != o.completeOrder)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CaptureAirbaseSavedObjective.completeOrder mismatch");
				}
				if (d.completeSomePercent != o.completeSomePercent)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CaptureAirbaseSavedObjective.completeSomePercent mismatch");
				}
				CompareLists(o.targetAirbases, d.targetAirbases, "CaptureAirbaseSavedObjective.targetAirbases");
			});
			TestObjective(new DialogueBoxSavedObjective("dialogue_obj")
			{
				title = "Title",
				body = "Body text",
				button = "Yes",
				factionOnly = true
			}, delegate(DialogueBoxSavedObjective o, DialogueBoxSavedObjective d)
			{
				if (d.title != o.title)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("DialogueBoxSavedObjective.title mismatch");
				}
				if (d.body != o.body)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("DialogueBoxSavedObjective.body mismatch");
				}
				if (d.button != o.button)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("DialogueBoxSavedObjective.button mismatch");
				}
				if (d.factionOnly != o.factionOnly)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("DialogueBoxSavedObjective.factionOnly mismatch");
				}
			});
			TestObjective(new CompleteOtherObjectiveSavedObjective("comp_other")
			{
				completeOrder = CompleteOrder.InOrder,
				completeSomePercent = 0.2f,
				targetObjectives = new List<string> { "other1", "other2" }
			}, delegate(CompleteOtherObjectiveSavedObjective o, CompleteOtherObjectiveSavedObjective d)
			{
				if (d.completeOrder != o.completeOrder)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CompleteOtherObjectiveSavedObjective.completeOrder mismatch");
				}
				if (d.completeSomePercent != o.completeSomePercent)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CompleteOtherObjectiveSavedObjective.completeSomePercent mismatch");
				}
				CompareLists(o.targetObjectives, d.targetObjectives, "CompleteOtherObjectiveSavedObjective.targetObjectives");
			});
			TestObjective(new SpotUnitSavedObjective("spot")
			{
				completeOrder = CompleteOrder.CompleteAll,
				completeSomePercent = 0.5f,
				targetUnits = new List<string> { "spotted1" }
			}, delegate(SpotUnitSavedObjective o, SpotUnitSavedObjective d)
			{
				if (d.completeOrder != o.completeOrder)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("SpotUnitSavedObjective.completeOrder mismatch");
				}
				if (d.completeSomePercent != o.completeSomePercent)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("SpotUnitSavedObjective.completeSomePercent mismatch");
				}
				CompareLists(o.targetUnits, d.targetUnits, "SpotUnitSavedObjective.targetUnits");
			});
			TestObjective(new CrashAircraftSavedObjective("crash")
			{
				livesPerPlayer = 3,
				extraLives = 5,
				includeDestroy = true,
				includeEject = false
			}, delegate(CrashAircraftSavedObjective o, CrashAircraftSavedObjective d)
			{
				if (d.livesPerPlayer != o.livesPerPlayer)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CrashAircraftSavedObjective.livesPerPlayer mismatch");
				}
				if (d.extraLives != o.extraLives)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CrashAircraftSavedObjective.extraLives mismatch");
				}
				if (d.includeDestroy != o.includeDestroy)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CrashAircraftSavedObjective.includeDestroy mismatch");
				}
				if (d.includeEject != o.includeEject)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CrashAircraftSavedObjective.includeEject mismatch");
				}
			});
			TestObjective(new SuccessfulSortieSavedObjective("sortie")
			{
				minimumScore = 1500f
			}, delegate(SuccessfulSortieSavedObjective o, SuccessfulSortieSavedObjective d)
			{
				if (d.minimumScore != o.minimumScore)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("SuccessfulSortieSavedObjective.minimumScore mismatch");
				}
			});
			TestOutcome(new StartObjectiveSavedOutcome
			{
				UniqueName = "start_outc",
				objectivesToStart = new List<string> { "obj1" }
			}, delegate(StartObjectiveSavedOutcome o, StartObjectiveSavedOutcome d)
			{
				CompareLists(o.objectivesToStart, d.objectivesToStart, "StartObjectiveSavedOutcome.objectivesToStart");
			});
			TestOutcome(new CompleteObjectiveSavedOutcome
			{
				UniqueName = "comp_outc",
				options = CompleteObjectiveOutcome.Options.Complete,
				objectivesToStart = new List<string> { "obj2" }
			}, delegate(CompleteObjectiveSavedOutcome o, CompleteObjectiveSavedOutcome d)
			{
				if (d.options != o.options)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("CompleteObjectiveSavedOutcome.options mismatch");
				}
				CompareLists(o.objectivesToStart, d.objectivesToStart, "CompleteObjectiveSavedOutcome.objectivesToStart");
			});
			TestOutcome(new ShowMessageSavedOutcome
			{
				UniqueName = "show_msg",
				Message = "Hello World",
				PlaySound = true,
				ObjectiveFactionOnly = false
			}, delegate(ShowMessageSavedOutcome o, ShowMessageSavedOutcome d)
			{
				if (d.Message != o.Message)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ShowMessageSavedOutcome.Message mismatch");
				}
				if (d.PlaySound != o.PlaySound)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ShowMessageSavedOutcome.PlaySound mismatch");
				}
				if (d.ObjectiveFactionOnly != o.ObjectiveFactionOnly)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ShowMessageSavedOutcome.ObjectiveFactionOnly mismatch");
				}
			});
			TestOutcome(new GiveScoreSavedOutcome
			{
				UniqueName = "give_score",
				bothFactions = true,
				playerFundsType = ChangeType.Add,
				playerFunds = 1000f,
				factionFundsType = ChangeType.Set,
				factionFunds = 1.5f,
				playerScoreType = ChangeType.Set,
				playerScore = 500f,
				rankType = ChangeType.Add,
				rank = 2,
				factionScoreType = ChangeType.Add,
				factionScore = 200f
			}, delegate(GiveScoreSavedOutcome o, GiveScoreSavedOutcome d)
			{
				if (d.bothFactions != o.bothFactions)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.bothFactions mismatch");
				}
				if (d.playerFundsType != o.playerFundsType)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.playerFundsType mismatch");
				}
				if (d.playerFunds != o.playerFunds)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.playerFunds mismatch");
				}
				if (d.factionFundsType != o.factionFundsType)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.factionFundsType mismatch");
				}
				if (d.factionFunds != o.factionFunds)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.factionFunds mismatch");
				}
				if (d.playerScoreType != o.playerScoreType)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.playerScoreType mismatch");
				}
				if (d.playerScore != o.playerScore)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.playerScore mismatch");
				}
				if (d.rankType != o.rankType)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.rankType mismatch");
				}
				if (d.rank != o.rank)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.rank mismatch");
				}
				if (d.factionScoreType != o.factionScoreType)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.factionScoreType mismatch");
				}
				if (d.factionScore != o.factionScore)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("GiveScoreSavedOutcome.factionScore mismatch");
				}
			});
			TestOutcome(new SpawnUnitSavedOutcome
			{
				UniqueName = "spawn_unit",
				UnitsToSpawn = new List<string> { "u1", "u2" }
			}, delegate(SpawnUnitSavedOutcome o, SpawnUnitSavedOutcome d)
			{
				CompareLists(o.UnitsToSpawn, d.UnitsToSpawn, "SpawnUnitSavedOutcome.UnitsToSpawn");
			});
			TestOutcome(new RemoveUnitSavedOutcome
			{
				UniqueName = "remove_unit",
				UnitsToRemove = new List<string> { "u3" }
			}, delegate(RemoveUnitSavedOutcome o, RemoveUnitSavedOutcome d)
			{
				CompareLists(o.UnitsToRemove, d.UnitsToRemove, "RemoveUnitSavedOutcome.UnitsToRemove");
			});
			TestOutcome(new RevealUnitSavedOutcome
			{
				UniqueName = "reveal_unit",
				UnitsToReveal = new List<string> { "u4" }
			}, delegate(RevealUnitSavedOutcome o, RevealUnitSavedOutcome d)
			{
				CompareLists(o.UnitsToReveal, d.UnitsToReveal, "RevealUnitSavedOutcome.UnitsToReveal");
			});
			TestOutcome(new EndGameSavedOutcome
			{
				UniqueName = "end_game",
				endType = EndType.Victory,
				endDelay = 5.5f
			}, delegate(EndGameSavedOutcome o, EndGameSavedOutcome d)
			{
				if (d.endType != o.endType)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("EndGameSavedOutcome.endType mismatch");
				}
				if (d.endDelay != o.endDelay)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("EndGameSavedOutcome.endDelay mismatch");
				}
			});
			TestOutcome(new ModifyAirbaseSavedOutcome
			{
				UniqueName = "modify_airbase",
				airbase = "airbase_1",
				faction = new Override<string>
				{
					Value = "Alpha",
					IsOverride = true
				},
				disabled = new Override<bool>
				{
					Value = true,
					IsOverride = true
				},
				capturable = new Override<bool>
				{
					Value = false,
					IsOverride = false
				},
				captureDefense = new Override<float>
				{
					Value = 10f,
					IsOverride = true
				}
			}, delegate(ModifyAirbaseSavedOutcome o, ModifyAirbaseSavedOutcome d)
			{
				if (d.airbase != o.airbase)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ModifyAirbaseSavedOutcome.airbase mismatch");
				}
				CompareOverride(o.faction, d.faction, "ModifyAirbaseSavedOutcome.faction");
				CompareOverride(o.disabled, d.disabled, "ModifyAirbaseSavedOutcome.disabled");
				CompareOverride(o.capturable, d.capturable, "ModifyAirbaseSavedOutcome.capturable");
				CompareOverride(o.captureDefense, d.captureDefense, "ModifyAirbaseSavedOutcome.captureDefense");
			});
			TestOutcome(new ModifyEnvironmentSavedOutcome
			{
				UniqueName = "modify_env",
				timeOfDay = new Override<float>
				{
					Value = 12f,
					IsOverride = true
				},
				weather = new Override<float>
				{
					Value = 0f,
					IsOverride = false
				},
				cloudAltitude = new Override<float>
				{
					Value = 2000f,
					IsOverride = true
				},
				windSpeed = new Override<float>
				{
					Value = 15f,
					IsOverride = true
				},
				windTurbulence = new Override<float>
				{
					Value = 5f,
					IsOverride = true
				},
				windHeading = new Override<float>
				{
					Value = 180f,
					IsOverride = true
				}
			}, delegate(ModifyEnvironmentSavedOutcome o, ModifyEnvironmentSavedOutcome d)
			{
				CompareOverride(o.timeOfDay, d.timeOfDay, "ModifyEnvironmentSavedOutcome.timeOfDay");
				CompareOverride(o.weather, d.weather, "ModifyEnvironmentSavedOutcome.weather");
				CompareOverride(o.cloudAltitude, d.cloudAltitude, "ModifyEnvironmentSavedOutcome.cloudAltitude");
				CompareOverride(o.windSpeed, d.windSpeed, "ModifyEnvironmentSavedOutcome.windSpeed");
				CompareOverride(o.windTurbulence, d.windTurbulence, "ModifyEnvironmentSavedOutcome.windTurbulence");
				CompareOverride(o.windHeading, d.windHeading, "ModifyEnvironmentSavedOutcome.windHeading");
			});
			TestOutcome(new ModifyFactionSavedOutcome
			{
				UniqueName = "modify_faction",
				bothFactions = true,
				excessFundsThreshold = new Override<float>
				{
					Value = 1000f,
					IsOverride = true
				},
				playerJoinAllowance = new Override<float>
				{
					Value = 500f,
					IsOverride = true
				},
				playerTaxRate = new Override<float>
				{
					Value = 0.1f,
					IsOverride = true
				},
				regularIncome = new Override<float>
				{
					Value = 50f,
					IsOverride = true
				},
				killReward = new Override<float>
				{
					Value = 100f,
					IsOverride = true
				},
				preventDonation = new Override<bool>
				{
					Value = true,
					IsOverride = true
				},
				aiAircraftLimit = new Override<float>
				{
					Value = 10f,
					IsOverride = true
				},
				reduceAIPerFriendlyPlayer = new Override<float>
				{
					Value = 1f,
					IsOverride = true
				},
				addAIPerEnemyPlayer = new Override<float>
				{
					Value = 2f,
					IsOverride = true
				},
				warheadsReserve = new Override<float>
				{
					Value = 20f,
					IsOverride = true
				},
				reserveAirframes = new Override<float>
				{
					Value = 30f,
					IsOverride = true
				},
				extraReservesPerPlayer = new Override<float>
				{
					Value = 5f,
					IsOverride = true
				},
				excessFundsDistributePercent = new Override<float>
				{
					Value = 0.5f,
					IsOverride = true
				},
				preventJoin = new Override<bool>
				{
					Value = false,
					IsOverride = true
				}
			}, delegate(ModifyFactionSavedOutcome o, ModifyFactionSavedOutcome d)
			{
				if (d.bothFactions != o.bothFactions)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("ModifyFactionSavedOutcome.bothFactions mismatch");
				}
				CompareOverride(o.excessFundsThreshold, d.excessFundsThreshold, "ModifyFactionSavedOutcome.excessFundsThreshold");
				CompareOverride(o.playerJoinAllowance, d.playerJoinAllowance, "ModifyFactionSavedOutcome.playerJoinAllowance");
				CompareOverride(o.playerTaxRate, d.playerTaxRate, "ModifyFactionSavedOutcome.playerTaxRate");
				CompareOverride(o.regularIncome, d.regularIncome, "ModifyFactionSavedOutcome.regularIncome");
				CompareOverride(o.killReward, d.killReward, "ModifyFactionSavedOutcome.killReward");
				CompareOverride(o.preventDonation, d.preventDonation, "ModifyFactionSavedOutcome.preventDonation");
				CompareOverride(o.aiAircraftLimit, d.aiAircraftLimit, "ModifyFactionSavedOutcome.aiAircraftLimit");
				CompareOverride(o.reduceAIPerFriendlyPlayer, d.reduceAIPerFriendlyPlayer, "ModifyFactionSavedOutcome.reduceAIPerFriendlyPlayer");
				CompareOverride(o.addAIPerEnemyPlayer, d.addAIPerEnemyPlayer, "ModifyFactionSavedOutcome.addAIPerEnemyPlayer");
				CompareOverride(o.warheadsReserve, d.warheadsReserve, "ModifyFactionSavedOutcome.warheadsReserve");
				CompareOverride(o.reserveAirframes, d.reserveAirframes, "ModifyFactionSavedOutcome.reserveAirframes");
				CompareOverride(o.extraReservesPerPlayer, d.extraReservesPerPlayer, "ModifyFactionSavedOutcome.extraReservesPerPlayer");
				CompareOverride(o.excessFundsDistributePercent, d.excessFundsDistributePercent, "ModifyFactionSavedOutcome.excessFundsDistributePercent");
				CompareOverride(o.preventJoin, d.preventJoin, "ModifyFactionSavedOutcome.preventJoin");
			});
		}

		private static void TestObjective<T>(T original, Action<T, T> assertFields) where T : SavedObjective
		{
			try
			{
				using PooledNetworkWriter pooledNetworkWriter = NetworkWriterPool.GetWriter();
				pooledNetworkWriter.WriteSavedObjective(original);
				using PooledNetworkReader reader = NetworkReaderPool.GetReader(pooledNetworkWriter.ToArraySegment(), null);
				SavedObjective savedObjective = reader.ReadSavedObjective();
				if (savedObjective == null)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] Deserialized objective is null for " + typeof(T).Name);
					return;
				}
				if (savedObjective.GetType() != typeof(T))
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] Type mismatch: expected " + typeof(T).Name + ", got " + savedObjective.GetType().Name);
					return;
				}
				T val = (T)savedObjective;
				if (val.UniqueName != original.UniqueName)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] " + typeof(T).Name + ".UniqueName mismatch. Expected '" + original.UniqueName + "', got '" + val.UniqueName + "'");
				}
				if (val.Faction != original.Faction)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] " + typeof(T).Name + ".Faction mismatch. Expected '" + original.Faction + "', got '" + val.Faction + "'");
				}
				if (val.DisplayName != original.DisplayName)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] " + typeof(T).Name + ".DisplayName mismatch. Expected '" + original.DisplayName + "', got '" + val.DisplayName + "'");
				}
				if (val.Hidden != original.Hidden)
				{
					ColorLog<ObjectiveSerializationTests>.LogError($"[SerializationTest] {typeof(T).Name}.Hidden mismatch. Expected '{original.Hidden}', got '{val.Hidden}'");
				}
				CompareLists(original.Outcomes, val.Outcomes, typeof(T).Name + ".Outcomes");
				assertFields(original, val);
			}
			catch (Exception arg)
			{
				ColorLog<ObjectiveSerializationTests>.LogError($"[SerializationTest] Exception testing {typeof(T).Name}: {arg}");
			}
		}

		private static void TestOutcome<T>(T original, Action<T, T> assertFields) where T : SavedOutcome
		{
			try
			{
				using PooledNetworkWriter pooledNetworkWriter = NetworkWriterPool.GetWriter();
				pooledNetworkWriter.WriteSavedOutcome(original);
				using PooledNetworkReader reader = NetworkReaderPool.GetReader(pooledNetworkWriter.ToArraySegment(), null);
				SavedOutcome savedOutcome = reader.ReadSavedOutcome();
				if (savedOutcome == null)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] Deserialized outcome is null for " + typeof(T).Name);
					return;
				}
				if (savedOutcome.GetType() != typeof(T))
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] Type mismatch: expected " + typeof(T).Name + ", got " + savedOutcome.GetType().Name);
					return;
				}
				T val = (T)savedOutcome;
				if (val.UniqueName != original.UniqueName)
				{
					ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] " + typeof(T).Name + ".UniqueName mismatch. Expected '" + original.UniqueName + "', got '" + val.UniqueName + "'");
				}
				assertFields(original, val);
			}
			catch (Exception arg)
			{
				ColorLog<ObjectiveSerializationTests>.LogError($"[SerializationTest] Exception testing {typeof(T).Name}: {arg}");
			}
		}

		private static void CompareOverride<T>(Override<T> o, Override<T> d, string name) where T : IEquatable<T>
		{
			if (o.IsOverride != d.IsOverride)
			{
				ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] " + name + ".IsOverride mismatch");
			}
			if (o.IsOverride && !EqualityComparer<T>.Default.Equals(o.Value, d.Value))
			{
				ColorLog<ObjectiveSerializationTests>.LogError($"[SerializationTest] {name}.Value mismatch. Expected '{o.Value}', got '{d.Value}'");
			}
		}

		private static void CompareLists<T>(List<T> o, List<T> d, string name)
		{
			if (o == null && d == null)
			{
				return;
			}
			if (o == null || d == null)
			{
				ColorLog<ObjectiveSerializationTests>.LogError("[SerializationTest] " + name + " null mismatch");
				return;
			}
			if (o.Count != d.Count)
			{
				ColorLog<ObjectiveSerializationTests>.LogError($"[SerializationTest] {name} count mismatch. Expected {o.Count}, got {d.Count}");
				return;
			}
			for (int i = 0; i < o.Count; i++)
			{
				if (!EqualityComparer<T>.Default.Equals(o[i], d[i]))
				{
					ColorLog<ObjectiveSerializationTests>.LogError($"[SerializationTest] {name}[{i}] mismatch");
				}
			}
		}
	}
}
