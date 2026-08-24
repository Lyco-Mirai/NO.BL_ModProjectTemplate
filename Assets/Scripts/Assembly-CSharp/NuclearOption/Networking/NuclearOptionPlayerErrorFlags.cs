using System;
using Mirage;

namespace NuclearOption.Networking
{
	public static class NuclearOptionPlayerErrorFlags
	{
		[Serializable]
		[Flags]
		public enum Names
		{
			None = 0,
			RpcNullException = 1,
			RpcException = 2,
			DeserializationException = 4,
			RpcSync = 8,
			RateLimit = 0x10,
			NoAuthority = 0x20,
			Unauthenticated = 0x40,
			Critical = 0x80,
			LikelyCheater = 0x100,
			CustomError = 0x10000,
			Other = 0x10000,
			InvalidLoadout = 0x20000,
			InvalidValue = 0x40000,
			OutOfBounds = 0x80000,
			InventoryCost = 0x100000,
			DistanceExploit = 0x200000,
			FactionViolation = 0x400000,
			InvalidState = 0x800000,
			MessagingSpam = 0x1000000,
			InvalidTransformSnapshot = 0x2000000
		}

		public static readonly PlayerErrorFlags Other = Custom(0);

		public static readonly PlayerErrorFlags InvalidLoadout = Custom(1);

		public static readonly PlayerErrorFlags InvalidValue = Custom(2);

		public static readonly PlayerErrorFlags OutOfBounds = Custom(3);

		public static readonly PlayerErrorFlags InventoryCost = Custom(4);

		public static readonly PlayerErrorFlags DistanceExploit = Custom(5);

		public static readonly PlayerErrorFlags FactionViolation = Custom(6);

		public static readonly PlayerErrorFlags InvalidState = Custom(7);

		public static readonly PlayerErrorFlags MessagingSpam = Custom(8);

		public static readonly PlayerErrorFlags InvalidTransformSnapshot = Custom(9);

		private static PlayerErrorFlags Custom(int index)
		{
			return (PlayerErrorFlags)(65536 << index);
		}
	}
}
