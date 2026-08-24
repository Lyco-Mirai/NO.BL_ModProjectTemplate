using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Microsoft.Win32;
using Rewired;
using Rewired.Interfaces;
using UnityEngine;

public static class RewiredSaveDataMigrator
{
	public static async UniTask RunAsync()
	{
		Regex OldIdRegex = new Regex("\\|hardwareIdentifier=WindowsRawInput[^|]*-0000-0000-0000-504944564944\\|", RegexOptions.Compiled);
		Regex BaseNameRegex = new Regex("\\|hardwareIdentifier=WindowsRawInput([a-zA-Z]+)[^|]*\\|", RegexOptions.Compiled);
		ReInput.players.GetPlayer(0);
		IUserDataStore store = ReInput.userDataStore;
		store.LoadPlayerData(0);
		await UniTask.Yield(PlayerLoopTiming.Update);
		store.SavePlayerData(0);
		await UniTask.Yield(PlayerLoopTiming.Update);
		List<(string, string)> pairs = new List<(string, string)>();
		string text = "Software\\" + Application.companyName + "\\" + Application.productName;
		List<(string, string)> list = new List<(string, string)>();
		List<(string, string)> list2 = new List<(string, string)>();
		using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text))
		{
			if (registryKey == null)
			{
				Debug.LogWarning("[RewiredMigration] Registry key not found: " + text);
				return;
			}
			string[] valueNames = registryKey.GetValueNames();
			foreach (string text2 in valueNames)
			{
				int num = text2.LastIndexOf("_h");
				if (num < 0)
				{
					continue;
				}
				string text3 = text2.Substring(0, num);
				if (text3.StartsWith("RewiredSaveData") && text3.Contains("hardwareIdentifier=WindowsRawInput"))
				{
					string item = BaseNameRegex.Replace(text3, "|hardwareIdentifier=WindowsRawInput$1|");
					if (OldIdRegex.IsMatch(text3))
					{
						list.Add((text3, item));
					}
					else
					{
						list2.Add((text3, item));
					}
				}
			}
		}/*
		Dictionary<string, List<(string, string)>> dictionary = (from k in list2
			group k by k.sig).ToDictionary((IGrouping<string, (string full, string sig)> g) => g.Key, (IGrouping<string, (string full, string sig)> g) => g.ToList());
		foreach (IGrouping<string, (string, string)> item4 in from k in list
			group k by k.sig)
		{
			if (!dictionary.TryGetValue(item4.Key, out var value))
			{
				continue;
			}
			List<(string, string)> list3 = item4.ToList();
			if (list3.Count == value.Count)
			{
				for (int num2 = 0; num2 < list3.Count; num2++)
				{
					pairs.Add((list3[num2].Item1, value[num2].Item1));
				}
			}
		}*/
		await UniTask.Yield(PlayerLoopTiming.Update);
		if (pairs.Count == 0)
		{
			Debug.Log("[RewiredMigration] Nothing to apply.");
		}
		else
		{
			int migrated = 0;
			foreach (var item5 in pairs)
			{
				string item2 = item5.Item1;
				string item3 = item5.Item2;
				string value2 = PlayerPrefs.GetString(item2, null);
				if (!string.IsNullOrEmpty(value2))
				{
					PlayerPrefs.SetString(item3, value2);
					migrated++;
				}
				PlayerPrefs.DeleteKey(item2);
			}
			await UniTask.Yield(PlayerLoopTiming.Update);
			PlayerPrefs.Save();
			store.LoadPlayerData(0);
			await UniTask.Yield(PlayerLoopTiming.Update);
			store.SavePlayerData(0);
			Debug.Log($"[RewiredMigration] Applied: {migrated} keys moved to new identifiers.");
		}
		PlayerPrefs.SetInt("RewiredMigrated", 1);
		PlayerPrefs.Save();
	}
}
