using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Jobs;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using Unity.Profiling;
using UnityEngine;

public class Ship : Unit, ICommandable
{
	[Serializable]
	private class WakeParticles
	{
		[SerializeField]
		private ParticleSystem system;

		private ParticleSystem.EmissionModule emit;

		private ParticleSystem.MainModule main;

		[SerializeField]
		private float minRate;

		[SerializeField]
		private float maxRate;

		[SerializeField]
		private float minOpacity;

		[SerializeField]
		private float maxOpacity;

		[SerializeField]
		private float minLife;

		[SerializeField]
		private float maxLife;

		[SerializeField]
		private float minSize;

		[SerializeField]
		private float maxSize;

		[SerializeField]
		private float minSpeed;

		[SerializeField]
		private float maxSpeed;

		private Unit parentUnit;

		public void Initialize(Unit parentUnit)
		{
			this.parentUnit = parentUnit;
			emit = system.emission;
			main = system.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = Datum.origin;
			main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Custom;
		}

		public void Update(float speed, Vector3 velocity)
		{
			if (!(system == null))
			{
				float num = Mathf.Clamp01((speed - minSpeed) / (maxSpeed - minSpeed));
				if (num <= 0f && system.isPlaying)
				{
					system.Stop();
				}
				if (num > 0f && !system.isPlaying)
				{
					system.Play();
				}
				emit.rateOverTime = Mathf.Lerp(minRate, maxRate, num);
				main.startRotation = parentUnit.transform.eulerAngles.y * (MathF.PI / 180f);
				main.startColor = new Color(1f, 1f, 1f, Mathf.Lerp(minOpacity, maxOpacity, num));
				main.startSize = Mathf.Lerp(minSize, maxSize, num);
				main.startLifetime = Mathf.Lerp(minLife, maxLife, num);
				velocity.y = 0f;
				main.emitterVelocity = velocity;
			}
		}

		public void Stop()
		{
			if (!(system == null))
			{
				system.Stop();
			}
		}
	}

	private static readonly ProfilerMarker applyJobResultsMarker = new ProfilerMarker("Ship.ApplyJobResults");

	public List<ShipPart> parts = new List<ShipPart>();

	public List<ShipPart> criticalParts = new List<ShipPart>();

	public float criticalRatio = 0.5f;

	[SerializeField]
	private WakeParticles[] wakeParticles;

	[SerializeField]
	private AudioSource[] waterSounds;

	[SerializeField]
	private AudioSource[] hullSounds;

	[SerializeField]
	private float disabledDespawnDelay;

	[SerializeField]
	private UnitCommand unitCommand;

	public float damageControlAvailable;

	public float damageControlDeploymentThreshold = 0.2f;

	public float AllowedSteerRate = 1f;

	[SerializeField]
	private AudioSource collisionSource;

	[SerializeField]
	private AudioClip collisionClip;

	[NonSerialized]
	public float skill = 1f;

	[NonSerialized]
	public bool holdPosition;

	private ShipInputs inputs = new ShipInputs();

	private List<VehicleWaypoint> savedWaypoints = new List<VehicleWaypoint>();

	private Airbase attachedAirbase;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 9;

	[NonSerialized]
	private const int RPC_COUNT = 21;

	public UnitCommand UnitCommand => unitCommand;

	bool ICommandable.Disabled => disabled;

	FactionHQ ICommandable.HQ => base.NetworkHQ;

	public event Action OnLaunch;

	public override Airbase GetAirbase()
	{
		return attachedAirbase;
	}

	public override void Awake()
	{
		base.Awake();
		attachedAirbase = GetComponent<Airbase>();
		base.Identity.OnStartClient.AddListener(OnStartClient);
	}

