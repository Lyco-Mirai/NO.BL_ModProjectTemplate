using Mirage;

namespace NuclearOption.Networking
{
	[NetworkMessage]
	public struct ServerLoadingProgressMessage
	{
		public string LoadingMessage;
	}
}
