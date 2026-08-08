using System;
using UnityEngine;

public abstract class SceneSingleton<T> : MonoBehaviour, ISceneSingleton where T : SceneSingleton<T>
{
	public static T i;

	protected virtual void Awake()
	{
		SetupSingleton();
	}

	protected void SetupSingleton()
	{
		if (i != this)
		{
			i = (T)this;
			GameManager.SceneSingletons.Add(i);
		}
	}

	public static void ThrowIfInstanceNull()
	{
		if (i == null)
		{
			throw new InvalidOperationException(typeof(T).FullName + " was not in scene");
		}
	}

	bool ISceneSingleton.ClearInstance()
	{
		bool num = this == null;
		if (num && this == i)
		{
			i = null;
		}
		return num;
	}
}
