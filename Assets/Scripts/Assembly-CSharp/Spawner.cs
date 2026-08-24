using System;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

public class Spawner : NetworkSceneSingleton<Spawner>
{
	private delegate bool TrySpawnAction<TSaved, TUnit>(TSaved saved, out TUnit unit) where TSaved : SavedUnit where TUnit : Unit;

	private readonly LazySortedQueue<SavedAircraft> pendingPlayerControlled = new LazySortedQueue<SavedAircraft>(SortControlledAircraft);

	private bool requestSpawnInProgress;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 0;

	[NonSerialized]
	private const int RPC_COUNT = 2;

	public static int SortControlledAircraft(SavedAircraft x, SavedAircraft y)
	{
		return y.playerControlledPriority.CompareTo(x.playerControlledPriority);
	}

	public GameObject SpawnLocal(GameObject prefab, Transform parent)
	{
		return UnityEngine.Object.Instantiate(prefab, parent);
	}

	public void DestroyLocal(GameObject gameObject, float delay)
	{
		UnityEngine.Object.Destroy(gameObject, delay);
	}

	public bool TrySpawnPlayerControlled(Player player, out Aircraft aircraft)
	{
		if (!pendingPlayerControlled.TryDequeue(out var item))
		{
			return Fail<Aircraft>(out aircraft);
		}
		return TrySpawnPlayerAircraft(item, player, out aircraft);
	}

	private bool TrySpawnPlayerAircraft(SavedAircraft savedAircraft, Player player, out Aircraft aircraft)
	{
		RpcOnGivenAircraftControl(player.Owner);
		int aircraftRank = GetAircraftRank(savedAircraft);
		MissionManager.CurrentMission.missionSettings.SetMinimumStartingRank(aircraftRank);
		if (TrySpawnAircraft(savedAircraft, player, out aircraft))
		{
			AfterSpawn(savedAircraft, aircraft);
			return true;
		}
		return false;
	}

	private static int GetAircraftRank(SavedAircraft savedAircraft)
	{
		return ((AircraftDefinition)Encyclopedia.Lookup[savedAircraft.type]).aircraftParameters.rankRequired;
	}

