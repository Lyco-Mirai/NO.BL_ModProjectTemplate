using System.Collections.Generic;
using UnityEngine;

public static class Explosion
{
	private static Collider[] colliderBuffer = new Collider[4096];

	public static void SimulateForce(Vector3 position, float yield)
	{
		float num = Mathf.Pow(yield, 0.3333f);
		float radius = num * 20f;
		Dictionary<Collider, IDamageable> dictionary = new Dictionary<Collider, IDamageable>();
		List<Collider> list = new List<Collider>();
		int num2 = Physics.OverlapSphereNonAlloc(position, radius, colliderBuffer);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = colliderBuffer[i];
			IDamageable component = collider.gameObject.GetComponent<IDamageable>();
			if (component != null)
			{
				dictionary.Add(collider, component);
			}
		}
		foreach (KeyValuePair<Collider, IDamageable> item in dictionary)
		{
			if (!Physics.Linecast(position, item.Key.gameObject.transform.position, out var hitInfo, ~(int)PhysicsLayers.ExclusionZonesMask))
			{
				continue;
			}
			if (dictionary.ContainsKey(hitInfo.collider) && !list.Contains(hitInfo.collider))
			{
				dictionary[hitInfo.collider].TakeShockwave(position, num, num);
				if (PlayerSettings.debugVis)
				{
					GameObject gameObject = NetworkSceneSingleton<Spawner>.i.SpawnLocal(GameAssets.i.debugArrow, hitInfo.collider.transform);
					gameObject.transform.position = position;
					gameObject.transform.rotation = Quaternion.LookRotation(hitInfo.point - position);
					gameObject.transform.localScale = new Vector3(1f, 1f, hitInfo.distance);
					NetworkSceneSingleton<Spawner>.i.DestroyLocal(gameObject, 10f);
				}
			}
			list.Add(hitInfo.collider);
		}
	}
}
