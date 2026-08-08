using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.DebugScripts;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using Unity.Profiling;
using UnityEngine;

public class GroundVehicle : Unit, ICommandable
{
	public struct VehicleInputs
	{
		public float throttle;

		public float brake;

		public float steering;
	}

	[Serializable]
	private class DeployablePart
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private float deployRate;

		[SerializeField]
		private Vector3 angleStowed;

		[SerializeField]
		private Vector3 angleDeployed;

		[SerializeField]
		private Vector3 positionStowed;

		[SerializeField]
		private Vector3 positionDeployed;

		[SerializeField]
		private bool disableWhenStowed;

		[SerializeField]
		public bool staysDeployed;

		[SerializeField]
		private Radar radar;

		[SerializeField]
		private Turret turret;

		[SerializeField]
		private Collider collider;

		private float deployedAmount;

		private bool deploying;

		public bool TryAnimate()
		{
			if (transform == null)
			{
				return false;
			}
			if (staysDeployed && deployedAmount == 1f)
			{
				return false;
			}
			deployedAmount = Mathf.Clamp01(deployedAmount + (deploying ? deployRate : (0f - deployRate)) * Time.deltaTime);
			transform.localEulerAngles = Vector3.Lerp(angleStowed, angleDeployed, deployedAmount);
			transform.localPosition = Vector3.Lerp(positionStowed, positionDeployed, deployedAmount);
			if (collider != null)
			{
				collider.enabled = deployedAmount == 1f;
			}
			if (deployedAmount == 0f || deployedAmount == 1f)
			{
				return false;
			}
			return true;
		}

		public void StartSequence(bool deploying)
		{
			if (!this.deploying || !staysDeployed)
			{
				this.deploying = deploying;
				if (radar != null)
				{
					radar.enabled = false;
					radar.ResetRotators();
				}
				if (disableWhenStowed)
				{
					transform.gameObject.SetActive(deploying);
				}
			}
		}

