using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

public class Building : Unit, IRepairable
{
	public struct RecentExplosion
	{
		private readonly float time;

		public readonly GlobalPosition globalPosition;

		public readonly float yield;

		public RecentExplosion(GlobalPosition globalPosition, float yield)
		{
			time = Time.timeSinceLevelLoad;
			this.globalPosition = globalPosition;
			this.yield = yield;
		}

		public bool IsRecent()
		{
			return Time.timeSinceLevelLoad - time < 1f;
		}
	}

	[SerializeField]
	private GameObject wreckage;

	[SerializeField]
	private float collapseTime;

	[SerializeField]
	private bool hideOnMap;

	[SerializeField]
	private GameObject repairPrefab;

	[SerializeField]
	protected float repairTime = 1000000f;

	public bool capturable;

	public bool needsRepair;

	private List<GameObject> wreckageSpawn;

	private RecentExplosion recentExplosion;

	private GameObject repairInstance;

	private float timeLastDamaged;

	[NonSerialized]
	[SyncVar(initialOnly = true)]
	internal NetworkBehaviorSyncvar airbase;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 10;

	[NonSerialized]
	private const int RPC_COUNT = 23;

	public Airbase Networkairbase
	{
		get
		{
			return (Airbase)airbase.Value;
		}
		set
		{
			airbase.Value = value;
		}
	}

	public event Action OnRepair;

	public override Airbase GetAirbase()
	{
		return Networkairbase;
	}

	public override void Awake()
	{
		base.Awake();
		base.Identity.OnStartServer.AddListener(OnStartServer);
		base.Identity.OnStartClient.AddListener(OnStartClient);
		base.Identity.OnStopClient.AddListener(OnStopClient);
		if (NetworkManagerNuclearOption.i != null && NetworkManagerNuclearOption.i.Server.Active)
		{
			SetLocalSim(localSim: true);
			base.NetworkunitName = definition.unitName;
			base.NetworkstartPosition = base.transform.position.ToGlobalPosition();
		}
	}

	protected override SavedUnit CreateBuiltInSavedUnit()
	{
		SavedBuilding savedBuilding = new SavedBuilding(MapUniqueName)
		{
			PlacementType = PlacementType.BuiltIn,
			type = definition.jsonKey,
			Unit = this,
			Airbase = ((MapAirbase != null) ? MapAirbase.SavedAirbase.UniqueName : null),
			capturable = capturable
		};
		if (TryGetComponent<Factory>(out var component))
		{
			SavedBuilding savedBuilding2 = savedBuilding;
			if (savedBuilding2.factoryOptions == null)
			{
				savedBuilding2.factoryOptions = new SavedBuilding.FactoryOptions();
			}
			savedBuilding.factoryOptions.productionTime = component.ProductionInterval;
			savedBuilding.factoryOptions.productionType = ((component.ProductionUnit != null) ? component.ProductionUnit.jsonKey : "");
		}
		return savedBuilding;
	}

	protected override void ResetBuiltInSavedUnit(SavedUnit savedUnit)
	{
		base.ResetBuiltInSavedUnit(savedUnit);
		SavedBuilding savedBuilding = (SavedBuilding)savedUnit;
		savedBuilding.capturable = capturable;
		SavedAirbase savedAirbase = ((MapAirbase != null) ? MapAirbase.SavedAirbase : null);
		if (savedBuilding.AirbaseRef != savedAirbase)
		{
			savedBuilding.SetOrRemoveAirbase(savedAirbase);
			if (savedAirbase != null && MapAirbase.MapTower == this && savedAirbase.TowerRef == null && string.IsNullOrEmpty(savedAirbase.Tower))
			{
				savedAirbase.TowerRef = savedBuilding;
				savedAirbase.Tower = savedBuilding.UniqueName;
			}
		}
		if (TryGetComponent<Factory>(out var component))
		{
			SavedBuilding savedBuilding2 = savedBuilding;
			if (savedBuilding2.factoryOptions == null)
			{
				savedBuilding2.factoryOptions = new SavedBuilding.FactoryOptions();
			}
			savedBuilding.factoryOptions.productionTime = component.ProductionInterval;
			savedBuilding.factoryOptions.productionType = ((component.ProductionUnit != null) ? component.ProductionUnit.jsonKey : "");
		}
	}

	private void OnStartServer()
	{
		CheckBuildingsBelow();
	}

