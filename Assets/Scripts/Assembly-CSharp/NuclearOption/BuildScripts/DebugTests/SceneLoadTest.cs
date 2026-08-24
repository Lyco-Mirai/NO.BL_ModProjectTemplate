using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NuclearOption.BuildScripts.DebugTests
{
	public class SceneLoadTest
	{
		private class RunChecker : MonoBehaviour
		{
			private void Awake()
			{
				IsRunning = true;
			}

			private void OnDestroy()
			{
				IsRunning = false;
			}
		}

		[Serializable]
		public class StopRunException : Exception
		{
		}

		private static bool IsRunning;

		private const int DELAY = 500;

		private const string MISSION_MAP_0 = "custom_airbase";

		private const string MISSION_MAP_1 = "NavalMap";

		private static void Log(string message)
		{
			Debug.Log("<color=red>" + new string('-', message.Length) + "</color>");
			Debug.Log("<color=red>[SceneLoadTest] " + message + "</color>");
			Debug.Log("<color=red>" + new string('-', message.Length) + "</color>");
		}

		private static void LogWarning(string message)
		{
			Debug.LogWarning("<color=red>" + new string('-', message.Length) + "</color>");
			Debug.LogWarning("<color=red>[SceneLoadTest] " + message + "</color>");
			Debug.LogWarning("<color=red>" + new string('-', message.Length) + "</color>");
		}

		private static void LogError(string message)
		{
			Debug.LogError("<color=red>" + new string('-', message.Length) + "</color>");
			Debug.LogError("<color=red>[SceneLoadTest] " + message + "</color>");
			Debug.LogError("<color=red>" + new string('-', message.Length) + "</color>");
		}

		private async UniTask Wait()
		{
			await UniTask.Delay(500, ignoreTimeScale: true);
			AssertNoNullUnits();
			if (!IsRunning)
			{
				throw new StopRunException();
			}
		}

		public async UniTask Run()
		{
			try
			{
				if (IsRunning)
				{
					LogWarning("Already running");
					return;
				}
				GameObject gameObject = new GameObject("RunChecker");
				gameObject.AddComponent<RunChecker>();
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				await RunInner();
			}
			catch (StopRunException)
			{
			}
		}

		public async UniTask RunInner()
		{
			await Wait();
			MapLoader mapLoader = Resources.FindObjectsOfTypeAll<MapLoader>().First();
			Log("New map 0 from main menu");
			await EditorNewMission(mapLoader.Maps[0]);
			await Wait();
			await ExitEditor();
			Log("New map 1 from main menu");
			await EditorNewMission(mapLoader.Maps[1]);
			await Wait();
			Log("New map 1->1 from Editor");
			await EditorNewMission(mapLoader.Maps[1]);
			await Wait();
			Log("New map 1->0 from Editor");
			await EditorNewMission(mapLoader.Maps[0]);
			await Wait();
			await ExitEditor();
			Log("Load map 0 from Menu");
			await EditorLoadMission("custom_airbase");
			await Wait();
			await ExitEditor();
			Log("Load map 1 from Menu");
			await EditorLoadMission("NavalMap");
			await Wait();
			await ExitEditor();
			Log("Reload same mission map 0 START");
			await EditorLoadMission("custom_airbase");
			await Wait();
			Log("Reload same mission map 0 SECOND LOAD");
			await EditorLoadMission("custom_airbase");
			await Wait();
			Log("Load map 1 from Editor");
			await EditorLoadMission("NavalMap");
			await Wait();
			Log("Load map 0 from Editor");
			await EditorLoadMission("custom_airbase");
			await Wait();
			await ExitEditor();
			Log("End");
		}

		private void AssertNoNullUnits()
		{
			foreach (Unit allUnit in UnitRegistry.allUnits)
			{
				if (allUnit == null)
				{
					LogError("Null unit found in UnitRegistry");
				}
			}
		}

		private static async UniTask ExitEditor()
		{
			Log("ExitEditor Start");
			await MissionEditor.ExitEditor();
			while (!(SceneManager.GetActiveScene().path == MapLoader.MainMenu))
			{
				Log("ExitEditor Yield");
				await UniTask.Yield();
			}
			await UniTask.Yield();
		}

		private static async UniTask EditorNewMission(MapDetails mapDetails)
		{
			NewMissionConfig config = NewMissionConfig.DefaultMission();
			config.Map = MapKey.GameWorldPrefab(mapDetails.PrefabName);
			await MissionEditor.LoadEditor(config);
		}

		private static async UniTask EditorLoadMission(string mapName)
		{
			if (MissionSaveLoad.TryLoad(new MissionKey(mapName, MissionGroup.User), out var mission, out var error))
			{
				await MissionEditor.LoadEditor(mission);
			}
			else
			{
				Debug.LogError(error);
			}
		}
	}
}
