using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirage.Authentication;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using Mirage.Serialization.BrotliCompression;
using NuclearOption;
using NuclearOption.NetworkTransforms;
using NuclearOption.Networking;
using NuclearOption.Networking.Authentication;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.Objectives;
using NuclearOption.SavedMission.Outcomes;
using NuclearOption.SceneLoading;
using RoadPathfinding;
using Steamworks;
using UnityEngine;

namespace Mirage
{
	[StructLayout(LayoutKind.Auto, CharSet = CharSet.Auto)]
	public static class GeneratedNetworkCode
	{
		public static void _Write_Mirage_002ESceneNotReadyMessage(NetworkWriter writer, SceneNotReadyMessage value)
		{
		}

		public static SceneNotReadyMessage _Read_Mirage_002ESceneNotReadyMessage(NetworkReader reader)
		{
			return default(SceneNotReadyMessage);
		}

		public static void _Write_Mirage_002EAddCharacterMessage(NetworkWriter writer, AddCharacterMessage value)
		{
		}

		public static AddCharacterMessage _Read_Mirage_002EAddCharacterMessage(NetworkReader reader)
		{
			return default(AddCharacterMessage);
		}

		public static void _Write_Mirage_002ESceneMessage(NetworkWriter writer, SceneMessage value)
		{
			writer.WriteString(value.ScenePath);
		}

		public static SceneMessage _Read_Mirage_002ESceneMessage(NetworkReader reader)
		{
			return new SceneMessage
			{
				ScenePath = reader.ReadString()
			};
		}

		public static void _Write_Mirage_002ESceneReadyMessage(NetworkWriter writer, SceneReadyMessage value)
		{
		}

		public static SceneReadyMessage _Read_Mirage_002ESceneReadyMessage(NetworkReader reader)
		{
			return default(SceneReadyMessage);
		}

		public static void _Write_Mirage_002ESpawnMessage(NetworkWriter writer, SpawnMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
			writer.WriteBooleanExtension(value.IsLocalPlayer);
			writer.WriteBooleanExtension(value.IsOwner);
			_Write_System_002ENullable_00601_003CSystem_002EUInt64_003E(writer, value.SceneId);
			_Write_System_002ENullable_00601_003CSystem_002EInt32_003E(writer, value.PrefabHash);
			_Write_Mirage_002ESpawnValues(writer, value.SpawnValues);
			writer.WriteBytesAndSizeSegment(value.Payload);
		}

