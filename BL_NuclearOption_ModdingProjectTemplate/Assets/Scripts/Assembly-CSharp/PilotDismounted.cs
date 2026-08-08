using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using UnityEngine;

public class PilotDismounted : Unit, IDamageable
{
	public enum PilotState : byte
	{
		ejecting = 1,
		detaching = 2,
		parachuting = 3,
		landing = 4,
		dead = 5
	}

	private float hitPoints;

	private float chuteOpenTime;

	private float landedTime;

	[SerializeField]
	private ArmorProperties armorProperties;

	[SerializeField]
	private Animator animator;

	[SyncVar]
	public PersistentID parentUnit;

	[SyncVar]
	public byte unitPart;

	[SyncVar]
	public byte pilotNumber;

	[SyncVar(hook = "PilotStateChanged")]
	public PilotState animationState = PilotState.ejecting;

	[SyncVar]
	public NetworkBehaviorSyncvar player;

	[SyncVar(hook = "OnKinematicChanged")]
	private bool isKinematic;

	[SerializeField]
	private EjectionSeat ejectionSeat;

	[SerializeField]
	private Collider pilotCollider;

	[SerializeField]
	private Collider deadCollider;

	private new RaycastHit hit;

	[SyncVar]
	private bool chuteDeployedNetwork;

	private bool chuteDeployedLocal;

	[SyncVar]
	private bool seatDetachedNetwork;

	private bool seatDetachedLocal;

	private bool inWater;

	private bool checkingForCapture;

	private bool captured;

	private Vector3 posPrev;

	private Vector3? velocityPrev;

	private float runWeight;

	private bool slung;

	private int pilotRank;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 17;

	[NonSerialized]
	private const int RPC_COUNT = 22;

	public float timeSinceSpawn { get; private set; }

	public UnitPart cockpitPart { get; private set; }

	public bool IsOnEjectionRail => ejectionSeat.IsOnEjectionRail;

	public PersistentID NetworkparentUnit
	{
		get
		{
			return parentUnit;
		}
		set
		{
			if (!SyncVarEqual(value, parentUnit))
			{
				PersistentID persistentID = parentUnit;
				parentUnit = value;
				SetDirtyBit(512uL);
			}
		}
	}

	public byte NetworkunitPart
	{
		get
		{
			return unitPart;
		}
		set
		{
			if (!SyncVarEqual(value, unitPart))
			{
				byte b = unitPart;
				unitPart = value;
				SetDirtyBit(1024uL);
			}
		}
	}

	public byte NetworkpilotNumber
	{
		get
		{
			return pilotNumber;
		}
		set
		{
			if (!SyncVarEqual(value, pilotNumber))
			{
				byte b = pilotNumber;
				pilotNumber = value;
				SetDirtyBit(2048uL);
			}
		}
	}

	public PilotState NetworkanimationState
	{
		get
		{
			return animationState;
		}
		set
		{
			if (!SyncVarEqual(value, animationState))
			{
				PilotState oldState = animationState;
				animationState = value;
				SetDirtyBit(4096uL);
				if (!GetSyncVarHookGuard(4096uL) && base.IsHost)
				{
					SetSyncVarHookGuard(4096uL, value: true);
					PilotStateChanged(oldState, value);
					SetSyncVarHookGuard(4096uL, value: false);
				}
			}
		}
	}

	public Player Networkplayer
	{
		get
		{
			return (Player)player.Value;
		}
		set
		{
			if (!SyncVarEqual(value, (Player)this.player.Value))
			{
				Player player = (Player)this.player.Value;
				this.player.Value = value;
				SetDirtyBit(8192uL);
			}
		}
	}

