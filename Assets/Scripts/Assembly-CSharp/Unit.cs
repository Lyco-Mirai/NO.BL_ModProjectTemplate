using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using UnityEngine;

public class Unit : NetworkBehaviour, IEditorSelectable, IIgnoreTerrainCheck
{
	public enum UnitState : byte
	{
		Active = 1,
		Damaged = 2,
		Abandoned = 3,
		Destroyed = 4,
		Returned = 5
	}

	public struct JamEventArgs
	{
		public Unit jammingUnit;

		public float jamAmount;
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CSearchForRearm_003Ed__193 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public AsyncUniTaskMethodBuilder _003C_003Et__builder;

		public Unit _003C_003E4__this;

		private CancellationToken _003Ccancel_003E5__2;

		private UniTask.Awaiter _003C_003Eu__1;

		private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__2;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			Unit unit = _003C_003E4__this;
			try
			{
				Cysharp.Threading.Tasks.YieldAwaitable.Awaiter awaiter;
				if (num != 0)
				{
					if (num != 1)
					{
						_003Ccancel_003E5__2 = unit.destroyCancellationToken;
						goto IL_014f;
					}
					awaiter = _003C_003Eu__2;
					_003C_003Eu__2 = default(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_00ef;
				}
				UniTask.Awaiter awaiter2 = _003C_003Eu__1;
				_003C_003Eu__1 = default(UniTask.Awaiter);
				num = (_003C_003E1__state = -1);
				goto IL_008e;
				IL_014f:
				if (unit.HasRequestedRearm)
				{
					awaiter2 = UniTask.WaitForSeconds(5).GetAwaiter();
					if (!awaiter2.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter2;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
						return;
					}
					goto IL_008e;
				}
				goto end_IL_000e;
				IL_00ef:
				awaiter.GetResult();
				if (!_003Ccancel_003E5__2.IsCancellationRequested)
				{
					if (!(unit.NetworkHQ == null) && !(unit.speed > 25f) && !(unit.radarAlt > 1f) && unit.NetworkHQ.RearmMissionController.TryGetRearmer(unit, out var bestRearmer))
					{
						bestRearmer.ProcessRearmRequest(unit, out var _);
					}
					goto IL_014f;
				}
				goto end_IL_000e;
				IL_008e:
				awaiter2.GetResult();
				awaiter = UniTask.WaitForFixedUpdate().GetAwaiter();
				if (!awaiter.IsCompleted)
				{
					num = (_003C_003E1__state = 1);
					_003C_003Eu__2 = awaiter;
					_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
					return;
				}
				goto IL_00ef;
				end_IL_000e:;
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003Ccancel_003E5__2 = default(CancellationToken);
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003Ccancel_003E5__2 = default(CancellationToken);
			_003C_003Et__builder.SetResult();
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	public const string BUILTIN_UNIT_PREFIX = "<MAP_UNIT>++";

	public const int MAX_STATION_TARGETS = 128;

	public const string MAX_STATION_TARGETS_WARNING = "SetStationTargets can only have a max of 128 targets";

	[SyncVar(initialOnly = true)]
	public PersistentID persistentID;

	[NonSerialized]
	[SyncVar(initialOnly = true)]
	public string unitName;

	[NonSerialized]
	[SyncVar(initialOnly = true)]
	public string UniqueName;

	[NonSerialized]
	[SyncVar]
	public GlobalPosition startPosition;

	[NonSerialized]
	[SyncVar]
	public Quaternion startRotation = Quaternion.identity;

	[NonSerialized]
	[SyncVar(hook = "HQChanged")]
	public NetworkBehaviorSyncvar HQ;

	[NonSerialized]
	[SyncVar(hook = "UnitDisabled")]
	public bool disabled;

	[NonSerialized]
	[SyncVar]
	public UnitState unitState;

	[NonSerialized]
	[SyncVar(hook = "OnFiringStateUpdated")]
	private WeaponMask remoteWeaponStates;

	[NonSerialized]
	private WeaponMask localWeaponStates;

	private bool isRemoteFiring;

	[Header("Map Values")]
	[SerializeField]
	public string MapUniqueName;

	[Tooltip("Starting HQ for map units")]
	[SerializeField]
	public FactionHQ MapHQ;

	[SerializeField]
	public Airbase MapAirbase;

	[Space]
	public List<WeaponStation> weaponStations = new List<WeaponStation>();

	[NonSerialized]
	public float maxRadius;

	[NonSerialized]
	public float obstacleTop;

	[SerializeField]
	private Transform obstacleTopTransform;

	[NonSerialized]
	public float displayDetail;

	protected float visibility = 6f;

	public Transform cockpitViewPoint;

	public UnitDefinition definition;

	protected Dictionary<PersistentID, float> damageCredit;

	public bool HasRequestedRearm;

	[NonSerialized]
	public List<UnitPart> partLookup = new List<UnitPart>();

	[NonSerialized]
	public List<DamageablePart> damageables = new List<DamageablePart>();

	private float extraCaptureStrength;

	[HideInInspector]
	public bool networked = true;

	[HideInInspector]
	public float airDensity = 1f;

	[HideInInspector]
	public float radarAlt;

	[HideInInspector]
	public float speed;

	protected float lastAltitudeCheck;

	public float RCS;

	protected RaycastHit hit;

	private List<IRSource> IRSources = new List<IRSource>();

	public TargetDetector radar;

	[SerializeField]
	private List<AudioSource> dopplerSounds = new List<AudioSource>();

	[HideInInspector]
	public List<DamageParticles> spawnedEffects = new List<DamageParticles>();

	private GridSquare gridSquare;

	private ObjectType objectType;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 9;

	[NonSerialized]
	private const int RPC_COUNT = 21;

	public FactionHQ Editor_HQ => NetworkHQ;

	public SavedUnit SavedUnit { get; protected set; }

	public bool BuiltIn
	{
		get
		{
			if (ObjectType != ObjectType.MapObject)
			{
				return objectType == ObjectType.SceneObject;
			}
			return true;
		}
	}

	public bool IsCustom
	{
		get
		{
			if (SavedUnit != null)
			{
				return SavedUnit.PlacementType == PlacementType.Custom;
			}
			return false;
		}
	}

	public Rigidbody rb { get; private set; }

	[field: NonSerialized]
	public bool remoteSim { get; private set; } = true;

	public bool LocalSim => !remoteSim;

	public float CaptureStrength => extraCaptureStrength + (SavedUnit?.CaptureStrength.AsNullable() ?? definition.captureStrength);

	public float CaptureDefense => SavedUnit?.CaptureDefense.AsNullable() ?? definition.captureDefense;

	public ObjectType ObjectType
	{
		get
		{
			if (objectType == ObjectType.NotSet)
			{
				objectType = MapLoader.GetObjectType(base.Identity);
			}
			return objectType;
		}
	}

	public PersistentID NetworkpersistentID
	{
		get
		{
			return persistentID;
		}
		set
		{
			persistentID = value;
		}
	}

	public string NetworkunitName
	{
		get
		{
			return unitName;
		}
		set
		{
			unitName = value;
		}
	}

	public string NetworkUniqueName
	{
		get
		{
			return UniqueName;
		}
		set
		{
			UniqueName = value;
		}
	}

	public GlobalPosition NetworkstartPosition
	{
		get
		{
			return startPosition;
		}
		set
		{
			if (!SyncVarEqual(value, startPosition))
			{
				GlobalPosition globalPosition = startPosition;
				startPosition = value;
				SetDirtyBit(8uL);
			}
		}
	}

	public Quaternion NetworkstartRotation
	{
		get
		{
			return startRotation;
		}
		set
		{
			if (!SyncVarEqual(value, startRotation))
			{
				Quaternion quaternion = startRotation;
				startRotation = value;
				SetDirtyBit(16uL);
			}
		}
	}

	public FactionHQ NetworkHQ
	{
		get
		{
			return (FactionHQ)HQ.Value;
		}
		set
		{
			if (!SyncVarEqual(value, (FactionHQ)HQ.Value))
			{
				FactionHQ oldHQ = (FactionHQ)HQ.Value;
				HQ.Value = value;
				SetDirtyBit(32uL);
				if (!GetSyncVarHookGuard(32uL) && base.IsHost)
				{
					SetSyncVarHookGuard(32uL, value: true);
					HQChanged(oldHQ, value);
					SetSyncVarHookGuard(32uL, value: false);
				}
			}
		}
	}

	public bool Networkdisabled
	{
		get
		{
			return disabled;
		}
		set
		{
			if (!SyncVarEqual(value, disabled))
			{
				bool _ = disabled;
				disabled = value;
				SetDirtyBit(64uL);
				if (!GetSyncVarHookGuard(64uL) && base.IsHost)
				{
					SetSyncVarHookGuard(64uL, value: true);
					UnitDisabled(_, value);
					SetSyncVarHookGuard(64uL, value: false);
				}
			}
		}
	}

	public UnitState NetworkunitState
	{
		get
		{
			return unitState;
		}
		set
		{
			if (!SyncVarEqual(value, this.unitState))
			{
				UnitState unitState = this.unitState;
				this.unitState = value;
				SetDirtyBit(128uL);
			}
		}
	}

	public WeaponMask NetworkremoteWeaponStates
	{
		get
		{
			return remoteWeaponStates;
		}
		set
		{
			if (!SyncVarEqual(value, remoteWeaponStates))
			{
				WeaponMask oldMask = remoteWeaponStates;
				remoteWeaponStates = value;
				SetDirtyBit(256uL);
				if (!GetSyncVarHookGuard(256uL) && base.IsHost)
				{
					SetSyncVarHookGuard(256uL, value: true);
					OnFiringStateUpdated(oldMask, value);
					SetSyncVarHookGuard(256uL, value: false);
				}
			}
		}
	}

	public event Action onInitialize;

	public event Action<Unit> onDisableUnit;

	public event Action<Unit> onChangeFaction;

	public event Action<IRSource> onAddIRSource;

	public event Action<RadarChaff> onAddRadarChaff;

	public event Action<Missile> onRegisterMissile;

	public event Action<Missile> onDeregisterMissile;

	public event Action<JamEventArgs> onJam;

	public event Action OnRearmUnit;

	public void ClearWeaponStates()
	{
		NetworkremoteWeaponStates = default(WeaponMask);
		localWeaponStates = default(WeaponMask);
	}

	public virtual Airbase GetAirbase()
	{
		return null;
	}

	protected virtual void OnValidate()
	{
		if (GetComponentInParent<NetworkMap>() != null && string.IsNullOrEmpty(MapUniqueName))
		{
			UnityEngine.Debug.LogError("Map Unit in NetworkMap is missing MapUniqueName");
		}
	}

	public virtual void Awake()
	{
		base.Identity.OnStartServer.AddListener(OnStartServer);
		RCS = definition.radarSize;
		if (!string.IsNullOrEmpty(MapUniqueName))
		{
			UnitRegistry.RegisterCustomID(MapUniqueName, this);
		}
	}

	private void OnStartServer()
	{
		NetworkpersistentID = UnitRegistry.GetNextIndex();
		if (!string.IsNullOrEmpty(MapUniqueName))
		{
			NetworkUniqueName = MapUniqueName;
		}
	}

	public void EditorMapLoaded()
	{
		if (!string.IsNullOrEmpty(MapUniqueName))
		{
			if (SavedUnit == null)
			{
				SavedUnit = CreateBuiltInSavedUnit();
			}
			else
			{
				UnityEngine.Debug.LogWarning("MapObject had SavedUnit before Awake");
			}
		}
	}

	public void EditorMapCleanup()
	{
		RemoveSavedUnitOverride(fullCleanup: true);
	}

	public void RemoveSavedUnitOverride(bool fullCleanup)
	{
		if (fullCleanup)
		{
			SavedUnit = null;
		}
		else if (SavedUnit != null)
		{
			ResetBuiltInSavedUnit(SavedUnit);
		}
		else
		{
			SavedUnit = CreateBuiltInSavedUnit();
		}
		NetworkHQ = MapHQ;
	}

	protected virtual void ResetBuiltInSavedUnit(SavedUnit saved)
	{
		saved.PlacementType = PlacementType.BuiltIn;
		saved.faction = "";
		saved.inventory = null;
		saved.CaptureStrength = default(Override<float>);
		saved.CaptureDefense = default(Override<float>);
	}

	protected virtual SavedUnit CreateBuiltInSavedUnit()
	{
		throw new NotSupportedException(GetType().Name + " has no method to create a built in SavedUnit");
	}

	public virtual void OnEnable()
	{
		visibility = definition.visibleRange;
		maxRadius = Mathf.Max(Mathf.Max(definition.length, definition.width), definition.height) * 0.5f;
		if (obstacleTopTransform != null)
		{
			obstacleTop = obstacleTopTransform.position.y - base.transform.position.y;
		}
		else
		{
			obstacleTop = float.MaxValue;
		}
		if (rb == null)
		{
			SetRB(base.gameObject.GetComponentInChildren<Rigidbody>());
		}
		base.gameObject.name = definition.unitPrefab.name;
		NetworkunitState = UnitState.Active;
	}

	public void ModifyCaptureStrength(float captureStrength)
	{
		extraCaptureStrength += captureStrength;
	}

	public void LinkSavedUnit(SavedUnit savedUnit)
	{
		SavedUnit = savedUnit;
		savedUnit.Unit = this;
		if (base.IsServer)
		{
			if (string.IsNullOrEmpty(UniqueName))
			{
				NetworkUniqueName = MapUniqueName;
			}
		}
		else if (!base.IsClient)
		{
			UnityEngine.Debug.LogError("Linking unit with SavedUnit before NetworkSpawn");
		}
	}

	public virtual float GetPrefabMass()
	{
		float num = 0f;
		UnitPart[] componentsInChildren = base.gameObject.GetComponentsInChildren<UnitPart>();
		foreach (UnitPart unitPart in componentsInChildren)
		{
			num += unitPart.GetMass();
		}
		return num;
	}

	public virtual PowerSupply GetPowerSupply()
	{
		return null;
	}

	public virtual Vector3 GetWindVelocity()
	{
		return Vector3.zero;
	}

	public virtual float GetAirDensity()
	{
		return 0f;
	}

	public virtual float GetMass()
	{
		float num = 0f;
		foreach (UnitPart item in partLookup)
		{
			num += item.GetMass();
		}
		return num;
	}

	public void ModifyRCS(float changeInRCS)
	{
		RCS += changeInRCS;
	}

	public Vector3 GetCenterOfMass()
	{
		Vector3 zero = Vector3.zero;
		float num = 0f;
		foreach (UnitPart item in partLookup)
		{
			float mass = item.GetMass();
			num += mass;
			Vector3 centerOfMass = item.rb.centerOfMass;
			Vector3 vector = item.xform.TransformPoint(centerOfMass);
			zero += vector * mass;
		}
		return zero / num;
	}

	public bool HasIRSignature()
	{
		return IRSources.Count > 0;
	}

	public bool HasRadarEmission()
	{
		if (radar is Radar)
		{
			return radar.activated;
		}
		return false;
	}

	public void RegisterUnit(float? updateInterval)
	{
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			if (NetworkManagerNuclearOption.i.Server.Active && NetworkHQ != null)
			{
				NetworkHQ.RegisterFactionUnit(this);
			}
			if (base.IsClientOnly && !string.IsNullOrEmpty(UniqueName))
			{
				UnitRegistry.RegisterCustomID(UniqueName, this);
			}
			RegisterOnGridAsync(updateInterval).Forget();
		}
	}

	private async UniTaskVoid RegisterOnGridAsync(float? updateInterval)
	{
		while (NetworkSceneSingleton<LevelInfo>.i == null)
		{
			await UniTask.Yield();
		}
		if (updateInterval.HasValue)
		{
			this.StartSlowUpdate(updateInterval.Value, UpdateGridNow);
		}
		else
		{
			UpdateGridNow();
		}
		UnitRegistry.RegisterUnit(this, persistentID);
	}

	public virtual void InitializeUnit()
	{
		this.onInitialize?.Invoke();
	}

	public virtual void SetLocalSim(bool localSim)
	{
		remoteSim = !localSim;
	}

	public virtual List<UnitPart> GetAllParts()
	{
		return partLookup;
	}

	public virtual Transform GetRandomPart()
	{
		List<UnitPart> list = new List<UnitPart>();
		foreach (UnitPart item in partLookup)
		{
			if (item != null && !item.IsDetached())
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			return base.transform;
		}
		int index = Mathf.FloorToInt(UnityEngine.Random.Range(0f, (float)list.Count - 1E-05f));
		return list[index].transform;
	}

	public byte RegisterDamageable(IDamageable damageable)
	{
		int count = damageables.Count;
		damageables.Add(new DamageablePart(damageable));
		return (byte)count;
	}

	public void DeregisterDamageable(int index)
	{
		damageables[index] = damageables[index].CreateRemoved();
	}

	public void AddRadarChaff(RadarChaff source)
	{
		this.onAddRadarChaff?.Invoke(source);
	}

	public void AddIRSource(IRSource source)
	{
		IRSources.Add(source);
		this.onAddIRSource?.Invoke(source);
	}

	public void RemoveIRSource(IRSource source)
	{
		IRSources.Remove(source);
	}

	public IRSource GetIRSource()
	{
		if (IRSources.Count == 0)
		{
			return null;
		}
		for (int num = IRSources.Count - 1; num >= 0; num--)
		{
			if (FastMath.OutOfRange(IRSources[num].transform.GlobalPosition(), this.GlobalPosition(), 100f))
			{
				IRSources.RemoveAt(num);
			}
		}
		int index = Mathf.FloorToInt(UnityEngine.Random.Range(0f, (float)IRSources.Count - 0.0001f));
		if (IRSources.Count <= 0)
		{
			return null;
		}
		return IRSources[index];
	}

	public void RegisterMissile(Missile missile)
	{
		this.onRegisterMissile?.Invoke(missile);
	}

	public void DeregisterMissile(Missile missile)
	{
		this.onDeregisterMissile?.Invoke(missile);
	}

	public virtual void HQChanged(FactionHQ oldHQ, FactionHQ newHQ)
	{
		if (UnitRegistry.persistentUnitLookup.TryGetValue(persistentID, out var value))
		{
			value.SetHQ(newHQ);
		}
		if (disabled || unitState != UnitState.Active || newHQ == null)
		{
			return;
		}
		this.onChangeFaction?.Invoke(this);
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			NetworkHQ.factionUnits.Add(persistentID);
			if (oldHQ != null)
			{
				oldHQ.RemoveFactionUnit(this);
			}
		}
	}

