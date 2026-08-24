using System.Collections.Generic;
using UnityEngine;

public class EffectManager : SceneSingleton<EffectManager>
{
	private class ImpactSet
	{
		private GameObject gameObject;

		private ParticleSystem[] systems;

		public ImpactSet(GameObject gameObject, ParticleSystem[] systems)
		{
			this.gameObject = gameObject;
			this.systems = systems;
		}

		public void Play(GlobalPosition position, Quaternion rotation)
		{
			gameObject.transform.position = position.ToLocalPosition();
			gameObject.transform.rotation = rotation;
			for (int i = 0; i < systems.Length; i++)
			{
				systems[i].Play();
			}
		}
	}

	public int maxEffects = 20;

	private Queue<GameObject> effectQueue = new Queue<GameObject>();

	private List<ImpactSet> groundImpacts = new List<ImpactSet>();

	private void Start()
	{
		GameObject[] array = GameAssets.i.groundImpacts;
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(array[i], Datum.origin);
			ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
			groundImpacts.Add(new ImpactSet(gameObject, componentsInChildren));
		}
	}

	public void ImpactDust(float impactForce, GlobalPosition globalPosition, Quaternion rotation)
	{
		int index = Mathf.FloorToInt(Mathf.Clamp01(impactForce / 50000f) * (float)groundImpacts.Count - 0.001f);
		groundImpacts[index].Play(globalPosition, rotation);
	}

	public void AddEffect(GameObject effect)
	{
		effectQueue.Enqueue(effect);
		if (effectQueue.Count > maxEffects)
		{
			Object.Destroy(effectQueue.Dequeue());
		}
	}
}
