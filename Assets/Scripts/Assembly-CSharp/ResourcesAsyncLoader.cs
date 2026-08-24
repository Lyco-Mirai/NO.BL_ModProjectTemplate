using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ResourcesAsyncLoader
{
	public static async UniTask LoadPrefab(string path, CancellationToken cancel, Action<GameObject> afterLoad)
	{
		long start = BenchmarkScope.GetTimestamp();
		GameObject gameObject = await InstantiateOne((GameObject)(await Resources.LoadAsync<GameObject>(path).ToUniTask()), cancel);
		if (!cancel.IsCancellationRequested)
		{
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			long timestamp = BenchmarkScope.GetTimestamp();
			afterLoad?.Invoke(gameObject);
			long timestamp2 = BenchmarkScope.GetTimestamp();
			ColorLog<ResourcesAsyncLoader>.Info($"Loaded {typeof(GameObject)}, load:{BenchmarkScope.MillisecondsBetween(start, timestamp):0}, setup:{BenchmarkScope.MillisecondsBetween(timestamp, timestamp2)}");
		}
	}

	public static ResourcesAsyncLoader<T> Create<T>(string path, Action<T> afterLoad = null) where T : ScriptableObject
	{
		return new ResourcesAsyncLoader<T>(path, delegate(UnityEngine.Object scriptableObject, CancellationToken cancel)
		{
			afterLoad?.Invoke((T)scriptableObject);
			return UniTask.FromResult((T)scriptableObject);
		});
	}

	public static ResourcesAsyncLoader<T> Create<T>(string path, Func<GameObject, T> afterLoad = null) where T : MonoBehaviour
	{
		return new ResourcesAsyncLoader<T>(path, async delegate(UnityEngine.Object prefab, CancellationToken cancel)
		{
			GameObject gameObject = await InstantiateOne((GameObject)prefab, cancel);
			if (cancel.IsCancellationRequested)
			{
				return (T)null;
			}
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			if (afterLoad == null)
			{
				afterLoad = (GameObject c) => c.GetComponent<T>();
			}
			return afterLoad(gameObject);
		});
	}

	private static async UniTask<GameObject> InstantiateOne(GameObject prefab, CancellationToken cancel)
	{
		var (flag, array) = await UnityEngine.Object.InstantiateAsync(prefab).ToUniTask(null, PlayerLoopTiming.Update, cancel).SuppressCancellationThrow();
		if (flag)
		{
			return null;
		}
		return array[0];
	}
}
public class ResourcesAsyncLoader<T> : ResourcesAsyncLoader where T : UnityEngine.Object
{
	private readonly string path;

	private readonly Func<UnityEngine.Object, CancellationToken, UniTask<T>> createInstance;

	private T instance;

	public bool IsLoaded => instance != null;

	public ResourcesAsyncLoader(string path, Func<UnityEngine.Object, CancellationToken, UniTask<T>> createInstance)
	{
		this.path = path;
		this.createInstance = createInstance;
	}

	public void AssetNotLoaded()
	{
		if (instance != null)
		{
			Debug.LogError($"{typeof(T)} Should not have been loaded multiple time");
		}
	}

	public T Get()
	{
		if (!MainMenu.ApplicationIsQuitting && instance == null)
		{
			Debug.LogError($"{typeof(T)} was not preloaded by menu");
		}
		return instance;
	}

	public async UniTask Load(CancellationToken cancel)
	{
		if (instance != null)
		{
			Debug.LogWarning($"{typeof(T)} already loaded");
			return;
		}
		long start = BenchmarkScope.GetTimestamp();
		UnityEngine.Object arg = await Resources.LoadAsync<UnityEngine.Object>(path).ToUniTask();
		if (cancel.IsCancellationRequested)
		{
			return;
		}
		if (instance != null)
		{
			Debug.LogError($"{typeof(T)} was loaded sync while preloading");
			return;
		}
		long load = BenchmarkScope.GetTimestamp();
		instance = await createInstance(arg, cancel);
		if (!cancel.IsCancellationRequested)
		{
			long timestamp = BenchmarkScope.GetTimestamp();
			ColorLog<ResourcesAsyncLoader>.Info($"Loaded {typeof(T)}, load:{BenchmarkScope.MillisecondsBetween(start, load):0}, setup:{BenchmarkScope.MillisecondsBetween(load, timestamp):0}");
		}
	}
}
