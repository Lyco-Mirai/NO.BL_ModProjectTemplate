using NuclearOption.Workshop;
using Steamworks;

namespace NuclearOption.ModScripts
{
	public class WorkshopUploadItem : IWorkshopCreateCallbacks
	{
		public readonly string Tag;

		public readonly SubscribedItemType Type;

		public readonly string Folder;

		public readonly string DisplayName;

		public string Description;

		public string ImagePath;

		public bool Public;

		public PublishedFileId_t WorkshopId { get; private set; }

		public WorkshopUploadItem(string tag, SubscribedItemType type, string folder, string displayName)
		{
			Tag = tag;
			Type = type;
			Folder = folder;
			DisplayName = displayName;
			WorkshopJson? workshopJson = WorkshopJson.ReadFile(Folder, ignoreNotFoundWarning: true);
			WorkshopId = (workshopJson.HasValue ? workshopJson.Value.PublishedId : PublishedFileId_t.Invalid);
		}

		public SteamWorkshopItem ToSteamItem()
		{
			return new SteamWorkshopItem(DisplayName, Tag, WorkshopId)
			{
				Description = Description,
				ImagePath = ImagePath,
				Public = Public,
				ContentPath = Folder
			};
		}

		void IWorkshopCreateCallbacks.OnCreateOrFail(PublishedFileId_t id)
		{
			WorkshopId = id;
			WorkshopJson.WriteFile(Folder, id, Type);
		}

		public void ClearSteamId()
		{
			WorkshopId = PublishedFileId_t.Invalid;
		}
	}
}
