using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables;
using NuclearOption.AddressableScripts;
using NuclearOption.SavedMission.ConvertVersions;
using NuclearOption.Workshop;
using Steamworks;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public abstract class MissionGroup
	{
		private class Fields
		{
			public readonly AllGroup _all = new AllGroup();

			public readonly ResourceGroup _default = new ResourceGroup("Free Flight", "DefaultMission");

			public readonly ResourceGroup _tutorial = new ResourceGroup("Tutorials");

			public readonly ResourceGroup _builtIn = new ResourceGroup("Missions");

			public readonly UserGroup _user = new UserGroup();

			public readonly TempGroup _temp = new TempGroup();

			public readonly WorkshopGroup _workshop = new WorkshopGroup();

			public readonly EditorMissionGroup _editor = new EditorMissionGroup();

			public readonly MissionGroup[] _allGroups;

			public Fields()
			{
				_allGroups = new MissionGroup[6] { _all, _default, _tutorial, _builtIn, _user, _workshop };
			}
		}

		[Serializable]
		public struct MissionMetaData : IMetaData
		{
			public string FileName;

			public PublishedFileId_t Id { get; set; }

			public string FolderFullPath { get; set; }

			public string GetMissionPath()
			{
				string text = Path.Combine(FolderFullPath, FileName + ".json");
				if (File.Exists(text))
				{
					return text;
				}
				ColorLog<MissionGroup>.LogError("Mission file not found: " + text);
				return null;
			}
		}

		public sealed class ResourceGroup : MissionGroup
		{
			private readonly TextAsset[] assets;

			private readonly MissionKey[] names;

			public ResourceGroup(string nameAndPath)
				: this(nameAndPath, nameAndPath)
			{
			}

			public ResourceGroup(string name, string path)
				: base(name)
			{
				assets = Resources.LoadAll<TextAsset>(path);
				names = assets.Select((TextAsset x) => new MissionKey(x.name, this)).ToArray();
			}

			public override IEnumerable<MissionKey> GetMissions()
			{
				return names;
			}

			public override bool TryGetJson(MissionKey key, out string json)
			{
				for (int i = 0; i < names.Length; i++)
				{
					if (names[i].Name == key.Key)
					{
						json = assets[i].text;
						return true;
					}
				}
				ColorLog<MissionGroup>.Info($"Failed to find key in Resources: {key}");
				json = string.Empty;
				return false;
			}

			public override UniTask<Sprite> GetPreview(string key, CancellationToken token)
			{
				return UniTask.FromResult(Resources.Load<Sprite>("MissionImages/" + key));
			}

			public string ReadFirst()
			{
				return assets[0].text;
			}

			public MissionKey First()
			{
				return names[0];
			}
		}

		public sealed class UserGroup : MissionGroup
		{
			public static string UserMissionDirectory;

			private readonly List<MissionKey> names = new List<MissionKey>();

			private static readonly ProfilerMarker getMissionsMarker = new ProfilerMarker("UserGroup.GetMissions");

			private static readonly ProfilerMarker tryGetJsonMarker = new ProfilerMarker("UserGroup.TryGetJson");

			[RuntimeInitializeOnLoadMethod]
			private static void SetDefaultDirectory()
			{
				UserMissionDirectory = Application.persistentDataPath + "/Missions";
			}

			public static void SetDirectory(string path)
			{
				ColorLog<MissionGroup>.Info("Setting load path to " + path);
				UserMissionDirectory = path;
			}

			public static void OpenFolder()
			{
				if (!Directory.Exists(UserMissionDirectory))
				{
					Directory.CreateDirectory(UserMissionDirectory);
				}
				Application.OpenURL(UserMissionDirectory);
			}

			public UserGroup()
				: base("User")
			{
			}

			public static string GetFolder(string itemName)
			{
				return Path.Combine(UserMissionDirectory, itemName);
			}

			public static string GetJsonPath(string itemName)
			{
				return Path.Combine(UserMissionDirectory, itemName, itemName + ".json");
			}

			public override IEnumerable<MissionKey> GetMissions()
			{
				using (getMissionsMarker.Auto())
				{
					if (!Directory.Exists(UserMissionDirectory))
					{
						return Enumerable.Empty<MissionKey>();
					}
					names.Clear();
					string[] directories = Directory.GetDirectories(UserMissionDirectory);
					for (int i = 0; i < directories.Length; i++)
					{
						string fileName = Path.GetFileName(directories[i]);
						names.Add(new MissionKey(fileName, this));
					}
					return names;
				}
			}

			public override bool TryGetFilePath(MissionKey key, out string path)
			{
				path = GetJsonPath(key.Key);
				return !string.IsNullOrEmpty(path);
			}

			public override bool TryGetJson(MissionKey key, out string json)
			{
				using (tryGetJsonMarker.Auto())
				{
					json = string.Empty;
					string jsonPath = GetJsonPath(key.Key);
					if (!File.Exists(jsonPath))
					{
						ColorLog<MissionGroup>.Info("No file at path: " + jsonPath);
						return false;
					}
					using (FileUtils.ReadAllTextMarker.Auto())
					{
						json = File.ReadAllText(jsonPath);
					}
					return true;
				}
			}

			public override UniTask<Sprite> GetPreview(string key, CancellationToken token)
			{
				return UniTask.FromResult<Sprite>(null);
			}

			public static void SaveMission(string name, Mission mission)
			{
				string folder = GetFolder(name);
				FileUtils.CheckDirectoryExists(folder);
				string jsonPath = GetJsonPath(name);
				CheckV5Backup(name, folder, jsonPath);
				mission.Name = name;
				mission.LoadKey = new MissionKey(name, User);
				string contents = NewtonsoftHelper.ToJson(mission, prettyPrint: true);
				File.WriteAllText(jsonPath, contents);
				ModLoader.WriteMetaData(folder, new MissionMetaData
				{
					FileName = name
				});
			}

			private static void CheckV5Backup(string name, string folder, string jsonPath)
			{
				if (!File.Exists(jsonPath))
				{
					return;
				}
				try
				{
					if (JsonUtility.FromJson<MissionVersion>(File.ReadAllText(jsonPath)).JsonVersion < 6)
					{
						string versionFolder = EditorMissionGroup.GetVersionFolder(name, "V5_Backup");
						FileUtils.CheckDirectoryExists(versionFolder);
						string versionJson = EditorMissionGroup.GetVersionJson(name, "V5_Backup");
						File.Copy(jsonPath, versionJson, overwrite: true);
						string text = Path.Combine(folder, "objective-graph.layout.json");
						if (File.Exists(text))
						{
							string destFileName = Path.Combine(versionFolder, "objective-graph.layout.json");
							File.Copy(text, destFileName, overwrite: true);
						}
					}
				}
				catch (Exception arg)
				{
					Debug.LogError($"Failed to backup v5 mission: {arg}");
				}
			}

			public static bool MoveMission(string oldName, string newName, out string error)
			{
				string folder = GetFolder(oldName);
				string folder2 = GetFolder(newName);
				if (Directory.Exists(folder2))
				{
					error = "Failed to move " + oldName + " because " + newName + " was not deleted";
					return false;
				}
				if (!Directory.Exists(folder))
				{
					error = "Failed to move " + oldName + " because it could not be found";
					return false;
				}
				ColorLog<UserGroup>.Info("Moving folder from " + folder + " to " + folder2);
				Directory.Move(folder, folder2);
				string sourceFileName = folder2 + "/" + oldName + ".json";
				string destFileName = folder2 + "/" + newName + ".json";
				File.Move(sourceFileName, destFileName);
				error = null;
				return true;
			}

			public static void DeleteMission(string name)
			{
				FileUtils.DeleteDirectory(GetFolder(name));
			}

			public static bool Exist(string name, out string validName)
			{
				MissionSaveLoad.ValidateName(ref name, allowEmpty: false);
				validName = name;
				if (!Directory.Exists(UserMissionDirectory))
				{
					return false;
				}
				return Directory.Exists(GetFolder(name));
			}
		}

		public sealed class TempGroup : MissionGroup
		{
			public static readonly string TempMissionDirectory = Application.persistentDataPath + "/TempMissions";

			public TempGroup()
				: base("Temp")
			{
			}

			public static string GetFolder(string itemName)
			{
				return Path.Combine(TempMissionDirectory, itemName);
			}

			public static string GetJsonPath(string itemName)
			{
				return Path.Combine(TempMissionDirectory, itemName, itemName + ".json");
			}

			public override IEnumerable<MissionKey> GetMissions()
			{
				throw new NotSupportedException();
			}

			public override UniTask<Sprite> GetPreview(string key, CancellationToken token)
			{
				throw new NotSupportedException();
			}

			public override bool TryGetFilePath(MissionKey key, out string path)
			{
				path = GetJsonPath(key.Key);
				return !string.IsNullOrEmpty(path);
			}

			public override bool TryGetJson(MissionKey key, out string json)
			{
				json = string.Empty;
				string jsonPath = GetJsonPath(key.Key);
				if (!File.Exists(jsonPath))
				{
					ColorLog<MissionGroup>.Info("No file at temp path: " + jsonPath);
					return false;
				}
				using (FileUtils.ReadAllTextMarker.Auto())
				{
					json = File.ReadAllText(jsonPath);
				}
				return true;
			}

			public static void SaveMission(string name, Mission mission)
			{
				string folder = GetFolder(name);
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
				string jsonPath = GetJsonPath(name);
				string contents = NewtonsoftHelper.ToJson(mission, prettyPrint: true);
				File.WriteAllText(jsonPath, contents);
				ModLoader.WriteMetaData(folder, new MissionMetaData
				{
					FileName = name
				});
			}
		}

		public sealed class WorkshopGroup : MissionGroup
		{
			private static readonly ProfilerMarker getMissionsMarker = new ProfilerMarker("WorkshopGroup.GetMissions");

			private static readonly ProfilerMarker getMissionJsonFileMarker = new ProfilerMarker("WorkshopGroup.GetMissionJsonFile");

			public WorkshopGroup()
				: base("Workshop")
			{
			}

			public override bool TryGetFilePath(MissionKey key, out string path)
			{
				path = key.Key;
				return !string.IsNullOrEmpty(path);
			}

			public override IEnumerable<MissionKey> GetMissions()
			{
				using (getMissionsMarker.Auto())
				{
					return Enumerable.Select(SteamWorkshop.GetSubscribedItems(refresh: true, SubscribedItemType.Mission), CreateItem).WhereNotNullable();
				}
			}

			private MissionKey? CreateItem(SubscribedItem item)
			{
				string missionJsonFile = GetMissionJsonFile(item);
				if (string.IsNullOrEmpty(missionJsonFile))
				{
					return null;
				}
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(missionJsonFile);
				return new MissionKey(missionJsonFile, fileNameWithoutExtension, item.Id, this);
			}

			private string GetMissionJsonFile(SubscribedItem item)
			{
				using (getMissionJsonFileMarker.Auto())
				{
					string folder = item.Folder;
					return ModLoader.ReadMetaData<MissionMetaData>(item.Id, folder)?.GetMissionPath();
				}
			}

			private List<SubscribedItem> GetAllItems()
			{
				return SteamWorkshop.GetSubscribedItems(refresh: false, null);
			}

			private bool TryGet(List<SubscribedItem> allItems, string key, out SubscribedItem item)
			{
				item = default(SubscribedItem);
				ulong.TryParse(key, out var result);
				string b = key;
				if (key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
				{
					b = Path.GetDirectoryName(key);
				}
				foreach (SubscribedItem allItem in allItems)
				{
					if (string.Equals(allItem.Folder, key, StringComparison.OrdinalIgnoreCase) || string.Equals(allItem.Folder, b, StringComparison.OrdinalIgnoreCase))
					{
						item = allItem;
						return true;
					}
					if (Path.GetFileName(allItem.Folder) == key)
					{
						item = allItem;
						return true;
					}
					if (result != 0L && allItem.Id.m_PublishedFileId == result)
					{
						item = allItem;
						return true;
					}
				}
				return false;
			}

			public MissionKey ResolveKey(MissionKey key)
			{
				if (TryFastResolveKey(key, out var resolvedKey))
				{
					return resolvedKey;
				}
				if (SteamManager.ClientInitialized && TryResolveFromSubscribedItems(key, out resolvedKey))
				{
					return resolvedKey;
				}
				ColorLog<MissionGroup>.LogError($"Could not resolve key: {key}");
				return key;
			}

			private bool TryFastResolveKey(MissionKey key, out MissionKey resolvedKey)
			{
				resolvedKey = default(MissionKey);
				if (!key.WorkshopId.HasValue || key.WorkshopId.Value == PublishedFileId_t.Invalid)
				{
					return false;
				}
				if (!SteamWorkshop.TryGetInstallFolder(key.WorkshopId.Value, out var folder))
				{
					ColorLog<MissionGroup>.LogError($"TryGetInstallFolder failed for {key.WorkshopId.Value}");
					return false;
				}
				MissionMetaData? missionMetaData = ModLoader.ReadMetaData<MissionMetaData>(key.WorkshopId.Value, folder);
				if (!missionMetaData.HasValue)
				{
					ColorLog<MissionGroup>.LogError($"Could not find MissionMetaData for {key.WorkshopId.Value}");
					return false;
				}
				MissionMetaData value = missionMetaData.Value;
				string missionPath = value.GetMissionPath();
				resolvedKey = new MissionKey(missionPath, value.FileName, key.WorkshopId, this);
				return true;
			}

			private bool TryResolveFromSubscribedItems(MissionKey key, out MissionKey resolvedKey)
			{
				resolvedKey = default(MissionKey);
				List<SubscribedItem> allItems = GetAllItems();
				if (TryGet(allItems, key.Key, out var item))
				{
					MissionMetaData? missionMetaData = ModLoader.ReadMetaData<MissionMetaData>(item.Id, item.Folder);
					if (missionMetaData.HasValue)
					{
						MissionMetaData value = missionMetaData.Value;
						string missionPath = value.GetMissionPath();
						resolvedKey = new MissionKey(missionPath, value.FileName, item.Id, this);
						return true;
					}
				}
				return false;
			}

			public override bool TryGetJson(MissionKey key, out string json)
			{
				if (string.IsNullOrEmpty(key.Key) || !key.Key.EndsWith(".json", ignoreCase: true, CultureInfo.InvariantCulture) || !File.Exists(key.Key))
				{
					ColorLog<MissionGroup>.InfoWarn($"TryGetJson was called but key was not resolved {key}, resolving key now.");
					key = ResolveKey(key);
				}
				if (TryGetJsonFromFile(key.Key, out json))
				{
					return true;
				}
				ColorLog<MissionGroup>.LogError($"no workshop item with id: {key}");
				return false;
			}

			private static bool TryGetJsonFromFile(string jsonPath, out string json)
			{
				json = null;
				if (string.IsNullOrEmpty(jsonPath))
				{
					return false;
				}
				if (!File.Exists(jsonPath))
				{
					return false;
				}
				json = File.ReadAllText(jsonPath);
				return true;
			}

			public override async UniTask<Sprite> GetPreview(string key, CancellationToken token)
			{
				List<SubscribedItem> subscribedItems = SteamWorkshop.GetSubscribedItems(refresh: false, null);
				if (!TryGet(subscribedItems, key, out var item))
				{
					return null;
				}
				var (flag, steamWorkshopItem) = await SteamWorkshop.GetDetails(item.Id);
				if (!flag || token.IsCancellationRequested)
				{
					return null;
				}
				return await steamWorkshopItem.GetPreview(token);
			}
		}

		public sealed class AllGroup : MissionGroup
		{
			public AllGroup()
				: base("All")
			{
			}

			public override IEnumerable<MissionKey> GetMissions()
			{
				MissionGroup[] allGroups = AllGroups;
				foreach (MissionGroup missionGroup in allGroups)
				{
					if (missionGroup == this)
					{
						continue;
					}
					foreach (MissionKey mission in missionGroup.GetMissions())
					{
						yield return mission;
					}
				}
			}

			public override bool TryGetJson(MissionKey key, out string json)
			{
				throw new NotSupportedException();
			}

			public override UniTask<Sprite> GetPreview(string key, CancellationToken token)
			{
				throw new NotSupportedException();
			}
		}

		private static readonly ProfilerMarker getManyJsonMarker = new ProfilerMarker("MissionGroup.GetManyJson");

		private static Fields i;

		public readonly string Name;

		public static AllGroup All => i._all;

		public static ResourceGroup Default => i._default;

		public static ResourceGroup Tutorial => i._tutorial;

		public static ResourceGroup BuiltIn => i._builtIn;

		public static UserGroup User => i._user;

		public static TempGroup Temp => i._temp;

		public static WorkshopGroup Workshop => i._workshop;

		public static EditorMissionGroup EditorMissions => i._editor;

		public static MissionGroup[] AllGroups => i._allGroups;

		public static MissionGroup GetGroup(string name)
		{
			Init();
			if (Temp.Name == name)
			{
				return Temp;
			}
			MissionGroup[] allGroups = AllGroups;
			foreach (MissionGroup missionGroup in allGroups)
			{
				if (missionGroup.Name == name)
				{
					return missionGroup;
				}
			}
			throw new KeyNotFoundException("No group with name " + name);
		}

		public static void Init()
		{
			if (i == null)
			{
				i = new Fields();
			}
		}

		public abstract IEnumerable<MissionKey> GetMissions();

		public abstract bool TryGetJson(MissionKey key, out string json);

		public virtual bool TryGetFilePath(MissionKey key, out string path)
		{
			path = null;
			return false;
		}

		public virtual IEnumerable<(MissionKey item, string json)> GetManyJson(IEnumerable<MissionKey> items)
		{
			using (getManyJsonMarker.Auto())
			{
				foreach (MissionKey item in items)
				{
					if (TryGetJson(item, out var json))
					{
						yield return (item: item, json: json);
					}
				}
			}
		}

		public abstract UniTask<Sprite> GetPreview(string key, CancellationToken token = default(CancellationToken));

		public virtual bool ContainsMission(string key)
		{
			foreach (MissionKey mission in GetMissions())
			{
				if (mission.Key == key)
				{
					return true;
				}
			}
			return false;
		}

		protected MissionGroup(string name)
		{
			Name = name;
		}
	}
}
