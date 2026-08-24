using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class DamageParticles : MonoBehaviour
{
	[Serializable]
	private class SystemBehaviour
	{
		[SerializeField]
		private ParticleSystem system;

		[SerializeField]
		[Tooltip("Seconds of particle main duration spent fading out")]
		private float fadeDuration;

		[SerializeField]
		private float increaseRateWithSpeed;

		[SerializeField]
		private float reduceOpacityWithSpeed;

		[SerializeField]
		private float reduceLifeWithSpeed;

		[SerializeField]
		private bool globalSim = true;

		private float fadeOpacity;

		private float baseRate;

		private float duration;

		private ParticleSystem.MainModule main;

		private ParticleSystem.EmissionModule emission;

		private float lowerLifetime;

		private float upperLifetime;

		private Color upperColor;

		private Color lowerColor;

		private bool active;

		private bool updating;

		public void Initialize(float speed)
		{
			active = true;
			main = system.main;
			fadeOpacity = 1f;
			duration = main.duration - fadeDuration;
			if (main.startColor.mode == ParticleSystemGradientMode.TwoColors)
			{
				upperColor = main.startColor.colorMax;
				lowerColor = main.startColor.colorMin;
			}
			else
			{
				upperColor = (lowerColor = main.startColor.color);
			}
			lowerLifetime = main.startLifetime.constantMin;
			upperLifetime = main.startLifetime.constantMax;
			emission = system.emission;
			baseRate = emission.rateOverTime.constant;
			if (globalSim)
			{
				main.simulationSpace = ParticleSystemSimulationSpace.Custom;
				main.customSimulationSpace = Datum.origin;
			}
			updating = fadeDuration > 0f || increaseRateWithSpeed > 0f || reduceOpacityWithSpeed > 0f || reduceLifeWithSpeed > 0f;
			Update(0f, speed);
		}

		public void Stop()
		{
			system.Stop();
		}

		public void Update(float time, float speed)
		{
			if (!updating)
			{
				return;
			}
			if (time > duration)
			{
				fadeOpacity -= 1f / Mathf.Max(fadeDuration, 0.5f);
				if (fadeOpacity <= 0f)
				{
					updating = false;
				}
			}
			emission.rateOverTime = baseRate + speed * increaseRateWithSpeed;
			float num = 1f / Mathf.Max(speed * reduceLifeWithSpeed, 1f);
			main.startLifetime = new ParticleSystem.MinMaxCurve(lowerLifetime * num, upperLifetime * num);
			float num2 = 1f / (1f + reduceOpacityWithSpeed * speed * 0.01f);
			main.startColor = new ParticleSystem.MinMaxGradient(lowerColor * new Color(1f, 1f, 1f, fadeOpacity * num2), upperColor * new Color(1f, 1f, 1f, fadeOpacity * num2));
		}

		public bool IsActive()
		{
			if (!active)
			{
				return false;
			}
			active = system.particleCount > 0;
			return active;
		}
	}

	[SerializeField]
	private float fireDamage;

	[SerializeField]
	private float fireRange;

	[SerializeField]
	private float fireLifetime;

	[SerializeField]
	private SystemBehaviour[] systemBehaviours;

	[SerializeField]
	private Light fireLight;

	[SerializeField]
	private bool orientUpward;

	[SerializeField]
	private bool stopWhenParentCulled;

	[SerializeField]
	private bool snapToWater;

	private static readonly Collider[] fireColliders = new Collider[100];

	private UnitPart unitPart;

	private float time;

	private float speed;

	private float lightBaseIntensity;

	private float fireAnimationSeed;

	private float windSpeed;

	private bool detached;

	private Rigidbody rb;

	private void Awake()
	{
		base.enabled = snapToWater || fireLight != null;
		unitPart = GetComponentInParent<UnitPart>();
		if (orientUpward)
		{
			base.transform.rotation = Quaternion.identity;
		}
		if (fireLight != null)
		{
			lightBaseIntensity = fireLight.intensity;
			fireAnimationSeed = UnityEngine.Random.Range(0, 5);
		}
		if (unitPart != null && unitPart.rb != null && !unitPart.rb.isKinematic)
		{
			speed = (unitPart.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.windVelocity).magnitude;
		}
		else
		{
			rb = base.transform.GetComponentInParent<Rigidbody>();
			if (rb != null && !rb.isKinematic)
			{
				speed = (rb.velocity - NetworkSceneSingleton<LevelInfo>.i.windVelocity).magnitude;
			}
			else
			{
				speed = (windSpeed = NetworkSceneSingleton<LevelInfo>.i.windVelocity.magnitude);
				detached = true;
			}
			DetachTimer().Forget();
		}
		SystemBehaviour[] array = systemBehaviours;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(speed);
		}
		this.StartSlowUpdateDelayed(1f, SlowUpdate);
	}

	public void ParentObjectCulled()
	{
		base.transform.SetParent(Datum.origin, worldPositionStays: true);
		unitPart = null;
		base.enabled = false;
		detached = true;
		if (fireLight != null)
		{
			UnityEngine.Object.Destroy(fireLight.gameObject);
		}
		RaycastHit hitInfo;
		if (stopWhenParentCulled)
		{
			SystemBehaviour[] array = systemBehaviours;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
		else if (Physics.Linecast(base.transform.position + Vector3.up * 10f, base.transform.position - Vector3.up * 30f, out hitInfo, PhysicsLayers.StaticsMask))
		{
			base.transform.position = hitInfo.point;
			SystemBehaviour[] array = systemBehaviours;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Update(time, 0f);
			}
			speed = windSpeed;
		}
		else
		{
			SystemBehaviour[] array = systemBehaviours;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
	}

	private async UniTask DetachTimer()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(1500);
		if (!cancel.IsCancellationRequested)
		{
			ParentObjectCulled();
		}
	}

	private void SlowUpdate()
	{
		SystemBehaviour[] array;
		if ((!snapToWater && base.transform.position.y < Datum.LocalSeaY) || (snapToWater && base.transform.parent.position.y < Datum.LocalSeaY - 10f))
		{
			array = systemBehaviours;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
			ParentObjectCulled();
			UnityEngine.Object.Destroy(base.gameObject, 60f);
			if (fireLight != null)
			{
				fireLifetime = 0f;
				fireDamage = 0f;
				base.enabled = false;
				UnityEngine.Object.Destroy(fireLight.gameObject);
			}
			return;
		}
		time += 1f;
		if (rb != null)
		{
			speed = (rb.velocity - NetworkSceneSingleton<LevelInfo>.i.windVelocity).magnitude;
		}
		else if (!detached)
		{
			if (!(unitPart != null) || !(unitPart.rb != null))
			{
				Debug.LogWarning("DamageParticles.SlowUpdate: Rigidbody is missing on " + base.gameObject.name);
				return;
			}
			speed = (unitPart.rb.velocity - NetworkSceneSingleton<LevelInfo>.i.windVelocity).magnitude;
		}
		bool flag = false;
		array = systemBehaviours;
		foreach (SystemBehaviour systemBehaviour in array)
		{
			flag = flag || systemBehaviour.IsActive();
			systemBehaviour.Update(time, speed);
		}
		if (!flag)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		if (!(fireLifetime > 0f))
		{
			return;
		}
		if (time > fireLifetime)
		{
			fireLifetime = 0f;
			fireDamage = 0f;
			base.enabled = false;
			if (fireLight != null)
			{
				UnityEngine.Object.Destroy(fireLight.gameObject);
			}
		}
		if (!(fireDamage > 0f) || !NetworkManagerNuclearOption.i.Server.Active)
		{
			return;
		}
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, fireRange, fireColliders);
		for (int j = 0; j < num; j++)
		{
			if (fireColliders[j].TryGetComponent<IDamageable>(out var component))
			{
				component.TakeDamage(0f, 0f, 1f, fireDamage, 0f, PersistentID.None);
			}
		}
	}

	private void Update()
	{
		if (fireLight != null)
		{
			fireLight.intensity = lightBaseIntensity * Mathf.PerlinNoise1D(fireAnimationSeed + Time.timeSinceLevelLoad * 3f);
		}
		if (snapToWater)
		{
			base.transform.position = new Vector3(base.transform.position.x, Datum.LocalSeaY, base.transform.position.z);
		}
		if (orientUpward)
		{
			base.transform.rotation = Quaternion.identity;
		}
	}
}
