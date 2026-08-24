using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NuclearOption.UIStyleSystem
{
	public static class ThemeManager
	{
		public enum ThemeContext
		{
			Menu = 0,
			TacScreen = 1,
			HUD = 2
		}

		private static readonly Dictionary<string, ThemeGroup> themeGroups = new Dictionary<string, ThemeGroup>();

		public static readonly Dictionary<ThemeContext, Dictionary<string, StyleLabel>> StyleLabels = new Dictionary<ThemeContext, Dictionary<string, StyleLabel>>();

		public static ThemeGroup Active { get; private set; }

		public static ThemeGroup Vanilla => themeGroups["vanilla"];

		public static event Action ThemeGroupChanged;

		private static void LoadAllThemeGroups()
		{
			ThemeGroup[] array = Resources.LoadAll<ThemeGroup>("StyleSystem/ThemeGroups");
			foreach (ThemeGroup themeGroup in array)
			{
				themeGroups[themeGroup.Id] = themeGroup;
			}
			LoadThemeGroupsFromJson();
		}

		private static void LoadAllStyleLabels()
		{
			StyleLabels[ThemeContext.HUD] = new Dictionary<string, StyleLabel>();
			StyleLabels[ThemeContext.Menu] = new Dictionary<string, StyleLabel>();
			StyleLabels[ThemeContext.TacScreen] = new Dictionary<string, StyleLabel>();
			StyleLabel[] array = Resources.LoadAll<StyleLabel>("StyleSystem/Labels/HUD");
			foreach (StyleLabel styleLabel in array)
			{
				StyleLabels[ThemeContext.HUD].Add(styleLabel.name, styleLabel);
			}
			array = Resources.LoadAll<StyleLabel>("StyleSystem/Labels/Menu");
			foreach (StyleLabel styleLabel2 in array)
			{
				StyleLabels[ThemeContext.Menu].Add(styleLabel2.name, styleLabel2);
			}
			array = Resources.LoadAll<StyleLabel>("StyleSystem/Labels/TacScreen");
			foreach (StyleLabel styleLabel3 in array)
			{
				StyleLabels[ThemeContext.TacScreen].Add(styleLabel3.name, styleLabel3);
			}
		}

		private static void LoadThemeGroupsFromJson()
		{
			string path = Application.persistentDataPath + "/themes/";
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] directories = Directory.GetDirectories(path);
			foreach (string text in directories)
			{
				Debug.Log("Loading theme group from directory: " + text);
				string fileName = Path.GetFileName(text);
				string text2 = Path.Combine(text, "COLOR.json");
				string text3 = Path.Combine(text, "HUD.json");
				string text4 = Path.Combine(text, "TAC.json");
				string text5 = Path.Combine(text, "MENU.json");
				if (!File.Exists(text2) || !File.Exists(text3) || !File.Exists(text4) || !File.Exists(text5))
				{
					Debug.LogWarning("ThemeManager: skipping '" + fileName + "', one or more JSON files are missing in " + text);
					continue;
				}
				ColorTheme colorTheme = ColorTheme.LoadFromJson(text2);
				Theme newHudTheme = Theme.LoadFromJson(text3, ThemeContext.HUD);
				Theme newTacScreenTheme = Theme.LoadFromJson(text4, ThemeContext.TacScreen);
				Theme newMenuTheme = Theme.LoadFromJson(text5, ThemeContext.Menu);
				ThemeGroup themeGroup = ScriptableObject.CreateInstance<ThemeGroup>();
				themeGroup.SetThemeGroup(colorTheme.Id, fileName, colorTheme, newMenuTheme, newTacScreenTheme, newHudTheme);
				themeGroups[colorTheme.Id] = themeGroup;
				Debug.Log("ThemeManager: loaded custom theme group '" + fileName + "' from " + text);
			}
		}

		public static void InitThemeManager()
		{
			LoadAllStyleLabels();
			LoadAllThemeGroups();
			ApplyThemeGroupSettings();
			NotifyThemeGroupChanged();
		}

		public static void SwitchToThemeGroup(string themeGroupId)
		{
			SetPlayerPrefs(themeGroupId);
			ApplyThemeGroupSettings();
			NotifyThemeGroupChanged();
		}

		public static void SetPlayerPrefs(string themeGroupId)
		{
			PlayerSettings.themeGroupId = themeGroupId;
			PlayerPrefs.SetString("ThemeGroupId", themeGroupId);
		}

		public static void ApplyThemeGroupSettings()
		{
			string themeGroupId = PlayerSettings.themeGroupId;
			if (string.IsNullOrEmpty(themeGroupId) || !themeGroups.TryGetValue(themeGroupId, out var value))
			{
				themeGroupId = (PlayerSettings.themeGroupId = "vanilla");
				themeGroups.TryGetValue(themeGroupId, out value);
			}
			Active = value;
		}

		public static void NotifyThemeGroupChanged()
		{
			ThemeManager.ThemeGroupChanged?.Invoke();
		}

		public static void RenameActiveThemeGroup(string newName)
		{
			string name = Active.name;
			if (string.IsNullOrWhiteSpace(newName) || (GetThemeGroupNames().Contains(newName) && Active.name != newName))
			{
				Active.name = name;
				return;
			}
			Active.name = newName;
			string text = Application.persistentDataPath + "/themes/" + name;
			if (Directory.Exists(text) && name != newName)
			{
				Directory.Move(text, Application.persistentDataPath + "/themes/" + Active.name);
			}
		}

		public static void DeleteActiveThemeGroup()
		{
			string path = Application.persistentDataPath + "/themes/" + Active.name;
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
				themeGroups.Remove(Active.Id);
			}
		}

		public static void SaveActiveThemeGroup()
		{
			Active.ColorTheme.SaveToJson();
			Active.HudTheme.SaveToJson("HUD");
			Active.TacScreenTheme.SaveToJson("TAC");
			Active.MenuTheme.SaveToJson("MENU");
		}

		public static string CopyActiveThemeGroupWithNewId()
		{
			if (Active == null)
			{
				throw new Exception("No active ThemeGroup to copy.");
			}
			string text = "default_" + Path.GetRandomFileName().Substring(0, 6);
			ColorTheme colorTheme = UnityEngine.Object.Instantiate(Active.ColorTheme);
			colorTheme.Id = text;
			colorTheme.name = text;
			Theme theme = UnityEngine.Object.Instantiate(Active.MenuTheme);
			theme.Id = text;
			theme.name = text;
			Theme theme2 = UnityEngine.Object.Instantiate(Active.TacScreenTheme);
			theme2.Id = text;
			theme2.name = text;
			Theme theme3 = UnityEngine.Object.Instantiate(Active.HudTheme);
			theme3.Id = text;
			theme3.name = text;
			ThemeGroup themeGroup = ScriptableObject.CreateInstance<ThemeGroup>();
			themeGroup.SetThemeGroup(text, text, colorTheme, theme, theme2, theme3);
			themeGroups[text] = themeGroup;
			return text;
		}

		public static ThemeGroup GetThemeGroupByIndex(int index)
		{
			List<string> themeGroupIds = GetThemeGroupIds();
			if (index < 0 || index >= themeGroupIds.Count)
			{
				return null;
			}
			return themeGroups[themeGroupIds[index]];
		}

		public static int GetCurrentThemeGroupIndex()
		{
			return GetThemeGroupIds().IndexOf(Active?.Id);
		}

		public static List<string> GetThemeGroupNames()
		{
			List<string> list = new List<string>();
			foreach (ThemeGroup value in themeGroups.Values)
			{
				list.Add(value.name);
			}
			return list;
		}

		private static List<string> GetThemeGroupIds()
		{
			return new List<string>(themeGroups.Keys);
		}
	}
}
