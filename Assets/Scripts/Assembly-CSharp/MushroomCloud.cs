using System;
using UnityEngine;
using UnityEngine.Rendering;

public class MushroomCloud : MonoBehaviour
{
	[Serializable]
	private class CloudRing
	{
		[SerializeField]
		private Renderer renderer;

		[SerializeField]
		private float altitudeMin;

		[SerializeField]
		private float altitudeMax;

		[SerializeField]
		private float maxRadius;

		[SerializeField]
		private float scrollSpeed;

		[SerializeField]
		private AnimationCurve emissionOverTime;

		[SerializeField]
		private Gradient colorOverRadius;

		private float heightOffset;

		private float blastScale;

		private int id_CloudColor = Shader.PropertyToID("_CloudColor");

		private int id_ScrollSpeed = Shader.PropertyToID("_ScrollSpeed");

		private int id_EmissiveStrength = Shader.PropertyToID("_EmissiveStrength");

		public void Initialize(float blastScale, float burstAltitude)
		{
			altitudeMin = Mathf.Max(altitudeMin, 0f - burstAltitude + 200f);
			renderer.transform.rotation = Quaternion.identity;
			renderer.transform.localScale = Vector3.one;
			renderer.material = new Material(renderer.material);
			renderer.material.SetColor(id_CloudColor, Color.clear);
			renderer.material.SetFloat(id_ScrollSpeed, scrollSpeed);
			heightOffset = UnityEngine.Random.Range(0f - altitudeMin, altitudeMax);
			renderer.transform.localPosition = Vector3.up * heightOffset;
			this.blastScale = blastScale;
		}

		public void Update(float time)
		{
			float num = blastScale * 50f + 340f * time;
			if (!(num < Mathf.Abs(heightOffset)))
			{
				float num2 = Mathf.Sqrt(num * num - heightOffset * heightOffset);
				renderer.transform.localScale = Vector3.one * num2;
				_ = num2 / maxRadius;
				float num3 = 1f - num / maxRadius;
				renderer.material.SetFloat(id_EmissiveStrength, emissionOverTime.Evaluate(time));
				renderer.material.SetColor(id_CloudColor, colorOverRadius.Evaluate(num3 * num2 / maxRadius));
			}
		}
	}

	[Serializable]
	private class Fireball
	{
		[SerializeField]
		private MeshRenderer renderer;

		[SerializeField]
		private AnimationCurve sizeOverTime;

		[SerializeField]
		private AnimationCurve emitOverTime;

		[SerializeField]
		private Gradient colorOverTime;

		private int id_FireballColor = Shader.PropertyToID("_FireballColor");

		private int id_EmissiveStrength = Shader.PropertyToID("_EmissiveStrength");

		public bool Update(float time)
		{
			if (!renderer.enabled)
			{
				return false;
			}
			renderer.transform.localScale = Vector3.one * sizeOverTime.Evaluate(time);
			renderer.material.SetColor(id_FireballColor, colorOverTime.Evaluate(time / emitOverTime.keys[^1].time));
			renderer.material.SetFloat(id_EmissiveStrength, emitOverTime.Evaluate(time));
			if (time > emitOverTime.keys[^1].time)
			{
				renderer.enabled = false;
				return false;
			}
			return true;
		}
	}

	[Serializable]
	private class Cloud
	{
		[SerializeField]
		private Optional<AnimationCurve> emitOverTime;

		[SerializeField]
		private Optional<AnimationCurve> emitRoughnessOverTime;

		[SerializeField]
		private AnimationCurve scrollOverTime;

		[SerializeField]
		private AnimationCurve horizontalSizeOverTime;

		[SerializeField]
		private AnimationCurve relativeHeightOverTime;

		[SerializeField]
		private AnimationCurve particleRateOverTime;

		[SerializeField]
		private AnimationCurve riseRateOverTime;

		[SerializeField]
		private AnimationCurve windInfluenceOverTime;

		[SerializeField]
		private Gradient colorOverTime;

		[SerializeField]
		private MeshRenderer meshRenderer;

		[SerializeField]
		private ParticleSystem cloudParticles;

		private float lifetime;

		private float groundBurstFactor;

		private ParticleSystem.MainModule main;

