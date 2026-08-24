using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FuelTank : MonoBehaviour
{
	[SerializeField]
	private float fuelCapacity;

	public float fuelMass;

	[SerializeField]
	private FuelTank[] connectedTanks;

	[SerializeField]
	private float leakThreshold;

	[SerializeField]
	private float leakPerHP;

	[SerializeField]
	private float maxLeakRate;

	[SerializeField]
	private float ruptureGMin = 20f;

	[SerializeField]
	private float ruptureGMax = 100f;

	[SerializeField]
	private float ignitionGMin = 20f;

	[SerializeField]
	private float ignitionGMax = 200f;

	[SerializeField]
	private float ignitionPierceMin;

	[SerializeField]
	private float ignitionPierceMax;

	[SerializeField]
	private float ignitionBlastMin;

	[SerializeField]
	private float ignitionBlastMax;

	[SerializeField]
	private float fireIntensity;

	[SerializeField]
	private GameObject leakEffect;

	[SerializeField]
	private GameObject fireEffect;

	[SerializeField]
	private GameObject fireball;

	private GameObject fireEffectSpawn;

	private GameObject fireballSpawn;

	private DamageParticles fireParticles;

	private ParticleSystem leakSystem;

	private float leakRate;

	private float lastCollision;

	private bool isLeaking;

	private Vector3 velocityPrev;

	[SerializeField]
	private UnitPart part;

	private Aircraft aircraft;

	private bool onFire;

	private bool ruptured;

	private static readonly Collider[] fireColliders = new Collider[100];

	public float FuelCapacity => fuelCapacity;

	private void Awake()
	{
		fuelMass = 0f;
		if (part == null)
		{
			part = base.gameObject.GetComponent<UnitPart>();
		}
		aircraft = part.parentUnit as Aircraft;
		aircraft.RegisterFuelTank(this);
		part.onApplyDamage += FuelTank_OnPartApplyDamage;
		part.onPartDetached += FuelTank_OnDetach;
		aircraft.onInitialize += FuelTank_OnInitialize;
	}

	private void FuelTank_OnInitialize()
	{
		aircraft.onInitialize -= FuelTank_OnInitialize;
		base.enabled = aircraft.LocalSim;
	}

	public float GetCapacity()
	{
		return fuelCapacity;
	}

	public float GetLevel()
	{
		return fuelMass;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.body == null)
		{
			lastCollision = Time.timeSinceLevelLoad;
		}
	}

	public bool Refuel(float ratio)
	{
		if (leakRate > 0f)
		{
			return false;
		}
		float num = fuelMass;
		fuelMass = fuelCapacity * ratio;
		float amount = fuelMass - num;
		part.ModifyMass(amount);
		return true;
	}

	private void FuelTank_OnDetach(UnitPart part)
	{
		part.onPartDetached -= FuelTank_OnDetach;
		if (!aircraft.remoteSim)
		{
			if (leakRate < maxLeakRate)
			{
				ruptured = true;
				PunctureTank(maxLeakRate);
				aircraft.FuelTankStatus(part.id, ruptured: true, onFire);
			}
			FuelTank[] array = connectedTanks;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].PunctureTank(maxLeakRate);
			}
		}
	}

	private void FuelTank_OnPartApplyDamage(UnitPart.OnApplyDamage e)
	{
		if (e.detached)
		{
			leakRate = maxLeakRate;
		}
		else if (e.hitPoints < leakThreshold)
		{
			PunctureTank((leakThreshold - e.hitPoints) * leakPerHP);
		}
		if (aircraft.LocalSim)
		{
			CheckDamageStatus(e);
		}
	}

	private void CheckDamageStatus(UnitPart.OnApplyDamage e)
	{
		bool flag = false;
		bool flag2 = false;
		if (leakRate < maxLeakRate && e.detached)
		{
			leakRate = maxLeakRate;
			flag = true;
		}
		float num = 1f / leakRate;
		float num2 = leakRate / maxLeakRate;
		if (leakRate > 0f && !onFire && (e.fireDamage > num || e.pierceDamage * num2 > Random.Range(ignitionPierceMin, ignitionPierceMax) || e.blastDamage * num2 > Random.Range(ignitionBlastMin, ignitionBlastMax)))
		{
			flag2 = true;
		}
		if (flag || flag2)
		{
			aircraft.FuelTankStatus(part.id, flag, flag2);
		}
	}

	public void UpdateStatus(bool ruptured, bool onFire)
	{
		if (!this.ruptured && ruptured)
		{
			this.ruptured = true;
			RuptureEffects();
		}
		if (!this.onFire && onFire)
		{
			if ((bool)leakEffect)
			{
				this.onFire = true;
			}
			if (aircraft.IsServer)
			{
				FuelTankFire().Forget();
			}
			IgnitionEffects();
		}
	}

	public void UseFuel(float rate)
	{
		float num = fuelMass;
		fuelMass -= rate;
		fuelMass = Mathf.Max(fuelMass, 0f);
		float amount = fuelMass - num;
		if (aircraft.LocalSim)
		{
			part.ModifyMass(amount);
		}
	}

	private void IgnitionEffects()
	{
		if (base.transform.position.y > Datum.LocalSeaY && fireEffectSpawn == null)
		{
			fireEffectSpawn = Object.Instantiate(fireEffect, base.transform);
			fireEffectSpawn.transform.localPosition = Vector3.zero;
			fireParticles = fireEffectSpawn.GetComponent<DamageParticles>();
			part.AddHostedParticles(fireParticles);
			if (leakRate == maxLeakRate)
			{
				Fireball();
			}
			if (leakSystem != null)
			{
				leakSystem.Stop();
				Object.Destroy(leakSystem.gameObject, 5f);
			}
		}
	}

	private void RuptureEffects()
	{
		leakRate = maxLeakRate;
		if (onFire)
		{
			Fireball();
		}
	}

	private void Fireball()
	{
		if (fireballSpawn == null && fireball != null && base.transform.position.y > Datum.LocalSeaY)
		{
			fireballSpawn = Object.Instantiate(fireball, base.transform);
			DamageParticles component = fireballSpawn.GetComponent<DamageParticles>();
			part.AddHostedParticles(component);
			fireballSpawn.transform.localPosition = Vector3.zero;
		}
	}

	public void PunctureTank(float leakRate)
	{
		if (!isLeaking)
		{
			isLeaking = true;
			GameObject gameObject = Object.Instantiate(leakEffect, base.transform);
			leakSystem = gameObject.GetComponent<ParticleSystem>();
		}
		this.leakRate = Mathf.Clamp(leakRate, this.leakRate, maxLeakRate);
	}

	private void FixedUpdate()
	{
		Vector3 velocity = part.rb.velocity;
		if (Time.timeSinceLevelLoad - lastCollision < 0.25f && !FastMath.InRange(velocityPrev, velocity, Mathf.Min(ruptureGMin, ignitionGMin) * 9.81f * Time.fixedDeltaTime) && part.xform.position.y > Datum.LocalSeaY && velocityPrev != Vector3.zero)
		{
			float num = (velocity - velocityPrev).magnitude / (9.81f * Time.fixedDeltaTime);
			bool flag = false;
			bool flag2 = false;
			if (num > Random.Range(ruptureGMin, ruptureGMax))
			{
				leakRate = maxLeakRate;
				flag = true;
			}
			if (num > Random.Range(ignitionGMin, ignitionGMax))
			{
				flag2 = true;
			}
			if (flag || flag2)
			{
				aircraft.FuelTankStatus(part.id, flag, flag2);
			}
		}
		if (leakRate > 0f)
		{
			LeakFuel();
		}
		velocityPrev = velocity;
	}

	private void LeakFuel()
	{
		float num = fuelMass;
		fuelMass -= leakRate * Time.deltaTime;
		if (fuelMass <= 0f)
		{
			fuelMass = 0f;
			leakRate = 0f;
			if (leakSystem != null)
			{
				leakSystem.Stop();
				Object.Destroy(leakSystem.gameObject, 5f);
				leakSystem = null;
			}
		}
		fuelMass = Mathf.Max(fuelMass, 0f);
		if (aircraft.LocalSim)
		{
			part.ModifyMass(fuelMass - num);
		}
	}

	private async UniTask FuelTankFire()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		while (!cancel.IsCancellationRequested)
		{
			float num = part.GetAverageRadius() * 1.5f;
			int num2 = Physics.OverlapSphereNonAlloc(base.transform.position, num, fireColliders);
			if (PlayerSettings.debugVis)
			{
				GameObject obj = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.blastRadiusDebug, base.transform);
				obj.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(0.5f, 0f, 0f, 1f));
				obj.transform.localScale = Vector3.one * num;
				Object.Destroy(obj, 1f);
			}
			if (base.transform.position.y < Datum.LocalSeaY && fireParticles != null)
			{
				fireParticles.ParentObjectCulled();
			}
			for (int i = 0; i < num2; i++)
			{
				if (fireColliders[i].TryGetComponent<IDamageable>(out var component))
				{
					component.TakeDamage(0f, 0f, 1f, fireIntensity, 0f, PersistentID.None);
				}
			}
			await UniTask.Delay(1000);
		}
	}
}
