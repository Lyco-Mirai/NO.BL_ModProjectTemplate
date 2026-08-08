using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NuclearOption.Networking.Lobbies
{
	public class FuzzySearch
	{
		private readonly int maxLength;

		private readonly float[,] distanceCache;

		public FuzzySearch(int maxLength)
		{
			this.maxLength = maxLength;
			distanceCache = new float[maxLength + 1, maxLength + 1];
		}

		public static bool SubstringSearch<T>(IEnumerable<T> items, Func<T, string> selector, string searchTerm, List<T> results)
		{
			results.Clear();
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				results.AddRange(items);
				return false;
			}
			searchTerm = searchTerm.ToLower();
			foreach (T item in items)
			{
				if (selector(item).ToLower().Contains(searchTerm))
				{
					results.Add(item);
				}
			}
			return true;
		}

		public bool Search<T>(IEnumerable<T> items, Func<T, string> selector, string searchTerm, int maxDistance, List<T> results, List<int> optionalScores)
		{
			results.Clear();
			optionalScores?.Clear();
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				results.AddRange(items);
				return false;
			}
			searchTerm = searchTerm.ToLower();
			List<(string, float, T)> list = new List<(string, float, T)>();
			foreach (T item in items)
			{
				string text = selector(item);
				float num = Distance(text.ToLower(), searchTerm);
				if (num <= (float)maxDistance)
				{
					list.Add((text, num, item));
				}
			}
			foreach (var item2 in list.OrderBy<(string, float, T), float>(((string Name, float Score, T item) x) => x.Score))
			{
				results.Add(item2.Item3);
				optionalScores.Add((int)item2.Item2);
			}
			return true;
		}

		public float Distance(string s, string t)
		{
			int length = s.Length;
			int length2 = t.Length;
			if (length > maxLength || length2 > maxLength)
			{
				return 2.1474836E+09f;
			}
			if (length == 0)
			{
				return length2;
			}
			if (length2 == 0)
			{
				return length;
			}
			float[,] array = distanceCache;
			for (int i = 0; i <= length; i++)
			{
				distanceCache[i, 0] = i;
			}
			for (int j = 0; j <= length2; j++)
			{
				distanceCache[0, j] = j;
			}
			for (int k = 1; k <= length; k++)
			{
				for (int l = 1; l <= length2; l++)
				{
					float val = array[k - 1, l] + 0.2f;
					float val2 = array[k, l - 1] + 3f;
					float num = ((t[l - 1] == s[k - 1]) ? 0f : 1.2f);
					float val3 = array[k - 1, l - 1] + num;
					array[k, l] = Math.Min(Math.Min(val, val2), val3);
				}
			}
			return array[length, length2];
		}

		public void LogDistanceCache(string s, string t)
		{
			if (s.Length > maxLength || t.Length > maxLength)
			{
				Debug.LogWarning($"Cannot log distance cache: One or both string lengths ({s.Length}, {t.Length}) exceed the cache's maximum length ({maxLength}).");
				return;
			}
			int length = s.Length;
			int length2 = t.Length;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(string.Format("{0,6}", ""));
			stringBuilder.Append(string.Format("{0,6}", "\"\""));
			for (int i = 0; i < length2; i++)
			{
				stringBuilder.Append($"{t[i],6}");
			}
			stringBuilder.AppendLine();
			for (int j = 0; j <= length; j++)
			{
				if (j == 0)
				{
					stringBuilder.Append(string.Format("{0,6}", "\"\""));
				}
				else
				{
					stringBuilder.Append($"{s[j - 1],6}");
				}
				for (int k = 0; k <= length2; k++)
				{
					stringBuilder.Append($"{distanceCache[j, k],6:F1}");
				}
				stringBuilder.AppendLine();
			}
			Debug.Log(stringBuilder.ToString());
		}
	}
}
