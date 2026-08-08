using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuclearOption.AddressableScripts.ModFoldersImpl;
using NuclearOption.Workshop;

namespace NuclearOption.ModScripts.Impl
{
	internal sealed class AircraftLiveryMod : ModType
	{
		public AircraftLiveryMod()
			: base("Aircraft Livery")
		{
		}

		protected override string GetRootFolder()
		{
			return AppDataSkins.Folder;
		}

		public override List<string> ListItems()
		{
			return (from x in AppDataSkins.ListItems()
				select Path.GetFileName(x)).ToList();
		}

		public override WorkshopUploadItem GetItem(string itemName)
		{
			if (string.IsNullOrEmpty(itemName))
			{
				throw new InvalidOperationException("Page 3 should not be active when mission name is null");
			}
			LiveryMetaData? liveryMetaData = AppDataSkins.FromFolderName(itemName);
			if (!liveryMetaData.HasValue)
			{
				return null;
			}
			string catalogFolder = AppDataSkins.GetCatalogFolder(itemName);
			return new WorkshopUploadItem(Tag, SubscribedItemType.AircraftLivery, catalogFolder, liveryMetaData.Value.DisplayName);
		}
	}
}
