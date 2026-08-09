using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuclearOption.Workshop;
using Steamworks;
using TMPro;
using UnityEngine;

namespace NuclearOption.ModScripts
{
	public abstract class ModType
	{
		public readonly string Name;

		public readonly string Tag;

		public ModType(string nameAndTag)
			: this(nameAndTag, nameAndTag)
		{
		}

		public ModType(string name, string tag)
		{
			Name = name;
			Tag = tag;
		}

		public void PopulateItemDropdown(TMP_Dropdown dropdown)
		{
			dropdown.ClearOptions();
			dropdown.options.Add(new TMP_Dropdown.OptionData
			{
				text = ""
			});
			dropdown.AddOptions(ListItems());
			dropdown.SetValueWithoutNotify(0);
		}

		public abstract List<string> ListItems();

		public abstract WorkshopUploadItem GetItem(string itemName);

		protected abstract string GetRootFolder();

		public PublishedFileId_t ReadWorkshopId(string itemName)
		{
			WorkshopJson? workshopJson = WorkshopJson.ReadFile(Path.Combine(GetRootFolder(), itemName), ignoreNotFoundWarning: true);
			if (!workshopJson.HasValue)
			{
				return PublishedFileId_t.Invalid;
			}
			return workshopJson.Value.PublishedId;
		}

		public bool TryGetLocalItem(SteamWorkshopItem details, out WorkshopUploadItem item)
		{
			PublishedFileId_t id = details.WorkshopId;
			IEnumerable<(string, PublishedFileId_t)> source = from itemName in ListItems()
				select (itemName: itemName, id: ReadWorkshopId(itemName)) into x
				where x.id == id
				select x;
			int num = source.Count();
			if (num == 1)
			{
				string item2 = source.First().Item1;
				item = GetItem(item2);
				return true;
			}
			if (num == 0)
			{
				Debug.LogError($"Failed to find local item with id {id}");
			}
			else if (num > 2)
			{
				Debug.LogError($"Multiple local items with the id {id}");
			}
			item = null;
			return false;
		}
	}
}
