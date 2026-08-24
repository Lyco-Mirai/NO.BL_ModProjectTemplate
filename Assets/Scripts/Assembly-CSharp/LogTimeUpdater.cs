using Cysharp.Threading.Tasks;
using UnityEngine;

public static class LogTimeUpdater
{
	public static float unscaledTime;

	public static bool IsRunning;

	public static async UniTask RunForever()
	{
		if (IsRunning)
		{
			Debug.LogError("LogTimeUpdater running");
			return;
		}
		IsRunning = true;
		try
		{
			while (true)
			{
				unscaledTime = Time.unscaledTime;
				await UniTask.Yield(PlayerLoopTiming.EarlyUpdate);
			}
		}
		finally
		{
			IsRunning = false;
		}
	}
}
