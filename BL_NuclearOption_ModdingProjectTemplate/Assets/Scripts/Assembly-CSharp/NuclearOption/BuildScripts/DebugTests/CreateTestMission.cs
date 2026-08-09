using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Objectives;
using NuclearOption.SavedMission.Outcomes;
using UnityEngine;

namespace NuclearOption.BuildScripts.DebugTests
{
	public static class CreateTestMission
	{
		public static async UniTask RunAsync(CancellationToken cancellationToken, string missionName = "testmission")
		{
			await MainMenu.WaitForLoaded(cancellationToken);
			MissionManager.NewMission(NewMissionConfig.DefaultMission());
			Mission currentMission = MissionManager.CurrentMission;
			SavedVehicle item = new SavedVehicle("test_unit")
			{
				type = "MBT",
				faction = "Boscali",
				rotation = Quaternion.identity
			};
			currentMission.vehicles.Add(item);
			SavedAirbase item2 = new SavedAirbase
			{
				UniqueName = "test_airbase",
				faction = "Boscali",
				DisplayName = "Test Airbase"
			};
			currentMission.airbases.Add(item2);
			NoSavedObjective item3 = new NoSavedObjective("mock_objective_to_complete")
			{
				Faction = "Boscali",
				DisplayName = "Mock Objective to Complete",
				Hidden = true
			};
			currentMission.objectives.Add(item3);
			NoSavedObjective item4 = new NoSavedObjective("dummy_objective")
			{
				Faction = "Boscali",
				DisplayName = "Dummy Objective",
				Hidden = true
			};
			currentMission.objectives.Add(item4);
			currentMission.objectives.Add(ConfigureDestroyUnits());
			currentMission.objectives.Add(ConfigureReachUnits());
			currentMission.objectives.Add(ConfigureReachWaypoints());
			currentMission.objectives.Add(ConfigureWaitSeconds());
			currentMission.objectives.Add(ConfigureCaptureAirbase());
			currentMission.objectives.Add(ConfigureDialogueBox());
			currentMission.objectives.Add(ConfigureCompleteOtherObjective());
			currentMission.objectives.Add(ConfigureSpotUnit());
			currentMission.objectives.Add(ConfigureCrashAircraft());
			currentMission.objectives.Add(ConfigureSuccessfulSortie());
			currentMission.outcomes.Add(ConfigureStartObjective());
			currentMission.outcomes.Add(ConfigureStopOrCompleteObjective());
			currentMission.outcomes.Add(ConfigureShowMessage());
			currentMission.outcomes.Add(ConfigureGiveScore());
			currentMission.outcomes.Add(ConfigureSpawnUnit());
			currentMission.outcomes.Add(ConfigureRemoveUnit());
			currentMission.outcomes.Add(ConfigureRevealUnit());
			currentMission.outcomes.Add(ConfigureEndGame());
			currentMission.outcomes.Add(ConfigureModifyAirbase());
			currentMission.outcomes.Add(ConfigureModifyEnvironment());
			currentMission.outcomes.Add(ConfigureModifyFaction());
			string saveName = missionName;
			MissionSaveLoad.SaveMission(MissionManager.CurrentMission, ref saveName, callBeforeSave: false);
			Debug.LogWarning("Created " + saveName + ", exiting play mode");
		}

		private static SavedObjective ConfigureDestroyUnits()
		{
			return new DestroyUnitSavedObjective("test_destroy_units")
			{
				Faction = "Boscali",
				DisplayName = "Destroy Units Test",
				Hidden = false,
				completeOrder = CompleteOrder.CompleteAll,
				completeSomePercent = 0.5f,
				targetUnits = new List<string> { "test_unit" }
			};
		}

		private static SavedObjective ConfigureReachUnits()
		{
			return new ReachUnitsSavedObjective("test_reach_units")
			{
				Faction = "Boscali",
				DisplayName = "Reach Units Test",
				Hidden = false,
				completeOrder = CompleteOrder.InOrder,
				completeSomePercent = 0.5f,
				targets = new List<SavedReachUnitData>
				{
					new SavedReachUnitData
					{
						TargetUnit = "test_unit",
						Range = 500f
					}
				}
			};
		}

