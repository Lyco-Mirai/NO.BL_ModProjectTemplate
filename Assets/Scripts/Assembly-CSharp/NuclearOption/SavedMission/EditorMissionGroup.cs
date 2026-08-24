using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.MissionEditorScripts;
using NuclearOption.NodeGraph;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public class EditorMissionGroup : MissionGroup
	{
		public enum AutoSaveType
		{
			Timed = 0,
			OnPlay = 1,
			OnExit = 2
		}

		private static readonly ProfilerMarker shouldAutoSaveMarker = new ProfilerMarker("EditorMissionGroup.ShouldAutoSave");

		private static readonly ProfilerMarker autoSaveWriteMarker = new ProfilerMarker("EditorMissionGroup.AutoSaveWrite");

		private const string AUTO_SAVE_PREFIX = "AutoSave";

		private static readonly int AUTO_SAVE_PREFIX_LENGTH = "00_AutoSave".Length;

		public static string MissionEditorDirectory;

		public static Dictionary<string, int> LatestVersionHash = new Dictionary<string, int>();

		[RuntimeInitializeOnLoadMethod]
		private static void SetDefaultDirectory()
		{
			MissionEditorDirectory = Path.Combine(Application.persistentDataPath, "MissionEditor");
		}

		public EditorMissionGroup()
			: base("EditorGroup")
		{
		}

		public override bool ContainsMission(string key)
		{
			throw new NotSupportedException();
		}

		public override IEnumerable<MissionKey> GetMissions()
		{
			throw new NotSupportedException();
		}

		public override UniTask<Sprite> GetPreview(string key, CancellationToken token = default(CancellationToken))
		{
			return UniTask.FromResult<Sprite>(null);
		}

		public override bool TryGetFilePath(MissionKey key, out string path)
		{
			path = GetVersionJson(key);
			return !string.IsNullOrEmpty(path);
		}

		public override bool TryGetJson(MissionKey key, out string json)
		{
			string versionJson = GetVersionJson(key);
			json = string.Empty;
			if (!File.Exists(versionJson))
			{
				ColorLog<MissionGroup>.Info("No file at path: " + versionJson);
				return false;
			}
			using (FileUtils.ReadAllTextMarker.Auto())
			{
				json = File.ReadAllText(versionJson);
			}
			return true;
		}

		public static MissionKey CreateUserKey(MissionKey key)
		{
			string name = key.Name;
			ColorLog<MissionKey>.Info("Using '" + name + "' for key " + key.Key);
			return new MissionKey(name, MissionGroup.User);
		}

		public static string[] GetVersions(string missionName)
		{
			string editorFolder = GetEditorFolder(missionName);
			if (!Directory.Exists(editorFolder))
			{
				return Array.Empty<string>();
			}
			string[] directories = Directory.GetDirectories(editorFolder);
			string[] array = new string[directories.Length];
			for (int i = 0; i < directories.Length; i++)
			{
				array[i] = Path.GetFileName(directories[i]);
			}
			return array;
		}

		public static MissionKey SaveVersion(string missionName, Mission mission, AutoSaveType autoSaveType)
		{
			return SaveVersion(missionName, mission, autoSaveType switch
			{
				AutoSaveType.Timed => "00_AutoSave_Timed", 
				AutoSaveType.OnPlay => "00_AutoSave_OnPlay", 
				AutoSaveType.OnExit => "00_AutoSave_OnExit", 
				_ => "00_AutoSave", 
			});
		}

		public static MissionKey SaveVersion(string missionName, Mission mission, string versionName)
		{
			string versionJson = GetVersionJson(missionName, versionName);
			FileUtils.CheckDirectoryExists(GetVersionFolder(missionName, versionName));
			string contents = NewtonsoftHelper.ToJson(mission, prettyPrint: true);
			ColorLog<EditorMissionGroup>.Info("Saving mission '" + missionName + "' version '" + versionName + "' to " + versionJson);
			File.WriteAllText(versionJson, contents);
			MissionKey missionKey = new MissionKey(versionName, missionName, null, MissionGroup.EditorMissions);
			MissionSaveLoad.SaveObjectiveGraphLayout(mission, missionKey);
			return missionKey;
		}

		public static void DeleteVersion(MissionKey key)
		{
			FileUtils.DeleteDirectory(GetVersionFolder(key));
		}

		public static bool MoveEditorFolder(string oldName, string newName, out string error)
		{
			string editorFolder = GetEditorFolder(oldName);
			string editorFolder2 = GetEditorFolder(newName);
			if (Directory.Exists(editorFolder2))
			{
				error = "Failed to move " + oldName + " because " + newName + " was not deleted";
				return false;
			}
			if (Directory.Exists(editorFolder))
			{
				ColorLog<EditorMissionGroup>.Info("Moving folder from " + editorFolder + " to " + editorFolder2);
				Directory.Move(editorFolder, editorFolder2);
			}
			error = null;
			return true;
		}

		public static void DeleteEditorFolder(string missionName)
		{
			FileUtils.DeleteDirectory(GetEditorFolder(missionName));
		}

		public static string GetEditorFolder(string missionName)
		{
			return Path.Combine(MissionEditorDirectory, missionName);
		}

		public static string GetVersionFolder(MissionKey key)
		{
			if (key.Group != MissionGroup.EditorMissions)
			{
				throw new InvalidOperationException("Key group was not EditorMissions");
			}
			return GetVersionFolder(key.Name, key.Key);
		}

		public static string GetVersionFolder(string missionName, string versionName)
		{
			return Path.Combine(GetEditorFolder(missionName), versionName);
		}

		public static string GetVersionJson(MissionKey key)
		{
			if (key.Group != MissionGroup.EditorMissions)
			{
				throw new InvalidOperationException("Key group was not EditorMissions");
			}
			return GetVersionJson(key.Name, key.Key);
		}

		public static string GetVersionJson(string missionName, string versionName)
		{
			return Path.Combine(GetVersionFolder(missionName, versionName), "mission.json");
		}

		public static UniTask CleanUpOldAutoSavesSideThread()
		{
			MissionAutoSaveSettings settings = MissionAutoSaveSettings.GetOrLoad();
			MissionGroup.Init();
			return UniTask.RunOnThreadPool(delegate
			{
				Debug.Log("[SideThread] Running CleanUpOldAutoSavesSideThread");
				try
				{
					CleanUpOldAutoSaves(settings.RetentionDays);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			});
		}

		public static void CleanUpOldAutoSaves(int retentionDays)
		{
			string missionEditorDirectory = MissionEditorDirectory;
			if (!Directory.Exists(missionEditorDirectory))
			{
				return;
			}
			DateTime dateTime = DateTime.Now.AddDays(-retentionDays);
			string[] directories = Directory.GetDirectories(missionEditorDirectory);
			for (int i = 0; i < directories.Length; i++)
			{
				string fileName = Path.GetFileName(directories[i]);
				string[] versions = GetVersions(fileName);
				foreach (string text in versions)
				{
					if (!IsAutoSave(text))
					{
						continue;
					}
					DateTime? dateTime2 = MissionSaveLoad.TryGetLastEditTime(GetVersionJson(fileName, text));
					if (dateTime2.HasValue && dateTime2.Value < dateTime)
					{
						string versionFolder = GetVersionFolder(fileName, text);
						try
						{
							FileUtils.DeleteDirectory(versionFolder);
						}
						catch (Exception arg)
						{
							Debug.LogError($"Failed to delete '{fileName}' - '{text}' because of {arg}");
						}
					}
				}
			}
		}

		public static bool AutoSave(MissionAutoSaveSettings settings, Mission mission, AutoSaveType autoSaveType, out MissionKey newKey)
		{
			newKey = default(MissionKey);
			if (string.IsNullOrEmpty(mission.Name))
			{
				ColorLog<EditorMissionGroup>.LogError("Mission has no name so can not be auto saved");
				return false;
			}
			if (!ShouldAutoSave(mission, out var missionName, out var hash))
			{
				return false;
			}
			using (BenchmarkScope.Create("AutoSaveWrite"))
			{
				using (autoSaveWriteMarker.Auto())
				{
					DeleteAndMoveOldAutoSaves(missionName, settings.MaxAutoSaves - 1);
					newKey = SaveVersion(missionName, mission, autoSaveType);
					LatestVersionHash[missionName] = hash;
					MissionSaveLoad.TriggerAfterSave(newKey);
					return true;
				}
			}
		}

		public static bool ShouldAutoSave(Mission mission, out string missionName, out int hash)
		{
			using (BenchmarkScope.Create("ShouldAutoSave"))
			{
				using (shouldAutoSaveMarker.Auto())
				{
					MissionKey orCreateUserSaveKey = MissionSaveLoad.GetOrCreateUserSaveKey(mission);
					missionName = orCreateUserSaveKey.Name;
					int newestAutoSaveHash = GetNewestAutoSaveHash(orCreateUserSaveKey, includeVersions: true);
					hash = GetCombinedHash(mission);
					if (hash == newestAutoSaveHash)
					{
						ColorLog<EditorMissionGroup>.Info($"Skipping auto save because combined mission and layout hash ({hash}) is the same as newest save");
						return false;
					}
					ColorLog<EditorMissionGroup>.Info($"Auto save {missionName} with combined hash ({hash})");
					return true;
				}
			}
		}

		public static bool MainSaveHasChanged(Mission mission)
		{
			using (BenchmarkScope.Create("ShouldAutoSave"))
			{
				using (shouldAutoSaveMarker.Auto())
				{
					MissionKey? loadKey = mission.LoadKey;
					if (!loadKey.HasValue)
					{
						return true;
					}
					MissionKey value = loadKey.Value;
					int combinedHash = GetCombinedHash(mission);
					if (!value.IsUserOrEditorGroup())
					{
						int newestAutoSaveHash = GetNewestAutoSaveHash(value, includeVersions: false);
						if (combinedHash == newestAutoSaveHash)
						{
							return false;
						}
					}
					int newestAutoSaveHash2 = GetNewestAutoSaveHash(MissionSaveLoad.GetOrCreateUserSaveKey(mission), includeVersions: false);
					if (combinedHash == newestAutoSaveHash2)
					{
						return false;
					}
					return true;
				}
			}
		}

		private static int SafeHash(string rawJson, MissionKey key)
		{
			if (MissionSaveLoad.TryReadJson(key, rawJson, out var mission, out var _))
			{
				return SafeHash(mission, runBeforeSave: false);
			}
			return 0;
		}

		private static int SafeHash(Mission mission, bool runBeforeSave = true)
		{
			if (runBeforeSave)
			{
				mission.BeforeSave();
			}
			return NewtonsoftHelper.ToJson(mission).GetStableHashCode();
		}

		public static int GetNewestAutoSaveHash(MissionKey missionKey, bool includeVersions)
		{
			if (includeVersions && missionKey.IsUserOrEditorGroup())
			{
				if (!LatestVersionHash.TryGetValue(missionKey.Name, out var value))
				{
					value = GetNewestAutoSaveHashInner(missionKey, includeVersions: true);
					if (value != 0)
					{
						LatestVersionHash[missionKey.Name] = value;
					}
				}
				return value;
			}
			return GetNewestAutoSaveHashInner(missionKey, includeVersions: false);
		}

		private static int GetNewestAutoSaveHashInner(MissionKey missionKey, bool includeVersions)
		{
			return GetDiskCombinedHash(includeVersions ? GetNewestKey(missionKey) : missionKey);
		}

		private static int GetDiskCombinedHash(MissionKey key)
		{
			int value = 0;
			if (key.TryGetJson(out var json))
			{
				value = SafeHash(json, key);
			}
			int value2 = 0;
			if (MissionSaveLoad.TryGetObjectiveGraphLayoutPath(key, out var path) && File.Exists(path))
			{
				try
				{
					value2 = File.ReadAllText(path).GetStableHashCode();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			return HashCode.Combine(value, value2);
		}

		private static int GetCombinedHash(Mission mission, bool runBeforeSave = true)
		{
			int value = SafeHash(mission, runBeforeSave);
			mission.ObjectiveGraphLayout?.SyncLayout?.Invoke();
			string json;
			int value2 = ((mission.ObjectiveGraphLayout != null) ? GraphLayoutJson.GetJsonHash(mission.ObjectiveGraphLayout, out json) : 0);
			return HashCode.Combine(value, value2);
		}

		private static MissionKey GetNewestKey(MissionKey missionKey)
		{
			MissionFileInfo fileInfo = MissionSaveLoad.GetFileInfo(new MissionKey(missionKey.Name, MissionGroup.User));
			if (fileInfo.SubItems == null)
			{
				return fileInfo.key;
			}
			DateTime? dateTime = fileInfo.ExpandedLastEdit;
			MissionFileInfo missionFileInfo = fileInfo;
			for (int i = 0; i < fileInfo.SubItems.Count; i++)
			{
				MissionFileInfo missionFileInfo2 = fileInfo.SubItems[i];
				if (missionFileInfo2.LastEdit.HasValue && (!dateTime.HasValue || missionFileInfo2.LastEdit > dateTime))
				{
					dateTime = missionFileInfo2.LastEdit;
					missionFileInfo = missionFileInfo2;
				}
			}
			return missionFileInfo.key;
		}

		public static bool IsAutoSave(string versionName)
		{
			if (string.IsNullOrEmpty(versionName) || versionName.Length < AUTO_SAVE_PREFIX_LENGTH)
			{
				return false;
			}
			ReadOnlySpan<char> readOnlySpan = versionName.AsSpan();
			if (!char.IsDigit(readOnlySpan[0]) || !char.IsDigit(readOnlySpan[1]))
			{
				return false;
			}
			return readOnlySpan.Slice(3, "AutoSave".Length).Equals("AutoSave".AsSpan(), StringComparison.OrdinalIgnoreCase);
		}

		public static void DeleteAndMoveOldAutoSaves(string missionName, int savesToKeep)
		{
			if (savesToKeep <= 0)
			{
				throw new ArgumentException("savesToKeep must be greater than 0");
			}
			if (savesToKeep > 100)
			{
				throw new ArgumentException("savesToKeep must be less than or equal to 100");
			}
			MissionFileInfo fileInfo = MissionSaveLoad.GetFileInfo(new MissionKey(missionName, MissionGroup.User));
			if (fileInfo.SubItems == null)
			{
				return;
			}
			List<MissionFileInfo> list = new List<MissionFileInfo>();
			foreach (MissionFileInfo subItem in fileInfo.SubItems)
			{
				if (IsAutoSave(subItem.key.Key))
				{
					list.Add(subItem);
				}
			}
			DeleteAndMoveOldAutoSaves(list, savesToKeep);
		}

		private static void DeleteAndMoveOldAutoSaves(List<MissionFileInfo> autoSaves, int savesToKeep)
		{
			autoSaves.Sort((MissionFileInfo a, MissionFileInfo b) => b.LastEdit.GetValueOrDefault().CompareTo(a.LastEdit.GetValueOrDefault()));
			while (autoSaves.Count > savesToKeep)
			{
				DeleteVersion(autoSaves[autoSaves.Count - 1].key);
				autoSaves.RemoveAt(autoSaves.Count - 1);
			}
			for (int num = autoSaves.Count - 1; num >= 0; num--)
			{
				string versionFolder = GetVersionFolder(autoSaves[num].key);
				string key = autoSaves[num].key.Key;
				string editorFolder = GetEditorFolder(autoSaves[num].key.Name);
				string arg = key.Substring(3);
				string path = $"{num + 1:D2}_{arg}";
				string pathNew = Path.Combine(editorFolder, path);
				FileUtils.MoveDirectory(versionFolder, pathNew);
			}
		}
	}
}
