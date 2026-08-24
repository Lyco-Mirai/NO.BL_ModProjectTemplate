using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using UnityEngine;

public class Pilot : MonoBehaviour, IDamageable
{
	public enum PilotType
	{
		Plane = 0,
		Helo = 1,
		Tiltwing = 2,
		VTOL = 3
	}

	public enum ExitDirection
	{
		Left = 0,
		Right = 1
	}

	public class FlightInfo
	{
		public float spawnTime;

		public bool HasTakenOff;

		public bool EnemyContact;

		public bool DeliveredCargo;

		public float LastCargoDelivery;
	}

	public PilotType pilotType;

	public ExitDirection exitDirection;

	public PilotBaseState currentState;

	public PilotPlayerState playerState = new PilotPlayerState();

	public PilotParkedState parkedState = new PilotParkedState();

	public AIPilotTaxiState AITaxiState;

	public AIPilotTakeoffState AITakeoffState;

	public AIPilotCombatModes AICombatState;

	public AIPilotLandingState AILandingState;

	public AIHeloTakeoffState AIHeloTakeoffState;

	public AIHeloCombatState AIHeloCombatState;

	public AIHeloLandingState AIHeloLandingState;

	public AIHeloTransportState AIHeloTransportState;

	public bool playerControlled;

	public bool dead;

	public bool ejected;

	public Aircraft aircraft;

	public Player player;

	private Unit primaryTarget;

	public Collider pilotCollider;

	[SerializeField]
	private SkinnedMeshRenderer skinnedMeshRenderer;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private UnitPart unitPart;

	[SerializeField]
	private bool ejectionSeat = true;

	public Vector3 accel = Vector3.zero;

	public Vector3 velocityPrev = Vector3.zero;

	public float gForce;

	private float hitPoints = 100f;

	[SerializeField]
	private ArmorProperties armorProperties;

	public RelaxedStabilityController relaxedStabilityController;

	[SerializeField]
	private ControlsFilter autoTrimmer;

	public FlightInfo flightInfo = new FlightInfo();

	private byte pilotNumber;

	private byte index;

	private bool escapeCapsule;

	public event Action onFire;

	public event Action onEject;

	public event Action onSwitchWeapon;

	public event Action On10sCheck;

	public event Action On5sCheck;

	public event Action On2sCheck;

	public event Action On1sCheck;

	public event Action On100msCheck;

	public void Awake()
	{
		flightInfo.spawnTime = Time.timeSinceLevelLoad;
		index = aircraft.RegisterDamageable(this);
		aircraft.onInitialize += Pilot_OnInitialize;
		JobManager.Add(this);
		_ = aircraft.pilots[0] != this;
	}

	private void OnDestroy()
	{
		JobManager.Remove(this);
	}

	public bool HasEjectionSeat()
	{
		return ejectionSeat;
	}

	public Rigidbody GetRB()
	{
		return unitPart.rb;
	}

	public string GetCurrentState()
	{
		if (currentState == null)
		{
			return "null";
		}
		if (dead)
		{
			return "dead";
		}
		return currentState.GetCurrentState();
	}

