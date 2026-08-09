using System.Collections.Generic;
using UnityEngine;

public class MaterialCleanup
{
	private readonly HashSet<Material> needCleanup = new HashSet<Material>();

	public void Add(Material material)
	{
		needCleanup.Add(material);
	}

	public void Remove(Material material)
	{
		needCleanup.Remove(material);
	}

	public void CleanupAll()
	{
		foreach (Material item in needCleanup)
		{
			if (item != null)
			{
				Object.Destroy(item);
			}
		}
		needCleanup.Clear();
	}
}
