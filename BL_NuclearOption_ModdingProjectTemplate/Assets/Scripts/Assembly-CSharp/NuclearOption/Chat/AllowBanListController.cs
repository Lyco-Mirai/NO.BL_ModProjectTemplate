using System.Collections.Generic;
using System.Linq;
using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.DedicatedServer;
using Steamworks;

namespace NuclearOption.Chat
{
	public class AllowBanListController : ListController<AllowBanListEntry>
	{
		private readonly List<AllowBanListEntry> list = new List<AllowBanListEntry>();

		private AllowBanList banList;

		private string filePath;

		public void Setup((AllowBanList banList, string filePath) listSaveTuple)
		{
			list.Clear();
			(banList, filePath) = listSaveTuple;
			foreach (var (steamID, text) in from x in banList.GetItems()
				orderby x.id
				select x)
			{
				list.Add(new AllowBanListEntry(steamID, text, DeleteItem));
			}
			UpdateList(list);
		}

		private void DeleteItem(CSteamID id)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].SteamID == id)
				{
					list.RemoveAt(i);
					banList.Remove(id);
					if (!string.IsNullOrEmpty(filePath))
					{
						AllowBanList.RemoveId(filePath, id);
					}
					UpdateList(list);
					return;
				}
			}
			ColorLog<AllowBanListController>.LogError($"Did not find item with id {id} to delete");
		}
	}
}