		public static void _Write_System_002ENullable_00601_003CSystem_002EUInt64_003E(NetworkWriter writer, ulong? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_System_002ENullable_00601_003CSystem_002EInt32_003E(NetworkWriter writer, int? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_Mirage_002ESpawnValues(NetworkWriter writer, SpawnValues value)
		{
			_Write_System_002ENullable_00601_003CUnityEngine_002EVector3_003E(writer, value.Position);
			_Write_System_002ENullable_00601_003CUnityEngine_002EQuaternion_003E(writer, value.Rotation);
			_Write_System_002ENullable_00601_003CUnityEngine_002EVector3_003E(writer, value.Scale);
			writer.WriteString(value.Name);
			_Write_System_002ENullable_00601_003CSystem_002EBoolean_003E(writer, value.SelfActive);
		}

		public static void _Write_System_002ENullable_00601_003CUnityEngine_002EVector3_003E(NetworkWriter writer, Vector3? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_System_002ENullable_00601_003CUnityEngine_002EQuaternion_003E(NetworkWriter writer, Quaternion? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_System_002ENullable_00601_003CSystem_002EBoolean_003E(NetworkWriter writer, bool? value)
		{
			writer.WriteNullable(value);
		}

		public static SpawnMessage _Read_Mirage_002ESpawnMessage(NetworkReader reader)
		{
			return new SpawnMessage
			{
				NetId = reader.ReadPackedUInt32(),
				IsLocalPlayer = reader.ReadBooleanExtension(),
				IsOwner = reader.ReadBooleanExtension(),
				SceneId = _Read_System_002ENullable_00601_003CSystem_002EUInt64_003E(reader),
				PrefabHash = _Read_System_002ENullable_00601_003CSystem_002EInt32_003E(reader),
				SpawnValues = _Read_Mirage_002ESpawnValues(reader),
				Payload = reader.ReadBytesAndSizeSegment()
			};
		}

		public static ulong? _Read_System_002ENullable_00601_003CSystem_002EUInt64_003E(NetworkReader reader)
		{
			return reader.ReadNullable<ulong>();
		}

		public static int? _Read_System_002ENullable_00601_003CSystem_002EInt32_003E(NetworkReader reader)
		{
			return reader.ReadNullable<int>();
		}

		public static SpawnValues _Read_Mirage_002ESpawnValues(NetworkReader reader)
		{
			return new SpawnValues
			{
				Position = _Read_System_002ENullable_00601_003CUnityEngine_002EVector3_003E(reader),
				Rotation = _Read_System_002ENullable_00601_003CUnityEngine_002EQuaternion_003E(reader),
				Scale = _Read_System_002ENullable_00601_003CUnityEngine_002EVector3_003E(reader),
				Name = reader.ReadString(),
				SelfActive = _Read_System_002ENullable_00601_003CSystem_002EBoolean_003E(reader)
			};
		}

		public static Vector3? _Read_System_002ENullable_00601_003CUnityEngine_002EVector3_003E(NetworkReader reader)
		{
			return reader.ReadNullable<Vector3>();
		}

		public static Quaternion? _Read_System_002ENullable_00601_003CUnityEngine_002EQuaternion_003E(NetworkReader reader)
		{
			return reader.ReadNullable<Quaternion>();
		}

		public static bool? _Read_System_002ENullable_00601_003CSystem_002EBoolean_003E(NetworkReader reader)
		{
			return reader.ReadNullable<bool>();
		}

		public static void _Write_Mirage_002ERemoveAuthorityMessage(NetworkWriter writer, RemoveAuthorityMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
		}

		public static RemoveAuthorityMessage _Read_Mirage_002ERemoveAuthorityMessage(NetworkReader reader)
		{
			return new RemoveAuthorityMessage
			{
				NetId = reader.ReadPackedUInt32()
			};
		}

		public static void _Write_Mirage_002ERemoveCharacterMessage(NetworkWriter writer, RemoveCharacterMessage value)
		{
			writer.WriteBooleanExtension(value.KeepAuthority);
		}

		public static RemoveCharacterMessage _Read_Mirage_002ERemoveCharacterMessage(NetworkReader reader)
		{
			return new RemoveCharacterMessage
			{
				KeepAuthority = reader.ReadBooleanExtension()
			};
		}

		public static void _Write_Mirage_002EObjectDestroyMessage(NetworkWriter writer, ObjectDestroyMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
		}

		public static ObjectDestroyMessage _Read_Mirage_002EObjectDestroyMessage(NetworkReader reader)
		{
			return new ObjectDestroyMessage
			{
				NetId = reader.ReadPackedUInt32()
			};
		}

		public static void _Write_Mirage_002EObjectHideMessage(NetworkWriter writer, ObjectHideMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
		}

		public static ObjectHideMessage _Read_Mirage_002EObjectHideMessage(NetworkReader reader)
		{
			return new ObjectHideMessage
			{
				NetId = reader.ReadPackedUInt32()
			};
		}

		public static void _Write_Mirage_002EUpdateVarsMessage(NetworkWriter writer, UpdateVarsMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
			writer.WriteBytesAndSizeSegment(value.Payload);
		}

		public static UpdateVarsMessage _Read_Mirage_002EUpdateVarsMessage(NetworkReader reader)
		{
			return new UpdateVarsMessage
			{
				NetId = reader.ReadPackedUInt32(),
				Payload = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_Mirage_002ENetworkPingMessage(NetworkWriter writer, NetworkPingMessage value)
		{
			writer.WriteDoubleConverter(value.ClientTime);
		}

		public static NetworkPingMessage _Read_Mirage_002ENetworkPingMessage(NetworkReader reader)
		{
			return new NetworkPingMessage
			{
				ClientTime = reader.ReadDoubleConverter()
			};
		}

		public static void _Write_Mirage_002ENetworkPongMessage(NetworkWriter writer, NetworkPongMessage value)
		{
			writer.WriteDoubleConverter(value.ClientTime);
			writer.WriteDoubleConverter(value.ServerTime);
		}

		public static NetworkPongMessage _Read_Mirage_002ENetworkPongMessage(NetworkReader reader)
		{
			return new NetworkPongMessage
			{
				ClientTime = reader.ReadDoubleConverter(),
				ServerTime = reader.ReadDoubleConverter()
			};
		}

		public static void _Write_Mirage_002ERemoteCalls_002ERpcMessage(NetworkWriter writer, RpcMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
			writer.WritePackedInt32(value.FunctionIndex);
			writer.WriteBytesAndSizeSegment(value.Payload);
		}

		public static RpcMessage _Read_Mirage_002ERemoteCalls_002ERpcMessage(NetworkReader reader)
		{
			return new RpcMessage
			{
				NetId = reader.ReadPackedUInt32(),
				FunctionIndex = reader.ReadPackedInt32(),
				Payload = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_Mirage_002ERemoteCalls_002ERpcWithReplyMessage(NetworkWriter writer, RpcWithReplyMessage value)
		{
			writer.WritePackedUInt32(value.NetId);
			writer.WritePackedInt32(value.FunctionIndex);
			writer.WritePackedInt32(value.ReplyId);
			writer.WriteBytesAndSizeSegment(value.Payload);
		}

		public static RpcWithReplyMessage _Read_Mirage_002ERemoteCalls_002ERpcWithReplyMessage(NetworkReader reader)
		{
			return new RpcWithReplyMessage
			{
				NetId = reader.ReadPackedUInt32(),
				FunctionIndex = reader.ReadPackedInt32(),
				ReplyId = reader.ReadPackedInt32(),
				Payload = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_Mirage_002ERemoteCalls_002ERpcReply(NetworkWriter writer, RpcReply value)
		{
			writer.WritePackedInt32(value.ReplyId);
			writer.WriteBooleanExtension(value.Success);
			writer.WriteBytesAndSizeSegment(value.Payload);
		}

		public static RpcReply _Read_Mirage_002ERemoteCalls_002ERpcReply(NetworkReader reader)
		{
			return new RpcReply
			{
				ReplyId = reader.ReadPackedInt32(),
				Success = reader.ReadBooleanExtension(),
				Payload = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_Mirage_002EAuthentication_002EAuthMessage(NetworkWriter writer, AuthMessage value)
		{
			writer.WriteBytesAndSizeSegment(value.Payload);
		}

		public static AuthMessage _Read_Mirage_002EAuthentication_002EAuthMessage(NetworkReader reader)
		{
			return new AuthMessage
			{
				Payload = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_Mirage_002EAuthentication_002EAuthSuccessMessage(NetworkWriter writer, AuthSuccessMessage value)
		{
			writer.WriteString(value.AuthenticatorName);
		}

		public static AuthSuccessMessage _Read_Mirage_002EAuthentication_002EAuthSuccessMessage(NetworkReader reader)
		{
			return new AuthSuccessMessage
			{
				AuthenticatorName = reader.ReadString()
			};
		}

		public static MissionMessages.ActiveDialogueState _Read_MissionMessages_002FActiveDialogueState(NetworkReader reader)
		{
			return new MissionMessages.ActiveDialogueState
			{
				id = reader.ReadPackedInt32(),
				title = reader.ReadString(),
				body = reader.ReadString(),
				button = reader.ReadString(),
				filterFaction = _Read_FactionHQ(reader)
			};
		}

		public static FactionHQ _Read_FactionHQ(NetworkReader reader)
		{
			return (FactionHQ)reader.ReadNetworkBehaviour();
		}

		public static void _Write_MissionMessages_002FActiveDialogueState(NetworkWriter writer, MissionMessages.ActiveDialogueState value)
		{
			writer.WritePackedInt32(value.id);
			writer.WriteString(value.title);
			writer.WriteString(value.body);
			writer.WriteString(value.button);
			_Write_FactionHQ(writer, value.filterFaction);
		}

		public static void _Write_FactionHQ(NetworkWriter writer, FactionHQ value)
		{
			writer.WriteNetworkBehaviour(value);
		}

		public static NetworkMission.SyncMissionHeader _Read_NetworkMission_002FSyncMissionHeader(NetworkReader reader)
		{
			return new NetworkMission.SyncMissionHeader
			{
				Name = reader.ReadString(),
				State = _Read_NetworkMission_002FState(reader),
				JsonVersion = reader.ReadPackedInt32(),
				missionSettings = _Read_NuclearOption_002ESavedMission_002EMissionSettings(reader),
				environment = _Read_NuclearOption_002ESavedMission_002EMissionEnvironment(reader)
			};
		}

		public static NetworkMission.State _Read_NetworkMission_002FState(NetworkReader reader)
		{
			return (NetworkMission.State)reader.ReadPackedInt32();
		}

		public static MissionSettings _Read_NuclearOption_002ESavedMission_002EMissionSettings(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			MissionSettings missionSettings = new MissionSettings();
			missionSettings.description = reader.ReadString();
			missionSettings.allowEventContent = reader.ReadBooleanExtension();
			missionSettings.Tags = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionTag_003E(reader);
			missionSettings.playerMode = _Read_NuclearOption_002ESavedMission_002EPlayerMode(reader);
			missionSettings.allowRespawn = reader.ReadBooleanExtension();
			missionSettings.playerStartingRank = reader.ReadPackedInt32();
			missionSettings.rankMultiplier = reader.ReadSingleConverter();
			missionSettings.successfulSortieBonus = reader.ReadSingleConverter();
			missionSettings.nuclearEscalationThreshold = reader.ReadSingleConverter();
			missionSettings.strategicEscalationThreshold = reader.ReadSingleConverter();
			missionSettings.minRankTacticalWarhead = reader.ReadPackedInt32();
			missionSettings.minRankStrategicWarhead = reader.ReadPackedInt32();
			missionSettings.cameraStartPosition = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E(reader);
			missionSettings.missionRoads = _Read_RoadPathfinding_002ERoadNetwork(reader);
			missionSettings.missionSeaLanes = _Read_RoadPathfinding_002ERoadNetwork(reader);
			missionSettings.wrecksMaxNumber = reader.ReadPackedInt32();
			missionSettings.wrecksDecayTime = reader.ReadSingleConverter();
			return missionSettings;
		}

		public static MissionTag _Read_NuclearOption_002ESavedMission_002EMissionTag(NetworkReader reader)
		{
			return new MissionTag
			{
				Tag = reader.ReadString(),
				Color = reader.ReadColor(),
				SortOrder = reader.ReadPackedInt32()
			};
		}

		public static List<MissionTag> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionTag_003E(NetworkReader reader)
		{
			return reader.ReadList<MissionTag>();
		}

		public static PlayerMode _Read_NuclearOption_002ESavedMission_002EPlayerMode(NetworkReader reader)
		{
			return (PlayerMode)reader.ReadPackedInt32();
		}

		public static PositionRotation _Read_NuclearOption_002ESavedMission_002EPositionRotation(NetworkReader reader)
		{
			return new PositionRotation
			{
				Position = reader.ReadGlobalPosition(),
				Rotation = reader.ReadQuaternion()
			};
		}

		public static Override<PositionRotation> _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E(NetworkReader reader)
		{
			return reader.ReadOverride<PositionRotation>();
		}

		public static RoadNetwork _Read_RoadPathfinding_002ERoadNetwork(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			RoadNetwork roadNetwork = new RoadNetwork();
			roadNetwork.roads = _Read_System_002ECollections_002EGeneric_002EList_00601_003CRoadPathfinding_002ERoad_003E(reader);
			return roadNetwork;
		}

		public static Road _Read_RoadPathfinding_002ERoad(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			Road road = new Road();
			road.points = _Read_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(reader);
			road.length = reader.ReadSingleConverter();
			return road;
		}

		public static List<GlobalPosition> _Read_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(NetworkReader reader)
		{
			return reader.ReadList<GlobalPosition>();
		}

		public static List<Road> _Read_System_002ECollections_002EGeneric_002EList_00601_003CRoadPathfinding_002ERoad_003E(NetworkReader reader)
		{
			return reader.ReadList<Road>();
		}

		public static MissionEnvironment _Read_NuclearOption_002ESavedMission_002EMissionEnvironment(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			MissionEnvironment missionEnvironment = new MissionEnvironment();
			missionEnvironment.timeOfDay = reader.ReadSingleConverter();
			missionEnvironment.timeFactor = reader.ReadSingleConverter();
			missionEnvironment.weatherIntensity = reader.ReadSingleConverter();
			missionEnvironment.cloudAltitude = reader.ReadSingleConverter();
			missionEnvironment.windSpeed = reader.ReadSingleConverter();
			missionEnvironment.windTurbulence = reader.ReadSingleConverter();
			missionEnvironment.windHeading = reader.ReadSingleConverter();
			missionEnvironment.windRandomHeading = reader.ReadSingleConverter();
			missionEnvironment.moonPhase = reader.ReadSingleConverter();
			return missionEnvironment;
		}

		public static void _Write_NetworkMission_002FSyncMissionHeader(NetworkWriter writer, NetworkMission.SyncMissionHeader value)
		{
			writer.WriteString(value.Name);
			_Write_NetworkMission_002FState(writer, value.State);
			writer.WritePackedInt32(value.JsonVersion);
			_Write_NuclearOption_002ESavedMission_002EMissionSettings(writer, value.missionSettings);
			_Write_NuclearOption_002ESavedMission_002EMissionEnvironment(writer, value.environment);
		}

		public static void _Write_NetworkMission_002FState(NetworkWriter writer, NetworkMission.State value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EMissionSettings(NetworkWriter writer, MissionSettings value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.description);
			writer.WriteBooleanExtension(value.allowEventContent);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionTag_003E(writer, value.Tags);
			_Write_NuclearOption_002ESavedMission_002EPlayerMode(writer, value.playerMode);
			writer.WriteBooleanExtension(value.allowRespawn);
			writer.WritePackedInt32(value.playerStartingRank);
			writer.WriteSingleConverter(value.rankMultiplier);
			writer.WriteSingleConverter(value.successfulSortieBonus);
			writer.WriteSingleConverter(value.nuclearEscalationThreshold);
			writer.WriteSingleConverter(value.strategicEscalationThreshold);
			writer.WritePackedInt32(value.minRankTacticalWarhead);
			writer.WritePackedInt32(value.minRankStrategicWarhead);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E(writer, value.cameraStartPosition);
			_Write_RoadPathfinding_002ERoadNetwork(writer, value.missionRoads);
			_Write_RoadPathfinding_002ERoadNetwork(writer, value.missionSeaLanes);
			writer.WritePackedInt32(value.wrecksMaxNumber);
			writer.WriteSingleConverter(value.wrecksDecayTime);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EMissionTag(NetworkWriter writer, MissionTag value)
		{
			writer.WriteString(value.Tag);
			writer.WriteColor(value.Color);
			writer.WritePackedInt32(value.SortOrder);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionTag_003E(NetworkWriter writer, List<MissionTag> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EPlayerMode(NetworkWriter writer, PlayerMode value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EPositionRotation(NetworkWriter writer, PositionRotation value)
		{
			writer.WriteGlobalPosition(value.Position);
			writer.WriteQuaternion(value.Rotation);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E(NetworkWriter writer, Override<PositionRotation> value)
		{
			writer.WriteOverride(value);
		}

		public static void _Write_RoadPathfinding_002ERoadNetwork(NetworkWriter writer, RoadNetwork value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CRoadPathfinding_002ERoad_003E(writer, value.roads);
		}

		public static void _Write_RoadPathfinding_002ERoad(NetworkWriter writer, Road value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(writer, value.points);
			writer.WriteSingleConverter(value.length);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(NetworkWriter writer, List<GlobalPosition> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CRoadPathfinding_002ERoad_003E(NetworkWriter writer, List<Road> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EMissionEnvironment(NetworkWriter writer, MissionEnvironment value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteSingleConverter(value.timeOfDay);
			writer.WriteSingleConverter(value.timeFactor);
			writer.WriteSingleConverter(value.weatherIntensity);
			writer.WriteSingleConverter(value.cloudAltitude);
			writer.WriteSingleConverter(value.windSpeed);
			writer.WriteSingleConverter(value.windTurbulence);
			writer.WriteSingleConverter(value.windHeading);
			writer.WriteSingleConverter(value.windRandomHeading);
			writer.WriteSingleConverter(value.moonPhase);
		}

		public static NetworkMission.SyncMissionFooter _Read_NetworkMission_002FSyncMissionFooter(NetworkReader reader)
		{
			return new NetworkMission.SyncMissionFooter
			{
				Name = reader.ReadString()
			};
		}

		public static void _Write_NetworkMission_002FSyncMissionFooter(NetworkWriter writer, NetworkMission.SyncMissionFooter value)
		{
			writer.WriteString(value.Name);
		}

		public static NetworkMission.SyncMission _Read_NetworkMission_002FSyncMission(NetworkReader reader)
		{
			return new NetworkMission.SyncMission
			{
				Name = reader.ReadString(),
				Mission = _Read_NuclearOption_002ESavedMission_002EMission(reader)
			};
		}

		public static Mission _Read_NuclearOption_002ESavedMission_002EMission(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			Mission mission = new Mission();
			mission.JsonVersion = reader.ReadPackedInt32();
			mission.MapKey = _Read_NuclearOption_002ESceneLoading_002EMapKey(reader);
			mission.missionSettings = _Read_NuclearOption_002ESavedMission_002EMissionSettings(reader);
			mission.environment = _Read_NuclearOption_002ESavedMission_002EMissionEnvironment(reader);
			mission.aircraft = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAircraft_003E(reader);
			mission.vehicles = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedVehicle_003E(reader);
			mission.ships = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedShip_003E(reader);
			mission.buildings = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedBuilding_003E(reader);
			mission.scenery = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedScenery_003E(reader);
			mission.containers = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedContainer_003E(reader);
			mission.missiles = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedMissile_003E(reader);
			mission.pilots = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedPilot_003E(reader);
			mission.factions = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionFaction_003E(reader);
			mission.airbases = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAirbase_003E(reader);
			mission.objectives = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedObjective_003E(reader);
			mission.outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedOutcome_003E(reader);
			return mission;
		}

		public static MapKey _Read_NuclearOption_002ESceneLoading_002EMapKey(NetworkReader reader)
		{
			return new MapKey
			{
				Type = _Read_NuclearOption_002ESceneLoading_002EMapKey_002FKeyType(reader),
				Path = reader.ReadString()
			};
		}

		public static MapKey.KeyType _Read_NuclearOption_002ESceneLoading_002EMapKey_002FKeyType(NetworkReader reader)
		{
			return (MapKey.KeyType)reader.ReadByteExtension();
		}

		public static SavedAircraft _Read_NuclearOption_002ESavedMission_002ESavedAircraft(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedAircraft savedAircraft = new SavedAircraft();
			savedAircraft.playerControlled = reader.ReadBooleanExtension();
			savedAircraft.playerControlledPriority = reader.ReadPackedInt32();
			savedAircraft.savedLoadout = _Read_NuclearOption_002ESavedMission_002ESavedLoadout(reader);
			savedAircraft.livery = reader.ReadPackedInt32();
			savedAircraft.liveryType = _Read_LiveryKey_002FKeyType(reader);
			savedAircraft.liveryName = reader.ReadString();
			savedAircraft.fuel = reader.ReadSingleConverter();
			savedAircraft.skill = reader.ReadSingleConverter();
			savedAircraft.bravery = reader.ReadSingleConverter();
			savedAircraft.startingSpeed = reader.ReadSingleConverter();
			savedAircraft.type = reader.ReadString();
			savedAircraft.faction = reader.ReadString();
			savedAircraft.private_uniqueName = reader.ReadString();
			savedAircraft.globalPosition = reader.ReadGlobalPosition();
			savedAircraft.rotation = reader.ReadQuaternion();
			savedAircraft.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedAircraft.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedAircraft.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedAircraft;
		}

		public static SavedLoadout _Read_NuclearOption_002ESavedMission_002ESavedLoadout(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedLoadout savedLoadout = new SavedLoadout();
			savedLoadout.Selected = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount_003E(reader);
			return savedLoadout;
		}

		public static SavedLoadout.SelectedMount _Read_NuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount(NetworkReader reader)
		{
			return new SavedLoadout.SelectedMount
			{
				Key = reader.ReadString()
			};
		}

		public static List<SavedLoadout.SelectedMount> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedLoadout.SelectedMount>();
		}

		public static LiveryKey.KeyType _Read_LiveryKey_002FKeyType(NetworkReader reader)
		{
			return (LiveryKey.KeyType)reader.ReadByteExtension();
		}

		public static Override<float> _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(NetworkReader reader)
		{
			return reader.ReadOverride<float>();
		}

		public static SavedInventory _Read_NuclearOption_002ESavedMission_002ESavedInventory(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedInventory savedInventory = new SavedInventory();
			savedInventory.StoredList = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E(reader);
			return savedInventory;
		}

		public static UnitCount _Read_NuclearOption_002ESavedMission_002EUnitCount(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			UnitCount unitCount = new UnitCount();
			unitCount.UnitType = reader.ReadString();
			unitCount.Count = reader.ReadPackedInt32();
			return unitCount;
		}

		public static List<UnitCount> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E(NetworkReader reader)
		{
			return reader.ReadList<UnitCount>();
		}

		public static List<SavedAircraft> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAircraft_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedAircraft>();
		}

		public static SavedVehicle _Read_NuclearOption_002ESavedMission_002ESavedVehicle(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedVehicle savedVehicle = new SavedVehicle();
			savedVehicle.holdPosition = reader.ReadBooleanExtension();
			savedVehicle.skill = reader.ReadSingleConverter();
			savedVehicle.waypoints = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E(reader);
			savedVehicle.type = reader.ReadString();
			savedVehicle.faction = reader.ReadString();
			savedVehicle.private_uniqueName = reader.ReadString();
			savedVehicle.globalPosition = reader.ReadGlobalPosition();
			savedVehicle.rotation = reader.ReadQuaternion();
			savedVehicle.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedVehicle.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedVehicle.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedVehicle;
		}

		public static VehicleWaypoint _Read_NuclearOption_002ESavedMission_002EVehicleWaypoint(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			VehicleWaypoint vehicleWaypoint = new VehicleWaypoint();
			vehicleWaypoint.position = reader.ReadGlobalPosition();
			vehicleWaypoint.objective = reader.ReadString();
			return vehicleWaypoint;
		}

		public static List<VehicleWaypoint> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E(NetworkReader reader)
		{
			return reader.ReadList<VehicleWaypoint>();
		}

		public static List<SavedVehicle> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedVehicle_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedVehicle>();
		}

		public static SavedShip _Read_NuclearOption_002ESavedMission_002ESavedShip(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedShip savedShip = new SavedShip();
			savedShip.holdPosition = reader.ReadBooleanExtension();
			savedShip.skill = reader.ReadSingleConverter();
			savedShip.waypoints = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E(reader);
			savedShip.type = reader.ReadString();
			savedShip.faction = reader.ReadString();
			savedShip.private_uniqueName = reader.ReadString();
			savedShip.globalPosition = reader.ReadGlobalPosition();
			savedShip.rotation = reader.ReadQuaternion();
			savedShip.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedShip.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedShip.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedShip;
		}

		public static List<SavedShip> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedShip_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedShip>();
		}

		public static SavedBuilding _Read_NuclearOption_002ESavedMission_002ESavedBuilding(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedBuilding savedBuilding = new SavedBuilding();
			savedBuilding.capturable = reader.ReadBooleanExtension();
			savedBuilding.Airbase = reader.ReadString();
			savedBuilding.factoryOptions = _Read_NuclearOption_002ESavedMission_002ESavedBuilding_002FFactoryOptions(reader);
			savedBuilding.type = reader.ReadString();
			savedBuilding.faction = reader.ReadString();
			savedBuilding.private_uniqueName = reader.ReadString();
			savedBuilding.globalPosition = reader.ReadGlobalPosition();
			savedBuilding.rotation = reader.ReadQuaternion();
			savedBuilding.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedBuilding.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedBuilding.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedBuilding;
		}

		public static SavedBuilding.FactoryOptions _Read_NuclearOption_002ESavedMission_002ESavedBuilding_002FFactoryOptions(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedBuilding.FactoryOptions factoryOptions = new SavedBuilding.FactoryOptions();
			factoryOptions.productionType = reader.ReadString();
			factoryOptions.productionTime = reader.ReadSingleConverter();
			return factoryOptions;
		}

		public static List<SavedBuilding> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedBuilding_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedBuilding>();
		}

		public static SavedScenery _Read_NuclearOption_002ESavedMission_002ESavedScenery(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedScenery savedScenery = new SavedScenery();
			savedScenery.indestructible = reader.ReadBooleanExtension();
			savedScenery.type = reader.ReadString();
			savedScenery.faction = reader.ReadString();
			savedScenery.private_uniqueName = reader.ReadString();
			savedScenery.globalPosition = reader.ReadGlobalPosition();
			savedScenery.rotation = reader.ReadQuaternion();
			savedScenery.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedScenery.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedScenery.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedScenery;
		}

		public static List<SavedScenery> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedScenery_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedScenery>();
		}

		public static SavedContainer _Read_NuclearOption_002ESavedMission_002ESavedContainer(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedContainer savedContainer = new SavedContainer();
			savedContainer.type = reader.ReadString();
			savedContainer.faction = reader.ReadString();
			savedContainer.private_uniqueName = reader.ReadString();
			savedContainer.globalPosition = reader.ReadGlobalPosition();
			savedContainer.rotation = reader.ReadQuaternion();
			savedContainer.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedContainer.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedContainer.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedContainer;
		}

		public static List<SavedContainer> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedContainer_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedContainer>();
		}

		public static SavedMissile _Read_NuclearOption_002ESavedMission_002ESavedMissile(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedMissile savedMissile = new SavedMissile();
			savedMissile.startingSpeed = reader.ReadSingleConverter();
			savedMissile.targetUnitName = reader.ReadString();
			savedMissile.guidingUnit = reader.ReadString();
			savedMissile.type = reader.ReadString();
			savedMissile.faction = reader.ReadString();
			savedMissile.private_uniqueName = reader.ReadString();
			savedMissile.globalPosition = reader.ReadGlobalPosition();
			savedMissile.rotation = reader.ReadQuaternion();
			savedMissile.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedMissile.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedMissile.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedMissile;
		}

		public static List<SavedMissile> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedMissile_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedMissile>();
		}

		public static SavedPilot _Read_NuclearOption_002ESavedMission_002ESavedPilot(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedPilot savedPilot = new SavedPilot();
			savedPilot.type = reader.ReadString();
			savedPilot.faction = reader.ReadString();
			savedPilot.private_uniqueName = reader.ReadString();
			savedPilot.globalPosition = reader.ReadGlobalPosition();
			savedPilot.rotation = reader.ReadQuaternion();
			savedPilot.CaptureStrength = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedPilot.CaptureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			savedPilot.inventory = _Read_NuclearOption_002ESavedMission_002ESavedInventory(reader);
			return savedPilot;
		}

		public static List<SavedPilot> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedPilot_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedPilot>();
		}

		public static MissionFaction _Read_NuclearOption_002ESavedMission_002EMissionFaction(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			MissionFaction missionFaction = new MissionFaction();
			missionFaction.factionName = reader.ReadString();
			missionFaction.preventJoin = reader.ReadBooleanExtension();
			missionFaction.preventDonation = reader.ReadBooleanExtension();
			missionFaction.supplies = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E(reader);
			missionFaction.startingBalance = reader.ReadSingleConverter();
			missionFaction.playerJoinAllowance = reader.ReadSingleConverter();
			missionFaction.playerTaxRate = reader.ReadSingleConverter();
			missionFaction.regularIncome = reader.ReadSingleConverter();
			missionFaction.excessFundsDistributePercent = reader.ReadSingleConverter();
			missionFaction.killReward = reader.ReadSingleConverter();
			missionFaction.airSkillMultiplier = reader.ReadSingleConverter();
			missionFaction.surfaceSkillMultiplier = reader.ReadSingleConverter();
			missionFaction.startingWarheads = reader.ReadPackedInt32();
			missionFaction.reserveWarheads = reader.ReadPackedInt32();
			missionFaction.reserveAirframes = reader.ReadPackedInt32();
			missionFaction.extraReservesPerPlayer = reader.ReadPackedInt32();
			missionFaction.AIAircraftLimit = reader.ReadPackedInt32();
			missionFaction.reduceAIPerFriendlyPlayer = reader.ReadSingleConverter();
			missionFaction.addAIPerEnemyPlayer = reader.ReadSingleConverter();
			missionFaction.restrictions = _Read_NuclearOption_002ESavedMission_002ERestrictions(reader);
			missionFaction.cameraStartPosition = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E(reader);
			return missionFaction;
		}

		public static Restrictions _Read_NuclearOption_002ESavedMission_002ERestrictions(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			Restrictions restrictions = new Restrictions();
			restrictions.aircraft = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			restrictions.weapons = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return restrictions;
		}

		public static List<string> _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(NetworkReader reader)
		{
			return reader.ReadList<string>();
		}

		public static List<MissionFaction> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionFaction_003E(NetworkReader reader)
		{
			return reader.ReadList<MissionFaction>();
		}

		public static SavedAirbase _Read_NuclearOption_002ESavedMission_002ESavedAirbase(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedAirbase savedAirbase = new SavedAirbase();
			savedAirbase.IsOverride = reader.ReadBooleanExtension();
			savedAirbase.faction = reader.ReadString();
			savedAirbase.UniqueName = reader.ReadString();
			savedAirbase.DisplayName = reader.ReadString();
			savedAirbase.Disabled = reader.ReadBooleanExtension();
			savedAirbase.Capturable = reader.ReadBooleanExtension();
			savedAirbase.CaptureDefense = reader.ReadSingleConverter();
			savedAirbase.CaptureRange = reader.ReadSingleConverter();
			savedAirbase.Center = reader.ReadGlobalPosition();
			savedAirbase.SelectionPosition = reader.ReadGlobalPosition();
			savedAirbase.Tower = reader.ReadString();
			savedAirbase.VerticalLandingPoints = _Read_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(reader);
			savedAirbase.ServicePoints = _Read_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(reader);
			savedAirbase.roads = _Read_RoadPathfinding_002ERoadNetwork(reader);
			savedAirbase.runways = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedRunway_003E(reader);
			return savedAirbase;
		}

		public static SavedRunway _Read_NuclearOption_002ESavedMission_002ESavedRunway(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedRunway savedRunway = new SavedRunway();
			savedRunway.Name = reader.ReadString();
			savedRunway.Reversable = reader.ReadBooleanExtension();
			savedRunway.Takeoff = reader.ReadBooleanExtension();
			savedRunway.Landing = reader.ReadBooleanExtension();
			savedRunway.Arrestor = reader.ReadBooleanExtension();
			savedRunway.SkiJump = reader.ReadBooleanExtension();
			savedRunway.Width = reader.ReadSingleConverter();
			savedRunway.Start = reader.ReadGlobalPosition();
			savedRunway.End = reader.ReadGlobalPosition();
			savedRunway.exitPoints = _Read_GlobalPosition_005B_005D(reader);
			return savedRunway;
		}

		public static GlobalPosition[] _Read_GlobalPosition_005B_005D(NetworkReader reader)
		{
			return reader.ReadArray<GlobalPosition>();
		}

		public static List<SavedRunway> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedRunway_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedRunway>();
		}

		public static List<SavedAirbase> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAirbase_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedAirbase>();
		}

		public static List<SavedObjective> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedObjective_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedObjective>();
		}

		public static List<SavedOutcome> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedOutcome_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedOutcome>();
		}

		public static void _Write_NetworkMission_002FSyncMission(NetworkWriter writer, NetworkMission.SyncMission value)
		{
			writer.WriteString(value.Name);
			_Write_NuclearOption_002ESavedMission_002EMission(writer, value.Mission);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EMission(NetworkWriter writer, Mission value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WritePackedInt32(value.JsonVersion);
			_Write_NuclearOption_002ESceneLoading_002EMapKey(writer, value.MapKey);
			_Write_NuclearOption_002ESavedMission_002EMissionSettings(writer, value.missionSettings);
			_Write_NuclearOption_002ESavedMission_002EMissionEnvironment(writer, value.environment);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAircraft_003E(writer, value.aircraft);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedVehicle_003E(writer, value.vehicles);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedShip_003E(writer, value.ships);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedBuilding_003E(writer, value.buildings);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedScenery_003E(writer, value.scenery);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedContainer_003E(writer, value.containers);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedMissile_003E(writer, value.missiles);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedPilot_003E(writer, value.pilots);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionFaction_003E(writer, value.factions);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAirbase_003E(writer, value.airbases);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedObjective_003E(writer, value.objectives);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedOutcome_003E(writer, value.outcomes);
		}

		public static void _Write_NuclearOption_002ESceneLoading_002EMapKey(NetworkWriter writer, MapKey value)
		{
			_Write_NuclearOption_002ESceneLoading_002EMapKey_002FKeyType(writer, value.Type);
			writer.WriteString(value.Path);
		}

		public static void _Write_NuclearOption_002ESceneLoading_002EMapKey_002FKeyType(NetworkWriter writer, MapKey.KeyType value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedAircraft(NetworkWriter writer, SavedAircraft value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.playerControlled);
			writer.WritePackedInt32(value.playerControlledPriority);
			_Write_NuclearOption_002ESavedMission_002ESavedLoadout(writer, value.savedLoadout);
			writer.WritePackedInt32(value.livery);
			_Write_LiveryKey_002FKeyType(writer, value.liveryType);
			writer.WriteString(value.liveryName);
			writer.WriteSingleConverter(value.fuel);
			writer.WriteSingleConverter(value.skill);
			writer.WriteSingleConverter(value.bravery);
			writer.WriteSingleConverter(value.startingSpeed);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedLoadout(NetworkWriter writer, SavedLoadout value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount_003E(writer, value.Selected);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount(NetworkWriter writer, SavedLoadout.SelectedMount value)
		{
			writer.WriteString(value.Key);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount_003E(NetworkWriter writer, List<SavedLoadout.SelectedMount> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_LiveryKey_002FKeyType(NetworkWriter writer, LiveryKey.KeyType value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(NetworkWriter writer, Override<float> value)
		{
			writer.WriteOverride(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedInventory(NetworkWriter writer, SavedInventory value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E(writer, value.StoredList);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EUnitCount(NetworkWriter writer, UnitCount value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.UnitType);
			writer.WritePackedInt32(value.Count);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E(NetworkWriter writer, List<UnitCount> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAircraft_003E(NetworkWriter writer, List<SavedAircraft> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedVehicle(NetworkWriter writer, SavedVehicle value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.holdPosition);
			writer.WriteSingleConverter(value.skill);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E(writer, value.waypoints);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EVehicleWaypoint(NetworkWriter writer, VehicleWaypoint value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteGlobalPosition(value.position);
			writer.WriteString(value.objective);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E(NetworkWriter writer, List<VehicleWaypoint> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedVehicle_003E(NetworkWriter writer, List<SavedVehicle> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedShip(NetworkWriter writer, SavedShip value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.holdPosition);
			writer.WriteSingleConverter(value.skill);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E(writer, value.waypoints);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedShip_003E(NetworkWriter writer, List<SavedShip> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedBuilding(NetworkWriter writer, SavedBuilding value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.capturable);
			writer.WriteString(value.Airbase);
			_Write_NuclearOption_002ESavedMission_002ESavedBuilding_002FFactoryOptions(writer, value.factoryOptions);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedBuilding_002FFactoryOptions(NetworkWriter writer, SavedBuilding.FactoryOptions value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.productionType);
			writer.WriteSingleConverter(value.productionTime);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedBuilding_003E(NetworkWriter writer, List<SavedBuilding> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedScenery(NetworkWriter writer, SavedScenery value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.indestructible);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedScenery_003E(NetworkWriter writer, List<SavedScenery> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedContainer(NetworkWriter writer, SavedContainer value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedContainer_003E(NetworkWriter writer, List<SavedContainer> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedMissile(NetworkWriter writer, SavedMissile value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteSingleConverter(value.startingSpeed);
			writer.WriteString(value.targetUnitName);
			writer.WriteString(value.guidingUnit);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedMissile_003E(NetworkWriter writer, List<SavedMissile> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedPilot(NetworkWriter writer, SavedPilot value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.type);
			writer.WriteString(value.faction);
			writer.WriteString(value.private_uniqueName);
			writer.WriteGlobalPosition(value.globalPosition);
			writer.WriteQuaternion(value.rotation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureStrength);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.CaptureDefense);
			_Write_NuclearOption_002ESavedMission_002ESavedInventory(writer, value.inventory);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedPilot_003E(NetworkWriter writer, List<SavedPilot> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EMissionFaction(NetworkWriter writer, MissionFaction value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.factionName);
			writer.WriteBooleanExtension(value.preventJoin);
			writer.WriteBooleanExtension(value.preventDonation);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E(writer, value.supplies);
			writer.WriteSingleConverter(value.startingBalance);
			writer.WriteSingleConverter(value.playerJoinAllowance);
			writer.WriteSingleConverter(value.playerTaxRate);
			writer.WriteSingleConverter(value.regularIncome);
			writer.WriteSingleConverter(value.excessFundsDistributePercent);
			writer.WriteSingleConverter(value.killReward);
			writer.WriteSingleConverter(value.airSkillMultiplier);
			writer.WriteSingleConverter(value.surfaceSkillMultiplier);
			writer.WritePackedInt32(value.startingWarheads);
			writer.WritePackedInt32(value.reserveWarheads);
			writer.WritePackedInt32(value.reserveAirframes);
			writer.WritePackedInt32(value.extraReservesPerPlayer);
			writer.WritePackedInt32(value.AIAircraftLimit);
			writer.WriteSingleConverter(value.reduceAIPerFriendlyPlayer);
			writer.WriteSingleConverter(value.addAIPerEnemyPlayer);
			_Write_NuclearOption_002ESavedMission_002ERestrictions(writer, value.restrictions);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E(writer, value.cameraStartPosition);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ERestrictions(NetworkWriter writer, Restrictions value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.aircraft);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.weapons);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(NetworkWriter writer, List<string> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionFaction_003E(NetworkWriter writer, List<MissionFaction> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedAirbase(NetworkWriter writer, SavedAirbase value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.IsOverride);
			writer.WriteString(value.faction);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Disabled);
			writer.WriteBooleanExtension(value.Capturable);
			writer.WriteSingleConverter(value.CaptureDefense);
			writer.WriteSingleConverter(value.CaptureRange);
			writer.WriteGlobalPosition(value.Center);
			writer.WriteGlobalPosition(value.SelectionPosition);
			writer.WriteString(value.Tower);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(writer, value.VerticalLandingPoints);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E(writer, value.ServicePoints);
			_Write_RoadPathfinding_002ERoadNetwork(writer, value.roads);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedRunway_003E(writer, value.runways);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ESavedRunway(NetworkWriter writer, SavedRunway value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.Name);
			writer.WriteBooleanExtension(value.Reversable);
			writer.WriteBooleanExtension(value.Takeoff);
			writer.WriteBooleanExtension(value.Landing);
			writer.WriteBooleanExtension(value.Arrestor);
			writer.WriteBooleanExtension(value.SkiJump);
			writer.WriteSingleConverter(value.Width);
			writer.WriteGlobalPosition(value.Start);
			writer.WriteGlobalPosition(value.End);
			_Write_GlobalPosition_005B_005D(writer, value.exitPoints);
		}

		public static void _Write_GlobalPosition_005B_005D(NetworkWriter writer, GlobalPosition[] value)
		{
			writer.WriteArray(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedRunway_003E(NetworkWriter writer, List<SavedRunway> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAirbase_003E(NetworkWriter writer, List<SavedAirbase> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedObjective_003E(NetworkWriter writer, List<SavedObjective> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedOutcome_003E(NetworkWriter writer, List<SavedOutcome> value)
		{
			writer.WriteList(value);
		}

		public static NetworkMission.SyncMissionStart _Read_NetworkMission_002FSyncMissionStart(NetworkReader reader)
		{
			return default(NetworkMission.SyncMissionStart);
		}

		public static void _Write_NetworkMission_002FSyncMissionStart(NetworkWriter writer, NetworkMission.SyncMissionStart value)
		{
		}

		public static LoadMapMessage _Read_NuclearOption_002ESceneLoading_002ELoadMapMessage(NetworkReader reader)
		{
			return new LoadMapMessage
			{
				Key = _Read_NuclearOption_002ESceneLoading_002EMapKey(reader)
			};
		}

		public static void _Write_NuclearOption_002ESceneLoading_002ELoadMapMessage(NetworkWriter writer, LoadMapMessage value)
		{
			_Write_NuclearOption_002ESceneLoading_002EMapKey(writer, value.Key);
		}

		public static string _Read_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot(NetworkReader reader)
		{
			return "";
		}

		/*public static NetworkTransformBase.NetworkSnapshot _Read_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot(NetworkReader reader)
		{
			return new NetworkTransformBase.NetworkSnapshot();
		}*/

		public static CompressedInputs _Read_CompressedInputs(NetworkReader reader)
		{
			/*
			return new CompressedInputs
			{
				pitch = _Read_CompressedFloat(reader),
				roll = _Read_CompressedFloat(reader),
				yaw = _Read_CompressedFloat(reader),
				throttle = _Read_CompressedFloat(reader),
				brake = _Read_CompressedFloat(reader),
				customAxis1 = _Read_CompressedFloat(reader)
			};*/
			return new CompressedInputs();
		}

		public static CompressedFloat _Read_CompressedFloat(NetworkReader reader)
		{
			return new CompressedFloat
			{
				Value = reader.ReadUInt16Extension()
			};
		}

		public static CompressedInputs? _Read_System_002ENullable_00601_003CCompressedInputs_003E(NetworkReader reader)
		{
			return reader.ReadNullable<CompressedInputs>();
		}

		public static double? _Read_System_002ENullable_00601_003CSystem_002EDouble_003E(NetworkReader reader)
		{
			return reader.ReadNullable<double>();
		}

		public static float? _Read_System_002ENullable_00601_003CSystem_002ESingle_003E(NetworkReader reader)
		{
			return reader.ReadNullable<float>();
		}

		public static Vector3Compressed _Read_Vector3Compressed(NetworkReader reader)
		{
			return new Vector3Compressed
			{
				x = _Read_CompressedFloat(reader),
				y = _Read_CompressedFloat(reader),
				z = _Read_CompressedFloat(reader)
			};
		}

		/*public static void _Write_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot(NetworkWriter writer, NetworkTransformBase.NetworkSnapshot value)
		{
			_Write_System_002ENullable_00601_003CCompressedInputs_003E(writer, value.ClientInputs);
			_Write_System_002ENullable_00601_003CSystem_002EDouble_003E(writer, value.timestamp);
			_Write_System_002ENullable_00601_003CSystem_002ESingle_003E(writer, value.extraExtrapolation);
			writer.WriteGlobalPosition(value.globalPos);
			_Write_Vector3Compressed(writer, value.velocity);
			NetworkTransformBase.NetworkSnapshot.rotation__Packer.Pack(writer, value.rotation);
		}*/

		public static void _Write_CompressedInputs(NetworkWriter writer, CompressedInputs value)
		{
			_Write_CompressedFloat(writer, value.pitch);
			_Write_CompressedFloat(writer, value.roll);
			_Write_CompressedFloat(writer, value.yaw);
			_Write_CompressedFloat(writer, value.throttle);
			_Write_CompressedFloat(writer, value.brake);
			_Write_CompressedFloat(writer, value.customAxis1);
		}

		public static void _Write_CompressedFloat(NetworkWriter writer, CompressedFloat value)
		{
			writer.WriteUInt16Extension(value.Value);
		}

		public static void _Write_System_002ENullable_00601_003CCompressedInputs_003E(NetworkWriter writer, CompressedInputs? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_System_002ENullable_00601_003CSystem_002EDouble_003E(NetworkWriter writer, double? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_System_002ENullable_00601_003CSystem_002ESingle_003E(NetworkWriter writer, float? value)
		{
			writer.WriteNullable(value);
		}

		public static void _Write_Vector3Compressed(NetworkWriter writer, Vector3Compressed value)
		{
			_Write_CompressedFloat(writer, value.x);
			_Write_CompressedFloat(writer, value.y);
			_Write_CompressedFloat(writer, value.z);
		}

		/*public static SendTransformBatcher.TransformMessage _Read_NuclearOption_002ENetworkTransforms_002ESendTransformBatcher_002FTransformMessage(NetworkReader reader)
		{
			return new SendTransformBatcher.TransformMessage
			{
				timestamp = reader.ReadDoubleConverter(),
				data = reader.ReadBytesAndSizeSegment()
			};
		}*/

		/*public static void _Write_NuclearOption_002ENetworkTransforms_002ESendTransformBatcher_002FTransformMessage(NetworkWriter writer, SendTransformBatcher.TransformMessage value)
		{
			writer.WriteDoubleConverter(value.timestamp);
			writer.WriteBytesAndSizeSegment(value.data);
		}*/

		public static HostEndedMessage _Read_NuclearOption_002ENetworking_002EHostEndedMessage(NetworkReader reader)
		{
			return default(HostEndedMessage);
		}

		public static void _Write_NuclearOption_002ENetworking_002EHostEndedMessage(NetworkWriter writer, HostEndedMessage value)
		{
		}

		public static LoadWaitingSceneMessage _Read_NuclearOption_002ENetworking_002ELoadWaitingSceneMessage(NetworkReader reader)
		{
			return default(LoadWaitingSceneMessage);
		}

		public static void _Write_NuclearOption_002ENetworking_002ELoadWaitingSceneMessage(NetworkWriter writer, LoadWaitingSceneMessage value)
		{
		}

		public static ServerLoadingProgressMessage _Read_NuclearOption_002ENetworking_002EServerLoadingProgressMessage(NetworkReader reader)
		{
			return new ServerLoadingProgressMessage
			{
				LoadingMessage = reader.ReadString()
			};
		}

		public static void _Write_NuclearOption_002ENetworking_002EServerLoadingProgressMessage(NetworkWriter writer, ServerLoadingProgressMessage value)
		{
			writer.WriteString(value.LoadingMessage);
		}

		public static NetworkAuthenticatorNuclearOption.AuthMessage _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthMessage(NetworkReader reader)
		{
			return new NetworkAuthenticatorNuclearOption.AuthMessage
			{
				BuildHash = reader.ReadPackedUInt32(),
				JoinAs = _Read_NuclearOption_002ENetworking_002EPlayerType(reader),
				SteamAuthToken = reader.ReadBytesAndSizeSegment(),
				SteamName = reader.ReadString()
			};
		}

		public static PlayerType _Read_NuclearOption_002ENetworking_002EPlayerType(NetworkReader reader)
		{
			return (PlayerType)reader.ReadPackedInt32();
		}

		public static void _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthMessage(NetworkWriter writer, NetworkAuthenticatorNuclearOption.AuthMessage value)
		{
			writer.WritePackedUInt32(value.BuildHash);
			_Write_NuclearOption_002ENetworking_002EPlayerType(writer, value.JoinAs);
			writer.WriteBytesAndSizeSegment(value.SteamAuthToken);
			writer.WriteString(value.SteamName);
		}

		public static void _Write_NuclearOption_002ENetworking_002EPlayerType(NetworkWriter writer, PlayerType value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static NetworkAuthenticatorNuclearOption.PasswordChallenge _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordChallenge(NetworkReader reader)
		{
			return new NetworkAuthenticatorNuclearOption.PasswordChallenge
			{
				Nonce = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordChallenge(NetworkWriter writer, NetworkAuthenticatorNuclearOption.PasswordChallenge value)
		{
			writer.WriteBytesAndSizeSegment(value.Nonce);
		}

		public static NetworkAuthenticatorNuclearOption.PasswordResponse _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordResponse(NetworkReader reader)
		{
			return new NetworkAuthenticatorNuclearOption.PasswordResponse
			{
				Response = reader.ReadBytesAndSizeSegment()
			};
		}

		public static void _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordResponse(NetworkWriter writer, NetworkAuthenticatorNuclearOption.PasswordResponse value)
		{
			writer.WriteBytesAndSizeSegment(value.Response);
		}

		public static NetworkAuthenticatorNuclearOption.AuthFailReason _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthFailReason(NetworkReader reader)
		{
			return new NetworkAuthenticatorNuclearOption.AuthFailReason
			{
				Reason = reader.ReadString()
			};
		}

		public static void _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthFailReason(NetworkWriter writer, NetworkAuthenticatorNuclearOption.AuthFailReason value)
		{
			writer.WriteString(value.Reason);
		}

		public static NetworkAuthenticatorNuclearOption.BuildHashMismatch _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FBuildHashMismatch(NetworkReader reader)
		{
			return new NetworkAuthenticatorNuclearOption.BuildHashMismatch
			{
				BuildHash = reader.ReadPackedUInt32()
			};
		}

		public static void _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FBuildHashMismatch(NetworkWriter writer, NetworkAuthenticatorNuclearOption.BuildHashMismatch value)
		{
			writer.WritePackedUInt32(value.BuildHash);
		}

		public static CompleteObjectiveSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			CompleteObjectiveSavedOutcome completeObjectiveSavedOutcome = new CompleteObjectiveSavedOutcome();
			completeObjectiveSavedOutcome.options = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveOutcome_002FOptions(reader);
			completeObjectiveSavedOutcome.objectivesToStart = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			completeObjectiveSavedOutcome.UniqueName = reader.ReadString();
			return completeObjectiveSavedOutcome;
		}

		public static CompleteObjectiveOutcome.Options _Read_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveOutcome_002FOptions(NetworkReader reader)
		{
			return (CompleteObjectiveOutcome.Options)reader.ReadPackedInt32();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveSavedOutcome(NetworkWriter writer, CompleteObjectiveSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveOutcome_002FOptions(writer, value.options);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.objectivesToStart);
			writer.WriteString(value.UniqueName);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveOutcome_002FOptions(NetworkWriter writer, CompleteObjectiveOutcome.Options value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static EndGameSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EEndGameSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			EndGameSavedOutcome endGameSavedOutcome = new EndGameSavedOutcome();
			endGameSavedOutcome.endType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EEndType(reader);
			endGameSavedOutcome.endDelay = reader.ReadSingleConverter();
			endGameSavedOutcome.UniqueName = reader.ReadString();
			return endGameSavedOutcome;
		}

		public static EndType _Read_NuclearOption_002ESavedMission_002EOutcomes_002EEndType(NetworkReader reader)
		{
			return (EndType)reader.ReadPackedInt32();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EEndGameSavedOutcome(NetworkWriter writer, EndGameSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002EEndType(writer, value.endType);
			writer.WriteSingleConverter(value.endDelay);
			writer.WriteString(value.UniqueName);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EEndType(NetworkWriter writer, EndType value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static GiveScoreSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EGiveScoreSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			GiveScoreSavedOutcome giveScoreSavedOutcome = new GiveScoreSavedOutcome();
			giveScoreSavedOutcome.bothFactions = reader.ReadBooleanExtension();
			giveScoreSavedOutcome.playerFundsType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(reader);
			giveScoreSavedOutcome.playerFunds = reader.ReadSingleConverter();
			giveScoreSavedOutcome.factionFundsType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(reader);
			giveScoreSavedOutcome.factionFunds = reader.ReadSingleConverter();
			giveScoreSavedOutcome.playerScoreType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(reader);
			giveScoreSavedOutcome.playerScore = reader.ReadSingleConverter();
			giveScoreSavedOutcome.rankType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(reader);
			giveScoreSavedOutcome.rank = reader.ReadPackedInt32();
			giveScoreSavedOutcome.factionScoreType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(reader);
			giveScoreSavedOutcome.factionScore = reader.ReadSingleConverter();
			giveScoreSavedOutcome.UniqueName = reader.ReadString();
			return giveScoreSavedOutcome;
		}

		public static ChangeType _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(NetworkReader reader)
		{
			return (ChangeType)reader.ReadPackedInt32();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EGiveScoreSavedOutcome(NetworkWriter writer, GiveScoreSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.bothFactions);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(writer, value.playerFundsType);
			writer.WriteSingleConverter(value.playerFunds);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(writer, value.factionFundsType);
			writer.WriteSingleConverter(value.factionFunds);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(writer, value.playerScoreType);
			writer.WriteSingleConverter(value.playerScore);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(writer, value.rankType);
			writer.WritePackedInt32(value.rank);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(writer, value.factionScoreType);
			writer.WriteSingleConverter(value.factionScore);
			writer.WriteString(value.UniqueName);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType(NetworkWriter writer, ChangeType value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static ModifyAirbaseSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EModifyAirbaseSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			ModifyAirbaseSavedOutcome modifyAirbaseSavedOutcome = new ModifyAirbaseSavedOutcome();
			modifyAirbaseSavedOutcome.airbase = reader.ReadString();
			modifyAirbaseSavedOutcome.faction = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EString_003E(reader);
			modifyAirbaseSavedOutcome.disabled = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(reader);
			modifyAirbaseSavedOutcome.capturable = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(reader);
			modifyAirbaseSavedOutcome.captureDefense = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyAirbaseSavedOutcome.UniqueName = reader.ReadString();
			return modifyAirbaseSavedOutcome;
		}

		public static Override<string> _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EString_003E(NetworkReader reader)
		{
			return reader.ReadOverride<string>();
		}

		public static Override<bool> _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(NetworkReader reader)
		{
			return reader.ReadOverride<bool>();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EModifyAirbaseSavedOutcome(NetworkWriter writer, ModifyAirbaseSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.airbase);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EString_003E(writer, value.faction);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(writer, value.disabled);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(writer, value.capturable);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.captureDefense);
			writer.WriteString(value.UniqueName);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EString_003E(NetworkWriter writer, Override<string> value)
		{
			writer.WriteOverride(value);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(NetworkWriter writer, Override<bool> value)
		{
			writer.WriteOverride(value);
		}

		public static ModifyEnvironmentSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EModifyEnvironmentSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			ModifyEnvironmentSavedOutcome modifyEnvironmentSavedOutcome = new ModifyEnvironmentSavedOutcome();
			modifyEnvironmentSavedOutcome.timeOfDay = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyEnvironmentSavedOutcome.weather = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyEnvironmentSavedOutcome.cloudAltitude = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyEnvironmentSavedOutcome.windSpeed = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyEnvironmentSavedOutcome.windTurbulence = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyEnvironmentSavedOutcome.windHeading = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyEnvironmentSavedOutcome.UniqueName = reader.ReadString();
			return modifyEnvironmentSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EModifyEnvironmentSavedOutcome(NetworkWriter writer, ModifyEnvironmentSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.timeOfDay);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.weather);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.cloudAltitude);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.windSpeed);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.windTurbulence);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.windHeading);
			writer.WriteString(value.UniqueName);
		}

		public static ModifyFactionSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EModifyFactionSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			ModifyFactionSavedOutcome modifyFactionSavedOutcome = new ModifyFactionSavedOutcome();
			modifyFactionSavedOutcome.bothFactions = reader.ReadBooleanExtension();
			modifyFactionSavedOutcome.excessFundsThreshold = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.playerJoinAllowance = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.playerTaxRate = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.regularIncome = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.killReward = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.preventDonation = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(reader);
			modifyFactionSavedOutcome.aiAircraftLimit = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.reduceAIPerFriendlyPlayer = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.addAIPerEnemyPlayer = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.warheadsReserve = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.reserveAirframes = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.extraReservesPerPlayer = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.excessFundsDistributePercent = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(reader);
			modifyFactionSavedOutcome.preventJoin = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(reader);
			modifyFactionSavedOutcome.UniqueName = reader.ReadString();
			return modifyFactionSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EModifyFactionSavedOutcome(NetworkWriter writer, ModifyFactionSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteBooleanExtension(value.bothFactions);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.excessFundsThreshold);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.playerJoinAllowance);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.playerTaxRate);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.regularIncome);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.killReward);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(writer, value.preventDonation);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.aiAircraftLimit);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.reduceAIPerFriendlyPlayer);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.addAIPerEnemyPlayer);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.warheadsReserve);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.reserveAirframes);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.extraReservesPerPlayer);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E(writer, value.excessFundsDistributePercent);
			_Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E(writer, value.preventJoin);
			writer.WriteString(value.UniqueName);
		}

		public static NoSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002ENoSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			NoSavedOutcome noSavedOutcome = new NoSavedOutcome();
			noSavedOutcome.UniqueName = reader.ReadString();
			return noSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ENoSavedOutcome(NetworkWriter writer, NoSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.UniqueName);
		}

		public static RemoveUnitSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERemoveUnitSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			RemoveUnitSavedOutcome removeUnitSavedOutcome = new RemoveUnitSavedOutcome();
			removeUnitSavedOutcome.UnitsToRemove = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			removeUnitSavedOutcome.UniqueName = reader.ReadString();
			return removeUnitSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERemoveUnitSavedOutcome(NetworkWriter writer, RemoveUnitSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.UnitsToRemove);
			writer.WriteString(value.UniqueName);
		}

		public static RestrictionSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			RestrictionSavedOutcome restrictionSavedOutcome = new RestrictionSavedOutcome();
			restrictionSavedOutcome.endType = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionOutcome_002FChange(reader);
			restrictionSavedOutcome.restrictionNames = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			restrictionSavedOutcome.UniqueName = reader.ReadString();
			return restrictionSavedOutcome;
		}

		public static RestrictionOutcome.Change _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionOutcome_002FChange(NetworkReader reader)
		{
			return (RestrictionOutcome.Change)reader.ReadPackedInt32();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionSavedOutcome(NetworkWriter writer, RestrictionSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionOutcome_002FChange(writer, value.endType);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.restrictionNames);
			writer.WriteString(value.UniqueName);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionOutcome_002FChange(NetworkWriter writer, RestrictionOutcome.Change value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static RevealUnitSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERevealUnitSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			RevealUnitSavedOutcome revealUnitSavedOutcome = new RevealUnitSavedOutcome();
			revealUnitSavedOutcome.UnitsToReveal = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			revealUnitSavedOutcome.UniqueName = reader.ReadString();
			return revealUnitSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERevealUnitSavedOutcome(NetworkWriter writer, RevealUnitSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.UnitsToReveal);
			writer.WriteString(value.UniqueName);
		}

		public static ShowMessageSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EShowMessageSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			ShowMessageSavedOutcome showMessageSavedOutcome = new ShowMessageSavedOutcome();
			showMessageSavedOutcome.Message = reader.ReadString();
			showMessageSavedOutcome.PlaySound = reader.ReadBooleanExtension();
			showMessageSavedOutcome.ObjectiveFactionOnly = reader.ReadBooleanExtension();
			showMessageSavedOutcome.UniqueName = reader.ReadString();
			return showMessageSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EShowMessageSavedOutcome(NetworkWriter writer, ShowMessageSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.Message);
			writer.WriteBooleanExtension(value.PlaySound);
			writer.WriteBooleanExtension(value.ObjectiveFactionOnly);
			writer.WriteString(value.UniqueName);
		}

		public static SpawnUnitSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002ESpawnUnitSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SpawnUnitSavedOutcome spawnUnitSavedOutcome = new SpawnUnitSavedOutcome();
			spawnUnitSavedOutcome.UnitsToSpawn = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			spawnUnitSavedOutcome.UniqueName = reader.ReadString();
			return spawnUnitSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002ESpawnUnitSavedOutcome(NetworkWriter writer, SpawnUnitSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.UnitsToSpawn);
			writer.WriteString(value.UniqueName);
		}

		public static StartObjectiveSavedOutcome _Read_NuclearOption_002ESavedMission_002EOutcomes_002EStartObjectiveSavedOutcome(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			StartObjectiveSavedOutcome startObjectiveSavedOutcome = new StartObjectiveSavedOutcome();
			startObjectiveSavedOutcome.objectivesToStart = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			startObjectiveSavedOutcome.UniqueName = reader.ReadString();
			return startObjectiveSavedOutcome;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EOutcomes_002EStartObjectiveSavedOutcome(NetworkWriter writer, StartObjectiveSavedOutcome value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.objectivesToStart);
			writer.WriteString(value.UniqueName);
		}

		public static CaptureAirbaseSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002ECaptureAirbaseSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			CaptureAirbaseSavedObjective captureAirbaseSavedObjective = new CaptureAirbaseSavedObjective();
			captureAirbaseSavedObjective.completeOrder = _Read_NuclearOption_002ESavedMission_002ECompleteOrder(reader);
			captureAirbaseSavedObjective.completeSomePercent = reader.ReadSingleConverter();
			captureAirbaseSavedObjective.targetAirbases = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			captureAirbaseSavedObjective.UniqueName = reader.ReadString();
			captureAirbaseSavedObjective.Faction = reader.ReadString();
			captureAirbaseSavedObjective.DisplayName = reader.ReadString();
			captureAirbaseSavedObjective.Hidden = reader.ReadBooleanExtension();
			captureAirbaseSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return captureAirbaseSavedObjective;
		}

		public static CompleteOrder _Read_NuclearOption_002ESavedMission_002ECompleteOrder(NetworkReader reader)
		{
			return (CompleteOrder)reader.ReadPackedInt32();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ECaptureAirbaseSavedObjective(NetworkWriter writer, CaptureAirbaseSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002ECompleteOrder(writer, value.completeOrder);
			writer.WriteSingleConverter(value.completeSomePercent);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.targetAirbases);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static void _Write_NuclearOption_002ESavedMission_002ECompleteOrder(NetworkWriter writer, CompleteOrder value)
		{
			writer.WritePackedInt32((int)value);
		}

		public static CompleteOtherObjectiveSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002ECompleteOtherObjectiveSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			CompleteOtherObjectiveSavedObjective completeOtherObjectiveSavedObjective = new CompleteOtherObjectiveSavedObjective();
			completeOtherObjectiveSavedObjective.completeOrder = _Read_NuclearOption_002ESavedMission_002ECompleteOrder(reader);
			completeOtherObjectiveSavedObjective.completeSomePercent = reader.ReadSingleConverter();
			completeOtherObjectiveSavedObjective.targetObjectives = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			completeOtherObjectiveSavedObjective.UniqueName = reader.ReadString();
			completeOtherObjectiveSavedObjective.Faction = reader.ReadString();
			completeOtherObjectiveSavedObjective.DisplayName = reader.ReadString();
			completeOtherObjectiveSavedObjective.Hidden = reader.ReadBooleanExtension();
			completeOtherObjectiveSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return completeOtherObjectiveSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ECompleteOtherObjectiveSavedObjective(NetworkWriter writer, CompleteOtherObjectiveSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002ECompleteOrder(writer, value.completeOrder);
			writer.WriteSingleConverter(value.completeSomePercent);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.targetObjectives);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static CrashAircraftSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002ECrashAircraftSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			CrashAircraftSavedObjective crashAircraftSavedObjective = new CrashAircraftSavedObjective();
			crashAircraftSavedObjective.livesPerPlayer = reader.ReadPackedInt32();
			crashAircraftSavedObjective.extraLives = reader.ReadPackedInt32();
			crashAircraftSavedObjective.includeDestroy = reader.ReadBooleanExtension();
			crashAircraftSavedObjective.includeEject = reader.ReadBooleanExtension();
			crashAircraftSavedObjective.UniqueName = reader.ReadString();
			crashAircraftSavedObjective.Faction = reader.ReadString();
			crashAircraftSavedObjective.DisplayName = reader.ReadString();
			crashAircraftSavedObjective.Hidden = reader.ReadBooleanExtension();
			crashAircraftSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return crashAircraftSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ECrashAircraftSavedObjective(NetworkWriter writer, CrashAircraftSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WritePackedInt32(value.livesPerPlayer);
			writer.WritePackedInt32(value.extraLives);
			writer.WriteBooleanExtension(value.includeDestroy);
			writer.WriteBooleanExtension(value.includeEject);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static DestroyUnitSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002EDestroyUnitSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			DestroyUnitSavedObjective destroyUnitSavedObjective = new DestroyUnitSavedObjective();
			destroyUnitSavedObjective.completeOrder = _Read_NuclearOption_002ESavedMission_002ECompleteOrder(reader);
			destroyUnitSavedObjective.completeSomePercent = reader.ReadSingleConverter();
			destroyUnitSavedObjective.targetUnits = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			destroyUnitSavedObjective.UniqueName = reader.ReadString();
			destroyUnitSavedObjective.Faction = reader.ReadString();
			destroyUnitSavedObjective.DisplayName = reader.ReadString();
			destroyUnitSavedObjective.Hidden = reader.ReadBooleanExtension();
			destroyUnitSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return destroyUnitSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002EDestroyUnitSavedObjective(NetworkWriter writer, DestroyUnitSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002ECompleteOrder(writer, value.completeOrder);
			writer.WriteSingleConverter(value.completeSomePercent);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.targetUnits);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static DialogueBoxSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002EDialogueBoxSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			DialogueBoxSavedObjective dialogueBoxSavedObjective = new DialogueBoxSavedObjective();
			dialogueBoxSavedObjective.title = reader.ReadString();
			dialogueBoxSavedObjective.body = reader.ReadString();
			dialogueBoxSavedObjective.button = reader.ReadString();
			dialogueBoxSavedObjective.factionOnly = reader.ReadBooleanExtension();
			dialogueBoxSavedObjective.UniqueName = reader.ReadString();
			dialogueBoxSavedObjective.Faction = reader.ReadString();
			dialogueBoxSavedObjective.DisplayName = reader.ReadString();
			dialogueBoxSavedObjective.Hidden = reader.ReadBooleanExtension();
			dialogueBoxSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return dialogueBoxSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002EDialogueBoxSavedObjective(NetworkWriter writer, DialogueBoxSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.title);
			writer.WriteString(value.body);
			writer.WriteString(value.button);
			writer.WriteBooleanExtension(value.factionOnly);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static NoSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002ENoSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			NoSavedObjective noSavedObjective = new NoSavedObjective();
			noSavedObjective.UniqueName = reader.ReadString();
			noSavedObjective.Faction = reader.ReadString();
			noSavedObjective.DisplayName = reader.ReadString();
			noSavedObjective.Hidden = reader.ReadBooleanExtension();
			noSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return noSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ENoSavedObjective(NetworkWriter writer, NoSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static ReachUnitsSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002EReachUnitsSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			ReachUnitsSavedObjective reachUnitsSavedObjective = new ReachUnitsSavedObjective();
			reachUnitsSavedObjective.completeOrder = _Read_NuclearOption_002ESavedMission_002ECompleteOrder(reader);
			reachUnitsSavedObjective.completeSomePercent = reader.ReadSingleConverter();
			reachUnitsSavedObjective.targets = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData_003E(reader);
			reachUnitsSavedObjective.UniqueName = reader.ReadString();
			reachUnitsSavedObjective.Faction = reader.ReadString();
			reachUnitsSavedObjective.DisplayName = reader.ReadString();
			reachUnitsSavedObjective.Hidden = reader.ReadBooleanExtension();
			reachUnitsSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return reachUnitsSavedObjective;
		}

		public static SavedReachUnitData _Read_NuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedReachUnitData savedReachUnitData = new SavedReachUnitData();
			savedReachUnitData.TargetUnit = reader.ReadString();
			savedReachUnitData.Range = reader.ReadSingleConverter();
			return savedReachUnitData;
		}

		public static List<SavedReachUnitData> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedReachUnitData>();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002EReachUnitsSavedObjective(NetworkWriter writer, ReachUnitsSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002ECompleteOrder(writer, value.completeOrder);
			writer.WriteSingleConverter(value.completeSomePercent);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData_003E(writer, value.targets);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData(NetworkWriter writer, SavedReachUnitData value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteString(value.TargetUnit);
			writer.WriteSingleConverter(value.Range);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData_003E(NetworkWriter writer, List<SavedReachUnitData> value)
		{
			writer.WriteList(value);
		}

		public static ReachWaypointsSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002EReachWaypointsSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			ReachWaypointsSavedObjective reachWaypointsSavedObjective = new ReachWaypointsSavedObjective();
			reachWaypointsSavedObjective.completeOrder = _Read_NuclearOption_002ESavedMission_002ECompleteOrder(reader);
			reachWaypointsSavedObjective.completeSomePercent = reader.ReadSingleConverter();
			reachWaypointsSavedObjective.completeOnEnterRange = reader.ReadBooleanExtension();
			reachWaypointsSavedObjective.waypoints = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint_003E(reader);
			reachWaypointsSavedObjective.UniqueName = reader.ReadString();
			reachWaypointsSavedObjective.Faction = reader.ReadString();
			reachWaypointsSavedObjective.DisplayName = reader.ReadString();
			reachWaypointsSavedObjective.Hidden = reader.ReadBooleanExtension();
			reachWaypointsSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return reachWaypointsSavedObjective;
		}

		public static SavedWaypoint _Read_NuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SavedWaypoint savedWaypoint = new SavedWaypoint();
			savedWaypoint.Position = reader.ReadVector3();
			savedWaypoint.Range = reader.ReadSingleConverter();
			return savedWaypoint;
		}

		public static List<SavedWaypoint> _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint_003E(NetworkReader reader)
		{
			return reader.ReadList<SavedWaypoint>();
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002EReachWaypointsSavedObjective(NetworkWriter writer, ReachWaypointsSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002ECompleteOrder(writer, value.completeOrder);
			writer.WriteSingleConverter(value.completeSomePercent);
			writer.WriteBooleanExtension(value.completeOnEnterRange);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint_003E(writer, value.waypoints);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint(NetworkWriter writer, SavedWaypoint value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteVector3(value.Position);
			writer.WriteSingleConverter(value.Range);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint_003E(NetworkWriter writer, List<SavedWaypoint> value)
		{
			writer.WriteList(value);
		}

		public static SpotUnitSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002ESpotUnitSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SpotUnitSavedObjective spotUnitSavedObjective = new SpotUnitSavedObjective();
			spotUnitSavedObjective.completeOrder = _Read_NuclearOption_002ESavedMission_002ECompleteOrder(reader);
			spotUnitSavedObjective.completeSomePercent = reader.ReadSingleConverter();
			spotUnitSavedObjective.targetUnits = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			spotUnitSavedObjective.UniqueName = reader.ReadString();
			spotUnitSavedObjective.Faction = reader.ReadString();
			spotUnitSavedObjective.DisplayName = reader.ReadString();
			spotUnitSavedObjective.Hidden = reader.ReadBooleanExtension();
			spotUnitSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return spotUnitSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ESpotUnitSavedObjective(NetworkWriter writer, SpotUnitSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_NuclearOption_002ESavedMission_002ECompleteOrder(writer, value.completeOrder);
			writer.WriteSingleConverter(value.completeSomePercent);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.targetUnits);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static SuccessfulSortieSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002ESuccessfulSortieSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			SuccessfulSortieSavedObjective successfulSortieSavedObjective = new SuccessfulSortieSavedObjective();
			successfulSortieSavedObjective.minimumScore = reader.ReadSingleConverter();
			successfulSortieSavedObjective.additive = reader.ReadBooleanExtension();
			successfulSortieSavedObjective.UniqueName = reader.ReadString();
			successfulSortieSavedObjective.Faction = reader.ReadString();
			successfulSortieSavedObjective.DisplayName = reader.ReadString();
			successfulSortieSavedObjective.Hidden = reader.ReadBooleanExtension();
			successfulSortieSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return successfulSortieSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002ESuccessfulSortieSavedObjective(NetworkWriter writer, SuccessfulSortieSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteSingleConverter(value.minimumScore);
			writer.WriteBooleanExtension(value.additive);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static WaitTimeSavedObjective _Read_NuclearOption_002ESavedMission_002EObjectives_002EWaitTimeSavedObjective(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			WaitTimeSavedObjective waitTimeSavedObjective = new WaitTimeSavedObjective();
			waitTimeSavedObjective.seconds = reader.ReadSingleConverter();
			waitTimeSavedObjective.UniqueName = reader.ReadString();
			waitTimeSavedObjective.Faction = reader.ReadString();
			waitTimeSavedObjective.DisplayName = reader.ReadString();
			waitTimeSavedObjective.Hidden = reader.ReadBooleanExtension();
			waitTimeSavedObjective.Outcomes = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(reader);
			return waitTimeSavedObjective;
		}

		public static void _Write_NuclearOption_002ESavedMission_002EObjectives_002EWaitTimeSavedObjective(NetworkWriter writer, WaitTimeSavedObjective value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			writer.WriteSingleConverter(value.seconds);
			writer.WriteString(value.UniqueName);
			writer.WriteString(value.Faction);
			writer.WriteString(value.DisplayName);
			writer.WriteBooleanExtension(value.Hidden);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E(writer, value.Outcomes);
		}

		public static void _Write_PersistentID(NetworkWriter writer, PersistentID value)
		{
			writer.WritePackedUInt32(value.Id);
		}

		public static PersistentID _Read_PersistentID(NetworkReader reader)
		{
			return new PersistentID
			{
				Id = reader.ReadPackedUInt32()
			};
		}

		public static PlayerRef _Read_NuclearOption_002ENetworking_002EPlayerRef(NetworkReader reader)
		{
			/*
			return new PlayerRef
			{
				PlayerId = reader.ReadPackedUInt32()
			};*/
			return new PlayerRef();
		}

		public static void _Write_NuclearOption_002ENetworking_002EPlayerRef(NetworkWriter writer, PlayerRef value)
		{
			writer.WritePackedUInt32(value.PlayerId);
		}

		public static Airbase _Read_Airbase(NetworkReader reader)
		{
			return (Airbase)reader.ReadNetworkBehaviour();
		}

		public static NetworkBehaviorSyncvar<Airbase> _Read_Mirage_002ENetworkBehaviorSyncvar_00601_003CAirbase_003E(NetworkReader reader)
		{
			return reader.ReadGenericNetworkBehaviourSyncVar<Airbase>();
		}

		public static void _Write_Airbase(NetworkWriter writer, Airbase value)
		{
			writer.WriteNetworkBehaviour(value);
		}

		public static void _Write_Mirage_002ENetworkBehaviorSyncvar_00601_003CAirbase_003E(NetworkWriter writer, NetworkBehaviorSyncvar<Airbase> value)
		{
			writer.WriteGenericNetworkBehaviorSyncVar(value);
		}

		public static FactionHQ.RuntimeSupply _Read_FactionHQ_002FRuntimeSupply(NetworkReader reader)
		{
			return new FactionHQ.RuntimeSupply();
		}

		public static void _Write_FactionHQ_002FRuntimeSupply(NetworkWriter writer, FactionHQ.RuntimeSupply value)
		{
			writer.WritePackedInt32(value.Count);
		}

		public static void _Write_NuclearOption_002EExclusionZone(NetworkWriter writer, ExclusionZone value)
		{
			_Write_PersistentID(writer, value.sourceId);
			writer.WriteGlobalPosition(value.position);
			writer.WriteSingleConverter(value.radius);
		}

		public static ExclusionZone _Read_NuclearOption_002EExclusionZone(NetworkReader reader)
		{
			return new ExclusionZone();
		}

		public static List<int> _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EInt32_003E(NetworkReader reader)
		{
			return reader.ReadList<int>();
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EInt32_003E(NetworkWriter writer, List<int> value)
		{
			writer.WriteList(value);
		}

		public static void _Write_MissionStatsTracker_002FTypeStat(NetworkWriter writer, MissionStatsTracker.TypeStat value)
		{
			_Write_MissionStatsTracker_002FStat(writer, value.total);
			_Write_MissionStatsTracker_002FStat(writer, value.buildings);
			_Write_MissionStatsTracker_002FStat(writer, value.ships);
			_Write_MissionStatsTracker_002FStat(writer, value.vehicles);
			_Write_MissionStatsTracker_002FStat(writer, value.aircraft);
		}

		public static void _Write_MissionStatsTracker_002FStat(NetworkWriter writer, MissionStatsTracker.Stat value)
		{
			writer.WriteSingleConverter(value.total);
			writer.WriteSingleConverter(value.current);
			writer.WriteSingleConverter(value.spent);
			writer.WriteSingleConverter(value.lost);
		}

		public static MissionStatsTracker.TypeStat _Read_MissionStatsTracker_002FTypeStat(NetworkReader reader)
		{
			return new MissionStatsTracker.TypeStat
			{
				total = _Read_MissionStatsTracker_002FStat(reader),
				buildings = _Read_MissionStatsTracker_002FStat(reader),
				ships = _Read_MissionStatsTracker_002FStat(reader),
				vehicles = _Read_MissionStatsTracker_002FStat(reader),
				aircraft = _Read_MissionStatsTracker_002FStat(reader)
			};
		}

		public static MissionStatsTracker.Stat _Read_MissionStatsTracker_002FStat(NetworkReader reader)
		{
			return new MissionStatsTracker.Stat
			{
				total = reader.ReadSingleConverter(),
				current = reader.ReadSingleConverter(),
				spent = reader.ReadSingleConverter(),
				lost = reader.ReadSingleConverter()
			};
		}

		public static RearmMissionController.RearmerWithMission _Read_RearmMissionController_002FRearmerWithMission(NetworkReader reader)
		{
			return new RearmMissionController.RearmerWithMission();
		}

		public static Unit _Read_Unit(NetworkReader reader)
		{
			return (Unit)reader.ReadNetworkBehaviour();
		}

		public static void _Write_RearmMissionController_002FRearmerWithMission(NetworkWriter writer, RearmMissionController.RearmerWithMission value)
		{
			_Write_Unit(writer, value.RearmerUnit);
			_Write_Unit(writer, value.RequesterUnit);
		}

		public static void _Write_Unit(NetworkWriter writer, Unit value)
		{
			writer.WriteNetworkBehaviour(value);
		}

		public static void _Write_Airbase_002FTrySpawnResult(NetworkWriter writer, Airbase.TrySpawnResult value)
		{
			writer.WriteBooleanExtension(value.Allowed);
			_Write_Hangar(writer, value.Hangar);
			writer.WriteBooleanExtension(value.DelayedSpawn);
		}

		public static void _Write_Hangar(NetworkWriter writer, Hangar value)
		{
			writer.WriteNetworkBehaviour(value);
		}

		public static Airbase.TrySpawnResult _Read_Airbase_002FTrySpawnResult(NetworkReader reader)
		{
			return new Airbase.TrySpawnResult();
		}

		public static Hangar _Read_Hangar(NetworkReader reader)
		{
			return (Hangar)reader.ReadNetworkBehaviour();
		}

		public static void _Write_LiveryKey(NetworkWriter writer, LiveryKey value)
		{
			_Write_LiveryKey_002FKeyType(writer, value.Type);
			writer.WritePackedInt32(value.Index);
			writer.WriteString(value.AppDataName, 128);
			writer.WritePackedUInt64(value.Id);
		}

		public static LiveryKey _Read_LiveryKey(NetworkReader reader)
		{
			return new LiveryKey();
		}

		public static void _Write_NuclearOption_002ESavedMission_002ELoadout(NetworkWriter writer, Loadout value)
		{
			if (value == null)
			{
				writer.WriteBooleanExtension(value: false);
				return;
			}
			writer.WriteBooleanExtension(value: true);
			_Write_System_002ECollections_002EGeneric_002EList_00601_003CWeaponMount_003E_WithLength(writer, value.weapons, 16);
		}

		public static void _Write_System_002ECollections_002EGeneric_002EList_00601_003CWeaponMount_003E_WithLength(NetworkWriter writer, List<WeaponMount> value, int maxLength)
		{
			writer.WriteList(value, maxLength);
		}

		public static Loadout _Read_NuclearOption_002ESavedMission_002ELoadout(NetworkReader reader)
		{
			if (!reader.ReadBooleanExtension())
			{
				return null;
			}
			Loadout loadout = new Loadout();
			loadout.weapons = _Read_System_002ECollections_002EGeneric_002EList_00601_003CWeaponMount_003E_WithLength(reader, 16);
			return loadout;
		}

		public static List<WeaponMount> _Read_System_002ECollections_002EGeneric_002EList_00601_003CWeaponMount_003E_WithLength(NetworkReader reader, int maxLength)
		{
			return reader.ReadList<WeaponMount>(maxLength);
		}

		public static void _Write_NuclearOption_002ENetworking_002EPlayer(NetworkWriter writer, Player value)
		{
			writer.WriteNetworkBehaviour(value);
		}

		public static Player _Read_NuclearOption_002ENetworking_002EPlayer(NetworkReader reader)
		{
			return (Player)reader.ReadNetworkBehaviour();
		}

		public static void _Write_FactionHQ_002FRewardType(NetworkWriter writer, FactionHQ.RewardType value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static FactionHQ.RewardType _Read_FactionHQ_002FRewardType(NetworkReader reader)
		{
			return (FactionHQ.RewardType)reader.ReadByteExtension();
		}

		public static void _Write_KillType(NetworkWriter writer, KillType value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static KillType _Read_KillType(NetworkReader reader)
		{
			return (KillType)reader.ReadByteExtension();
		}

		public static void _Write_Unit_002FUnitState(NetworkWriter writer, Unit.UnitState value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static Unit.UnitState _Read_Unit_002FUnitState(NetworkReader reader)
		{
			return (Unit.UnitState)reader.ReadByteExtension();
		}

		public static void _Write_WeaponMask(NetworkWriter writer, WeaponMask value)
		{
			writer.WritePackedInt32(value.Mask);
		}

		public static WeaponMask _Read_WeaponMask(NetworkReader reader)
		{
			return new WeaponMask();
		}

		public static void _Write_RearmEventArgs(NetworkWriter writer, RearmEventArgs value)
		{
			_Write_Unit(writer, value.Rearmer);
			_Write_System_002EInt32_005B_005D(writer, value.Stations);
		}

		public static void _Write_System_002EInt32_005B_005D(NetworkWriter writer, int[] value)
		{
			writer.WriteArray(value);
		}

		public static RearmEventArgs _Read_RearmEventArgs(NetworkReader reader)
		{
			return new RearmEventArgs();
		}

		public static int[] _Read_System_002EInt32_005B_005D(NetworkReader reader)
		{
			return reader.ReadArray<int>();
		}

		public static void _Write_Unit_002FJamEventArgs(NetworkWriter writer, Unit.JamEventArgs value)
		{
			_Write_Unit(writer, value.jammingUnit);
			writer.WriteSingleConverter(value.jamAmount);
		}

		public static Unit.JamEventArgs _Read_Unit_002FJamEventArgs(NetworkReader reader)
		{
			return new Unit.JamEventArgs();
		}

		public static void _Write_System_002EReadOnlySpan_00601_003CPersistentID_003E_WithLength(NetworkWriter writer, ReadOnlySpan<PersistentID> value, int maxLength)
		{
			writer.WriteReadOnlySpan(value, maxLength);
		}

		public static ReadOnlySpan<PersistentID> _Read_System_002EReadOnlySpan_00601_003CPersistentID_003E_WithLength(NetworkReader reader, int maxLength)
		{
			return reader.ReadReadOnlySpan<PersistentID>(maxLength);
		}

		public static void _Write_System_002EReadOnlySpan_00601_003CPersistentID_003E(NetworkWriter writer, ReadOnlySpan<PersistentID> value)
		{
			writer.WriteReadOnlySpan(value);
		}

		public static ReadOnlySpan<PersistentID> _Read_System_002EReadOnlySpan_00601_003CPersistentID_003E(NetworkReader reader)
		{
			return reader.ReadReadOnlySpan<PersistentID>();
		}

		public static void _Write_DamageInfo(NetworkWriter writer, DamageInfo value)
		{
			_Write_CompressedFloat(writer, value.pierceDamage);
			_Write_CompressedFloat(writer, value.blastDamage);
			_Write_CompressedFloat(writer, value.fireDamage);
			_Write_CompressedFloat(writer, value.impactDamage);
		}

		public static DamageInfo _Read_DamageInfo(NetworkReader reader)
		{
			return new DamageInfo();
		}

		public static void _Write_SlingloadHook_002FDeployState(NetworkWriter writer, SlingloadHook.DeployState value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static SlingloadHook.DeployState _Read_SlingloadHook_002FDeployState(NetworkReader reader)
		{
			return (SlingloadHook.DeployState)reader.ReadByteExtension();
		}

		public static void _Write_Aircraft(NetworkWriter writer, Aircraft value)
		{
			writer.WriteNetworkBehaviour(value);
		}

		public static Aircraft _Read_Aircraft(NetworkReader reader)
		{
			return (Aircraft)reader.ReadNetworkBehaviour();
		}

		public static void _Write_System_002ENullable_00601_003CSystem_002EByte_003E(NetworkWriter writer, byte? value)
		{
			writer.WriteNullable(value);
		}

		public static byte? _Read_System_002ENullable_00601_003CSystem_002EByte_003E(NetworkReader reader)
		{
			return reader.ReadNullable<byte>();
		}

		public static void _Write_Hangar_002FDoorState(NetworkWriter writer, Hangar.DoorState value)
		{
			writer.WriteBooleanExtension(value.opening);
			writer.WriteSingleConverter(value.openAmount);
		}

		public static Hangar.DoorState _Read_Hangar_002FDoorState(NetworkReader reader)
		{
			return new Hangar.DoorState();
		}

		public static void _Write_UnitCommand_002FCommand(NetworkWriter writer, UnitCommand.Command value)
		{
			writer.WriteSingleConverter(value.time);
			_Write_NuclearOption_002ENetworking_002EPlayer(writer, value.player);
			writer.WriteGlobalPosition(value.position);
		}

		public static UnitCommand.Command _Read_UnitCommand_002FCommand(NetworkReader reader)
		{
			return new UnitCommand.Command();
		}

		public static void _Write_PilotDismounted_002FPilotState(NetworkWriter writer, PilotDismounted.PilotState value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static PilotDismounted.PilotState _Read_PilotDismounted_002FPilotState(NetworkReader reader)
		{
			return (PilotDismounted.PilotState)reader.ReadByteExtension();
		}

		public static void _Write_Missile_002FSeekerMode(NetworkWriter writer, Missile.SeekerMode value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static Missile.SeekerMode _Read_Missile_002FSeekerMode(NetworkReader reader)
		{
			return (Missile.SeekerMode)reader.ReadByteExtension();
		}

		public static void _Write_NuclearOption_002EOwnedAirframe(NetworkWriter writer, OwnedAirframe value)
		{
			writer.WriteAircraftDefinition(value.Definition);
			writer.WriteBooleanExtension(value.Reserved);
		}

		public static void _Write_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E(NetworkWriter writer, OwnedAirframe? value)
		{
			writer.WriteNullable(value);
		}

		public static OwnedAirframe _Read_NuclearOption_002EOwnedAirframe(NetworkReader reader)
		{
			return new OwnedAirframe();
		}

		public static OwnedAirframe? _Read_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E(NetworkReader reader)
		{
			return reader.ReadNullable<OwnedAirframe>();
		}

		public static void _Write_NuclearOption_002EReserveNotice(NetworkWriter writer, ReserveNotice value)
		{
			_Write_NuclearOption_002EReserveEvent(writer, value.outcome);
			writer.WriteAircraftDefinition(value.aircraftDefinition);
			writer.WriteBooleanExtension(value.isReserving);
			writer.WritePackedInt32(value.queuePosition);
		}

		public static void _Write_NuclearOption_002EReserveEvent(NetworkWriter writer, ReserveEvent value)
		{
			writer.WriteByteExtension((byte)value);
		}

		public static ReserveNotice _Read_NuclearOption_002EReserveNotice(NetworkReader reader)
		{
			return new ReserveNotice();
		}

		public static ReserveEvent _Read_NuclearOption_002EReserveEvent(NetworkReader reader)
		{
			return (ReserveEvent)reader.ReadByteExtension();
		}

		public static void _Write_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig(NetworkWriter writer, VoteKickConfig.ClientConfig value)
		{
			writer.WriteBooleanExtension(value.Enabled);
			writer.WriteSingleConverter(value.VoteDuration);
			writer.WriteSingleConverter(value.ResolutionDisplayTime);
			writer.WriteSingleConverter(value.NewVoteLockout);
			writer.WriteSingleConverter(value.RequesterCooldown);
		}

		public static VoteKickConfig.ClientConfig _Read_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig(NetworkReader reader)
		{
			return new VoteKickConfig.ClientConfig();
		}

		public static void _Write_NuclearOption_002ENetworking_002EVoteKickState(NetworkWriter writer, VoteKickState value)
		{
			writer.WriteBooleanExtension(value.Active);
			writer.WriteBooleanExtension(value.Passed);
			_Write_Steamworks_002ECSteamID(writer, value.TargetID);
			writer.WritePackedInt32(value.YesVotes);
			writer.WritePackedInt32(value.NoVotes);
			writer.WritePackedInt32(value.VotesRequired);
			writer.WritePackedInt32(value.EligibleVoters);
			writer.WriteDoubleConverter(value.VoteEndTimeNetwork);
			writer.WriteDoubleConverter(value.LockoutEndTimeNetwork);
			writer.WritePackedInt32(value.VoteID);
		}

		public static void _Write_Steamworks_002ECSteamID(NetworkWriter writer, CSteamID value)
		{
			writer.WritePackedUInt64(value.m_SteamID);
		}

		public static VoteKickState _Read_NuclearOption_002ENetworking_002EVoteKickState(NetworkReader reader)
		{
			return new VoteKickState();
		}

		public static CSteamID _Read_Steamworks_002ECSteamID(NetworkReader reader)
		{
			return new CSteamID();
		}

		public static void _Write_System_002ENullable_00601_003CNuclearOption_002ENetworking_002EVoteKickState_003E(NetworkWriter writer, VoteKickState? value)
		{
			writer.WriteNullable(value);
		}

		public static VoteKickState? _Read_System_002ENullable_00601_003CNuclearOption_002ENetworking_002EVoteKickState_003E(NetworkReader reader)
		{
			return reader.ReadNullable<VoteKickState>();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void InitReadWriters()
		{
		/*
			Writer<INetworkDefinition>.Write = DefinitionWriters.WriteNetworkDefinition;
			Writer<UnitDefinition>.Write = DefinitionWriters.WriteUnitDefinition;
			Writer<AircraftDefinition>.Write = DefinitionWriters.WriteAircraftDefinition;
			Writer<VehicleDefinition>.Write = DefinitionWriters.WriteVehicleDefinition;
			Writer<MissileDefinition>.Write = DefinitionWriters.WriteMissileDefinition;
			Writer<BuildingDefinition>.Write = DefinitionWriters.WriteBuildingDefinition;
			Writer<ShipDefinition>.Write = DefinitionWriters.WriteShipDefinition;
			Writer<WeaponMount>.Write = DefinitionWriters.WriteWeaponMount;
			Writer<Faction>.Write = DefinitionWriters.WriteFaction;
			Writer<GlobalPosition>.Write = GlobalPositionExtensions.WriteGlobalPosition;
			Writer<NetworkMission.SyncMissionPart>.Write = NetworkMissionCustomWriter.WriteSyncMissionPart;
			Writer<SavedObjective>.Write = MissionNetworkExtensions.WriteSavedObjective;
			Writer<SavedOutcome>.Write = MissionNetworkExtensions.WriteSavedOutcome;
			Writer<GameObjectSyncvar>.Write = GameObjectSerializers.WriteGameObjectSyncVar;
			Writer<NetworkBehaviorSyncvar>.Write = NetworkBehaviorSerializers.WriteNetworkBehaviorSyncVar;
			Writer<NetworkIdentitySyncvar>.Write = NetworkIdentitySerializers.WriteNetworkIdentitySyncVar;
			Writer<SyncPrefab>.Write = SyncPrefabSerialize.WriteSyncPrefab;
			Writer<Span<byte>>.Write = Mirage.Serialization.CollectionExtensions.WriteSpanAndSize;
			Writer<ReadOnlySpan<byte>>.Write = Mirage.Serialization.CollectionExtensions.WriteSpanAndSize;
			Writer<byte[]>.Write = Mirage.Serialization.CollectionExtensions.WriteBytesAndSize;
			Writer<ArraySegment<byte>>.Write = Mirage.Serialization.CollectionExtensions.WriteBytesAndSizeSegment;
			Writer<Quaternion>.Write = CompressedExtensions.WriteQuaternion;
			Writer<NetworkIdentity>.Write = MirageTypesExtensions.WriteNetworkIdentity;
			Writer<NetworkBehaviour>.Write = MirageTypesExtensions.WriteNetworkBehaviour;
			Writer<GameObject>.Write = MirageTypesExtensions.WriteGameObject;
			Writer<int>.Write = PackedExtensions.WritePackedInt32;
			Writer<uint>.Write = PackedExtensions.WritePackedUInt32;
			Writer<long>.Write = PackedExtensions.WritePackedInt64;
			Writer<ulong>.Write = PackedExtensions.WritePackedUInt64;
			Writer<string>.Write = StringExtensions.WriteString;
			Writer<StringStore>.Write = StringStoreExtensions.WriteStringStore;
			Writer<byte>.Write = SystemTypesExtensions.WriteByteExtension;
			Writer<sbyte>.Write = SystemTypesExtensions.WriteSByteExtension;
			Writer<char>.Write = SystemTypesExtensions.WriteChar;
			Writer<bool>.Write = SystemTypesExtensions.WriteBooleanExtension;
			Writer<ushort>.Write = SystemTypesExtensions.WriteUInt16Extension;
			Writer<short>.Write = SystemTypesExtensions.WriteInt16Extension;
			Writer<float>.Write = SystemTypesExtensions.WriteSingleConverter;
			Writer<double>.Write = SystemTypesExtensions.WriteDoubleConverter;
			Writer<decimal>.Write = SystemTypesExtensions.WriteDecimalConverter;
			Writer<Guid>.Write = SystemTypesExtensions.WriteGuid;
			Writer<Vector2>.Write = UnityTypesExtensions.WriteVector2;
			Writer<Vector3>.Write = UnityTypesExtensions.WriteVector3;
			Writer<Vector4>.Write = UnityTypesExtensions.WriteVector4;
			Writer<Vector2Int>.Write = UnityTypesExtensions.WriteVector2Int;
			Writer<Vector3Int>.Write = UnityTypesExtensions.WriteVector3Int;
			Writer<Color>.Write = UnityTypesExtensions.WriteColor;
			Writer<Color32>.Write = UnityTypesExtensions.WriteColor32;
			Writer<Rect>.Write = UnityTypesExtensions.WriteRect;
			Writer<Plane>.Write = UnityTypesExtensions.WritePlane;
			Writer<Ray>.Write = UnityTypesExtensions.WriteRay;
			Writer<Matrix4x4>.Write = UnityTypesExtensions.WriteMatrix4X4;
			Writer<StringStoreLengthsMessage>.Write = StringStoreBrotliEncoderExtensions.WriteStringStoreLengthsMessage;
			Writer<StringStoreStringsMessage>.Write = StringStoreBrotliEncoderExtensions.WriteStringStoreStringsMessage;
			Writer<SceneNotReadyMessage>.Write = _Write_Mirage_002ESceneNotReadyMessage;
			Writer<AddCharacterMessage>.Write = _Write_Mirage_002EAddCharacterMessage;
			Writer<SceneMessage>.Write = _Write_Mirage_002ESceneMessage;
			Writer<SceneReadyMessage>.Write = _Write_Mirage_002ESceneReadyMessage;
			Writer<SpawnMessage>.Write = _Write_Mirage_002ESpawnMessage;
			Writer<ulong?>.Write = _Write_System_002ENullable_00601_003CSystem_002EUInt64_003E;
			Writer<int?>.Write = _Write_System_002ENullable_00601_003CSystem_002EInt32_003E;
			Writer<SpawnValues>.Write = _Write_Mirage_002ESpawnValues;
			Writer<Vector3?>.Write = _Write_System_002ENullable_00601_003CUnityEngine_002EVector3_003E;
			Writer<Quaternion?>.Write = _Write_System_002ENullable_00601_003CUnityEngine_002EQuaternion_003E;
			Writer<bool?>.Write = _Write_System_002ENullable_00601_003CSystem_002EBoolean_003E;
			Writer<RemoveAuthorityMessage>.Write = _Write_Mirage_002ERemoveAuthorityMessage;
			Writer<RemoveCharacterMessage>.Write = _Write_Mirage_002ERemoveCharacterMessage;
			Writer<ObjectDestroyMessage>.Write = _Write_Mirage_002EObjectDestroyMessage;
			Writer<ObjectHideMessage>.Write = _Write_Mirage_002EObjectHideMessage;
			Writer<UpdateVarsMessage>.Write = _Write_Mirage_002EUpdateVarsMessage;
			Writer<NetworkPingMessage>.Write = _Write_Mirage_002ENetworkPingMessage;
			Writer<NetworkPongMessage>.Write = _Write_Mirage_002ENetworkPongMessage;
			Writer<RpcMessage>.Write = _Write_Mirage_002ERemoteCalls_002ERpcMessage;
			Writer<RpcWithReplyMessage>.Write = _Write_Mirage_002ERemoteCalls_002ERpcWithReplyMessage;
			Writer<RpcReply>.Write = _Write_Mirage_002ERemoteCalls_002ERpcReply;
			Writer<AuthMessage>.Write = _Write_Mirage_002EAuthentication_002EAuthMessage;
			Writer<AuthSuccessMessage>.Write = _Write_Mirage_002EAuthentication_002EAuthSuccessMessage;
			Writer<MissionMessages.ActiveDialogueState>.Write = _Write_MissionMessages_002FActiveDialogueState;
			Writer<FactionHQ>.Write = _Write_FactionHQ;
			Writer<NetworkMission.SyncMissionHeader>.Write = _Write_NetworkMission_002FSyncMissionHeader;
			Writer<NetworkMission.State>.Write = _Write_NetworkMission_002FState;
			Writer<MissionSettings>.Write = _Write_NuclearOption_002ESavedMission_002EMissionSettings;
			Writer<MissionTag>.Write = _Write_NuclearOption_002ESavedMission_002EMissionTag;
			Writer<List<MissionTag>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionTag_003E;
			Writer<PlayerMode>.Write = _Write_NuclearOption_002ESavedMission_002EPlayerMode;
			Writer<PositionRotation>.Write = _Write_NuclearOption_002ESavedMission_002EPositionRotation;
			Writer<Override<PositionRotation>>.Write = _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E;
			Writer<RoadNetwork>.Write = _Write_RoadPathfinding_002ERoadNetwork;
			Writer<Road>.Write = _Write_RoadPathfinding_002ERoad;
			Writer<List<GlobalPosition>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E;
			Writer<List<Road>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CRoadPathfinding_002ERoad_003E;
			Writer<MissionEnvironment>.Write = _Write_NuclearOption_002ESavedMission_002EMissionEnvironment;
			Writer<NetworkMission.SyncMissionFooter>.Write = _Write_NetworkMission_002FSyncMissionFooter;
			Writer<NetworkMission.SyncMission>.Write = _Write_NetworkMission_002FSyncMission;
			Writer<Mission>.Write = _Write_NuclearOption_002ESavedMission_002EMission;
			Writer<MapKey>.Write = _Write_NuclearOption_002ESceneLoading_002EMapKey;
			Writer<MapKey.KeyType>.Write = _Write_NuclearOption_002ESceneLoading_002EMapKey_002FKeyType;
			Writer<SavedAircraft>.Write = _Write_NuclearOption_002ESavedMission_002ESavedAircraft;
			Writer<SavedLoadout>.Write = _Write_NuclearOption_002ESavedMission_002ESavedLoadout;
			Writer<SavedLoadout.SelectedMount>.Write = _Write_NuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount;
			Writer<List<SavedLoadout.SelectedMount>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount_003E;
			Writer<LiveryKey.KeyType>.Write = _Write_LiveryKey_002FKeyType;
			Writer<Override<float>>.Write = _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E;
			Writer<SavedInventory>.Write = _Write_NuclearOption_002ESavedMission_002ESavedInventory;
			Writer<UnitCount>.Write = _Write_NuclearOption_002ESavedMission_002EUnitCount;
			Writer<List<UnitCount>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E;
			Writer<List<SavedAircraft>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAircraft_003E;
			Writer<SavedVehicle>.Write = _Write_NuclearOption_002ESavedMission_002ESavedVehicle;
			Writer<VehicleWaypoint>.Write = _Write_NuclearOption_002ESavedMission_002EVehicleWaypoint;
			Writer<List<VehicleWaypoint>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E;
			Writer<List<SavedVehicle>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedVehicle_003E;
			Writer<SavedShip>.Write = _Write_NuclearOption_002ESavedMission_002ESavedShip;
			Writer<List<SavedShip>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedShip_003E;
			Writer<SavedBuilding>.Write = _Write_NuclearOption_002ESavedMission_002ESavedBuilding;
			Writer<SavedBuilding.FactoryOptions>.Write = _Write_NuclearOption_002ESavedMission_002ESavedBuilding_002FFactoryOptions;
			Writer<List<SavedBuilding>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedBuilding_003E;
			Writer<SavedScenery>.Write = _Write_NuclearOption_002ESavedMission_002ESavedScenery;
			Writer<List<SavedScenery>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedScenery_003E;
			Writer<SavedContainer>.Write = _Write_NuclearOption_002ESavedMission_002ESavedContainer;
			Writer<List<SavedContainer>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedContainer_003E;
			Writer<SavedMissile>.Write = _Write_NuclearOption_002ESavedMission_002ESavedMissile;
			Writer<List<SavedMissile>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedMissile_003E;
			Writer<SavedPilot>.Write = _Write_NuclearOption_002ESavedMission_002ESavedPilot;
			Writer<List<SavedPilot>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedPilot_003E;
			Writer<MissionFaction>.Write = _Write_NuclearOption_002ESavedMission_002EMissionFaction;
			Writer<Restrictions>.Write = _Write_NuclearOption_002ESavedMission_002ERestrictions;
			Writer<List<string>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E;
			Writer<List<MissionFaction>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionFaction_003E;
			Writer<SavedAirbase>.Write = _Write_NuclearOption_002ESavedMission_002ESavedAirbase;
			Writer<SavedRunway>.Write = _Write_NuclearOption_002ESavedMission_002ESavedRunway;
			Writer<GlobalPosition[]>.Write = _Write_GlobalPosition_005B_005D;
			Writer<List<SavedRunway>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedRunway_003E;
			Writer<List<SavedAirbase>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAirbase_003E;
			Writer<List<SavedObjective>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedObjective_003E;
			Writer<List<SavedOutcome>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedOutcome_003E;
			Writer<NetworkMission.SyncMissionStart>.Write = _Write_NetworkMission_002FSyncMissionStart;
			Writer<LoadMapMessage>.Write = _Write_NuclearOption_002ESceneLoading_002ELoadMapMessage;
			//Writer<NetworkTransformBase.NetworkSnapshot>.Write = _Write_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot;
			Writer<CompressedInputs>.Write = _Write_CompressedInputs;
			Writer<CompressedFloat>.Write = _Write_CompressedFloat;
			Writer<CompressedInputs?>.Write = _Write_System_002ENullable_00601_003CCompressedInputs_003E;
			Writer<double?>.Write = _Write_System_002ENullable_00601_003CSystem_002EDouble_003E;
			Writer<float?>.Write = _Write_System_002ENullable_00601_003CSystem_002ESingle_003E;
			Writer<Vector3Compressed>.Write = _Write_Vector3Compressed;
			//Writer<SendTransformBatcher.TransformMessage>.Write = _Write_NuclearOption_002ENetworkTransforms_002ESendTransformBatcher_002FTransformMessage;
			Writer<HostEndedMessage>.Write = _Write_NuclearOption_002ENetworking_002EHostEndedMessage;
			Writer<LoadWaitingSceneMessage>.Write = _Write_NuclearOption_002ENetworking_002ELoadWaitingSceneMessage;
			Writer<ServerLoadingProgressMessage>.Write = _Write_NuclearOption_002ENetworking_002EServerLoadingProgressMessage;
			Writer<NetworkAuthenticatorNuclearOption.AuthMessage>.Write = _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthMessage;
			Writer<PlayerType>.Write = _Write_NuclearOption_002ENetworking_002EPlayerType;
			Writer<NetworkAuthenticatorNuclearOption.PasswordChallenge>.Write = _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordChallenge;
			Writer<NetworkAuthenticatorNuclearOption.PasswordResponse>.Write = _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordResponse;
			Writer<NetworkAuthenticatorNuclearOption.AuthFailReason>.Write = _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthFailReason;
			Writer<NetworkAuthenticatorNuclearOption.BuildHashMismatch>.Write = _Write_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FBuildHashMismatch;
			Writer<CompleteObjectiveSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveSavedOutcome;
			Writer<CompleteObjectiveOutcome.Options>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveOutcome_002FOptions;
			Writer<EndGameSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EEndGameSavedOutcome;
			Writer<EndType>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EEndType;
			Writer<GiveScoreSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EGiveScoreSavedOutcome;
			Writer<ChangeType>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType;
			Writer<ModifyAirbaseSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EModifyAirbaseSavedOutcome;
			Writer<Override<string>>.Write = _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EString_003E;
			Writer<Override<bool>>.Write = _Write_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E;
			Writer<ModifyEnvironmentSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EModifyEnvironmentSavedOutcome;
			Writer<ModifyFactionSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EModifyFactionSavedOutcome;
			Writer<NoSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ENoSavedOutcome;
			Writer<RemoveUnitSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERemoveUnitSavedOutcome;
			Writer<RestrictionSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionSavedOutcome;
			Writer<RestrictionOutcome.Change>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionOutcome_002FChange;
			Writer<RevealUnitSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ERevealUnitSavedOutcome;
			Writer<ShowMessageSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EShowMessageSavedOutcome;
			Writer<SpawnUnitSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002ESpawnUnitSavedOutcome;
			Writer<StartObjectiveSavedOutcome>.Write = _Write_NuclearOption_002ESavedMission_002EOutcomes_002EStartObjectiveSavedOutcome;
			Writer<CaptureAirbaseSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ECaptureAirbaseSavedObjective;
			Writer<CompleteOrder>.Write = _Write_NuclearOption_002ESavedMission_002ECompleteOrder;
			Writer<CompleteOtherObjectiveSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ECompleteOtherObjectiveSavedObjective;
			Writer<CrashAircraftSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ECrashAircraftSavedObjective;
			Writer<DestroyUnitSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002EDestroyUnitSavedObjective;
			Writer<DialogueBoxSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002EDialogueBoxSavedObjective;
			Writer<NoSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ENoSavedObjective;
			Writer<ReachUnitsSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002EReachUnitsSavedObjective;
			Writer<SavedReachUnitData>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData;
			Writer<List<SavedReachUnitData>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData_003E;
			Writer<ReachWaypointsSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002EReachWaypointsSavedObjective;
			Writer<SavedWaypoint>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint;
			Writer<List<SavedWaypoint>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint_003E;
			Writer<SpotUnitSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ESpotUnitSavedObjective;
			Writer<SuccessfulSortieSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002ESuccessfulSortieSavedObjective;
			Writer<WaitTimeSavedObjective>.Write = _Write_NuclearOption_002ESavedMission_002EObjectives_002EWaitTimeSavedObjective;
			Writer<PersistentID>.Write = _Write_PersistentID;
			Writer<PlayerRef>.Write = _Write_NuclearOption_002ENetworking_002EPlayerRef;
			Writer<Airbase>.Write = _Write_Airbase;
			Writer<NetworkBehaviorSyncvar<Airbase>>.Write = _Write_Mirage_002ENetworkBehaviorSyncvar_00601_003CAirbase_003E;
			Writer<FactionHQ.RuntimeSupply>.Write = _Write_FactionHQ_002FRuntimeSupply;
			Writer<ExclusionZone>.Write = _Write_NuclearOption_002EExclusionZone;
			Writer<List<int>>.Write = _Write_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EInt32_003E;
			Writer<MissionStatsTracker.TypeStat>.Write = _Write_MissionStatsTracker_002FTypeStat;
			Writer<MissionStatsTracker.Stat>.Write = _Write_MissionStatsTracker_002FStat;
			Writer<RearmMissionController.RearmerWithMission>.Write = _Write_RearmMissionController_002FRearmerWithMission;
			Writer<Unit>.Write = _Write_Unit;
			Writer<Airbase.TrySpawnResult>.Write = _Write_Airbase_002FTrySpawnResult;
			Writer<Hangar>.Write = _Write_Hangar;
			Writer<LiveryKey>.Write = _Write_LiveryKey;
			Writer<Loadout>.Write = _Write_NuclearOption_002ESavedMission_002ELoadout;
			Writer<Player>.Write = _Write_NuclearOption_002ENetworking_002EPlayer;
			Writer<FactionHQ.RewardType>.Write = _Write_FactionHQ_002FRewardType;
			Writer<KillType>.Write = _Write_KillType;
			Writer<Unit.UnitState>.Write = _Write_Unit_002FUnitState;
			Writer<WeaponMask>.Write = _Write_WeaponMask;
			Writer<RearmEventArgs>.Write = _Write_RearmEventArgs;
			Writer<int[]>.Write = _Write_System_002EInt32_005B_005D;
			Writer<Unit.JamEventArgs>.Write = _Write_Unit_002FJamEventArgs;
			Writer<ReadOnlySpan<PersistentID>>.Write = _Write_System_002EReadOnlySpan_00601_003CPersistentID_003E;
			Writer<DamageInfo>.Write = _Write_DamageInfo;
			Writer<SlingloadHook.DeployState>.Write = _Write_SlingloadHook_002FDeployState;
			Writer<Aircraft>.Write = _Write_Aircraft;
			Writer<byte?>.Write = _Write_System_002ENullable_00601_003CSystem_002EByte_003E;
			Writer<Hangar.DoorState>.Write = _Write_Hangar_002FDoorState;
			Writer<UnitCommand.Command>.Write = _Write_UnitCommand_002FCommand;
			Writer<PilotDismounted.PilotState>.Write = _Write_PilotDismounted_002FPilotState;
			Writer<Missile.SeekerMode>.Write = _Write_Missile_002FSeekerMode;
			Writer<OwnedAirframe>.Write = _Write_NuclearOption_002EOwnedAirframe;
			Writer<OwnedAirframe?>.Write = _Write_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E;
			Writer<ReserveNotice>.Write = _Write_NuclearOption_002EReserveNotice;
			Writer<ReserveEvent>.Write = _Write_NuclearOption_002EReserveEvent;
			Writer<VoteKickConfig.ClientConfig>.Write = _Write_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig;
			Writer<VoteKickState>.Write = _Write_NuclearOption_002ENetworking_002EVoteKickState;
			Writer<CSteamID>.Write = _Write_Steamworks_002ECSteamID;
			Writer<VoteKickState?>.Write = _Write_System_002ENullable_00601_003CNuclearOption_002ENetworking_002EVoteKickState_003E;
			Writer<Span<byte>>.WriteWithLength = Mirage.Serialization.CollectionExtensions.WriteSpanAndSize;
			Writer<ReadOnlySpan<byte>>.WriteWithLength = Mirage.Serialization.CollectionExtensions.WriteSpanAndSize;
			Writer<byte[]>.WriteWithLength = Mirage.Serialization.CollectionExtensions.WriteBytesAndSize;
			Writer<ArraySegment<byte>>.WriteWithLength = Mirage.Serialization.CollectionExtensions.WriteBytesAndSizeSegment;
			Writer<string>.WriteWithLength = StringExtensions.WriteString;
			Writer<List<WeaponMount>>.WriteWithLength = _Write_System_002ECollections_002EGeneric_002EList_00601_003CWeaponMount_003E_WithLength;
			Writer<ReadOnlySpan<PersistentID>>.WriteWithLength = _Write_System_002EReadOnlySpan_00601_003CPersistentID_003E_WithLength;
			Reader<UnitDefinition>.Read = DefinitionWriters.ReadUnitDefinition;
			Reader<AircraftDefinition>.Read = DefinitionWriters.ReadAircraftDefinition;
			Reader<VehicleDefinition>.Read = DefinitionWriters.ReadVehicleDefinition;
			Reader<MissileDefinition>.Read = DefinitionWriters.ReadMissileDefinition;
			Reader<BuildingDefinition>.Read = DefinitionWriters.ReadBuildingDefinition;
			Reader<ShipDefinition>.Read = DefinitionWriters.ReadShipDefinition;
			Reader<WeaponMount>.Read = DefinitionWriters.ReadWeaponMount;
			Reader<Faction>.Read = DefinitionWriters.ReadFaction;
			Reader<GlobalPosition>.Read = GlobalPositionExtensions.ReadGlobalPosition;
			Reader<NetworkMission.SyncMissionPart>.Read = NetworkMissionCustomWriter.ReadSyncMissionPart;
			Reader<SavedObjective>.Read = MissionNetworkExtensions.ReadSavedObjective;
			Reader<SavedOutcome>.Read = MissionNetworkExtensions.ReadSavedOutcome;
			Reader<GameObjectSyncvar>.Read = GameObjectSerializers.ReadGameObjectSyncVar;
			Reader<NetworkBehaviorSyncvar>.Read = NetworkBehaviorSerializers.ReadNetworkBehaviourSyncVar;
			Reader<NetworkIdentitySyncvar>.Read = NetworkIdentitySerializers.ReadNetworkIdentitySyncVar;
			Reader<SyncPrefab>.Read = SyncPrefabSerialize.ReadSyncPrefab;
			Reader<byte[]>.Read = Mirage.Serialization.CollectionExtensions.ReadBytesAndSize;
			Reader<ArraySegment<byte>>.Read = Mirage.Serialization.CollectionExtensions.ReadBytesAndSizeSegment;
			Reader<Span<byte>>.Read = Mirage.Serialization.CollectionExtensions.ReadSpanAndSize;
			Reader<ReadOnlySpan<byte>>.Read = Mirage.Serialization.CollectionExtensions.ReadReadOnlySpanAndSize;
			Reader<Quaternion>.Read = CompressedExtensions.ReadQuaternion;
			Reader<MirageNetworkReader>.Read = MirageTypesExtensions.ToMirageReader;
			Reader<NetworkIdentity>.Read = MirageTypesExtensions.ReadNetworkIdentity;
			Reader<NetworkBehaviour>.Read = MirageTypesExtensions.ReadNetworkBehaviour;
			Reader<GameObject>.Read = MirageTypesExtensions.ReadGameObject;
			Reader<int>.Read = PackedExtensions.ReadPackedInt32;
			Reader<uint>.Read = PackedExtensions.ReadPackedUInt32;
			Reader<long>.Read = PackedExtensions.ReadPackedInt64;
			Reader<ulong>.Read = PackedExtensions.ReadPackedUInt64;
			Reader<string>.Read = StringExtensions.ReadString;
			Reader<StringStore>.Read = StringStoreExtensions.ReadStringStore;
			Reader<byte>.Read = SystemTypesExtensions.ReadByteExtension;
			Reader<sbyte>.Read = SystemTypesExtensions.ReadSByteExtension;
			Reader<char>.Read = SystemTypesExtensions.ReadChar;
			Reader<bool>.Read = SystemTypesExtensions.ReadBooleanExtension;
			Reader<short>.Read = SystemTypesExtensions.ReadInt16Extension;
			Reader<ushort>.Read = SystemTypesExtensions.ReadUInt16Extension;
			Reader<float>.Read = SystemTypesExtensions.ReadSingleConverter;
			Reader<double>.Read = SystemTypesExtensions.ReadDoubleConverter;
			Reader<decimal>.Read = SystemTypesExtensions.ReadDecimalConverter;
			Reader<Guid>.Read = SystemTypesExtensions.ReadGuid;
			Reader<Vector2>.Read = UnityTypesExtensions.ReadVector2;
			Reader<Vector3>.Read = UnityTypesExtensions.ReadVector3;
			Reader<Vector4>.Read = UnityTypesExtensions.ReadVector4;
			Reader<Vector2Int>.Read = UnityTypesExtensions.ReadVector2Int;
			Reader<Vector3Int>.Read = UnityTypesExtensions.ReadVector3Int;
			Reader<Color>.Read = UnityTypesExtensions.ReadColor;
			Reader<Color32>.Read = UnityTypesExtensions.ReadColor32;
			Reader<Rect>.Read = UnityTypesExtensions.ReadRect;
			Reader<Plane>.Read = UnityTypesExtensions.ReadPlane;
			Reader<Ray>.Read = UnityTypesExtensions.ReadRay;
			Reader<Matrix4x4>.Read = UnityTypesExtensions.ReadMatrix4x4;
			Reader<StringStoreLengthsMessage>.Read = StringStoreBrotliEncoderExtensions.ReadStringStoreLengthsMessage;
			Reader<StringStoreStringsMessage>.Read = StringStoreBrotliEncoderExtensions.ReadStringStoreStringsMessage;
			Reader<SceneNotReadyMessage>.Read = _Read_Mirage_002ESceneNotReadyMessage;
			Reader<AddCharacterMessage>.Read = _Read_Mirage_002EAddCharacterMessage;
			Reader<SceneMessage>.Read = _Read_Mirage_002ESceneMessage;
			Reader<SceneReadyMessage>.Read = _Read_Mirage_002ESceneReadyMessage;
			Reader<SpawnMessage>.Read = _Read_Mirage_002ESpawnMessage;
			Reader<ulong?>.Read = _Read_System_002ENullable_00601_003CSystem_002EUInt64_003E;
			Reader<int?>.Read = _Read_System_002ENullable_00601_003CSystem_002EInt32_003E;
			Reader<SpawnValues>.Read = _Read_Mirage_002ESpawnValues;
			Reader<Vector3?>.Read = _Read_System_002ENullable_00601_003CUnityEngine_002EVector3_003E;
			Reader<Quaternion?>.Read = _Read_System_002ENullable_00601_003CUnityEngine_002EQuaternion_003E;
			Reader<bool?>.Read = _Read_System_002ENullable_00601_003CSystem_002EBoolean_003E;
			Reader<RemoveAuthorityMessage>.Read = _Read_Mirage_002ERemoveAuthorityMessage;
			Reader<RemoveCharacterMessage>.Read = _Read_Mirage_002ERemoveCharacterMessage;
			Reader<ObjectDestroyMessage>.Read = _Read_Mirage_002EObjectDestroyMessage;
			Reader<ObjectHideMessage>.Read = _Read_Mirage_002EObjectHideMessage;
			Reader<UpdateVarsMessage>.Read = _Read_Mirage_002EUpdateVarsMessage;
			Reader<NetworkPingMessage>.Read = _Read_Mirage_002ENetworkPingMessage;
			Reader<NetworkPongMessage>.Read = _Read_Mirage_002ENetworkPongMessage;
			Reader<RpcMessage>.Read = _Read_Mirage_002ERemoteCalls_002ERpcMessage;
			Reader<RpcWithReplyMessage>.Read = _Read_Mirage_002ERemoteCalls_002ERpcWithReplyMessage;
			Reader<RpcReply>.Read = _Read_Mirage_002ERemoteCalls_002ERpcReply;
			Reader<AuthMessage>.Read = _Read_Mirage_002EAuthentication_002EAuthMessage;
			Reader<AuthSuccessMessage>.Read = _Read_Mirage_002EAuthentication_002EAuthSuccessMessage;
			Reader<MissionMessages.ActiveDialogueState>.Read = _Read_MissionMessages_002FActiveDialogueState;
			Reader<FactionHQ>.Read = _Read_FactionHQ;
			Reader<NetworkMission.SyncMissionHeader>.Read = _Read_NetworkMission_002FSyncMissionHeader;
			Reader<NetworkMission.State>.Read = _Read_NetworkMission_002FState;
			Reader<MissionSettings>.Read = _Read_NuclearOption_002ESavedMission_002EMissionSettings;
			Reader<MissionTag>.Read = _Read_NuclearOption_002ESavedMission_002EMissionTag;
			Reader<List<MissionTag>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionTag_003E;
			Reader<PlayerMode>.Read = _Read_NuclearOption_002ESavedMission_002EPlayerMode;
			Reader<PositionRotation>.Read = _Read_NuclearOption_002ESavedMission_002EPositionRotation;
			Reader<Override<PositionRotation>>.Read = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CNuclearOption_002ESavedMission_002EPositionRotation_003E;
			Reader<RoadNetwork>.Read = _Read_RoadPathfinding_002ERoadNetwork;
			Reader<Road>.Read = _Read_RoadPathfinding_002ERoad;
			Reader<List<GlobalPosition>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CGlobalPosition_003E;
			Reader<List<Road>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CRoadPathfinding_002ERoad_003E;
			Reader<MissionEnvironment>.Read = _Read_NuclearOption_002ESavedMission_002EMissionEnvironment;
			Reader<NetworkMission.SyncMissionFooter>.Read = _Read_NetworkMission_002FSyncMissionFooter;
			Reader<NetworkMission.SyncMission>.Read = _Read_NetworkMission_002FSyncMission;
			Reader<Mission>.Read = _Read_NuclearOption_002ESavedMission_002EMission;
			Reader<MapKey>.Read = _Read_NuclearOption_002ESceneLoading_002EMapKey;
			Reader<MapKey.KeyType>.Read = _Read_NuclearOption_002ESceneLoading_002EMapKey_002FKeyType;
			Reader<SavedAircraft>.Read = _Read_NuclearOption_002ESavedMission_002ESavedAircraft;
			Reader<SavedLoadout>.Read = _Read_NuclearOption_002ESavedMission_002ESavedLoadout;
			Reader<SavedLoadout.SelectedMount>.Read = _Read_NuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount;
			Reader<List<SavedLoadout.SelectedMount>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedLoadout_002FSelectedMount_003E;
			Reader<LiveryKey.KeyType>.Read = _Read_LiveryKey_002FKeyType;
			Reader<Override<float>>.Read = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002ESingle_003E;
			Reader<SavedInventory>.Read = _Read_NuclearOption_002ESavedMission_002ESavedInventory;
			Reader<UnitCount>.Read = _Read_NuclearOption_002ESavedMission_002EUnitCount;
			Reader<List<UnitCount>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EUnitCount_003E;
			Reader<List<SavedAircraft>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAircraft_003E;
			Reader<SavedVehicle>.Read = _Read_NuclearOption_002ESavedMission_002ESavedVehicle;
			Reader<VehicleWaypoint>.Read = _Read_NuclearOption_002ESavedMission_002EVehicleWaypoint;
			Reader<List<VehicleWaypoint>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EVehicleWaypoint_003E;
			Reader<List<SavedVehicle>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedVehicle_003E;
			Reader<SavedShip>.Read = _Read_NuclearOption_002ESavedMission_002ESavedShip;
			Reader<List<SavedShip>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedShip_003E;
			Reader<SavedBuilding>.Read = _Read_NuclearOption_002ESavedMission_002ESavedBuilding;
			Reader<SavedBuilding.FactoryOptions>.Read = _Read_NuclearOption_002ESavedMission_002ESavedBuilding_002FFactoryOptions;
			Reader<List<SavedBuilding>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedBuilding_003E;
			Reader<SavedScenery>.Read = _Read_NuclearOption_002ESavedMission_002ESavedScenery;
			Reader<List<SavedScenery>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedScenery_003E;
			Reader<SavedContainer>.Read = _Read_NuclearOption_002ESavedMission_002ESavedContainer;
			Reader<List<SavedContainer>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedContainer_003E;
			Reader<SavedMissile>.Read = _Read_NuclearOption_002ESavedMission_002ESavedMissile;
			Reader<List<SavedMissile>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedMissile_003E;
			Reader<SavedPilot>.Read = _Read_NuclearOption_002ESavedMission_002ESavedPilot;
			Reader<List<SavedPilot>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedPilot_003E;
			Reader<MissionFaction>.Read = _Read_NuclearOption_002ESavedMission_002EMissionFaction;
			Reader<Restrictions>.Read = _Read_NuclearOption_002ESavedMission_002ERestrictions;
			Reader<List<string>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EString_003E;
			Reader<List<MissionFaction>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EMissionFaction_003E;
			Reader<SavedAirbase>.Read = _Read_NuclearOption_002ESavedMission_002ESavedAirbase;
			Reader<SavedRunway>.Read = _Read_NuclearOption_002ESavedMission_002ESavedRunway;
			Reader<GlobalPosition[]>.Read = _Read_GlobalPosition_005B_005D;
			Reader<List<SavedRunway>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedRunway_003E;
			Reader<List<SavedAirbase>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedAirbase_003E;
			Reader<List<SavedObjective>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedObjective_003E;
			Reader<List<SavedOutcome>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002ESavedOutcome_003E;
			Reader<NetworkMission.SyncMissionStart>.Read = _Read_NetworkMission_002FSyncMissionStart;
			Reader<LoadMapMessage>.Read = _Read_NuclearOption_002ESceneLoading_002ELoadMapMessage;
			//Reader<NetworkTransformBase.NetworkSnapshot>.Read = _Read_NuclearOption_002ENetworkTransforms_002ENetworkTransformBase_002FNetworkSnapshot;
			Reader<CompressedInputs>.Read = _Read_CompressedInputs;
			Reader<CompressedFloat>.Read = _Read_CompressedFloat;
			Reader<CompressedInputs?>.Read = _Read_System_002ENullable_00601_003CCompressedInputs_003E;
			Reader<double?>.Read = _Read_System_002ENullable_00601_003CSystem_002EDouble_003E;
			Reader<float?>.Read = _Read_System_002ENullable_00601_003CSystem_002ESingle_003E;
			Reader<Vector3Compressed>.Read = _Read_Vector3Compressed;
			//Reader<SendTransformBatcher.TransformMessage>.Read = _Read_NuclearOption_002ENetworkTransforms_002ESendTransformBatcher_002FTransformMessage;
			Reader<HostEndedMessage>.Read = _Read_NuclearOption_002ENetworking_002EHostEndedMessage;
			Reader<LoadWaitingSceneMessage>.Read = _Read_NuclearOption_002ENetworking_002ELoadWaitingSceneMessage;
			Reader<ServerLoadingProgressMessage>.Read = _Read_NuclearOption_002ENetworking_002EServerLoadingProgressMessage;
			Reader<NetworkAuthenticatorNuclearOption.AuthMessage>.Read = _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthMessage;
			Reader<PlayerType>.Read = _Read_NuclearOption_002ENetworking_002EPlayerType;
			Reader<NetworkAuthenticatorNuclearOption.PasswordChallenge>.Read = _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordChallenge;
			Reader<NetworkAuthenticatorNuclearOption.PasswordResponse>.Read = _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FPasswordResponse;
			Reader<NetworkAuthenticatorNuclearOption.AuthFailReason>.Read = _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FAuthFailReason;
			Reader<NetworkAuthenticatorNuclearOption.BuildHashMismatch>.Read = _Read_NuclearOption_002ENetworking_002EAuthentication_002ENetworkAuthenticatorNuclearOption_002FBuildHashMismatch;
			Reader<CompleteObjectiveSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveSavedOutcome;
			Reader<CompleteObjectiveOutcome.Options>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ECompleteObjectiveOutcome_002FOptions;
			Reader<EndGameSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EEndGameSavedOutcome;
			Reader<EndType>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EEndType;
			Reader<GiveScoreSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EGiveScoreSavedOutcome;
			Reader<ChangeType>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EChangeType;
			Reader<ModifyAirbaseSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EModifyAirbaseSavedOutcome;
			Reader<Override<string>>.Read = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EString_003E;
			Reader<Override<bool>>.Read = _Read_NuclearOption_002ESavedMission_002EOverride_00601_003CSystem_002EBoolean_003E;
			Reader<ModifyEnvironmentSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EModifyEnvironmentSavedOutcome;
			Reader<ModifyFactionSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EModifyFactionSavedOutcome;
			Reader<NoSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ENoSavedOutcome;
			Reader<RemoveUnitSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERemoveUnitSavedOutcome;
			Reader<RestrictionSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionSavedOutcome;
			Reader<RestrictionOutcome.Change>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERestrictionOutcome_002FChange;
			Reader<RevealUnitSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ERevealUnitSavedOutcome;
			Reader<ShowMessageSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EShowMessageSavedOutcome;
			Reader<SpawnUnitSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002ESpawnUnitSavedOutcome;
			Reader<StartObjectiveSavedOutcome>.Read = _Read_NuclearOption_002ESavedMission_002EOutcomes_002EStartObjectiveSavedOutcome;
			Reader<CaptureAirbaseSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ECaptureAirbaseSavedObjective;
			Reader<CompleteOrder>.Read = _Read_NuclearOption_002ESavedMission_002ECompleteOrder;
			Reader<CompleteOtherObjectiveSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ECompleteOtherObjectiveSavedObjective;
			Reader<CrashAircraftSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ECrashAircraftSavedObjective;
			Reader<DestroyUnitSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002EDestroyUnitSavedObjective;
			Reader<DialogueBoxSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002EDialogueBoxSavedObjective;
			Reader<NoSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ENoSavedObjective;
			Reader<ReachUnitsSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002EReachUnitsSavedObjective;
			Reader<SavedReachUnitData>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData;
			Reader<List<SavedReachUnitData>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedReachUnitData_003E;
			Reader<ReachWaypointsSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002EReachWaypointsSavedObjective;
			Reader<SavedWaypoint>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint;
			Reader<List<SavedWaypoint>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CNuclearOption_002ESavedMission_002EObjectives_002ESavedWaypoint_003E;
			Reader<SpotUnitSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ESpotUnitSavedObjective;
			Reader<SuccessfulSortieSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002ESuccessfulSortieSavedObjective;
			Reader<WaitTimeSavedObjective>.Read = _Read_NuclearOption_002ESavedMission_002EObjectives_002EWaitTimeSavedObjective;
			Reader<PersistentID>.Read = _Read_PersistentID;
			Reader<PlayerRef>.Read = _Read_NuclearOption_002ENetworking_002EPlayerRef;
			Reader<Airbase>.Read = _Read_Airbase;
			Reader<NetworkBehaviorSyncvar<Airbase>>.Read = _Read_Mirage_002ENetworkBehaviorSyncvar_00601_003CAirbase_003E;
			Reader<FactionHQ.RuntimeSupply>.Read = _Read_FactionHQ_002FRuntimeSupply;
			Reader<ExclusionZone>.Read = _Read_NuclearOption_002EExclusionZone;
			Reader<List<int>>.Read = _Read_System_002ECollections_002EGeneric_002EList_00601_003CSystem_002EInt32_003E;
			Reader<MissionStatsTracker.TypeStat>.Read = _Read_MissionStatsTracker_002FTypeStat;
			Reader<MissionStatsTracker.Stat>.Read = _Read_MissionStatsTracker_002FStat;
			Reader<RearmMissionController.RearmerWithMission>.Read = _Read_RearmMissionController_002FRearmerWithMission;
			Reader<Unit>.Read = _Read_Unit;
			Reader<Airbase.TrySpawnResult>.Read = _Read_Airbase_002FTrySpawnResult;
			Reader<Hangar>.Read = _Read_Hangar;
			Reader<LiveryKey>.Read = _Read_LiveryKey;
			Reader<Loadout>.Read = _Read_NuclearOption_002ESavedMission_002ELoadout;
			Reader<Player>.Read = _Read_NuclearOption_002ENetworking_002EPlayer;
			Reader<FactionHQ.RewardType>.Read = _Read_FactionHQ_002FRewardType;
			Reader<KillType>.Read = _Read_KillType;
			Reader<Unit.UnitState>.Read = _Read_Unit_002FUnitState;
			Reader<WeaponMask>.Read = _Read_WeaponMask;
			Reader<RearmEventArgs>.Read = _Read_RearmEventArgs;
			Reader<int[]>.Read = _Read_System_002EInt32_005B_005D;
			Reader<Unit.JamEventArgs>.Read = _Read_Unit_002FJamEventArgs;
			Reader<ReadOnlySpan<PersistentID>>.Read = _Read_System_002EReadOnlySpan_00601_003CPersistentID_003E;
			Reader<DamageInfo>.Read = _Read_DamageInfo;
			Reader<SlingloadHook.DeployState>.Read = _Read_SlingloadHook_002FDeployState;
			Reader<Aircraft>.Read = _Read_Aircraft;
			Reader<byte?>.Read = _Read_System_002ENullable_00601_003CSystem_002EByte_003E;
			Reader<Hangar.DoorState>.Read = _Read_Hangar_002FDoorState;
			Reader<UnitCommand.Command>.Read = _Read_UnitCommand_002FCommand;
			Reader<PilotDismounted.PilotState>.Read = _Read_PilotDismounted_002FPilotState;
			Reader<Missile.SeekerMode>.Read = _Read_Missile_002FSeekerMode;
			Reader<OwnedAirframe>.Read = _Read_NuclearOption_002EOwnedAirframe;
			Reader<OwnedAirframe?>.Read = _Read_System_002ENullable_00601_003CNuclearOption_002EOwnedAirframe_003E;
			Reader<ReserveNotice>.Read = _Read_NuclearOption_002EReserveNotice;
			Reader<ReserveEvent>.Read = _Read_NuclearOption_002EReserveEvent;
			Reader<VoteKickConfig.ClientConfig>.Read = _Read_NuclearOption_002ENetworking_002EVoteKickConfig_002FClientConfig;
			Reader<VoteKickState>.Read = _Read_NuclearOption_002ENetworking_002EVoteKickState;
			Reader<CSteamID>.Read = _Read_Steamworks_002ECSteamID;
			Reader<VoteKickState?>.Read = _Read_System_002ENullable_00601_003CNuclearOption_002ENetworking_002EVoteKickState_003E;
			Reader<byte[]>.ReadWithLength = Mirage.Serialization.CollectionExtensions.ReadBytesAndSize;
			Reader<ArraySegment<byte>>.ReadWithLength = Mirage.Serialization.CollectionExtensions.ReadBytesAndSizeSegment;
			Reader<Span<byte>>.ReadWithLength = Mirage.Serialization.CollectionExtensions.ReadSpanAndSize;
			Reader<ReadOnlySpan<byte>>.ReadWithLength = Mirage.Serialization.CollectionExtensions.ReadReadOnlySpanAndSize;
			Reader<string>.ReadWithLength = StringExtensions.ReadString;
			Reader<List<WeaponMount>>.ReadWithLength = _Read_System_002ECollections_002EGeneric_002EList_00601_003CWeaponMount_003E_WithLength;
			Reader<ReadOnlySpan<PersistentID>>.ReadWithLength = _Read_System_002EReadOnlySpan_00601_003CPersistentID_003E_WithLength;
			MessagePacker.RegisterMessage<SceneNotReadyMessage>();
			MessagePacker.RegisterMessage<AddCharacterMessage>();
			MessagePacker.RegisterMessage<SceneMessage>();
			MessagePacker.RegisterMessage<SceneReadyMessage>();
			MessagePacker.RegisterMessage<SpawnMessage>();
			MessagePacker.RegisterMessage<RemoveAuthorityMessage>();
			MessagePacker.RegisterMessage<RemoveCharacterMessage>();
			MessagePacker.RegisterMessage<ObjectDestroyMessage>();
			MessagePacker.RegisterMessage<ObjectHideMessage>();
			MessagePacker.RegisterMessage<UpdateVarsMessage>();
			MessagePacker.RegisterMessage<NetworkPingMessage>();
			MessagePacker.RegisterMessage<NetworkPongMessage>();
			MessagePacker.RegisterMessage<StringStoreLengthsMessage>();
			MessagePacker.RegisterMessage<StringStoreStringsMessage>();
			MessagePacker.RegisterMessage<RpcMessage>();
			MessagePacker.RegisterMessage<RpcWithReplyMessage>();
			MessagePacker.RegisterMessage<RpcReply>();
			MessagePacker.RegisterMessage<AuthMessage>();
			MessagePacker.RegisterMessage<AuthSuccessMessage>();
			MessagePacker.RegisterMessage<MissionMessages.ActiveDialogueState>();
			MessagePacker.RegisterMessage<NetworkMission.SyncMissionPart>();
			MessagePacker.RegisterMessage<NetworkMission.SyncMissionHeader>();
			MessagePacker.RegisterMessage<NetworkMission.SyncMissionFooter>();
			MessagePacker.RegisterMessage<NetworkMission.SyncMission>();
			MessagePacker.RegisterMessage<NetworkMission.SyncMissionStart>();
			MessagePacker.RegisterMessage<LoadMapMessage>();
			//MessagePacker.RegisterMessage<NetworkTransformBase.NetworkSnapshot>();
			//MessagePacker.RegisterMessage<SendTransformBatcher.TransformMessage>();
			MessagePacker.RegisterMessage<HostEndedMessage>();
			MessagePacker.RegisterMessage<LoadWaitingSceneMessage>();
			MessagePacker.RegisterMessage<ServerLoadingProgressMessage>();
			MessagePacker.RegisterMessage<NetworkAuthenticatorNuclearOption.AuthMessage>();
			MessagePacker.RegisterMessage<NetworkAuthenticatorNuclearOption.PasswordChallenge>();
			MessagePacker.RegisterMessage<NetworkAuthenticatorNuclearOption.PasswordResponse>();
			MessagePacker.RegisterMessage<NetworkAuthenticatorNuclearOption.AuthFailReason>();
			MessagePacker.RegisterMessage<NetworkAuthenticatorNuclearOption.BuildHashMismatch>();
			MessagePacker.RegisterMessage<Mission>();
			MessagePacker.RegisterMessage<CompleteObjectiveSavedOutcome>();
			MessagePacker.RegisterMessage<EndGameSavedOutcome>();
			MessagePacker.RegisterMessage<GiveScoreSavedOutcome>();
			MessagePacker.RegisterMessage<ModifyAirbaseSavedOutcome>();
			MessagePacker.RegisterMessage<ModifyEnvironmentSavedOutcome>();
			MessagePacker.RegisterMessage<ModifyFactionSavedOutcome>();
			MessagePacker.RegisterMessage<NoSavedOutcome>();
			MessagePacker.RegisterMessage<RemoveUnitSavedOutcome>();
			MessagePacker.RegisterMessage<RestrictionSavedOutcome>();
			MessagePacker.RegisterMessage<RevealUnitSavedOutcome>();
			MessagePacker.RegisterMessage<ShowMessageSavedOutcome>();
			MessagePacker.RegisterMessage<SpawnUnitSavedOutcome>();
			MessagePacker.RegisterMessage<StartObjectiveSavedOutcome>();
			MessagePacker.RegisterMessage<CaptureAirbaseSavedObjective>();
			MessagePacker.RegisterMessage<CompleteOtherObjectiveSavedObjective>();
			MessagePacker.RegisterMessage<CrashAircraftSavedObjective>();
			MessagePacker.RegisterMessage<DestroyUnitSavedObjective>();
			MessagePacker.RegisterMessage<DialogueBoxSavedObjective>();
			MessagePacker.RegisterMessage<NoSavedObjective>();
			MessagePacker.RegisterMessage<ReachUnitsSavedObjective>();
			MessagePacker.RegisterMessage<ReachWaypointsSavedObjective>();
			MessagePacker.RegisterMessage<SpotUnitSavedObjective>();
			MessagePacker.RegisterMessage<SuccessfulSortieSavedObjective>();
			MessagePacker.RegisterMessage<WaitTimeSavedObjective>();
			*/
		}
	}
}