	protected virtual void OnStartClient()
	{
		if (base.IsServer && repairTime < 999999f)
		{
			foreach (UnitPart item in partLookup)
			{
				item.onApplyDamage += Building_OnPartDamage;
			}
		}
		base.transform.position = startPosition.ToLocalPosition();
		if (!hideOnMap)
		{
			RegisterUnit(null);
		}
		InitializeUnit();
		CheckUnderground();
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			ClientAddBuildingToAirbase();
			if (capturable)
			{
				CheckForCapture().Forget();
			}
		}
	}

	public void ClientAddBuildingToAirbase()
	{
		if (Networkairbase != null)
		{
			Networkairbase.AddBuilding(this, errorIfAlreadyMember: false);
		}
	}

	public void SetAirbase(Airbase airbase)
	{
		if (airbase == null)
		{
			Debug.LogError("SetAirbase should not be set with null airbase");
		}
		else if (GameManager.gameState != GameState.Editor)
		{
			Networkairbase = airbase;
			if (base.Identity.IsSpawned)
			{
				RpcSetAirbase(airbase);
				airbase.AddBuilding(this, errorIfAlreadyMember: true);
			}
		}
	}

	[ClientRpc(excludeHost = true)]
	private void RpcSetAirbase(Airbase airbase)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Airbase(writer, airbase);
		ClientRpcSender.Send(this, 21, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public bool IsRepairable()
	{
		if (repairTime < 999999f)
		{
			return Time.timeSinceLevelLoad - timeLastDamaged > 30f;
		}
		return false;
	}

	private void OnStopClient()
	{
		if (Networkairbase != null && !base.Identity.IsSceneObject)
		{
			Networkairbase.RemoveBuilding(this);
			if (TryGetComponent<Hangar>(out var component))
			{
				Networkairbase.RemoveHangar(component);
			}
		}
	}

	public void RegisterRecentExplosion(GlobalPosition globalPosition, float yield)
	{
		recentExplosion = new RecentExplosion(globalPosition, yield);
	}

	private void CheckUnderground()
	{
		Physics.SyncTransforms();
		radarAlt = definition.height * 0.5f;
		if (!Physics.Linecast(base.transform.position + Vector3.up * 200f, base.transform.position, out var hitInfo, PhysicsLayers.StaticsMask))
		{
			return;
		}
		if (hitInfo.collider.sharedMaterial == GameAssets.i.terrainMaterial)
		{
			if (Physics.Linecast(hitInfo.point - Vector3.up * 0.1f, base.transform.position, out var hitInfo2, PhysicsLayers.StaticsMask))
			{
				radarAlt = 0f - hitInfo2.distance;
			}
		}
		else if (hitInfo.point.y < Datum.LocalSeaY)
		{
			radarAlt = hitInfo.point.y - Datum.LocalSeaY;
		}
	}

	private void CheckBuildingsBelow()
	{
		Vector3 position = base.transform.position;
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		Collider[] array = componentsInChildren;
		foreach (Collider collider in array)
		{
			if (collider.gameObject.layer != PhysicsLayers.ExclusionZones)
			{
				collider.gameObject.layer = PhysicsLayers.IgnoreRaycast;
			}
		}
		if (Physics.Linecast(position + Vector3.up * 5f, position - Vector3.up * 200f, out var hitInfo, PhysicsLayers.StaticsMask) && hitInfo.collider.gameObject.TryGetComponent<Unit>(out var component) && component != this)
		{
			if (component is Building building)
			{
				building.onDisableUnit += OnBuildingBelowCollapse;
			}
			else if (component is Scenery scenery)
			{
				scenery.onDisableUnit += OnSceneryBelowCollapse;
			}
		}
		array = componentsInChildren;
		foreach (Collider collider2 in array)
		{
			if (collider2.gameObject.layer != PhysicsLayers.ExclusionZones)
			{
				collider2.gameObject.layer = PhysicsLayers.Statics;
			}
		}
	}

	private void OnSceneryBelowCollapse(Unit obj)
	{
		if (!disabled)
		{
			base.Networkdisabled = true;
		}
	}

	private void OnBuildingBelowCollapse(Unit unit)
	{
		if (!disabled)
		{
			ReportKilled();
			base.Networkdisabled = true;
		}
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		if (GameManager.gameState == GameState.Editor)
		{
			return;
		}
		if (repairInstance != null)
		{
			UnityEngine.Object.Destroy(repairInstance);
		}
		MeshRenderer[] componentsInChildren;
		Collider[] componentsInChildren2;
		if (newState)
		{
			if (wreckage != null)
			{
				if (wreckageSpawn == null)
				{
					wreckageSpawn = new List<GameObject>();
				}
				else
				{
					wreckageSpawn.Clear();
				}
				if (Time.timeSinceLevelLoad > 10f)
				{
					wreckageSpawn.Add(UnityEngine.Object.Instantiate(wreckage, base.transform.position, base.transform.rotation, Datum.origin));
					FragmentManager component = wreckageSpawn[0].GetComponent<FragmentManager>();
					componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].enabled = false;
					}
					componentsInChildren2 = base.gameObject.GetComponentsInChildren<Collider>();
					for (int i = 0; i < componentsInChildren2.Length; i++)
					{
						componentsInChildren2[i].enabled = false;
					}
					if (component != null)
					{
						wreckageSpawn.AddRange(component.GetWreckageObjects());
						if (recentExplosion.IsRecent())
						{
							component.ApplyExplosionForce(recentExplosion);
						}
					}
				}
			}
			Collapse().Forget();
		}
		if (newState)
		{
			return;
		}
		foreach (UnitPart item in partLookup)
		{
			item.Repair();
		}
		base.transform.position = startPosition.ToLocalPosition();
		componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = true;
		}
		componentsInChildren2 = base.gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].enabled = true;
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			NetworkSceneSingleton<MessageManager>.i.RpcRepairMessage(persistentID);
		}
		RegisterUnit(null);
		damageCredit = null;
		if (wreckageSpawn != null)
		{
			foreach (GameObject item2 in wreckageSpawn)
			{
				if (item2 != null)
				{
					UnityEngine.Object.Destroy(item2);
				}
			}
			wreckageSpawn.Clear();
		}
		collapseTime = 0f;
	}

	private async UniTask CheckForCapture()
	{
		List<GridSquare> gridSquares = BattlefieldGrid.GetGridSquaresInRange(base.transform.GlobalPosition(), 1000f);
		FactionHQ capturingHQ = null;
		float controlBalance = 0f;
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(UnityEngine.Random.Range(1, 3) * 1000, ignoreTimeScale: true);
		while (!cancel.IsCancellationRequested)
		{
			controlBalance = Mathf.Min(controlBalance, 1f);
			for (int i = 0; i < gridSquares.Count; i++)
			{
				for (int j = 0; j < gridSquares[i].units.Count; j++)
				{
					Unit unit = gridSquares[i].units[j];
					if (!(unit == null) && !unit.disabled && !(unit.NetworkHQ == null) && unit is GroundVehicle && !FastMath.OutOfRange(base.transform.position, unit.transform.position, maxRadius * 2f))
					{
						controlBalance += ((unit.NetworkHQ == base.NetworkHQ) ? 0.1f : (-0.1f));
						if (unit.NetworkHQ != base.NetworkHQ)
						{
							capturingHQ = unit.NetworkHQ;
						}
					}
				}
			}
			if (controlBalance < 0f && base.NetworkHQ == null)
			{
				controlBalance = -1f;
			}
			if (controlBalance <= -1f)
			{
				base.NetworkHQ = capturingHQ;
			}
			await UniTask.Delay(5000, ignoreTimeScale: true);
		}
	}

	private async UniTask Collapse()
	{
		Vector3 velocity = Vector3.zero;
		CancellationToken cancel = base.destroyCancellationToken;
		while (collapseTime > 0f)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
			velocity += Vector3.down * 9.81f * Time.deltaTime;
			collapseTime -= Time.deltaTime;
			base.transform.position += velocity * Time.deltaTime;
		}
		if (repairTime < 999999f)
		{
			base.gameObject.GetComponent<Renderer>().enabled = false;
			Collider[] componentsInChildren = base.gameObject.GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
		}
		else if (NetworkManagerNuclearOption.i.Server.Active)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public float GetRepairPriority(GlobalPosition repairerPosition, float range)
	{
		if (!IsRepairable() || !needsRepair)
		{
			return 0f;
		}
		float num = 0f;
		if (!disabled)
		{
			foreach (UnitPart item in partLookup)
			{
				num += Mathf.Max(item.hitPoints, 0f) * 0.01f;
			}
		}
		num /= (float)partLookup.Count;
		float num2 = FastMath.SquareDistance(startPosition, repairerPosition);
		float num3 = 1f / num2;
		if (num2 > range * range)
		{
			num3 *= 0.001f;
		}
		if (!disabled)
		{
			num3 *= 0.5f;
		}
		if (repairInstance != null)
		{
			num3 *= 0.01f;
		}
		return (1f - num) * num3 * definition.value;
	}

	public bool NeedsRepair()
	{
		return needsRepair;
	}

	public void Building_OnPartDamage(UnitPart.OnApplyDamage e)
	{
		if (repairTime < 999999f && !needsRepair)
		{
			timeLastDamaged = Time.timeSinceLevelLoad;
			needsRepair = true;
			if (base.NetworkHQ != null)
			{
				base.NetworkHQ.NotifyNeedsRepair(this);
			}
		}
	}

	public override void HQChanged(FactionHQ oldHQ, FactionHQ newHQ)
	{
		base.HQChanged(oldHQ, newHQ);
		if (needsRepair && NetworkManagerNuclearOption.i.Server.Active)
		{
			base.NetworkHQ.NotifyNeedsRepair(this);
			if (oldHQ != null)
			{
				oldHQ.NotifyRepaired(this);
			}
		}
	}

	[ClientRpc]
	private void RpcToggleRepairStatus(bool repairInProgress)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcToggleRepairStatus_1687816056(repairInProgress);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBooleanExtension(repairInProgress);
		ClientRpcSender.Send(this, 22, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[Server]
	public void Repair(Unit repairer, float strength)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'Repair' called when server not active");
		}
		if (repairTime > 999999f || !needsRepair)
		{
			return;
		}
		if (disabled)
		{
			RpcToggleRepairStatus(repairInProgress: true);
		}
		float num = 0f;
		foreach (UnitPart item in partLookup)
		{
			item.hitPoints = Mathf.Clamp(item.hitPoints + strength / repairTime, 0f, 100f);
			num += 100f - item.hitPoints;
		}
		if (num <= 0f)
		{
			needsRepair = false;
			base.NetworkHQ.NotifyRepaired(this);
			if (disabled)
			{
				base.Networkdisabled = false;
				OnRepairComplete();
			}
		}
	}

	public virtual void OnRepairComplete()
	{
		this.OnRepair?.Invoke();
		Debug.Log("BUILDING REPAIRS COMPLETE " + UniqueName);
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
			writer.WriteNetworkBehaviorSyncVar(airbase);
			return true;
		}
		writer.Write((ulong)((long)syncVarDirtyBits >> 9), 1);
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			airbase = reader.ReadNetworkBehaviourSyncVar();
			return;
		}
		ulong dirtyBit = reader.Read(1);
		SetDeserializeMask(dirtyBit, 9);
	}

	private void UserCode_RpcSetAirbase__002D1101525328(Airbase airbase)
	{
		Networkairbase = airbase;
		airbase.AddBuilding(this, errorIfAlreadyMember: true);
	}

	protected static void Skeleton_RpcSetAirbase__002D1101525328(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Building)behaviour).UserCode_RpcSetAirbase__002D1101525328(GeneratedNetworkCode._Read_Airbase(reader));
	}

	private void UserCode_RpcToggleRepairStatus_1687816056(bool repairInProgress)
	{
		if (repairInProgress)
		{
			if (repairInstance == null && repairPrefab != null)
			{
				repairInstance = UnityEngine.Object.Instantiate(repairPrefab, base.transform);
				repairInstance.transform.position = startPosition.ToLocalPosition() - definition.spawnOffset;
				repairInstance.transform.localScale = new Vector3(definition.width * 1.05f, definition.height, definition.length * 1.05f);
			}
		}
		else if (repairInstance != null)
		{
			UnityEngine.Object.Destroy(repairInstance);
		}
	}

	protected static void Skeleton_RpcToggleRepairStatus_1687816056(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Building)behaviour).UserCode_RpcToggleRepairStatus_1687816056(reader.ReadBooleanExtension());
	}

	protected override int GetRpcCount()
	{
		return 23;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(21, "Building.RpcSetAirbase", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSetAirbase__002D1101525328, RpcRateLimitConfig.Disabled());
		collection.Register(22, "Building.RpcToggleRepairStatus", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcToggleRepairStatus_1687816056, RpcRateLimitConfig.Disabled());
	}
}
