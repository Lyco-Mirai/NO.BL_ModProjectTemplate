using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables;

namespace NuclearOption.AddressableScripts.ModFoldersImpl
{
	public class AppDataSkins : Skins
	{
		public static string Folder => AddressablePaths.AppDataSkinPath;

		public static string[] ListItems()
		{
			if (!Directory.Exists(Folder))
			{
				return Array.Empty<string>();
			}
			return Directory.GetDirectories(Folder);
		}

		public static string GetCatalogFolder(string itemName)
		{
			return Path.Combine(Folder, itemName);
		}

		public static LiveryMetaData? FromFolderName(string itemName)
		{
			return ModLoader.ReadMetaData<LiveryMetaData>(GetCatalogFolder(itemName));
		}

		public override UniTask<string> GetCatalogPath(string folderName)
		{
			return UniTask.FromResult(folderName + "/catalog_1.json");
		}

		public override IEnumerable<LiveryMetaData> ListMetaData()
		{
			return Enumerable.Select(ListItems(), ModLoader.ReadMetaData<LiveryMetaData>).WhereNotNullable();
		}
	}
}
