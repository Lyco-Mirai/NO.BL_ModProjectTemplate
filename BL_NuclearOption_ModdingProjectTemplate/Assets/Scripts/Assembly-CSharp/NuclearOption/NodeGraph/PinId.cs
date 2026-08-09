using System;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	[Serializable]
	public struct PinId : IEquatable<PinId>
	{
		[SerializeField]
		private string value;

		public string Value => value;

		public PinId(string value)
		{
			this.value = value;
		}

		public bool Equals(PinId other)
		{
			return value == other.value;
		}

		public override bool Equals(object obj)
		{
			if (obj is PinId other)
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

		public static bool operator ==(PinId left, PinId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(PinId left, PinId right)
		{
			return !left.Equals(right);
		}
	}
}