	public bool NetworkisKinematic
	{
		get
		{
			return isKinematic;
		}
		set
		{
			if (!SyncVarEqual(value, isKinematic))
			{
				bool _ = isKinematic;
				isKinematic = value;
				SetDirtyBit(16384uL);
				if (!GetSyncVarHookGuard(16384uL) && base.IsHost)
				{
					SetSyncVarHookGuard(16384uL, value: true);
					OnKinematicChanged(_, value);
					SetSyncVarHookGuard(16384uL, value: false);
				}
			}
		}
	}

	public bool NetworkchuteDeployedNetwork
	{
		get
		{
			return chuteDeployedNetwork;
		}
		set
		{
			if (!SyncVarEqual(value, chuteDeployedNetwork))
			{
				bool flag = chuteDeployedNetwork;
				chuteDeployedNetwork = value;
				SetDirtyBit(32768uL);
			}
		}
	}

	public bool NetworkseatDetachedNetwork
	{
		get
		{
			return seatDetachedNetwork;
		}
		set
		{
			if (!SyncVarEqual(value, seatDetachedNetwork))
			{
				bool flag = seatDetachedNetwork;
				seatDetachedNetwork = value;
				SetDirtyBit(65536uL);
			}
		}
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float collisionDamage, PersistentID dealerID)
	{
		if (!disabled)
		{
			float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / armorProperties.pierceTolerance;
			float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) * amountAffected / armorProperties.blastTolerance;
			float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / armorProperties.fireTolerance;
			if (hitPoints - (num + num2 + num3) <= 0f)
			{
				base.Networkdisabled = true;
				SetPilotState(PilotState.dead);
			}
			RpcTakeDamage(num + num2 + num3);
		}
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		hitPoints -= pierceDamage;
	}

	[ClientRpc]
	public void RpcTakeDamage(float damage)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcTakeDamage_446604653(damage);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		writer.WriteSingleConverter(damage);
		ClientRpcSender.Send(this, 21, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void SetPilotState(PilotState state)
	{
		if (base.IsServer && animationState != state)
		{
			NetworkanimationState = state;
		}
	}

	private void PilotStateChanged(PilotState oldState, PilotState newState)
	{
		animator.SetInteger("PilotState", (int)newState);
		if (oldState == PilotState.dead)
		{
			return;
		}
		if (newState == PilotState.dead)
		{
			pilotCollider.enabled = false;
			deadCollider.enabled = true;
			animator.SetLayerWeight(1, 0f);
			if (ejectionSeat != null)
			{
				ejectionSeat.Detach();
			}
			base.rb.angularDrag = 1f;
			base.rb.drag = 5f;
		}
		if (newState == PilotState.landing)
		{
			base.rb.angularDrag = 5f;
			base.rb.drag = 5f;
		}
	}

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	public Unit GetUnit()
	{
		return this;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public new float GetMass()
	{
		return base.rb.mass;
	}

	public void TakeShockwave(Vector3 origin, float blastEffectScale, float blastPower)
	{
		float num = Vector3.Distance(base.transform.position, origin) / blastEffectScale;
		float a = 8000f / (num * num * num);
		float num2 = Mathf.Sqrt(base.rb.mass);
		a = Mathf.Min(a, 100f);
		float a2 = num2 * Mathf.Min(a, blastPower) * 5f;
		a2 = Mathf.Min(a2, 100f * base.rb.mass);
		Vector3 vector = FastMath.NormalizedDirection(origin, base.transform.position);
		base.rb.AddForce(vector * a2, ForceMode.Impulse);
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public void OnKinematicChanged(bool _, bool isKinematic)
	{
		if (base.IsServer)
		{
			base.NetworkstartPosition = this.GlobalPosition();
		}
		base.enabled = !isKinematic;
		base.rb.isKinematic = isKinematic;
		base.rb.interpolation = ((!isKinematic) ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
		animator.enabled = !isKinematic;
	}

	public void SetCollidable(bool enabled)
	{
		pilotCollider.enabled = enabled;
	}

	public override void Awake()
	{
		base.Awake();
		hitPoints = 100f;
		base.Identity.OnStartClient.AddListener(OnStartClient);
		base.Identity.OnStartServer.AddListener(OnStartServer);
	}

	private void OnStartServer()
	{
		Setup().Forget();
	}

	private void OnStartClient()
	{
		if (!base.IsServer)
		{
			Setup().Forget();
		}
	}

	private async UniTask Setup()
	{
		await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		SetRB(base.gameObject.GetComponent<Rigidbody>());
		SetLocalSim(base.IsServer);
		ejectionSeat.LinkToPilot(this);
		if (Networkplayer != null)
		{
			Networkplayer.SetPilotDismounted(this);
			pilotRank = Networkplayer.PlayerRank;
		}
		if (UnitRegistry.TryGetUnit((PersistentID?)parentUnit, out Aircraft unit))
		{
			Pilot pilot = unit.pilots[pilotNumber];
			cockpitPart = pilot.GetUnitPart();
			pilot.SetEjected();
			base.rb.velocity = cockpitPart.rb.velocity;
			bool num = unit.speed < 10f;
			if (Networkplayer == null)
			{
				pilotRank = unit.definition.aircraftParameters.rankRequired;
			}
			if (num || !unit.pilots[pilotNumber].HasEjectionSeat())
			{
				ejectionSeat.BailOut(base.rb, cockpitPart.rb);
				NetworkseatDetachedNetwork = true;
				seatDetachedLocal = true;
				base.rb.mass = 85f;
				if (base.LocalSim)
				{
					base.rb.velocity += Vector3.up * 2f;
					Vector3 vector = FindDismountSpot(unit);
					if (Physics.Linecast(vector, vector - Vector3.up * 20f, out hit, ~(int)PhysicsLayers.ExclusionZonesMask))
					{
						vector = hit.point + Vector3.up * 1.3f;
					}
					Quaternion rotation = ((cockpitPart != null) ? Quaternion.LookRotation(vector - cockpitPart.transform.position, Vector3.up) : Quaternion.identity);
					base.transform.SetPositionAndRotation(vector, rotation);
					base.rb.Move(vector, rotation);
					base.NetworkstartPosition = vector.ToGlobalPosition();
				}
				else
				{
					base.transform.SetPositionAndRotation(startPosition.ToLocalPosition(), unit.pilots[pilotNumber].transform.rotation);
					base.rb.Move(startPosition.ToLocalPosition(), unit.pilots[pilotNumber].transform.rotation);
				}
				SetCollidable(enabled: true);
			}
			else
			{
				base.transform.SetPositionAndRotation(unit.pilots[pilotNumber].transform.position, unit.pilots[pilotNumber].transform.rotation);
				base.rb.Move(unit.pilots[pilotNumber].transform.position, unit.pilots[pilotNumber].transform.rotation);
				ejectionSeat.Fire(cockpitPart);
			}
			if (unit == SceneSingleton<CameraStateManager>.i.followingUnit && pilotNumber == 0)
			{
				SceneSingleton<CameraStateManager>.i.SetFollowingUnit(this);
			}
			unit.pilots[pilotNumber].gameObject.SetActive(value: false);
			if (base.IsServer)
			{
				NetworkanimationState = PilotState.ejecting;
			}
			PilotStateChanged((PilotState)0, animationState);
		}
		else
		{
			if (!base.IsServer)
			{
				Vector3 position = startPosition.ToLocalPosition();
				base.transform.SetPositionAndRotation(position, startRotation);
				base.rb.MovePosition(position);
			}
			else
			{
				DetachSeat();
				NetworkanimationState = PilotState.detaching;
				PilotStateChanged((PilotState)0, animationState);
			}
			SetCollidable(enabled: true);
		}
		if (base.remoteSim && chuteDeployedNetwork)
		{
			DeployChute();
		}
		if (base.remoteSim && seatDetachedNetwork)
		{
			DetachSeat();
		}
		if (base.remoteSim && isKinematic)
		{
			OnKinematicChanged(_: false, isKinematic: true);
		}
		RegisterUnit(4f);
	}

	private Vector3 FindDismountSpot(Aircraft aircraft)
	{
		if (cockpitPart == null)
		{
			return base.transform.position;
		}
		Vector3 vector = cockpitPart.transform.position;
		int num = 1;
		int num2 = ((aircraft.pilots[pilotNumber].exitDirection != Pilot.ExitDirection.Left) ? 1 : (-1));
		while (num < 10)
		{
			vector = cockpitPart.transform.position + cockpitPart.transform.right * (num + 1) * num2;
			if (!Physics.Linecast(vector + Vector3.up * 10f, vector, out hit, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				return vector;
			}
			num++;
			num2 *= -1;
		}
		return vector;
	}

	private async UniTask CheckForCapture()
	{
		checkingForCapture = true;
		if (Networkplayer != null)
		{
			Networkplayer.RemovePilotDismounted(this);
		}
		List<GridSquare> gridSquares = BattlefieldGrid.GetGridSquaresInRange(base.transform.GlobalPosition(), 1000f);
		UnitRegistry.TryGetUnit(parentUnit, out var parentAircraft);
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(3000);
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		if (base.NetworkHQ.AnyNearAirbase(base.transform.position, out var _))
		{
			base.NetworkunitState = UnitState.Returned;
			captured = true;
			base.Networkdisabled = true;
			await UniTask.Delay(2000);
			if (!cancel.IsCancellationRequested)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			return;
		}
		while (!(hitPoints <= 0f))
		{
			if (slung)
			{
				await UniTask.Delay(5000, ignoreTimeScale: true);
				if (cancel.IsCancellationRequested)
				{
					break;
				}
				continue;
			}
			Unit capturingUnit = null;
			for (int i = 0; i < gridSquares.Count; i++)
			{
				for (int j = 0; j < gridSquares[i].units.Count; j++)
				{
					Unit unit = gridSquares[i].units[j];
					if (!(unit == null) && !unit.disabled && !(unit.NetworkHQ == null) && !(unit.radarAlt > 3f) && unit.definition.captureCapacity != 0 && !(unit == parentAircraft))
					{
						Aircraft aircraft = unit as Aircraft;
						if ((!(aircraft != null) || (!(aircraft.speed > 1f) && !(aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f))) && !FastMath.OutOfRange(base.transform.position, unit.transform.position, 500f))
						{
							capturingUnit = unit;
							break;
						}
					}
				}
			}
			if (capturingUnit != null)
			{
				await UniTask.Delay(2000);
				if (!cancel.IsCancellationRequested)
				{
					Capture(capturingUnit);
				}
				break;
			}
			await UniTask.Delay(5000, ignoreTimeScale: true);
			if (cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	[Server]
	public void Capture(Unit capturingUnit)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'Capture' called when server not active");
		}
		captured = true;
		bool flag = capturingUnit.NetworkHQ == base.NetworkHQ;
		if (!flag)
		{
			capturingUnit.NetworkHQ.AddScore(2f * (1f + (float)pilotRank));
		}
		if (capturingUnit is Aircraft aircraft && aircraft.Player != null)
		{
			if (!flag)
			{
				base.NetworkHQ.ReportCapturePilotsAction(aircraft.Player, this);
			}
			else
			{
				base.NetworkHQ.ReportRescuePilotsAction(aircraft.Player, this);
			}
		}
		NetworkSceneSingleton<MessageManager>.i.RpcPilotCaptureMessage(persistentID, capturingUnit.persistentID, flag);
		if (flag)
		{
			base.NetworkunitState = UnitState.Returned;
		}
		else
		{
			base.NetworkunitState = UnitState.Destroyed;
		}
		base.Networkdisabled = true;
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		if (!captured)
		{
			if (Physics.Raycast(base.transform.position, -Vector3.up, out hit, float.MaxValue, PhysicsLayers.StaticsMask))
			{
				pilotCollider.enabled = false;
			}
			if (NetworkManagerNuclearOption.i.Server.Active)
			{
				WaitDespawn().Forget();
			}
		}
	}

	private async UniTask WaitDespawn()
	{
		await UniTask.Delay(60000);
		if (!(this == null))
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public override bool IsSlung()
	{
		return slung;
	}

	private void Update()
	{
		timeSinceSpawn += Time.deltaTime;
		if (chuteDeployedLocal)
		{
			chuteOpenTime += Time.deltaTime;
		}
		if (!seatDetachedLocal && timeSinceSpawn > 2f && (animationState == PilotState.dead || (radarAlt < 1000f && base.rb.velocity.sqrMagnitude > 100f)))
		{
			DetachSeat();
			if (base.LocalSim)
			{
				SetPilotState(PilotState.detaching);
			}
		}
		if (base.LocalSim && animationState != PilotState.dead && chuteOpenTime > 1f && radarAlt > 10f && !slung)
		{
			SetPilotState(PilotState.parachuting);
		}
	}

	public override void CheckRadarAlt()
	{
		if (!(Time.timeSinceLevelLoad > lastAltitudeCheck + 0.1f))
		{
			return;
		}
		lastAltitudeCheck = Time.timeSinceLevelLoad;
		Vector3 velocity = base.rb.velocity;
		if (Physics.Linecast(base.transform.position, base.transform.position - Vector3.up * 10000f, out hit, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask))
		{
			radarAlt = hit.distance;
			if (hit.collider.attachedRigidbody != null)
			{
				velocity -= hit.collider.attachedRigidbody.GetPointVelocity(hit.point);
			}
		}
		else
		{
			radarAlt = base.transform.position.GlobalY();
		}
		if (inWater)
		{
			radarAlt = 0f;
		}
		speed = velocity.magnitude;
	}

	private void CheckLanded()
	{
		if (base.NetworkHQ != null && (inWater || landedTime > 0f) && !disabled && !checkingForCapture && base.IsServer)
		{
			CheckForCapture().Forget();
		}
		if (landedTime > 35f && base.IsServer && !slung)
		{
			NetworkisKinematic = true;
		}
	}

	public void DeployChute()
	{
		chuteDeployedLocal = true;
		NetworkchuteDeployedNetwork = true;
	}

	private void DetachSeat()
	{
		seatDetachedLocal = true;
		NetworkseatDetachedNetwork = true;
		if (ejectionSeat != null)
		{
			ejectionSeat.Detach();
		}
	}

	private void KillPilot()
	{
		DisableUnit();
		SetPilotState(PilotState.dead);
	}

	private void FixedUpdate()
	{
		inWater = base.transform.position.GlobalY() < 0f;
		CheckRadarAlt();
		landedTime = ((radarAlt < 2f) ? (landedTime + Time.deltaTime) : 0f);
		if (base.LocalSim)
		{
			LocalFixedUpdate();
		}
		runWeight = Mathf.Lerp(runWeight, speed * 2f - 0.5f, 6f * Time.fixedDeltaTime);
		runWeight = Mathf.Clamp(runWeight, 0f, 1f);
		if (animationState == PilotState.landing && !inWater)
		{
			animator.SetLayerWeight(1, runWeight);
			animator.SetFloat("RunSpeed", speed * 0.25f);
		}
		else
		{
			animator.SetLayerWeight(1, 0f);
		}
	}

	private bool TooMuchForce()
	{
		if (!velocityPrev.HasValue)
		{
			return false;
		}
		float range = 500f * Time.fixedDeltaTime;
		return FastMath.OutOfRange(velocityPrev.Value, base.rb.velocity, range);
	}

	private void LocalFixedUpdate()
	{
		CheckLanded();
		if (speed > 30f && Physics.Raycast(base.transform.position, base.rb.velocity, out var _, speed * 1.1f * Time.fixedDeltaTime, PhysicsLayers.StaticsMask))
		{
			base.transform.position = hit.point + Vector3.up * 1f;
			base.rb.velocity = Vector3.Reflect(base.rb.velocity, hit.normal) * 0.3f;
		}
		if (inWater)
		{
			radarAlt = 0f;
			base.rb.AddForce(Vector3.up * base.rb.mass * 25f * Mathf.Clamp01(Datum.LocalSeaY - base.transform.position.y));
			base.rb.AddTorque(Vector3.Cross(base.transform.up, Vector3.up) * base.rb.mass * 8f, ForceMode.Force);
			if (animationState != PilotState.ejecting)
			{
				SetPilotState(PilotState.ejecting);
			}
		}
		if (!disabled && TooMuchForce())
		{
			KillPilot();
		}
		velocityPrev = base.rb.velocity;
		Vector3 velocity = base.rb.velocity;
		base.rb.drag = 0.1f;
		if (animationState != PilotState.dead && radarAlt < 20f && speed < 10f && Physics.Linecast(base.transform.position, base.transform.position - Vector3.up * 1.5f, out hit, ~(int)PhysicsLayers.ExclusionZonesMask))
		{
			Vector3 vector = ((hit.collider.attachedRigidbody != null) ? hit.collider.attachedRigidbody.GetPointVelocity(hit.point) : Vector3.zero);
			velocity -= vector;
			Vector3 vector2 = (1.3f - hit.distance - base.rb.velocity.y * 0.2f) * Vector3.up * base.rb.mass * 25f;
			base.rb.AddForce(vector2 - velocity * base.rb.mass * 2f);
			base.rb.AddTorque(Vector3.Cross(base.transform.up, Vector3.up) * base.rb.mass * 4f, ForceMode.Force);
			if (cockpitPart != null && cockpitPart.parentUnit.disabled && cockpitPart.parentUnit.unitState != UnitState.Abandoned && cockpitPart.parentUnit.unitState != UnitState.Returned && FastMath.InRange(cockpitPart.transform.position, base.transform.position, 10f))
			{
				Vector3 vector3 = base.transform.position - cockpitPart.transform.position;
				vector3.y = 0f;
				base.rb.AddForce(vector3.normalized * base.rb.mass * 8f);
			}
			if (!slung && animationState != PilotState.landing)
			{
				SetPilotState(PilotState.landing);
			}
		}
		if (inWater)
		{
			base.rb.drag = 20f;
			base.rb.angularDrag = 5f;
		}
	}

	public override void AttachOrDetachSlingHook(Aircraft aircraft, bool attached)
	{
		base.AttachOrDetachSlingHook(aircraft, attached);
		slung = attached;
		if (base.IsServer)
		{
			NetworkisKinematic = false;
			SetPilotState(PilotState.parachuting);
		}
		if (!attached)
		{
			inWater = false;
			landedTime = 0f;
		}
	}

	public int GetPilotRank()
	{
		return pilotRank;
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
			GeneratedNetworkCode._Write_PersistentID(writer, parentUnit);
			writer.WriteByteExtension(unitPart);
			writer.WriteByteExtension(pilotNumber);
			GeneratedNetworkCode._Write_PilotDismounted_002FPilotState(writer, animationState);
			writer.WriteNetworkBehaviorSyncVar(player);
			writer.WriteBooleanExtension(isKinematic);
			writer.WriteBooleanExtension(chuteDeployedNetwork);
			writer.WriteBooleanExtension(seatDetachedNetwork);
			return true;
		}
		writer.Write((ulong)((long)syncVarDirtyBits >> 9), 8);
		if ((syncVarDirtyBits & 0x200L) != 0L)
		{
			GeneratedNetworkCode._Write_PersistentID(writer, parentUnit);
			result = true;
		}
		if ((syncVarDirtyBits & 0x400L) != 0L)
		{
			writer.WriteByteExtension(unitPart);
			result = true;
		}
		if ((syncVarDirtyBits & 0x800L) != 0L)
		{
			writer.WriteByteExtension(pilotNumber);
			result = true;
		}
		if ((syncVarDirtyBits & 0x1000L) != 0L)
		{
			GeneratedNetworkCode._Write_PilotDismounted_002FPilotState(writer, animationState);
			result = true;
		}
		if ((syncVarDirtyBits & 0x2000L) != 0L)
		{
			writer.WriteNetworkBehaviorSyncVar(player);
			result = true;
		}
		if ((syncVarDirtyBits & 0x4000L) != 0L)
		{
			writer.WriteBooleanExtension(isKinematic);
			result = true;
		}
		if ((syncVarDirtyBits & 0x8000L) != 0L)
		{
			writer.WriteBooleanExtension(chuteDeployedNetwork);
			result = true;
		}
		if ((syncVarDirtyBits & 0x10000L) != 0L)
		{
			writer.WriteBooleanExtension(seatDetachedNetwork);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			parentUnit = GeneratedNetworkCode._Read_PersistentID(reader);
			unitPart = reader.ReadByteExtension();
			pilotNumber = reader.ReadByteExtension();
			PilotState pilotState = animationState;
			animationState = GeneratedNetworkCode._Read_PilotDismounted_002FPilotState(reader);
			player = reader.ReadNetworkBehaviourSyncVar();
			bool flag = isKinematic;
			isKinematic = reader.ReadBooleanExtension();
			chuteDeployedNetwork = reader.ReadBooleanExtension();
			seatDetachedNetwork = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(pilotState, animationState))
			{
				PilotStateChanged(pilotState, animationState);
			}
			if (!base.IsServer && !SyncVarEqual(flag, isKinematic))
			{
				OnKinematicChanged(flag, isKinematic);
			}
			return;
		}
		ulong num = reader.Read(8);
		SetDeserializeMask(num, 9);
		if ((num & 1L) != 0L)
		{
			parentUnit = GeneratedNetworkCode._Read_PersistentID(reader);
		}
		if ((num & 2L) != 0L)
		{
			unitPart = reader.ReadByteExtension();
		}
		if ((num & 4L) != 0L)
		{
			pilotNumber = reader.ReadByteExtension();
		}
		if ((num & 8L) != 0L)
		{
			PilotState pilotState2 = animationState;
			animationState = GeneratedNetworkCode._Read_PilotDismounted_002FPilotState(reader);
			if (!base.IsServer && !SyncVarEqual(pilotState2, animationState))
			{
				PilotStateChanged(pilotState2, animationState);
			}
		}
		if ((num & 0x10L) != 0L)
		{
			player = reader.ReadNetworkBehaviourSyncVar();
		}
		if ((num & 0x20L) != 0L)
		{
			bool flag2 = isKinematic;
			isKinematic = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(flag2, isKinematic))
			{
				OnKinematicChanged(flag2, isKinematic);
			}
		}
		if ((num & 0x40L) != 0L)
		{
			chuteDeployedNetwork = reader.ReadBooleanExtension();
		}
		if ((num & 0x80L) != 0L)
		{
			seatDetachedNetwork = reader.ReadBooleanExtension();
		}
	}

	public void UserCode_RpcTakeDamage_446604653(float damage)
	{
		ApplyDamage(damage, 0f, 0f, 0f);
	}

	protected static void Skeleton_RpcTakeDamage_446604653(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((PilotDismounted)behaviour).UserCode_RpcTakeDamage_446604653(reader.ReadSingleConverter());
	}

	protected override int GetRpcCount()
	{
		return 22;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(21, "PilotDismounted.RpcTakeDamage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcTakeDamage_446604653, RpcRateLimitConfig.Disabled());
	}
}
