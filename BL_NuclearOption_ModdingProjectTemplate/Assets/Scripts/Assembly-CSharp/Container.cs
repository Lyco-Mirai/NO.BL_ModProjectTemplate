using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.Serialization;
using NuclearOption.Networking;
using UnityEngine;

public class Container : Unit
{
	[Serializable]
	private class FlotationDevice
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private Vector3 stowedScale;

		[SerializeField]
		private Vector3 inflatedScale;

		[SerializeField]
		private float inflationSpeed;

		private float inflatedAmount;

		public void Inflate()
		{
			inflatedAmount += inflationSpeed * Time.deltaTime;
			transform.localScale = Vector3.Lerp(stowedScale, inflatedScale, inflatedAmount);
		}

		public void Remove()
		{
			transform.gameObject.SetActive(value: false);
		}
	}

	[SyncVar]
	public PersistentID ownerID;

	[SyncVar(hook = "KinematicChanged")]
	private bool kinematic;

	[SerializeField]
	private float stability;

	[SerializeField]
	private float stabilityDamping;

	[SerializeField]
	private float waterDragCoef;

	[SerializeField]
	private float airDragCoef;

	private float volume;

	private float height;

	private float distanceSubmerged;

	private float timeStationary;

	[SerializeField]
	private CollisionTriggerZone collisionTriggerZone;

	[SerializeField]
	private bool buoyant;

	[SerializeField]
	private FlotationDevice[] flotationDevices;

	[SerializeField]
	private GameObject parachuteSystem;

	private BoxCollider mainCollider;

	private float lastRadarAltCheck;

	private bool slung;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 11;

	[NonSerialized]
	private const int RPC_COUNT = 21;

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

	public bool Networkkinematic
	{
		get
		{
			return kinematic;
		}
		set
		{
			if (!SyncVarEqual(value, kinematic))
			{
				bool oldValue = kinematic;
				kinematic = value;
				SetDirtyBit(1024uL);
				if (!GetSyncVarHookGuard(1024uL) && base.IsHost)
				{
					SetSyncVarHookGuard(1024uL, value: true);
					KinematicChanged(oldValue, value);
					SetSyncVarHookGuard(1024uL, value: false);
				}
			}
		}
	}

	public override void Awake()
	{
		int ignoreCollisions = PhysicsLayers.IgnoreCollisions;
		base.transform.gameObject.layer = ignoreCollisions;
		base.Awake();
		base.Identity.OnStartClient.AddListener(OnStartClient);
		if (collisionTriggerZone != null)
		{
			collisionTriggerZone.OnTriggerEntered += Container_OnTriggerZoneEntered;
		}
	}

	private void OnStartClient()
	{
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			SetLocalSim(localSim: true);
		}
		mainCollider = base.gameObject.GetComponent<BoxCollider>();
		volume = mainCollider.size.x * mainCollider.size.y * mainCollider.size.z;
		height = mainCollider.size.y;
		Aircraft localAircraft;
		bool flag = GameManager.GetLocalAircraft(out localAircraft) && localAircraft.persistentID == ownerID;
		if (base.remoteSim && flag)
		{
			base.transform.gameObject.layer = PhysicsLayers.IgnoreCollisions;
			ClientCollisionDelay().Forget();
		}
		else
		{
			base.transform.gameObject.layer = PhysicsLayers.Default;
		}
		base.transform.SetPositionAndRotation(startPosition.ToLocalPosition(), startRotation);
		RegisterUnit(4f);
		InitializeUnit();
	}

	private async UniTask ClientCollisionDelay()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(5000);
		if (!cancel.IsCancellationRequested)
		{
			base.transform.gameObject.layer = PhysicsLayers.Default;
		}
	}

	private void Container_OnTriggerZoneEntered()
	{
		if (NetworkManagerNuclearOption.i.Server.Active && kinematic)
		{
			Networkkinematic = false;
		}
	}

	public override void AttachOrDetachSlingHook(Aircraft aircraft, bool attached)
	{
		base.AttachOrDetachSlingHook(aircraft, attached);
		if (base.IsServer)
		{
			NetworkownerID = aircraft.persistentID;
			Networkkinematic = false;
			if (!attached && base.gameObject.GetComponent<ImpactDetector>() == null)
			{
				base.gameObject.AddComponent<ImpactDetector>().SetGLimit(100f);
			}
		}
		slung = attached;
	}

	private void KinematicChanged(bool oldValue, bool newValue)
	{
		base.rb.isKinematic = newValue;
		base.rb.interpolation = ((!base.rb.isKinematic) ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
		base.enabled = !newValue;
	}

	public override void InitializeUnit()
	{
		base.InitializeUnit();
		base.rb.isKinematic = kinematic;
		if (parachuteSystem != null && !Physics.Linecast(startPosition.ToLocalPosition(), startPosition.ToLocalPosition() - Vector3.up * 10f, PhysicsLayers.StaticsMask))
		{
			UnityEngine.Object.Instantiate(parachuteSystem, base.transform).GetComponent<CargoDeploymentSystem>().Initialize(this);
		}
	}

	public override void SetLocalSim(bool localSim)
	{
		base.SetLocalSim(localSim);
		base.rb.useGravity = localSim;
	}

	public override void UnitDisabled(bool oldState, bool newState)
	{
		base.UnitDisabled(oldState, newState);
		GetComponent<Renderer>().enabled = false;
		mainCollider.enabled = false;
		FlotationDevice[] array = flotationDevices;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Remove();
		}
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			UnityEngine.Object.Destroy(base.gameObject, 2f);
		}
	}

	public override bool IsSlung()
	{
		return slung;
	}

	private void FixedUpdate()
	{
		speed = base.rb.velocity.magnitude;
		if (!NetworkManagerNuclearOption.i.Server.Active)
		{
			return;
		}
		timeStationary = ((speed > 1f) ? 0f : (timeStationary + Time.fixedDeltaTime));
		CheckRadarAlt();
		if (timeStationary > 5f)
		{
			timeStationary = 0f;
			if (slung)
			{
				return;
			}
			if (!Physics.Linecast(base.transform.position, base.transform.position - Vector3.up * 5f, out var hitInfo, (int)PhysicsLayers.StaticsMask | (int)PhysicsLayers.ShipsMask) || hitInfo.collider.attachedRigidbody == null)
			{
				base.NetworkstartPosition = base.transform.GlobalPosition();
				Networkkinematic = true;
				return;
			}
		}
		float num = Datum.LocalSeaY - (base.transform.position.y - height * 0.5f);
		if (num <= 0f)
		{
			Vector3 wind = NetworkSceneSingleton<LevelInfo>.i.GetWind(base.transform.GlobalPosition());
			Vector3 vector = base.rb.velocity - wind;
			float sqrMagnitude = vector.sqrMagnitude;
			Vector3 force = -vector.normalized * sqrMagnitude * airDragCoef * (height * height);
			base.rb.AddForce(force);
			return;
		}
		FlotationDevice[] array = flotationDevices;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Inflate();
		}
		if (distanceSubmerged <= 0f && SceneSingleton<ParticleEffectManager>.i != null)
		{
			SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(GameAssets.i.splash_large).Play(new Vector3(base.transform.position.x, Datum.LocalSeaY, base.transform.position.z), Quaternion.LookRotation(Vector3.up + new Vector3(base.rb.velocity.x, 0f, base.rb.velocity.z) * 0.1f));
		}
		distanceSubmerged = num;
		float num2 = Mathf.Clamp01(distanceSubmerged / height);
		float num3 = num2 * volume;
		float num4 = 9810f * num3;
		Vector3 vector2 = -base.rb.velocity.normalized * speed * speed * num2 * waterDragCoef * (height * height);
		Vector3 vector3 = Vector3.Cross(base.transform.up, Vector3.up) * base.rb.mass * stability;
		Vector3 vector4 = -base.rb.angularVelocity * base.rb.mass * stabilityDamping;
		base.rb.AddForce(Vector3.up * num4 + vector2);
		base.rb.AddTorque((vector3 + vector4) * num2);
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
			writer.WriteBooleanExtension(kinematic);
			return true;
		}
		writer.Write((ulong)((long)syncVarDirtyBits >> 9), 2);
		if ((syncVarDirtyBits & 0x200L) != 0L)
		{
			GeneratedNetworkCode._Write_PersistentID(writer, ownerID);
			result = true;
		}
		if ((syncVarDirtyBits & 0x400L) != 0L)
		{
			writer.WriteBooleanExtension(kinematic);
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
			bool flag = kinematic;
			kinematic = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(flag, kinematic))
			{
				KinematicChanged(flag, kinematic);
			}
			return;
		}
		ulong num = reader.Read(2);
		SetDeserializeMask(num, 9);
		if ((num & 1L) != 0L)
		{
			ownerID = GeneratedNetworkCode._Read_PersistentID(reader);
		}
		if ((num & 2L) != 0L)
		{
			bool flag2 = kinematic;
			kinematic = reader.ReadBooleanExtension();
			if (!base.IsServer && !SyncVarEqual(flag2, kinematic))
			{
				KinematicChanged(flag2, kinematic);
			}
		}
	}

	protected override int GetRpcCount()
	{
		return 21;
	}
}
