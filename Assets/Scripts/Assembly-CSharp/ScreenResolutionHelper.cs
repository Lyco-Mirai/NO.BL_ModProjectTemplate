using System.Collections.Generic;
using UnityEngine;

public class ScreenResolutionHelper
{
	public Resolution Current { get; private set; }

	public string CurrentString { get; private set; }

	public bool FullScreen { get; private set; }

	public List<Resolution> Options { get; }

	public List<string> OptionStrings { get; }

	public static Resolution Default_1080 => new Resolution
	{
		width = 1920,
		height = 1080,
		refreshRateRatio = new RefreshRate
		{
			numerator = 60u,
			denominator = 1u
		}
	};

	private static List<Resolution> GetSupportedResolutions()
	{
		List<Resolution> list = new List<Resolution>();
		Resolution[] resolutions = Screen.resolutions;
		foreach (Resolution resolution in resolutions)
		{
			if (ShouldSupport(resolution) && !AlreadyInList(list, resolution))
			{
				list.Add(resolution);
			}
		}
		return list;
		static bool AlreadyInList(List<Resolution> supported, Resolution value)
		{
			for (int j = 0; j < supported.Count; j++)
			{
				Resolution resolution2 = supported[j];
				if (resolution2.width == value.width && resolution2.height == value.height)
				{
					if (resolution2.refreshRateRatio.value < value.refreshRateRatio.value)
					{
						supported[j] = value;
					}
					return true;
				}
			}
			return false;
		}
		static bool ShouldSupport(Resolution resolution2)
		{
			if (resolution2.height < 720)
			{
				return false;
			}
			float num = (float)resolution2.width / (float)resolution2.height;
			if (1.49f <= num)
			{
				return num <= 6f;
			}
			return false;
		}
	}

	public ScreenResolutionHelper()
	{
		Options = GetSupportedResolutions();
		OptionStrings = new List<string>(Options.Count);
		ColorLog<Resolution>.Info($"Supported Resolutions, count={Options.Count}");
		foreach (Resolution option in Options)
		{
			string resolutionString = GetResolutionString(option);
			ColorLog<Resolution>.Info(" - " + resolutionString);
			OptionStrings.Add(resolutionString);
		}
		Current = Screen.currentResolution;
		CurrentString = GetResolutionString(Current);
		FullScreen = Screen.fullScreen;
	}

	public void LoadPrefs()
	{
		if (PlayerPrefs.HasKey("ScreenResolution"))
		{
			string resolutionStr = PlayerPrefs.GetString("ScreenResolution");
			bool fullScreen = PlayerPrefs.GetInt("FullScreen", 1) == 1;
			SetValues(resolutionStr, fullScreen);
		}
	}

	private void SavePrefs()
	{
		PlayerPrefs.SetString("ScreenResolution", CurrentString);
		PlayerPrefs.SetInt("FullScreen", FullScreen ? 1 : 0);
	}

	public void SetValues(string resolutionStr, bool fullScreen)
	{
		Current = FindResolution(ref resolutionStr);
		CurrentString = resolutionStr;
		FullScreen = fullScreen;
		Screen.SetResolution(Current.width, Current.height, FullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, Current.refreshRateRatio);
		Debug.Log($"Resoultion '{Current}', fullscreen: '{FullScreen}'");
		SavePrefs();
	}

	public Resolution FindResolution(ref string resolutionStr)
	{
		for (int i = 0; i < OptionStrings.Count; i++)
		{
			if (OptionStrings[i] == resolutionStr)
			{
				return Options[i];
			}
		}
		Debug.LogError("Could not find available " + resolutionStr + " in resolution list, Keeping current resolution: " + GetResolutionString(Screen.currentResolution));
		Resolution currentResolution = Screen.currentResolution;
		resolutionStr = GetResolutionString(currentResolution);
		return currentResolution;
	}

	public static string GetResolutionString(Resolution resolution)
	{
		return $"{resolution.width}x{resolution.height}";
	}
}
