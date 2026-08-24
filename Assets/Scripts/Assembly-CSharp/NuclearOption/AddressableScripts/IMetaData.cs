using Steamworks;

namespace NuclearOption.AddressableScripts
{
	public interface IMetaData
	{
		PublishedFileId_t Id { get; set; }

		string FolderFullPath { get; set; }
	}
}
