using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace NuclearOption.Debugging
{
	public static class PlayerLoopPerformanceTracker
	{
		private struct PerformanceTracker
		{
			private const float WARNING_THRESHOLD_MS = 100f;

			private const float LOG_COOLDOWN_SECONDS = 5f;

			private readonly string label;

			private int countSinceLastLog;

			private double lastLogTime;

			private float accumulatedMs;

			private long startTimestamp;

			public float LastElapsedMs { get; private set; }

			public float FrameAccumulatedMs { get; private set; }

			public PerformanceTracker(string label)
			{
				this.label = label;
				countSinceLastLog = 0;
				lastLogTime = -5.0;
				accumulatedMs = 0f;
				startTimestamp = 0L;
				LastElapsedMs = 0f;
				FrameAccumulatedMs = 0f;
			}

			public void Start()
			{
				startTimestamp = BenchmarkScope.GetTimestamp();
			}

			public void End()
			{
				Record((float)BenchmarkScope.MillisecondsSince(startTimestamp));
			}

			public void Record(float ms)
			{
				LastElapsedMs = ms;
				FrameAccumulatedMs += ms;
				if (!(Time.timeSinceLevelLoad < 5f) && !NetworkManagerNuclearOption.IsLoadingScene)
				{
					if (ms > 100f)
					{
						countSinceLastLog++;
						accumulatedMs += ms;
					}
					if (countSinceLastLog > 0 && Time.unscaledTimeAsDouble - lastLogTime > 5.0)
					{
						LogWarning(accumulatedMs);
					}
				}
			}

			public float FlushFrameTime()
			{
				float frameAccumulatedMs = FrameAccumulatedMs;
				FrameAccumulatedMs = 0f;
				return frameAccumulatedMs;
			}

			private void LogWarning(double ms)
			{
				if (countSinceLastLog > 1)
				{
					ColorLog<PerformanceTracker>.InfoWarn($"{label} took {ms:F1}ms over {countSinceLastLog} calls (Time since load: {Time.timeSinceLevelLoad:F1}s)");
				}
				else
				{
					ColorLog<PerformanceTracker>.InfoWarn($"{label} took {ms:F1}ms. (Time since load: {Time.timeSinceLevelLoad:F1}s)");
				}
				countSinceLastLog = 0;
				accumulatedMs = 0f;
				lastLogTime = Time.unscaledTimeAsDouble;
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct UpdateStart
		{
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct UpdateEnd
		{
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct FixedUpdateStart
		{
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct FixedUpdateEnd
		{
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct LateUpdateStart
		{
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct LateUpdateEnd
		{
		}

		private const float SCENE_LOAD_COOLDOWN_SECONDS = 5f;

		private static PerformanceTracker updateTracker = new PerformanceTracker("Update loop");

		private static PerformanceTracker fixedUpdateTracker = new PerformanceTracker("FixedUpdate loop");

		private static PerformanceTracker lateUpdateTracker = new PerformanceTracker("LateUpdate loop");

		private static PerformanceTracker receiveTracker = new PerformanceTracker("Receive loop");

		private static PerformanceTracker sendTracker = new PerformanceTracker("Send loop");

		private static readonly List<PlayerLoopSystem> tempList = new List<PlayerLoopSystem>();

		public static float UpdateTime => updateTracker.LastElapsedMs;

		public static float FixedUpdateTime => fixedUpdateTracker.LastElapsedMs;

		public static float LateUpdateTime => lateUpdateTracker.LastElapsedMs;

		public static float ReceiveTime => receiveTracker.LastElapsedMs;

		public static float SendTime => sendTracker.LastElapsedMs;

		public static event Action<FrameTiming> OnFrameFinished;

		public static void CheckSetup()
		{
			try
			{
				PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
				if (currentPlayerLoop.subSystemList == null || currentPlayerLoop.subSystemList.Length == 0)
				{
					ColorLog<PerformanceTracker>.LogError("PlayerLoop has no subsystems!");
					return;
				}
				bool flag = false;
				for (int i = 0; i < currentPlayerLoop.subSystemList.Length; i++)
				{
					PlayerLoopSystem playerLoopSystem = currentPlayerLoop.subSystemList[i];
					if (playerLoopSystem.type == typeof(Update))
					{
						currentPlayerLoop.subSystemList[i].subSystemList = PositionHooks<UpdateStart, UpdateEnd>(playerLoopSystem.subSystemList, OnUpdateStart, OnUpdateEnd);
						flag = true;
					}
					else if (playerLoopSystem.type == typeof(FixedUpdate))
					{
						currentPlayerLoop.subSystemList[i].subSystemList = PositionHooks<FixedUpdateStart, FixedUpdateEnd>(playerLoopSystem.subSystemList, OnFixedUpdateStart, OnFixedUpdateEnd);
						flag = true;
					}
					else if (playerLoopSystem.type == typeof(PreLateUpdate))
					{
						currentPlayerLoop.subSystemList[i].subSystemList = PositionHooks<LateUpdateStart, LateUpdateEnd>(playerLoopSystem.subSystemList, OnLateUpdateStart, OnLateUpdateEnd);
						flag = true;
					}
				}
				if (flag)
				{
					PlayerLoop.SetPlayerLoop(currentPlayerLoop);
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"Exception setting up performance loops: {arg}");
			}
		}

		private static PlayerLoopSystem[] PositionHooks<TStart, TEnd>(PlayerLoopSystem[] subSystems, PlayerLoopSystem.UpdateFunction startDelegate, PlayerLoopSystem.UpdateFunction endDelegate)
		{
			tempList.Clear();
			if (subSystems != null)
			{
				tempList.AddRange(subSystems);
			}
			int num = tempList.FindIndex((PlayerLoopSystem s) => s.type == typeof(TStart));
			switch (num)
			{
			case -1:
				tempList.Insert(0, new PlayerLoopSystem
				{
					type = typeof(TStart),
					updateDelegate = startDelegate
				});
				break;
			default:
			{
				PlayerLoopSystem item = tempList[num];
				tempList.RemoveAt(num);
				tempList.Insert(0, item);
				break;
			}
			case 0:
				break;
			}
			int num2 = tempList.FindIndex((PlayerLoopSystem s) => s.type == typeof(TEnd));
			if (num2 == -1)
			{
				tempList.Add(new PlayerLoopSystem
				{
					type = typeof(TEnd),
					updateDelegate = endDelegate
				});
			}
			else if (num2 != tempList.Count - 1)
			{
				PlayerLoopSystem item2 = tempList[num2];
				tempList.RemoveAt(num2);
				tempList.Add(item2);
			}
			PlayerLoopSystem[] result = tempList.ToArray();
			tempList.Clear();
			return result;
		}

		public static void StartReceive()
		{
			receiveTracker.Start();
		}

		public static void EndReceive()
		{
			receiveTracker.End();
		}

		public static void StartSend()
		{
			sendTracker.Start();
		}

		public static void EndSend()
		{
			sendTracker.End();
		}

		private static void OnUpdateStart()
		{
			updateTracker.Start();
		}

		private static void OnUpdateEnd()
		{
			updateTracker.End();
		}

		private static void OnFixedUpdateStart()
		{
			fixedUpdateTracker.Start();
		}

		private static void OnFixedUpdateEnd()
		{
			fixedUpdateTracker.End();
		}

		private static void OnLateUpdateStart()
		{
			lateUpdateTracker.Start();
		}

		private static void OnLateUpdateEnd()
		{
			lateUpdateTracker.End();
			if (PlayerLoopPerformanceTracker.OnFrameFinished != null)
			{
				PlayerLoopPerformanceTracker.OnFrameFinished(new FrameTiming
				{
					UpdateTime = updateTracker.FlushFrameTime(),
					FixedUpdateTime = fixedUpdateTracker.FlushFrameTime(),
					LateUpdateTime = lateUpdateTracker.FlushFrameTime(),
					ReceiveTime = receiveTracker.FlushFrameTime(),
					SendTime = sendTracker.FlushFrameTime()
				});
			}
		}
	}
}
