using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.BuildScripts.DebugTests
{
	public class LeakTest
	{
		public bool running;

		private static List<string> logs;

		private static object logLock = new object();

		public static void SafeLog(string msg)
		{
			lock (logLock)
			{
				logs?.Add(msg);
			}
		}

		private void FlushLogs()
		{
			int count = logs.Count;
			for (int i = 0; i < count; i++)
			{
				string text = logs[i];
				Debug.Log("[SafeLog] " + text);
			}
			lock (logLock)
			{
				logs.RemoveRange(0, count);
			}
		}

		private LeakTest()
		{
		}

		public static void StartNew(ref LeakTest field)
		{
			field?.Stop();
			field = new LeakTest();
			field.RunLeakTest().Forget();
		}

		public void Stop()
		{
			running = false;
		}

		private async UniTask LogMemory()
		{
			while (running)
			{
				NetworkManagerNuclearOption.LogMonoHeap("MonoHeap: {0}");
				await UniTask.Delay(1000);
			}
		}

		private async UniTask LogLoop()
		{
			logs = new List<string>();
			while (running)
			{
				await UniTask.Yield();
				FlushLogs();
			}
			logs = null;
		}

		public async UniTask RunLeakTest()
		{
			running = true;
			LogMemory().Forget();
			LogLoop().Forget();
			while (NetworkManagerNuclearOption.i == null)
			{
				await UniTask.Yield();
			}
			await UniTask.Yield();
			if (running)
			{
				await RunLoop();
			}
		}

		private async UniTask RunLoop()
		{
			do
			{
				MissionSaveLoad.TryLoad(new MissionKey("Escalation", MissionGroup.BuiltIn), out var mission, out var _);
				MissionManager.SetMission(mission, checkIfSame: false);
				NetworkManagerNuclearOption.i.StartHost(new HostOptions(SocketType.Offline, GameState.SinglePlayer, mission.MapKey));
				await UniTask.Delay(20000);
				if (!running)
				{
					break;
				}
				NetworkManagerNuclearOption.i.Stop(setDisconnectReason: false);
				await UniTask.Delay(5000);
			}
			while (running);
		}
	}
}