	public override void OnEnable()
	{
		base.OnEnable();
		WakeParticles[] array = wakeParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(this);
		}
		this.StartSlowUpdateDelayed(1f, UpdateParticles);
	}

	public void ReduceSteeringRate()
	{
		AllowedSteerRate = 0.1f;
	}

	public void RestoreSteeringRate()
	{
		AllowedSteerRate = 1f;
	}

	private void OnStartClient()
	{
		SetLocalSim(NetworkManagerNuclearOption.i.Server.Active);
		Vector3 position = startPosition.ToLocalPosition();
		base.transform.position = position;
		base.transform.rotation = startRotation;
		base.rb.MovePosition(position);
		base.rb.MoveRotation(startRotation);
		RegisterUnit(4f);
		InitializeUnit();
		base.rb.ResetCenterOfMass();
		base.rb.ResetInertiaTensor();
		if (!base.LocalSim)
		{
			return;
		}
		JobManager.Add(this);
		this.StartSlowUpdateDelayed(2f, CheckShipBuoyancy);
		foreach (ShipPart criticalPart in criticalParts)
		{
			criticalPart.onApplyDamage += Ship_OnCriticalPartDamage;
		}
	}

	public void Launch()
	{
		this.OnLaunch?.Invoke();
	}

	public ShipInputs GetInputs()
	{
		return inputs;
	}

	public TargetDetector GetRadar()
	{
		return radar;
	}

	public override Transform GetRandomPart()
	{
		int index = Mathf.FloorToInt(UnityEngine.Random.Range(0f, (float)partLookup.Count - 0.0001f));
		if (partLookup[index] != null)
		{
			return partLookup[index].transform;
		}
		return base.transform;
	}

	private void Animate()
	{
		AudioSource[] array = waterSounds;
		foreach (AudioSource audioSource in array)
		{
			float sqrMagnitude = base.rb.GetPointVelocity(audioSource.transform.position).sqrMagnitude;
			audioSource.volume = Mathf.Clamp01(sqrMagnitude * 0.005f);
		}
		array = hullSounds;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].volume = 0.25f + Mathf.Clamp01(speed * 0.05f);
		}
		if (collisionSource != null && collisionSource.isPlaying)
		{
			collisionSource.volume = Mathf.Lerp(collisionSource.volume, 0f, 0.05f);
			if (collisionSource.volume < 0.01f)
			{
				collisionSource.Stop();
			}
		}
	}

	private void UpdateParticles()
	{
		WakeParticles[] array = wakeParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Update(speed, base.rb.velocity);
		}
		if (base.transform.position.y < Datum.LocalSeaY - definition.spawnOffset.y)
		{
			array = wakeParticles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		MissionManager.onObjectiveStarted -= Ship_OnObjectiveStarted;
		foreach (ShipPart part in parts)
		{
			part.Flood();
		}
		inputs.throttle = 0f;
		inputs.steering = 0f;
		base.UnitDisabled(oldState, newState);
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			WaitRemove().Forget();
		}
	}

	private async UniTask WaitRemove()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay((int)disabledDespawnDelay * 1000);
		if (!cancel.IsCancellationRequested)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		JobManager.Remove(this);
		MissionManager.onObjectiveStarted -= Ship_OnObjectiveStarted;
		foreach (ShipPart part in parts)
		{
			if (part != null)
			{
				UnityEngine.Object.Destroy(part.gameObject);
			}
		}
		if (!base.IsServer)
		{
			return;
		}
		foreach (ShipPart criticalPart in criticalParts)
		{
			criticalPart.onApplyDamage -= Ship_OnCriticalPartDamage;
		}
	}

	public void GetMissionWaypoints(SavedShip savedShip)
	{
		savedWaypoints = new List<VehicleWaypoint>(savedShip.waypoints);
		MissionManager.onObjectiveStarted += Ship_OnObjectiveStarted;
		if (savedWaypoints[0].objective == "Unit Spawn")
		{
			unitCommand.SetDestination(savedWaypoints[0].position, playerCommand: false);
			savedWaypoints.RemoveAt(0);
		}
	}

	private void Ship_OnObjectiveStarted(Objective objective)
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

	public void SetHoldPosition(bool enabled)
	{
		holdPosition = enabled;
	}

	private void Update()
	{
		if (GameManager.ShowEffects)
		{
			Animate();
		}
		speed = base.rb.velocity.magnitude;
	}

	public void ApplyJobResults()
	{
		using (applyJobResultsMarker.Auto())
		{
			ApplyPartsForce();
		}
	}

	private void ApplyPartsForce()
	{
		Vector3 force = default(Vector3);
		Vector3 torque = default(Vector3);
		Vector3 vector = base.transform.TransformPoint(base.rb.centerOfMass);
		foreach (ShipPart part in parts)
		{
			if (!part.IsDetached() && part.JobFields.IsCreated)
			{
				ref ShipPartFields reference = ref part.JobFields.Ref();
				Vector3 vector2 = reference.forcePosition - vector;
				Vector3 vector3 = Vector3.Cross(reference.force, -vector2);
				force += reference.force;
				torque += vector3;
			}
		}
		base.rb.AddForce(force);
		base.rb.AddTorque(torque);
	}

	private void CheckShipBuoyancy()
	{
		if (!disabled && GameManager.gameState != GameState.Encyclopedia && (base.transform.position.y < Datum.LocalSeaY - definition.spawnOffset.y || Vector3.Dot(base.transform.up, Vector3.up) < 0.5f || Vector3.Dot(base.transform.forward, Vector3.up) > 0.25f))
		{
			base.Networkdisabled = true;
			ReportKilled();
		}
	}

	private void Ship_OnCriticalPartDamage(UnitPart.OnApplyDamage damageArgs)
	{
		if (disabled || damageArgs.hitPoints > 0f)
		{
			return;
		}
		int num = 0;
		foreach (ShipPart criticalPart in criticalParts)
		{
			if (criticalPart.IsCriticallyDamaged())
			{
				num++;
			}
		}
		if ((float)num >= criticalRatio * (float)criticalParts.Count)
		{
			base.Networkdisabled = true;
			ReportKilled();
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collisionSource != null && (collision.rigidbody == null || collision.rigidbody.isKinematic || collision.rigidbody.mass >= GetMass()) && !collisionSource.isPlaying)
		{
			collisionSource.transform.position = collision.GetContact(0).point;
			collisionSource.time = collisionSource.clip.length * UnityEngine.Random.value;
			collisionSource.volume = 1f;
			collisionSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
			collisionSource.Play();
			collisionSource.PlayOneShot(collisionClip, Mathf.Clamp01(0.25f + collision.relativeVelocity.sqrMagnitude * 0.01f));
		}
	}

	private void OnCollisionStay()
	{
		if (collisionSource != null && collisionSource.isPlaying && speed > 2f)
		{
			collisionSource.volume = Mathf.Lerp(collisionSource.volume, 1f, 0.1f);
		}
	}

	private void MirageProcessed()
	{
	}

	protected override int GetRpcCount()
	{
		return 21;
	}
}
