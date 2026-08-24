using System;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	[Serializable]
	public struct PinType : IEquatable<PinType>
	{
		[SerializeField]
		private string value;

		public string Value => value;

		public PinType(string value)
		{
			this.value = value;
		}

		public bool Equals(PinType other)
		{
			return value == other.value;
		}

		public override bool Equals(object obj)
		{
			if (obj is PinType other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (value == null)
			{
				return 0;
			}
			return value.GetHashCode();
		}

		public override string ToString()
		{
			return value ?? string.Empty;
		}

		public static bool operator ==(PinType left, PinType right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(PinType left, PinType right)
		{
			return !left.Equals(right);
		}
	}
}
