using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.SceneLoading;
using UnityEngine;

public class MapSettingsManager : SceneSingleton<MapSettingsManager>
{
	[Serializable]
	public class Map
	{
		public MapDetails Details;

		public MapSettings Prefab;
	}

	public MapLoader MapLoader;

	public Map[] Maps;

	private Map currentMap;

	private MapSettings mapInScene;

	private void OnValidate()
	{
		if (Maps.Length != MapLoader.Maps.Length)
		{
			UnityEngine.Debug.LogError("Map lists had different counts");
			return;
		}
		if (Maps.Length != Maps.Select((Map x) => x.Details).Distinct().Count())
		{
			UnityEngine.Debug.LogError("Duplicate maps found in list");
			return;
		}
		Map[] maps = Maps;
		foreach (Map map in maps)
		{
			bool flag = false;
			MapDetails[] maps2 = MapLoader.Maps;
			foreach (MapDetails mapDetails in maps2)
			{
				if (map.Details == mapDetails)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				UnityEngine.Debug.LogError("Map " + map.Details.PrefabName + " now found in MapLoader");
				break;
			}
		}
	}

	public async UniTask<bool> EnableMap(string mapName, LoadingFade? loadingFade = null)
	{
		if (mapName == currentMap?.Details.PrefabName)
		{
			UnityEngine.Debug.LogWarning("Map " + mapName + " was already enabled");
			return true;
		}
		ColorLog<MapSettingsManager>.Info("Enabling Map=" + mapName);
		Map toLoad = Maps.FirstOrDefault((Map x) => x.Details.PrefabName == mapName);
		if (toLoad == null)
		{
			UnityEngine.Debug.LogError("Could not find map with name " + mapName);
			return false;
		}
		CancellationToken cancel = base.destroyCancellationToken;
		bool runFade = false;
		if (mapInScene != null)
		{
			if (loadingFade.HasValue)
			{
				runFade = true;
				await FadeAsync(loadingFade.Value.group, enable: true, loadingFade.Value.fadeIn, cancel);
			}
			if (cancel.IsCancellationRequested)
			{
				return false;
			}
			long timestamp = Stopwatch.GetTimestamp();
			UnloadMap(mapInScene);
			ColorLog<MapLoader>.Info($"Prefab Unload duration {BenchmarkScope.MillisecondsSince(timestamp)}ms");
			await UniTask.Yield();
		}
		long timestamp2 = Stopwatch.GetTimestamp();
		mapInScene = LoadMap(toLoad);
		ColorLog<MapLoader>.Info($"Prefab Load duration {BenchmarkScope.MillisecondsSince(timestamp2)}ms");
		currentMap = toLoad;
		if (runFade)
		{
			FadeAsync(loadingFade.Value.group, enable: false, loadingFade.Value.fadeOut, cancel).Forget();
		}
		return true;
	}

	private static async UniTask FadeAsync(CanvasGroup loadingFade, bool enable, float duration, CancellationToken token)
	{
		if (enable)
		{
			loadingFade.gameObject.SetActive(value: true);
		}
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float p = (enable ? (1f / 3f) : 3f);
			float num = Mathf.Pow(Mathf.Clamp01(elapsed / duration), p);
			loadingFade.alpha = (enable ? num : (1f - num));
			await UniTask.Yield();
			if (token.IsCancellationRequested)
			{
				return;
			}
		}
		loadingFade.alpha = (enable ? 1 : 0);
		if (!enable)
		{
			loadingFade.gameObject.SetActive(value: false);
		}
	}

	private static MapSettings LoadMap(Map mapPrefab)
	{
		MapSettings mapSettings = UnityEngine.Object.Instantiate(mapPrefab.Prefab, Datum.origin.position, Quaternion.identity, Datum.origin);
		NetworkMap networkMap = mapSettings.NetworkMap;
		mapSettings.gameObject.SetActive(value: true);
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			networkMap.ServerSpawn(NetworkManagerNuclearOption.i.ServerObjectManager);
		}
		else if (NetworkManagerNuclearOption.i.Client.Active)
		{
			networkMap.ClientRegister(NetworkManagerNuclearOption.i.ClientObjectManager);
		}
		NetworkSceneSingleton<LevelInfo>.i.ApplyMapSettings(mapSettings);
		return mapSettings;
	}

	private static void UnloadMap(MapSettings map)
	{
		NetworkMap networkMap = map.NetworkMap;
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			networkMap.ServerUnspawn(NetworkManagerNuclearOption.i.ServerObjectManager);
		}
		else if (NetworkManagerNuclearOption.i.Client.Active)
		{
			networkMap.ClientUnregister(NetworkManagerNuclearOption.i.ClientObjectManager);
		}
		UnitRegistry.Clear();
		if (map != null)
		{
			map.gameObject.SetActive(value: false);
			UnityEngine.Object.Destroy(map.gameObject);
		}
	}
}
