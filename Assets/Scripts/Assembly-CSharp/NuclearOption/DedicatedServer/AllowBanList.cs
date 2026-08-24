using System.Collections.Generic;
using System.IO;
using System.Text;
using Steamworks;

namespace NuclearOption.DedicatedServer
{
	public class AllowBanList
	{
		private readonly Dictionary<CSteamID, string> players = new Dictionary<CSteamID, string>();

		public IEnumerable<(CSteamID id, string reason)> GetItems()
		{
			foreach (var (item, item2) in players)
			{
				yield return (id: item, reason: item2);
			}
		}

		public bool Contains(CSteamID id)
		{
			return players.ContainsKey(id);
		}

		public void Add(CSteamID id, string reason)
		{
			if (players.TryAdd(id, reason))
			{
				ColorLog<AllowBanList>.Info($"Adding {id} to list");
			}
			else
			{
				ColorLog<AllowBanList>.Info($"{id} already in list");
			}
		}

		public void Remove(CSteamID id)
		{
			if (players.Remove(id))
			{
				ColorLog<AllowBanList>.Info($"Removed {id} from list");
			}
			else
			{
				ColorLog<AllowBanList>.Info($"{id} not in list");
			}
		}

		public void Clear()
		{
			ColorLog<AllowBanList>.Info("Clearing list");
			players.Clear();
		}

		public void Load(string path, bool allowMissing = false)
		{
			ColorLog<AllowBanList>.Info("Loading Steam Ids from " + path);
			List<(CSteamID, string)> list = new List<(CSteamID, string)>();
			Load(path, list, allowMissing);
			foreach (var (cSteamID, value) in list)
			{
				if (players.TryAdd(cSteamID, value))
				{
					ColorLog<AllowBanList>.Info($"Adding {cSteamID} to list");
				}
				else
				{
					ColorLog<AllowBanList>.Info($"{cSteamID} already in list");
				}
			}
		}

		public static void Load(string path, List<(CSteamID, string reason)> banned, bool allowMissing = false)
		{
			if (!File.Exists(path))
			{
				if (allowMissing)
				{
					ColorLog<AllowBanList>.Info("File does not exist: " + path);
				}
				else
				{
					ColorLog<AllowBanList>.LogError("File does not exist: " + path);
				}
				return;
			}
			string[] array = File.ReadAllText(path).Split('\r', '\n');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split("//", 2);
				string obj = array2[0];
				string text = ((array2.Length > 1) ? array2[1] : null);
				string text2 = obj.Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					string item = text?.Trim();
					if (ulong.TryParse(text2, out var result))
					{
						CSteamID item2 = new CSteamID(result);
						banned.Add((item2, item));
					}
					else
					{
						ColorLog<AllowBanList>.LogError("Failed to parse " + text2);
					}
				}
			}
		}

		public static void Save(string path, List<(CSteamID id, string reason)> banned)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var (id, reason) in banned)
			{
				stringBuilder.AppendLine(FormatLine(id, reason));
			}
			File.WriteAllText(path, stringBuilder.ToString());
		}

		private static string FormatLine(CSteamID id, string reason)
		{
			string text = id.m_SteamID.ToString();
			if (string.IsNullOrEmpty(reason))
			{
				return text;
			}
			return text + " // " + reason;
		}

		public void BanAndAppendId(string path, CSteamID id, string reason, bool createIfMissing = false)
		{
			BanAndAppendId(this, path, id, reason, createIfMissing);
		}

		public static void BanAndAppendId(AllowBanList list, string path, CSteamID id, string reason, bool createIfMissing = false)
		{
			list.Add(id, reason);
			AppendId(path, id, reason, createIfMissing);
		}

		public static void AppendId(string path, CSteamID id, string reason, bool createIfMissing = false)
		{
			if (!File.Exists(path))
			{
				if (!createIfMissing)
				{
					ColorLog<AllowBanList>.LogError("File does not exist: " + path);
					return;
				}
				ColorLog<AllowBanList>.Info("Creating new list file at " + path);
			}
			ColorLog<AllowBanList>.Info($"Appending Steam {id} to {path}");
			File.AppendAllLines(path, new string[1] { FormatLine(id, reason) });
		}

		public static void RemoveId(string path, CSteamID id, bool allowMissing = false)
		{
			if (!File.Exists(path))
			{
				if (!allowMissing)
				{
					ColorLog<AllowBanList>.LogError("File does not exist: " + path);
				}
				return;
			}
			ColorLog<AllowBanList>.Info($"Removing Steam {id} from {path}");
			List<(CSteamID, string)> list = new List<(CSteamID, string)>();
			Load(path, list);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (list[num].Item1 == id)
				{
					list.RemoveAt(num);
				}
			}
			Save(path, list);
		}
	}
}
