using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirage;
using Unity.Profiling;
using UnityEngine;

public static class SlowUpdateExtensions
{
	private class Runner : MonoBehaviour
	{
		private float now;

		private int count;

		private Call[] calls = new Call[128];

		private float[] nextInvokeTime = new float[128];

		private List<int> toCall = new List<int>();

		private List<int> toRemove = new List<int>();

		private void OnDestroy()
		{
			runner = null;
		}

		public void AddCall(Action action, float interval, float startDelay, CancellationToken cancellationToken)
		{
			if (startDelay > 0f)
			{
				float num = UnityEngine.Random.value * Mathf.Min(5f, interval);
				startDelay += num;
			}
			if (calls.Length < count + 1)
			{
				int newSize = calls.Length * 2;
				Array.Resize(ref calls, newSize);
				Array.Resize(ref nextInvokeTime, newSize);
			}
			calls[count] = new Call(action, interval, cancellationToken);
			nextInvokeTime[count] = now + startDelay;
			count++;
		}

		private void Update()
		{
			using (UpdateMarker.Auto())
			{
				now = Time.timeSinceLevelLoad;
				using (CheckMarker.Auto())
				{
					toCall.Clear();
					for (int i = 0; i < count; i++)
					{
						if (now > nextInvokeTime[i])
						{
							toCall.Add(i);
						}
					}
				}
				if (toCall.Count > 0)
				{
					using (InvokeMarker.Auto())
					{
						foreach (int item in toCall)
						{
							ref Call reference = ref calls[item];
							CancellationToken cancellation = reference.Cancellation;
							if (cancellation.IsCancellationRequested)
							{
								toRemove.Add(item);
							}
							else
							{
								Invoke(ref nextInvokeTime[item], ref reference);
							}
						}
					}
				}
				if (toRemove.Count > 0)
				{
					RemoveTimers();
				}
			}
		}

		private void RemoveTimers()
		{
			using (RemoveMarker.Auto())
			{
				foreach (int item in toRemove)
				{
					calls[item] = default(Call);
				}
				int num = toRemove[0];
				for (int i = num + 1; i < count; i++)
				{
					if (calls[i].Action != null)
					{
						calls[num] = calls[i];
						nextInvokeTime[num] = nextInvokeTime[i];
						num++;
					}
				}
				int num2 = count - toRemove.Count;
				if (num2 != num)
				{
					Debug.LogError($"Write Index should be the new count {num2} != {num}");
				}
				count = num;
				toRemove.Clear();
			}
		}

		private void Invoke(ref float nextInvoke, ref Call call)
		{
			try
			{
				call.Action();
			}
			catch (Exception ex)
			{
				Debug.LogError($"{ex.GetType().Name} in SlowUpdate: {ex}");
			}
			nextInvoke = now + call.Interval;
		}
	}

	private readonly struct Call
	{
		public readonly Action Action;

		public readonly float Interval;

		public readonly CancellationToken Cancellation;

		public Call(Action action, float delay, CancellationToken cancellationToken)
		{
			this = default(Call);
			Action = action;
			Interval = delay;
			Cancellation = cancellationToken;
		}
	}

	private static readonly ProfilerMarker slowUpdateMarker = new ProfilerMarker("SlowUpdate");

	private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("SlowUpdateRunner.Update");

	private static readonly ProfilerMarker CheckMarker = new ProfilerMarker("SlowUpdateRunner.Check");

	private static readonly ProfilerMarker InvokeMarker = new ProfilerMarker("SlowUpdateRunner.Invoke");

	private static readonly ProfilerMarker RemoveMarker = new ProfilerMarker("SlowUpdateRunner.Remove");

	private static Runner runner;

	private static ProfilerMarker CreateMarkerFromAction(Action action)
	{
		MethodInfo methodInfo = action.GetMethodInfo();
		if (methodInfo == null)
		{
			return new ProfilerMarker("NoMethodName");
		}
		Type declaringType = methodInfo.DeclaringType;
		if (declaringType == null)
		{
			return new ProfilerMarker(methodInfo.Name);
		}
		return new ProfilerMarker($"{declaringType}.{methodInfo.Name}");
	}

	public static void StartSlowUpdateDelayed(this MonoBehaviour behaviour, float startDelayAndInterval, Action update, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken == CancellationToken.None)
		{
			cancellationToken = behaviour.destroyCancellationToken;
		}
		AddCall(update, startDelayAndInterval, startDelayAndInterval, cancellationToken);
	}

	public static void StartSlowUpdateDelayed(this MonoBehaviour behaviour, float startDelay, float interval, Action update, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken == CancellationToken.None)
		{
			cancellationToken = behaviour.destroyCancellationToken;
		}
		AddCall(update, interval, startDelay, cancellationToken);
	}

	public static void StartSlowUpdate(this MonoBehaviour behaviour, float interval, Action update, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken == CancellationToken.None)
		{
			cancellationToken = behaviour.destroyCancellationToken;
		}
		AddCall(update, interval, 0f, cancellationToken);
	}

	public static async UniTask Loop(Action update, int delayMs, bool ignoreTimescale, CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Update)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				update();
			}
			catch (Exception ex)
			{
				Debug.LogError($"{ex.GetType().Name} in SlowUpdate: {ex}");
			}
			if (delayMs == 0)
			{
				await UniTask.Yield(timing);
			}
			else
			{
				await UniTask.Delay(delayMs, ignoreTimescale, timing);
			}
		}
	}

	public static CancellationToken GetCancellationTokenOnNetworkStop(this NetworkBehaviour behaviour)
	{
		NetworkIdentity identity = behaviour.Identity;
		CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(behaviour.destroyCancellationToken);
		identity.OnStopClient.AddListener(OnStop);
		identity.OnStopServer.AddListener(OnStop);
		return cts.Token;
		void OnStop()
		{
			cts.Cancel();
			identity.OnStopClient.RemoveListener(OnStop);
			identity.OnStopServer.RemoveListener(OnStop);
		}
	}

	public static void AddCall(Action action, float interval, float startDelay, CancellationToken cancellationToken)
	{
		if ((object)runner == null)
		{
			runner = new GameObject("SlowUpdateRunner").AddComponent<Runner>();
		}
		runner.AddCall(action, interval, startDelay, cancellationToken);
	}

	public static void DestroyRunner()
	{
		if (runner != null)
		{
			UnityEngine.Object.Destroy(runner.gameObject);
		}
		runner = null;
	}
}
