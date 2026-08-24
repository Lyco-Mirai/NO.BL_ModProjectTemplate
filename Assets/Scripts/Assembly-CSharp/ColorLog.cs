using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.Networking;
using UnityEngine;

public class ColorLog
{
	private const int ColorSeed = 403;

	private const float ColorSaturation = 0.6f;

	private const float ColorValue = 0.8f;

	protected ColorLog()
	{
	}

	public static Color ColorFromName(string fullName)
	{
		return ColorFromName(fullName, 0.6f, 0.8f);
	}

	public static Color ColorFromName(string fullName, float saturation, float value)
	{
		if (string.IsNullOrEmpty(fullName))
		{
			return Color.white;
		}
		int stableHashCode = fullName.GetStableHashCode();
		stableHashCode = 403 * stableHashCode;
		return Color.HSVToRGB(Mathf.Abs((float)stableHashCode / 2.1474836E+09f), saturation, value);
	}

	public static string CreatePrefixForType<T>(bool addColor)
	{
		return CreatePrefixForType(typeof(T), addColor);
	}

	public static string CreatePrefixForType(Type type, bool addColor)
	{
		string name = type.Name;
		if (addColor)
		{
			string text = ColorUtility.ToHtmlStringRGB(ColorFromName(type.FullName));
			return "<color=#" + text + ">[" + name + "]</color>";
		}
		return "[" + name + "]";
	}

	[Conditional("UNITY_ASSERTIONS")]
	public static void AssertDestroyedAfter(MonoBehaviour self, float delaySeconds = 1f)
	{
		UniTask.Void(async delegate
		{
			await UniTask.Delay((int)(delaySeconds * 1000f), ignoreTimeScale: true);
			if (self != null)
			{
				string arg = CreatePrefixForType(self.GetType(), addColor: true);
				UnityEngine.Debug.LogError($"{LogTimeUpdater.unscaledTime:0.000}: {arg} [Assert] was not destroyed");
			}
		});
	}

	[Conditional("UNITY_ASSERTIONS")]
	public static void AssertIsServer(string message = null)
	{
		if (!NetworkManagerNuclearOption.i.Server.Active)
		{
			if (message == null)
			{
				message = "";
			}
			UnityEngine.Debug.LogError($"{LogTimeUpdater.unscaledTime:0.000}: [Assert] Not server. {message}");
		}
	}

	[Conditional("UNITY_ASSERTIONS")]
	public static void AssertAreClose(float a, float b, string message = null)
	{
		if (Math.Abs(a - b) > 0.01f)
		{
			if (message == null)
			{
				message = "";
			}
			UnityEngine.Debug.LogError($"{LogTimeUpdater.unscaledTime:0.000}: [Assert] {a} and {b} are not close {message}");
		}
	}
}
public class ColorLog<T> : ColorLog
{
	private static string prefix;

	private ColorLog()
	{
	}

	static ColorLog()
	{
		prefix = ColorLog.CreatePrefixForType<T>(Application.isEditor);
	}

	[Conditional("UNITY_EDITOR")]
	public static void Trace(string extra = null)
	{
		Console.WriteLine(string.Format("{0:0.000}: {1}{2} TraceStack:{3}", LogTimeUpdater.unscaledTime, prefix, (extra == null) ? "" : (" " + extra), Environment.StackTrace));
	}

	[Conditional("UNITY_EDITOR")]
	public static void Log(string message)
	{
		UnityEngine.Debug.Log($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} {message}");
	}

	[Conditional("UNITY_EDITOR")]
	public static void LogWarning(string message)
	{
		UnityEngine.Debug.LogWarning($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} [Warn] {message}");
	}

	public static void LogError(string message)
	{
		UnityEngine.Debug.LogError($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} [Error] {message}");
	}

	[Conditional("UNITY_EDITOR")]
	public static void EditorAssert(bool condition, string message)
	{
		if (!condition)
		{
			UnityEngine.Debug.LogError($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} [Assert] {message}");
		}
	}

	public static void Info(string message)
	{
		UnityEngine.Debug.Log($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} {message}");
	}

	public static void InfoWarn(string message)
	{
		UnityEngine.Debug.LogWarning($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} [Warn] {message}");
	}

	public static void InfoAssert(bool condition, string message)
	{
		if (!condition)
		{
			UnityEngine.Debug.LogError($"{LogTimeUpdater.unscaledTime:0.000}: {prefix} [Assert] {message}");
		}
	}
}
