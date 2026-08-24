using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.MissionEditorScripts;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.BuildScripts.DebugTests
{
	public class SaveLoadTests
	{
		public enum SaveLoadMode
		{
			ReloadEditor = 0,
			FullLoadMission = 1,
			Fast = 2
		}

		public static async UniTask RunAsync(SaveLoadMode mode)
		{
			await MainMenu.WaitForLoaded(CancellationToken.None);
			try
			{
				foreach (MissionKey mission in MissionGroup.BuiltIn.GetMissions())
				{
					if (!Application.isPlaying)
					{
						return;
					}
					await TestSingleMission(mission, mode);
				}
			}
			finally
			{
				if (mode == SaveLoadMode.FullLoadMission && GameManager.gameState == GameState.Editor)
				{
					await MissionEditor.ExitEditor();
					await UniTask.Yield();
				}
			}
		}

		private static async UniTask TestSingleMission(MissionKey key, SaveLoadMode mode)
		{
			if (!key.TryLoad(out var mission, out var error))
			{
				ColorLog<SaveLoadTests>.LogError($"Failed to load Json for {key}, with errors:{error}");
				return;
			}
			string json = "";
			Action inner = delegate
			{
				mission.BeforeSave();
				json = NewtonsoftHelper.ToJson(mission, prettyPrint: true);
			};
			if (await TestLoad(mission, inner, mode))
			{
				if (!MissionSaveLoad.TryReadJson(new MissionKey(mission.Name, MissionGroup.User), json, out var mission2, out var error2))
				{
					ColorLog<SaveLoadTests>.LogError($"Failed to load json after save {key}, with errors:{error2}");
				}
				else
				{
					await TestLoad(mission2, null, mode);
				}
			}
		}

		private static async UniTask<bool> TestLoad(Mission mission, Action inner, SaveLoadMode mode)
		{
			if (mode == SaveLoadMode.ReloadEditor || mode == SaveLoadMode.FullLoadMission)
			{
				await MissionEditor.LoadEditor(mission);
			}
			await UniTask.Yield();
			if (!Application.isPlaying)
			{
				return false;
			}
			if (mode != SaveLoadMode.Fast && mission.LoadErrors != null && mission.LoadErrors.AnyMessages())
			{
				ColorLog<SaveLoadTests>.LogError("Mission load had errors: " + mission.LoadErrors.CreateDetailedList(mission.Name));
				if (mode == SaveLoadMode.ReloadEditor)
				{
					await MissionEditor.ExitEditor();
				}
				await UniTask.Yield();
				return false;
			}
			inner?.Invoke();
			if (mode == SaveLoadMode.ReloadEditor)
			{
				await MissionEditor.ExitEditor();
			}
			await UniTask.Yield();
			return Application.isPlaying;
		}
	}
}
