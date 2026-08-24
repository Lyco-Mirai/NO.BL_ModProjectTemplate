namespace NuclearOption.DedicatedServer.Commands
{
	public enum StatusCode
	{
		Success = 2000,
		BadRequest = 4000,
		BadHeader = 4001,
		BadLength = 4002,
		JsonError = 4003,
		UnknownCommand = 4004,
		BadArguments = 4005,
		InternalServerError = 5000,
		CommandError = 5001,
		ConfigError = 5002
	}
}
