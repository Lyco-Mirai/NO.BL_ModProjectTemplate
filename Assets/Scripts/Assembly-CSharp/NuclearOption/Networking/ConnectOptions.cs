namespace NuclearOption.Networking
{
	public class ConnectOptions
	{
		public readonly SocketType SocketType;

		public string Password;

		public readonly string UdpHost;

		public readonly int? UdpPort;

		public readonly string SteamLobbyIDString;

		public ConnectOptions(SocketType socketType, string udpHost = null, int? udpPort = null)
		{
			SocketType = socketType;
			UdpHost = udpHost;
			UdpPort = udpPort;
		}

		public ConnectOptions(SocketType socketType, string steamLobbyIDString)
		{
			SocketType = socketType;
			SteamLobbyIDString = steamLobbyIDString;
		}
	}
}
