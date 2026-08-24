using NuclearOption.SceneLoading;

namespace NuclearOption.Networking
{
	public class HostOptions
	{
		public readonly GameState GameState;

		public readonly SocketType SocketType;

		public int? MaxConnections;

		public readonly MapKey Map;

		public readonly string SystemScene;

		public string Password;

		public int? UdpPort;

		public HostOptions(SocketType socketType, GameState gameState, MapKey map)
		{
			GameState = gameState;
			SocketType = socketType;
			Map = map;
		}

		public HostOptions(SocketType socketType, GameState gameState, string systemScene)
		{
			GameState = gameState;
			SocketType = socketType;
			SystemScene = systemScene;
		}
	}
}