		private static SavedObjective ConfigureReachWaypoints()
		{
			return new ReachWaypointsSavedObjective("test_reach_waypoints")
			{
				Faction = "Boscali",
				DisplayName = "Reach Waypoints Test",
				Hidden = false,
				completeOrder = CompleteOrder.InOrder,
				completeSomePercent = 0.5f,
				waypoints = new List<SavedWaypoint>
				{
					new SavedWaypoint
					{
						Position = new Vector3(100f, 200f, 300f),
						Range = 150f
					}
				}
			};
		}

		private static SavedObjective ConfigureWaitSeconds()
		{
			return new WaitTimeSavedObjective("test_wait_seconds")
			{
				Faction = "Boscali",
				DisplayName = "Wait Seconds Test",
				Hidden = false,
				seconds = 30f
			};
		}

		private static SavedObjective ConfigureCaptureAirbase()
		{
			return new CaptureAirbaseSavedObjective("test_capture_airbase")
			{
				Faction = "Boscali",
				DisplayName = "Capture Airbase Test",
				Hidden = false,
				completeOrder = CompleteOrder.CompleteAll,
				completeSomePercent = 0.75f,
				targetAirbases = new List<string> { "test_airbase" }
			};
		}

		private static SavedObjective ConfigureDialogueBox()
		{
			return new DialogueBoxSavedObjective("test_dialogue_box")
			{
				Faction = "Boscali",
				DisplayName = "Dialogue Box Test",
				Hidden = false,
				title = "Test Title",
				body = "Test Body Text",
				button = "OK",
				factionOnly = true
			};
		}

		private static SavedObjective ConfigureCompleteOtherObjective()
		{
			return new CompleteOtherObjectiveSavedObjective("test_complete_other_objective")
			{
				Faction = "Boscali",
				DisplayName = "Complete Other Objective Test",
				Hidden = false,
				completeOrder = CompleteOrder.CompleteSome,
				completeSomePercent = 0.75f,
				targetObjectives = new List<string> { "mock_objective_to_complete" }
			};
		}

		private static SavedObjective ConfigureSpotUnit()
		{
			return new SpotUnitSavedObjective("test_spot_unit")
			{
				Faction = "Boscali",
				DisplayName = "Spot Unit Test",
				Hidden = false,
				completeOrder = CompleteOrder.CompleteSome,
				completeSomePercent = 0.75f,
				targetUnits = new List<string> { "test_unit" }
			};
		}

		private static SavedObjective ConfigureCrashAircraft()
		{
			return new CrashAircraftSavedObjective("test_crash_aircraft")
			{
				Faction = "Boscali",
				DisplayName = "Crash Aircraft Test",
				Hidden = false,
				livesPerPlayer = 5,
				extraLives = 3,
				includeDestroy = true,
				includeEject = true
			};
		}

		private static SavedObjective ConfigureSuccessfulSortie()
		{
			return new SuccessfulSortieSavedObjective("test_successful_sortie")
			{
				Faction = "Boscali",
				DisplayName = "Successful Sortie Test",
				Hidden = false,
				minimumScore = 100f,
				additive = true
			};
		}

		private static SavedOutcome ConfigureStartObjective()
		{
			return new StartObjectiveSavedOutcome
			{
				UniqueName = "test_start_objective",
				objectivesToStart = new List<string> { "dummy_objective" }
			};
		}

		private static SavedOutcome ConfigureStopOrCompleteObjective()
		{
			return new CompleteObjectiveSavedOutcome
			{
				UniqueName = "test_stop_or_complete_objective",
				options = CompleteObjectiveOutcome.Options.Complete,
				objectivesToStart = new List<string> { "dummy_objective" }
			};
		}