		public void CompleteSequence(bool deployed)
		{
			if (deployed && radar != null)
			{
				radar.enabled = true;
			}
			if (turret != null)
			{
				turret.SetStowed(!deployed);
			}
		}
	}

	private static readonly ProfilerMarker setArgsFieldsMarker = new ProfilerMarker("GroundVehicle SetArgs_fields");

	private static readonly ProfilerMarker setArgsPathFinderMarker = new ProfilerMarker("GroundVehicle SetArgs_pathfinder");

	private static readonly ProfilerMarker setArgsObstaclesMarker = new ProfilerMarker("GroundVehicle SetArgs_obstacles");

	[Header("Config")]
	[SerializeField]
	private bool mobile = true;

	[SerializeField]
	private float topSpeedOnroad;

	[SerializeField]
	private float topSpeedOffroad;

	[SerializeField]
	private float acceleration;

	[SerializeField]
	private float suspensionTravel;

	[SerializeField]
	private float springRate;

	[SerializeField]
	private float dampingRate;

	[SerializeField]
	private float frictionCoef;

	[Header("AI")]
	[SerializeField]
	private bool holdAtPlayerCommandedDestination = true;

	[SerializeField]
	private bool navigateToObjectives = true;

	[Header("References")]
	[SerializeField]
	private GameObject wreckage;

	[SerializeField]
	private GameObject parachuteSystem;

	[SerializeField]
	private UnitCommand unitCommand;

	[Header("Visuals")]
	[SerializeField]
	private GameObject[] wheels;

	[SerializeField]
	private DeployablePart[] deployableParts;

	[SerializeField]
	private float wheelRadius = 0.3f;

	[SerializeField]
	private Renderer tracks;

	[Header("Sound")]
	[SerializeField]
	private AudioSource engineIdleSound;

	[SerializeField]
	private AudioSource engineDriveSound;

	[SerializeField]
	private float engineMinPitch = 0.5f;

	[SerializeField]
	private float engineMaxPitch = 1f;

	[SerializeField]
	private float enginePitchMult = 5f;

	[SyncVar(initialOnly = true)]
	public NetworkBehaviorSyncvar owner;

	[SyncVar(hook = "OnStationaryChanged")]
	private bool networkStationary;

	private PtrAllocation<GroundVehicleFields> JobFields;

	private JobPart<GroundVehicle, GroundVehicleFields> JobPart;

	[NonSerialized]
	public float skill = 1f;

	private PathfindingAgent pathfinder;

	private readonly List<Obstacle> obstacles = new List<Obstacle>();

	private GlobalPosition destination;

	private bool commandedDestination;

	private bool holdPosition;

	private bool anchored;

	private List<VehicleWaypoint> savedWaypoints = new List<VehicleWaypoint>();

	private Unit unitBelow;

	protected Collider[] colliders;

	private GameObject debugSteerMarker;

	private GameObject debugPlayerCommandClickPosition;

	private GameObject debugPlayerCommandPosition;

	private bool wheelsLocked;

	private bool slung;

	[NonSerialized]
	public Rigidbody hitRB;

	private bool resetStationary;

	private List<GlobalPosition> offroadWaypoints;

	private static int contactDustID = -1;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 11;

	[NonSerialized]
	private const int RPC_COUNT = 22;

	public UnitCommand UnitCommand => unitCommand;

	bool ICommandable.Disabled => disabled;

	FactionHQ ICommandable.HQ => base.NetworkHQ;

	public Player Networkowner
	{
		get
		{
			return (Player)owner.Value;
		}
		set
		{
			owner.Value = value;
		}
	}

	public bool NetworknetworkStationary
	{
		get
		{
			return networkStationary;
		}
		set
		{
			if (!SyncVarEqual(value, networkStationary))
			{
				bool flag = networkStationary;
				networkStationary = value;
				SetDirtyBit(1024uL);
				if (!GetSyncVarHookGuard(1024uL) && base.IsHost)
				{
					SetSyncVarHookGuard(1024uL, value: true);
					OnStationaryChanged(value);
					SetSyncVarHookGuard(1024uL, value: false);
				}
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		base.Identity.OnStartClient.AddListener(OnStartClient);
		base.Identity.OnStartServer.AddListener(OnStartServer);
		pathfinder = new PathfindingAgent(this);
	}

	private void OnStartServer()
	{
		unitCommand.ProcessSetDestination += UnitCommand_ProcessSetDestination;
		JobPart = new JobPart<GroundVehicle, GroundVehicleFields>(this, GetOrCreateJobField());
		JobManager.Add(JobPart);
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			NetworknetworkStationary = true;
		}
		if (holdPosition && Vector3.Dot(base.transform.up, Vector3.up) < 0.5f && TryAttachToSurface())
		{
			NetworknetworkStationary = true;
		}
	}

	public void LeaveRoad(float maxDistance)
	{
		if (offroadWaypoints == null)
		{
			offroadWaypoints = new List<GlobalPosition>();
		}
		offroadWaypoints.Clear();
		NavigateOffRoad(maxDistance).Forget();
	}

	public void ReturnToRoad(GlobalPosition? newDestination)
	{
		NavigateBackToRoad(newDestination).Forget();
	}

	private async UniTask NavigateOffRoad(float maxDistance)
	{
		pathfinder.ClearDestination();
		GlobalPosition startPoint = this.GlobalPosition() + base.transform.forward * (10f + speed * speed * 0.1f);
		if (Physics.Linecast(startPoint.ToLocalPosition() + Vector3.up * 20f, startPoint.ToLocalPosition() - Vector3.up * 20f, out var hitInfo, PhysicsLayers.StaticsMask))
		{
			startPoint = hitInfo.point.ToGlobalPosition();
		}
		if (Physics.Linecast(base.transform.position, startPoint.ToLocalPosition() + Vector3.up, PhysicsLayers.StaticsMask))
		{
			return;
		}
		pathfinder.AddWaypoint(startPoint);
		offroadWaypoints.Add(startPoint);
		Vector3 roadDirection = base.transform.forward;
		GlobalPosition exploredPoint = startPoint;
		CancellationToken cancel = base.destroyCancellationToken;
		int attempt = 0;
		int waypointsAdded = 0;
		Vector3 perpendicularDirection = Vector3.Cross(base.transform.forward, Vector3.up);
		perpendicularDirection *= (float)((!(UnityEngine.Random.value > 0.5f)) ? 1 : (-1));
		while (attempt < 5)
		{
			await UniTask.WaitForSeconds(0.5f);
			if (cancel.IsCancellationRequested)
			{
				break;
			}
			if (attempt == 3)
			{
				perpendicularDirection *= -1f;
			}
			if ((waypointsAdded <= 0 || attempt <= 2) && !FastMath.OutOfRange(startPoint, exploredPoint, maxDistance))
			{
				Vector3 vector = perpendicularDirection;
				switch (attempt)
				{
				case 0:
					vector = perpendicularDirection;
					break;
				case 1:
					vector = perpendicularDirection * 0.7f + roadDirection * 0.7f;
					break;
				case 2:
					vector = perpendicularDirection * 0.7f - roadDirection * 0.7f;
					break;
				case 3:
					vector = perpendicularDirection;
					break;
				case 4:
					vector = perpendicularDirection * 0.7f + roadDirection * 0.7f;
					break;
				case 5:
					vector = perpendicularDirection * 0.7f - roadDirection * 0.7f;
					break;
				}
				Vector3 vector2 = exploredPoint.ToLocalPosition() + vector * 20f;
				if (Physics.Linecast(vector2 + Vector3.up * 20f, vector2 - Vector3.up * 20f, out var hitInfo2, PhysicsLayers.StaticsMask) && !Physics.Linecast(exploredPoint.ToLocalPosition() + Vector3.up, hitInfo2.point + Vector3.up) && Vector3.Dot(hitInfo2.normal, Vector3.up) > 0.9f && hitInfo2.point.y > Datum.LocalSeaY && Mathf.Abs(hitInfo2.point.y - exploredPoint.ToLocalPosition().y) < 3f)
				{
					exploredPoint = hitInfo2.point.ToGlobalPosition();
					pathfinder.AddWaypoint(exploredPoint);
					offroadWaypoints.Add(exploredPoint);
					waypointsAdded++;
					attempt = 0;
				}
				else
				{
					attempt++;
				}
				continue;
			}
			break;
		}
	}

	private async UniTask NavigateBackToRoad(GlobalPosition? newDestination)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		pathfinder.ClearDestination();
		if (offroadWaypoints == null || offroadWaypoints.Count == 0)
		{
			if (newDestination.HasValue)
			{
				unitCommand.SetDestination(newDestination.Value, playerCommand: false);
			}
			return;
		}
		offroadWaypoints.Reverse();
		pathfinder.AddWaypoints(offroadWaypoints);
		while (offroadWaypoints.Count > 0)
		{
			GlobalPosition a = this.GlobalPosition();
			List<GlobalPosition> list = offroadWaypoints;
			if (!FastMath.OutOfRange(a, list[list.Count - 1], 20f))
			{
				break;
			}
			await UniTask.WaitForSeconds(1);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		pathfinder.ClearDestination();
		offroadWaypoints.Clear();
		if (newDestination.HasValue)
		{
			unitCommand.SetDestination(newDestination.Value, playerCommand: false);
		}
	}

	public void StopImmediately()
	{
		pathfinder.ClearDestination();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		JobManager.Remove(ref JobPart);
		DisposeJobFields(ref JobFields);
		MissionManager.onObjectiveStarted -= Vehicle_OnObjectiveStarted;
		if (DebugVis.Enabled)
		{
			if (debugPlayerCommandClickPosition != null)
			{
				UnityEngine.Object.Destroy(debugPlayerCommandClickPosition);
			}
			if (debugPlayerCommandPosition != null)
			{
				UnityEngine.Object.Destroy(debugPlayerCommandPosition);
			}
		}
	}

	public void GetMissionWaypoints(SavedVehicle savedVehicle)
	{
		savedWaypoints = new List<VehicleWaypoint>(savedVehicle.waypoints);
		MissionManager.onObjectiveStarted += Vehicle_OnObjectiveStarted;
		if (savedWaypoints[0].objective == "Unit Spawn")
		{
			unitCommand.SetDestination(savedWaypoints[0].position, playerCommand: false);
			savedWaypoints.RemoveAt(0);
		}
	}

	[Server]
	private bool TryAttachToSurface()
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'TryAttachToSurface' called when server not active");
		}
		base.transform.GetPositionAndRotation(out var position, out var rotation);
		if (Physics.Raycast(new Ray(position, rotation * Vector3.down), out hit, 10f, PhysicsLayers.StaticsMask))
		{
			unitBelow = hit.collider.gameObject.GetComponent<Unit>();
			if (unitBelow != null)
			{
				unitBelow.onDisableUnit += Vehicle_OnUnitBelowDestroyed;
			}
			Vector3 rhs = rotation * Vector3.left;
			Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(hit.normal, rhs), hit.normal);
			Vector3 vector = quaternion * Vector3.up;
			Vector3 position2 = hit.point + vector * definition.spawnOffset.y;
			base.transform.SetPositionAndRotation(position2, quaternion);
			base.NetworkstartPosition = position2.ToGlobalPosition();
			anchored = holdPosition;
			return true;
		}
		anchored = false;
		return false;
	}

	public GlobalPosition GetDestination()
	{
		return destination;
	}

	public float GetTopSpeed()
	{
		return topSpeedOnroad;
	}

	public void MoveFromDepot()
	{
		unitCommand.SetDestination(base.transform.GlobalPosition() + base.transform.forward * 60f, playerCommand: false);
	}

	public void SetHoldPosition(bool hold)
	{
		holdPosition = hold;
	}

	public bool GetHoldPosition()
	{
		return holdPosition;
	}

	public override bool IsSlung()
	{
		return slung;
	}

	public void StowTurrets(bool stowed)
	{
		foreach (WeaponStation weaponStation in weaponStations)
		{
			foreach (Turret turret in weaponStation.Turrets)
			{
				if (turret != null)
				{
					turret.SetStowed(stowed);
				}
			}
		}
	}

	[ClientRpc]
	public void RpcDeployFireControl(bool deploy)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcDeployFireControl_549297332(deploy);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBooleanExtension(deploy);
		ClientRpcSender.Send(this, 21, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public override void AttachOrDetachSlingHook(Aircraft aircraft, bool attached)
	{
		base.AttachOrDetachSlingHook(aircraft, attached);
		SetWheelsLocked(attached);
		StowTurrets(attached);
		base.enabled = !attached;
		slung = attached;
		if (base.IsServer)
		{
			resetStationary = true;
			NetworknetworkStationary = false;
			if (aircraft.Player != null)
			{
				Networkowner = aircraft.Player;
			}
			if (!attached && !base.gameObject.TryGetComponent<ImpactDetector>(out var _))
			{
				base.gameObject.AddComponent<ImpactDetector>().SetGLimit(100f);
			}
		}
		if (base.gameObject.TryGetComponent<BoxCollider>(out var component2))
		{
			component2.enabled = attached;
		}
		radarAlt = 100f;
	}

	public void SetWheelsLocked(bool wheelsLocked)
	{
		this.wheelsLocked = wheelsLocked;
	}

	public void OnStationaryChanged(bool nowStationary)
	{
		base.rb.isKinematic = nowStationary;
		if (nowStationary)
		{
			base.rb.interpolation = RigidbodyInterpolation.None;
		}
		DeployParts(nowStationary).Forget();
		if (base.IsServer)
		{
			if (unitBelow != null)
			{
				unitBelow.onDisableUnit -= Vehicle_OnUnitBelowDestroyed;
				unitBelow = null;
			}
			if (nowStationary)
			{
				TryAttachToSurface();
			}
		}
		if (base.IsClient && nowStationary && engineDriveSound != null)
		{
			engineDriveSound.Stop();
			engineIdleSound.Stop();
		}
	}

	private void Vehicle_OnObjectiveStarted(Objective objective)
	{
		for (int num = savedWaypoints.Count - 1; num >= 0; num--)
		{
			if (savedWaypoints[num].objective == objective.SavedObjective.UniqueName)
			{
				unitCommand.SetDestination(savedWaypoints[num].position, playerCommand: false);
				savedWaypoints.RemoveAt(num);
			}
		}
	}

	private void Vehicle_OnUnitBelowDestroyed(Unit unit)
	{
		resetStationary = true;
		unitBelow.onDisableUnit -= Vehicle_OnUnitBelowDestroyed;
		unitBelow = null;
	}

	private void OnStartClient()
	{
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			DeployParts(deployed: true).Forget();
		}
		SetLocalSim(NetworkManagerNuclearOption.i.Server.Active);
		if (base.remoteSim && GameManager.IsLocalPlayer(Networkowner))
		{
			ClientCollisionDelay().Forget();
		}
		base.transform.SetPositionAndRotation(startPosition.ToLocalPosition(), startRotation);
		if (parachuteSystem != null)
		{
			CheckAirdrop().Forget();
		}
		if (engineDriveSound != null)
		{
			RegisterDopplerSound(engineDriveSound);
			RegisterDopplerSound(engineIdleSound);
		}
		RegisterUnit(4f);
		InitializeUnit();
		OnStationaryChanged(networkStationary);
	}

	private async UniTask CheckAirdrop()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.WaitForSeconds(0.5f);
		if (!cancel.IsCancellationRequested && !Physics.Linecast(startPosition.ToLocalPosition(), startPosition.ToLocalPosition() - Vector3.up * 10f, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ExclusionZonesMask))
		{
			UnityEngine.Object.Instantiate(parachuteSystem, base.transform).GetComponent<CargoDeploymentSystem>().Initialize(this);
		}
	}

	public override void InitializeUnit()
	{
		base.InitializeUnit();
		colliders = base.gameObject.GetComponentsInChildren<Collider>();
		destination = base.transform.GlobalPosition();
		if (NetworkManagerNuclearOption.i.Server.Active && GameManager.gameState != GameState.Editor)
		{
			this.StartSlowUpdateDelayed(4f, CheckObstacles);
		}
	}

	public override void SetLocalSim(bool localSim)
	{
		base.SetLocalSim(localSim);
		base.rb.useGravity = localSim;
	}

	private async UniTask ClientCollisionDelay()
	{
		base.transform.gameObject.layer = PhysicsLayers.IgnoreCollisions;
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(5000);
		if (!cancel.IsCancellationRequested)
		{
			base.transform.gameObject.layer = PhysicsLayers.Default;
		}
	}

	private async UniTask DeployParts(bool deployed)
	{
		bool partsMoving = true;
		CancellationToken cancel = base.destroyCancellationToken;
		if (deployableParts.Length == 0 || cancel.IsCancellationRequested)
		{
			return;
		}
		DeployablePart[] array = deployableParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].StartSequence(deployed);
		}
		while (partsMoving)
		{
			partsMoving = false;
			array = deployableParts;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].TryAnimate())
				{
					partsMoving = true;
				}
			}
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		array = deployableParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].CompleteSequence(deployed);
		}
	}

	private void CheckObstacles()
	{
		if (speed > 1f)
		{
			UpdateObstacles();
		}
		if (!holdPosition && navigateToObjectives && !commandedDestination)
		{
			float num = FastMath.Distance(base.transform.GlobalPosition(), destination);
			if (!MissionPosition.TryGetClosestPosition(this, out var lastKnownPosition) && base.NetworkHQ.TryGetNearestGroundEnemy(this.GlobalPosition(), out var nearestUnit))
			{
				lastKnownPosition = nearestUnit.lastKnownPosition;
			}
			float num2 = FastMath.Distance(destination, lastKnownPosition);
			if (destination != lastKnownPosition && !pathfinder.IsOnBridge() && num2 / num > 0.2f)
			{
				unitCommand.SetDestination(lastKnownPosition, playerCommand: false);
			}
		}
		if (commandedDestination && !holdAtPlayerCommandedDestination && FastMath.InRange(this.GlobalPosition(), destination, 50f) && Mathf.Abs(speed) < 1f)
		{
			commandedDestination = false;
		}
	}

	private void UpdateObstacles()
	{
		obstacles.Clear();
		if (!BattlefieldGrid.TryGetGridSquare(base.transform.GlobalPosition(), out var gridSquare))
		{
			return;
		}
		Vector3 position = base.transform.position;
		foreach (Unit unit in gridSquare.units)
		{
			if (!(unit == this) && unit.definition.IsObstacle && FastMath.InRange(unit.transform.position, position, 200f))
			{
				obstacles.Add(new Obstacle(unit.transform, unit.maxRadius * 1.4f, unit.obstacleTop));
			}
		}
		for (int num = gridSquare.obstacles.Count - 1; num >= 0; num--)
		{
			Obstacle item = gridSquare.obstacles[num];
			if (item.Transform == null)
			{
				gridSquare.obstacles.RemoveAt(num);
			}
			else if (FastMath.InRange(item.Transform.position, position, 200f))
			{
				obstacles.Add(item);
			}
		}
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			WreckAndRemove().Forget();
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			NetworknetworkStationary = false;
			MissionManager.onObjectiveStarted -= Vehicle_OnObjectiveStarted;
		}
	}

	private async UniTask WreckAndRemove()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(5000);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		while (!base.rb.isKinematic && base.transform.position.y > Datum.LocalSeaY - 10f && base.rb.velocity.sqrMagnitude > 1f)
		{
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		base.enabled = false;
		SpawnWreckage();
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		Collider[] array = colliders;
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i]);
		}
		if (unitBelow != null)
		{
			unitBelow.onDisableUnit -= Vehicle_OnUnitBelowDestroyed;
			unitBelow = null;
		}
		base.rb.isKinematic = true;
		await UniTask.Delay(5000);
		if (!cancel.IsCancellationRequested && NetworkManagerNuclearOption.i.Server.Active)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void SpawnWreckage()
	{
		if (!BattlefieldGrid.TryGetGridSquare(base.transform.GlobalPosition(), out var gridSquare))
		{
			return;
		}
		foreach (DamageParticles spawnedEffect in spawnedEffects)
		{
			if (spawnedEffect != null)
			{
				spawnedEffect.ParentObjectCulled();
			}
		}
		if (!(wreckage == null))
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(wreckage, base.transform.position, base.transform.rotation);
			gameObject.transform.SetParent((unitBelow != null) ? unitBelow.transform : Datum.origin);
			if (Physics.Linecast(base.transform.position, base.transform.position - base.transform.up * 10f, out hit, PhysicsLayers.StaticsMask))
			{
				gameObject.transform.SetParent(hit.collider.transform);
			}
			gridSquare.obstacles.Add(new Obstacle(gameObject.transform, definition.length, float.MaxValue));
		}
	}

	private void UnitCommand_ProcessSetDestination(ref UnitCommand.Command command)
	{
		if (pathfinder.IsOnBridge())
		{
			return;
		}
		GlobalPosition position = command.position;
		if (command.FromPlayer)
		{
			command.position = pathfinder.GetDryPosition(base.transform.GlobalPosition(), command.position);
		}
		anchored = false;
		destination = command.position;
		pathfinder.Pathfind(NetworkSceneSingleton<LevelInfo>.i.roadNetwork, command.position, null);
		resetStationary = true;
		if (!command.FromPlayer)
		{
			return;
		}
		commandedDestination = true;
		if (!DebugVis.Enabled)
		{
			return;
		}
		if (DebugVis.Create(ref debugPlayerCommandPosition, GameAssets.i.debugArrowGreen))
		{
			debugPlayerCommandPosition.name = $"Debug CommandPosition {base.name} Id={persistentID}";
			debugPlayerCommandPosition.transform.forward = Vector3.down;
			debugPlayerCommandPosition.transform.localScale = new Vector3(10f, 10f, 30f);
		}
		if (DebugVis.Create(ref debugPlayerCommandClickPosition, GameAssets.i.debugArrowGreen))
		{
			debugPlayerCommandClickPosition.name = $"Debug CommandPositionClick {base.name} Id={persistentID}";
			debugPlayerCommandClickPosition.transform.forward = Vector3.down;
			debugPlayerCommandClickPosition.transform.localScale = new Vector3(10f, 10f, 30f);
			if (debugPlayerCommandClickPosition.TryGetComponent<MeshRenderer>(out var component))
			{
				component.material.color = Color.cyan;
			}
		}
		debugPlayerCommandPosition.transform.position = command.position.ToLocalPosition() + new Vector3(0f, 30f, 0f);
		if (position.y == 0f && PathfindingAgent.RaycastTerrain(position, out var raycastHit))
		{
			position.y = raycastHit.point.GlobalY();
		}
		debugPlayerCommandClickPosition.transform.position = position.ToLocalPosition() + new Vector3(0f, 30f, 0f);
	}

	private void Update()
	{
		if (base.IsClientOnly)
		{
			speed = Vector3.Dot(base.rb.velocity, base.transform.forward);
		}
		if (displayDetail < 1f)
		{
			if (engineIdleSound != null && engineIdleSound.isPlaying)
			{
				engineIdleSound.Stop();
			}
			if (engineDriveSound != null && engineDriveSound.isPlaying)
			{
				engineDriveSound.Stop();
			}
		}
		else
		{
			if (engineDriveSound != null && engineIdleSound != null)
			{
				if (!disabled)
				{
					engineDriveSound.pitch = Mathf.Clamp(speed * enginePitchMult / Mathf.Max(topSpeedOnroad, 1f), engineMinPitch, engineMaxPitch);
					if (speed < 3f)
					{
						if (!engineIdleSound.isPlaying)
						{
							engineIdleSound.Play();
							engineIdleSound.time = UnityEngine.Random.Range(0f, engineIdleSound.clip.length);
						}
						if (engineIdleSound.volume < 0.5f)
						{
							engineIdleSound.volume += Time.deltaTime;
						}
						if (engineDriveSound.volume > 0f)
						{
							engineDriveSound.volume -= Time.deltaTime;
						}
						else
						{
							engineDriveSound.Stop();
						}
					}
					else
					{
						if (engineIdleSound.volume > 0f)
						{
							engineIdleSound.volume -= Time.deltaTime;
						}
						else
						{
							engineIdleSound.Stop();
						}
						if (!engineDriveSound.isPlaying)
						{
							engineDriveSound.Play();
							engineDriveSound.time = UnityEngine.Random.Range(0f, engineIdleSound.clip.length);
						}
						if (engineDriveSound.volume < 0.5f)
						{
							engineDriveSound.volume += Time.deltaTime;
						}
						else
						{
							engineDriveSound.volume = 0.5f + engineDriveSound.pitch * 0.5f;
						}
					}
				}
				else
				{
					float num = Time.deltaTime * 0.5f;
					engineDriveSound.volume -= num;
					if (engineDriveSound.pitch - num > 0f)
					{
						engineDriveSound.pitch -= num;
					}
					engineIdleSound.volume -= num;
					if (engineIdleSound.pitch - num > 0f)
					{
						engineIdleSound.pitch -= num;
					}
				}
			}
			AnimateWheels(speed);
		}
		if (DebugVis.Enabled && debugSteerMarker != null && JobFields.IsCreated)
		{
			GroundVehicleFields groundVehicleFields = JobFields.Ref();
			if (groundVehicleFields.steeringInfoNullable.HasValue)
			{
				debugSteerMarker.transform.rotation = Quaternion.LookRotation(groundVehicleFields.steeringInfoNullable.Value.steerVector);
			}
		}
	}

	public void AnimateWheels(float speed)
	{
		if (wheelsLocked)
		{
			return;
		}
		if (tracks != null)
		{
			tracks.material.mainTextureOffset += new Vector2(0f, speed * -0.2f * Time.deltaTime);
		}
		if (wheels.Length != 0)
		{
			Vector3 eulers = new Vector3(speed * 57.29578f * Time.deltaTime / wheelRadius, 0f, 0f);
			for (int i = 0; i < wheels.Length; i++)
			{
				wheels[i].transform.Rotate(eulers, Space.Self);
			}
		}
	}

	public void ContactDustParticles(Vector3 point)
	{
		if (speed > 2f && FastMath.InRange(base.transform.position, SceneSingleton<CameraStateManager>.i.transform.position, 3000f))
		{
			if (contactDustID == -1 && SceneSingleton<ParticleEffectManager>.i != null)
			{
				contactDustID = SceneSingleton<ParticleEffectManager>.i.GetSystemID("ContactDust");
			}
			SceneSingleton<ParticleEffectManager>.i.EmitParticles(contactDustID, 1, point.ToGlobalPosition(), base.rb.velocity, 0f, Mathf.Max(5f - speed * 0.1f, 1f), 0.5f, Mathf.Max(maxRadius * 2f, 3f) + Mathf.Min(speed * 0.05f, 10f), 0.3f, speed * 0.3f, 0.5f, 0.2f);
		}
	}

	private static void DisposeJobFields(ref PtrAllocation<GroundVehicleFields> fields)
	{
		if (fields.IsCreated)
		{
			fields.Ref().ObstaclesArray.Dispose();
		}
		fields.Dispose();
	}

	public Transform GetJobTransforms()
	{
		return base.transform;
	}

	private Ptr<GroundVehicleFields> GetOrCreateJobField()
	{
		if (!JobFields.IsCreated)
		{
			JobsAllocator<GroundVehicleFields>.Allocate(ref JobFields);
			ref GroundVehicleFields reference = ref JobFields.Ref();
			reference.maxRadius = maxRadius;
			reference.acceleration = acceleration;
			base.rb.mass = definition.mass;
			reference.mass = base.rb.mass;
			reference.inertiaTensor = (base.rb.inertiaTensor.x + base.rb.inertiaTensor.y + base.rb.inertiaTensor.z) / 3f;
			reference.mobile = mobile;
			reference.topSpeedOnroad = topSpeedOnroad;
			reference.topSpeedOffroad = topSpeedOffroad;
			reference.suspensionTravel = suspensionTravel;
			reference.dampingRate = dampingRate;
			reference.springRate = springRate;
			reference.frictionCoef = frictionCoef;
		}
		return JobFields;
	}

	unsafe ~GroundVehicle()
	{
		if (JobFields.ptr != null)
		{
			Console.WriteLine("[PtrAllocation] GroundVehicle memory leaked.");
		}
	}

	public void UpdateJobFields()
	{
		ref GroundVehicleFields reference = ref JobFields.Ref();
		reference.monoBehaviourEnabled = base.enabled;
		if (reference.monoBehaviourEnabled)
		{
			reference.velocity = base.rb.velocity;
			reference.angularVelocity = base.rb.angularVelocity;
			reference.unitDisabled = disabled;
			if (resetStationary)
			{
				resetStationary = false;
				reference.stationary = false;
				reference.stationaryTime = 0f;
			}
			reference.DEBUG_VIS = PlayerSettings.debugVis && SceneSingleton<CameraStateManager>.i.followingUnit == this;
		}
	}

	public void UpdateJobFields_Pathfinder()
	{
		ref GroundVehicleFields reference = ref JobFields.Ref();
		if (reference.monoBehaviourEnabled)
		{
			reference.steeringInfoNullable = pathfinder?.GetSteerpoint(base.transform.GlobalPosition(), base.transform.forward, speed, stayOnRoad: false);
			if (PlayerSettings.debugVis && SceneSingleton<CameraStateManager>.i.followingUnit == this && reference.steeringInfoNullable.HasValue && DebugVis.Create(ref debugSteerMarker, GameAssets.i.debugArrowGreen, base.transform))
			{
				debugSteerMarker.transform.localPosition = Vector3.zero;
				debugSteerMarker.transform.localScale = new Vector3(4f, 4f, 12f);
			}
		}
	}

	public void UpdateJobFields_Obstacles()
	{
		ref GroundVehicleFields reference = ref JobFields.Ref();
		if (!reference.monoBehaviourEnabled)
		{
			return;
		}
		ObstacleCopy(ref reference.ObstaclesArray, obstacles);
		if (!PlayerSettings.debugVis || !(SceneSingleton<CameraStateManager>.i.followingUnit == this))
		{
			return;
		}
		foreach (Obstacle obstacle in obstacles)
		{
			if (obstacle.Transform != null)
			{
				GameObject obj = UnityEngine.Object.Instantiate(GameAssets.i.debugSphere, Datum.origin);
				obj.transform.position = obstacle.Transform.position;
				obj.transform.localScale = Vector3.one * (obstacle.Radius * 2f);
				UnityEngine.Object.Destroy(obj, 0.2f);
			}
		}
	}

	public static void ObstacleCopy(ref PtrList<ObstaclePosition> array, List<Obstacle> obstacles)
	{
		int count = obstacles.Count;
		array.EnsureCapacity(count);
		array.Length = count;
		for (int i = 0; i < count; i++)
		{
			Obstacle obstacle = obstacles[i];
			if (obstacle.Transform != null)
			{
				array[i] = new ObstaclePosition
				{
					Position = obstacle.Transform.position,
					Radius = obstacle.Radius,
					Top = obstacle.Top
				};
			}
			else
			{
				array[i] = new ObstaclePosition
				{
					Radius = 0f
				};
			}
		}
	}

	public void ApplyJobFields()
	{
		if (!JobFields.IsCreated)
		{
			return;
		}
		ref GroundVehicleFields reference = ref JobFields.Ref();
		if (reference.monoBehaviourEnabled && base.enabled)
		{
			if (reference.underwater)
			{
				base.Networkdisabled = true;
			}
			reference.ApplyForce(base.rb);
			speed = reference.speed;
			radarAlt = Math.Max(reference.radarAlt - definition.spawnOffset.y, 0f);
			if (!anchored)
			{
				NetworknetworkStationary = reference.stationary;
			}
			if (!networkStationary && Mathf.Abs(speed) < 1f && !disabled && Vector3.Dot(base.transform.up, Vector3.up) < 0.8f)
			{
				Vector3 vector = base.transform.InverseTransformDirection(base.rb.angularVelocity);
				Vector3 vector2 = ((Vector3.Dot(base.transform.right, Vector3.up) > 0f) ? (-base.transform.forward) : base.transform.forward);
				base.rb.AddTorque(vector2 * (0.3f - vector.z), ForceMode.VelocityChange);
			}
		}
	}

	private void MirageProcessed()
	{
	}

	public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
	{
		ulong syncVarDirtyBits = base.SyncVarDirtyBits;
		bool result = base.SerializeSyncVars(writer, initialize);
		if (initialize)
		{
			writer.WriteNetworkBehaviorSyncVar(owner);
			writer.WriteBooleanExtension(networkStationary);
			return true;
		}
		writer.Write((ulong)((long)syncVarDirtyBits >> 9), 2);
		if ((syncVarDirtyBits & 0x400L) != 0L)
		{
			writer.WriteBooleanExtension(networkStationary);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			owner = reader.ReadNetworkBehaviourSyncVar();
			bool value = networkStationary;
			networkStationary = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(value, networkStationary))
			{
				OnStationaryChanged(networkStationary);
			}
			return;
		}
		ulong num = reader.Read(2);
		SetDeserializeMask(num, 9);
		if ((num & 2L) != 0L)
		{
			bool value2 = networkStationary;
			networkStationary = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(value2, networkStationary))
			{
				OnStationaryChanged(networkStationary);
			}
		}
	}

	public void UserCode_RpcDeployFireControl_549297332(bool deploy)
	{
		if (base.gameObject.TryGetComponent<FireControl>(out var component))
		{
			component.DeployOrStowLaunchers(deploy);
		}
	}

	protected static void Skeleton_RpcDeployFireControl_549297332(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((GroundVehicle)behaviour).UserCode_RpcDeployFireControl_549297332(reader.ReadBooleanExtension());
	}

	protected override int GetRpcCount()
	{
		return 22;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(21, "GroundVehicle.RpcDeployFireControl", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcDeployFireControl_549297332, RpcRateLimitConfig.Disabled());
	}
}