		private ParticleSystem.EmissionModule emission;

		private ParticleSystem.ShapeModule shape;

		private ParticleSystem.ForceOverLifetimeModule force;

		private Vector3 baseShape;

		private Vector3 appliedWind;

		private float lastSlowUpdate;

		private float riseRate;

		private bool underwater;

		private int id_EmissiveStrength = Shader.PropertyToID("_EmissiveStrength");

		private int id_EmissiveRoughness = Shader.PropertyToID("_EmissiveRoughness");

		private int id_CloudColor = Shader.PropertyToID("_CloudColor");

		private int id_ScrollPosition = Shader.PropertyToID("_ScrollPosition");

		public void Initialize(float groundBurstFactor, float blastScale)
		{
			meshRenderer.material = new Material(meshRenderer.material);
			meshRenderer.transform.position += Vector3.up * 25f * blastScale * (groundBurstFactor * groundBurstFactor);
			meshRenderer.transform.rotation = Quaternion.identity;
			main = cloudParticles.main;
			lifetime = main.duration;
			emission = cloudParticles.emission;
			shape = cloudParticles.shape;
			baseShape = new Vector3(shape.scale.x / meshRenderer.transform.localScale.x, shape.scale.y / meshRenderer.transform.localScale.y, shape.scale.z / meshRenderer.transform.localScale.z);
			riseRate = riseRateOverTime.Evaluate(0f);
			float num = horizontalSizeOverTime.Evaluate(0f);
			meshRenderer.transform.localScale = new Vector3(num, num * relativeHeightOverTime.Evaluate(0f), num);
		}

		public Vector3 GetPosition()
		{
			return meshRenderer.transform.position;
		}

		public void Update(float time)
		{
			if (!underwater)
			{
				if (emitOverTime.Enabled)
				{
					float num = emitOverTime.Value.Evaluate(time);
					emitOverTime.SetEnabled(num > 0f);
					meshRenderer.material.SetFloat(id_EmissiveStrength, num);
					meshRenderer.material.SetFloat(id_EmissiveRoughness, emitRoughnessOverTime.Value.Evaluate(time));
					meshRenderer.material.SetColor(id_CloudColor, colorOverTime.Evaluate(time / lifetime));
				}
				meshRenderer.material.SetFloat(id_ScrollPosition, scrollOverTime.Evaluate(time));
				meshRenderer.transform.position += (Vector3.up * riseRate + NetworkSceneSingleton<LevelInfo>.i.GetWind(meshRenderer.transform.position.ToGlobalPosition())) * Time.deltaTime;
				float num2 = horizontalSizeOverTime.Evaluate(time);
				float num3 = relativeHeightOverTime.Evaluate(time);
				meshRenderer.transform.localScale = new Vector3(num2, num2 * num3, num2);
				if (!(Time.timeSinceLevelLoad - lastSlowUpdate < 1f))
				{
					lastSlowUpdate = Time.timeSinceLevelLoad;
					Color color = colorOverTime.Evaluate(time / lifetime);
					meshRenderer.material.SetColor(id_CloudColor, color);
					riseRate = riseRateOverTime.Evaluate(time);
					appliedWind = NetworkSceneSingleton<LevelInfo>.i.GetWind(meshRenderer.transform.position.ToGlobalPosition()) * windInfluenceOverTime.Evaluate(time);
					main.startSize = new ParticleSystem.MinMaxCurve(num2 * 0.5f);
					emission.rateOverTime = new ParticleSystem.MinMaxCurve(particleRateOverTime.Evaluate(time));
					main.startColor = new ParticleSystem.MinMaxGradient(color * 0.8f, color * 1.2f);
				}
			}
		}
	}

	[Serializable]
	private class BaseCloud
	{
		[SerializeField]
		private ParticleSystem system;

		[SerializeField]
		private Optional<AnimationCurve> radiusOverTime;

		[SerializeField]
		private Optional<AnimationCurve> rateOverTime;

		[SerializeField]
		private Optional<AnimationCurve> startSizeOverTime;

		[SerializeField]
		private Gradient colorOverTime;

		private float lifetime;

		private float lastSlowUpdate;

		private float groundBurstFactor;

		private ParticleSystem.MainModule main;