	public void RegisterWeaponStation(WeaponStation weaponStation)
	{
		weaponStations.Add(weaponStation);
	}

	public void ClearWeaponStations()
	{
		weaponStations.Clear();
	}

	public void RequestRearm()
	{
		if (!(NetworkHQ == null) && !HasRequestedRearm && (!(this is Aircraft aircraft) || !(aircraft.Player == null)))
		{
			NetworkHQ.RearmMissionController.RegisterNeedsRearm(this);
			HasRequestedRearm = true;
			if (base.IsServer)
			{
				SearchForRearm().Forget();
			}
		}
	}

	[ClientRpc]
	public void RpcRearm(RearmEventArgs args)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcRearm_1512805087(args);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_RearmEventArgs(writer, args);
		ClientRpcSender.Send(this, 0, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcUpdateRearmerCapacity(float capacity)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcUpdateRearmerCapacity_520309312(capacity);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteSingleConverter(capacity);
		ClientRpcSender.Send(this, 1, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public float GetMaxRange()
	{
		if (weaponStations.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		foreach (WeaponStation weaponStation in weaponStations)
		{
			num = Mathf.Max(num, weaponStation.WeaponInfo.targetRequirements.maxRange);
		}
		return num;
	}

	public void RegisterDopplerSound(AudioSource audioSource)
	{
		dopplerSounds.Add(audioSource);
	}

	public void DeregisterDopplerSound(AudioSource audioSource)
	{
		dopplerSounds.Remove(audioSource);
	}

	public void SetDoppler(bool enabled)
	{
		foreach (AudioSource dopplerSound in dopplerSounds)
		{
			if (!(dopplerSound == null))
			{
				dopplerSound.dopplerLevel = (enabled ? 0.6f : 0f);
				dopplerSound.spatialBlend = (enabled ? 1 : 0);
			}
		}
	}

	public void SetSoundsMuted(bool muted)
	{
		foreach (AudioSource dopplerSound in dopplerSounds)
		{
			if (dopplerSound != null)
			{
				dopplerSound.mute = muted;
			}
		}
	}

	public virtual void UnitDisabled(bool _, bool nowDisabled)
	{
		if (nowDisabled)
		{
			RemoveFromGrid();
			this.onDisableUnit?.Invoke(this);
			if (HasRequestedRearm && NetworkHQ != null)
			{
				NetworkHQ.RearmMissionController.DeregisterNeedsRearm(this);
			}
			if (NetworkManagerNuclearOption.i.Server.Active && NetworkHQ != null)
			{
				NetworkHQ.RemoveFactionUnit(this);
			}
		}
	}

	public virtual void AttachOrDetachSlingHook(Aircraft aircraft, bool attached)
	{
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.drag = 0.02f;
		rb.angularDrag = 0.1f;
		if (aircraft.LocalSim && attached)
		{
			remoteSim = false;
		}
		else
		{
			remoteSim = !base.IsServer;
		}
		rb.useGravity = !remoteSim;
	}

	public virtual void ChangeUnitState(UnitState newState)
	{
		NetworkunitState = newState;
	}

	public virtual bool IsSlung()
	{
		return false;
	}

	private void RemoveFromGrid()
	{
		gridSquare?.units.Remove(this);
		gridSquare = null;
	}

	private void UpdateGridNow()
	{
		if (!disabled && unitState == UnitState.Active)
		{
			BattlefieldGrid.UpdateUnit(this, ref gridSquare);
		}
	}

	public void ModifyVisibility(float visibility)
	{
		this.visibility += visibility;
	}

	public float GetVisibility()
	{
		return visibility;
	}

	public bool LineOfSight(Vector3 origin, float magnification)
	{
		if (FastMath.OutOfRange(origin, base.transform.position, visibility * magnification))
		{
			return false;
		}
		if (Physics.Linecast(origin, base.transform.position, out hit, PhysicsLayers.StaticsMask))
		{
			return FastMath.InRange(hit.point, base.transform.position, maxRadius * 2f);
		}
		return true;
	}

	public void Jam(JamEventArgs args)
	{
		RpcJam(args);
	}

	[ClientRpc]
	public void RpcJam(JamEventArgs args)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcJam_569024588(args);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit_002FJamEventArgs(writer, args);
		ClientRpcSender.Send(this, 2, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcAssignRadar(Unit unit)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcAssignRadar__002D1001571913(unit);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit(writer, unit);
		ClientRpcSender.Send(this, 3, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void SetRB(Rigidbody rb)
	{
		this.rb = rb;
	}

	public void SetFiringState(int index, bool firing)
	{
		if (remoteSim || localWeaponStates.Get(index) == firing)
		{
			return;
		}
		localWeaponStates = WeaponMask.Set(localWeaponStates, index, firing);
		if (firing)
		{
			TrackFiringState(index).Forget();
		}
		if (base.IsServer)
		{
			NetworkremoteWeaponStates = localWeaponStates;
			if (!firing)
			{
				RpcSyncAmmoCount((byte)index, weaponStations[index].Ammo);
			}
		}
		else
		{
			CmdSetFiringState(localWeaponStates);
			if (!firing)
			{
				CmdStoppedFiring((byte)index);
			}
		}
	}

	private async UniTask TrackFiringState(int stationNumber)
	{
		if (stationNumber < 0)
		{
			ColorLog<Unit>.LogError("stationNumber was negative");
			return;
		}
		if (stationNumber >= weaponStations.Count)
		{
			ColorLog<Unit>.LogError($"stationNumber was {stationNumber}, but count is {weaponStations.Count}");
			return;
		}
		WeaponStation station = weaponStations[stationNumber];
		CancellationToken cancel = base.destroyCancellationToken;
		while (Time.timeSinceLevelLoad - station.LastFiredTime <= 0.2f)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested || stationNumber >= weaponStations.Count)
			{
				return;
			}
		}
		SetFiringState(stationNumber, firing: false);
	}

	[ServerRpc]
	private void CmdSetFiringState(WeaponMask weaponMask)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetFiringState_1458755207(weaponMask);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_WeaponMask(writer, weaponMask);
		ServerRpcSender.Send(this, 4, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	private void OnFiringStateUpdated(WeaponMask oldMask, WeaponMask newMask)
	{
		if (!LocalSim && newMask.Mask != 0 && !isRemoteFiring)
		{
			RemoteFireWeaponStation().Forget();
		}
	}

	private async UniTask RemoteFireWeaponStation()
	{
		isRemoteFiring = true;
		await UniTask.Yield();
		CancellationToken cancel = base.destroyCancellationToken;
		while (!cancel.IsCancellationRequested)
		{
			if (remoteWeaponStates.Mask == 0)
			{
				isRemoteFiring = false;
				break;
			}
			for (int i = 0; i < weaponStations.Count; i++)
			{
				if (remoteWeaponStates.Get(i))
				{
					weaponStations[i].RemoteFireAuto(this);
				}
			}
			await UniTask.Yield();
		}
	}

	public void SingleRemoteFire(byte stationIndex, int ammo)
	{
		if (base.IsServer)
		{
			RpcSingleRemoteFire(stationIndex, ammo);
		}
		else if (base.HasAuthority)
		{
			CmdSingleRemoteFire(stationIndex);
		}
	}

	[RateLimit(Refill = 20, MaxTokens = 100, Penalty = 1)]
	[ServerRpc]
	private void CmdSingleRemoteFire(byte stationIndex)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSingleRemoteFire_1708169300(stationIndex);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		ServerRpcSender.Send(this, 5, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	public void RpcSingleRemoteFire(byte stationIndex, int ammo)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcSingleRemoteFire__002D1161895954(stationIndex, ammo);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		writer.WritePackedInt32(ammo);
		ClientRpcSender.Send(this, 6, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ServerRpc]
	private void CmdStoppedFiring(byte stationIndex)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdStoppedFiring__002D1238964004(stationIndex);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		ServerRpcSender.Send(this, 7, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc(excludeHost = true)]
	public void RpcSyncAmmoCount(byte stationIndex, int ammo)
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		writer.WritePackedInt32(ammo);
		ClientRpcSender.Send(this, 8, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ServerRpc]
	public void CmdSetStationTargets(byte stationIndex, [MaxLength(128)] ReadOnlySpan<PersistentID> targetIDs)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetStationTargets_872088460(stationIndex, targetIDs);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		GeneratedNetworkCode._Write_System_002EReadOnlySpan_00601_003CPersistentID_003E_WithLength(writer, targetIDs, 128);
		ServerRpcSender.Send(this, 9, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	public virtual void RpcSetStationTargets(byte stationIndex, ReadOnlySpan<PersistentID> targetIDs)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcSetStationTargets_1363862903(stationIndex, targetIDs);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		GeneratedNetworkCode._Write_System_002EReadOnlySpan_00601_003CPersistentID_003E(writer, targetIDs);
		ClientRpcSender.Send(this, 10, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public bool CheckIsTarget(Unit candidate)
	{
		if (weaponStations.Count == 0)
		{
			if (radar != null)
			{
				return (radar as Radar).CheckIsTarget(candidate);
			}
			return false;
		}
		foreach (WeaponStation weaponStation in weaponStations)
		{
			Unit stationTarget = weaponStation.GetStationTarget();
			if (candidate == stationTarget)
			{
				return true;
			}
		}
		return false;
	}

	public void RecordDamage(PersistentID lastDamagedBy, float damageAmount)
	{
		if (damageCredit == null)
		{
			damageCredit = new Dictionary<PersistentID, float>();
		}
		damageCredit.TryGetValue(lastDamagedBy, out var value);
		damageCredit[lastDamagedBy] = value + damageAmount;
	}

	public void RegisterHit(Unit hitUnit, Vector3 relativePos, Vector3 bulletVelocity, WeaponInfo weaponInfo)
	{
		if (base.IsServer)
		{
			Vector3 vector = hitUnit.transform.TransformPoint(relativePos);
			if (PlayerSettings.debugVis)
			{
				GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrow, hitUnit.transform);
				gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1f, 1f, 1f, 1f));
				gameObject.transform.position = vector - bulletVelocity.normalized;
				gameObject.transform.rotation = Quaternion.LookRotation(bulletVelocity);
				gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
				NetworkSceneSingleton<Spawner>.i.DestroyLocal(gameObject, 10f);
			}
			DamageEffects.ArmorPenetrate(vector, bulletVelocity, weaponInfo.muzzleVelocity, weaponInfo.pierceDamage, weaponInfo.blastDamage, persistentID);
		}
		else
		{
			if (!base.HasAuthority)
			{
				return;
			}
			byte? b = null;
			for (int i = 0; i < weaponStations.Count; i++)
			{
				if (weaponStations[i].WeaponInfo == weaponInfo)
				{
					b = checked((byte)i);
					break;
				}
			}
			if (!b.HasValue)
			{
				UnityEngine.Debug.LogError($"Could not find weaponStations for {weaponInfo}");
			}
			else
			{
				CmdClaimHit(hitUnit.persistentID, NetworkFloatHelper.CompressIfValid(relativePos, logErrors: true, "relativePos"), NetworkFloatHelper.CompressIfValid(bulletVelocity, logErrors: true, "bulletVelocity"), b.Value);
			}
		}
	}

	[RateLimit(Refill = 20, MaxTokens = 400, Penalty = 5)]
	[ServerRpc]
	private void CmdClaimHit(PersistentID hitID, Vector3Compressed relativePosCompressed, Vector3Compressed bulletVelocityCompressed, byte weaponStationIndex)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdClaimHit__002D1122942669(hitID, relativePosCompressed, bulletVelocityCompressed, weaponStationIndex);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_PersistentID(writer, hitID);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, relativePosCompressed);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, bulletVelocityCompressed);
		writer.WriteByteExtension(weaponStationIndex);
		ServerRpcSender.Send(this, 11, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	private async UniTask HitOnPhysicsFrame(Unit hitUnit, Vector3 relativePos, Vector3 hitVelocity, byte weaponStationIndex)
	{
		await UniTask.WaitForFixedUpdate();
		Vector3 vector = hitUnit.transform.TransformPoint(relativePos);
		if (HitValidator.HitValidated(this, vector - Datum.origin.position, hitVelocity))
		{
			WeaponInfo weaponInfo = weaponStations[weaponStationIndex].WeaponInfo;
			DamageEffects.ArmorPenetrate(vector, Vector3.ClampMagnitude(hitVelocity, weaponInfo.muzzleVelocity * 1.5f), weaponInfo.muzzleVelocity, weaponInfo.pierceDamage, weaponInfo.blastDamage, persistentID);
		}
	}

	public void Damage(byte index, DamageInfo damageInfo)
	{
		if (base.IsServer)
		{
			RpcDamage(index, damageInfo);
		}
		else if (base.HasAuthority)
		{
			CmdDamage(index, damageInfo);
		}
	}

	[RateLimit(Refill = 20, MaxTokens = 100, Penalty = 1)]
	[ServerRpc]
	private void CmdDamage(byte partID, DamageInfo damageInfo)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdDamage__002D2104360672(partID, damageInfo);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(partID);
		GeneratedNetworkCode._Write_DamageInfo(writer, damageInfo);
		ServerRpcSender.Send(this, 12, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	public virtual void RpcDamage(byte index, DamageInfo damageInfo)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcDamage_1709046923(index, damageInfo);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(index);
		GeneratedNetworkCode._Write_DamageInfo(writer, damageInfo);
		ClientRpcSender.Send(this, 13, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void FuelTankStatus(byte partID, bool ruptured, bool onFire)
	{
		if (base.IsServer)
		{
			RpcFuelTankStatus(partID, ruptured, onFire);
		}
		else if (base.HasAuthority)
		{
			CmdFuelTankStatus(partID, ruptured, onFire);
		}
	}

	[ServerRpc]
	private void CmdFuelTankStatus(byte partID, bool ruptured, bool onFire)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdFuelTankStatus_423953574(partID, ruptured, onFire);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(partID);
		writer.WriteBooleanExtension(ruptured);
		writer.WriteBooleanExtension(onFire);
		ServerRpcSender.Send(this, 14, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	public void RpcFuelTankStatus(byte partID, bool ruptured, bool onFire)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcFuelTankStatus_414334555(partID, ruptured, onFire);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(partID);
		writer.WriteBooleanExtension(ruptured);
		writer.WriteBooleanExtension(onFire);
		ClientRpcSender.Send(this, 15, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void DetachPart(byte partID, Vector3 velocity, Vector3 relativePos)
	{
		if (base.IsServer)
		{
			RpcBreakPart(partID, NetworkFloatHelper.CompressIfValid(velocity, logErrors: false, null), NetworkFloatHelper.CompressIfValid(relativePos, logErrors: false, null));
		}
		else if (base.HasAuthority)
		{
			CmdBreakPart(partID, NetworkFloatHelper.CompressIfValid(velocity, logErrors: true, "velocity"), NetworkFloatHelper.CompressIfValid(relativePos, logErrors: true, "relativePos"));
		}
	}

	[RateLimit(Refill = 10, MaxTokens = 60, Penalty = 2)]
	[ServerRpc]
	private void CmdBreakPart(byte partID, Vector3Compressed velocity, Vector3Compressed relativePos)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdBreakPart__002D789722400(partID, velocity, relativePos);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(partID);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, velocity);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, relativePos);
		ServerRpcSender.Send(this, 16, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	private void RpcBreakPart(byte partID, Vector3Compressed velocityCompressed, Vector3Compressed relativePosCompressed)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcBreakPart__002D1087911989(partID, velocityCompressed, relativePosCompressed);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(partID);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, velocityCompressed);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, relativePosCompressed);
		ClientRpcSender.Send(this, 17, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void DisableUnit()
	{
		if (base.IsServer)
		{
			ServerDisableUnit();
		}
		else if (base.HasAuthority)
		{
			CmdDisableUnit();
		}
	}

	[RateLimit(Refill = 2, MaxTokens = 10, Penalty = 5)]
	[ServerRpc]
	public void CmdDisableUnit()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdDisableUnit__002D340399461();
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ServerRpcSender.Send(this, 18, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[Server]
	protected virtual void ServerDisableUnit()
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'ServerDisableUnit' called when server not active");
		}
		if (!disabled)
		{
			Networkdisabled = true;
		}
	}

	public void ReportKilled()
	{
		if (!UnitRegistry.TryGetPersistentUnit(this.persistentID, out var persistentUnit))
		{
			return;
		}
		FactionHQ hQ = persistentUnit.GetHQ();
		float num = 0f;
		float num2 = 0f;
		PersistentID persistentID = PersistentID.None;
		if (damageCredit != null)
		{
			foreach (KeyValuePair<PersistentID, float> item in damageCredit)
			{
				num2 += item.Value;
			}
			Dictionary<Player, float> dictionary = new Dictionary<Player, float>();
			foreach (KeyValuePair<PersistentID, float> item2 in damageCredit)
			{
				if (!UnitRegistry.TryGetPersistentUnit(item2.Key, out var persistentUnit2))
				{
					continue;
				}
				float num3 = item2.Value / num2;
				if (num3 < 0.01f)
				{
					continue;
				}
				if (item2.Value >= num)
				{
					num = item2.Value;
					persistentID = item2.Key;
				}
				FactionHQ hQ2 = persistentUnit2.GetHQ();
				if (hQ == hQ2 || hQ == null)
				{
					continue;
				}
				float num4 = Mathf.Sqrt(persistentUnit.definition.value) * num3;
				float num5 = num4 * hQ2.killReward;
				hQ2.AddScore(num4);
				hQ2.AddFunds(num5 * hQ2.playerTaxRate);
				if (persistentUnit2.player != null)
				{
					if (!dictionary.ContainsKey(persistentUnit2.player))
					{
						dictionary.Add(persistentUnit2.player, 0f);
					}
					dictionary[persistentUnit2.player] += num3;
				}
			}
			foreach (KeyValuePair<Player, float> item3 in dictionary)
			{
				//item3.Key.HQ.ReportKillAction(item3.Key, this, item3.Value);
			}
		}
		if (this is Aircraft aircraft && aircraft.Player != null && num2 > 1f && UnitRegistry.TryGetUnit(persistentID, out var unit) && unit is Aircraft aircraft2 && aircraft2.Player != null && unit.NetworkHQ == aircraft.NetworkHQ)
		{
			float num6 = aircraft.definition.value + aircraft.weaponManager.GetCurrentValue(includeCargo: true);
			aircraft2.Player.AddScore(0f - Mathf.Sqrt(aircraft.definition.value));
			aircraft2.Player.AddAllocation(0f - num6);
			aircraft.Player.AddAllocation(num6);
		}
		KillType killType = ((this is Missile) ? KillType.Missile : ((this is Building) ? KillType.Building : ((!(this is Aircraft)) ? ((!(this is Ship)) ? KillType.Vehicle : KillType.Ship) : KillType.Aircraft)));
		KillType killedType = killType;
		if (NetworkSceneSingleton<MessageManager>.i != null)
		{
			NetworkSceneSingleton<MessageManager>.i.RpcKillMessage(persistentID, this.persistentID, killedType);
		}
	}

	protected virtual void OnDestroy()
	{
		if (!MainMenu.ApplicationIsQuitting && NetworkManagerNuclearOption.i != null && (NetworkManagerNuclearOption.i.Client.Active || NetworkManagerNuclearOption.i.Server.Active) && GameManager.gameState != GameState.Encyclopedia && !disabled)
		{
			this.onDisableUnit?.Invoke(this);
		}
		UnitRegistry.UnregisterUnit(this);
		RemoveFromGrid();
		Networkdisabled = true;
		NetworkunitState = UnitState.Destroyed;
		this.onInitialize = null;
		this.onDisableUnit = null;
		this.onChangeFaction = null;
		this.onAddIRSource = null;
		this.onRegisterMissile = null;
		this.onDeregisterMissile = null;
		this.onJam = null;
	}

	public int[] GetAmmoByType(List<WeaponInfo> listTypes)
	{
		int[] array = new int[2];
		for (int i = 0; i < weaponStations.Count; i++)
		{
			if (listTypes.Contains(weaponStations[i].WeaponInfo))
			{
				array[0] += weaponStations[i].Ammo;
				array[1] += weaponStations[i].FullAmmo;
			}
		}
		return array;
	}

	public virtual void CheckRadarAlt()
	{
		if (Time.timeSinceLevelLoad > lastAltitudeCheck + 0.1f)
		{
			lastAltitudeCheck = Time.timeSinceLevelLoad;
			if (Physics.Linecast(base.transform.position, base.transform.position - Vector3.up * 10000f, out hit, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
			{
				radarAlt = hit.distance;
			}
			else
			{
				radarAlt = base.transform.position.GlobalY();
			}
			radarAlt -= definition.spawnOffset.y;
			radarAlt = Mathf.Clamp(radarAlt, 0f, base.transform.position.GlobalY() - definition.spawnOffset.y);
		}
	}

	public float GetAmmoLevel()
	{
		float num = 0f;
		foreach (WeaponStation weaponStation in weaponStations)
		{
			num += weaponStation.GetAmmoLevel();
		}
		if (num > 0f)
		{
			num /= (float)weaponStations.Count;
		}
		return num;
	}

	public AmmoValue GetAmmoValue()
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < weaponStations.Count; i++)
		{
			num += (float)weaponStations[i].Ammo * weaponStations[i].WeaponInfo.costPerRound;
			num2 += (float)weaponStations[i].FullAmmo * weaponStations[i].WeaponInfo.costPerRound;
		}
		return new AmmoValue(num, num2);
	}

	[AsyncStateMachine(typeof(_003CSearchForRearm_003Ed__193))]
	[Server]
	public UniTask SearchForRearm()
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'SearchForRearm' called when server not active");
		}
		_003CSearchForRearm_003Ed__193 stateMachine = default(_003CSearchForRearm_003Ed__193);
		stateMachine._003C_003Et__builder = AsyncUniTaskMethodBuilder.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	public async UniTask CmdAskAmmo()
	{
		int[] ammoValues = await CmdAskAmmoInternal();
		int[] array = await CmdAskFullAmmoInternal();
		for (int i = 0; i < weaponStations.Count; i++)
		{
			weaponStations[i].Ammo = ammoValues[i];
			weaponStations[i].FullAmmo = array[i];
		}
	}

	[RateLimit(Refill = 5, MaxTokens = 15, Penalty = 2)]
	[ServerRpc(requireAuthority = false)]
	private UniTask<int[]> CmdAskAmmoInternal()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			return UserCode_CmdAskAmmoInternal_1214831455();
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		UniTask<int[]> result = ServerRpcSender.SendWithReturn<int[]>(this, 19, writer, requireAuthority: false);
		writer.Release();
		return result;
	}

	[RateLimit(Refill = 5, MaxTokens = 15, Penalty = 2)]
	[ServerRpc(requireAuthority = false)]
	private UniTask<int[]> CmdAskFullAmmoInternal()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			return UserCode_CmdAskFullAmmoInternal__002D1739804082();
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		UniTask<int[]> result = ServerRpcSender.SendWithReturn<int[]>(this, 20, writer, requireAuthority: false);
		writer.Release();
		return result;
	}

	public override string ToString()
	{
		return $"({base.ToString()}, {persistentID})";
	}

	SingleSelectionDetails IEditorSelectable.CreateSelectionDetails()
	{
		return new UnitSelectionDetails(this);
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
			GeneratedNetworkCode._Write_PersistentID(writer, persistentID);
			writer.WriteString(unitName);
			writer.WriteString(UniqueName);
			writer.WriteGlobalPosition(startPosition);
			writer.WriteQuaternion(startRotation);
			writer.WriteNetworkBehaviorSyncVar(HQ);
			writer.WriteBooleanExtension(disabled);
			GeneratedNetworkCode._Write_Unit_002FUnitState(writer, unitState);
			GeneratedNetworkCode._Write_WeaponMask(writer, remoteWeaponStates);
			return true;
		}
		writer.Write(syncVarDirtyBits, 9);
		if ((syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteGlobalPosition(startPosition);
			result = true;
		}
		if ((syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteQuaternion(startRotation);
			result = true;
		}
		if ((syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteNetworkBehaviorSyncVar(HQ);
			result = true;
		}
		if ((syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteBooleanExtension(disabled);
			result = true;
		}
		if ((syncVarDirtyBits & 0x80L) != 0L)
		{
			GeneratedNetworkCode._Write_Unit_002FUnitState(writer, unitState);
			result = true;
		}
		if ((syncVarDirtyBits & 0x100L) != 0L)
		{
			GeneratedNetworkCode._Write_WeaponMask(writer, remoteWeaponStates);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			persistentID = GeneratedNetworkCode._Read_PersistentID(reader);
			unitName = reader.ReadString();
			UniqueName = reader.ReadString();
			startPosition = reader.ReadGlobalPosition();
			startRotation = reader.ReadQuaternion();
			FactionHQ factionHQ = (FactionHQ)HQ.Value;
			HQ = reader.ReadNetworkBehaviourSyncVar();
			bool flag = disabled;
			disabled = reader.ReadBooleanExtension();
			unitState = GeneratedNetworkCode._Read_Unit_002FUnitState(reader);
			WeaponMask weaponMask = remoteWeaponStates;
			remoteWeaponStates = GeneratedNetworkCode._Read_WeaponMask(reader);
			if (!base.IsServer && !SyncVarEqual(factionHQ, (FactionHQ)HQ.Value))
			{
				HQChanged(factionHQ, (FactionHQ)HQ.Value);
			}
			if (!base.IsServer && !SyncVarEqual(flag, disabled))
			{
				UnitDisabled(flag, disabled);
			}
			if (!base.IsServer && !SyncVarEqual(weaponMask, remoteWeaponStates))
			{
				OnFiringStateUpdated(weaponMask, remoteWeaponStates);
			}
			return;
		}
		ulong num = reader.Read(9);
		SetDeserializeMask(num, 0);
		if ((num & 8L) != 0L)
		{
			startPosition = reader.ReadGlobalPosition();
		}
		if ((num & 0x10L) != 0L)
		{
			startRotation = reader.ReadQuaternion();
		}
		if ((num & 0x20L) != 0L)
		{
			FactionHQ factionHQ2 = (FactionHQ)HQ.Value;
			HQ = reader.ReadNetworkBehaviourSyncVar();
			if (!base.IsServer && !SyncVarEqual(factionHQ2, (FactionHQ)HQ.Value))
			{
				HQChanged(factionHQ2, (FactionHQ)HQ.Value);
			}
		}
		if ((num & 0x40L) != 0L)
		{
			bool flag2 = disabled;
			disabled = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(flag2, disabled))
			{
				UnitDisabled(flag2, disabled);
			}
		}
		if ((num & 0x80L) != 0L)
		{
			unitState = GeneratedNetworkCode._Read_Unit_002FUnitState(reader);
		}
		if ((num & 0x100L) != 0L)
		{
			WeaponMask weaponMask2 = remoteWeaponStates;
			remoteWeaponStates = GeneratedNetworkCode._Read_WeaponMask(reader);
			if (!base.IsServer && !SyncVarEqual(weaponMask2, remoteWeaponStates))
			{
				OnFiringStateUpdated(weaponMask2, remoteWeaponStates);
			}
		}
	}

	public void UserCode_RpcRearm_1512805087(RearmEventArgs args)
	{
		int num = -4;
		float num2 = 0f;
		float num3 = 0f;
		bool flag = true;
		for (int i = 0; i < weaponStations.Count; i++)
		{
			int num4 = args.Stations[i];
			_ = weaponStations[i].Weapons[0].RequestRearmLevel;
			if (num4 <= 0)
			{
				if (num4 == 0)
				{
					num3 += 1f;
				}
				else
				{
					num = Mathf.Max(num, num4);
				}
			}
			else
			{
				num2 += (float)num4 * weaponStations[i].WeaponInfo.costPerRound;
				weaponStations[i].Rearm(num4);
				num3 += 1f;
			}
			flag = flag && weaponStations[i].GetAmmoTotal() == weaponStations[i].FullAmmo;
		}
		num3 = 100f * num3 / (float)weaponStations.Count;
		if (flag)
		{
			HasRequestedRearm = false;
			NetworkHQ.RearmMissionController.DeregisterNeedsRearm(this);
			this.OnRearmUnit?.Invoke();
		}
		if (!(this is Aircraft aircraft))
		{
			return;
		}
		aircraft.countermeasureManager.Rearm(args);
		if (SceneSingleton<CombatHUD>.i.aircraft == this)
		{
			string text = "Rearmed ";
			text += $" {num3:F0}% complete";
			switch (num)
			{
			case -1:
				text += " insufficient ammo";
				break;
			case -2:
				text += " insufficient funds";
				break;
			case -3:
				text += " insufficient warheads";
				break;
			}
			text = text + " - cost: " + UnitConverter.ValueReading(num2);
			text = text + " by " + args.Rearmer.unitName;
			SceneSingleton<AircraftActionsReport>.i.ReportText(text, 5f);
		}
	}

	protected static void Skeleton_RpcRearm_1512805087(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcRearm_1512805087(GeneratedNetworkCode._Read_RearmEventArgs(reader));
	}

	public void UserCode_RpcUpdateRearmerCapacity_520309312(float capacity)
	{
		if (base.gameObject.TryGetComponent<Rearmer>(out var component))
		{
			component.SetCapacity(capacity);
		}
	}

	protected static void Skeleton_RpcUpdateRearmerCapacity_520309312(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcUpdateRearmerCapacity_520309312(reader.ReadSingleConverter());
	}

	public void UserCode_RpcJam_569024588(JamEventArgs args)
	{
		this.onJam?.Invoke(args);
	}

	protected static void Skeleton_RpcJam_569024588(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcJam_569024588(GeneratedNetworkCode._Read_Unit_002FJamEventArgs(reader));
	}

	public void UserCode_RpcAssignRadar__002D1001571913(Unit unit)
	{
		if (unit != null && unit.radar != null)
		{
			radar = unit.radar;
		}
	}

	protected static void Skeleton_RpcAssignRadar__002D1001571913(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcAssignRadar__002D1001571913(GeneratedNetworkCode._Read_Unit(reader));
	}

	private void UserCode_CmdSetFiringState_1458755207(WeaponMask weaponMask)
	{
		NetworkremoteWeaponStates = weaponMask;
	}

	protected static void Skeleton_CmdSetFiringState_1458755207(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdSetFiringState_1458755207(GeneratedNetworkCode._Read_WeaponMask(reader));
	}

	private void UserCode_CmdSingleRemoteFire_1708169300(byte stationIndex)
	{
		if (stationIndex >= weaponStations.Count)
		{
			base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcSingleRemoteFire(stationIndex, weaponStations[stationIndex].Ammo - 1);
		}
	}

	protected static void Skeleton_CmdSingleRemoteFire_1708169300(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdSingleRemoteFire_1708169300(reader.ReadByteExtension());
	}

	public void UserCode_RpcSingleRemoteFire__002D1161895954(byte stationIndex, int ammo)
	{
		WeaponStation weaponStation = weaponStations[stationIndex];
		weaponStation.Ammo = ammo;
		if (!LocalSim)
		{
			weaponStation.RemoteFireSingle(this);
			weaponStation.Updated();
		}
	}

	protected static void Skeleton_RpcSingleRemoteFire__002D1161895954(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcSingleRemoteFire__002D1161895954(reader.ReadByteExtension(), reader.ReadPackedInt32());
	}

	private void UserCode_CmdStoppedFiring__002D1238964004(byte stationIndex)
	{
		if (stationIndex >= weaponStations.Count)
		{
			base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.OutOfBounds);
			return;
		}
		WeaponStation weaponStation = weaponStations[stationIndex];
		RpcSyncAmmoCount(stationIndex, weaponStation.Ammo);
	}

	protected static void Skeleton_CmdStoppedFiring__002D1238964004(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdStoppedFiring__002D1238964004(reader.ReadByteExtension());
	}

	public void UserCode_RpcSyncAmmoCount__002D1454761002(byte stationIndex, int ammo)
	{
		WeaponStation weaponStation = weaponStations[stationIndex];
		weaponStation.Ammo = ammo;
		weaponStation.Updated();
	}

	protected static void Skeleton_RpcSyncAmmoCount__002D1454761002(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcSyncAmmoCount__002D1454761002(reader.ReadByteExtension(), reader.ReadPackedInt32());
	}

	public void UserCode_CmdSetStationTargets_872088460(byte stationIndex, ReadOnlySpan<PersistentID> targetIDs)
	{
		if (stationIndex >= weaponStations.Count)
		{
			base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcSetStationTargets(stationIndex, targetIDs);
		}
	}

	protected static void Skeleton_CmdSetStationTargets_872088460(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdSetStationTargets_872088460(reader.ReadByteExtension(), GeneratedNetworkCode._Read_System_002EReadOnlySpan_00601_003CPersistentID_003E_WithLength(reader, 128));
	}

	public virtual void UserCode_RpcSetStationTargets_1363862903(byte stationIndex, ReadOnlySpan<PersistentID> targetIDs)
	{
		if (stationIndex < weaponStations.Count)
		{
			weaponStations[stationIndex].SetStationTargets(targetIDs);
		}
		if (!LocalSim && this is Aircraft aircraft)
		{
			aircraft.weaponManager.SetTargetList(targetIDs);
		}
	}

	protected static void Skeleton_RpcSetStationTargets_1363862903(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcSetStationTargets_1363862903(reader.ReadByteExtension(), GeneratedNetworkCode._Read_System_002EReadOnlySpan_00601_003CPersistentID_003E(reader));
	}

	private void UserCode_CmdClaimHit__002D1122942669(PersistentID hitID, Vector3Compressed relativePosCompressed, Vector3Compressed bulletVelocityCompressed, byte weaponStationIndex)
	{
		Vector3 outValue;
		Vector3 outValue2;
		if (!UnitRegistry.TryGetUnit(hitID, out var unit))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
		}
		else if (!NetworkFloatHelper.TryDecompress(relativePosCompressed, out outValue, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
		}
		else if (!NetworkFloatHelper.TryDecompress(bulletVelocityCompressed, out outValue2, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
		}
		else if (weaponStationIndex >= weaponStations.Count || weaponStationIndex < 0)
		{
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			HitOnPhysicsFrame(unit, outValue, outValue2, weaponStationIndex).Forget();
		}
	}

	protected static void Skeleton_CmdClaimHit__002D1122942669(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdClaimHit__002D1122942669(GeneratedNetworkCode._Read_PersistentID(reader), GeneratedNetworkCode._Read_Vector3Compressed(reader), GeneratedNetworkCode._Read_Vector3Compressed(reader), reader.ReadByteExtension());
	}

	private void UserCode_CmdDamage__002D2104360672(byte partID, DamageInfo damageInfo)
	{
		if (!damageInfo.IsValid())
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
		}
		else if (partID >= damageables.Count)
		{
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcDamage(partID, damageInfo);
		}
	}

	protected static void Skeleton_CmdDamage__002D2104360672(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdDamage__002D2104360672(reader.ReadByteExtension(), GeneratedNetworkCode._Read_DamageInfo(reader));
	}

	public virtual void UserCode_RpcDamage_1709046923(byte index, DamageInfo damageInfo)
	{
		DamageablePart damageablePart = damageables[index];
		if (!damageablePart.Removed)
		{
			damageablePart.Damageable.ApplyDamage(damageInfo.pierceDamage.Decompress(), damageInfo.blastDamage.Decompress(), damageInfo.fireDamage.Decompress(), damageInfo.impactDamage.Decompress());
		}
	}

	protected static void Skeleton_RpcDamage_1709046923(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcDamage_1709046923(reader.ReadByteExtension(), GeneratedNetworkCode._Read_DamageInfo(reader));
	}

	private void UserCode_CmdFuelTankStatus_423953574(byte partID, bool ruptured, bool onFire)
	{
		if (partID >= damageables.Count)
		{
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcFuelTankStatus(partID, ruptured, onFire);
		}
	}

	protected static void Skeleton_CmdFuelTankStatus_423953574(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdFuelTankStatus_423953574(reader.ReadByteExtension(), reader.ReadBooleanExtension(), reader.ReadBooleanExtension());
	}

	public void UserCode_RpcFuelTankStatus_414334555(byte partID, bool ruptured, bool onFire)
	{
		DamageablePart damageablePart = damageables[partID];
		if (!damageablePart.Removed && damageablePart.Damageable.GetTransform().TryGetComponent<FuelTank>(out var component))
		{
			component.UpdateStatus(ruptured, onFire);
		}
	}

	protected static void Skeleton_RpcFuelTankStatus_414334555(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcFuelTankStatus_414334555(reader.ReadByteExtension(), reader.ReadBooleanExtension(), reader.ReadBooleanExtension());
	}

	private void UserCode_CmdBreakPart__002D789722400(byte partID, Vector3Compressed velocity, Vector3Compressed relativePos)
	{
		if (partID >= damageables.Count)
		{
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
			return;
		}
		if (!NetworkFloatHelper.Validate(velocity, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			velocity = Vector3Compressed.zero;
		}
		if (!NetworkFloatHelper.Validate(relativePos, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			relativePos = Vector3Compressed.zero;
		}
		RpcBreakPart(partID, velocity, relativePos);
	}

	protected static void Skeleton_CmdBreakPart__002D789722400(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdBreakPart__002D789722400(reader.ReadByteExtension(), GeneratedNetworkCode._Read_Vector3Compressed(reader), GeneratedNetworkCode._Read_Vector3Compressed(reader));
	}

	private void UserCode_RpcBreakPart__002D1087911989(byte partID, Vector3Compressed velocityCompressed, Vector3Compressed relativePosCompressed)
	{
		DamageablePart damageablePart = damageables[partID];
		if (!damageablePart.Removed)
		{
			IDamageable damageable = damageablePart.Damageable;
			Vector3 relativePos = NetworkFloatHelper.DecompressIfValid(relativePosCompressed, logErrors: true, "relativePosCompressed");
			Vector3 velocity = NetworkFloatHelper.DecompressIfValid(velocityCompressed, logErrors: true, "velocityCompressed");
			damageable.Detach(velocity, relativePos);
		}
	}

	protected static void Skeleton_RpcBreakPart__002D1087911989(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_RpcBreakPart__002D1087911989(reader.ReadByteExtension(), GeneratedNetworkCode._Read_Vector3Compressed(reader), GeneratedNetworkCode._Read_Vector3Compressed(reader));
	}

	public void UserCode_CmdDisableUnit__002D340399461()
	{
		ServerDisableUnit();
	}

	protected static void Skeleton_CmdDisableUnit__002D340399461(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Unit)behaviour).UserCode_CmdDisableUnit__002D340399461();
	}

	private UniTask<int[]> UserCode_CmdAskAmmoInternal_1214831455()
	{
		int count = weaponStations.Count;
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = weaponStations[i].Ammo;
		}
		return UniTask.FromResult(array);
	}

	protected static UniTask<int[]> Skeleton_CmdAskAmmoInternal_1214831455(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		return ((Unit)behaviour).UserCode_CmdAskAmmoInternal_1214831455();
	}

	private UniTask<int[]> UserCode_CmdAskFullAmmoInternal__002D1739804082()
	{
		int count = weaponStations.Count;
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = weaponStations[i].FullAmmo;
		}
		return UniTask.FromResult(array);
	}

