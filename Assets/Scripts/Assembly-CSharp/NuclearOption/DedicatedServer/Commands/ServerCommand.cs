namespace NuclearOption.DedicatedServer.Commands
{
	public class ServerCommand
	{
		public delegate CommandResponse CommandDelegate(ServerRemoteCommands server, string[] arguments);

		public readonly string Name;

		private readonly CommandDelegate action;

		public ServerCommand(string name, CommandDelegate action)
		{
			Name = name;
			this.action = action;
		}

		public CommandResponse Run(ServerRemoteCommands server, string[] arguments)
		{
			return action(server, arguments);
		}
	}
}
