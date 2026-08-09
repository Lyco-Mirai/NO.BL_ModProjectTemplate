using System;
using System.Runtime.CompilerServices;

namespace NuclearOption.Networking
{
	public struct PlayerRef : IEquatable<PlayerRef>
	{
		public readonly uint PlayerId;

		private Player player;

		public static PlayerRef Invalid => default(PlayerRef);

		public Player Player
		{
			get
			{
				if (!Valid())
				{
					return null;
				}
				if (player == null)
				{
					UnitRegistry.playerLookup.TryGetValue(this, out player);
				}
				return player;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Valid()
		{
			return PlayerId != 0;
		}

		public PlayerRef(Player player)
		{
			PlayerId = player.NetId;
			this.player = player;
		}

		public override int GetHashCode()
		{
			return (int)PlayerId;
		}

		public override bool Equals(object obj)
		{
			if (obj is PlayerRef other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(PlayerRef other)
		{
			return PlayerId == other.PlayerId;
		}

		public override string ToString()
		{
			return $"Player({PlayerId})";
		}
	}
}
