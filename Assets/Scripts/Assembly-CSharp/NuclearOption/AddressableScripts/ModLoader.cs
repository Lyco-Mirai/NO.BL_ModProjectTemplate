using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.Workshop;
using Steamworks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace NuclearOption.AddressableScripts
{
	public abstract class ModLoader
	{
		private class LocationLocator : IResourceLocator
		{
			public string LocatorId => "LocationToLocationLocator";

			public IEnumerable<object> Keys => Enumerable.Empty<object>();

			public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
			{
				if (key is IResourceLocation resourceLocation && type.IsAssignableFrom(resourceLocation.ResourceType))
				{
					locations = new List<IResourceLocation> { resourceLocation };
					return true;
				}
				locations = null;
				return false;
			}
		}

		private static readonly ProfilerMarker readMetaDataMarker = new ProfilerMarker("ModLoader.ReadMetaData");

		private static bool hasInit;

		public abstract string Label { get; }

		public abstract Type AssetType { get; }

		private static void CheckInit()
		{
			if (!hasInit)
			{
				hasInit = true;
				Addressables.AddResourceLocator(new LocationLocator());
			}
		}

		private static string GetMetaPath(string folderName)
		{
			return Path.Combine(folderName, "meta.json");
		}

		public static void WriteMetaData<TMeta>(string folder, TMeta meta) where TMeta : struct, IMetaData
		{
			string metaPath = GetMetaPath(folder);
			string contents = JsonUtility.ToJson(meta);
			File.WriteAllText(metaPath, contents);
		}

		public static TMeta? ReadMetaData<TMeta>(SubscribedItem steamItem) where TMeta : struct, IMetaData
		{
			return ReadMetaData<TMeta>(steamItem.Id, steamItem.Folder);
		}

		public static TMeta? ReadMetaData<TMeta>(string folder) where TMeta : struct, IMetaData
		{
			return ReadMetaData<TMeta>(PublishedFileId_t.Invalid, folder);
		}

		public static TMeta? ReadMetaData<TMeta>(PublishedFileId_t id, string folder) where TMeta : struct, IMetaData
		{
			using (readMetaDataMarker.Auto())
			{
				string metaPath = GetMetaPath(folder);
				if (!File.Exists(metaPath))
				{
					Debug.LogWarning(metaPath + " does not exist");
					return null;
				}
				string json = File.ReadAllText(metaPath);
				try
				{
					TMeta value = JsonUtility.FromJson<TMeta>(json);
					value.FolderFullPath = folder;
					value.Id = id;
					return value;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					return null;
				}
			}
		}

		public abstract UniTask<string> GetCatalogPath(string folderName);

		protected async UniTask<IResourceLocation> GetAddressableKey(string catalogFolder)
		{
			if (ModLoadCache.CacheLocations.TryGetValue(catalogFolder, out var locationCache))
			{
				bool pending = locationCache.TaskPending;
				if (pending)
				{
					ColorLog<ModLoader>.Info("Waiting on pending task for " + catalogFolder);
				}
				while (locationCache.TaskPending)
				{
					await UniTask.Yield();
				}
				if (pending)
				{
					ColorLog<ModLoader>.Info($"Pending task finished for {catalogFolder} success={locationCache.Location != null}");
				}
				return locationCache.Location;
			}
			ColorLog<ModLoader>.Info("First load for " + catalogFolder);
			locationCache = new ModLoadCache.LocationCache
			{
				TaskPending = true
			};
			ModLoadCache.CacheLocations.Add(catalogFolder, locationCache);
			try
			{
				return locationCache.Location = await GetAddressableKeyFullLoad(catalogFolder);
			}
			finally
			{
				locationCache.TaskPending = false;
			}
		}

		protected async UniTask<IResourceLocation> GetAddressableKeyFullLoad(string catalogFolder)
		{
			string text = await GetCatalogPath(catalogFolder);
			if (!File.Exists(text))
			{
				Debug.LogError(text + " does not exist");
				return null;
			}
			AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(text);
			(await handle).Locate(Label, AssetType, out var locations);
			IResourceLocation result = locations[0];
			Addressables.Release(handle);
			return result;
		}

		public async UniTask<AsyncOperationHandle<TData>> LoadAsset<TData>(string folder)
		{
			IResourceLocation resourceLocation = await GetAddressableKey(folder);
			if (resourceLocation == null)
			{
				return default(AsyncOperationHandle<TData>);
			}
			return await LoadAssetInternal<TData>(resourceLocation);
		}

		public static UniTask<AsyncOperationHandle<TData>> LoadAsset<TData>(AssetReferenceT<TData> obj) where TData : UnityEngine.Object
		{
			return LoadAssetInternal<TData>(obj);
		}

		public static async UniTask<AsyncOperationHandle<TData>> LoadAssetInternal<TData>(object obj)
		{
			CheckInit();
			AsyncOperationHandle<TData> handle = Addressables.LoadAssetAsync<TData>(obj);
			await handle;
			return handle;
		}
	}
	public abstract class ModLoader<TMeta, TData> : ModLoader where TMeta : struct, IMetaData
	{
		public override Type AssetType => typeof(TData);

		public abstract IEnumerable<TMeta> ListMetaData();
	}
}
