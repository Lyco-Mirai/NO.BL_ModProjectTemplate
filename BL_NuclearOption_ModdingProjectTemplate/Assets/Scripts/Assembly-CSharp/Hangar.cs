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
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

public class Hangar : NetworkBehaviour
{
	public struct DoorState
	{
		public bool opening;

		public float openAmount;
	}

	private readonly struct QueuedAircraftToSpawn
	{
		public readonly Player player;

		public readonly AircraftDefinition definition;

		public readonly LiveryKey livery;

		public readonly Loadout loadout;

		public readonly float fuelLevel;

		public QueuedAircraftToSpawn(Player player, AircraftDefinition definition, LiveryKey livery, Loadout loadout, float fuelLevel)
		{
			this.player = player;
			this.definition = definition;
			this.livery = livery;
			this.loadout = loadout;
			this.fuelLevel = fuelLevel;
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CDoorSequenceCarrier_003Ed__58 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public AsyncUniTaskMethodBuilder _003C_003Et__builder;

		public Hangar _003C_003E4__this;

		public QueuedAircraftToSpawn spawnAircraft;

		public float radius;

		private CancellationToken _003CdestroyToken_003E5__2;

		private UniTask.Awaiter _003C_003Eu__1;

		private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__2;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			Hangar hangar = _003C_003E4__this;
			try
			{
				UniTask.Awaiter awaiter;
				switch (num)
				{
				default:
					_003CdestroyToken_003E5__2 = hangar.destroyCancellationToken;
					goto case 0;
				case 0:
				case 1:
					try
					{
						Cysharp.Threading.Tasks.YieldAwaitable.Awaiter awaiter2;
						if (num != 0)
						{
							if (num == 1)
							{
								awaiter2 = _003C_003Eu__2;
								_003C_003Eu__2 = default(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter);
								num = (_003C_003E1__state = -1);
								goto IL_0106;
							}
							awaiter = hangar.OpenDoors().GetAwaiter();
							if (!awaiter.IsCompleted)
							{
								num = (_003C_003E1__state = 0);
								_003C_003Eu__1 = awaiter;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
								return;
							}
						}
						else
						{
							awaiter = _003C_003Eu__1;
							_003C_003Eu__1 = default(UniTask.Awaiter);
							num = (_003C_003E1__state = -1);
						}
						awaiter.GetResult();
						if (!_003CdestroyToken_003E5__2.IsCancellationRequested)
						{
							awaiter2 = UniTask.Yield().GetAwaiter();
							if (!awaiter2.IsCompleted)
							{
								num = (_003C_003E1__state = 1);
								_003C_003Eu__2 = awaiter2;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
								return;
							}
							goto IL_0106;
						}
						goto end_IL_0031;
						IL_0106:
						awaiter2.GetResult();
						if (!_003CdestroyToken_003E5__2.IsCancellationRequested && hangar.IsFunctional())
						{
							QueuedAircraftToSpawn queuedAircraftToSpawn = spawnAircraft;
							hangar.SpawnAircraft(queuedAircraftToSpawn.player, queuedAircraftToSpawn.definition, queuedAircraftToSpawn.loadout, queuedAircraftToSpawn.fuelLevel, queuedAircraftToSpawn.livery);
							goto IL_0189;
						}
						end_IL_0031:;
					}
					finally
					{
						if (num < 0 && spawnAircraft.player != null && hangar.IsServer)
						{
							spawnAircraft.player.RpcClearSpawnPending();
						}
					}
					goto end_IL_000e;
				case 2:
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(UniTask.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_01e1;
				case 3:
					{
						awaiter = _003C_003Eu__1;
						_003C_003Eu__1 = default(UniTask.Awaiter);
						num = (_003C_003E1__state = -1);
						break;
					}
					IL_0189:
					awaiter = hangar.CloseDoors().GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 2);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
					goto IL_01e1;
					IL_01e1:
					awaiter.GetResult();
					if (!_003CdestroyToken_003E5__2.IsCancellationRequested)
					{
						awaiter = hangar.WaitForUnitToLeave(radius, hangar.destroyCancellationToken).GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							num = (_003C_003E1__state = 3);
							_003C_003Eu__1 = awaiter;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
							return;
						}
						break;
					}
					goto end_IL_000e;
				}
				awaiter.GetResult();
				if (!_003CdestroyToken_003E5__2.IsCancellationRequested)
				{
					hangar.Networkavailable = true;
					hangar.RpcHangarAvailable();
				}
				end_IL_000e:;
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003CdestroyToken_003E5__2 = default(CancellationToken);
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003CdestroyToken_003E5__2 = default(CancellationToken);
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

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CDoorSequenceNormal_003Ed__57 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public AsyncUniTaskMethodBuilder _003C_003Et__builder;

		public Hangar _003C_003E4__this;

		public float maxRadius;

		private CancellationToken _003CdestroyToken_003E5__2;

		private UniTask.Awaiter _003C_003Eu__1;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			Hangar hangar = _003C_003E4__this;
			try
			{
				UniTask.Awaiter awaiter;
				switch (num)
				{
				default:
					_003CdestroyToken_003E5__2 = hangar.destroyCancellationToken;
					awaiter = hangar.OpenDoors().GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
					goto IL_0083;
				case 0:
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(UniTask.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_0083;
				case 1:
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(UniTask.Awaiter);
					num = (_003C_003E1__state = -1);
					goto IL_00ff;
				case 2:
					{
						awaiter = _003C_003Eu__1;
						_003C_003Eu__1 = default(UniTask.Awaiter);
						num = (_003C_003E1__state = -1);
						break;
					}
					IL_00ff:
					awaiter.GetResult();
					if (!_003CdestroyToken_003E5__2.IsCancellationRequested)
					{
						awaiter = hangar.CloseDoors().GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							num = (_003C_003E1__state = 2);
							_003C_003Eu__1 = awaiter;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
							return;
						}
						break;
					}
					goto end_IL_000e;
					IL_0083:
					awaiter.GetResult();
					if (!_003CdestroyToken_003E5__2.IsCancellationRequested)
					{
						awaiter = hangar.WaitForUnitToLeave(maxRadius, hangar.destroyCancellationToken).GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							num = (_003C_003E1__state = 1);
							_003C_003Eu__1 = awaiter;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
							return;
						}
						goto IL_00ff;
					}
					goto end_IL_000e;
				}
				awaiter.GetResult();
				if (!_003CdestroyToken_003E5__2.IsCancellationRequested)
				{
					hangar.Networkavailable = true;
					hangar.RpcHangarAvailable();
				}
				end_IL_000e:;
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003CdestroyToken_003E5__2 = default(CancellationToken);
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003CdestroyToken_003E5__2 = default(CancellationToken);
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

	public Unit attachedUnit;

	[SerializeField]
	private float priority;

	[SerializeField]
	private UnitPart criticalPart;

	[SerializeField]
	private AircraftDefinition[] availableAircraft;

	[SerializeField]
	private bool waitForOpenBeforeSpawn;

	[SerializeField]
	private HangarDoor[] doors;

	[SerializeField]
	private HangarLighting[] lights;

	[SerializeField]
	private bool elevator;

	[SerializeField]
	private Transform spawnTransform;

	[SerializeField]
	private float doorSpeed;

	[SerializeField]
	private float clearDistance = 30f;

	[SerializeField]
	private AudioClip closedSound;

	[SerializeField]
	private AudioClip movingSound;

	[SerializeField]
	private AudioClip openSound;

	[SerializeField]
	private float pitchMin = 0.5f;

	[SerializeField]
	private float pitchMax = 1f;

	[SerializeField]
	[Range(0f, 2f)]
	private float movingVolume;

	[SerializeField]
	private bool speedVolume;

	[SerializeField]
	private bool speedPitch;

	private AudioSource oneShotSource;

	private AudioSource loopSource;

	private GameObject spawnedObject;

	private float doorCurrentSpeed;

	private CancellationTokenSource cancelMoveDoors;

	private bool selfDisabled;

	public Airbase parentAirbase;

	[SyncVar(initialOnly = true)]
	private DoorState doorState;

	[SyncVar(initialOnly = true)]
	private bool available;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 2;

	[NonSerialized]
	private const int RPC_COUNT = 3;

	public bool Disabled
	{
		get
		{
			if (!selfDisabled)
			{
				return attachedUnit.disabled;
			}
			return true;
		}
	}

	public bool Available
	{
		get
		{
			if (available)
			{
				return !Disabled;
			}
			return false;
		}
	}

	public DoorState NetworkdoorState
	{
		get
		{
			return doorState;
		}
		set
		{
			doorState = value;
		}
	}

	public bool Networkavailable
	{
		get
		{
			return available;
		}
		set
		{
			available = value;
		}
	}

	private void OnValidate()
	{
	}

	private void Awake()
	{
		base.Identity.OnStartServer.AddListener(OnStartServer);
		base.Identity.OnStartClient.AddListener(OnStartClient);
	}

	private void OnStartServer()
	{
		Networkavailable = true;
		NetworkdoorState = new DoorState
		{
			openAmount = 0f,
			opening = false
		};
		HangarDoor[] array = doors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(0f);
		}
		HangarLighting[] array2 = lights;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Enable(state: false);
		}
		if (criticalPart != null)
		{
			criticalPart.onApplyDamage += Hangar_OnApplyDamage;
		}
	}