	[ClientRpc(target = RpcTarget.Player)]
	public void RpcOnGivenAircraftControl(INetworkPlayer _)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Player, _, excludeOwner: false))
		{
			UserCode_RpcOnGivenAircraftControl_2052851100(base.Client.Player);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.SendTarget(this, 0, writer, Mirage.Channel.Reliable, _);
		writer.Release();
	}

	public void SpawnSavedUnit(SavedUnit savedUnit)
	{
		if (TrySpawnSavedUnitInternal(savedUnit, out var unit))
		{
			AfterSpawn(savedUnit, unit);
		}
	}

	private void AfterSpawn(SavedUnit savedUnit, Unit spawnedUnit)
	{
		spawnedUnit.LinkSavedUnit(savedUnit);
		if (!string.IsNullOrEmpty(savedUnit.UniqueName))
		{
			UnitRegistry.RegisterCustomID(savedUnit.UniqueName, spawnedUnit);
		}
	}

	private bool TrySpawnSavedUnitInternal(SavedUnit savedUnit, out Unit unit)
	{
		if (Encyclopedia.Lookup.TryGetValue(savedUnit.type, out var value))
		{
			if (!value.IsAllowed(MissionManager.AllowEventContent))
			{
				ColorLog<Spawner>.InfoWarn("Unit Type '" + savedUnit.type + "' is disabled or blocked by AllowEventContent setting");
				return Fail<Unit>(out unit);
			}
			if (!(savedUnit is SavedAircraft savedAircraft))
			{
				if (!(savedUnit is SavedVehicle savedVehicle))
				{
					if (!(savedUnit is SavedShip savedShip))
					{
						if (!(savedUnit is SavedBuilding savedBuilding))
						{
							if (!(savedUnit is SavedScenery savedScenery))
							{
								if (!(savedUnit is SavedContainer savedContainer))
								{
									if (!(savedUnit is SavedMissile savedMissile))
									{
										if (savedUnit is SavedPilot savedPilot)
										{
											if (TrySpawnPilot(savedPilot, out var spawnedPilot))
											{
												return Success<PilotDismounted, Unit>(spawnedPilot, out unit);
											}
											return Fail<Unit>(out unit);
										}
										throw new ArgumentException($"No Spawn code for SpawnUnit of type {savedUnit?.GetType()}");
									}
									if (TrySpawnMissile(savedMissile, out var spawnedMissile))
									{
										return Success<Missile, Unit>(spawnedMissile, out unit);
									}
									return Fail<Unit>(out unit);
								}
								if (TrySpawnContainer(savedContainer, out var spawnedContainer))
								{
									return Success<Container, Unit>(spawnedContainer, out unit);
								}
								return Fail<Unit>(out unit);
							}
							if (TrySpawnScenery(savedScenery, out var spawnedScenery))
							{
								return Success<Scenery, Unit>(spawnedScenery, out unit);
							}
							return Fail<Unit>(out unit);
						}
						if (TrySpawnBuilding(savedBuilding, out var spawnedBuilding))
						{
							return Success<Building, Unit>(spawnedBuilding, out unit);
						}
						return Fail<Unit>(out unit);
					}
					if (TrySpawnShip(savedShip, out var spawnedShip))
					{
						if (savedShip.waypoints.Count > 0)
						{
							spawnedShip.GetMissionWaypoints(savedShip);
						}
						return Success<Ship, Unit>(spawnedShip, out unit);
					}
					return Fail<Unit>(out unit);
				}
				if (TrySpawnVehicle(savedVehicle, out var spawnedVehicle))
				{
					if (savedVehicle.waypoints.Count > 0)
					{
						spawnedVehicle.GetMissionWaypoints(savedVehicle);
					}
					return Success<GroundVehicle, Unit>(spawnedVehicle, out unit);
				}
				return Fail<Unit>(out unit);
			}
			if (savedAircraft.playerControlled && GameManager.gameState != GameState.Editor)
			{
				pendingPlayerControlled.Enqueue(savedAircraft);
				return Fail<Unit>(out unit);
			}
			Aircraft spawnedAircraft;
			return Result<Aircraft, Unit>(TrySpawnAircraft(savedAircraft, out spawnedAircraft), spawnedAircraft, out unit);
		}
		ColorLog<Spawner>.LogError("Unit Type '" + savedUnit.type + "' not found in Encyclopedia");
		return Fail<Unit>(out unit);
	}

	public void SpawnFromMissionInEditor(Mission mission, Action<Unit, SavedUnit> registerEditorUnit)
	{
		if (mission == null)
		{
			return;
		}
		foreach (SavedScenery item in mission.scenery)
		{
			FindOrSpawn<SavedScenery, Scenery>(item, TrySpawnScenery);
		}
		foreach (SavedContainer container in mission.containers)
		{
			FindOrSpawn<SavedContainer, Container>(container, TrySpawnContainer);
		}
		foreach (SavedMissile missile in mission.missiles)
		{
			FindOrSpawn<SavedMissile, Missile>(missile, TrySpawnMissile);
		}
		foreach (SavedPilot pilot in mission.pilots)
		{
			FindOrSpawn<SavedPilot, PilotDismounted>(pilot, TrySpawnPilot);
		}
		foreach (SavedBuilding building in mission.buildings)
		{
			FindOrSpawn<SavedBuilding, Building>(building, TrySpawnBuilding);
		}
		foreach (SavedShip ship in mission.ships)
		{
			FindOrSpawn<SavedShip, Ship>(ship, TrySpawnShip);
		}
		foreach (SavedVehicle vehicle in mission.vehicles)
		{
			FindOrSpawn<SavedVehicle, GroundVehicle>(vehicle, TrySpawnVehicle);
		}
		foreach (SavedAircraft item2 in mission.aircraft)
		{
			FindOrSpawn<SavedAircraft, Aircraft>(item2, TrySpawnAircraft);
		}
		Physics.SyncTransforms();
		void FindOrSpawn<TSaved, TUnit>(TSaved saved, TrySpawnAction<TSaved, TUnit> trySpawnAction) where TSaved : SavedUnit where TUnit : Unit
		{
			TUnit unit;
			if (UnitRegistry.customIDLookup.TryGetValue(saved.UniqueName, out var value))
			{
				registerEditorUnit(value, saved);
			}
			else if (trySpawnAction(saved, out unit))
			{
				registerEditorUnit(unit, saved);
			}
		}
	}

	public Unit SpawnFromUnitDefinitionInEditor(UnitDefinition placingDefinition, GlobalPosition position, Quaternion rotation, FactionHQ factionHq, string uniqueName)
	{
		if (!(placingDefinition is VehicleDefinition vehicleDefinition))
		{
			if (!(placingDefinition is ShipDefinition shipDefinition))
			{
				if (!(placingDefinition is BuildingDefinition buildingDefinition))
				{
					if (!(placingDefinition is AircraftDefinition aircraftDefinition))
					{
						if (!(placingDefinition is SceneryDefinition sceneryDefinition))
						{
							if (!(placingDefinition is MissileDefinition missileDefinition))
							{
								if ((object)placingDefinition != null)
								{
									if (placingDefinition.code == "PILOT")
									{
										return SpawnPilot(placingDefinition.unitPrefab, position, rotation, factionHq, uniqueName);
									}
									return SpawnContainer(placingDefinition.unitPrefab, position, rotation, factionHq, uniqueName);
								}
								throw new ArgumentException($"Can't spawn object with unity type: {placingDefinition?.GetType()}");
							}
							return SpawnSavedMissile(missileDefinition.unitPrefab, position, rotation, factionHq, "", "", Vector3.zero, uniqueName);
						}
						return SpawnScenery(sceneryDefinition.unitPrefab, position, rotation, uniqueName);
					}
					return SpawnAircraft(null, aircraftDefinition.unitPrefab, null, 1f, default(LiveryKey), position, rotation, Vector3.zero, null, factionHq, uniqueName, 1f, 0.5f);
				}
				return SpawnBuilding(buildingDefinition.unitPrefab, position, rotation, factionHq, null, uniqueName, capturable: false, null);
			}
			return SpawnShip(shipDefinition.unitPrefab, position, rotation, factionHq, uniqueName, 1f, holdPosition: false);
		}
		return SpawnVehicle(vehicleDefinition.unitPrefab, position, rotation, Vector3.zero, factionHq, uniqueName, 1f, holdPosition: false, null);
	}

	public bool TrySpawnVehicle(SavedVehicle savedVehicle, out GroundVehicle spawnedVehicle)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedVehicle.type, out var prefab))
		{
			return Fail<GroundVehicle>(out spawnedVehicle);
		}
		//spawnedVehicle = SpawnVehicle(prefab, savedVehicle.globalPosition, savedVehicle.rotation, Vector3.zero, FactionRegistry.HqFromName(savedVehicle.faction), savedVehicle.UniqueName, savedVehicle.skill, savedVehicle.holdPosition, null);
		spawnedVehicle = null;
		return true;
	}

	public GroundVehicle SpawnVehicle(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, Vector3 velocity, FactionHQ hq, string uniqueName, float skill, bool holdPosition, Player player)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.position = globalPosition.ToLocalPosition();
		gameObject.transform.rotation = rotation;
		GroundVehicle component = gameObject.GetComponent<GroundVehicle>();
		component.rb.MovePosition(globalPosition.ToLocalPosition());
		component.rb.MoveRotation(rotation);
		component.rb.velocity = velocity;
		component.Networkowner = player;
		component.NetworkHQ = hq;
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkunitName = component.definition.unitName;
		component.skill = skill;
		if (hq != null)
		{
			component.skill *= hq.surfaceSkillMultiplier;
		}
		component.SetHoldPosition(holdPosition);
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	public bool TrySpawnShip(SavedShip savedShip, out Ship spawnedShip)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedShip.type, out var prefab))
		{
			return Fail<Ship>(out spawnedShip);
		}
		//spawnedShip = SpawnShip(prefab, savedShip.globalPosition, savedShip.rotation, FactionRegistry.HqFromName(savedShip.faction), savedShip.UniqueName, savedShip.skill, savedShip.holdPosition);
		spawnedShip = null;
		return true;
	}

	public Ship SpawnShip(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, FactionHQ HQ, string uniqueName, float skill, bool holdPosition)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.position = globalPosition.ToLocalPosition();
		gameObject.transform.rotation = rotation;
		Ship component = gameObject.GetComponent<Ship>();
		component.NetworkHQ = HQ;
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkunitName = component.definition.unitName;
		component.skill = skill;
		if (HQ != null)
		{
			component.skill *= HQ.surfaceSkillMultiplier;
		}
		component.SetHoldPosition(holdPosition);
		if (component.TryGetComponent<Airbase>(out var component2))
		{
			component2.SetupAttachedAirbase(component);
		}
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	private bool TrySpawnAircraft(SavedAircraft savedAircraft, out Aircraft spawnedAircraft)
	{
		return TrySpawnAircraft(savedAircraft, null, out spawnedAircraft);
	}

	private bool TrySpawnAircraft(SavedAircraft savedAircraft, Player player, out Aircraft spawnedAircraft)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedAircraft.type, out var prefab))
		{
			return Fail<Aircraft>(out spawnedAircraft);
		}
		spawnedAircraft = SpawnAircraft(player, prefab, savedAircraft.savedLoadout.CreateLoadout(prefab), savedAircraft.fuel, savedAircraft.liveryKey, savedAircraft.globalPosition, savedAircraft.rotation, savedAircraft.startingSpeed * (savedAircraft.rotation * Vector3.forward), null, FactionRegistry.HqFromName(savedAircraft.faction), savedAircraft.UniqueName, savedAircraft.skill, savedAircraft.bravery);
		return true;
	}

	public Aircraft SpawnAircraft(Player player, GameObject prefab, Loadout loadout, float fuelLevel, LiveryKey livery, GlobalPosition globalPosition, Quaternion rotation, Vector3 startingVel, Hangar spawningHangar, FactionHQ HQ, string uniqueName, float skill, float bravery)
	{
		PlayerRef networkplayerRef = ((player != null) ? player.PlayerRef : PlayerRef.Invalid);
		Vector3 position = globalPosition.ToLocalPosition();
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab, position, rotation);
		Aircraft component = gameObject.GetComponent<Aircraft>();
		component.NetworkHQ = HQ;
		component.NetworkUniqueName = uniqueName;
		component.NetworkspawningHangar = spawningHangar;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkstartingVelocity = startingVel;
		component.Networkloadout = loadout;
		component.NetworkfuelLevel = Mathf.Clamp01(fuelLevel);
		component.skill = skill;
		if (HQ != null)
		{
			component.skill *= HQ.airSkillMultiplier;
		}
		component.bravery = bravery;
		component.SetLiveryKey(livery);
		component.NetworkplayerRef = networkplayerRef;
		component.NetworkunitName = component.definition.unitName;
		if (player != null)
		{
			base.ServerObjectManager.Spawn(gameObject, player.Owner);
			return component;
		}
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	private bool TrySpawnBuilding(SavedBuilding savedBuilding, out Building spawnedBuilding)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedBuilding.type, out var prefab))
		{
			return Fail<Building>(out spawnedBuilding);
		}
		Airbase airbase = savedBuilding.AirbaseRef?.Airbase;
		spawnedBuilding = SpawnBuilding(prefab, savedBuilding.globalPosition, savedBuilding.rotation, FactionRegistry.HqFromName(savedBuilding.faction), airbase, savedBuilding.UniqueName, savedBuilding.capturable, savedBuilding.factoryOptions);
		return true;
	}

	public Building SpawnBuilding(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, FactionHQ HQ, Airbase airbase, string uniqueName, bool capturable, SavedBuilding.FactoryOptions factoryOptions)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.SetPositionAndRotation(globalPosition.ToLocalPosition(), rotation);
		Building component = gameObject.GetComponent<Building>();
		component.NetworkHQ = HQ;
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkunitName = component.definition.unitName;
		component.capturable = capturable;
		if (airbase != null)
		{
			component.SetAirbase(airbase);
		}
		if (factoryOptions != null && component.TryGetComponent<Factory>(out var component2))
		{
			component2.SetFactory(factoryOptions);
		}
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	private bool TrySpawnScenery(SavedScenery savedScenery, out Scenery spawnedScenery)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedScenery.type, out var prefab))
		{
			return Fail<Scenery>(out spawnedScenery);
		}
		spawnedScenery = SpawnScenery(prefab, savedScenery.globalPosition, savedScenery.rotation, savedScenery.UniqueName);
		return true;
	}

	public Scenery SpawnScenery(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, string uniqueName)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.SetPositionAndRotation(globalPosition.ToLocalPosition(), rotation);
		Scenery component = gameObject.GetComponent<Scenery>();
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkunitName = component.definition.unitName;
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	private bool TrySpawnContainer(SavedContainer savedContainer, out Container spawnedContainer)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedContainer.type, out var prefab))
		{
			return Fail<Container>(out spawnedContainer);
		}
		spawnedContainer = SpawnContainer(prefab, savedContainer.globalPosition, savedContainer.rotation, FactionRegistry.HqFromName(savedContainer.faction), savedContainer.UniqueName);
		return true;
	}

	public Container SpawnContainer(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, FactionHQ hq, string uniqueName)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.SetPositionAndRotation(globalPosition.ToLocalPosition(), rotation);
		Container component = gameObject.GetComponent<Container>();
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkHQ = hq;
		component.NetworkunitName = component.definition.unitName;
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	private bool TrySpawnPilot(SavedPilot savedPilot, out PilotDismounted spawnedPilot)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedPilot.type, out var prefab))
		{
			return Fail<PilotDismounted>(out spawnedPilot);
		}
		spawnedPilot = SpawnPilot(prefab, savedPilot.globalPosition, savedPilot.rotation, FactionRegistry.HqFromName(savedPilot.faction), savedPilot.UniqueName);
		return true;
	}

	public PilotDismounted SpawnPilot(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, FactionHQ hq, string uniqueName)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.SetPositionAndRotation(globalPosition.ToLocalPosition(), rotation);
		PilotDismounted component = gameObject.GetComponent<PilotDismounted>();
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkHQ = hq;
		component.NetworkunitName = component.definition.unitName;
		component.SetCollidable(enabled: true);
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	private bool TrySpawnMissile(SavedMissile savedMissile, out Missile spawnedMissile)
	{
		if (!Encyclopedia.i.TryGetPrefab(savedMissile.type, out var prefab))
		{
			return Fail<Missile>(out spawnedMissile);
		}
		spawnedMissile = SpawnSavedMissile(prefab, savedMissile.globalPosition, savedMissile.rotation, FactionRegistry.HqFromName(savedMissile.faction), savedMissile.targetUnitName, savedMissile.guidingUnit, savedMissile.startingSpeed * (savedMissile.rotation * Vector3.forward), savedMissile.UniqueName);
		return true;
	}

	public Missile SpawnSavedMissile(GameObject prefab, GlobalPosition globalPosition, Quaternion rotation, FactionHQ hq, string targetName, string guidingUnitName, Vector3 startingVel, string uniqueName)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		gameObject.transform.SetPositionAndRotation(globalPosition.ToLocalPosition(), rotation);
		Missile component = gameObject.GetComponent<Missile>();
		component.NetworkUniqueName = uniqueName;
		component.NetworkstartPosition = globalPosition;
		component.NetworkstartRotation = rotation;
		component.NetworkstartingVelocity = startingVel;
		component.NetworkunitName = component.definition.unitName;
		component.NetworkownerID = PersistentID.None;
		component.NetworkHQ = hq;
		if (targetName != "")
		{
			Unit unit = UnitRegistry.allUnits.Find((Unit x) => x.UniqueName == targetName);
			if (!(unit == null))
			{
				component.SetTarget(unit);
			}
		}
		if (guidingUnitName != "")
		{
			Unit unit2 = UnitRegistry.allUnits.Find((Unit x) => x.UniqueName == guidingUnitName);
			if (!(unit2 == null))
			{
				component.NetworkownerID = unit2.persistentID;
				component.NetworkstartOffsetFromOwner = component.transform.position - unit2.transform.position;
			}
		}
		gameObject.GetComponent<Rigidbody>().velocity = startingVel;
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	[Server]
	public Missile SpawnMissileEncyclopedia(MissileDefinition missile, Transform spawnTransform)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'SpawnMissileEncyclopedia' called when server not active");
		}
		Vector3 position = spawnTransform.position + missile.spawnOffset.y * Vector3.up;
		GameObject gameObject = UnityEngine.Object.Instantiate(missile.unitPrefab, position, spawnTransform.rotation);
		Missile component = gameObject.GetComponent<Missile>();
		component.NetworkunitName = component.definition.unitName;
		component.NetworkstartPosition = position.ToGlobalPosition();
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	[Server]
	public Missile SpawnMissile(MissileDefinition missile, Vector3 launchPosition, Quaternion rotation, Vector3 velocity, Unit target, Unit owner)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'SpawnMissile' called when server not active");
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(missile.unitPrefab, launchPosition, rotation);
		gameObject.GetComponent<Rigidbody>().velocity = velocity;
		Missile component = gameObject.GetComponent<Missile>();
		component.NetworkHQ = ((owner != null) ? owner.NetworkHQ : null);
		component.NetworkunitName = component.definition.unitName;
		component.NetworkownerID = ((owner != null) ? owner.persistentID : PersistentID.None);
		component.SetTarget(target);
		component.NetworkstartPosition = launchPosition.ToGlobalPosition();
		component.NetworkstartOffsetFromOwner = launchPosition - owner.transform.position;
		component.NetworkstartingVelocity = velocity;
		component.NetworkstartRotation = rotation;
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	[Server]
	public Missile SpawnMissile(GameObject missile, Vector3 launchPosition, Quaternion rotation, Vector3 velocity, Unit target, Unit owner)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'SpawnMissile' called when server not active");
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(missile, launchPosition, rotation);
		gameObject.GetComponent<Rigidbody>().velocity = velocity;
		Missile component = gameObject.GetComponent<Missile>();
		component.NetworkHQ = ((owner != null) ? owner.NetworkHQ : null);
		component.NetworkunitName = component.definition.unitName;
		component.NetworkownerID = ((owner != null) ? owner.persistentID : PersistentID.None);
		component.SetTarget(target);
		component.NetworkstartPosition = launchPosition.ToGlobalPosition();
		component.NetworkstartOffsetFromOwner = launchPosition - owner.transform.position;
		component.NetworkstartingVelocity = velocity;
		component.NetworkstartRotation = rotation;
		base.ServerObjectManager.Spawn(gameObject);
		return component;
	}

	[Server]
	public Unit SpawnUnit(UnitDefinition unit, Vector3 spawnPosition, Quaternion rotation, Vector3 velocity, Unit owner, Player player)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'SpawnUnit' called when server not active");
		}
		if (unit is VehicleDefinition)
		{
			return SpawnVehicle(unit.unitPrefab, spawnPosition.ToGlobalPosition(), rotation, velocity, owner.NetworkHQ, unit.unitName, 1f, holdPosition: false, player);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(unit.unitPrefab, spawnPosition, rotation);
		Rigidbody component = gameObject.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.velocity = velocity;
		}
		Unit component2 = gameObject.GetComponent<Unit>();
		component2.NetworkHQ = ((owner != null) ? owner.NetworkHQ : null);
		component2.NetworkunitName = component2.definition.unitName;
		component2.NetworkstartPosition = spawnPosition.ToGlobalPosition();
		component2.NetworkstartRotation = rotation;
		if (component2 is Container container)
		{
			container.NetworkownerID = owner.persistentID;
		}
		base.ServerObjectManager.Spawn(gameObject);
		return component2;
	}

	public async UniTask<bool> RequestSpawnAtAirbase(Airbase airbase, AircraftDefinition definition, LiveryKey livery, Loadout playerLoadout, float fuelLevel)
	{
		if (requestSpawnInProgress)
		{
			throw new InvalidOperationException("Spawn request already pending");
		}
		if (!GameManager.GetLocalPlayer<Player>(out var player))
		{
			Debug.LogError("No Local player, can't request spawn");
			return false;
		}
		if (fuelLevel < 0f || fuelLevel > 1f)
		{
			Debug.LogError("Fuel should be between 0 and 1");
			return false;
		}
		if (!AllowedToSpawn(airbase, definition, playerLoadout, player))
		{
			return false;
		}
		bool delaySpawn = false;
		requestSpawnInProgress = true;
		try
		{
			player.SetSpawnPending(pending: true);
			Airbase.TrySpawnResult trySpawnResult = await CmdRequestSpawnAircraft(airbase, definition, livery, playerLoadout, fuelLevel);
			delaySpawn = trySpawnResult.DelayedSpawn;
			if (trySpawnResult.Allowed)
			{
				Hangar hangar = trySpawnResult.Hangar;
				if (hangar != null)
				{
					hangar.CheckAttachCamera();
				}
				return true;
			}
			return false;
		}
		finally
		{
			requestSpawnInProgress = false;
			if (!delaySpawn)
			{
				player.SetSpawnPending(pending: false);
			}
		}
	}

	private bool AllowedToSpawn(Airbase airbase, AircraftDefinition definition, Loadout playerLoadout, Player player)
	{
		/*
		if (player.HQ == null)
		{
			ColorLog<Spawner>.InfoWarn("Client Spawn Request failed: player not part of faction");
			return false;
		}
		if (player.HQ != airbase.CurrentHQ)
		{
			ColorLog<Spawner>.InfoWarn($"Client Spawn Request failed: player HQ ({player.HQ}) was not the same as airbase HQ ({airbase.CurrentHQ})");
			return false;
		}
		if (airbase.CurrentHQ.restrictedAircraft.Contains(definition.jsonKey))
		{
			ColorLog<Spawner>.InfoWarn($"Client Spawn Request failed: {definition.unitName} not allowed {airbase}");
			return false;
		}
		if (!player.OwnsAirframe(definition, includeReserved: true))
		{
			ColorLog<Spawner>.InfoWarn($"Client Spawn Request failed: player with ID {player.SteamID} doesn't own {definition.unitName}");
			return false;
		}*/
		return true;
	}

	[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
	[ServerRpc(requireAuthority = false)]
	public UniTask<Airbase.TrySpawnResult> CmdRequestSpawnAircraft(Airbase airbase, AircraftDefinition definition, LiveryKey livery, Loadout loadout, float fuelAmount, INetworkPlayer sender = null)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			return UserCode_CmdRequestSpawnAircraft__002D421181233(airbase, definition, livery, loadout, fuelAmount, base.Server.LocalPlayer);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Airbase(writer, airbase);
		writer.WriteAircraftDefinition(definition);
		GeneratedNetworkCode._Write_LiveryKey(writer, livery);
		GeneratedNetworkCode._Write_NuclearOption_002ESavedMission_002ELoadout(writer, loadout);
		writer.WriteSingleConverter(fuelAmount);
		UniTask<Airbase.TrySpawnResult> result = ServerRpcSender.SendWithReturn<Airbase.TrySpawnResult>(this, 1, writer, requireAuthority: false);
		writer.Release();
		return result;
	}

	private Airbase.TrySpawnResult TrySpawnAircraft(Airbase airbase, AircraftDefinition definition, LiveryKey livery, Loadout loadout, float fuelLevel, INetworkPlayer sender)
	{
		if (airbase == null)
		{
			return default(Airbase.TrySpawnResult);
		}
		if (definition == null)
		{
			return default(Airbase.TrySpawnResult);
		}
		if (!NetworkFloatHelper.Validate(fuelLevel, logErrors: false, null))
		{
			return default(Airbase.TrySpawnResult);
		}
		if (fuelLevel < 0f || fuelLevel > 1f)
		{
			sender.SetError(1, PlayerErrorFlags.LikelyCheater);
			return default(Airbase.TrySpawnResult);
		}
		if (!sender.TryGetPlayer<Player>(out var player))
		{
			Debug.LogError("Sender did not have player object");
			return default(Airbase.TrySpawnResult);
		}
		if (livery.Type == LiveryKey.KeyType.AppData && (string.IsNullOrEmpty(livery.AppDataName) || livery.AppDataName.Contains('/') || livery.AppDataName.Contains('\\') || livery.AppDataName.Contains("..")))
		{
			ColorLog<Spawner>.InfoWarn($"Blocked spawn request from player '{player.SteamID}' due to invalid AppDataName traversal pattern: '{livery.AppDataName}'");
			sender.SetError(10, PlayerErrorFlags.LikelyCheater);
			return default(Airbase.TrySpawnResult);
		}
		if (!AllowedToSpawn(airbase, definition, loadout, player))
		{
			return default(Airbase.TrySpawnResult);
		}
		WeaponChecker.VetLoadout(definition, loadout, player, airbase, sender);
		return airbase.TrySpawnAircraft(player, definition, livery, loadout, fuelLevel);
	}

	private static bool Result<TIn, TOut>(bool success, TIn result, out TOut unit) where TIn : TOut where TOut : Unit
	{
		unit = (TOut)(success ? result : null);
		return success;
	}

	private static bool Success<TIn, TOut>(TIn result, out TOut unit) where TIn : TOut where TOut : Unit
	{
		unit = (TOut)result;
		return true;
	}

	private static bool Fail<T>(out T unit) where T : Unit
	{
		unit = null;
		return false;
	}

	private void MirageProcessed()
	{
	}

	public void UserCode_RpcOnGivenAircraftControl_2052851100(INetworkPlayer _)
	{
		SceneSingleton<GameplayUI>.i.HideSelectAirbase();
	}

	protected static void Skeleton_RpcOnGivenAircraftControl_2052851100(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Spawner)behaviour).UserCode_RpcOnGivenAircraftControl_2052851100(behaviour.Client.Player);
	}

	public UniTask<Airbase.TrySpawnResult> UserCode_CmdRequestSpawnAircraft__002D421181233(Airbase airbase, AircraftDefinition definition, LiveryKey livery, Loadout loadout, float fuelAmount, INetworkPlayer sender)
	{
		Airbase.TrySpawnResult value = default(Airbase.TrySpawnResult);
		try
		{
			value = TrySpawnAircraft(airbase, definition, livery, loadout, fuelAmount, sender);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		return UniTask.FromResult(value);
	}

	protected static UniTask<Airbase.TrySpawnResult> Skeleton_CmdRequestSpawnAircraft__002D421181233(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		return ((Spawner)behaviour).UserCode_CmdRequestSpawnAircraft__002D421181233(GeneratedNetworkCode._Read_Airbase(reader), reader.ReadAircraftDefinition(), GeneratedNetworkCode._Read_LiveryKey(reader), GeneratedNetworkCode._Read_NuclearOption_002ESavedMission_002ELoadout(reader), reader.ReadSingleConverter(), senderConnection);
	}

	protected override int GetRpcCount()
	{
		return 2;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "Spawner.RpcOnGivenAircraftControl", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcOnGivenAircraftControl_2052851100, RpcRateLimitConfig.Disabled());
		collection.RegisterRequest(1, "Spawner.CmdRequestSpawnAircraft", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdRequestSpawnAircraft__002D421181233, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
	}
}
