using System.Collections.Generic;
using UnityEngine;

public static class DebrisManager
{
	private static int maxDebris = 100;

	private static List<GameObject> debrisObjects = new List<GameObject>();

	public static void RegisterDebris(GameObject debrisObject)
	{
		debrisObjects.Add(debrisObject);
		if (debrisObjects.Count > maxDebris)
		{
			NetworkSceneSingleton<Spawner>.i.DestroyLocal(debrisObjects[0], 0f);
			debrisObjects.RemoveAt(0);
		}
	}
}
