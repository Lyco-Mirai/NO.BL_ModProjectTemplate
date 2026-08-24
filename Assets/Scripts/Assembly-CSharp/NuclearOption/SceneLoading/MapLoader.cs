using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace NuclearOption.SceneLoading
{
	[CreateAssetMenu(fileName = "MapLoader", menuName = "ScriptableObjects/MapLoader", order = 998)]
	public class MapLoader : ScriptableObject
	{
		public enum LoadResult
		{
			None = 0,
			InvalidKey = 1,
			Failed = 2,
			ChangedScene = 3,
			ChangedWorldPrefab = 4,
			AlreadyLoaded = 5
		}

		[Serializable]
		public struct SceneKey : IEquatable<SceneKey>
		{
			[Serializable]
			public enum KeyType : byte
			{
				BuiltinScene = 1,
				Addressables = 2
			}

			public KeyType Type;

			public string Path;

			public SceneKey(KeyType type, string path)
			{
				Type = type;
				Path = path;
			}

			public override string ToString()
			{
				return $"({Type},{Path})";
			}

			public readonly bool Equals(SceneKey other)
			{
				if (Type == other.Type)
				{
					return Path == other.Path;
				}
				return false;
			}

			public readonly bool IsDefault()
			{
				return default(SceneKey).Equals(this);
			}
		}

		public static readonly string Empty = "EMPTY";

		public static readonly string MainMenu = "Assets/Scenes/MainMenu/MainMenu.unity";

		public static readonly string MultiplayerMenu = "Assets/Scenes/MultiplayerMenu/MultiplayerMenu.unity";

		public static readonly string MissionsMenu = "Assets/Scenes/MissionsMenu/MissionsMenu.unity";

		public static readonly string Encyclopedia = "Assets/Scenes/Encyclopedia/Encyclopedia.unity";

		[SerializeField]
		private GameObject menuCameraPrefab;

		[FormerlySerializedAs("DefaultScene")]
		public MapKey DefaultMap;

		[Scene]
		public string[] GameScenes;

		[Header("Game World")]
		[Scene]
		public string GameWorldScene;

		public MapDetails[] Maps;

		public MapKey CurrentMap { get; private set; }

		public SceneKey CurrentScene { get; private set; }

		public void ClearMap()
		{
			CurrentMap = default(MapKey);
			CurrentScene = default(SceneKey);
		}

		public async UniTask<LoadResult> Load(MapKey mapKey, IProgress<float> progress = null, LoadingFade? loadingFade = null)
		{
			if (mapKey.Type == MapKey.KeyType.None)
			{
				mapKey = DefaultMap;
				ColorLog<MapLoader>.Info($"Using Default scene {mapKey}");
			}
			else
			{
				ColorLog<MapLoader>.Info($"Try load {mapKey}");
			}
			if (mapKey.Equals(CurrentMap))
			{
				ColorLog<MapLoader>.Info("Already loaded");
				return LoadResult.AlreadyLoaded;
			}
			if (!CanLoad(mapKey))
			{
				return LoadResult.InvalidKey;
			}
			SceneKey sceneKey = SceneFromMap(mapKey);
			long startSceneTime = Stopwatch.GetTimestamp();
			LoadResult loadResult = await LoadScene(sceneKey, progress);
			ColorLog<MapLoader>.Info($"Scene Load duration {BenchmarkScope.MillisecondsSince(startSceneTime)}ms");
			if (loadResult == LoadResult.Failed)
			{
				return LoadResult.Failed;
			}
			CurrentScene = sceneKey;
			if (loadResult == LoadResult.ChangedScene)
			{
				ColorLog<MapLoader>.Info("Scene changed, settings up network objects");
				if (NetworkManagerNuclearOption.i.Server.Active)
				{
					ServerSceneSetup();
				}
				else if (NetworkManagerNuclearOption.i.Client.Active)
				{
					ClientSceneSetup();
				}
			}
			if (mapKey.Type == MapKey.KeyType.GameWorldPrefab)
			{
				if (!(await SceneSingleton<MapSettingsManager>.i.EnableMap(mapKey.Path, (loadResult != LoadResult.ChangedScene) ? loadingFade : ((LoadingFade?)null))))
				{
					return LoadResult.Failed;
				}
				if (loadResult == LoadResult.AlreadyLoaded)
				{
					loadResult = LoadResult.ChangedWorldPrefab;
				}
			}
			CurrentMap = mapKey;
			return loadResult;
		}

		public bool CanLoad(MapKey key)
		{
			switch (key.Type)
			{
			case MapKey.KeyType.None:
				ColorLog<MapLoader>.InfoWarn("Can't load key None");
				return false;
			case MapKey.KeyType.GameWorldPrefab:
			{
				bool num = Maps.Any((MapDetails x) => x.PrefabName == key.Path);
				if (num)
				{
					ColorLog<MapLoader>.Info("Found GameWorldPrefab " + key.Path);
					return num;
				}
				ColorLog<MapLoader>.InfoWarn("Could not find GameWorldPrefab " + key.Path);
				return num;
			}
			case MapKey.KeyType.BuiltinScene:
			{
				if (key.Path == GameWorldScene)
				{
					ColorLog<MapLoader>.InfoWarn("BuiltinScene should not be loading GameWorld, Use GameWorldPrefab instead");
					return false;
				}
				string[] gameScenes = GameScenes;
				foreach (string text in gameScenes)
				{
					if (text == key.Path)
					{
						ColorLog<MapLoader>.Info("Found Scene full path " + key.Path);
						return true;
					}
					if (Path.GetFileNameWithoutExtension(text) == key.Path)
					{
						ColorLog<MapLoader>.Info("Found Scene name " + key.Path);
						return true;
					}
				}
				ColorLog<MapLoader>.InfoWarn("Could not find " + key.Path + " in Allowed scene list");
				return false;
			}
			default:
				throw new InvalidEnumArgumentException("Type", (int)key.Type, typeof(MapKey.KeyType));
			}
		}

		private async UniTask<LoadResult> LoadScene(SceneKey key, IProgress<float> progress)
		{
			if (key.Equals(CurrentScene))
			{
				ColorLog<MapLoader>.Info($"Scene already loaded {key}");
				return LoadResult.AlreadyLoaded;
			}
			ColorLog<MapLoader>.Info($"Trying to load {key}");
			if (key.Type == SceneKey.KeyType.BuiltinScene)
			{
				try
				{
					ColorLog<MapLoader>.Info("Start Load " + key.Path);
					await SceneManager.LoadSceneAsync(key.Path).ToUniTask(progress);
					ColorLog<MapLoader>.Info("Load Finished, active scene = " + SceneManager.GetSceneAt(SceneManager.sceneCount - 1).path);
					return LoadResult.ChangedScene;
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError($"Loading scene threw {ex.GetType()}");
					UnityEngine.Debug.LogException(ex);
					return LoadResult.Failed;
				}
			}
			throw new InvalidEnumArgumentException("Type", (int)key.Type, typeof(MapKey.KeyType));
		}

		private static void ClientSceneSetup()
		{
			ClientObjectManager clientObjectManager = NetworkManagerNuclearOption.i.ClientObjectManager;
			clientObjectManager.spawnableObjects.Clear();
			NetworkIdentity[] array = Resources.FindObjectsOfTypeAll<NetworkIdentity>();
			foreach (NetworkIdentity networkIdentity in array)
			{
				if (!networkIdentity.IsSpawned && GetObjectType(networkIdentity) == ObjectType.SceneObject)
				{
					clientObjectManager.spawnableObjects.Add(networkIdentity.SceneId, networkIdentity);
				}
			}
		}

		private static void ServerSceneSetup()
		{
			ServerObjectManager serverObjectManager = NetworkManagerNuclearOption.i.ServerObjectManager;
			NetworkIdentity[] array = Resources.FindObjectsOfTypeAll<NetworkIdentity>();
			List<NetworkIdentity> list = new List<NetworkIdentity>();
			NetworkIdentity[] array2 = array;
			foreach (NetworkIdentity networkIdentity in array2)
			{
				if (networkIdentity.IsSpawned || GetObjectType(networkIdentity) == ObjectType.SceneObject)
				{
					list.Add(networkIdentity);
				}
			}
			list.Sort((NetworkIdentity x, NetworkIdentity y) => (x.NetId == 0 && y.NetId == 0) ? x.SceneId.CompareTo(y.SceneId) : x.NetId.CompareTo(y.NetId));
			foreach (NetworkIdentity item in list)
			{
				serverObjectManager.Spawn(item);
			}
		}

		public SceneKey SceneFromMap(MapKey key)
		{
			switch (key.Type)
			{
			case MapKey.KeyType.GameWorldPrefab:
				ColorLog<MapLoader>.Info("Loading GameWorld=" + GameWorldScene);
				return new SceneKey(SceneKey.KeyType.BuiltinScene, GameWorldScene);
			case MapKey.KeyType.BuiltinScene:
				return new SceneKey(SceneKey.KeyType.BuiltinScene, key.Path);
			default:
				throw new InvalidEnumArgumentException("Type", (int)key.Type, typeof(MapKey.KeyType));
			}
		}

		public static ObjectType GetObjectType(NetworkIdentity identity)
		{
			if (identity.gameObject.GetComponentInParent<NetworkMap>(includeInactive: true) != null)
			{
				return ObjectType.MapObject;
			}
			if (identity.IsSceneObject)
			{
				return ObjectType.SceneObject;
			}
			return ObjectType.Prefab;
		}

		public UnityEngine.AsyncOperation LoadEmpty()
		{
			ColorLog<MapLoader>.Info("Creating empty scene");
			Scene activeScene = SceneManager.GetActiveScene();
			try
			{
				Scene scene = SceneManager.CreateScene("empty");
				if (GameManager.ShowEffects)
				{
					SceneManager.MoveGameObjectToScene(UnityEngine.Object.Instantiate(menuCameraPrefab), scene);
				}
			}
			catch (Exception arg)
			{
				UnityEngine.Debug.LogError($"Failed to create empty: {arg}");
			}
			if (activeScene.IsValid() && activeScene.isLoaded)
			{
				UnityEngine.AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(activeScene);
				if (asyncOperation == null)
				{
					UnityEngine.Debug.LogWarning("UnloadSceneAsync returned null");
				}
				return asyncOperation;
			}
			UnityEngine.Debug.LogWarning("Active scene was not loaded");
			return null;
		}

		public bool TryGetMapName(MapKey key, out string mapName)
		{
			mapName = null;
			if (key.Type == MapKey.KeyType.None)
			{
				key = DefaultMap;
			}
			MapDetails mapDetails = Maps.FirstOrDefault((MapDetails x) => x.PrefabName == key.Path);
			if (mapDetails != null)
			{
				mapName = mapDetails.MapName;
			}
			return !string.IsNullOrEmpty(mapName);
		}
	}
}
