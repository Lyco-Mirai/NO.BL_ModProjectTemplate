using System;
using System.Collections.Generic;
using System.Text;
using ProfanityFilter;
using Steamworks;
using TMPro;
using UnityEngine;

public static class StringHelper
{
	public const int STEAM_NAME_MAX_LENGTH = 32;

	public const char MISSING_CHARACTER = '□';

	private static global::ProfanityFilter.ProfanityFilter profanityFilter;

	public static string ProfanityFilter(this string message)
	{
		if (PlayerSettings.chatFilter)
		{
			return RunProfanityFilter(message);
		}
		return message;
	}

	public static string RunProfanityFilter(string message)
	{
		if (profanityFilter == null)
		{
			profanityFilter = new global::ProfanityFilter.ProfanityFilter();
		}
		return profanityFilter.CensorString(message);
	}

	public static string SanitizeRichText(this string input, int maxLength)
	{
		if (input == null)
		{
			return string.Empty;
		}
		if (input.Length > maxLength)
		{
			input = input.Substring(0, maxLength);
		}
		input = input.Replace("<", "<<i></i>");
		return input;
	}

	public static void GetSanitizeSteamName(out string rawName, out string safeName, TMP_FontAsset font)
	{
		rawName = SteamFriends.GetPersonaName();
		safeName = rawName.SanitizeRichText(32);
		if (font != null)
		{
			safeName = safeName.ReplaceCharactersNotInFont(font);
		}
	}

	public static string ReplaceCharactersNotInFont(this string input, TMP_FontAsset font)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		List<char> missingCharacters;
		bool flag = font.HasCharacters(input, out missingCharacters);
		if (!flag && missingCharacters == null)
		{
			font.ReadFontAssetDefinition();
			flag = font.HasCharacters(input, out missingCharacters);
			if (!flag && missingCharacters == null)
			{
				Debug.LogError("Failed to load font asset for ReplaceCharactersNotInFont");
				return input;
			}
		}
		if (flag)
		{
			return input;
		}
		Span<char> span = stackalloc char[input.Length];
		input.AsSpan().CopyTo(span);
		for (int i = 0; i < input.Length; i++)
		{
			if (missingCharacters.Contains(span[i]))
			{
				span[i] = '□';
			}
		}
		return new string(span);
	}

	public static string AddColor(this string message, Color color)
	{
		string text = ColorUtility.ToHtmlStringRGBA(color);
		return "<color=#" + text + ">" + message + "</color>";
	}

	public static string AddColor(this string message, string colorHex)
	{
		return "<color=" + colorHex + ">" + message + "</color>";
	}

	public static string AddSize(this string message, float sizePercent)
	{
		return $"<size={sizePercent * 100f}%>{message}</size>";
	}

	public static string AddSize(this string message, string size)
	{
		return "<size=" + size + ">" + message + "</size>";
	}

	public static string AddLink(this string message, string linkID)
	{
		return "<link=\"" + linkID + "\">" + message + "</link>";
	}

	public static Color ChangeHue(this Color color, float hue)
	{
		Color.RGBToHSV(color, out var _, out var S, out var V);
		return Color.HSVToRGB(hue, S, V);
	}

	public static int GetByteLength(string str)
	{
		return Encoding.UTF8.GetByteCount(str);
	}

	public static List<string> SplitStringByByteCount(string rawInput, bool includeEmptyLast, int chunkSize, int maxChunks)
	{
		ReadOnlySpan<char> readOnlySpan = rawInput.AsSpan();
		List<string> list = new List<string>();
		Encoding uTF = Encoding.UTF8;
		int num = 0;
		int num2 = 0;
		do
		{
			int num3 = readOnlySpan.Length - num;
			if (num3 == 0)
			{
				break;
			}
			int num4 = chunkSize / 4;
			int num5 = Math.Min(num3, chunkSize);
			if (num4 > num5)
			{
				num4 = num5;
			}
			int num6 = num5;
			ReadOnlySpan<char> chars = readOnlySpan.Slice(num, num5);
			if (uTF.GetByteCount(chars) <= chunkSize)
			{
				num6 = num5;
			}
			else
			{
				while (num4 <= num5)
				{
					int num7 = num4 + (num5 - num4) / 2;
					if (num7 == 0)
					{
						num4 = num7 + 1;
						continue;
					}
					chars = readOnlySpan.Slice(num, num7);
					if (uTF.GetByteCount(chars) <= chunkSize)
					{
						num6 = num7;
						num4 = num7 + 1;
					}
					else
					{
						num5 = num7 - 1;
					}
				}
			}
			list.Add(new string(readOnlySpan.Slice(num, num6)));
			num += num6;
			num2++;
		}
		while (num2 < maxChunks);
		if (includeEmptyLast && num2 < maxChunks)
		{
			list.Add("");
		}
		return list;
	}

	public static int CountLines(this string str)
	{
		if (str == null)
		{
			return 0;
		}
		int num = 1;
		for (int i = 0; i < str.Length; i++)
		{
			if (str[i] == '\n')
			{
				num++;
			}
		}
		return num;
	}
}
