using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectManager : SceneSingleton<ParticleEffectManager>
{
	[Serializable]
	private class GlobalParticleSystem
	{
		public string name;

		public ParticleSystem system;

		public Color mainColor;

		[NonSerialized]
		public ParticleSystem.EmissionModule emission;

		[NonSerialized]
		public ParticleSystem.MainModule main;

		public void Initialize()
		{
			emission = system.emission;
			main = system.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = Datum.origin;
			mainColor = main.startColor.color;
		}
	}

	public class PrefabEffect
	{
		private GameObject gameObject;

		private ParticleSystem[] systems;

		private AudioSource source;

		private ParticleSystem.Burst[][] bursts;

		public PrefabEffect(GameObject prefab)
		{
			if (!(prefab == null))
			{
				gameObject = UnityEngine.Object.Instantiate(prefab, Datum.origin);
				systems = gameObject.GetComponentsInChildren<ParticleSystem>();
				bursts = new ParticleSystem.Burst[systems.Length][];
				for (int i = 0; i < systems.Length; i++)
				{
					ParticleSystem obj = systems[i];
					ParticleSystem.MainModule main = obj.main;
					main.simulationSpace = ParticleSystemSimulationSpace.Custom;
					main.customSimulationSpace = Datum.origin;
					ParticleSystem.EmissionModule emission = obj.emission;
					bursts[i] = new ParticleSystem.Burst[emission.burstCount];
					emission.GetBursts(bursts[i]);
				}
				source = gameObject.GetComponent<AudioSource>();
			}
		}

		public void Play(Vector3 position, Quaternion rotation)
		{
			if (gameObject == null)
			{
				return;
			}
			gameObject.transform.position = position;
			gameObject.transform.rotation = rotation;
			gameObject.SetActive(value: true);
			if (source != null && !source.isPlaying)
			{
				source.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				source.Play();
			}
			for (int i = 0; i < systems.Length; i++)
			{
				ParticleSystem particleSystem = systems[i];
				ParticleSystem.Burst[] array = bursts[i];
				foreach (ParticleSystem.Burst burst in array)
				{
					particleSystem.Emit((int)burst.count.constantMax);
				}
			}
		}
	}

	private Dictionary<string, GlobalParticleSystem> systemLookup = new Dictionary<string, GlobalParticleSystem>();

	[SerializeField]
	private GlobalParticleSystem[] globalParticleSystems;

	private ParticleSystem.EmitParams emitParams;

	private Dictionary<GameObject, PrefabEffect[]> globalPrefabEffects = new Dictionary<GameObject, PrefabEffect[]>();

	private Dictionary<GameObject, int> globalPrefabIndices = new Dictionary<GameObject, int>();

	protected override void Awake()
	{
		base.Awake();
		GlobalParticleSystem[] array = globalParticleSystems;
		foreach (GlobalParticleSystem globalParticleSystem in array)
		{
			globalParticleSystem.Initialize();
			systemLookup.Add(globalParticleSystem.name, globalParticleSystem);
		}
	}

	public int GetSystemID(string name)
	{
		for (int i = 0; i < globalParticleSystems.Length; i++)
		{
			if (globalParticleSystems[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}

	public void EmitParticles(int systemID, int number, GlobalPosition origin, Vector3 startVelocity, float positionVariation, float lifetime, float lifetimeVariation, float startSize, float sizeVariation, float velocityVariation, float opacity, float opacityVariation)
	{
		if (systemID >= 0 && systemID < globalParticleSystems.Length)
		{
			GlobalParticleSystem globalParticleSystem = globalParticleSystems[systemID];
			for (int i = 0; i < number; i++)
			{
				emitParams.position = origin.AsVector3() + startVelocity * Time.deltaTime * i / number + ((positionVariation == 0f) ? Vector3.zero : (UnityEngine.Random.insideUnitSphere * positionVariation));
				emitParams.velocity = startVelocity + ((velocityVariation == 0f) ? Vector3.zero : (UnityEngine.Random.insideUnitSphere * velocityVariation));
				globalParticleSystem.main.startLifetime = lifetime + ((lifetimeVariation == 0f) ? 0f : (UnityEngine.Random.value * lifetime * lifetimeVariation));
				globalParticleSystem.main.startSize = startSize + ((sizeVariation == 0f) ? 0f : (UnityEngine.Random.value * startSize * sizeVariation));
				Color mainColor = globalParticleSystem.mainColor;
				mainColor.a = opacity + ((opacityVariation == 0f) ? 0f : (UnityEngine.Random.value * opacity * opacityVariation));
				globalParticleSystem.main.startColor = mainColor;
				globalParticleSystem.system.Emit(emitParams, 1);
			}
		}
	}

	public PrefabEffect GetPrefabEffect(GameObject prefab)
	{
		if (prefab == null)
		{
			return null;
		}
		if (!globalPrefabEffects.TryGetValue(prefab, out var value))
		{
			value = new PrefabEffect[10];
			globalPrefabEffects[prefab] = value;
			globalPrefabIndices[prefab] = 0;
		}
		int num = globalPrefabIndices[prefab];
		globalPrefabIndices[prefab] = (num + 1) % 10;
		if (value[num] == null)
		{
			value[num] = new PrefabEffect(prefab);
		}
		return value[num];
	}
}
