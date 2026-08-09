using System;
using Mirage;

public abstract class NetworkSceneSingleton<T> : NetworkBehaviour, ISceneSingleton where T : NetworkSceneSingleton<T>
{
	public static T i;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 0;

	[NonSerialized]
	private const int RPC_COUNT = 0;

	protected virtual void Awake()
	{
		i = (T)this;
		GameManager.SceneSingletons.Add(i);
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

	private void MirageProcessed()
	{
	}

	protected override int GetRpcCount()
	{
		return 0;
	}
}