	public Vector3 GetAccel()
	{
		return accel;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public void TakeShockwave(Vector3 origin, float blastEffectScale, float blastPower)
	{
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public void SwitchState(PilotBaseState state)
	{
		if (currentState != state)
		{
			currentState?.LeaveState();
			currentState = state;
			currentState?.EnterState(this);
		}
	}

	public void SwitchStateNew(PilotBaseState state)
	{
		currentState?.LeaveState();
		currentState = state;
		currentState?.EnterState(this);
	}

	public void AssignPilotNumber(byte index)
	{
		pilotNumber = index;
	}

	public void SetPrimaryTarget(Unit target)
	{
		primaryTarget = target;
	}

	public Unit GetPrimaryTarget()
	{
		return primaryTarget;
	}

	public void TogglePilotVisibility(bool enabled)
	{
		skinnedMeshRenderer.enabled = enabled;
	}

	private void SlowCheck_10s()
	{
		this.On10sCheck?.Invoke();
	}

	private void SlowCheck_5s()
	{
		this.On5sCheck?.Invoke();
	}

	private void SlowCheck_2s()
	{
		this.On2sCheck?.Invoke();
	}

	private void SlowCheck_1s()
	{
		this.On1sCheck?.Invoke();
	}

	private void Pilot_OnInitialize()
	{
		player = aircraft.Player;
		if (!(aircraft.pilots[0] != this) && GameManager.gameState != GameState.Editor && GameManager.gameState != GameState.Encyclopedia)
		{
			if (player == null && aircraft.IsServer)
			{
				SetStartingAiState();
			}
			if (GameManager.IsLocalPlayer(player))
			{
				SwitchState(playerState);
			}
		}
	}

	protected virtual void SetStartingAiState()
	{
		if (!aircraft.IsServer)
		{
			throw new MethodInvocationException("SetStartingAiState called when server is not active");
		}
		if (aircraft.NetworkHQ == null)
		{
			SwitchState(parkedState);
			return;
		}
		bool flag = aircraft.radarAlt > aircraft.definition.spawnOffset.y + 1f;
		this.StartSlowUpdateDelayed(10f, SlowCheck_10s);
		this.StartSlowUpdateDelayed(5f, SlowCheck_5s);
		this.StartSlowUpdateDelayed(2f, SlowCheck_2s);
		this.StartSlowUpdateDelayed(1f, SlowCheck_1s);
		if (pilotType == PilotType.Plane)
		{
			AITaxiState = new AIPilotTaxiState();
			AITakeoffState = new AIPilotTakeoffState();
			AICombatState = new AIPilotCombatModes(aircraft);
			AILandingState = new AIPilotLandingState();
			SwitchState(flag ? ((PilotBaseState)AICombatState) : ((PilotBaseState)AITaxiState));
		}
		else if (pilotType == PilotType.Helo || pilotType == PilotType.Tiltwing)
		{
			AIHeloTakeoffState = new AIHeloTakeoffState();
			AIHeloCombatState = new AIHeloCombatState(this);
			AIHeloLandingState = new AIHeloLandingState();
			SwitchState(flag ? ((PilotBaseState)AIHeloCombatState) : ((PilotBaseState)AIHeloTakeoffState));
		}
	}

	public void Fire()
	{
		if (!aircraft.cockpit.IsDetached())
		{
			aircraft.weaponManager.Fire();
		}
	}

	public void Remove()
	{
		base.gameObject.SetActive(value: false);
	}

	public UnitPart GetUnitPart()
	{
		return unitPart;
	}

	public float GetMass()
	{
		return 0f;
	}

	public void NextWeapon()
	{
		aircraft.weaponManager.NextWeaponStation();
	}

	public void PreviousWeapon()
	{
		aircraft.weaponManager.PreviousWeaponStation();
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID dealerID)
	{
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / (armorProperties.pierceTolerance + 1f);
		float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) * amountAffected / (armorProperties.blastTolerance + 1f);
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / (armorProperties.fireTolerance + 1f);
		if (hitPoints > 0f && num + num2 + num3 + impactDamage > 0f)
		{
			aircraft.Damage(index, new DamageInfo(num, num2, num3, impactDamage));
		}
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		if (ejected || dead)
		{
			return;
		}
		float num = pierceDamage + blastDamage + fireDamage + impactDamage;
		hitPoints -= num;
		if (GameManager.IsLocalAircraft(aircraft))
		{
			SceneSingleton<GameplayUI>.i.FlashHurt(num, hitPoints);
		}
		if (!(hitPoints < 0f))
		{
			return;
		}
		dead = true;
		animator.SetLayerWeight(1, 0f);
		animator.SetInteger("PilotState", 6);
		if (pilotNumber == 0 && GameManager.gameState != GameState.Encyclopedia)
		{
			if (GameManager.IsLocalAircraft(aircraft) && !MissionHelper.CanRespawn)
			{
				GameManager.FinishGame(GameResolution.Defeat);
			}
			if (GameManager.IsLocalPlayer(aircraft.Player))
			{
				SceneSingleton<CameraStateManager>.i.SetFollowingUnit(null);
				MusicManager.i.CrossFadeMusic(GameAssets.i.deathSound, 2f, 0f, repeat: false, allowReplay: true, replacePlaying: true);
			}
			if (aircraft.IsServer)
			{
				aircraft.DisableUnit();
				CommandEjection().Forget();
			}
			SwitchState(null);
		}
	}

	public Unit GetUnit()
	{
		return aircraft;
	}

	public void TakeGForceDamage(float sqrGForces)
	{
		float num = (sqrGForces - 400f) * 0.007f;
		if (aircraft != null)
		{
			aircraft.Damage(index, new DamageInfo(0f, 0f, 0f, num));
			if (GameManager.IsLocalAircraft(aircraft) && GameManager.IsLocalPlayer(player))
			{
				SceneSingleton<GameplayUI>.i.FlashHurt(num, hitPoints);
			}
		}
	}

	public void SetEjected()
	{
		ejected = true;
		JobManager.Remove(this);
	}

	public void TakeWaterDamage(float damage)
	{
		aircraft.Damage(index, new DamageInfo(0f, 0f, 0f, damage));
	}

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	private void Update()
	{
		if (currentState != null)
		{
			currentState.UpdateState(this);
		}
	}

	private async UniTask CommandEjection()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(1000);
		if (!cancel.IsCancellationRequested)
		{
			int num = 0;
			Pilot[] pilots = aircraft.pilots;
			foreach (Pilot pilot in pilots)
			{
				num += ((!pilot.dead) ? 1 : 0);
			}
			if (num > 0)
			{
				aircraft.StartEjectionSequence();
			}
		}
	}

	public PartResult Pilot_OnAeroInputsApplied()
	{
		if (aircraft.remoteSim)
		{
			return PartResult.None;
		}
		if (dead || ejected)
		{
			return PartResult.Remove;
		}
		if (base.transform.position.y < Datum.LocalSeaY - 10f)
		{
			TakeWaterDamage(1000f);
			return PartResult.Remove;
		}
		accel = ((velocityPrev == Vector3.zero) ? Vector3.zero : (unitPart.rb.velocity - velocityPrev));
		velocityPrev = unitPart.rb.velocity;
		accel /= Time.fixedDeltaTime * 9.81f;
		gForce = Vector3.Dot(accel, base.transform.up);
		float magnitude = accel.magnitude;
		if (magnitude > 20f)
		{
			TakeGForceDamage(magnitude * magnitude);
		}
		try
		{
			currentState?.FixedUpdateState(this);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		return PartResult.None;
	}
}
