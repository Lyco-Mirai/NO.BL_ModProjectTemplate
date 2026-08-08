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
using NuclearOption.DebugScripts;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.Social;
using Unity.Profiling;
using UnityEngine;

public class Aircraft : Unit, IRadarReturn, IRefuelable
{
	public struct OnFlightAssistToggle
	{
		public bool enabled;
	}

	public struct OnShake
	{
		public float lowFreqShake;

		public float highFreqShake;
	}

	public struct OnRadarWarning
	{
		public Radar radar;

		public Unit emitter;

		public float power;

		public bool detected;

		public bool isTarget;
	}

	public struct OnSetGear
	{
		public LandingGear.GearState gearState;
	}

	private class PartChecker
	{
		private List<AeroPart> parts;

		private int i;

		public PartChecker(Aircraft aircraft)
		{
			parts = aircraft.partsWithAero;
		}

		public void Check()
		{
			if (parts.Count != 0)
			{
				i++;
				if (i >= parts.Count)
				{
					i = 0;
				}
				parts[i].CheckAttachment();
			}
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CEjectionSequence_003Ed__207 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public AsyncUniTaskMethodBuilder _003C_003Et__builder;

		public Aircraft _003C_003E4__this;

		private CancellationToken _003Ccancel_003E5__2;

		private bool _003CusingEscapeCapsule_003E5__3;

		private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

		private UniTask.Awaiter _003C_003Eu__2;

		private int _003Ci_003E5__4;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			Aircraft aircraft = _003C_003E4__this;
			try
			{
				Cysharp.Threading.Tasks.YieldAwaitable.Awaiter awaiter2;
				UniTask.Awaiter awaiter;
				int num2;
				Pilot[] pilots;
				switch (num)
				{
				default:
					_003Ccancel_003E5__2 = aircraft.destroyCancellationToken;
					awaiter2 = UniTask.Yield(PlayerLoopTiming.FixedUpdate).GetAwaiter();
					if (!awaiter2.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter2;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
						return;
					}
					goto IL_0093;
				case 0:
					awaiter2 = _003C_003Eu__1;
					_003C_003Eu__1 = default(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_0093;
				case 1:
					awaiter = _003C_003Eu__2;
					_003C_003Eu__2 = default(UniTask.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_0186;
				case 2:
					awaiter = _003C_003Eu__2;
					_003C_003Eu__2 = default(UniTask.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_020e;
				case 3:
					awaiter2 = _003C_003Eu__1;
					_003C_003Eu__1 = default(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_02a1;
				case 4:
					awaiter = _003C_003Eu__2;
					_003C_003Eu__2 = default(UniTask.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_032b;
				case 5:
					{
						awaiter = _003C_003Eu__2;
						_003C_003Eu__2 = default(UniTask.Awaiter);
						num = (_003C_003E1__state = -1);
						goto IL_03d9;
					}
					IL_0186:
					awaiter.GetResult();
					goto IL_018d;
					IL_0093:
					awaiter2.GetResult();
					num2 = 0;
					pilots = aircraft.pilots;
					foreach (Pilot pilot in pilots)
					{
						if (!(pilot.currentState is PilotPlayerState))
						{
							pilot.SwitchState(null);
						}
						num2 += ((!pilot.dead) ? 1 : 0);
					}
					if (num2 != 0)
					{
						EscapeCapsule component = aircraft.cockpit.GetComponent<EscapeCapsule>();
						_003CusingEscapeCapsule_003E5__3 = false;
						if (component != null && !aircraft.IsLanded())
						{
							aircraft.RpcEscapeCapsule();
							_003CusingEscapeCapsule_003E5__3 = true;
							awaiter = UniTask.WaitForSeconds(1).GetAwaiter();
							if (!awaiter.IsCompleted)
							{
								num = (_003C_003E1__state = 1);
								_003C_003Eu__2 = awaiter;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
								return;
							}
							goto IL_0186;
						}
						goto IL_018d;
					}
					goto end_IL_000e;
					IL_018d:
					aircraft.RpcJettisonCanopy();
					awaiter = UniTask.Delay(aircraft.IsLanded() ? 1000 : 250).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 2);
						_003C_003Eu__2 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
					goto IL_020e;
					IL_02a1:
					awaiter2.GetResult();
					awaiter = UniTask.Delay((!aircraft.pilots[_003Ci_003E5__4].HasEjectionSeat()) ? 500 : 0).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 4);
						_003C_003Eu__2 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
					goto IL_032b;
					IL_0404:
					if (_003Ci_003E5__4 < 0)
					{
						break;
					}
					awaiter2 = UniTask.WaitForFixedUpdate().GetAwaiter();
					if (!awaiter2.IsCompleted)
					{
						num = (_003C_003E1__state = 3);
						_003C_003Eu__1 = awaiter2;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
						return;
					}
					goto IL_02a1;
					IL_032b:
					awaiter.GetResult();
					if (!aircraft.pilots[_003Ci_003E5__4].dead)
					{
						aircraft.SpawnEjectingPilot(_003Ci_003E5__4);
					}
					awaiter = UniTask.Delay(aircraft.pilots[_003Ci_003E5__4].HasEjectionSeat() ? 500 : 1000).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 5);
						_003C_003Eu__2 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
					goto IL_03d9;
					IL_03d9:
					awaiter.GetResult();
					if (!_003Ccancel_003E5__2.IsCancellationRequested)
					{
						_003Ci_003E5__4--;
						goto IL_0404;
					}
					goto end_IL_000e;
					IL_020e:
					awaiter.GetResult();
					if (!_003Ccancel_003E5__2.IsCancellationRequested)
					{
						if (_003CusingEscapeCapsule_003E5__3)
						{
							break;
						}
						_003Ci_003E5__4 = aircraft.pilots.Length - 1;
						goto IL_0404;
					}
					goto end_IL_000e;
				}
				if (aircraft.speed < 2f && aircraft.NetworkHQ != null && aircraft.NetworkHQ.AnyNearAirbase(aircraft.transform.position, out var _) && aircraft.transform.position.y > Datum.LocalSeaY)
				{
					aircraft.NetworkunitState = UnitState.Abandoned;
					aircraft.ReturnToInventory();
				}
				else if (!aircraft.disabled)
				{
					if (!_003CusingEscapeCapsule_003E5__3 && GameManager.IsLocalAircraft(aircraft))
					{
						aircraft.Player.RemoveAircraft(aircraft);
						aircraft.Player.ShowMap(5f);
						SceneSingleton<CombatHUD>.i.RemoveAircraft();
						if (aircraft.statusDisplay != null)
						{
							UnityEngine.Object.Destroy(aircraft.statusDisplay.gameObject);
						}
					}
					aircraft.DisableUnit();
				}
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

	private static readonly ProfilerMarker localSimFixedUpdateMarker = new ProfilerMarker("LocalSimFixedUpdate");

	[NonSerialized]
	[SyncVar]
	public PlayerRef playerRef;

	[NonSerialized]
	[SyncVar(hook = "onLoadoutChanged", hookType = SyncHookType.EventWith0Arg)]
	public Loadout loadout;

	[NonSerialized]
	[SyncVar]
	public NetworkBehaviorSyncvar spawningHangar;

	[NonSerialized]
	[SyncVar]
	public Vector3 startingVelocity;

	[NonSerialized]
	[SyncVar]
	private LiveryKey LiveryKey;

	[NonSerialized]
	[SyncVar]
	public float sortieScore;

	[NonSerialized]
	[SyncVar]
	public bool countermeasureTrigger;

	[NonSerialized]
	[SyncVar]
	public float fuelLevel;

	[NonSerialized]
	[SyncVar]
	public bool Ignition;

	[NonSerialized]
	[SyncVar(hook = "GearStateChangedHook")]
	public bool gearDeployed;

	public TargetDetector EOTS;

	public Pilot[] pilots;

	private bool hasRequestedRearm;

	public TargetCam targetCam;

	public WeaponManager weaponManager;

	public CountermeasureManager countermeasureManager;

	public AudioClip scrapeSound;

	public UnitPart cockpit;

	[SerializeField]
	private ControlsFilter controlsFilter;

	[SerializeField]
	private RelaxedStabilityController relaxedStabilityController;

	public bool flightAssist = true;

	private TargetDetector defaultRadar;

	[SerializeField]
	private PowerSupply powerSupply;

	[SerializeField]
	private Canopy[] canopies;

	[SerializeField]
	private Renderer[] cockpitRenderers;

	[SerializeField]
	private Renderer[] exteriorRenderers;

	[SerializeField]
	private ParticleSystem sparksEmitter;

	[SerializeField]
	private GameObject[] groundEquipment;

	[HideInInspector]
	public Autopilot autopilot;

	[HideInInspector]
	public LandingGear.GearState gearState;

	[HideInInspector]
	public Vector3 accel = Vector3.zero;

	[HideInInspector]
	public Vector3 velocityPrev = Vector3.zero;

	private Vector3 windVelocity;

	[HideInInspector]
	public float gForce;

	[HideInInspector]
	public bool simplePhysics;

	[HideInInspector]
	public float skill = 1f;

	[HideInInspector]
	public float bravery = 0.5f;

	private StatusDisplay statusDisplay;

	private readonly ControlInputs controlInputs = new ControlInputs();

	private List<Unit> knownRadarSources = new List<Unit>();

	private List<TargetDetector> targetDetectors = new List<TargetDetector>();

	public List<IEngine> engineStates = new List<IEngine>();

	public List<IEngine> engines = new List<IEngine>();

	private List<FuelTank> fuelTanks = new List<FuelTank>();

	private MissileWarning missileWarning;

	private LaserDesignator laserDesignator;

	private bool needsFuel;

	private bool ejected;

	private bool airborne;

	[SerializeField]
	private float ecmIntensity;

	[SerializeField]
	private float fuelCapacity;

	private List<AeroPart> partsWithAero = new List<AeroPart>();

	public PartDamageTracker partDamageTracker;

	private ParticleSystem.EmitParams emitParams;

	private AudioSource scrapeSource;

	private Vector3 smoothingVel;

	private Vector3 rotationSmoothingVel;

	private bool spawnedInPosition;

	private PartChecker partChecker;

	private LiveryBehaviour liveryBehaviour;

	private GameObject CoMDebug;

	private DebugControlInputsDisplay debugControlInputsDisplay;

	private readonly List<ControlSurface> controlSurfaces = new List<ControlSurface>();

	private Unit slungUnit;

	private bool initializedLocalSim;

	private NavLights navLights;

	private int inputIntervalCounter;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 19;

	[NonSerialized]
	private const int RPC_COUNT = 45;

	public Player Player => playerRef.Player;

	public new AircraftDefinition definition => (AircraftDefinition)base.definition;

	public PlayerRef NetworkplayerRef
	{
		get
		{
			return playerRef;
		}
		set
		{
			if (!SyncVarEqual(value, this.playerRef))
			{
				PlayerRef playerRef = this.playerRef;
				this.playerRef = value;
				SetDirtyBit(512uL);
			}
		}
	}

	public Loadout Networkloadout
	{
		get
		{
			return loadout;
		}
		set
		{
			if (!SyncVarEqual(value, this.loadout))
			{
				Loadout loadout = this.loadout;
				this.loadout = value;
				SetDirtyBit(1024uL);
				if (!GetSyncVarHookGuard(1024uL) && base.IsHost)
				{
					SetSyncVarHookGuard(1024uL, value: true);
					this.onLoadoutChanged?.Invoke();
					SetSyncVarHookGuard(1024uL, value: false);
				}
			}
		}
	}

	public Hangar NetworkspawningHangar
	{
		get
		{
			return (Hangar)spawningHangar.Value;
		}
		set
		{
			if (!SyncVarEqual(value, (Hangar)spawningHangar.Value))
			{
				Hangar hangar = (Hangar)spawningHangar.Value;
				spawningHangar.Value = value;
				SetDirtyBit(2048uL);
			}
		}
	}

	public Vector3 NetworkstartingVelocity
	{
		get
		{
			return startingVelocity;
		}
		set
		{
			if (!SyncVarEqual(value, startingVelocity))
			{
				Vector3 vector = startingVelocity;
				startingVelocity = value;
				SetDirtyBit(4096uL);
			}
		}
	}

	public LiveryKey NetworkLiveryKey
	{
		get
		{
			return LiveryKey;
		}
		set
		{
			if (!SyncVarEqual(value, LiveryKey))
			{
				LiveryKey liveryKey = LiveryKey;
				LiveryKey = value;
				SetDirtyBit(8192uL);
			}
		}
	}

	public float NetworksortieScore
	{
		get
		{
			return sortieScore;
		}
		set
		{
			if (!SyncVarEqual(value, sortieScore))
			{
				float num = sortieScore;
				sortieScore = value;
				SetDirtyBit(16384uL);
			}
		}
	}

	public bool NetworkcountermeasureTrigger
	{
		get
		{
			return countermeasureTrigger;
		}
		set
		{
			if (!SyncVarEqual(value, countermeasureTrigger))
			{
				bool flag = countermeasureTrigger;
				countermeasureTrigger = value;
				SetDirtyBit(32768uL);
			}
		}
	}

	public float NetworkfuelLevel
	{
		get
		{
			return fuelLevel;
		}
		set
		{
			if (!SyncVarEqual(value, fuelLevel))
			{
				float num = fuelLevel;
				fuelLevel = value;
				SetDirtyBit(65536uL);
			}
		}
	}

	public bool NetworkIgnition
	{
		get
		{
			return Ignition;
		}
		set
		{
			if (!SyncVarEqual(value, Ignition))
			{
				bool ignition = Ignition;
				Ignition = value;
				SetDirtyBit(131072uL);
			}
		}
	}

	public bool NetworkgearDeployed
	{
		get
		{
			return gearDeployed;
		}
		set
		{
			if (!SyncVarEqual(value, gearDeployed))
			{
				bool _ = gearDeployed;
				gearDeployed = value;
				SetDirtyBit(262144uL);
				if (!GetSyncVarHookGuard(262144uL) && base.IsHost)
				{
					SetSyncVarHookGuard(262144uL, value: true);
					GearStateChangedHook(_, value);
					SetSyncVarHookGuard(262144uL, value: false);
				}
			}
		}
	}

	public event Action onLoadoutChanged;

	public event Action onSpawnedInPosition;

	public event Action onEject;

	public event Action OnTouchdown;

	public event Action<float> onSortieSuccessful;

	public event Action<OnSetGear> onSetGear;

	public event Action<OnFlightAssistToggle> onSetFlightAssist;

	public event Action<OnShake> onShake;

	public event Action<OnRadarWarning> onRadarWarning;

	public bool KnownRadarWarning(Unit radarSource)
	{
		if (knownRadarSources.Contains(radarSource))
		{
			return true;
		}
		knownRadarSources.Add(radarSource);
		return false;
	}

	public MissileWarning GetMissileWarningSystem()
	{
		return missileWarning;
	}

	public bool TryGetLiveryBehaviour(out LiveryBehaviour liveryBehaviour)
	{
		liveryBehaviour = this.liveryBehaviour;
		return liveryBehaviour != null;
	}

	public void SetLaserDesignator(LaserDesignator laserDesignator)
	{
		this.laserDesignator = laserDesignator;
	}

	public LaserDesignator GetLaserDesignator()
	{
		return laserDesignator;
	}

	public void LockedByMissile(Missile missile)
	{
		if (!disabled)
		{
			missileWarning.LockedByMissile(this, missile);
		}
	}

	[ClientRpc]
	public void RpcGetRadarWarning(Unit emitter)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcGetRadarWarning__002D1586111906(emitter);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit(writer, emitter);
		ClientRpcSender.Send(this, 21, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public float GetRadarReturn(Vector3 source, Radar radar, Unit emitter, float dist, float clutter, RadarParams radarParams, bool triggerWarning)
	{
		Vector3 direction = FastMath.NormalizedDirection(source, base.transform.position);
		if (base.IsServer && triggerWarning)
		{
			RpcGetRadarWarning(emitter);
		}
		return radarParams.GetSignalStrength(direction, dist, base.rb, RCS, clutter, ecmIntensity);
	}

	public bool EstimateDetection(Radar radar, out float returnSignal)
	{
		returnSignal = 0f;
		if (radar == null)
		{
			return false;
		}
		float num = FastMath.Distance(radar.transform.position, base.transform.position);
		float maxRange = radar.RadarParameters.maxRange;
		float maxSignal = radar.RadarParameters.maxSignal;
		float minSignal = radar.RadarParameters.minSignal;
		float num2 = maxRadius * maxRadius * 2f / (radarAlt * radarAlt);
		float num3 = maxRange / num;
		returnSignal = num3 * Mathf.Pow(RCS, 0.25f);
		returnSignal = Mathf.Min(returnSignal, maxSignal);
		returnSignal -= num2 * 0.15f;
		returnSignal = Mathf.Max(returnSignal, 0f);
		return returnSignal > minSignal;
	}

	[RateLimit(Refill = 10, MaxTokens = 30, Penalty = 1)]
	[ServerRpc]
	public void CmdToggleRadar()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdToggleRadar_1821461427();
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ServerRpcSender.Send(this, 22, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	private void RpcToggleRadar(bool activated)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcToggleRadar_1325449311(activated);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBooleanExtension(activated);
		ClientRpcSender.Send(this, 23, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[RateLimit(Refill = 10, MaxTokens = 30, Penalty = 1)]
	[ServerRpc]
	public void CmdToggleIgnition()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdToggleIgnition__002D1067807998();
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ServerRpcSender.Send(this, 24, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	public float GetECMIntensity()
	{
		return ecmIntensity;
	}

	public void CheckNeedsFuel(float fuelRatio)
	{
		if (networked && base.LocalSim && !needsFuel && fuelRatio < fuelLevel * 0.9f)
		{
			needsFuel = true;
			if (!base.IsServer)
			{
				CmdSetCanRefuel();
			}
		}
	}

	[RateLimit(Refill = 10, MaxTokens = 30, Penalty = 1)]
	[ServerRpc]
	private void CmdSetCanRefuel()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetCanRefuel__002D170919880();
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ServerRpcSender.Send(this, 25, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	public void SetSlingLoadAttachment(Unit slingLoadUnit, SlingloadHook.DeployState state)
	{
		if (base.LocalSim)
		{
			ApplySlingLoadAttachment(slingLoadUnit, state);
		}
		if (base.IsServer)
		{
			RpcSlingLoadAttachment(slingLoadUnit, state);
		}
		else if (base.HasAuthority)
		{
			CmdSlingLoadAttachment(slingLoadUnit, state);
		}
	}

	[RateLimit(Refill = 5, MaxTokens = 15, Penalty = 3)]
	[ServerRpc]
	public void CmdSlingLoadAttachment(Unit slingLoadUnit, SlingloadHook.DeployState state)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSlingLoadAttachment__002D82313074(slingLoadUnit, state);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit(writer, slingLoadUnit);
		GeneratedNetworkCode._Write_SlingloadHook_002FDeployState(writer, state);
		ServerRpcSender.Send(this, 26, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc(excludeOwner = true)]
	public void RpcSlingLoadAttachment(Unit slingLoadUnit, SlingloadHook.DeployState state)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: true))
		{
			UserCode_RpcSlingLoadAttachment__002D57940477(slingLoadUnit, state);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit(writer, slingLoadUnit);
		GeneratedNetworkCode._Write_SlingloadHook_002FDeployState(writer, state);
		ClientRpcSender.Send(this, 27, writer, Mirage.Channel.Reliable, excludeOwner: true);
		writer.Release();
	}

	private void ApplySlingLoadAttachment(Unit slingLoadUnit, SlingloadHook.DeployState state)
	{
		bool flag = state == SlingloadHook.DeployState.Connected || state == SlingloadHook.DeployState.RescuePilot;
		if (slingLoadUnit != null)
		{
			slingLoadUnit.AttachOrDetachSlingHook(this, flag);
		}
		slungUnit = (flag ? slingLoadUnit : null);
		foreach (WeaponStation weaponStation in weaponStations)
		{
			if (weaponStation.WeaponInfo.sling && weaponStation.Weapons[0] is SlingloadHook slingloadHook)
			{
				slingloadHook.ApplyState(slingLoadUnit, state);
			}
		}
	}

	[RateLimit(Refill = 25, MaxTokens = 100, Penalty = 1)]
	[ServerRpc]
	public void CmdSendSlungTransform(Vector3 position, Quaternion rotation)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSendSlungTransform__002D695501160(position, rotation);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteVector3(position);
		writer.WriteQuaternion(rotation);
		ServerRpcSender.Send(this, 28, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	public bool CanRefuel()
	{
		if (!needsFuel)
		{
			return false;
		}
		if (radarAlt <= 5f)
		{
			return speed <= 1f;
		}
		return false;
	}

	public void RegisterFuelTank(FuelTank tank)
	{
		fuelTanks.Add(tank);
	}

	public List<FuelTank> GetFuelTanks()
	{
		return fuelTanks;
	}

	public void RecalcFuelCapacity()
	{
		fuelCapacity = 0f;
		foreach (FuelTank fuelTank in fuelTanks)
		{
			fuelCapacity += fuelTank.GetCapacity();
		}
	}

	public bool UseFuel(float fuelDrawn)
	{
		float num = 0f;
		foreach (FuelTank fuelTank in fuelTanks)
		{
			num += fuelTank.fuelMass;
		}
		float fuelRatio = num / fuelCapacity;
		if (num <= 0f)
		{
			CheckNeedsFuel(0f);
			return false;
		}
		foreach (FuelTank fuelTank2 in fuelTanks)
		{
			fuelTank2.UseFuel(fuelDrawn * (fuelTank2.fuelMass / num));
		}
		CheckNeedsFuel(fuelRatio);
		return true;
	}

	public bool GetMaxThrust(out float maxThrust)
	{
		maxThrust = 0f;
		if (base.gameObject.TryGetComponent<DuctedThrustSystem>(out var component))
		{
			maxThrust = component.GetMaxThrust();
			return true;
		}
		foreach (IEngine engineState in engineStates)
		{
			if (engineState is IThrustSource thrustSource)
			{
				maxThrust += thrustSource.GetMaxThrust();
			}
		}
		return maxThrust > 0f;
	}

	public bool GetMaxPower(out float maxPower)
	{
		maxPower = 0f;
		foreach (IEngine engineState in engineStates)
		{
			if (engineState is IPowerSource powerSource)
			{
				maxPower += powerSource.GetMaxPower();
			}
		}
		return maxPower > 0f;
	}

	public float GetFuelLevel()
	{
		float num = 0f;
		float num2 = 0f;
		foreach (FuelTank fuelTank in fuelTanks)
		{
			num += fuelTank.GetCapacity();
			num2 += fuelTank.GetLevel();
		}
		if (num == 0f)
		{
			return 0f;
		}
		return num2 / num;
	}

	public float GetFuelQuantity()
	{
		float num = 0f;
		foreach (FuelTank fuelTank in fuelTanks)
		{
			num += fuelTank.GetLevel();
		}
		return num;
	}

	public override Vector3 GetWindVelocity()
	{
		return windVelocity;
	}

	public override float GetAirDensity()
	{
		return airDensity;
	}

	public void Refuel(Unit refueler)
	{
		if (networked)
		{
			RpcRefuel(refueler);
		}
		else
		{
			SetFuelLevel(refueler);
		}
	}

	[ClientRpc]
	private void RpcRefuel(Unit refueler)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcRefuel_1587114129(refueler);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit(writer, refueler);
		ClientRpcSender.Send(this, 29, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private void SetFuelLevel(Unit refueler)
	{
		bool flag = false;
		foreach (FuelTank fuelTank in fuelTanks)
		{
			if (!fuelTank.Refuel(fuelLevel))
			{
				flag = true;
			}
		}
		if (PlayerSettings.debugVis)
		{
			DebugCoM();
		}
		if (GameManager.IsLocalAircraft(this) && refueler != null)
		{
			if (flag)
			{
				SceneSingleton<AircraftActionsReport>.i.ReportText("<b>Fuel leak detected; unable to refuel</b>", 5f);
				return;
			}
			SceneSingleton<AircraftActionsReport>.i.ReportText("Refueled by " + refueler.unitName, 5f);
		}
		needsFuel = false;
	}

	public void AddControlSurface(ControlSurface controlSurface)
	{
		controlSurfaces.Add(controlSurface);
	}

	public override void Awake()
	{
		base.Awake();
		base.Identity.OnStartClient.AddListener(OnStartClient);
		base.Identity.OnStartServer.AddListener(OnStartServer);
		base.Identity.OnAuthorityChanged.AddListener(OnAuthorityChanged);
	}

	private void OnAuthorityChanged(bool hasAuthority)
	{
		if (!base.IsServer && base.LocalSim && !hasAuthority)
		{
			ColorLog<Aircraft>.Info($"Authority removed from {this} disabling local sim");
			SetLocalSim(localSim: false);
		}
	}

	public void DebugCoM()
	{
		if (CoMDebug != null)
		{
			UnityEngine.Object.Destroy(CoMDebug);
		}
		CoMDebug = UnityEngine.Object.Instantiate(GameAssets.i.debugArrow, GetCenterOfMass(), Quaternion.LookRotation(Vector3.up));
		CoMDebug.transform.localScale = new Vector3(1f, 1f, 5f);
		CoMDebug.transform.SetParent(base.transform);
	}

	private void OnStartServer()
	{
		RegisterUnit(1f);
		base.rb.velocity = startingVelocity;
		if (Player != null)
		{
			Player.SetAircraft(this);
			Player.SetFaction(base.NetworkHQ);
		}
		if (networked)
		{
			NetworkIgnition = true;
		}
	}

	private void OnStartClient()
	{
		defaultRadar = radar;
		gearState = LandingGear.GearState.Uninitialized;
		partDamageTracker = new PartDamageTracker(this);
		if (playerRef.Valid())
		{
			SetupUnitName().Forget();
		}
		if (!base.IsServer)
		{
			if (NetworkspawningHangar == null)
			{
				base.transform.SetPositionAndRotation(startPosition.ToLocalPosition(), startRotation);
				base.rb.MovePosition(startPosition.ToLocalPosition());
				base.rb.MoveRotation(startRotation);
				base.rb.velocity = startingVelocity;
			}
			else
			{
				Transform spawnTransform = NetworkspawningHangar.GetSpawnTransform();
				base.transform.SetPositionAndRotation(spawnTransform.position + spawnTransform.up * definition.spawnOffset.y + spawnTransform.forward * definition.spawnOffset.z, spawnTransform.rotation);
				base.rb.MovePosition(base.transform.position);
				base.rb.MoveRotation(base.transform.rotation);
				base.rb.velocity = NetworkspawningHangar.GetVelocity();
				base.rb.angularVelocity = NetworkspawningHangar.GetAngularVelocity();
			}
			RegisterUnit(1f);
		}
		missileWarning = base.gameObject.AddComponent<MissileWarning>();
		for (byte b = 0; b < pilots.Length; b++)
		{
			pilots[b].AssignPilotNumber(b);
		}
		SetLocalSim(CheckIfLocalSim());
		if (Player != null)
		{
			if (!base.IsServer)
			{
				Player.SetAircraft(this);
			}
			if (Player.IsLocalPlayer)
			{
				SceneSingleton<DynamicMap>.i.SetFaction(base.NetworkHQ);
				SceneSingleton<CombatHUD>.i.SetAircraft(this);
				SceneSingleton<DynamicMap>.i.DeselectAllIcons();
				RichPresenceManager.SetAircraft(definition.unitName, PresenceActivity.Flying);
			}
		}
		else
		{
			_ = base.remoteSim;
		}
		if (loadout == null || loadout.weapons.Count == 0)
		{
			Networkloadout = definition.aircraftParameters.loadouts[1];
		}
		SetLiveryKey(LiveryKey);
		TogglePitchLimiter();
		this.StartSlowUpdateDelayed(0.1f, CheckRadarAlt);
		if (base.LocalSim)
		{
			SpawnedInPosition();
			if (radarAlt > definition.spawnOffset.y + 1f)
			{
				controlInputs.throttle = 0.6f;
				SetGear(deployed: false);
				GearStateChanged(gearDeployed: false);
			}
			else
			{
				SetGear(deployed: true);
			}
			partChecker = new PartChecker(this);
			if (GameManager.gameState == GameState.Editor || GameManager.gameState == GameState.Encyclopedia)
			{
				return;
			}
			if (Player != null)
			{
				SetupLocalPlayerAndUI();
			}
		}
		else
		{
			GearStateChanged(gearDeployed);
		}
		SetFuelLevel(null);
		base.InitializeUnit();
		if (base.NetworkHQ != null && base.NetworkHQ.TryGetNearestAirbase(base.transform.position, out var nearestAirbase))
		{
			NetworkSceneSingleton<MessageManager>.i.AircraftDeployedMessage(this, nearestAirbase, playersOnly: true);
		}
	}

	private async UniTask SetupUnitName()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		while (playerRef.Player == null)
		{
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				return;
			}
		}
		playerRef.Player.OnNameResolved.AddListener(OwnerNameResolved);
	}

	private void OwnerNameResolved(PlayerName playerName)
	{
		if (playerRef.Player != null)
		{
			playerRef.Player.OnNameResolved.RemoveListener(OwnerNameResolved);
		}
		base.NetworkunitName = playerName.GetDisplayName(PlayerNameContext.Other) + " [" + definition.unitName + "]";
		if (UnitRegistry.TryGetPersistentUnit(persistentID, out var persistentUnit))
		{
			persistentUnit.unitName = unitName;
		}
	}

	private void SetupLocalPlayerAndUI()
	{
		_ = base.IsServer;
		Player.AttachToAircraft(this);
		pilots[0].SwitchState(pilots[0].playerState);
		AircraftParameters aircraftParameters = GetAircraftParameters();
		GameObject gameObject = UnityEngine.Object.Instantiate(aircraftParameters.StatusDisplay, Vector3.zero, Quaternion.identity);
		if (aircraftParameters.HUDExtras != null)
		{
			UnityEngine.Object.Instantiate(aircraftParameters.HUDExtras, SceneSingleton<FlightHud>.i.GetHUDCenter());
		}
		statusDisplay = gameObject.GetComponent<StatusDisplay>();
		statusDisplay.Initialize(this);
		SceneSingleton<CameraStateManager>.i.SetFollowingUnit(this);
		SceneSingleton<CameraStateManager>.i.SwitchState(SceneSingleton<CameraStateManager>.i.cockpitState);
		SceneSingleton<DynamicMap>.i.Maximize();
		SceneSingleton<DynamicMap>.i.Minimize();
		DynamicMap.EnableCanvas(enable: true);
	}

	public override void SetLocalSim(bool localSim)
	{
		if (initializedLocalSim && !base.LocalSim && localSim)
		{
			ColorLog<Aircraft>.LogError("Invalid transition: Cannot transition from remote simulation to local simulation after spawn!");
		}
		initializedLocalSim = true;
		bool num = base.LocalSim && !localSim;
		base.SetLocalSim(localSim);
		if (localSim)
		{
			SetComplexPhysics();
		}
		else
		{
			SetSimplePhysics();
		}
		base.rb.interpolation = (localSim ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
		base.rb.useGravity = localSim;
		if (!num)
		{
			return;
		}
		foreach (AeroPart item in partsWithAero)
		{
			item.UnregisterFromJob();
		}
	}

	public void RegisterAeroPart(AeroPart aeroPart)
	{
		partsWithAero.Add(aeroPart);
	}

	public void DeregisterAeroPart(AeroPart aeroPart)
	{
		partsWithAero.Remove(aeroPart);
		partLookup.Remove(aeroPart);
		UnityEngine.Debug.Log("Deregistering aeroPart " + aeroPart.gameObject.name);
	}

	private bool CheckIfLocalSim()
	{
		if (Player != null)
		{
			return Player.IsLocalPlayer;
		}
		if (GameManager.gameState == GameState.Editor)
		{
			return false;
		}
		return NetworkManagerNuclearOption.i.Server.Active;
	}

	public void SetComplexPhysics()
	{
		foreach (UnitPart item in partLookup)
		{
			(item as AeroPart).CreateRB(base.rb.GetPointVelocity(item.transform.position), Vector3.zero);
		}
		foreach (UnitPart item2 in partLookup)
		{
			(item2 as AeroPart).CreateJoints();
		}
		simplePhysics = false;
		base.rb.ResetCenterOfMass();
		if (base.rb != null && !base.rb.isKinematic && (playerRef.Valid() || PlayerSettings.debugVis))
		{
			base.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}
	}

	public void SetSimplePhysics()
	{
		foreach (UnitPart item in partLookup)
		{
			(item as AeroPart).MergeWithParent();
		}
		base.rb.mass = definition.mass;
		base.rb.ResetCenterOfMass();
		base.rb.ResetInertiaTensor();
		simplePhysics = true;
		if (base.rb != null)
		{
			base.rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		}
	}

	public Vector3 CalcCenterOfMass()
	{
		Vector3 zero = Vector3.zero;
		float num = 0f;
		foreach (UnitPart item in partLookup)
		{
			if (!item.IsDetached())
			{
				num += item.mass;
				zero += item.transform.position * item.mass;
			}
		}
		return zero / num;
	}

	public void SetLiveryKey(LiveryKey liveryKey, bool loadIfUnspawned = false)
	{
		NetworkLiveryKey = liveryKey;
		if (!GameManager.IsHeadless && (loadIfUnspawned || base.IsClient))
		{
			if ((object)liveryBehaviour == null && !TryGetComponent<LiveryBehaviour>(out liveryBehaviour))
			{
				liveryBehaviour = base.gameObject.AddComponent<LiveryBehaviour>();
				int firstLiveryForFaction = definition.aircraftParameters.GetFirstLiveryForFaction((base.NetworkHQ != null) ? base.NetworkHQ.faction : null);
				liveryBehaviour.Setup(this, new LiveryKey(firstLiveryForFaction));
			}
			liveryBehaviour.SetKey(liveryKey);
		}
	}

	public LiveryBehaviour GetLiveryBehaviour()
	{
		return liveryBehaviour;
	}

	public void CheckSpawnedInPosition()
	{
		if (!spawnedInPosition)
		{
			SpawnedInPosition();
		}
	}

	public void SpawnedInPosition()
	{
		spawnedInPosition = true;
		speed = base.rb.velocity.magnitude;
		radarAlt = (base.transform.position - Datum.origin.position).y - definition.spawnOffset.y;
		if (Physics.Linecast(base.transform.position, base.transform.position - Vector3.up * 10000f, out hit, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
		{
			radarAlt = hit.distance - definition.spawnOffset.y;
		}
		this.onSpawnedInPosition?.Invoke();
		RecalcFuelCapacity();
	}

	public void SetPreview()
	{
		OnStartClient();
	}

	public void SetCockpitRenderers(bool enabled)
	{
		for (int i = 0; i < cockpitRenderers.Length; i++)
		{
			cockpitRenderers[i].enabled = enabled;
		}
		for (int j = 0; j < exteriorRenderers.Length; j++)
		{
			exteriorRenderers[j].enabled = !enabled;
		}
		foreach (IEngine engine in engines)
		{
			engine.SetInteriorSounds(enabled);
		}
	}

	public void ThrowSparks(Vector3 position, Vector3 extraVelocity)
	{
		if (scrapeSource == null)
		{
			GameObject gameObject = new GameObject("scrapes");
			gameObject.transform.SetParent(base.transform);
			scrapeSource = gameObject.AddComponent<AudioSource>();
			scrapeSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			scrapeSource.spatialBlend = 1f;
			scrapeSource.dopplerLevel = 0f;
			scrapeSource.minDistance = 20f;
			scrapeSource.maxDistance = 100f;
			scrapeSource.rolloffMode = AudioRolloffMode.Linear;
			scrapeSource.clip = scrapeSound;
			scrapeSource.loop = true;
		}
		if (!scrapeSource.isPlaying)
		{
			scrapeSource.time = UnityEngine.Random.value * scrapeSound.length;
			scrapeSource.Play();
		}
		if (!(base.rb == null))
		{
			scrapeSource.transform.position = Vector3.Lerp(scrapeSource.transform.position, position, 5f * Time.deltaTime);
			scrapeSource.volume += 20f * Time.deltaTime;
			scrapeSource.volume = Mathf.Clamp01(scrapeSource.volume);
			emitParams.position = position.ToGlobalPosition().AsVector3();
			emitParams.velocity = base.rb.velocity + extraVelocity + Vector3.up * UnityEngine.Random.Range(0, 5) + Vector3.right * UnityEngine.Random.Range(-3, 3) + Vector3.forward * UnityEngine.Random.Range(-3, 3);
			sparksEmitter.Emit(emitParams, 1);
		}
	}

	[ClientRpc]
	public override void RpcDamage(byte index, DamageInfo damageInfo)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcDamage_870168505(index, damageInfo);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(index);
		GeneratedNetworkCode._Write_DamageInfo(writer, damageInfo);
		ClientRpcSender.Send(this, 30, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void ShakeAircraft(float lowFreqShake, float highFreqShake)
	{
		this.onShake?.Invoke(new OnShake
		{
			lowFreqShake = lowFreqShake,
			highFreqShake = highFreqShake
		});
	}

	[RateLimit(Refill = 15, MaxTokens = 45, Penalty = 2)]
	[ServerRpc]
	public void CmdLaunchMissile(byte stationIndex, Unit target, GlobalPosition aimpoint)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdLaunchMissile_644415535(stationIndex, target, aimpoint);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		GeneratedNetworkCode._Write_Unit(writer, target);
		writer.WriteGlobalPosition(aimpoint);
		ServerRpcSender.Send(this, 31, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc(excludeOwner = true)]
	public void RpcLaunchMissile(byte stationIndex, Unit target, GlobalPosition aimpoint)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: true))
		{
			UserCode_RpcLaunchMissile_1465828762(stationIndex, target, aimpoint);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		GeneratedNetworkCode._Write_Unit(writer, target);
		writer.WriteGlobalPosition(aimpoint);
		ClientRpcSender.Send(this, 32, writer, Mirage.Channel.Reliable, excludeOwner: true);
		writer.Release();
	}

	public Rigidbody CockpitRB()
	{
		if (cockpit.rb != null)
		{
			return cockpit.rb;
		}
		return base.rb;
	}

	public AircraftParameters GetAircraftParameters()
	{
		return definition.aircraftParameters;
	}

	public bool IsLanded()
	{
		if (radarAlt < 5f)
		{
			return speed < 2.5f;
		}
		return false;
	}

	public void OpenCanopies()
	{
		Canopy[] array = canopies;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OpenHinges();
		}
	}

	public void ShowGroundEquipment()
	{
		GameObject[] array = groundEquipment;
		foreach (GameObject gameObject in array)
		{
			gameObject.SetActive(value: true);
			if (gameObject.transform.parent == base.transform && Physics.Linecast(gameObject.transform.position + gameObject.transform.up, gameObject.transform.position - gameObject.transform.up, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
			{
				gameObject.transform.SetPositionAndRotation(hitInfo.point, Quaternion.LookRotation(gameObject.transform.forward, hitInfo.normal));
				gameObject.transform.SetParent(hitInfo.collider.transform);
			}
		}
	}

	public ControlInputs GetInputs()
	{
		return controlInputs;
	}

	public ControlsFilter GetControlsFilter()
	{
		return controlsFilter;
	}

	public override PowerSupply GetPowerSupply()
	{
		return powerSupply;
	}

	public void FilterInputs()
	{
		Vector3 rawInputs = new Vector3(controlInputs.pitch, controlInputs.yaw, controlInputs.roll);
		if (relaxedStabilityController != null)
		{
			relaxedStabilityController.FilterInput(controlInputs, CockpitRB(), gForce, rawInputs.x);
		}
		if (controlsFilter != null)
		{
			controlsFilter.Filter(controlInputs, rawInputs, CockpitRB(), gForce, flightAssist);
		}
	}

	public void SetFlightAssist(bool enabled)
	{
		controlsFilter.SetFlightAssist(enabled, this);
		if (!controlsFilter.HasFlightAssist())
		{
			flightAssist = true;
			return;
		}
		flightAssist = enabled;
		this.onSetFlightAssist?.Invoke(new OnFlightAssistToggle
		{
			enabled = enabled
		});
	}

	public void SetFlightAssistToDefault()
	{
		bool flag = controlsFilter.FlightAssistDefault();
		controlsFilter.SetFlightAssist(flag, this);
		if (controlsFilter.HasFlightAssist())
		{
			this.onSetFlightAssist?.Invoke(new OnFlightAssistToggle
			{
				enabled = flag
			});
		}
	}

	public bool IsAutoHoverEnabled()
	{
		return controlsFilter.IsAutoHoverEnabled();
	}

	public void TogglePitchLimiter()
	{
		if (GameManager.gameState == GameState.SinglePlayer || GameManager.gameState == GameState.Multiplayer)
		{
			controlsFilter.SetFlightAssist(!flightAssist, this);
			if (controlsFilter.HasFlightAssist())
			{
				flightAssist = !flightAssist;
				this.onSetFlightAssist?.Invoke(new OnFlightAssistToggle
				{
					enabled = flightAssist
				});
			}
		}
	}

	public void ToggleNavLights()
	{
		if (GameManager.gameState == GameState.SinglePlayer || GameManager.gameState == GameState.Multiplayer)
		{
			if (navLights == null)
			{
				navLights = base.transform.GetComponentInChildren<NavLights>();
			}
			navLights.ToggleNavLights();
		}
	}

	public void SetRadar(Radar radar)
	{
		if (base.radar != null)
		{
			base.radar.activated = false;
		}
		base.radar = ((radar != null) ? radar : defaultRadar);
	}

	public void Countermeasures(bool active, byte index)
	{
		countermeasureManager.activeIndex = index;
		if (base.IsServer)
		{
			RpcCountermeasures(index);
			NetworkcountermeasureTrigger = active;
		}
		else if (base.HasAuthority)
		{
			CmdCountermeasures(active, index);
		}
	}

	[ServerRpc]
	private void CmdCountermeasures(bool active, byte index)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdCountermeasures__002D1391811506(active, index);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBooleanExtension(active);
		writer.WriteByteExtension(index);
		ServerRpcSender.Send(this, 33, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	private void RpcCountermeasures(byte index)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcCountermeasures_1466036644(index);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(index);
		ClientRpcSender.Send(this, 34, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void SetGear(bool deployed)
	{
		NetworkgearDeployed = deployed;
		GearStateChanged(deployed);
		if (base.HasAuthority && !base.IsServer)
		{
			CmdSetGear(deployed);
		}
	}

	[RateLimit(Refill = 5, MaxTokens = 15, Penalty = 3)]
	[ServerRpc]
	private void CmdSetGear(bool deployed)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetGear__002D1969689879(deployed);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteBooleanExtension(deployed);
		ServerRpcSender.Send(this, 35, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	private void GearStateChangedHook(bool _, bool newValue)
	{
		GearStateChanged(newValue);
	}

	private void GearStateChanged(bool gearDeployed)
	{
		if (gearState == LandingGear.GearState.Extending || gearState == LandingGear.GearState.Retracting)
		{
			return;
		}
		if (gearState == LandingGear.GearState.Uninitialized)
		{
			SetGear((!gearDeployed) ? LandingGear.GearState.LockedRetracted : LandingGear.GearState.LockedExtended);
			return;
		}
		if (gearState == LandingGear.GearState.LockedRetracted && gearDeployed)
		{
			SetGear(LandingGear.GearState.Extending);
		}
		if (gearState == LandingGear.GearState.LockedExtended && !gearDeployed)
		{
			SetGear(LandingGear.GearState.Retracting);
		}
	}

	public void SetGear(LandingGear.GearState gearState)
	{
		if (this.gearState != gearState)
		{
			this.gearState = gearState;
			this.onSetGear?.Invoke(new OnSetGear
			{
				gearState = this.gearState
			});
		}
	}

	public void ApplySetInputs(CompressedInputs inputs)
	{
		controlInputs.pitch = inputs.pitch.Decompress();
		controlInputs.roll = inputs.roll.Decompress();
		controlInputs.yaw = inputs.yaw.Decompress();
		controlInputs.brake = inputs.brake.Decompress();
		controlInputs.throttle = inputs.throttle.Decompress();
		controlInputs.customAxis1 = inputs.customAxis1.Decompress();
	}

	public override void CheckRadarAlt()
	{
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
		bool flag = airborne;
		airborne = radarAlt > 0.2f;
		if (airborne != flag)
		{
			if (airborne && !disabled && GameManager.IsLocalAircraft(this))
			{
				AudioClip audioClip = (gearDeployed ? GetAircraftParameters().takeoffMusic : null);
				MusicManager.i.CrossFadeMusic(audioClip, 2f, 0f, repeat: false, allowReplay: false, replacePlaying: true);
			}
			else
			{
				this.OnTouchdown?.Invoke();
			}
		}
	}

	private void FixedUpdate()
	{
		if (countermeasureTrigger)
		{
			countermeasureManager.DeployCountermeasure(this);
		}
		Vector3 velocity = cockpit.rb.velocity;
		if (hit.collider != null && hit.collider.attachedRigidbody != null)
		{
			velocity -= hit.collider.attachedRigidbody.GetPointVelocity(hit.point);
		}
		speed = velocity.magnitude;
		airDensity = LevelInfo.GetAirDensity(cockpit.xform.position.GlobalY());
		if (base.LocalSim)
		{
			LocalSimFixedUpdate();
		}
		if (scrapeSource != null && scrapeSource.isPlaying)
		{
			scrapeSource.volume -= 2f * Time.deltaTime;
			if (scrapeSource.volume <= 0f)
			{
				scrapeSource.Stop();
			}
		}
		if (DebugVis.Enabled && pilots.Length != 0 && DebugVis.Create(ref debugControlInputsDisplay, GameAssets.i.debugControlInputsDisplay, base.transform))
		{
			debugControlInputsDisplay.Setup(pilots[0]);
		}
		float speedOfSound = LevelInfo.GetSpeedOfSound(base.transform.GlobalPosition().y);
		if (speed > 0.99f * speedOfSound && speed < speedOfSound)
		{
			float highFreqShake = 0.25f * airDensity;
			ShakeAircraft(0f, highFreqShake);
		}
	}

	private void LocalSimFixedUpdate()
	{
		using (localSimFixedUpdateMarker.Auto())
		{
			accel = ((velocityPrev == Vector3.zero) ? Vector3.zero : (CockpitRB().velocity - velocityPrev));
			partChecker.Check();
			velocityPrev = CockpitRB().velocity;
			accel /= Time.fixedDeltaTime * 9.81f;
			gForce = accel.magnitude;
			windVelocity = NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
			foreach (ControlSurface controlSurface in controlSurfaces)
			{
				controlSurface.Aero();
			}
			if (SceneSingleton<CombatHUD>.i != null && SceneSingleton<CombatHUD>.i.aircraft == this)
			{
				countermeasureManager.UpdateHUD();
			}
		}
	}

	private void CheckPhysicsLod()
	{
		bool flag = FastMath.InRange(SceneSingleton<CameraStateManager>.i.transform.position, base.transform.position, 10000f);
		if (simplePhysics)
		{
			if (gForce < 2f && flag)
			{
				SetComplexPhysics();
			}
		}
		else if (gForce < 2f && !flag)
		{
			SetSimplePhysics();
		}
	}

	public void AddECMIntensity(float ecmIntensity)
	{
		this.ecmIntensity += ecmIntensity;
		if (Mathf.Abs(this.ecmIntensity) < 0.001f)
		{
			this.ecmIntensity = 0f;
		}
	}

	public void SetStationTargets(byte stationIndex, ReadOnlySpan<PersistentID> targetIDs)
	{
		if (targetIDs.Length > 128)
		{
			ReadOnlySpan<PersistentID> readOnlySpan = targetIDs;
			targetIDs = readOnlySpan.Slice(0, 128);
			ColorLog<Aircraft>.InfoWarn("SetStationTargets can only have a max of 128 targets");
		}
		if (base.IsServer)
		{
			RpcSetStationTargets(stationIndex, targetIDs);
		}
		else if (base.HasAuthority)
		{
			CmdSetStationTargets(stationIndex, targetIDs);
		}
	}

	public void SetStationTurretTarget(byte stationIndex, byte turretIndex, PersistentID targetID)
	{
		if (base.IsServer)
		{
			RpcSetStationTurretTarget(stationIndex, turretIndex, targetID);
		}
		else if (base.HasAuthority)
		{
			CmdSetStationTurretTarget(stationIndex, turretIndex, targetID);
		}
	}

	[RateLimit(Refill = 10, MaxTokens = 30, Penalty = 2)]
	[ServerRpc]
	public void CmdSetStationTurretTarget(byte stationIndex, byte turretIndex, PersistentID targetID)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetStationTurretTarget__002D461984136(stationIndex, turretIndex, targetID);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		writer.WriteByteExtension(turretIndex);
		GeneratedNetworkCode._Write_PersistentID(writer, targetID);
		ServerRpcSender.Send(this, 36, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	public void RpcSetStationTurretTarget(byte stationIndex, byte turretIndex, PersistentID targetID)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcSetStationTurretTarget_740629667(stationIndex, turretIndex, targetID);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		writer.WriteByteExtension(turretIndex);
		GeneratedNetworkCode._Write_PersistentID(writer, targetID);
		ClientRpcSender.Send(this, 37, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void SetActiveStation(byte stationIndex)
	{
		weaponManager.SetActiveStation(stationIndex);
		if (base.IsServer)
		{
			RpcSetActiveStation(stationIndex);
		}
		else if (base.HasAuthority)
		{
			CmdSetActiveStation(stationIndex);
		}
	}

	[RateLimit(Refill = 10, MaxTokens = 30, Penalty = 2)]
	[ServerRpc]
	private void CmdSetActiveStation(byte stationIndex)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetActiveStation_126720094(stationIndex);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		ServerRpcSender.Send(this, 38, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	private void RpcSetActiveStation(byte stationIndex)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcSetActiveStation_1081017235(stationIndex);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(stationIndex);
		ClientRpcSender.Send(this, 39, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void SetTurretVector(byte weaponStationIndex, Vector3 direction)
	{
		if (base.IsServer)
		{
			RpcSetTurretVector(weaponStationIndex, NetworkFloatHelper.CompressIfValid(direction, logErrors: false, null, Vector3.forward));
		}
		else if (base.HasAuthority)
		{
			CmdSetTurretVector(weaponStationIndex, NetworkFloatHelper.CompressIfValid(direction, logErrors: true, "direction", Vector3.forward));
		}
	}

	[RateLimit(Refill = 20, MaxTokens = 100, Penalty = 1)]
	[ServerRpc]
	private void CmdSetTurretVector(byte weaponStationIndex, Vector3Compressed direction)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdSetTurretVector__002D1133982608(weaponStationIndex, direction);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(weaponStationIndex);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, direction);
		ServerRpcSender.Send(this, 40, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc(excludeOwner = true)]
	public void RpcSetTurretVector(byte weaponStationIndex, Vector3Compressed direction)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: true))
		{
			UserCode_RpcSetTurretVector__002D312569381(weaponStationIndex, direction);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteByteExtension(weaponStationIndex);
		GeneratedNetworkCode._Write_Vector3Compressed(writer, direction);
		ClientRpcSender.Send(this, 41, writer, Mirage.Channel.Reliable, excludeOwner: true);
		writer.Release();
	}

	public void StartEjectionSequence()
	{
		if (!ejected)
		{
			ejected = true;
			if (base.IsServer)
			{
				EjectionSequence().Forget();
			}
			else if (base.HasAuthority)
			{
				CmdStartEjectionSequence();
			}
		}
	}

	[RateLimit(Refill = 1, MaxTokens = 5, Penalty = 10)]
	[ServerRpc]
	private void CmdStartEjectionSequence()
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: true, allowServerToCall: false))
		{
			UserCode_CmdStartEjectionSequence_1872290971();
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ServerRpcSender.Send(this, 42, writer, Mirage.Channel.Reliable, requireAuthority: true);
		writer.Release();
	}

	[ClientRpc]
	public void RpcJettisonCanopy()
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcJettisonCanopy_1196305304();
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.Send(this, 43, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcEscapeCapsule()
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcEscapeCapsule__002D1155040382();
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.Send(this, 44, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[AsyncStateMachine(typeof(_003CEjectionSequence_003Ed__207))]
	[Server]
	private UniTask EjectionSequence()
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'EjectionSequence' called when server not active");
		}
		_003CEjectionSequence_003Ed__207 stateMachine = default(_003CEjectionSequence_003Ed__207);
		stateMachine._003C_003Et__builder = AsyncUniTaskMethodBuilder.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	public void SpawnEjectingPilot(int pilotNumber)
	{
		UnitPart unitPart = pilots[pilotNumber].GetUnitPart();
		GameObject gameObject = UnityEngine.Object.Instantiate(GameAssets.i.pilotDismounted, pilots[pilotNumber].transform.position, pilots[pilotNumber].transform.rotation);
		PilotDismounted component = gameObject.GetComponent<PilotDismounted>();
		component.NetworkparentUnit = persistentID;
		component.NetworkunitPart = unitPart.id;
		component.NetworkunitName = unitName + " pilot";
		component.NetworkHQ = base.NetworkHQ;
		component.NetworkpilotNumber = (byte)pilotNumber;
		component.NetworkstartPosition = pilots[pilotNumber].transform.position.ToGlobalPosition();
		pilots[pilotNumber].ejected = true;
		if (pilotNumber == 0)
		{
			component.Networkplayer = Player;
		}
		base.ServerObjectManager.Spawn(gameObject, base.Owner);
	}

	protected override void ServerDisableUnit()
	{
		if (GameManager.gameState != GameState.Encyclopedia)
		{
			base.ServerDisableUnit();
			if (!IsLanded() || !(base.NetworkHQ != null) || !base.NetworkHQ.AnyNearAirbase(base.transform.position, out var _))
			{
				ReportKilled();
			}
		}
	}

	private void OnDisable()
	{
		foreach (UnitPart item in partLookup)
		{
			if (item != null)
			{
				item.DetachDamageParticles();
			}
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (playerRef.Player != null)
		{
			playerRef.Player.OnNameResolved.RemoveListener(OwnerNameResolved);
		}
		foreach (UnitPart item in partLookup)
		{
			if (item != null)
			{
				item.RemovePart();
			}
		}
		GameObject[] array = groundEquipment;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	public bool HasEjected()
	{
		return ejected;
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		SetDoppler(enabled: true);
		if (newState && base.HasAuthority)
		{
			if (IsLanded() && base.NetworkHQ != null && base.NetworkHQ.AnyNearAirbase(base.transform.position, out var _))
			{
				RichPresenceManager.SetActivity(PresenceActivity.Landed);
			}
			else if (ejected)
			{
				RichPresenceManager.SetActivity(PresenceActivity.Ejected);
			}
			else
			{
				RichPresenceManager.SetActivity(PresenceActivity.Crashed);
			}
		}
		if (base.gameObject.activeSelf)
		{
			WaitRemoveAircraft(30f).Forget();
		}
	}

	[Server]
	public void ReturnToInventory()
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'ReturnToInventory' called when server not active");
		}
		weaponManager.ReturnWeapons();
		float num = sortieScore * MissionManager.CurrentMission.missionSettings.successfulSortieBonus;
		if (Player != null)
		{
			if (num > 0f)
			{
				SuccessfulSortie();
			}
			Player.RecoverAirframeInUse(definition);
		}
		else
		{
			base.NetworkHQ.AddSupplyUnit(definition, 1);
		}
		base.NetworkHQ.AddScore(num);
		base.NetworkunitState = UnitState.Returned;
		base.Networkdisabled = true;
		WaitRemoveAircraft(2f).Forget();
	}

	public void SuccessfulSortie()
	{
		float num = sortieScore * MissionManager.CurrentMission.missionSettings.successfulSortieBonus;
		if (num != 0f)
		{
			base.NetworkHQ.AddScore(num);
			Player.RpcShowSortieBonus(num);
			Player.AddScore(num);
			NetworksortieScore = 0f;
			this.onSortieSuccessful?.Invoke(num);
		}
	}

	private async UniTask WaitRemoveAircraft(float delay)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(delay * 1000f));
		if (!cancel.IsCancellationRequested && NetworkManagerNuclearOption.i.Server.Active)
		{
			UnityEngine.Object.Destroy(base.gameObject);
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
			GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EPlayerRef(writer, playerRef);
			GeneratedNetworkCode._Write_NuclearOption_002ESavedMission_002ELoadout(writer, loadout);
			writer.WriteNetworkBehaviorSyncVar(spawningHangar);
			writer.WriteVector3(startingVelocity);
			GeneratedNetworkCode._Write_LiveryKey(writer, LiveryKey);
			writer.WriteSingleConverter(sortieScore);
			writer.WriteBooleanExtension(countermeasureTrigger);
			writer.WriteSingleConverter(fuelLevel);
			writer.WriteBooleanExtension(Ignition);
			writer.WriteBooleanExtension(gearDeployed);
			return true;
		}
		writer.Write((ulong)((long)syncVarDirtyBits >> 9), 10);
		if ((syncVarDirtyBits & 0x200L) != 0L)
		{
			GeneratedNetworkCode._Write_NuclearOption_002ENetworking_002EPlayerRef(writer, playerRef);
			result = true;
		}
		if ((syncVarDirtyBits & 0x400L) != 0L)
		{
			GeneratedNetworkCode._Write_NuclearOption_002ESavedMission_002ELoadout(writer, loadout);
			result = true;
		}
		if ((syncVarDirtyBits & 0x800L) != 0L)
		{
			writer.WriteNetworkBehaviorSyncVar(spawningHangar);
			result = true;
		}
		if ((syncVarDirtyBits & 0x1000L) != 0L)
		{
			writer.WriteVector3(startingVelocity);
			result = true;
		}
		if ((syncVarDirtyBits & 0x2000L) != 0L)
		{
			GeneratedNetworkCode._Write_LiveryKey(writer, LiveryKey);
			result = true;
		}
		if ((syncVarDirtyBits & 0x4000L) != 0L)
		{
			writer.WriteSingleConverter(sortieScore);
			result = true;
		}
		if ((syncVarDirtyBits & 0x8000L) != 0L)
		{
			writer.WriteBooleanExtension(countermeasureTrigger);
			result = true;
		}
		if ((syncVarDirtyBits & 0x10000L) != 0L)
		{
			writer.WriteSingleConverter(fuelLevel);
			result = true;
		}
		if ((syncVarDirtyBits & 0x20000L) != 0L)
		{
			writer.WriteBooleanExtension(Ignition);
			result = true;
		}
		if ((syncVarDirtyBits & 0x40000L) != 0L)
		{
			writer.WriteBooleanExtension(gearDeployed);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			playerRef = GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EPlayerRef(reader);
			Loadout value = loadout;
			loadout = GeneratedNetworkCode._Read_NuclearOption_002ESavedMission_002ELoadout(reader);
			spawningHangar = reader.ReadNetworkBehaviourSyncVar();
			startingVelocity = reader.ReadVector3();
			LiveryKey = GeneratedNetworkCode._Read_LiveryKey(reader);
			sortieScore = reader.ReadSingleConverter();
			countermeasureTrigger = reader.ReadBooleanExtension();
			fuelLevel = reader.ReadSingleConverter();
			Ignition = reader.ReadBooleanExtension();
			bool flag = gearDeployed;
			gearDeployed = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(value, loadout))
			{
				this.onLoadoutChanged?.Invoke();
			}
			if (!base.IsServer && !SyncVarEqual(flag, gearDeployed))
			{
				GearStateChangedHook(flag, gearDeployed);
			}
			return;
		}
		ulong num = reader.Read(10);
		SetDeserializeMask(num, 9);
		if ((num & 1L) != 0L)
		{
			playerRef = GeneratedNetworkCode._Read_NuclearOption_002ENetworking_002EPlayerRef(reader);
		}
		if ((num & 2L) != 0L)
		{
			Loadout value2 = loadout;
			loadout = GeneratedNetworkCode._Read_NuclearOption_002ESavedMission_002ELoadout(reader);
			if (!base.IsServer && !SyncVarEqual(value2, loadout))
			{
				this.onLoadoutChanged?.Invoke();
			}
		}
		if ((num & 4L) != 0L)
		{
			spawningHangar = reader.ReadNetworkBehaviourSyncVar();
		}
		if ((num & 8L) != 0L)
		{
			startingVelocity = reader.ReadVector3();
		}
		if ((num & 0x10L) != 0L)
		{
			LiveryKey = GeneratedNetworkCode._Read_LiveryKey(reader);
		}
		if ((num & 0x20L) != 0L)
		{
			sortieScore = reader.ReadSingleConverter();
		}
		if ((num & 0x40L) != 0L)
		{
			countermeasureTrigger = reader.ReadBooleanExtension();
		}
		if ((num & 0x80L) != 0L)
		{
			fuelLevel = reader.ReadSingleConverter();
		}
		if ((num & 0x100L) != 0L)
		{
			Ignition = reader.ReadBooleanExtension();
		}
		if ((num & 0x200L) != 0L)
		{
			bool flag2 = gearDeployed;
			gearDeployed = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(flag2, gearDeployed))
			{
				GearStateChangedHook(flag2, gearDeployed);
			}
		}
	}

	public void UserCode_RpcGetRadarWarning__002D1586111906(Unit emitter)
	{
		Radar radar = emitter.radar as Radar;
		float returnSignal;
		bool flag = EstimateDetection(radar, out returnSignal);
		bool isTarget = !(emitter is Aircraft) && flag && emitter.CheckIsTarget(this);
		this.onRadarWarning?.Invoke(new OnRadarWarning
		{
			emitter = emitter,
			radar = radar,
			power = returnSignal,
			detected = flag,
			isTarget = isTarget
		});
	}

	protected static void Skeleton_RpcGetRadarWarning__002D1586111906(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcGetRadarWarning__002D1586111906(GeneratedNetworkCode._Read_Unit(reader));
	}

	public void UserCode_CmdToggleRadar_1821461427()
	{
		RpcToggleRadar(!radar.activated);
	}

	protected static void Skeleton_CmdToggleRadar_1821461427(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdToggleRadar_1821461427();
	}

	private void UserCode_RpcToggleRadar_1325449311(bool activated)
	{
		radar.activated = activated;
		if (SceneSingleton<CombatHUD>.i.aircraft == this)
		{
			string report = (radar.activated ? "Radar Armed" : "Radar Disarmed");
			SceneSingleton<AircraftActionsReport>.i.ReportText(report, 5f);
		}
	}

	protected static void Skeleton_RpcToggleRadar_1325449311(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcToggleRadar_1325449311(reader.ReadBooleanExtension());
	}

	public void UserCode_CmdToggleIgnition__002D1067807998()
	{
		NetworkIgnition = !Ignition;
	}

	protected static void Skeleton_CmdToggleIgnition__002D1067807998(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdToggleIgnition__002D1067807998();
	}

	private void UserCode_CmdSetCanRefuel__002D170919880()
	{
		needsFuel = true;
	}

	protected static void Skeleton_CmdSetCanRefuel__002D170919880(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSetCanRefuel__002D170919880();
	}

	public void UserCode_CmdSlingLoadAttachment__002D82313074(Unit slingLoadUnit, SlingloadHook.DeployState state)
	{
		bool flag = state == SlingloadHook.DeployState.Connected || state == SlingloadHook.DeployState.RescuePilot;
		bool flag2 = slingLoadUnit != null && flag && slingLoadUnit.IsSlung() && state != SlingloadHook.DeployState.RescuePilot;
		bool flag3 = slingLoadUnit != null && FastMath.OutOfRange(this.GlobalPosition(), slingLoadUnit.GlobalPosition(), flag ? 50 : 200);
		bool flag4 = slingLoadUnit != null && !slingLoadUnit.definition.CanSlingLoad;
		if (flag2 || flag3 || flag4)
		{
			UnityEngine.Debug.LogError($"Unable to validate sling load attachment: doubleAttach: {flag2}, outOfRange: {flag3}, invalidUnit: {flag4}");
			if (flag2)
			{
				base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidState);
			}
			if (flag3)
			{
				base.Owner.SetError(5, NuclearOptionPlayerErrorFlags.DistanceExploit);
			}
			if (flag4)
			{
				base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			}
		}
		else
		{
			RpcSlingLoadAttachment(slingLoadUnit, state);
		}
	}

	protected static void Skeleton_CmdSlingLoadAttachment__002D82313074(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSlingLoadAttachment__002D82313074(GeneratedNetworkCode._Read_Unit(reader), GeneratedNetworkCode._Read_SlingloadHook_002FDeployState(reader));
	}

	public void UserCode_RpcSlingLoadAttachment__002D57940477(Unit slingLoadUnit, SlingloadHook.DeployState state)
	{
		ApplySlingLoadAttachment(slingLoadUnit, state);
	}

	protected static void Skeleton_RpcSlingLoadAttachment__002D57940477(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcSlingLoadAttachment__002D57940477(GeneratedNetworkCode._Read_Unit(reader), GeneratedNetworkCode._Read_SlingloadHook_002FDeployState(reader));
	}

	public void UserCode_CmdSendSlungTransform__002D695501160(Vector3 position, Quaternion rotation)
	{
		if (slungUnit == null)
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidState);
			return;
		}
		if (!NetworkFloatHelper.Validate(position, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			return;
		}
		if (!NetworkFloatHelper.Validate(rotation, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			return;
		}
		if (position.sqrMagnitude > 2500f)
		{
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.DistanceExploit);
			position = Vector3.ClampMagnitude(position, 50f);
		}
		Vector3 position2 = base.transform.TransformPoint(position);
		slungUnit.transform.SetPositionAndRotation(position2, rotation);
		slungUnit.rb.MovePosition(position2);
		slungUnit.rb.MoveRotation(rotation);
		slungUnit.rb.velocity = base.rb.velocity;
	}

	protected static void Skeleton_CmdSendSlungTransform__002D695501160(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSendSlungTransform__002D695501160(reader.ReadVector3(), reader.ReadQuaternion());
	}

	private void UserCode_RpcRefuel_1587114129(Unit refueler)
	{
		SetFuelLevel(refueler);
	}

	protected static void Skeleton_RpcRefuel_1587114129(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcRefuel_1587114129(GeneratedNetworkCode._Read_Unit(reader));
	}

	public virtual void UserCode_RpcDamage_870168505(byte index, DamageInfo damageInfo)
	{
		base.UserCode_RpcDamage_1709046923(index, damageInfo);
		ShakeAircraft(damageInfo.blastDamage.Decompress() * 0.01f, damageInfo.pierceDamage.Decompress() * 0.01f);
	}

	protected static void Skeleton_RpcDamage_870168505(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcDamage_870168505(reader.ReadByteExtension(), GeneratedNetworkCode._Read_DamageInfo(reader));
	}

	public void UserCode_CmdLaunchMissile_644415535(byte stationIndex, Unit target, GlobalPosition aimpoint)
	{
		if (stationIndex >= weaponStations.Count)
		{
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
			return;
		}
		if (!NetworkFloatHelper.Validate(aimpoint, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
			return;
		}
		weaponStations[stationIndex].LaunchMount(this, target, aimpoint);
		RpcLaunchMissile(stationIndex, target, aimpoint);
	}

	protected static void Skeleton_CmdLaunchMissile_644415535(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdLaunchMissile_644415535(reader.ReadByteExtension(), GeneratedNetworkCode._Read_Unit(reader), reader.ReadGlobalPosition());
	}

	public void UserCode_RpcLaunchMissile_1465828762(byte stationIndex, Unit target, GlobalPosition aimpoint)
	{
		if (!NetworkManagerNuclearOption.i.Server.Active && !CheckIfLocalSim())
		{
			weaponStations[stationIndex].LaunchMount(this, target, aimpoint);
		}
	}

	protected static void Skeleton_RpcLaunchMissile_1465828762(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcLaunchMissile_1465828762(reader.ReadByteExtension(), GeneratedNetworkCode._Read_Unit(reader), reader.ReadGlobalPosition());
	}

	private void UserCode_CmdCountermeasures__002D1391811506(bool active, byte index)
	{
		NetworkcountermeasureTrigger = active;
		byte activeIndex = countermeasureManager.activeIndex;
		countermeasureManager.activeIndex = index;
		if (activeIndex != index)
		{
			RpcCountermeasures(index);
		}
	}

	protected static void Skeleton_CmdCountermeasures__002D1391811506(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdCountermeasures__002D1391811506(reader.ReadBooleanExtension(), reader.ReadByteExtension());
	}

	private void UserCode_RpcCountermeasures_1466036644(byte index)
	{
		countermeasureManager.activeIndex = index;
	}

	protected static void Skeleton_RpcCountermeasures_1466036644(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcCountermeasures_1466036644(reader.ReadByteExtension());
	}

	private void UserCode_CmdSetGear__002D1969689879(bool deployed)
	{
		NetworkgearDeployed = deployed;
	}

	protected static void Skeleton_CmdSetGear__002D1969689879(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSetGear__002D1969689879(reader.ReadBooleanExtension());
	}

	public void UserCode_CmdSetStationTurretTarget__002D461984136(byte stationIndex, byte turretIndex, PersistentID targetID)
	{
		if (stationIndex >= weaponStations.Count)
		{
			ColorLog<Aircraft>.LogError("stationIndex was out of bounds");
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcSetStationTurretTarget(stationIndex, turretIndex, targetID);
		}
	}

	protected static void Skeleton_CmdSetStationTurretTarget__002D461984136(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSetStationTurretTarget__002D461984136(reader.ReadByteExtension(), reader.ReadByteExtension(), GeneratedNetworkCode._Read_PersistentID(reader));
	}

	public void UserCode_RpcSetStationTurretTarget_740629667(byte stationIndex, byte turretIndex, PersistentID targetID)
	{
		weaponStations[stationIndex].SetStationTurretTarget(turretIndex, targetID);
	}

	protected static void Skeleton_RpcSetStationTurretTarget_740629667(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcSetStationTurretTarget_740629667(reader.ReadByteExtension(), reader.ReadByteExtension(), GeneratedNetworkCode._Read_PersistentID(reader));
	}

	private void UserCode_CmdSetActiveStation_126720094(byte stationIndex)
	{
		if (stationIndex >= weaponStations.Count)
		{
			ColorLog<Aircraft>.LogError("stationIndex was out of bounds");
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcSetActiveStation(stationIndex);
		}
	}

	protected static void Skeleton_CmdSetActiveStation_126720094(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSetActiveStation_126720094(reader.ReadByteExtension());
	}

	private void UserCode_RpcSetActiveStation_1081017235(byte stationIndex)
	{
		if (!base.LocalSim)
		{
			weaponManager.SetActiveStation(stationIndex);
		}
	}

	protected static void Skeleton_RpcSetActiveStation_1081017235(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcSetActiveStation_1081017235(reader.ReadByteExtension());
	}

	private void UserCode_CmdSetTurretVector__002D1133982608(byte weaponStationIndex, Vector3Compressed direction)
	{
		if (!NetworkFloatHelper.Validate(direction, logErrors: false, null))
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidValue);
		}
		else if (weaponStationIndex >= weaponStations.Count)
		{
			ColorLog<Aircraft>.LogError("stationIndex was out of bounds");
			base.Owner.SetError(2, NuclearOptionPlayerErrorFlags.OutOfBounds);
		}
		else
		{
			RpcSetTurretVector(weaponStationIndex, direction);
		}
	}

	protected static void Skeleton_CmdSetTurretVector__002D1133982608(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdSetTurretVector__002D1133982608(reader.ReadByteExtension(), GeneratedNetworkCode._Read_Vector3Compressed(reader));
	}

	public void UserCode_RpcSetTurretVector__002D312569381(byte weaponStationIndex, Vector3Compressed direction)
	{
		if (!base.LocalSim)
		{
			weaponStations[weaponStationIndex].SetTurretVector(NetworkFloatHelper.DecompressIfValid(direction, logErrors: true, "direction", Vector3.forward));
		}
	}

	protected static void Skeleton_RpcSetTurretVector__002D312569381(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcSetTurretVector__002D312569381(reader.ReadByteExtension(), GeneratedNetworkCode._Read_Vector3Compressed(reader));
	}

	private void UserCode_CmdStartEjectionSequence_1872290971()
	{
		if (ejected)
		{
			base.Owner.SetError(1, NuclearOptionPlayerErrorFlags.InvalidState);
			return;
		}
		ejected = true;
		EjectionSequence().Forget();
	}

	protected static void Skeleton_CmdStartEjectionSequence_1872290971(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_CmdStartEjectionSequence_1872290971();
	}

	public void UserCode_RpcJettisonCanopy_1196305304()
	{
		ejected = true;
		if (IsLanded())
		{
			OpenCanopies();
			if (base.NetworkHQ != null && base.NetworkHQ.AnyNearAirbase(base.transform.position, out var _))
			{
				return;
			}
		}
		else
		{
			Canopy[] array = canopies;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Eject();
			}
			this.onEject?.Invoke();
		}
		if (GameManager.IsLocalAircraft(this) && !MissionHelper.CanRespawn)
		{
			GameManager.FinishGame(GameResolution.Defeat);
		}
	}

	protected static void Skeleton_RpcJettisonCanopy_1196305304(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcJettisonCanopy_1196305304();
	}

	public void UserCode_RpcEscapeCapsule__002D1155040382()
	{
		if (cockpit.TryGetComponent<EscapeCapsule>(out var component))
		{
			component.StartEjection();
		}
	}

	protected static void Skeleton_RpcEscapeCapsule__002D1155040382(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Aircraft)behaviour).UserCode_RpcEscapeCapsule__002D1155040382();
	}

	protected override int GetRpcCount()
	{
		return 45;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(21, "Aircraft.RpcGetRadarWarning", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcGetRadarWarning__002D1586111906, RpcRateLimitConfig.Disabled());
		collection.Register(22, "Aircraft.CmdToggleRadar", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdToggleRadar_1821461427, RpcRateLimitConfig.Enabled(1f, 10, 30, 1));
		collection.Register(23, "Aircraft.RpcToggleRadar", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcToggleRadar_1325449311, RpcRateLimitConfig.Disabled());
		collection.Register(24, "Aircraft.CmdToggleIgnition", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdToggleIgnition__002D1067807998, RpcRateLimitConfig.Enabled(1f, 10, 30, 1));
		collection.Register(25, "Aircraft.CmdSetCanRefuel", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetCanRefuel__002D170919880, RpcRateLimitConfig.Enabled(1f, 10, 30, 1));
		collection.Register(26, "Aircraft.CmdSlingLoadAttachment", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSlingLoadAttachment__002D82313074, RpcRateLimitConfig.Enabled(1f, 5, 15, 3));
		collection.Register(27, "Aircraft.RpcSlingLoadAttachment", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSlingLoadAttachment__002D57940477, RpcRateLimitConfig.Disabled());
		collection.Register(28, "Aircraft.CmdSendSlungTransform", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSendSlungTransform__002D695501160, RpcRateLimitConfig.Enabled(1f, 25, 100, 1));
		collection.Register(29, "Aircraft.RpcRefuel", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcRefuel_1587114129, RpcRateLimitConfig.Disabled());
		collection.Register(30, "Aircraft.RpcDamage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcDamage_870168505, RpcRateLimitConfig.Disabled());
		collection.Register(31, "Aircraft.CmdLaunchMissile", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdLaunchMissile_644415535, RpcRateLimitConfig.Enabled(1f, 15, 45, 2));
		collection.Register(32, "Aircraft.RpcLaunchMissile", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcLaunchMissile_1465828762, RpcRateLimitConfig.Disabled());
		collection.Register(33, "Aircraft.CmdCountermeasures", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdCountermeasures__002D1391811506, RpcRateLimitConfig.Disabled());
		collection.Register(34, "Aircraft.RpcCountermeasures", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcCountermeasures_1466036644, RpcRateLimitConfig.Disabled());
		collection.Register(35, "Aircraft.CmdSetGear", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetGear__002D1969689879, RpcRateLimitConfig.Enabled(1f, 5, 15, 3));
		collection.Register(36, "Aircraft.CmdSetStationTurretTarget", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetStationTurretTarget__002D461984136, RpcRateLimitConfig.Enabled(1f, 10, 30, 2));
		collection.Register(37, "Aircraft.RpcSetStationTurretTarget", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSetStationTurretTarget_740629667, RpcRateLimitConfig.Disabled());
		collection.Register(38, "Aircraft.CmdSetActiveStation", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetActiveStation_126720094, RpcRateLimitConfig.Enabled(1f, 10, 30, 2));
		collection.Register(39, "Aircraft.RpcSetActiveStation", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSetActiveStation_1081017235, RpcRateLimitConfig.Disabled());
		collection.Register(40, "Aircraft.CmdSetTurretVector", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdSetTurretVector__002D1133982608, RpcRateLimitConfig.Enabled(1f, 20, 100, 1));
		collection.Register(41, "Aircraft.RpcSetTurretVector", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcSetTurretVector__002D312569381, RpcRateLimitConfig.Disabled());
		collection.Register(42, "Aircraft.CmdStartEjectionSequence", cmdRequireAuthority: true, RpcInvokeType.ServerRpc, this, Skeleton_CmdStartEjectionSequence_1872290971, RpcRateLimitConfig.Enabled(1f, 1, 5, 10));
		collection.Register(43, "Aircraft.RpcJettisonCanopy", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcJettisonCanopy_1196305304, RpcRateLimitConfig.Disabled());
		collection.Register(44, "Aircraft.RpcEscapeCapsule", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcEscapeCapsule__002D1155040382, RpcRateLimitConfig.Disabled());
	}
}
