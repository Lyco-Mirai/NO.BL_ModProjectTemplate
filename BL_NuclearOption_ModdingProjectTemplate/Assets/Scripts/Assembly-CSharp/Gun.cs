using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class Gun : Weapon
{
	[Serializable]
	private class Heat
	{
		[SerializeField]
		private AudioSource barrelSteamSound;

		[SerializeField]
		private ParticleSystem barrelSteamParticles;

		[SerializeField]
		private ParticleSystem breachSteamParticles;

		[SerializeField]
		private float maxSteamAlpha = 1f;

		[SerializeField]
		private float barrelSteamMinLife = 0.2f;

		[SerializeField]
		private float barrelSteamMaxLife = 2f;

		[SerializeField]
		private AudioSource barrelDamageSound;

		[SerializeField]
		private ParticleSystem barrelDamageParticles;

		private Gun gun;

		private float heat;

		private float overheatFactor;

		[SerializeField]
		private float heatPerShot;

		[SerializeField]
		private float coolingPerSecond;

		[SerializeField]
		private float coolingPer100kph;

		[SerializeField]
		private float maxHeat;

		private float baseFireRate;

		[SerializeField]
		private float firerateDegradation;

		private float baseSpread;

		[SerializeField]
		private float accuracyDegradation;

		[SerializeField]
		private float velocityDegradation;

		private ParticleSystem.MainModule particleMain;

		public void Initialize(Gun gun)
		{
			this.gun = gun;
			baseFireRate = gun.fireRate;
			baseSpread = gun.bulletSpread;
		}

		public void Update(float time)
		{
			if (!(heat > 0f))
			{
				return;
			}
			float num = (coolingPerSecond + coolingPer100kph * gun.attachedUnit.speed * 0.036f) * time;
			if (heat > num)
			{
				heat -= num;
				float max = Mathf.Min(baseFireRate / firerateDegradation * 1.01f, gun.info.muzzleVelocity / velocityDegradation);
				overheatFactor = Mathf.Clamp(heat / maxHeat - 1f, 0f, max);
				if (overheatFactor > 0f)
				{
					gun.fireRate = baseFireRate - firerateDegradation * overheatFactor;
					gun.fireInterval = 60f / gun.fireRate;
					if (gun.recoilSound != null)
					{
						gun.recoilSound.pitch = 1f - overheatFactor * firerateDegradation / baseFireRate;
					}
					gun.bulletSpread = baseSpread + accuracyDegradation * overheatFactor;
					gun.muzzleVelocity = gun.info.muzzleVelocity - velocityDegradation * overheatFactor;
					if (barrelSteamSound != null)
					{
						if (!barrelSteamSound.isPlaying)
						{
							barrelSteamSound.Play();
						}
						barrelSteamSound.volume = Mathf.Clamp01(overheatFactor);
					}
					if (barrelSteamParticles != null && breachSteamParticles != null)
					{
						Color color = new Color(1f, 1f, 1f, Mathf.Clamp01(overheatFactor) * maxSteamAlpha);
						if (!barrelSteamParticles.isPlaying)
						{
							barrelSteamParticles.Play();
						}
						if (!breachSteamParticles.isPlaying)
						{
							breachSteamParticles.Play();
						}
						particleMain = barrelSteamParticles.main;
						particleMain.startColor = color;
						particleMain.startLifetimeMultiplier = Mathf.Lerp(barrelSteamMaxLife, barrelSteamMinLife, Mathf.Clamp01(gun.attachedUnit.speed / 10f));
						particleMain = breachSteamParticles.main;
						particleMain.startColor = color;
					}
				}
				else
				{
					overheatFactor = 0f;
					gun.fireRate = baseFireRate;
					gun.fireInterval = 60f / baseFireRate;
					if (gun.recoilSound != null)
					{
						gun.recoilSound.pitch = 1f;
					}
					gun.bulletSpread = baseSpread;
					gun.muzzleVelocity = gun.info.muzzleVelocity;
					barrelSteamSound.Stop();
					barrelSteamParticles.Stop();
					breachSteamParticles.Stop();
				}
			}
			else
			{
				heat = 0f;
			}
		}

		public void GunFired()
		{
			if (overheatFactor > 1f)
			{
				heat += heatPerShot * (overheatFactor - 1f);
				if (barrelDamageParticles != null)
				{
					barrelDamageParticles.Play();
				}
				if (barrelDamageSound != null)
				{
					barrelDamageSound.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
					barrelDamageSound.Play();
				}
			}
			heat += heatPerShot;
		}

		public float GetNormalisedHeat()
		{
			return heat / maxHeat;
		}
	}

	[SerializeField]
	[ColorUsage(false, true)]
	private Color tracerColor = Color.red;

	[SerializeField]
	private MissileDefinition guidedProjectile;

	[SerializeField]
	private int tracerRatio = 6;

	[SerializeField]
	private float tracerSize = 1f;

	[SerializeField]
	private float bulletSelfDestruct = 5f;

	[SerializeField]
	private float bulletSpread = 0.1f;

	[SerializeField]
	private float fireRate;

	[SerializeField]
	private float reloadTime;

	[SerializeField]
	private int magazineCapacity;

	[SerializeField]
	private int magazines;

	[SerializeField]
	private bool proximityTimer;

	[SerializeField]
	private bool startLoaded = true;

	public bool ForceServerAuthority;

	private Unit proximityFuseTarget;

	private BulletSim bulletSim;

	private float queuedBullets;

	private float fireInterval;

	private int bulletsLoaded;

	private int tracerSeed;

	private int maxMagazines;

	private float muzzleVelocity;

	private float timeUntilReload;

	private int ticksSinceTriggerPull;

	public Rigidbody velocityInherit;

	[SerializeField]
	private Transform[] muzzles;

	[SerializeField]
	private AudioClip[] fireSounds;

	[SerializeField]
	private AudioClip fireStart;

	[SerializeField]
	private AudioClip fireSustained;

	[SerializeField]
	private AudioClip fireEnd;

	private AudioSource[] sources;

	[SerializeField]
	private float volume = 1f;

	[SerializeField]
	private float pitch = 1f;

	[SerializeField]
	private float pitchVariation;

	[SerializeField]
	private bool modifyPitch;

	[SerializeField]
	private float startPitchMultiplier = 1f;

	[SerializeField]
	private float pitchClimbRate = 1f;

	private float startPitch;

	[SerializeField]
	private AudioSource recoilSound;

	[SerializeField]
	private AudioSource reloadSound;

	[SerializeField]
	private Transform recoilTransform;

	[SerializeField]
	private Transform spinTransform;

	[SerializeField]
	private Transform ejectionTransform;

	[SerializeField]
	private GameObject ejectionPrefab;

	[SerializeField]
	private float ejectionDelay = 0.2f;

	[SerializeField]
	private Vector3 ejectionLinearVelocity;

	[SerializeField]
	private Vector3 ejectionRotationalVelocity;

	[SerializeField]
	private float ejectionLifetime;

	[SerializeField]
	private float recoilImpulse;

	[SerializeField]
	private float recoilTravel;

	[SerializeField]
	private float recoilRate;

	[SerializeField]
	private float recoilReturnRate;

	[SerializeField]
	private float spinRate;

	private float recoilEnergy;

	private float recoilPosition;

	private float currentSpinRate;

	[SerializeField]
	private ParticleSystem[] muzzleParticles;

	[SerializeField]
	private GameObject impactEffectGround;

	[SerializeField]
	private GameObject impactEffectArmor;

	[SerializeField]
	private GameObject impactEffectWater;

	[SerializeField]
	private GameObject selfDestructEffect;

	[SerializeField]
	private bool heatEnabled;

	[SerializeField]
	private Heat heat;

	public GameObject[] ImpactEffectsPrefabs { get; private set; }

	private void Awake()
	{
		if (startLoaded)
		{
			LoadFirstMag();
		}
		maxMagazines = magazines;
		tracerSeed = UnityEngine.Random.Range(0, 6);
		base.enabled = false;
		fireInterval = 60f / fireRate;
		muzzleVelocity = info.muzzleVelocity;
		float num = 1f;
		if (attachedUnit is GroundVehicle groundVehicle)
		{
			num = Mathf.Clamp(0.5f * (1f + groundVehicle.skill), 0.5f, 1.5f);
		}
		else if (attachedUnit is Ship ship)
		{
			num = Mathf.Clamp(0.5f * (1f + ship.skill), 0.5f, 1.5f);
		}
		reloadTime /= num;
		ImpactEffectsPrefabs = new GameObject[4];
		ImpactEffectsPrefabs[0] = impactEffectGround;
		ImpactEffectsPrefabs[1] = impactEffectArmor;
		ImpactEffectsPrefabs[2] = impactEffectWater;
		ImpactEffectsPrefabs[3] = selfDestructEffect;
		sources = base.gameObject.GetComponents<AudioSource>();
		AudioSource[] array = sources;
		foreach (AudioSource obj in array)
		{
			obj.volume = volume;
			obj.pitch = pitch;
		}
		if (sources.Length > 1)
		{
			sources[1].loop = true;
			sources[1].clip = fireSustained;
			if (modifyPitch)
			{
				startPitch = pitch * startPitchMultiplier;
				sources[1].pitch = startPitch;
			}
		}
		if (heatEnabled)
		{
			heat.Initialize(this);
		}
	}

	public override void AttachToUnit(Unit unit)
	{
		base.AttachToUnit(unit);
		velocityInherit = unit.rb;
	}

	public override void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount mount)
	{
		base.AttachToHardpoint(aircraft, hardpoint, mount);
		if (mount != null && mount.GunAmmo)
		{
			LoadAmmunition(mount);
		}
		hardpoint.ModifyMass((float)ammo * info.massPerRound);
		attachedUnit = hardpoint.part.parentUnit;
		AudioSource[] array = sources;
		foreach (AudioSource audioSource in array)
		{
			aircraft.RegisterDopplerSound(audioSource);
		}
	}

	public void LoadAmmunition(WeaponMount weaponMount)
	{
		if (weaponMount == null)
		{
			magazines = 0;
			if (hardpoint != null)
			{
				hardpoint.ModifyMass((float)(-ammo) * info.massPerRound);
			}
			ammo = 0;
		}
		else
		{
			info = weaponMount.info;
			magazines = maxMagazines;
			bulletsLoaded = magazineCapacity;
			ammo = bulletsLoaded + magazines * magazineCapacity;
		}
	}

	public void LoadFirstMag()
	{
		bulletsLoaded = magazineCapacity;
		ammo = bulletsLoaded + magazines * magazineCapacity;
	}

	public override void Rearm(int ammoToRearm, WeaponStation weaponStation)
	{
		base.weaponStation = weaponStation;
		int num = ammo;
		int num2 = ammo + ammoToRearm;
		if (maxMagazines > 0)
		{
			magazines = Mathf.FloorToInt(num2 / magazineCapacity);
		}
		else
		{
			bulletsLoaded = num2;
		}
		if (num == 0)
		{
			if (startLoaded)
			{
				LoadFirstMag();
			}
			ReportReloading(reloading: false);
		}
		ammo = num2;
		if (hardpoint != null)
		{
			hardpoint.ModifyMass(info.massPerRound * (float)(ammo - num));
		}
	}

	public override int GetAmmoLoaded()
	{
		return bulletsLoaded;
	}

	public override int GetAmmoTotal()
	{
		return ammo;
	}

	public override int GetFullAmmo()
	{
		return magazineCapacity * (1 + maxMagazines);
	}

	public override float GetReloadProgress()
	{
		if (!(reloadTime > 0f))
		{
			return 0f;
		}
		return timeUntilReload / reloadTime;
	}

	private void OnDestroy()
	{
		if (hardpoint != null)
		{
			hardpoint.ModifyMass((0f - info.massPerRound) * (float)ammo);
		}
		if (attachedUnit is Aircraft aircraft)
		{
			AudioSource[] array = sources;
			foreach (AudioSource audioSource in array)
			{
				aircraft.DeregisterDopplerSound(audioSource);
			}
		}
	}

	public override void SetTarget(Unit target)
	{
		currentTarget = target;
		if (proximityTimer || (bool)guidedProjectile)
		{
			proximityFuseTarget = target;
		}
	}

	private void ShotSound()
	{
		if (attachedUnit.displayDetail < 1f)
		{
			return;
		}
		if (sources.Length < 2)
		{
			sources[0].pitch = pitch + UnityEngine.Random.value * pitchVariation - UnityEngine.Random.value * pitchVariation;
			sources[0].PlayOneShot(fireSounds[UnityEngine.Random.Range(0, fireSounds.Length)]);
			if (recoilSound != null)
			{
				if (!heatEnabled)
				{
					recoilSound.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
				}
				recoilSound.Play();
			}
		}
		else if (!sources[1].isPlaying)
		{
			sources[0].PlayOneShot(fireStart);
		}
		if (sources.Length > 1 && !sources[1].isPlaying && Time.timeSinceLevelLoad - lastFired < fireInterval * 1.1f + Time.deltaTime)
		{
			sources[1].Play();
			sources[1].time = UnityEngine.Random.Range(0f, sources[1].clip.length);
		}
	}

	private void LoopSounds()
	{
		if (sources[1].isPlaying)
		{
			if (modifyPitch)
			{
				if (sources[1].pitch < pitch)
				{
					sources[1].pitch += Time.deltaTime * pitchClimbRate;
				}
				else
				{
					sources[1].pitch = pitch;
				}
			}
			if (Time.timeSinceLevelLoad - lastFired > Time.deltaTime + fireInterval)
			{
				sources[1].Stop();
				sources[0].PlayOneShot(fireEnd);
			}
		}
		else if (modifyPitch)
		{
			if (sources[1].pitch > startPitch)
			{
				sources[1].pitch -= Time.deltaTime;
			}
			else
			{
				sources[1].pitch = startPitch;
			}
		}
	}

	private void SpawnBullet(float timeOffset)
	{
		TrackFiringVisibility().Forget();
		ShotSound();
		if (recoilTransform != null)
		{
			recoilEnergy += 1f;
			if (attachedUnit.LocalSim && attachedUnit.rb != null)
			{
				attachedUnit.rb.AddForceAtPosition(-base.transform.forward * recoilImpulse, recoilTransform.position, ForceMode.Impulse);
			}
		}
		if (attachedUnit.LocalSim && fireInterval > 0.2f)
		{
			attachedUnit.SingleRemoteFire(weaponStation.Number, weaponStation.Ammo - 1);
		}
		ParticleSystem[] array = muzzleParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
		if (ejectionTransform != null)
		{
			SpawnEjection().Forget();
		}
		if (heatEnabled)
		{
			heat.GunFired();
		}
		Transform[] array2 = muzzles;
		foreach (Transform transform in array2)
		{
			Vector3 vector = ((velocityInherit != null) ? velocityInherit.velocity : Vector3.zero);
			tracerSeed++;
			bool tracer = false;
			if (tracerSeed > tracerRatio)
			{
				tracerSeed -= tracerRatio;
				tracer = true;
			}
			if (hardpoint != null)
			{
				hardpoint.ModifyMass(0f - info.massPerRound);
			}
			bool active = NetworkManagerNuclearOption.i.Server.Active;
			if (guidedProjectile != null && active)
			{
				NetworkSceneSingleton<Spawner>.i.SpawnMissile(guidedProjectile, transform.transform.position, transform.transform.rotation, vector + transform.transform.forward * muzzleVelocity, proximityFuseTarget, attachedUnit);
				break;
			}
			if (bulletSim == null)
			{
				bulletSim = BulletSim.Create(attachedUnit, this, weaponStation.GetTurret());
			}
			bulletSim.AddBullet(transform.transform, vector + transform.transform.forward * muzzleVelocity, bulletSpread, bulletSelfDestruct, tracer, tracerSize, tracerColor, timeOffset, proximityFuseTarget);
			if (active && attachedUnit != null && attachedUnit.NetworkHQ != null)
			{
				attachedUnit.NetworkHQ.missionStatsTracker.MunitionCost(attachedUnit, info.costPerRound);
			}
		}
	}

	private async UniTask SpawnEjection()
	{
		await UniTask.Delay((int)(ejectionDelay * 1000f));
		await UniTask.WaitForFixedUpdate();
		GameObject obj = UnityEngine.Object.Instantiate(ejectionPrefab, null);
		Rigidbody component = obj.GetComponent<Rigidbody>();
		component.solverIterations = 1;
		component.Move(ejectionTransform.position, ejectionTransform.rotation);
		component.transform.position = ejectionTransform.position;
		Vector3 velocity = ((velocityInherit != null) ? velocityInherit.velocity : Vector3.zero);
		component.velocity = velocity;
		component.AddRelativeForce(ejectionLinearVelocity, ForceMode.VelocityChange);
		component.AddRelativeTorque(ejectionRotationalVelocity, ForceMode.VelocityChange);
		UnityEngine.Object.Destroy(obj, ejectionLifetime);
	}

	public override void RemoteSingleFire(Unit firingUnit, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (timeUntilReload > 0f || Safety)
		{
			return;
		}
		if (hardpoint != null)
		{
			if (hardpoint.part.IsDetached())
			{
				return;
			}
			if (info.useWeaponDoors)
			{
				hardpoint.SpringOpenBayDoors();
			}
		}
		SpawnBullet(0f);
	}

	public override void Fire(Unit firingUnit, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
	{
		if (timeUntilReload > 0f || Safety || (weaponStation.Ammo <= 0 && attachedUnit.LocalSim))
		{
			return;
		}
		if (hardpoint != null)
		{
			if (hardpoint.part.IsDetached())
			{
				return;
			}
			if (info.useWeaponDoors)
			{
				hardpoint.SpringOpenBayDoors();
			}
		}
		ticksSinceTriggerPull = 0;
		base.weaponStation = weaponStation;
		base.enabled = true;
	}

	private void Update()
	{
		ticksSinceTriggerPull++;
	}

	private async UniTask SpinBarrels()
	{
		currentSpinRate = 1f;
		CancellationToken cancel = base.destroyCancellationToken;
		while (currentSpinRate > 0f && !cancel.IsCancellationRequested)
		{
			spinTransform.Rotate(currentSpinRate * spinRate * Time.deltaTime * Vector3.forward, Space.Self);
			currentSpinRate -= Time.deltaTime;
			await UniTask.Yield();
		}
	}

	private void FixedUpdate()
	{
		if (queuedBullets < 1f && Time.timeSinceLevelLoad - lastFired > fireInterval)
		{
			queuedBullets = 1f;
		}
		queuedBullets = ((ticksSinceTriggerPull < 2) ? (queuedBullets + Time.fixedDeltaTime * fireRate * 0.01667f) : 0f);
		queuedBullets = Mathf.Min(queuedBullets, bulletsLoaded);
		int num = 0;
		while (queuedBullets >= 1f)
		{
			queuedBullets -= 1f;
			SpawnBullet(queuedBullets * fireInterval);
			num++;
			ammo--;
			bulletsLoaded--;
			weaponStation.Updated();
		}
		if (num > 0)
		{
			lastFired = Time.timeSinceLevelLoad;
			weaponStation.UpdateLastFired(num);
			if (spinRate > 0f && FastMath.InRange(spinTransform.position, SceneSingleton<CameraStateManager>.i.transform.position, 100f))
			{
				if (currentSpinRate <= 0f)
				{
					SpinBarrels().Forget();
				}
				currentSpinRate = 1f;
			}
		}
		if (bulletsLoaded == 0 && timeUntilReload <= 0f && magazines > 0)
		{
			timeUntilReload = reloadTime;
			ReportReloading(reloading: true);
			magazines--;
			if (reloadSound != null)
			{
				reloadSound.Play();
			}
		}
		if (sources.Length > 1)
		{
			LoopSounds();
		}
		if (recoilTravel > 0f && recoilTransform != null)
		{
			recoilPosition += ((recoilEnergy > 0f) ? (recoilRate * Time.deltaTime) : ((0f - recoilReturnRate) * Time.deltaTime));
			if (recoilPosition >= 1f)
			{
				recoilEnergy = 0f;
			}
			recoilPosition = Mathf.Clamp01(recoilPosition);
			recoilTransform.localPosition = new Vector3(0f, 0f, (0f - recoilPosition) * recoilTravel);
		}
		if (heatEnabled)
		{
			heat.Update(Time.deltaTime);
		}
		if (timeUntilReload > 0f)
		{
			timeUntilReload -= Time.fixedDeltaTime;
			if (timeUntilReload <= 0f)
			{
				bulletsLoaded = magazineCapacity;
				ReportReloading(reloading: false);
				weaponStation.Updated();
			}
		}
		else if (Time.timeSinceLevelLoad - lastFired > 1f && (!heatEnabled || heat.GetNormalisedHeat() <= 0f))
		{
			base.enabled = false;
		}
	}

	public int GetCurrentAmmo()
	{
		return bulletsLoaded + magazines * magazineCapacity;
	}
}
