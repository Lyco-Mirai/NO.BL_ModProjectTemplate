using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using JamesFrowen.ScriptableVariables;
using NuclearOption.Workshop;

namespace NuclearOption.AddressableScripts.ModFoldersImpl
{
	public class WorkshopSkins : Skins
	{
		private static readonly Regex pattern = new Regex("\\{NuclearOption\\.AddressablePaths\\.AppDataSkinPath\\}\\\\(.+)\\\\(.+_assets_all\\.bundle)");

		private const string CATALOG_WORKSHOP = "catalog_workshop.json";

		private const string CATALOG_1 = "catalog_1.json";

		private async UniTask<string> CreateWorkshopCatalog(string folder)
		{
			return await UniTask.RunOnThreadPool(delegate
			{
				File.Copy(Path.Combine(folder, "catalog_1.hash"), Path.Combine(folder, "catalog_workshop.hash"), overwrite: true);
				string input = File.ReadAllText(Path.Combine(folder, "catalog_1.json"));
				string replacement = folder.Replace("\\", "\\\\") + "\\\\$2";
				input = pattern.Replace(input, replacement);
				string text = Path.Combine(folder, "catalog_workshop.json");
				File.WriteAllText(text, input);
				return text;
			});
		}

		public override async UniTask<string> GetCatalogPath(string folderName)
		{
			return await CreateWorkshopCatalog(folderName);
		}

		public override IEnumerable<LiveryMetaData> ListMetaData()
		{
			return Enumerable.Select(SteamWorkshop.GetSubscribedItems(refresh: true, SubscribedItemType.AircraftLivery), ModLoader.ReadMetaData<LiveryMetaData>).WhereNotNullable();
		}
	}
}
