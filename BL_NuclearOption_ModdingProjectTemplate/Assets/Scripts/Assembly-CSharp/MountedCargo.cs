using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class MountedCargo : Weapon, IDamageable
{
	public enum RailDirection
	{
		Forward = 0,
		Backward = 1,
		Down = 2,
		Right = 3,
		Left = 4
	}

	[SerializeField]
	private RailDirection railDirection;

	[SerializeField]
	public UnitDefinition cargo;

	[SerializeField]
	private float railSpeed;

	[SerializeField]
	private float railDelay;

	[SerializeField]
	private AudioClip deploySound;

	[SerializeField]
	private float deployVolume;

	[SerializeField]
	private float pushDistance;

	[SerializeField]
	private float pushSpeed = 2f;

	[SerializeField]
	private ArmorProperties armorProperties;

	[SerializeField]
	private List<DamageEffect> damageEffects = new List<DamageEffect>();

	private float hitPoints = 100f;

	private UnitPart attachedPart;

	private BayDoor cargoDoor;

	private byte? id;

	private bool fired;

	private bool detached;

	private Vector3 railVector;

	private Vector3 mountedPosition;

	private void Awake()
	{
		base.enabled = false;
		ammo = 1;
		if (railDirection == RailDirection.Forward)
		{
			railVector = new Vector3(0f, 0f, 1f);
		}
		if (railDirection == RailDirection.Backward)
		{
			railVector = new Vector3(0f, 0f, -1f);
		}
		if (railDirection == RailDirection.Down)
		{
			railVector = new Vector3(0f, -1f, 0f);
		}
	}

	public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (!fired)
		{
			if (hardpoint != null)
			{
				hardpoint.SpringOpenBayDoors();
			}
			fired = true;
			attachedUnit = owner;
			ammo = 0;
			Aircraft aircraft = owner as Aircraft;
			if (aircraft.IsServer)
			{
				aircraft.RpcLaunchMissile(weaponStation.Number, target, aimpoint);
			}
			else if (aircraft.HasAuthority)
			{
				aircraft.CmdLaunchMissile(weaponStation.Number, target, aimpoint);
			}
			if (!hardpoint.part.IsDetached())
			{
				RailLaunch(owner, target).Forget();
			}
		}
	}

	public override void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount mount)
	{
		base.AttachToHardpoint(aircraft, hardpoint, mount);
		attachedPart = hardpoint.part;
		attachedPart.onPartDetached += MountedCargo_OnPartDetached;
		attachedPart.onApplyDamage += MountedCargo_OnPartDamage;
		attachedUnit = hardpoint.part.parentUnit;
		mountedPosition = base.transform.localPosition;
		cargoDoor = hardpoint.GetCargoDoor();
		float prefabMass = cargo.unitPrefab.GetComponent<Unit>().GetPrefabMass();
		hardpoint.ModifyMass(prefabMass);
		hardpoint.ModifyDrag(mount.GetDragPerRound());
		hardpoint.ModifyRCS(mount.GetRCSPerRound());
		if (damageEffects.Count > 0)
		{
			id = hardpoint.part.parentUnit.RegisterDamageable(this);
		}
	}

	public void OnDestroy()
	{
		if (!fired && !detached)
		{
			RemoveFromHardpoint();
		}
		if (id.HasValue)
		{
			attachedUnit.DeregisterDamageable(id.Value);
		}
	}

	private void RemoveFromHardpoint()
	{
		if (hardpoint != null)
		{
			float prefabMass = cargo.unitPrefab.GetComponent<Unit>().GetPrefabMass();
			hardpoint.ModifyMass(0f - prefabMass);
			hardpoint.ModifyDrag(0f - mount.GetDragPerRound());
			hardpoint.ModifyRCS(0f - mount.GetRCSPerRound());
		}
	}

	public override float GetMass()
	{
		if (fired)
		{
			return 0f;
		}
		return cargo.mass;
	}

	public override void Rearm(int ammoToRearm, WeaponStation weaponStation)
	{
		if (fired)
		{
			base.weaponStation = weaponStation;
			base.transform.localPosition = mountedPosition;
			base.gameObject.SetActive(value: true);
			fired = false;
			if (hardpoint != null)
			{
				hardpoint.ModifyMass(info.massPerRound);
				hardpoint.ModifyDrag(mount.GetDragPerRound());
				hardpoint.ModifyRCS(mount.GetRCSPerRound());
			}
			ReportReloading(reloading: false);
		}
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID dealerID)
	{
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / Mathf.Max(armorProperties.pierceTolerance, 0.01f);
		float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) * amountAffected / Mathf.Max(armorProperties.blastTolerance, 0.01f);
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / Mathf.Max(armorProperties.fireTolerance, 0.01f);
		float num4 = num + num2 + num3 + impactDamage;
		if (!(attachedUnit == null) && !(num4 <= 0f))
		{
			if (dealerID.IsValid && dealerID != attachedUnit.persistentID)
			{
				attachedUnit.RecordDamage(dealerID, num4);
			}
			if (id.HasValue)
			{
				attachedUnit.RpcDamage(id.Value, new DamageInfo(num, num2, num3, impactDamage));
			}
		}
	}

	public void ApplyDamage(float netPierceDamage, float netBlastDamage, float netFireDamage, float netImpactDamage)
	{
		hitPoints -= netPierceDamage + netBlastDamage + netFireDamage + netImpactDamage;
		if (detached || this == null)
		{
			return;
		}
		for (int num = damageEffects.Count - 1; num >= 0; num--)
		{
			DamageEffect damageEffect = damageEffects[num];
			if (hitPoints < damageEffect.threshold)
			{
				GameObject gameObject = Object.Instantiate(damageEffect.prefab, Datum.origin);
				gameObject.transform.position = base.transform.position;
				if (gameObject.TryGetComponent<DamageParticles>(out var _))
				{
					gameObject.transform.SetParent(base.transform);
				}
				damageEffects.RemoveAt(num);
				attachedPart.onPartDetached -= MountedCargo_OnPartDetached;
				MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = false;
				}
				Collider[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<Collider>();
				for (int i = 0; i < componentsInChildren2.Length; i++)
				{
					componentsInChildren2[i].enabled = false;
				}
				detached = true;
			}
		}
	}

	public void TakeShockwave(Vector3 origin, float overpressure, float blastPower)
	{
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	public Unit GetUnit()
	{
		return attachedUnit;
	}

	public override int GetAmmoLoaded()
	{
		if (!fired)
		{
			return 1;
		}
		return 0;
	}

	public override int GetAmmoTotal()
	{
		if (!fired)
		{
			return 1;
		}
		return 0;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	private void MountedCargo_OnPartDamage(UnitPart.OnApplyDamage e)
	{
		if (e.impactDamage > 0f)
		{
			ApplyDamage(0f, 0f, 0f, e.impactDamage);
		}
	}

	private void MountedCargo_OnPartDetached(UnitPart part)
	{
		if (this == null || !base.gameObject.activeSelf)
		{
			return;
		}
		part.onPartDetached -= MountedCargo_OnPartDetached;
		base.gameObject.SetActive(value: false);
		if (attachedUnit.IsServer)
		{
			Player player = null;
			if (attachedUnit is Aircraft aircraft)
			{
				player = aircraft.Player;
			}
			if (!detached)
			{
				RemoveFromHardpoint();
			}
			NetworkSceneSingleton<Spawner>.i.SpawnUnit(cargo, base.transform.position, base.transform.rotation, part.rb.GetPointVelocity(base.transform.position), attachedUnit, player).DisableUnit();
			detached = true;
		}
	}

	private async UniTask RailLaunch(Unit owner, Unit target)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		if (railDelay > 0f)
		{
			await UniTask.Delay((int)(railDelay * 1000f));
		}
		BayDoor bayDoor = cargoDoor;
		if (bayDoor is CargoRamp cargoRamp)
		{
			while (!cargoRamp.IsOpen())
			{
				if (hardpoint != null)
				{
					hardpoint.SpringOpenBayDoors();
				}
				await UniTask.Delay(100);
				if (cancel.IsCancellationRequested)
				{
					return;
				}
			}
		}
		if (deploySound != null)
		{
			PlayLaunchSound();
		}
		if (cargoDoor != null)
		{
			while (Vector3.Dot(base.transform.position - cargoDoor.transform.position, base.transform.forward) > 0f)
			{
				if (owner == null)
				{
					return;
				}
				if (hardpoint != null)
				{
					hardpoint.SpringOpenBayDoors();
				}
				base.transform.localPosition += railSpeed * Time.deltaTime * railVector;
				await UniTask.Yield();
				if (cancel.IsCancellationRequested)
				{
					return;
				}
			}
		}
		if (!detached)
		{
			await UniTask.WaitForFixedUpdate();
			base.gameObject.SetActive(value: false);
			RemoveFromHardpoint();
			Vector3 vector = base.transform.TransformVector(railVector) * railSpeed;
			if (!cancel.IsCancellationRequested && owner.IsServer)
			{
				Player player = attachedUnit.GetPlayer();
				Unit unit = NetworkSceneSingleton<Spawner>.i.SpawnUnit(cargo, base.transform.position, base.transform.rotation, owner.rb.GetPointVelocity(base.transform.position) + vector, owner, player);
				attachedPart.onApplyDamage -= MountedCargo_OnPartDamage;
				attachedPart.onPartDetached -= MountedCargo_OnPartDetached;
				Rigidbody component = unit.GetComponent<Rigidbody>();
				PushCargoOut(component).Forget();
			}
		}
	}

	private async UniTask PushCargoOut(Rigidbody cargoRB)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		BoxCollider cargoCollider = cargoRB.gameObject.GetComponent<BoxCollider>();
		cargoCollider.enabled = true;
		PhysicMaterial cargoColliderMaterial = cargoCollider.sharedMaterial;
		Unit cargoUnit = cargoRB.gameObject.GetComponent<Unit>();
		cargoCollider.sharedMaterial = GameAssets.i.frictionlessMaterial;
		if (cargoUnit != null)
		{
			cargoUnit.enabled = false;
			cargoUnit.displayDetail = 2f;
		}
		while (!attachedUnit.disabled && FastMath.InRange(cargoRB.position.ToGlobalPosition(), base.transform.GlobalPosition(), pushDistance))
		{
			if (cargoUnit is GroundVehicle groundVehicle)
			{
				groundVehicle.AnimateWheels(Vector3.Dot(cargoRB.transform.forward, cargoRB.velocity));
			}
			await UniTask.WaitForFixedUpdate();
			if (cancel.IsCancellationRequested)
			{
				ActivateCargoVehicle(cargoUnit, cargoCollider, cargoColliderMaterial);
				return;
			}
			float num = Vector3.Dot(cargoRB.velocity - attachedPart.rb.velocity, -base.transform.forward);
			Vector3 vector = -base.transform.forward * (cargoRB.mass * 10f * Mathf.Clamp(pushSpeed - num, 0f, 1.2f));
			cargoRB.AddForce(vector);
			attachedPart.rb.AddForce(-vector);
		}
		ActivateCargoVehicle(cargoUnit, cargoCollider, cargoColliderMaterial);
	}

	public void ActivateCargoVehicle(Unit cargoUnit, Collider cargoCollider, PhysicMaterial cargoColliderMaterial)
	{
		if (cargoUnit is GroundVehicle groundVehicle)
		{
			cargoCollider.enabled = false;
			cargoCollider.sharedMaterial = cargoColliderMaterial;
			groundVehicle.enabled = true;
			groundVehicle.SetHoldPosition(attachedUnit is Aircraft aircraft && aircraft.Player != null);
		}
		else if (cargoUnit is Container container)
		{
			cargoCollider.sharedMaterial = cargoColliderMaterial;
			container.enabled = true;
		}
	}

	private void PlayLaunchSound()
	{
		AudioSource audioSource = base.transform.parent.gameObject.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
		audioSource.bypassListenerEffects = true;
		audioSource.clip = deploySound;
		audioSource.volume = deployVolume;
		audioSource.pitch = Random.Range(0.9f, 1.1f);
		audioSource.spatialBlend = 1f;
		audioSource.dopplerLevel = 0f;
		audioSource.spread = 5f;
		audioSource.maxDistance = 100f;
		audioSource.minDistance = 15f;
		audioSource.Play();
		Object.Destroy(audioSource, 5f);
	}
}