	public Transform GetSpawnTransform()
	{
		return spawnTransform;
	}

	public float GetPriority()
	{
		return priority;
	}

	private void OnStartClient()
	{
		InitClientDoors();
		HangarLighting[] array = lights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Enable(state: false);
		}
	}

	public bool IsFunctional()
	{
		if (!Disabled)
		{
			return base.transform.position.y > Datum.LocalSeaY;
		}
		return false;
	}

	public void Repair()
	{
		if (!(attachedUnit == null))
		{
			attachedUnit.Networkdisabled = false;
			selfDisabled = false;
		}
	}

	private void Hangar_OnApplyDamage(UnitPart.OnApplyDamage e)
	{
		if (e.hitPoints < 0f || e.detached)
		{
			selfDisabled = true;
			HangarLighting[] array = lights;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnUnitDisabled();
			}
		}
	}

	public AircraftDefinition[] GetAvailableAircraft()
	{
		if (attachedUnit.disabled)
		{
			return Array.Empty<AircraftDefinition>();
		}
		return availableAircraft;
	}

	public bool CanSpawnAircraft(AircraftDefinition definition)
	{
		if (Available)
		{
			return ((ICollection<AircraftDefinition>)availableAircraft).Contains(definition);
		}
		return false;
	}

	[Server]
	public Airbase.TrySpawnResult TrySpawnAircraft(Player player, AircraftDefinition definition, LiveryKey livery, Loadout loadout, float fuelLevel)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'TrySpawnAircraft' called when server not active");
		}
		if (!CanSpawnAircraft(definition))
		{
			return default(Airbase.TrySpawnResult);
		}
		float num = Mathf.Max(definition.length, definition.width, definition.height, clearDistance);
		if (waitForOpenBeforeSpawn)
		{
			QueuedAircraftToSpawn spawnAircraft = new QueuedAircraftToSpawn(player, definition, livery, loadout, fuelLevel);
			DoorSequenceCarrier(num, spawnAircraft).Forget();
		}
		else
		{
			SpawnAircraft(player, definition, loadout, fuelLevel, livery);
			DoorSequenceNormal(num).Forget();
		}
		if (player != null)
		{
			player.FlyOwnedAirframe(definition);
		}
		else
		{
			attachedUnit.NetworkHQ.AddSupplyUnit(definition, -1);
		}
		return new Airbase.TrySpawnResult(allowed: true, this, waitForOpenBeforeSpawn);
	}

	public void CheckAttachCamera()
	{
		if (waitForOpenBeforeSpawn)
		{
			AttachCamera();
		}
	}

	private void AttachCamera()
	{
		SceneSingleton<CameraStateManager>.i.followingUnit = attachedUnit;
		SceneSingleton<CameraStateManager>.i.SwitchState(SceneSingleton<CameraStateManager>.i.relativeState);
		SceneSingleton<CameraStateManager>.i.transform.SetPositionAndRotation(base.transform.position, base.transform.rotation);
	}

	public Unit GetUnit()
	{
		return attachedUnit;
	}

	public Vector3 GetVelocity()
	{
		if (!(attachedUnit.rb == null))
		{
			return attachedUnit.rb.GetPointVelocity(spawnTransform.position);
		}
		return Vector3.zero;
	}

	public Vector3 GetAngularVelocity()
	{
		if (!(attachedUnit.rb == null))
		{
			return attachedUnit.rb.angularVelocity;
		}
		return Vector3.zero;
	}

	private void SpawnAircraft(Player player, AircraftDefinition definition, Loadout loadout, float fuelLevel, LiveryKey livery)
	{
		GlobalPosition globalPosition = spawnTransform.GlobalPosition() + spawnTransform.up * definition.spawnOffset.y + spawnTransform.forward * definition.spawnOffset.z;
		Vector3 velocity = GetVelocity();
		Aircraft aircraft = NetworkSceneSingleton<Spawner>.i.SpawnAircraft(player, definition.unitPrefab, loadout, fuelLevel, livery, globalPosition, spawnTransform.rotation * Quaternion.Euler(definition.restRotation), velocity, this, attachedUnit.NetworkHQ, null, 1f, 0.5f);
		if (loadout == null)
		{
			aircraft.Networkloadout = aircraft.weaponManager.SelectAIAircraftWeapons(parentAirbase);
		}
		spawnedObject = aircraft.gameObject;
	}

	private void OnDestroy()
	{
		cancelMoveDoors?.Cancel();
		selfDisabled = true;
	}

	[ClientRpc(excludeHost = true)]
	private void RpcOpenHangar()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.Send(this, 0, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc(excludeHost = true)]
	private void RpcCloseHangar()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.Send(this, 1, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	[ClientRpc(excludeHost = true)]
	private void RpcHangarAvailable()
	{
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.Send(this, 2, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private void InitClientDoors()
	{
		HangarDoor[] array = doors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(doorState.openAmount);
		}
		if (doorState.opening && doorState.openAmount < 1f)
		{
			OpenDoors().Forget();
		}
		else if (!doorState.opening && doorState.openAmount > 0f)
		{
			CloseDoors().Forget();
		}
	}

	[AsyncStateMachine(typeof(_003CDoorSequenceNormal_003Ed__57))]
	[Server]
	private UniTask DoorSequenceNormal(float maxRadius)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'DoorSequenceNormal' called when server not active");
		}
		_003CDoorSequenceNormal_003Ed__57 stateMachine = default(_003CDoorSequenceNormal_003Ed__57);
		stateMachine._003C_003Et__builder = AsyncUniTaskMethodBuilder.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.maxRadius = maxRadius;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	[AsyncStateMachine(typeof(_003CDoorSequenceCarrier_003Ed__58))]
	[Server]
	private UniTask DoorSequenceCarrier(float radius, QueuedAircraftToSpawn spawnAircraft)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'DoorSequenceCarrier' called when server not active");
		}
		_003CDoorSequenceCarrier_003Ed__58 stateMachine = default(_003CDoorSequenceCarrier_003Ed__58);
		stateMachine._003C_003Et__builder = AsyncUniTaskMethodBuilder.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.radius = radius;
		stateMachine.spawnAircraft = spawnAircraft;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	private async UniTask OpenDoors()
	{
		Networkavailable = false;
		if (base.IsServer)
		{
			RpcOpenHangar();
		}
		if (oneShotSource == null)
		{
			CreateAudioSources();
		}
		loopSource.Play();
		if (closedSound != null)
		{
			oneShotSource.PlayOneShot(closedSound, movingVolume);
		}
		CancellationToken cancel = GetNewCancellationToken();
		_ = base.name;
		_ = base.NetId;
		HangarLighting[] array = lights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Enable(state: true);
		}
		await MoveDoors(open: true, cancel);
		if (!cancel.IsCancellationRequested)
		{
			NetworkdoorState = new DoorState
			{
				opening = true,
				openAmount = 1f
			};
		}
	}

	private async UniTask WaitForUnitToLeave(float radius, CancellationToken cancel)
	{
		_ = base.name;
		_ = base.NetId;
		await UniTask.Delay(1000);
		if (!cancel.IsCancellationRequested)
		{
			await UniTask.WaitUntil(UnitLeft, PlayerLoopTiming.Update, cancel).SuppressCancellationThrow();
		}
		bool UnitLeft()
		{
			if (!(spawnedObject == null))
			{
				return FastMath.OutOfRange(spawnedObject.transform.position, spawnTransform.position, radius);
			}
			return true;
		}
	}

	private async UniTask CloseDoors()
	{
		if (base.IsServer)
		{
			RpcCloseHangar();
		}
		if (oneShotSource == null)
		{
			CreateAudioSources();
		}
		loopSource.Play();
		if (openSound != null)
		{
			oneShotSource.PlayOneShot(openSound);
		}
		CancellationToken cancel = GetNewCancellationToken();
		_ = base.name;
		_ = base.NetId;
		await MoveDoors(open: false, cancel);
		if (!cancel.IsCancellationRequested)
		{
			NetworkdoorState = new DoorState
			{
				opening = false,
				openAmount = 0f
			};
		}
	}

	private CancellationToken GetNewCancellationToken()
	{
		cancelMoveDoors?.Cancel();
		cancelMoveDoors = new CancellationTokenSource();
		return cancelMoveDoors.Token;
	}

	private async UniTask MoveDoors(bool open, CancellationToken cancel)
	{
		float openAmount = doorState.openAmount;
		float target = (open ? 1.05f : (-0.05f));
		while (!cancel.IsCancellationRequested)
		{
			float num = 0.2f / doorSpeed;
			openAmount = Mathf.SmoothDamp(openAmount, target, ref doorCurrentSpeed, 0.5f / doorSpeed, num);
			float num2 = Mathf.Abs(doorCurrentSpeed / num);
			float openAmount2 = Mathf.Clamp01(openAmount);
			HangarDoor[] array = doors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Move(openAmount2);
			}
			if (speedVolume)
			{
				loopSource.volume = Mathf.Clamp01(num2 * movingVolume);
			}
			else
			{
				loopSource.volume = movingVolume;
			}
			if (speedPitch)
			{
				loopSource.pitch = Mathf.Lerp(pitchMin, pitchMax, num2);
			}
			if (open)
			{
				if (openAmount >= 1f)
				{
					if (openSound != null)
					{
						oneShotSource.PlayOneShot(openSound, movingVolume);
					}
					if (loopSource.isPlaying)
					{
						loopSource.Stop();
					}
					break;
				}
			}
			else if (openAmount <= 0f)
			{
				if (closedSound != null)
				{
					oneShotSource.PlayOneShot(closedSound, movingVolume);
				}
				if (loopSource.isPlaying)
				{
					loopSource.Stop();
				}
				HangarLighting[] array2 = lights;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].Enable(state: false);
				}
				break;
			}
			NetworkdoorState = new DoorState
			{
				opening = open,
				openAmount = openAmount
			};
			await UniTask.WaitForFixedUpdate();
		}
	}

	private void CreateAudioSources()
	{
		oneShotSource = base.gameObject.AddComponent<AudioSource>();
		oneShotSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		oneShotSource.spatialBlend = 1f;
		oneShotSource.dopplerLevel = 0f;
		oneShotSource.minDistance = 20f;
		oneShotSource.maxDistance = 500f;
		loopSource = base.gameObject.AddComponent<AudioSource>();
		loopSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		loopSource.spatialBlend = 1f;
		loopSource.dopplerLevel = 0f;
		loopSource.minDistance = 20f;
		loopSource.maxDistance = 500f;
		loopSource.clip = movingSound;
		loopSource.loop = true;
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
			GeneratedNetworkCode._Write_Hangar_002FDoorState(writer, doorState);
			writer.WriteBooleanExtension(available);
			return true;
		}
		writer.Write(syncVarDirtyBits, 2);
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			doorState = GeneratedNetworkCode._Read_Hangar_002FDoorState(reader);
			available = reader.ReadBooleanExtension();
		}
		else
		{
			ulong dirtyBit = reader.Read(2);
			SetDeserializeMask(dirtyBit, 0);
		}
	}

	private void UserCode_RpcOpenHangar_1172819820()
	{
		OpenDoors().Forget();
	}

	protected static void Skeleton_RpcOpenHangar_1172819820(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Hangar)behaviour).UserCode_RpcOpenHangar_1172819820();
	}

	private void UserCode_RpcCloseHangar__002D1009291362()
	{
		CloseDoors().Forget();
	}

	protected static void Skeleton_RpcCloseHangar__002D1009291362(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Hangar)behaviour).UserCode_RpcCloseHangar__002D1009291362();
	}

	private void UserCode_RpcHangarAvailable__002D1837624951()
	{
		Networkavailable = true;
	}

	protected static void Skeleton_RpcHangarAvailable__002D1837624951(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Hangar)behaviour).UserCode_RpcHangarAvailable__002D1837624951();
	}

	protected override int GetRpcCount()
	{
		return 3;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "Hangar.RpcOpenHangar", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcOpenHangar_1172819820, RpcRateLimitConfig.Disabled());
		collection.Register(1, "Hangar.RpcCloseHangar", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcCloseHangar__002D1009291362, RpcRateLimitConfig.Disabled());
		collection.Register(2, "Hangar.RpcHangarAvailable", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcHangarAvailable__002D1837624951, RpcRateLimitConfig.Disabled());
	}
}
