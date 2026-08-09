using Steamworks;

namespace NuclearOption.Workshop
{
	public readonly struct SubscribedItem
	{
		public readonly PublishedFileId_t Id;

		public readonly string Folder;

		public readonly WorkshopJson Workshop;

		public SubscribedItem(PublishedFileId_t itemId, string folder, WorkshopJson workshop)
		{
			this = default(SubscribedItem);
			Id = itemId;
			Folder = folder;
			Workshop = workshop;
		}
	}
}
