using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using NuclearOption.AddressableScripts;
using NuclearOption.NodeGraph;
using NuclearOption.SavedMission.ConvertVersions;
using NuclearOption.Workshop;
using Steamworks;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public static class MissionSaveLoad
	{
		public readonly struct CachedMissionHeader
		{
			public readonly MissionQuickLoad QuickLoad;

			public readonly string FilePath;

			public readonly DateTime LastWriteTime;

			public CachedMissionHeader(MissionQuickLoad quickLoad, string filePath, DateTime lastWriteTime)
			{
				QuickLoad = quickLoad;
				FilePath = filePath;
				LastWriteTime = lastWriteTime;
			}

			public static CachedMissionHeader Create(MissionKey key, MissionQuickLoad quickLoad)
			{
				string path = null;
				DateTime lastWriteTime = default(DateTime);
				key.Group?.TryGetFilePath(key, out path);
				if (!string.IsNullOrEmpty(path) && File.Exists(path))
				{
					lastWriteTime = File.GetLastWriteTimeUtc(path);
				}
				return new CachedMissionHeader(quickLoad, path, lastWriteTime);
			}

			public bool IsUpToDate()
			{
				if (string.IsNullOrEmpty(FilePath))
				{
					return true;
				}
				if (!File.Exists(FilePath))
				{
					ColorLog<MissionKey>.InfoWarn("File could not be found: " + FilePath);
					return false;
				}
				return File.GetLastWriteTimeUtc(FilePath) == LastWriteTime;
			}
		}

		private static readonly ProfilerMarker saveMissionMarker = new ProfilerMarker("MissionSaveLoad.SaveMission");

		private static readonly ProfilerMarker saveMissionVersionMarker = new ProfilerMarker("MissionSaveLoad.SaveMissionVersion");

		private static readonly ProfilerMarker saveMissionTempMarker = new ProfilerMarker("MissionSaveLoad.SaveMissionTemp");

		private static readonly ProfilerMarker tryLoadMarker = new ProfilerMarker("MissionSaveLoad.TryLoad");

		private static readonly ProfilerMarker tryReadJsonMarker = new ProfilerMarker("MissionSaveLoad.TryReadJson");

		private static readonly ProfilerMarker quickLoadOneMarker = new ProfilerMarker("MissionSaveLoad.QuickLoadOne");

		private static readonly ProfilerMarker quickLoadManyMarker = new ProfilerMarker("MissionSaveLoad.QuickLoadMany");

		private static readonly ProfilerMarker readQuickLoadMarker = new ProfilerMarker("MissionSaveLoad.ReadQuickLoad");

		private static readonly ProfilerMarker getFileInfoMarker = new ProfilerMarker("MissionSaveLoad.GetFileInfo");

		private static readonly StringBuilder stringBuilder = new StringBuilder();

		private static readonly Dictionary<MissionKey, CachedMissionHeader> quickLoadCache = new Dictionary<MissionKey, CachedMissionHeader>();

		public const string UNTITLED_NAME = "Untitled Mission";

		public static event Action<MissionKey> AfterSave;

		public static void TriggerAfterSave(MissionKey key)
		{
			MissionSaveLoad.AfterSave?.Invoke(key);
		}

		public static void ClearCache()
		{
			quickLoadCache.Clear();
		}

		public static bool ValidateName(ref string saveName, bool allowEmpty)
		{
			if (string.IsNullOrWhiteSpace(saveName))
			{
				if (!allowEmpty)
				{
					saveName = "Untitled Mission";
					return true;
				}
				return false;
			}
			bool result = FileUtils.CheckOsSafeName(ref saveName);
			if (string.Equals(saveName, "meta", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning("'meta.json' is a reserved name, changing name to 'meta2.json' instead");
				saveName = "meta2";
				return true;
			}
			if (string.Equals(saveName, "workshop", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning("'workshop.json' is a reserved name, changing name to 'workshop2.json' instead");
				saveName = "workshop2";
				return true;
			}
			return result;
		}

		public static MissionKey GetOrCreateUserSaveKey(Mission mission)
		{
			if (mission.LoadKey.HasValue)
			{
				MissionKey value = mission.LoadKey.Value;
				if (value.Group == MissionGroup.User)
				{
					return value;
				}
				if (value.Group == MissionGroup.EditorMissions)
				{
					return EditorMissionGroup.CreateUserKey(value);
				}
				ColorLog<MissionKey>.Info("Converting " + mission.Name + " to User groups key");
				return new MissionKey(value.Name, MissionGroup.User);
			}
			string saveName = mission.Name;
			ValidateName(ref saveName, allowEmpty: false);
			return new MissionKey(saveName, MissionGroup.User);
		}

		public static void SaveMission(Mission mission, bool callBeforeSave = true)
		{
			SaveMission(mission, ref mission.Name, callBeforeSave);
		}

		public static void SaveMission(Mission mission, ref string saveName, bool callBeforeSave = true)
		{
			using (saveMissionMarker.Auto())
			{
				ValidateName(ref saveName, allowEmpty: false);
				if (callBeforeSave)
				{
					mission.BeforeSave();
				}
				MissionGroup.UserGroup.SaveMission(saveName, mission);
				MissionKey value = mission.LoadKey.Value;
				SaveObjectiveGraphLayout(mission, value);
				MissionSaveLoad.AfterSave?.Invoke(value);
			}
		}

		public static void SaveMissionVersion(Mission mission, string versionName, bool callBeforeSave = true)
		{
			using (saveMissionVersionMarker.Auto())
			{
				ValidateName(ref mission.Name, allowEmpty: false);
				if (callBeforeSave)
				{
					mission.BeforeSave();
				}
				MissionKey missionKey = EditorMissionGroup.SaveVersion(mission.Name, mission, versionName);
				mission.LoadKey = missionKey;
				SaveObjectiveGraphLayout(mission, missionKey);
				MissionSaveLoad.AfterSave?.Invoke(missionKey);
			}
		}

		public static bool SaveMissionTemp(Mission mission, bool runBeforeSave, out Mission missionCopy, out string loadErrors)
		{
			return SaveMissionTemp(mission, mission.Name, runBeforeSave, out missionCopy, out loadErrors);
		}

		public static bool SaveMissionTemp(Mission mission, string overrideName, bool runBeforeSave, out Mission missionCopy, out string loadErrors)
		{
			using (saveMissionTempMarker.Auto())
			{
				ValidateName(ref overrideName, allowEmpty: false);
				if (runBeforeSave)
				{
					mission.BeforeSave();
				}
				MissionGroup.TempGroup.SaveMission(overrideName, mission);
				MissionKey missionKey = new MissionKey(overrideName, mission.Name, null, MissionGroup.Temp);
				SaveObjectiveGraphLayout(mission, missionKey);
				MissionSaveLoad.AfterSave?.Invoke(missionKey);
				return missionKey.TryLoad(out missionCopy, out loadErrors);
			}
		}

		public static bool LoadMissionTemp(Mission mission, out Mission missionCopy, out string loadErrors)
		{
			return new MissionKey(mission.Name, MissionGroup.Temp).TryLoad(out missionCopy, out loadErrors);
		}

		public static bool TryLoad(MissionKey item, out Mission mission, out string error)
		{
			using (tryLoadMarker.Auto())
			{
				item.ThrowIfInvalid();
				mission = null;
				string json = null;
				bool flag = false;
				error = null;
				string name = item.Name;
				try
				{
					flag = item.Group.TryGetJson(item, out json);
				}
				catch (Exception ex)
				{
					string text = "Failed to load mission with name " + name + " because of " + ex.GetType().Name;
					Debug.LogError(text + " (see below)");
					Debug.LogException(ex);
					error = $"{text}\n{ex}";
				}
				if (!flag)
				{
					if (string.IsNullOrEmpty(error))
					{
						error = "Could not find mission with name " + name;
					}
					return false;
				}
				return TryReadJson(item, json, out mission, out error);
			}
		}

		public static Mission LoadDefault()
		{
			string json = MissionGroup.Default.ReadFirst();
			if (!TryReadJson(new MissionKey("Free Flight", MissionGroup.Default), json, out var mission, out var error))
			{
				throw new Exception("Default to load default mission: " + error);
			}
			return mission;
		}

		public static bool TryReadJson(MissionKey key, string json, out Mission mission, out string error)
		{
			using (tryReadJsonMarker.Auto())
			{
				try
				{
					if (JsonUtility.FromJson<MissionVersion>(json).JsonVersion < 6)
					{
						mission = MissionVersionUpgrade.FromV5(json, key.Name);
					}
					else
					{
						mission = NewtonsoftHelper.FromJson<Mission>(json);
					}
					mission.AfterLoad(key);
					error = null;
					return true;
				}
				catch (ArgumentException ex) when (ex.Message.StartsWith("JSON parse error"))
				{
					error = ex.Message;
				}
				catch (AggregateException ex2)
				{
					stringBuilder.Clear();
					string text = $"Failed to load mission with name {key.Name} because of {ex2.InnerExceptions.Count} Exception(s)";
					Debug.LogError(text + " (see below)");
					stringBuilder.AppendLine(text);
					foreach (Exception innerException in ex2.InnerExceptions)
					{
						Debug.LogException(innerException);
						stringBuilder.AppendLine($"- {innerException.GetType()}: {innerException.Message}");
					}
					error = stringBuilder.ToString();
				}
				catch (Exception ex3)
				{
					Debug.LogError($"Unexpected error when loading {key} (see below)");
					Debug.LogException(ex3);
					error = ex3.ToString();
				}
				mission = null;
				return false;
			}
		}

		public static IEnumerable<(MissionKey key, MissionQuickLoad mission)> QuickLoadMany(IEnumerable<MissionKey> items)
		{
			using (quickLoadManyMarker.Auto())
			{
				List<MissionKey> uncachedKeys = new List<MissionKey>();
				foreach (IGrouping<MissionGroup, MissionKey> item3 in from x in items
					group x by x.Group)
				{
					MissionGroup group = item3.Key;
					uncachedKeys.Clear();
					foreach (MissionKey item4 in item3)
					{
						if (quickLoadCache.TryGetValue(item4, out var value) && value.IsUpToDate())
						{
							yield return (key: item4, mission: value.QuickLoad);
						}
						else
						{
							uncachedKeys.Add(item4);
						}
					}
					if (uncachedKeys.Count <= 0)
					{
						continue;
					}
					foreach (var item5 in group.GetManyJson(uncachedKeys))
					{
						MissionKey item = item5.item;
						string item2 = item5.json;
						MissionQuickLoad? missionQuickLoad = null;
						try
						{
							missionQuickLoad = ReadQuickLoad(item, item2);
						}
						catch (Exception arg)
						{
							Debug.LogError($"Failed to quickLoad {item}, {arg}");
						}
						if (missionQuickLoad.HasValue)
						{
							yield return (key: item, mission: missionQuickLoad.Value);
						}
					}
				}
			}
		}

		private static MissionQuickLoad ReadQuickLoad(MissionKey key, string json)
		{
			using (readQuickLoadMarker.Auto())
			{
				MissionQuickLoad missionQuickLoad = JsonUtility.FromJson<MissionQuickLoad>(json);
				missionQuickLoad.AfterLoad(key);
				MissionTag.AddAutoTags(missionQuickLoad);
				quickLoadCache[key] = CachedMissionHeader.Create(key, missionQuickLoad);
				return missionQuickLoad;
			}
		}

		public static bool QuickLoadOne(MissionKey key, out MissionQuickLoad mission)
		{
			using (quickLoadOneMarker.Auto())
			{
				if (quickLoadCache.TryGetValue(key, out var value) && value.IsUpToDate())
				{
					mission = value.QuickLoad;
					return true;
				}
				mission = default(MissionQuickLoad);
				try
				{
					if (key.Group.TryGetJson(key, out var json))
					{
						mission = ReadQuickLoad(key, json);
						return true;
					}
					return false;
				}
				catch (Exception exception)
				{
					Debug.LogError("Unexpected error when loading (see below)");
					Debug.LogException(exception);
					return false;
				}
			}
		}

		public static DateTime? TryGetLastEditTime(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					return File.GetLastWriteTime(path);
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error accessing file for {path}: {arg}");
			}
			return null;
		}

		public static UniTask ConvertMissionToFolders()
		{
			MissionGroup.Init();
			return UniTask.RunOnThreadPool(delegate
			{
				Debug.Log("[SideThread] Running ConvertMissionToFolders");
				try
				{
					string userMissionDirectory = MissionGroup.UserGroup.UserMissionDirectory;
					if (Directory.Exists(userMissionDirectory))
					{
						string[] files = Directory.GetFiles(userMissionDirectory);
						foreach (string text in files)
						{
							string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
							string text2 = Path.Combine(userMissionDirectory, fileNameWithoutExtension);
							if (Directory.Exists(text2))
							{
								Debug.LogError("Failed to move mission file to folder because " + text2 + " already exists");
							}
							else
							{
								Directory.CreateDirectory(text2);
								File.Move(text, Path.Combine(text2, fileNameWithoutExtension + ".json"));
								ModLoader.WriteMetaData(text2, new MissionGroup.MissionMetaData
								{
									FileName = fileNameWithoutExtension
								});
								CreateWorkshopJson(fileNameWithoutExtension, text2);
							}
						}
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			});
		}

		[Obsolete("need Obsolete to use WorkshopId")]
		private static void CreateWorkshopJson(string name, string folder)
		{
			if (QuickLoadOne(new MissionKey(name, MissionGroup.User), out var mission) && mission.WorkshopId != 0L)
			{
				WorkshopJson.WriteFile(folder, new PublishedFileId_t(mission.WorkshopId), SubscribedItemType.Mission);
			}
		}

		public static MissionFileInfo GetFileInfo(MissionKey key)
		{
			using (getFileInfoMarker.Auto())
			{
				MissionFileInfo entry = new MissionFileInfo
				{
					key = key
				};
				if (key.Group == MissionGroup.User)
				{
					GetUserGroupFileInfo(ref entry);
				}
				return entry;
			}
		}

		private static void GetUserGroupFileInfo(ref MissionFileInfo entry)
		{
			try
			{
				string jsonPath = MissionGroup.UserGroup.GetJsonPath(entry.key.Key);
				entry.LastEdit = TryGetLastEditTime(jsonPath);
				entry.SubItems = GetSubItems(entry.key.Key);
				if (entry.SubItems == null)
				{
					return;
				}
				entry.ExpandedLastEdit = entry.LastEdit;
				foreach (MissionFileInfo subItem in entry.SubItems)
				{
					if (subItem.LastEdit.HasValue && (!entry.LastEdit.HasValue || subItem.LastEdit > entry.LastEdit))
					{
						entry.LastEdit = subItem.LastEdit;
					}
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error getting details for User mission {entry.key.Key}: {arg}");
			}
		}

		private static List<MissionFileInfo> GetSubItems(string missionName)
		{
			List<MissionFileInfo> list = null;
			string[] versions = EditorMissionGroup.GetVersions(missionName);
			foreach (string text in versions)
			{
				string versionJson = EditorMissionGroup.GetVersionJson(missionName, text);
				if (File.Exists(versionJson))
				{
					MissionFileInfo item = new MissionFileInfo
					{
						key = new MissionKey(text, missionName, null, MissionGroup.EditorMissions),
						LastEdit = TryGetLastEditTime(versionJson)
					};
					if (list == null)
					{
						list = new List<MissionFileInfo>();
					}
					list.Add(item);
				}
			}
			return list;
		}

		public static void TryLoadObjectiveGraphLayout(Mission missionToLoad)
		{
			try
			{
				if (missionToLoad.LoadKey.HasValue && TryGetObjectiveGraphLayoutPath(missionToLoad.LoadKey.Value, out var path))
				{
					GraphLayoutJson.TryLoadLayout(path, out missionToLoad.ObjectiveGraphLayout);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		public static void SaveObjectiveGraphLayout(Mission mission, MissionKey key)
		{
			if (mission.ObjectiveGraphLayout != null && TryGetObjectiveGraphLayoutPath(key, out var path))
			{
				mission.ObjectiveGraphLayout.SyncLayout?.Invoke();
				GraphLayoutJson.SaveToFile(mission.ObjectiveGraphLayout, path);
			}
		}

		public static bool TryGetObjectiveGraphLayoutPath(MissionKey key, out string path)
		{
			if (key.Group == MissionGroup.User)
			{
				path = Path.Combine(MissionGroup.UserGroup.GetFolder(key.Key), "objective-graph.layout.json");
				return true;
			}
			if (key.Group == MissionGroup.Temp)
			{
				path = Path.Combine(MissionGroup.TempGroup.GetFolder(key.Key), "objective-graph.layout.json");
				return true;
			}
			if (key.Group == MissionGroup.EditorMissions)
			{
				path = Path.Combine(EditorMissionGroup.GetVersionFolder(key), "objective-graph.layout.json");
				return true;
			}
			path = null;
			return false;
		}
	}
}
