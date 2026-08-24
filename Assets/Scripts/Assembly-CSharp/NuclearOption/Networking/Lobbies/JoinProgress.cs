using Mirage;

namespace NuclearOption.Networking.Lobbies
{
	public struct JoinProgress
	{
		public string Body;

		public bool Join;

		public override string ToString()
		{
			return $"JoinProgress({Join}, {Body})";
		}

		public static JoinProgress GetFailReason(ClientStoppedReason? disconnectReason)
		{
			switch (disconnectReason)
			{
			default:
				return Fail("Unknown Reason");
			case ClientStoppedReason.Timeout:
				return Fail("Connection timeout with server");
			case ClientStoppedReason.LocalConnectionClosed:
			case ClientStoppedReason.ConnectingCancel:
				return Fail("Local client stopped");
			case ClientStoppedReason.RemoteConnectionClosed:
			case ClientStoppedReason.InvalidPacket:
			case ClientStoppedReason.KeyInvalid:
			case ClientStoppedReason.SendBufferFull:
				return Fail("Disconnected by server");
			case ClientStoppedReason.ServerFull:
				return Fail("Server full");
			case ClientStoppedReason.ConnectingTimeout:
				return Fail("Failed to connect to server");
			}
		}

		public static JoinProgress Joining(string step)
		{
			return new JoinProgress
			{
				Join = true,
				Body = step
			};
		}

		public static JoinProgress Fail(string reason)
		{
			return new JoinProgress
			{
				Body = reason,
				Join = false
			};
		}
	}
}
