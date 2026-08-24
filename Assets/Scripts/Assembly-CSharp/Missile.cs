using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.Networking;
using UnityEngine;

public class Missile : Unit, IDamageable, IRadarReturn, ICommandable
{
	[Serializable]
	public class FoldingFin
	{
		[SerializeField]
		private Transform fin;

		[SerializeField]
		private Vector3 foldAngle;

		[SerializeField]
		private Vector3 deployAngle;

		[SerializeField]
		private float deploySpeed = 1f;

		private float deployedAmount;

		public bool UnfoldFin()
		{
			deployedAmount = Mathf.Min(deployedAmount + Time.fixedDeltaTime * deploySpeed, 1f);
			fin.transform.localEulerAngles = Vector3.Lerp(foldAngle, deployAngle, deployedAmount);
			return deployedAmount == 1f;
		}
	}

	private class ProxyFuse
	{
		private Transform targetTransform;

		private Transform missileTransform;

		private Rigidbody targetRB;

		private Rigidbody missileRB;

		public ProxyFuse(Transform missileTransform, Rigidbody missileRB)
		{
			this.missileTransform = missileTransform;
			this.missileRB = missileRB;
		}

		public void SetTarget(Transform targetTransform, Rigidbody targetRB)
		{
			this.targetTransform = targetTransform;
			this.targetRB = targetRB;
		}

		public bool ConditionsMet(Vector3 targetVelocity, float missileSpeed)
		{
			if (targetTransform == null || FastMath.OutOfRange(targetTransform.position, missileTransform.position, missileSpeed * 0.25f))
			{
				return false;
			}
			Vector3 vector = ((targetTransform != null && targetRB != null) ? targetRB.velocity : targetVelocity);
			Vector3 vector2 = missileTransform.position - targetTransform.position;
			Vector3 vector3 = missileRB.velocity - vector;
			Vector3 rhs = vector2 + Time.fixedDeltaTime * 1.1f * vector3;
			if (Vector3.Dot(vector3, rhs) > 0f)
			{
				missileTransform.position += Vector3.Project(-vector2, vector3);
				missileRB.MovePosition(missileTransform.position);
				return true;
			}
			return false;
		}
	}

	public enum SeekerMode : byte
	{
		activeLock = 1,
		activeSearch = 2,
		passive = 3
	}

	[Serializable]
	public class Warhead
	{
		public bool Armed = true;

		[SerializeField]
		private GameObject airEffect;

		[SerializeField]
		private GameObject armorEffect;

		[SerializeField]
		private GameObject terrainEffect;

		[SerializeField]
		private GameObject waterSurfaceEffect;

		[SerializeField]
		private GameObject underwaterEffect;

		[SerializeField]
		private GameObject fizzleEffect;

		private bool detonated;

		public void Arm()
		{
			Armed = true;
		}

		public void Detonate(Rigidbody rb, PersistentID ownerID, Vector3 position, Vector3 normal, bool armed, float blastYield, bool hitArmor, bool hitTerrain)
		{
			if (detonated)
			{
				return;
			}
			detonated = true;
			if (!armed)
			{
				if (fizzleEffect != null)
				{
					UnityEngine.Object.Instantiate(fizzleEffect, Datum.origin).transform.SetPositionAndRotation(rb.position, FastMath.LookRotation(rb.velocity));
				}
				return;
			}
			float num = Mathf.Pow(blastYield, 0.3333f) * 2f;
			GameObject gameObject = null;
			bool num2 = position.y < Datum.LocalSeaY + 0.1f;
			Vector3 position2 = new Vector3(position.x, Datum.LocalSeaY, position.z);
			if (num2)
			{
				gameObject = UnityEngine.Object.Instantiate(underwaterEffect, Datum.origin);
				gameObject.transform.SetPositionAndRotation(position2, Quaternion.identity);
			}
			else
			{
				if (hitTerrain)
				{
					gameObject = UnityEngine.Object.Instantiate(terrainEffect, Datum.origin);
					gameObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
				}
				if (hitArmor)
				{
					gameObject = UnityEngine.Object.Instantiate(armorEffect, Datum.origin);
					gameObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
				}
				RaycastHit hitInfo;
				bool flag = hitTerrain || (Physics.Linecast(position, position - Vector3.up * num, out hitInfo, PhysicsLayers.StaticsMask) && hitInfo.point.y > Datum.LocalSeaY);
				if (waterSurfaceEffect != null && !flag && position.y < Datum.LocalSeaY + num && position.y > Datum.LocalSeaY + 1f)
				{
					GameObject gameObject2 = UnityEngine.Object.Instantiate(waterSurfaceEffect, Datum.origin);
					gameObject2.transform.SetPositionAndRotation(position2, Quaternion.identity);
					UnityEngine.Object.Destroy(gameObject2, 30f);
				}
			}
			if (gameObject == null && airEffect != null)
			{
				gameObject = UnityEngine.Object.Instantiate(airEffect, Datum.origin);
				gameObject.transform.SetPositionAndRotation(position, FastMath.LookRotation(normal));
			}
			if (blastYield > 200f)
			{
				Shockwave componentInChildren = gameObject.GetComponentInChildren<Shockwave>();
				if (componentInChildren != null)
				{
					componentInChildren.SetOwner(ownerID, blastYield * 1E-06f);
				}
			}
			else
			{
				UnityEngine.Object.Destroy(gameObject, 30f);
			}
		}
	}

	[Serializable]
	private class Motor
	{
		[SerializeField]
		private bool activated;

		[SerializeField]
		private float delayTimer;

		[SerializeField]
		private float thrustVectoring;

		public float thrust;

		public float burnTime;

		public float fuelMass;

		public float topSpeed = 299792450f;

		public float IR_intensity = 1f;

		private float burnRate;

		[SerializeField]
		private ParticleSystem[] particleSystems;

		[SerializeField]
		private TrailEmitter[] trailEmitters;

		[SerializeField]
		public AudioSource[] audioSources;

		[SerializeField]
		private AudioSource startupSource;

		[SerializeField]
		private Light[] lights;

		[SerializeField]
		private GameObject[] destructEffects;