		private static SavedOutcome ConfigureShowMessage()
		{
			return new ShowMessageSavedOutcome
			{
				UniqueName = "test_show_message",
				Message = "Test Message Content",
				PlaySound = true,
				ObjectiveFactionOnly = true
			};
		}

		private static SavedOutcome ConfigureGiveScore()
		{
			return new GiveScoreSavedOutcome
			{
				UniqueName = "test_give_score",
				bothFactions = true,
				playerFundsType = ChangeType.Add,
				playerFunds = 500f,
				factionFundsType = ChangeType.Set,
				factionFunds = 1000f,
				playerScoreType = ChangeType.Add,
				playerScore = 50f,
				rankType = ChangeType.Add,
				rank = 2,
				factionScoreType = ChangeType.Subtract,
				factionScore = 10f
			};
		}

		private static SavedOutcome ConfigureSpawnUnit()
		{
			return new SpawnUnitSavedOutcome
			{
				UniqueName = "test_spawn_unit",
				UnitsToSpawn = new List<string> { "test_unit" }
			};
		}

		private static SavedOutcome ConfigureRemoveUnit()
		{
			return new RemoveUnitSavedOutcome
			{
				UniqueName = "test_remove_unit",
				UnitsToRemove = new List<string> { "test_unit" }
			};
		}

		private static SavedOutcome ConfigureRevealUnit()
		{
			return new RevealUnitSavedOutcome
			{
				UniqueName = "test_reveal_unit",
				UnitsToReveal = new List<string> { "test_unit" }
			};
		}

		private static SavedOutcome ConfigureEndGame()
		{
			return new EndGameSavedOutcome
			{
				UniqueName = "test_end_game",
				endType = EndType.Victory,
				endDelay = 5f
			};
		}

		private static SavedOutcome ConfigureModifyAirbase()
		{
			return new ModifyAirbaseSavedOutcome
			{
				UniqueName = "test_modify_airbase",
				airbase = "test_airbase",
				faction = new Override<string>(isOverride: true, "Boscali"),
				disabled = new Override<bool>(isOverride: true, value: true),
				capturable = new Override<bool>(isOverride: true, value: false),
				captureDefense = new Override<float>(isOverride: true, 15f)
			};
		}

		private static SavedOutcome ConfigureModifyEnvironment()
		{
			return new ModifyEnvironmentSavedOutcome
			{
				UniqueName = "test_modify_environment",
				timeOfDay = new Override<float>(isOverride: true, 12.5f),
				weather = new Override<float>(isOverride: true, 0.5f),
				cloudAltitude = new Override<float>(isOverride: true, 1500f),
				windSpeed = new Override<float>(isOverride: true, 10f),
				windTurbulence = new Override<float>(isOverride: true, 0.1f),
				windHeading = new Override<float>(isOverride: true, 180f)
			};
		}

		private static SavedOutcome ConfigureModifyFaction()
		{
			return new ModifyFactionSavedOutcome
			{
				UniqueName = "test_modify_faction",
				bothFactions = true,
				excessFundsThreshold = new Override<float>(isOverride: true, 1000f),
				playerJoinAllowance = new Override<float>(isOverride: true, 500f),
				playerTaxRate = new Override<float>(isOverride: true, 0.15f),
				regularIncome = new Override<float>(isOverride: true, 50f),
				killReward = new Override<float>(isOverride: true, 100f),
				preventDonation = new Override<bool>(isOverride: true, value: true),
				aiAircraftLimit = new Override<float>(isOverride: true, 10f),
				reduceAIPerFriendlyPlayer = new Override<float>(isOverride: true, 1f),
				addAIPerEnemyPlayer = new Override<float>(isOverride: true, 2f),
				warheadsReserve = new Override<float>(isOverride: true, 5f),
				reserveAirframes = new Override<float>(isOverride: true, 8f),
				extraReservesPerPlayer = new Override<float>(isOverride: true, 3f),
				excessFundsDistributePercent = new Override<float>(isOverride: true, 0.5f),
				preventJoin = new Override<bool>(isOverride: true, value: false)
			};
		}
	}
}