	protected static UniTask<int[]> Skeleton_CmdAskFullAmmoInternal__002D1739804082(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		return ((Unit)behaviour).UserCode_CmdAskFullAmmoInternal__002D1739804082();
	}

	protected override int GetRpcCount()
	{
		return 21;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "Unit.RpcRearm", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcRearm_1512805087, RpcRateLimitConfig.Disabled());
		collection.Register(1, "Unit.RpcUpdateRearmerCapacity", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcUpdateRearmerCapacity_520309312, RpcRateLimitConfig.Disabled());
		collection.Register(2, "Unit.RpcJam", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcJam_569024588, RpcRateLimitConfig.Disabled());
		collection.Register(3, "Unit.RpcAssignRadar", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcAssignRadar__002D1001571913, RpcRateLimitConfig.Disabled());
		collection.Register(4, "Unit.CmdSetFiringState", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetFiringState_1458755207, RpcRateLimitConfig.Disabled());
		collection.Register(5, "Unit.CmdSingleRemoteFire", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSingleRemoteFire_1708169300, RpcRateLimitConfig.Enabled(1f, 20, 100, 1));
		collection.Register(6, "Unit.RpcSingleRemoteFire", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSingleRemoteFire__002D1161895954, RpcRateLimitConfig.Disabled());
		collection.Register(7, "Unit.CmdStoppedFiring", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdStoppedFiring__002D1238964004, RpcRateLimitConfig.Disabled());
		collection.Register(8, "Unit.RpcSyncAmmoCount", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSyncAmmoCount__002D1454761002, RpcRateLimitConfig.Disabled());
		collection.Register(9, "Unit.CmdSetStationTargets", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetStationTargets_872088460, RpcRateLimitConfig.Disabled());
		collection.Register(10, "Unit.RpcSetStationTargets", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSetStationTargets_1363862903, RpcRateLimitConfig.Disabled());
		collection.Register(11, "Unit.CmdClaimHit", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdClaimHit__002D1122942669, RpcRateLimitConfig.Enabled(1f, 20, 400, 5));
		collection.Register(12, "Unit.CmdDamage", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdDamage__002D2104360672, RpcRateLimitConfig.Enabled(1f, 20, 100, 1));
		collection.Register(13, "Unit.RpcDamage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcDamage_1709046923, RpcRateLimitConfig.Disabled());
		collection.Register(14, "Unit.CmdFuelTankStatus", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdFuelTankStatus_423953574, RpcRateLimitConfig.Disabled());
		collection.Register(15, "Unit.RpcFuelTankStatus", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcFuelTankStatus_414334555, RpcRateLimitConfig.Disabled());
		collection.Register(16, "Unit.CmdBreakPart", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdBreakPart__002D789722400, RpcRateLimitConfig.Enabled(1f, 10, 60, 2));
		collection.Register(17, "Unit.RpcBreakPart", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcBreakPart__002D1087911989, RpcRateLimitConfig.Disabled());
		collection.Register(18, "Unit.CmdDisableUnit", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdDisableUnit__002D340399461, RpcRateLimitConfig.Enabled(1f, 2, 10, 5));
		collection.RegisterRequest(19, "Unit.CmdAskAmmoInternal", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdAskAmmoInternal_1214831455, RpcRateLimitConfig.Enabled(1f, 5, 15, 2));
		collection.RegisterRequest(20, "Unit.CmdAskFullAmmoInternal", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdAskFullAmmoInternal__002D1739804082, RpcRateLimitConfig.Enabled(1f, 5, 15, 2));
	}
}
