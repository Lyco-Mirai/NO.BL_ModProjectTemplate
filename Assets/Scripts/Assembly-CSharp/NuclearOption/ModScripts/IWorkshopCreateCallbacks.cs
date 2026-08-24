using Steamworks;

namespace NuclearOption.ModScripts
{
	public interface IWorkshopCreateCallbacks
	{
		void OnCreateOrFail(PublishedFileId_t id);
	}
}