		private void Activate(Missile missile)
		{
			if (!missile.HasIRSignature())
			{
				missile.AddIRSource(new IRSource(missile.transform, IR_intensity, flare: false));
			}
			burnRate = fuelMass / burnTime;
			ParticleSystem[] array = particleSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
			AudioSource[] array2 = audioSources;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Play();
			}
			Light[] array3 = lights;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].enabled = true;
			}
			TrailEmitter[] array4 = trailEmitters;
			for (int i = 0; i < array4.Length; i++)
			{
				array4[i].StartTrail();
			}
		}

		public void Destruct(Missile missile)
		{
			Burnout(forceStopEffects: true);
			if (missile.transform.position.y > Datum.LocalSeaY)
			{
				GameObject[] array = destructEffects;
				for (int i = 0; i < array.Length; i++)
				{
					UnityEngine.Object.Instantiate(array[i], missile.transform);
				}
			}
		}

		public void Burnout(bool forceStopEffects)
		{
			ParticleSystem[] array = particleSystems;
			foreach (ParticleSystem particleSystem in array)
			{
				if (forceStopEffects || particleSystem.main.loop)
				{
					particleSystem.Stop();
				}
			}
			if (forceStopEffects)
			{
				TrailEmitter[] array2 = trailEmitters;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].StopTrail();
				}
			}
			AudioSource[] array3 = audioSources;
			foreach (AudioSource audioSource in array3)
			{
				if (forceStopEffects || audioSource.loop)
				{
					audioSource.Stop();
				}
			}
			if (startupSource != null)
			{
				startupSource.Stop();
			}
			Light[] array4 = lights;
			for (int i = 0; i < array4.Length; i++)
			{
				array4[i].enabled = false;
			}
		}

		public float Thrust(Missile missile, bool localSim, Vector3 inputs, float throttle = 1f)
		{
			if (delayTimer > 0f)
			{
				delayTimer -= Time.deltaTime;
				if (startupSource != null && !startupSource.isPlaying)
				{
					startupSource.Play();
				}
				return 0f;
			}
			if (!activated)
			{
				activated = true;
				Activate(missile);
			}
			fuelMass -= burnRate * Time.deltaTime;
			missile.rb.mass -= burnRate * Time.deltaTime;
			if (fuelMass <= 0f)
			{
				Burnout(forceStopEffects: false);
			}
			if (thrustVectoring > 0f)
			{
				ParticleSystem[] array = particleSystems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].transform.localEulerAngles = new Vector3(inputs.x * thrustVectoring, 180f + (0f - inputs.y) * thrustVectoring, 0f);
				}
			}
			if (localSim && missile.speed < topSpeed)
			{
				missile.rb.AddForce(thrust * throttle * missile.transform.forward);
			}
			return thrust;
		}

		public float GetRemainingDeltaV(float currentMass)
		{
			if (burnRate == 0f)
			{
				burnRate = fuelMass / burnTime;
			}
			float num = currentMass - fuelMass;
			float num2 = fuelMass / burnRate;
			return thrust * num2 / ((currentMass + num) * 0.5f);
		}

		public float GetRemainingBurnTime()
		{
			if (burnRate == 0f)
			{
				burnRate = fuelMass / burnTime;
			}
			return fuelMass / burnRate;
		}

		public void TerminalBoost(float value)
		{
			thrust = value;
		}
	}

	[SyncVar]
	public PersistentID ownerID;

	[SyncVar(hook = "TargetIDChanged")]
	private PersistentID _targetID;

	[SyncVar]
	public Vector3 startingVelocity;

	[SyncVar]
	public Vector3 startOffsetFromOwner;

	[SyncVar]
	public SeekerMode seekerMode = SeekerMode.passive;

	private Unit target;

	[Header("Physics")]
	[SerializeField]
	private Motor[] motors;

	[SerializeField]
	private float mass;

	[SerializeField]
	private float finArea;

	[SerializeField]
	private float uprightPreference;

	[SerializeField]
	private float supersonicDrag;

	[SerializeField]
	private AnimationCurve liftCurve;

	[SerializeField]
	private AnimationCurve dragCurve;

	[SerializeField]
	private FoldingFin[] foldingFins;

	[SerializeField]
	private ArmorProperties armorProperties;

	private float hitpoints;

	private float engineCurrentThrust;

	private Vector3 localAngularVel;

	private int motorStage;

	private Motor motor;

	[Header("Targeting")]
	[SerializeField]
	private PIDFactors PIDFactors;

	[SerializeField]
	private float torque;

	[SerializeField]
	private float gLimit;

	[SerializeField]
	private float maxTurnRate;

	private float onTargetSmoothed;

	private Vector3 inputs;

	private PID2D pid;

	[Header("Effects")]
	[SerializeField]
	private Transform effectsTransform;

	[SerializeField]
	private AudioSource flightSound;

	[SerializeField]
	private AudioClip nearbyDetonationClip;

	[SerializeField]
	private float basePitch = 0.5f;

	[SerializeField]
	private float pitchRange = 1f;

	[SerializeField]
	private float maxPitchSpeed = 340f;

	[Header("Payload")]
	[SerializeField]
	private Warhead warhead;

	[SerializeField]
	private float blastYield;

	[SerializeField]
	private float pierceDamage;

	private ProxyFuse proxyFuse;

	[SerializeField]
	private bool impactFuse;

	[SerializeField]
	private float impactFuseDelay;

	[Header("Unit")]
	[SerializeField]
	private WeaponInfo info;

	[SerializeField]
	private UnitCommand unitCommand;

	public bool boosterIsAttached;

	private float currentFinArea = 0.01f;

	private float throttle = 1f;

	private bool ignition;

	private bool tangible;

	private bool reachedOnTarget;

	private MissileSeeker seeker;

	[SerializeField]
	private GlobalPosition aimPoint;

	private Vector3 targetVel;

	private Vector3 torqueAxes;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 14;

	[NonSerialized]
	private const int RPC_COUNT = 23;

	public PersistentID targetID => _targetID;

	public Unit owner { get; private set; }

	public float timeSinceSpawn { get; private set; }

	public UnitCommand UnitCommand => unitCommand;

	bool ICommandable.Disabled => disabled;

	FactionHQ ICommandable.HQ => base.NetworkHQ;

	public PersistentID NetworkownerID
	{
		get
		{
			return ownerID;
		}
		set
		{
			if (!SyncVarEqual(value, ownerID))
			{
				PersistentID persistentID = ownerID;
				ownerID = value;
				SetDirtyBit(512uL);
			}
		}
	}

	public PersistentID Network_targetID
	{
		get
		{
			return _targetID;
		}
		set
		{
			if (!SyncVarEqual(value, _targetID))
			{
				PersistentID oldValue = _targetID;
				_targetID = value;
				SetDirtyBit(1024uL);
				if (!GetSyncVarHookGuard(1024uL) && base.IsHost)
				{
					SetSyncVarHookGuard(1024uL, value: true);
					TargetIDChanged(oldValue, value);
					SetSyncVarHookGuard(1024uL, value: false);
				}
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
				SetDirtyBit(2048uL);
			}
		}
	}

	public Vector3 NetworkstartOffsetFromOwner
	{
		get
		{
			return startOffsetFromOwner;
		}
		set
		{
			if (!SyncVarEqual(value, startOffsetFromOwner))
			{
				Vector3 vector = startOffsetFromOwner;
				startOffsetFromOwner = value;
				SetDirtyBit(4096uL);
			}
		}
	}

	public SeekerMode NetworkseekerMode
	{
		get
		{
			return seekerMode;
		}
		set
		{
			if (!SyncVarEqual(value, this.seekerMode))
			{
				SeekerMode seekerMode = this.seekerMode;
				this.seekerMode = value;
				SetDirtyBit(8192uL);
			}
		}
	}

	public event Action<Aircraft.OnRadarWarning> onRadarPing;

	public override void Awake()
	{
		base.Awake();
		seeker = base.gameObject.GetComponent<MissileSeeker>();
		base.Identity.OnStartClient.AddListener(OnStartClient);
		base.Identity.OnStartServer.AddListener(OnStartServer);
		SetTangible(tangible: false);
	}

	public override void OnEnable()
	{
		radarAlt = 1000f;
		hitpoints = 100f;
		currentFinArea = 0.1f * finArea;
		base.OnEnable();
		if (GameManager.gameState == GameState.Encyclopedia)
		{
			base.enabled = false;
			base.rb.isKinematic = true;
		}
	}

	public bool IsTangible()
	{
		return tangible;
	}

	public void SetTangible(bool tangible)
	{
		this.tangible = tangible;
		if (tangible)
		{
			int layer = PhysicsLayers.Default;
			base.transform.gameObject.layer = layer;
		}
		else
		{
			int ignoreCollisions = PhysicsLayers.IgnoreCollisions;
			base.transform.gameObject.layer = ignoreCollisions;
		}
	}

	public WeaponInfo GetWeaponInfo()
	{
		return info;
	}

	public float GetYield()
	{
		return blastYield;
	}

	public float GetPierce()
	{
		return pierceDamage;
	}

	public float CalcDeltaV()
	{
		float num = 0f;
		float num2 = mass;
		for (int i = 0; i < motors.Length; i++)
		{
			Motor motor = motors[i];
			float num3 = motor.thrust * motor.burnTime / (num2 - (num2 - motor.fuelMass));
			num += num3 * Mathf.Log(num2 / (num2 - motor.fuelMass), MathF.E);
			num2 -= motor.fuelMass;
		}
		return num;
	}

	public float GetTopSpeed(float launchAltitude, float targetAltitude)
	{
		if (motors.Length == 0)
		{
			return 0f;
		}
		float b = CalcDeltaV();
		float dragCoef = GetDragCoef(MathF.PI / 360f);
		float num = GameAssets.i.airDensityAltitude.Evaluate(Mathf.Lerp(launchAltitude, targetAltitude, 0.5f) * 0.001f);
		return Mathf.Min(Mathf.Sqrt(motors[^1].thrust / (dragCoef * num * 0.5f * finArea)), b);
	}

	public float CalcRange(float launchSpeed, float launchAltitude, float targetAltitude, float targetDist, float targetRelativeSpeed, out float noEscapeDistance)
	{
		float num = GameAssets.i.airDensityAltitude.Evaluate(Mathf.Lerp(launchAltitude, targetAltitude, 0.5f) * 0.001f);
		float num2 = CalcDeltaV();
		float dragCoef = GetDragCoef(MathF.PI / 360f);
		float minSpeed = GetComponent<MissileSeeker>().GetMinSpeed();
		noEscapeDistance = 0f;
		float num3 = 0f;
		float num4 = mass;
		Motor[] array = motors;
		foreach (Motor motor in array)
		{
			num3 += motor.burnTime;
			num4 -= motor.fuelMass;
		}
		float num5 = -0.1f;
		if (targetDist > 0f)
		{
			num5 = Mathf.Clamp((targetAltitude - launchAltitude) / targetDist, -0.5f, 0.5f);
		}
		float num6 = Mathf.Abs(launchAltitude - targetAltitude);
		float num7 = 0f;
		float num8 = 0f;
		float num9 = launchSpeed;
		float num10 = launchSpeed;
		if (motors.Length != 0)
		{
			float num11 = Mathf.Sqrt(motors[^1].thrust / (dragCoef * num * 0.5f * finArea));
			num10 = Mathf.Min(launchSpeed + num2, num11);
			num7 = ((GetTotalBurnTime() < 30f) ? (Mathf.Lerp(launchSpeed, num10, 0.5f) * num3) : (num11 * num3));
			num9 = num10;
		}
		float num12 = 0.1f;
		int num13 = 0;
		float num14 = 0.5f * dragCoef * num * finArea / num4;
		bool flag = true;
		while (flag && num13 < 120)
		{
			num13++;
			num7 += num12 * num9;
			num8 += num12;
			if (num6 > 0f)
			{
				float num15 = 0.5f * num4 * num9 * num9;
				float num16 = num12 * num5 * num9;
				num6 -= Mathf.Abs(num16);
				float num17 = num15 + num4 * -9.81f * num16;
				num9 = Mathf.Sqrt(2f * num17 / num4);
			}
			num9 -= num12 * num9 * num9 * num14;
			num12 += 0.05f;
			if (num13 > 10)
			{
				if (num9 < minSpeed)
				{
					flag = false;
				}
				if (noEscapeDistance == 0f && num9 < targetRelativeSpeed)
				{
					noEscapeDistance = num7;
					noEscapeDistance -= targetRelativeSpeed * num8;
					targetRelativeSpeed = 0f;
				}
			}
		}
		if (noEscapeDistance <= 0f)
		{
			noEscapeDistance = num7 - num8 * targetRelativeSpeed;
		}
		return num7;
	}

	private void OnStartServer()
	{
		OnStartNetwork();
	}

	private void OnStartClient()
	{
		if (!base.IsServer)
		{
			OnStartNetwork();
		}
	}

	private void OnStartNetwork()
	{
		StartMissile();
		if (base.LocalSim)
		{
			LocalStart();
		}
	}

	private void StartMissile()
	{
		SetLocalSim(NetworkManagerNuclearOption.i.Server.Active);
		if (UnitRegistry.TryGetUnit(ownerID, out var foundOwner))
		{
			owner = foundOwner;
			owner.RegisterMissile(this);
		}
		else
		{
			Arm();
		}
		TargetIDChanged(PersistentID.None, targetID);
		if (base.LocalSim)
		{
			pid = new PID2D(PIDFactors, 1f, 0.1f);
			if (foundOwner != null)
			{
				UniTask.Void(async delegate
				{
					await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
					base.transform.position = foundOwner.transform.position + startOffsetFromOwner;
					base.rb.MovePosition(foundOwner.transform.position + startOffsetFromOwner);
					if (owner is Missile missile)
					{
						_ = missile.ownerID;
						if (missile.ownerID.TryGetUnit(out var unit))
						{
							owner = unit;
							NetworkownerID = missile.ownerID;
						}
					}
				});
			}
		}
		else
		{
			base.rb.useGravity = false;
			if (foundOwner != null)
			{
				Vector3 position = startOffsetFromOwner + foundOwner.transform.position;
				base.transform.position = position;
				base.rb.MovePosition(position);
			}
			GetComponent<Collider>().enabled = false;
		}
		SetRB(base.gameObject.GetComponent<Rigidbody>());
		base.rb.mass = mass;
		base.rb.MoveRotation(startRotation);
		base.rb.velocity = startingVelocity;
		torqueAxes = base.rb.inertiaTensor * torque;
		RegisterUnit(1f);
		InitializeUnit();
		CheckExclusionZone();
		if (flightSound != null)
		{
			RegisterDopplerSound(flightSound);
			flightSound.volume = 0f;
			flightSound.Play();
		}
		Motor[] array = motors;
		for (int num = 0; num < array.Length; num++)
		{
			AudioSource[] audioSources = array[num].audioSources;
			foreach (AudioSource audioSource in audioSources)
			{
				RegisterDopplerSound(audioSource);
			}
		}
	}

	private void CheckExclusionZone()
	{
		if (base.LocalSim && blastYield > 100000f && target != null)
		{
			float num = Mathf.Pow(blastYield, 0.3333f) * 13f;
			if (base.NetworkHQ.TryGetKnownPosition(target, out var knownPosition))
			{
				base.NetworkHQ.AddExclusionZone(this, knownPosition, num * 2f);
			}
		}
	}

	private void LocalStart()
	{
		if (GameManager.gameState == GameState.SinglePlayer || GameManager.gameState == GameState.Multiplayer)
		{
			GlobalPosition globalPosition = ((owner != null) ? owner.transform.GlobalPosition() : base.transform.GlobalPosition());
			Vector3 vector = ((owner != null) ? owner.transform.forward : base.transform.forward);
			aimPoint = globalPosition + vector * 100000f;
			seeker.Initialize(target, aimPoint);
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

	public void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
		float num = Mathf.Sqrt(base.rb.mass);
		overpressure = Mathf.Min(overpressure, 100f);
		float a = num * overpressure * 2f;
		a = Mathf.Min(a, 30f * base.rb.mass);
		Vector3 vector = FastMath.NormalizedDirection(origin, base.transform.position);
		base.rb.AddForce(vector * a, ForceMode.Impulse);
	}

	public override float GetMass()
	{
		return mass;
	}

	public override float GetPrefabMass()
	{
		return mass;
	}

	public float GetFinArea()
	{
		return finArea;
	}

	public float GetTorque()
	{
		return torque;
	}

	public float GetMaxTurnRate()
	{
		return 90f;
	}

	public void SetTorque(float torque, float maxTurnRate)
	{
		this.torque = torque;
		torqueAxes = base.rb.inertiaTensor * torque;
		if (base.LocalSim)
		{
			pid.SetPLimit(maxTurnRate);
		}
	}

	public void SetThrottle(float throttle)
	{
		this.throttle = throttle;
	}

	public void DeployFins()
	{
		if (foldingFins.Length != 0)
		{
			RpcUnfoldFins();
		}
		else
		{
			currentFinArea = finArea;
		}
	}

	[ClientRpc]
	private void RpcUnfoldFins()
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcUnfoldFins_1465559174();
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		ClientRpcSender.Send(this, 21, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private async UniTask UnfoldFins()
	{
		bool finishedDeploying = false;
		CancellationToken cancel = base.destroyCancellationToken;
		while (!finishedDeploying)
		{
			currentFinArea = Mathf.Lerp(currentFinArea, finArea, 0.1f);
			FoldingFin[] array = foldingFins;
			foreach (FoldingFin foldingFin in array)
			{
				finishedDeploying = foldingFin.UnfoldFin();
			}
			await UniTask.Yield();
			if (cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public float GetDragCoef(float a)
	{
		return dragCurve.Evaluate(a);
	}

	public float GetTotalBurnTime()
	{
		float num = 0f;
		Motor[] array = motors;
		foreach (Motor motor in array)
		{
			num += motor.burnTime;
		}
		return num;
	}

	public float GetRemainingBurnTime()
	{
		if (motor == null)
		{
			if (motors.Length == 0)
			{
				return 0f;
			}
			return motors[0].GetRemainingBurnTime();
		}
		return motor.GetRemainingBurnTime();
	}

	public float GetRemainingDeltaV()
	{
		if (motor == null)
		{
			if (motors.Length == 0)
			{
				return 0f;
			}
			return motors[0].GetRemainingDeltaV(base.rb.mass);
		}
		return motor.GetRemainingDeltaV(base.rb.mass);
	}

	public bool EngineOn()
	{
		return engineCurrentThrust > 0f;
	}

	public float GetLiftCoeff(float a)
	{
		return liftCurve.Evaluate(a);
	}

	public float GetThrust()
	{
		if (motors.Length != 0 && motors[0] != null)
		{
			return motors[0].thrust;
		}
		return 0f;
	}

	public float GetThrustDuration()
	{
		if (motors.Length != 0 && motors[0] != null)
		{
			float num = 0f;
			for (int i = 0; i < motors.Length; i++)
			{
				num += motors[i].burnTime;
			}
			return num;
		}
		return 0f;
	}

	public float GetMinRange(float launchSpeed)
	{
		if (motors.Length != 0 && motors[0] != null)
		{
			float dragCoef = GetDragCoef(MathF.PI / 360f);
			float num = 0f;
			for (int i = 0; i < motors.Length; i++)
			{
				num += motors[i].burnTime;
			}
			float num2 = CalcDeltaV();
			float b = Mathf.Sqrt(motors[^1].thrust / (dragCoef * airDensity * 0.5f * finArea));
			return Mathf.Min(launchSpeed + num2, b) * num;
		}
		return 0f;
	}

	public string GetSeekerType()
	{
		return seeker.GetSeekerType();
	}

	public GlobalPosition GetEvasionPoint()
	{
		return seeker.GetEvasionPoint();
	}

	public float GetRadarReturn(Vector3 source, Radar radar, Unit emitter, float dist, float clutter, RadarParams radarParameters, bool triggerWarning)
	{
		Vector3 direction = FastMath.NormalizedDirection(source, base.transform.position);
		this.onRadarPing?.Invoke(new Aircraft.OnRadarWarning
		{
			emitter = emitter,
			radar = radar
		});
		return radarParameters.GetSignalStrength(direction, dist, base.rb, RCS, clutter, 0f);
	}

	public float GetECMIntensity()
	{
		return 0f;
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID dealerID)
	{
		if (disabled)
		{
			return;
		}
		if (impactDamage > 0f)
		{
			Detonate(-base.rb.velocity, hitArmor: false, hitTerrain: false);
			NetworkSceneSingleton<MessageManager>.i.RpcBombFailMessage(persistentID, impactDamage / 9.81f);
		}
		if (dealerID == persistentID || dealerID == ownerID || dealerID.NotValid || disabled || (!(pierceDamage > armorProperties.pierceArmor) && !(blastDamage > armorProperties.blastArmor) && !(fireDamage > armorProperties.fireArmor)))
		{
			return;
		}
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / Mathf.Max(armorProperties.pierceTolerance, 0.1f);
		float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) * amountAffected / Mathf.Max(armorProperties.blastTolerance, 0.1f);
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / Mathf.Max(armorProperties.fireTolerance, 0.1f);
		hitpoints -= num + num2 + num3;
		if (!(hitpoints > 0f))
		{
			if (UnitRegistry.TryGetPersistentUnit(dealerID, out var persistentUnit) && persistentUnit.GetHQ() != base.NetworkHQ)
			{
				RecordDamage(dealerID, 1000f);
				ReportKilled();
			}
			Detonate(base.rb.velocity, hitArmor: false, hitTerrain: false);
		}
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
	}

	public void SetTarget(Unit target)
	{
		PersistentID obj = targetID;
		PersistentID persistentID = ((target != null) ? target.persistentID : PersistentID.None);
		if (!(obj == persistentID))
		{
			Network_targetID = persistentID;
		}
	}

	public void SetProxyFuse(Transform targetTransform, Rigidbody targetRB)
	{
		if (proxyFuse == null)
		{
			proxyFuse = new ProxyFuse(base.transform, base.rb);
		}
		proxyFuse.SetTarget(targetTransform, targetRB);
	}

	private void TargetIDChanged(PersistentID oldValue, PersistentID newValue)
	{
		if (!(base.NetworkHQ != null))
		{
			return;
		}
		if (oldValue.IsValid && base.NetworkHQ.trackingDatabase.TryGetValue(oldValue, out var value))
		{
			value.missileAttacks--;
		}
		if (newValue.IsValid && UnitRegistry.TryGetUnit(newValue, out target))
		{
			if (seeker != null && seeker.triggerMissileWarning && target is Aircraft aircraft)
			{
				aircraft.LockedByMissile(this);
			}
			if (base.NetworkHQ.trackingDatabase.TryGetValue(newValue, out var value2))
			{
				value2.missileAttacks++;
			}
		}
		else
		{
			target = null;
		}
	}

	public void UpdateRadarAlt()
	{
		radarAlt = (Physics.Linecast(base.transform.position, base.transform.position - Vector3.up * 10000f, out hit, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask) ? hit.distance : base.transform.position.GlobalY());
		radarAlt = Mathf.Min(radarAlt, base.transform.position.GlobalY());
	}

	public bool IsArmed()
	{
		return warhead.Armed;
	}

	public void Arm()
	{
		warhead.Arm();
	}

	public void SetAimpoint(GlobalPosition aimPoint, Vector3 targetVel)
	{
		this.aimPoint = aimPoint;
		this.targetVel = targetVel;
	}

	private void Steering()
	{
		Vector3 vector = FastMath.NormalizedDirection(this.GlobalPosition(), aimPoint);
		Vector3 vector2 = ((!reachedOnTarget) ? base.transform.forward : (base.transform.forward * 0.5f + base.rb.velocity.normalized * 0.5f));
		if (!reachedOnTarget && timeSinceSpawn > 2f && Vector3.Angle(vector, vector2) < 10f)
		{
			reachedOnTarget = true;
		}
		if (Vector3.Dot(vector, vector2) < 0.71f)
		{
			vector = Vector3.RotateTowards(vector2, vector, MathF.PI / 4f, 1f);
		}
		Vector3 vector3 = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(vector2, base.transform.up), Vector3.one).inverse.MultiplyVector(vector) * 30f;
		vector3.z = 0f;
		if (uprightPreference > 0f)
		{
			Vector3 other = ((Vector3.Dot(base.transform.forward, Vector3.up) > 0.25f) ? Vector3.up : (Vector3.up + 0.03f * Mathf.Clamp(vector3.x, -30f, 30f) * base.transform.right));
			other -= vector * 0.5f;
			vector3.z = TargetCalc.GetAngleOnAxis(base.transform.up, other, base.transform.forward) * uprightPreference;
		}
		localAngularVel = base.transform.InverseTransformVector(base.rb.angularVelocity);
		Vector2 output = pid.GetOutput(new Vector2(0f - vector3.y, vector3.x), Time.fixedDeltaTime);
		inputs = new Vector3(Mathf.Clamp(output.x, -1f, 1f), Mathf.Clamp(output.y, -1f, 1f), Mathf.Clamp(0f - localAngularVel.z * 5f + vector3.z * 0.3f, -1f, 1f));
	}

	private void ApplyAero()
	{
		Vector3 vector = base.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
		float sqrMagnitude = vector.sqrMagnitude;
		Vector3 normalized = Vector3.Cross(Vector3.Cross(base.transform.forward, vector), vector).normalized;
		float time = MathF.PI / 180f * Vector3.Angle(base.transform.forward, vector);
		float num = liftCurve.Evaluate(time);
		float num2 = dragCurve.Evaluate(time) * airDensity * sqrMagnitude * 0.5f * currentFinArea;
		float num3 = num * airDensity * sqrMagnitude * -0.5f * currentFinArea;
		Vector3 vector2 = -vector.normalized * num2;
		if (supersonicDrag > 0f)
		{
			float speedOfSound = LevelInfo.GetSpeedOfSound(base.transform.GlobalPosition().y);
			float num4 = 0.1f;
			if (speed > (1f + num4) * speedOfSound)
			{
				vector2 *= 1f + supersonicDrag;
			}
			else if (speed > (1f - num4) * speedOfSound)
			{
				float num5 = supersonicDrag + 0.15f;
				float num6 = Mathf.Min(Mathf.Abs((speedOfSound - speed) / speedOfSound), num4);
				float num7 = (num4 - num6) / num4;
				vector2 *= 1f + num7 * num7 * num7 * num5;
			}
		}
		Vector3 vector3 = normalized * num3;
		base.rb.AddForce(vector3 + vector2);
		Vector3 vector4 = inputs * torque;
		if (maxTurnRate > 0f || gLimit > 0f)
		{
			float num8 = Mathf.Min(maxTurnRate * (MathF.PI / 180f), 9.81f * gLimit / Mathf.Max(speed, 1f));
			Vector3 vector5 = localAngularVel + vector4 * Time.fixedDeltaTime;
			float num9 = Mathf.Max(Mathf.Abs(vector5.x) - num8, 0f);
			float num10 = Mathf.Max(Mathf.Abs(vector5.y) - num8, 0f);
			vector4 -= new Vector3(Mathf.Sign(vector4.x) * num9 / Time.fixedDeltaTime, Mathf.Sign(vector4.y) * num10 / Time.fixedDeltaTime, 0f);
		}
		base.rb.AddRelativeTorque(vector4, ForceMode.Acceleration);
	}

	public float InterceptPriority(Unit toUnit, float minRange, float interceptorValue)
	{
		float num = 1f;
		if (info.blastDamage < 500f && (target == null || target.disabled || target is Missile))
		{
			return 0f;
		}
		if (target != null)
		{
			float num2 = definition.value - target.definition.value * info.pK;
			float num3 = definition.value - interceptorValue;
			if (num2 > num3)
			{
				return 0f;
			}
			float num4 = (num3 - num2) / (interceptorValue * 10f);
			float num5 = FastMath.SquareDistance(target.GlobalPosition(), toUnit.GlobalPosition());
			num *= Mathf.Clamp01(num4 * minRange * minRange / num5);
		}
		return num;
	}

	public bool LosingGround()
	{
		Vector3 rhs = base.rb.velocity - targetVel;
		return Vector3.Dot(base.rb.velocity, rhs) < 0f;
	}

	public bool MissedTarget()
	{
		return Vector3.Dot(aimPoint - this.GlobalPosition(), base.rb.velocity) < 0f;
	}

	private void DetectCollisions()
	{
		if (base.transform.position.y < Datum.LocalSeaY)
		{
			if (impactFuse && IsArmed())
			{
				Detonate(Vector3.up, hitArmor: false, hitTerrain: false);
				base.rb.velocity = Vector3.zero;
			}
			else
			{
				if (motor != null)
				{
					motor.Burnout(forceStopEffects: true);
				}
				if (speed > 30f)
				{
					base.rb.angularDrag = 0.3f;
					Vector3 position = base.transform.position;
					position.y = Datum.LocalSeaY;
					if (UnityEngine.Random.value > 0.5f)
					{
						UnityEngine.Object.Destroy(UnityEngine.Object.Instantiate(GameAssets.i.rotorStrike_water, position, Quaternion.LookRotation(Vector3.up + new Vector3(base.rb.velocity.x, 0f, base.rb.velocity.z) * 0.1f)), 20f);
					}
				}
				base.rb.velocity -= Vector3.ClampMagnitude(0.2f * Time.fixedDeltaTime * base.rb.velocity.sqrMagnitude * base.rb.velocity.normalized, speed);
			}
		}
		int layerMask = (tangible ? (~PhysicsLayers.ExclusionZonesMask.value) : PhysicsLayers.StaticsMask.value);
		Vector3 vector = ((target != null && target.rb != null) ? target.rb.velocity : targetVel);
		Vector3 vector2 = ((target != null && target.maxRadius < 20f) ? target.transform.position : aimPoint.ToLocalPosition());
		RaycastHit hitInfo2;
		if (FastMath.InRange(base.transform.position, vector2, speed * 0.25f))
		{
			_ = base.transform.position - vector2;
			Vector3 vector3 = base.rb.velocity - vector;
			if (Physics.Linecast(base.transform.position, base.transform.position + 1.1f * Time.fixedDeltaTime * vector3, out var hitInfo, layerMask))
			{
				base.transform.position = hitInfo.point - vector3.normalized * 0.2f;
				base.rb.MovePosition(base.transform.position);
				if (impactFuse)
				{
					bool flag = hitInfo.collider.sharedMaterial == GameAssets.i.terrainMaterial;
					bool hitArmor = !flag;
					if (!hitInfo.collider.gameObject.TryGetComponent<IDamageable>(out var component) || !PenetrateObject(component, hitInfo.point, hitInfo.normal))
					{
						Detonate(hitInfo.normal, hitArmor, flag);
					}
				}
				if (!base.rb.isKinematic)
				{
					base.rb.velocity = Vector3.zero;
				}
			}
		}
		else if (Physics.Linecast(base.transform.position, base.transform.position + 1.1f * Time.fixedDeltaTime * base.rb.velocity, out hitInfo2, layerMask))
		{
			base.transform.position = hitInfo2.point - base.rb.velocity.normalized * 0.2f;
			base.rb.MovePosition(hitInfo2.point - base.rb.velocity.normalized * 0.2f);
			Rigidbody attachedRigidbody = hitInfo2.collider.attachedRigidbody;
			if (attachedRigidbody != null && !attachedRigidbody.isKinematic && FastMath.InRange(attachedRigidbody.velocity, base.rb.velocity, 100f))
			{
				return;
			}
			bool flag2 = hitInfo2.point.y < Datum.LocalSeaY;
			bool flag3 = !flag2 && hitInfo2.collider.sharedMaterial == GameAssets.i.terrainMaterial;
			if (impactFuse)
			{
				bool hitArmor2 = !flag2 && !flag3;
				if ((!hitInfo2.collider.gameObject.TryGetComponent<IDamageable>(out var component2) || !PenetrateObject(component2, hitInfo2.point, hitInfo2.normal)) && IsArmed())
				{
					Detonate(hitInfo2.normal, hitArmor2, flag3);
				}
				if (!base.rb.isKinematic)
				{
					base.rb.velocity = Vector3.zero;
				}
			}
			else if (speed > 10f)
			{
				base.rb.velocity = Vector3.Reflect(base.rb.velocity, hitInfo2.normal) * 0.25f;
				UnityEngine.Object.Destroy(flag3 ? UnityEngine.Object.Instantiate(GameAssets.i.rotorStrike_dirt, base.transform.position, Quaternion.LookRotation(Vector3.up + new Vector3(base.rb.velocity.x, 0f, base.rb.velocity.z) * 0.1f)) : UnityEngine.Object.Instantiate(GameAssets.i.rotorStrike_solid, base.transform.position, Quaternion.LookRotation(Vector3.up + new Vector3(base.rb.velocity.x, 0f, base.rb.velocity.z) * 0.1f)), 20f);
			}
		}
		if (proxyFuse != null && proxyFuse.ConditionsMet(vector, speed))
		{
			Detonate(base.rb.velocity, hitArmor: false, hitTerrain: false);
		}
	}

	private bool PenetrateObject(IDamageable damageable, Vector3 hitPoint, Vector3 hitNormal)
	{
		if (impactFuseDelay == 0f)
		{
			DamageEffects.ArmorPenetrate(hitPoint - base.transform.forward * 0.1f, base.transform.forward * 1000f, 1000f, pierceDamage, 0f, ownerID);
			return false;
		}
		ArmorProperties armorProperties = damageable.GetArmorProperties();
		if (pierceDamage < armorProperties.pierceArmor)
		{
			return false;
		}
		impactFuse = false;
		Vector3 position = hitPoint + definition.length * 2f * base.rb.velocity.normalized;
		base.rb.MovePosition(position);
		base.transform.position = position;
		GameObject obj = UnityEngine.Object.Instantiate(GameAssets.i.rotorStrike_dirt, Datum.origin);
		obj.transform.SetPositionAndRotation(hitPoint, Quaternion.LookRotation(-base.rb.velocity));
		UnityEngine.Object.Destroy(obj, 10f);
		ImpactDelayedFuse(hitNormal, damageable, hitArmor: true, hitTerrain: false).Forget();
		return true;
	}

	private async UniTask ImpactDelayedFuse(Vector3 normal, IDamageable penetratedObject, bool hitArmor, bool hitTerrain)
	{
		warhead.Armed = false;
		impactFuse = false;
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)(impactFuseDelay * 1000f));
		if (!cancel.IsCancellationRequested)
		{
			warhead.Armed = true;
			float blastArmorPrev = 0f;
			if (penetratedObject != null)
			{
				blastArmorPrev = penetratedObject.GetArmorProperties().blastArmor;
				penetratedObject.GetArmorProperties().blastArmor = 0f;
			}
			Detonate(normal, hitArmor, hitTerrain);
			await UniTask.Delay(200);
			if (penetratedObject != null)
			{
				penetratedObject.GetArmorProperties().blastArmor = blastArmorPrev;
			}
		}
	}

	public void Detonate(Vector3 normal, bool hitArmor, bool hitTerrain)
	{
		if (!disabled)
		{
			base.Networkdisabled = true;
			if (target != null && FastMath.InRange(base.transform.GlobalPosition(), target.GlobalPosition(), 200f))
			{
				RpcDetonate(target, useUnit: true, target.transform.InverseTransformPoint(base.transform.position), IsArmed(), hitArmor, hitTerrain, normal);
			}
			else
			{
				RpcDetonate(null, useUnit: false, base.transform.GlobalPosition().AsVector3(), IsArmed(), hitArmor, hitTerrain, normal);
			}
			SetTarget(null);
			DelayedDestroy(2f).Forget();
		}
	}

	private async UniTask DelayedDestroy(float delay)
	{
		await UniTask.Delay((int)(delay * 1000f));
		if (this != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		if (owner != null)
		{
			owner.DeregisterMissile(this);
		}
	}

	[ClientRpc]
	public void RpcDetonate(Unit relativeUnit, bool useUnit, Vector3 pos, bool armed, bool hitArmor, bool hitTerrain, Vector3 normal)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcDetonate_897349600(relativeUnit, useUnit, pos, armed, hitArmor, hitTerrain, normal);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Unit(writer, relativeUnit);
		writer.WriteBooleanExtension(useUnit);
		writer.WriteVector3(pos);
		writer.WriteBooleanExtension(armed);
		writer.WriteBooleanExtension(hitArmor);
		writer.WriteBooleanExtension(hitTerrain);
		writer.WriteVector3(normal);
		ClientRpcSender.Send(this, 22, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	private void Disappear()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		SetTangible(tangible: false);
	}

	private async UniTask ExplosionForceOnPhysicsFrame(Transform relativeTransform, Vector3 pos)
	{
		await UniTask.WaitForFixedUpdate();
		Vector3 blastPosition = ((relativeTransform != null) ? relativeTransform.TransformPoint(pos) : (pos + Datum.origin.position));
		DamageEffects.BlastFrag(blastYield, blastPosition, ownerID, persistentID);
	}

	private void MotorThrust()
	{
		if (boosterIsAttached)
		{
			return;
		}
		if (motorStage >= motors.Length)
		{
			motor = null;
			return;
		}
		motor = motors[motorStage];
		if (motor.fuelMass <= 0f)
		{
			engineCurrentThrust = 0f;
			motorStage++;
			visibility = definition.visibleRange;
		}
		else
		{
			ignition = true;
			engineCurrentThrust = motor.Thrust(this, base.LocalSim, inputs, throttle);
			visibility = definition.visibleRange + Mathf.Sqrt(engineCurrentThrust) * 40f;
		}
	}

	private void FixedUpdate()
	{
		if (!disabled)
		{
			MotorThrust();
		}
		speed = base.rb.velocity.magnitude;
		timeSinceSpawn += Time.fixedDeltaTime;
		if (flightSound != null)
		{
			float num = Mathf.Clamp01((-50f + speed) / maxPitchSpeed);
			if (flightSound.volume < num)
			{
				flightSound.volume += Time.deltaTime;
			}
			else
			{
				flightSound.volume = num;
			}
			flightSound.pitch = pitchRange * num + basePitch;
		}
		if (!disabled && base.LocalSim)
		{
			ServerFixedUpdate();
		}
	}

	private void ServerFixedUpdate()
	{
		airDensity = GameAssets.i.airDensityAltitude.Evaluate(base.rb.transform.position.GlobalY() * 0.001f);
		if (seeker != null)
		{
			seeker.Seek();
		}
		Steering();
		ApplyAero();
		DetectCollisions();
	}

	public void ApplyTerminalBoost(float value)
	{
		if (motor != null)
		{
			motor.TerminalBoost(value);
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
			GeneratedNetworkCode._Write_PersistentID(writer, ownerID);
			GeneratedNetworkCode._Write_PersistentID(writer, _targetID);
			writer.WriteVector3(startingVelocity);
			writer.WriteVector3(startOffsetFromOwner);
			GeneratedNetworkCode._Write_Missile_002FSeekerMode(writer, seekerMode);
			return true;
		}
		writer.Write((ulong)((long)syncVarDirtyBits >> 9), 5);
		if ((syncVarDirtyBits & 0x200L) != 0L)
		{
			GeneratedNetworkCode._Write_PersistentID(writer, ownerID);
			result = true;
		}
		if ((syncVarDirtyBits & 0x400L) != 0L)
		{
			GeneratedNetworkCode._Write_PersistentID(writer, _targetID);
			result = true;
		}
		if ((syncVarDirtyBits & 0x800L) != 0L)
		{
			writer.WriteVector3(startingVelocity);
			result = true;
		}
		if ((syncVarDirtyBits & 0x1000L) != 0L)
		{
			writer.WriteVector3(startOffsetFromOwner);
			result = true;
		}
		if ((syncVarDirtyBits & 0x2000L) != 0L)
		{
			GeneratedNetworkCode._Write_Missile_002FSeekerMode(writer, seekerMode);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			ownerID = GeneratedNetworkCode._Read_PersistentID(reader);
			PersistentID persistentID = _targetID;
			_targetID = GeneratedNetworkCode._Read_PersistentID(reader);
			startingVelocity = reader.ReadVector3();
			startOffsetFromOwner = reader.ReadVector3();
			seekerMode = GeneratedNetworkCode._Read_Missile_002FSeekerMode(reader);
			if (!base.IsServer && !SyncVarEqual(persistentID, _targetID))
			{
				TargetIDChanged(persistentID, _targetID);
			}
			return;
		}
		ulong num = reader.Read(5);
		SetDeserializeMask(num, 9);
		if ((num & 1L) != 0L)
		{
			ownerID = GeneratedNetworkCode._Read_PersistentID(reader);
		}
		if ((num & 2L) != 0L)
		{
			PersistentID persistentID2 = _targetID;
			_targetID = GeneratedNetworkCode._Read_PersistentID(reader);
			if (!base.IsServer && !SyncVarEqual(persistentID2, _targetID))
			{
				TargetIDChanged(persistentID2, _targetID);
			}
		}
		if ((num & 4L) != 0L)
		{
			startingVelocity = reader.ReadVector3();
		}
		if ((num & 8L) != 0L)
		{
			startOffsetFromOwner = reader.ReadVector3();
		}
		if ((num & 0x10L) != 0L)
		{
			seekerMode = GeneratedNetworkCode._Read_Missile_002FSeekerMode(reader);
		}
	}

	private void UserCode_RpcUnfoldFins_1465559174()
	{
		UnfoldFins().Forget();
	}

	protected static void Skeleton_RpcUnfoldFins_1465559174(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Missile)behaviour).UserCode_RpcUnfoldFins_1465559174();
	}

	public void UserCode_RpcDetonate_897349600(Unit relativeUnit, bool useUnit, Vector3 pos, bool armed, bool hitArmor, bool hitTerrain, Vector3 normal)
	{
		if (motor != null)
		{
			motor.Destruct(this);
		}
		if (effectsTransform != null)
		{
			effectsTransform.SetParent(Datum.origin);
			UnityEngine.Object.Destroy(effectsTransform.gameObject, 20f);
		}
		if (flightSound != null)
		{
			flightSound.Stop();
			flightSound.clip = nearbyDetonationClip;
			flightSound.pitch = 1f;
			flightSound.volume = 1f;
			flightSound.dopplerLevel = 1f;
			flightSound.loop = false;
			flightSound.Play();
		}
		if (armed)
		{
			Disappear();
			base.rb.isKinematic = true;
		}
		Vector3 zero = Vector3.zero;
		zero = ((!(relativeUnit != null)) ? (pos + Datum.origin.position) : relativeUnit.transform.TransformPoint(pos));
		base.transform.position = zero;
		base.rb.MovePosition(zero);
		warhead.Detonate(base.rb, ownerID, zero, normal, armed, blastYield, hitArmor, hitTerrain);
		if (blastYield <= 200f)
		{
			ExplosionForceOnPhysicsFrame((relativeUnit != null) ? relativeUnit.transform : null, pos).Forget();
		}
	}

	protected static void Skeleton_RpcDetonate_897349600(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Missile)behaviour).UserCode_RpcDetonate_897349600(GeneratedNetworkCode._Read_Unit(reader), reader.ReadBooleanExtension(), reader.ReadVector3(), reader.ReadBooleanExtension(), reader.ReadBooleanExtension(), reader.ReadBooleanExtension(), reader.ReadVector3());
	}

	protected override int GetRpcCount()
	{
		return 23;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(21, "Missile.RpcUnfoldFins", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcUnfoldFins_1465559174, RpcRateLimitConfig.Disabled());
		collection.Register(22, "Missile.RpcDetonate", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcDetonate_897349600, RpcRateLimitConfig.Disabled());
	}
}