		private ParticleSystem.EmissionModule emission;

		private ParticleSystem.ShapeModule shape;

		public bool Initialize(float groundBurstFactor, float blastScale, Vector3 groundZero, Vector3 groundNormal)
		{
			if (groundBurstFactor == 0f)
			{
				UnityEngine.Object.Destroy(system);
				return false;
			}
			this.groundBurstFactor = groundBurstFactor;
			Vector3 forward = ((groundNormal == Vector3.zero) ? Vector3.forward : Vector3.Cross(groundNormal, Vector3.up));
			system.transform.rotation = Quaternion.LookRotation(forward, groundNormal);
			system.transform.position = groundZero + system.transform.up * 10f * blastScale;
			main = system.main;
			lifetime = main.duration;
			emission = system.emission;
			shape = system.shape;
			return true;
		}

		public bool Update(float time)
		{
			if (radiusOverTime.Enabled)
			{
				float num = time / lifetime;
				shape.radius = radiusOverTime.Value.Evaluate(num);
				radiusOverTime.SetEnabled(num < radiusOverTime.Value.keys[^1].time);
			}
			if (Time.timeSinceLevelLoad - lastSlowUpdate < 1f)
			{
				return true;
			}
			lastSlowUpdate = Time.timeSinceLevelLoad;
			float time2 = time / (lifetime * Mathf.Clamp(groundBurstFactor, 0.5f, 1f));
			Color color = colorOverTime.Evaluate(time2);
			Color min = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, 1.2f * color.a * Mathf.Clamp(groundBurstFactor, 0.5f, 1f));
			Color max = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.8f * color.a * Mathf.Clamp(groundBurstFactor, 0.5f, 1f));
			main.startColor = new ParticleSystem.MinMaxGradient(min, max);
			if (startSizeOverTime.Enabled)
			{
				main.startSize = new ParticleSystem.MinMaxCurve(startSizeOverTime.Value.Evaluate(time2));
			}
			if (rateOverTime.Enabled)
			{
				emission.rateOverTime = new ParticleSystem.MinMaxCurve(groundBurstFactor * rateOverTime.Value.Evaluate(time2));
			}
			return color.a > 0f;
		}
	}

	[Serializable]
	private class Updraft
	{
		[SerializeField]
		private ParticleSystemForceField forceField;

		[SerializeField]
		private Optional<AnimationCurve> strengthOverTime;

		private float startingStrength;

		private float startingUpdraft;

		private float lastSlowUpdate;

		public bool Initialize(float groundBurstFactor, Vector3 groundZero)
		{
			if (groundBurstFactor == 0f)
			{
				UnityEngine.Object.Destroy(forceField);
				return false;
			}
			startingStrength = forceField.gravity.constant;
			startingUpdraft = forceField.directionY.constant;
			forceField.transform.SetPositionAndRotation(groundZero, Quaternion.identity);
			return true;
		}

		public bool Update(float time, Vector3 cloudPosition)
		{
			if (Time.timeSinceLevelLoad - lastSlowUpdate < 1f)
			{
				return true;
			}
			lastSlowUpdate = Time.timeSinceLevelLoad;
			float num = strengthOverTime.Value.Evaluate(time);
			forceField.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, cloudPosition - forceField.transform.position), cloudPosition - forceField.transform.position);
			forceField.gravity = new ParticleSystem.MinMaxCurve(startingStrength * num);
			forceField.directionY = new ParticleSystem.MinMaxCurve(startingUpdraft * num);
			return num > 0f;
		}
	}

	[Serializable]
	private class Stem
	{
		[SerializeField]
		private ParticleSystem system;

		[SerializeField]
		private Optional<AnimationCurve> rateOverTime;

		[SerializeField]
		private Optional<AnimationCurve> startSizeOverTime;

		[SerializeField]
		private Optional<AnimationCurve> lifeOverTime;

		[SerializeField]
		private Optional<AnimationCurve> noiseOverTime;

		[SerializeField]
		private Gradient startColorOverTime;

		private float lifetime;

		private float lastSlowUpdate;

		private float groundBurstFactor;

		private ParticleSystem.MainModule main;

		private ParticleSystem.EmissionModule emission;

		private ParticleSystem.NoiseModule noise;

		private ParticleSystem.ForceOverLifetimeModule force;

		private float lastEmit;

		private float emitInterval;

		private float blastScale;

		public bool Initialize(float groundBurstFactor, float blastScale, Transform baseCloud, Vector3 groundZero, Vector3 groundNormal)
		{
			if (groundBurstFactor == 0f)
			{
				UnityEngine.Object.Destroy(system);
				return false;
			}
			this.blastScale = blastScale;
			this.groundBurstFactor = groundBurstFactor;
			system.transform.rotation = Quaternion.identity;
			main = system.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = baseCloud;
			lifetime = main.duration;
			emission = system.emission;
			noise = system.noise;
			return true;
		}

		public bool Update(float time)
		{
			if (Time.timeSinceLevelLoad - lastEmit > emitInterval)
			{
				lastEmit = Time.timeSinceLevelLoad;
				ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
				Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
				insideUnitCircle = insideUnitCircle.normalized * insideUnitCircle.sqrMagnitude;
				Vector3 vector = new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y);
				emitParams.position = vector * blastScale * 300f + Vector3.up * blastScale * 10f;
				emitParams.velocity = -(vector * blastScale * 20f) + Vector3.up * 5f;
				system.Emit(emitParams, 1);
			}
			if (Time.timeSinceLevelLoad - lastSlowUpdate < 1f)
			{
				return true;
			}
			lastSlowUpdate = Time.timeSinceLevelLoad;
			float time2 = time / (lifetime * groundBurstFactor);
			Color color = startColorOverTime.Evaluate(time2);
			Color min = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, 1.2f * color.a * Mathf.Clamp(groundBurstFactor, 0.5f, 1f));
			Color max = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.8f * color.a * Mathf.Clamp(groundBurstFactor, 0.5f, 1f));
			main.startColor = new ParticleSystem.MinMaxGradient(min, max);
			if (rateOverTime.Enabled)
			{
				emitInterval = rateOverTime.Value.Evaluate(time2);
				emission.rateOverTime = new ParticleSystem.MinMaxCurve(groundBurstFactor * emitInterval);
				emitInterval = 0.5f / emitInterval;
			}
			if (startSizeOverTime.Enabled)
			{
				float num = startSizeOverTime.Value.Evaluate(time2);
				main.startSize = new ParticleSystem.MinMaxCurve(num * 0.8f, num * 1.2f);
			}
			if (lifeOverTime.Enabled)
			{
				float num2 = Mathf.Clamp(groundBurstFactor, 0.5f, 1f) * lifeOverTime.Value.Evaluate(time);
				main.startLifetime = new ParticleSystem.MinMaxCurve(num2 * 0.6f, num2 * 1.2f);
			}
			return color.a > 0f;
		}
	}

	[Serializable]
	private class WaterEffect
	{
		[SerializeField]
		private GameObject[] surfaceEffects;

		[SerializeField]
		private GameObject[] underwaterEffects;

		[SerializeField]
		private GameObject[] aboveWaterEffects;

		[SerializeField]
		private Renderer[] aboveWaterRenderers;

		public bool Initialize(float waterBurstFactor)
		{
			if (waterBurstFactor == 0f)
			{
				return false;
			}
			GameObject[] array = surfaceEffects;
			foreach (GameObject gameObject in array)
			{
				gameObject.SetActive(value: true);
				gameObject.transform.position = new Vector3(gameObject.transform.position.x, Datum.origin.position.y, gameObject.transform.position.z);
			}
			if (waterBurstFactor == 1f)
			{
				array = underwaterEffects;
				foreach (GameObject gameObject2 in array)
				{
					gameObject2.SetActive(value: true);
					gameObject2.transform.position = new Vector3(gameObject2.transform.position.x, Datum.origin.position.y, gameObject2.transform.position.z);
				}
				array = aboveWaterEffects;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(value: false);
				}
				Renderer[] array2 = aboveWaterRenderers;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].enabled = false;
				}
			}
			return true;
		}
	}

	public float yield;

	[SerializeField]
	private float effectReferenceYield;

	[SerializeField]
	private AnimationCurve lensFlareIntensity;

	[SerializeField]
	private LensFlareComponentSRP lensFlare;

	[SerializeField]
	private Optional<Fireball> fireball;

	[SerializeField]
	private Cloud cloud;

	[SerializeField]
	private Optional<BaseCloud> baseCloud;

	[SerializeField]
	private Optional<Stem> stem;

	[SerializeField]
	private Optional<Updraft> updraft;

	[SerializeField]
	private Optional<WaterEffect> waterEffect;

	[SerializeField]
	private CloudRing[] cloudRings;

	private Vector3 groundZero;

	private Vector3 groundNormal;

	private float time;

	private float blastScale;

	private float scaleAdjust;

	private void Start()
	{
		base.transform.rotation = Quaternion.identity;
		scaleAdjust = Mathf.Pow(yield / effectReferenceYield, 0.3333333f);
		blastScale = Mathf.Pow(yield / 1000000f, 0.3333333f);
		float num = 0f;
		float num2 = 0f;
		float waterBurstFactor = 0f;
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, blastScale * 300f, PhysicsLayers.StaticsMask) && hitInfo.point.y > Datum.origin.position.y)
		{
			num = hitInfo.distance;
			num2 = SampleGroundPosition(hitInfo.point);
		}
		if (num2 == 0f)
		{
			num = base.transform.position.y - Datum.origin.position.y;
			waterBurstFactor = Mathf.Clamp01((blastScale * 250f - num) / (blastScale * 250f));
		}
		cloud.Initialize(num2, blastScale);
		if (baseCloud.Enabled)
		{
			baseCloud.SetEnabled(baseCloud.Value.Initialize(num2, blastScale, groundZero, groundNormal));
		}
		if (updraft.Enabled)
		{
			updraft.SetEnabled(updraft.Value.Initialize(num2, groundZero));
		}
		if (stem.Enabled)
		{
			GameObject gameObject = new GameObject();
			gameObject.transform.position = groundZero;
			gameObject.transform.SetParent(Datum.origin);
			stem.SetEnabled(stem.Value.Initialize(num2, blastScale, gameObject.transform, groundZero, groundNormal));
		}
		if (waterEffect.Enabled)
		{
			waterEffect.Value.Initialize(waterBurstFactor);
		}
		CloudRing[] array = cloudRings;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize(blastScale, num);
		}
	}

	private float SampleGroundPosition(Vector3 groundZero)
	{
		Vector3[] array = new Vector3[3];
		for (int i = 0; i < 3; i++)
		{
			Vector3 vector = groundZero + new Vector3(blastScale * 200f * Mathf.Sin((float)(i * 120) * (MathF.PI / 180f)), 0f, blastScale * 200f * Mathf.Cos((float)(i * 120) * (MathF.PI / 180f)));
			if (Physics.Linecast(vector + Vector3.up * blastScale * 200f, vector - Vector3.up * blastScale * 200f, out var hitInfo, PhysicsLayers.StaticsMask))
			{
				array[i] = hitInfo.point;
				continue;
			}
			return 0f;
		}
		Plane plane = new Plane(array[0], array[1], array[2]);
		this.groundZero = plane.ClosestPointOnPlane(groundZero);
		groundNormal = plane.normal;
		return Mathf.Clamp01((blastScale * 250f - Vector3.Distance(groundZero, base.transform.position)) / (blastScale * 150f));
	}

	private void Update()
	{
		time += Time.deltaTime;
		if (fireball.Enabled)
		{
			fireball.SetEnabled(fireball.Value.Update(time));
		}
		cloud.Update(time);
		if (baseCloud.Enabled)
		{
			baseCloud.SetEnabled(baseCloud.Value.Update(time));
		}
		if (updraft.Enabled)
		{
			updraft.SetEnabled(updraft.Value.Update(time, cloud.GetPosition()));
		}
		if (stem.Enabled)
		{
			stem.SetEnabled(stem.Value.Update(time));
		}
		CloudRing[] array = cloudRings;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Update(time);
		}
		if (lensFlare != null)
		{
			lensFlare.intensity = lensFlareIntensity.Evaluate(time);
			if (lensFlare.intensity <= 0f)
			{
				UnityEngine.Object.Destroy(lensFlare);
			}
		}
	}
}
