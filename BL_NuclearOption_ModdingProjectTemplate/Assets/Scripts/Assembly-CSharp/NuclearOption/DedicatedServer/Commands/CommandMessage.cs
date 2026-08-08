using System;

namespace NuclearOption.DedicatedServer.Commands
{
	[Serializable]
	public struct CommandMessage
	{
		public string name;

		public string[] arguments;
	}
}
