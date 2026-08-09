using System;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	[Serializable]
	public struct NodeType : IEquatable<NodeType>
	{
		[SerializeField]
		private string value;

		public string Value => value;

		public NodeType(string value)
		{
			this.value = value;
		}

		public bool Equals(NodeType other)
		{
			return value == other.value;
		}

		public override bool Equals(object obj)
		{
			if (obj is NodeType other)
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

		public static bool operator ==(NodeType left, NodeType right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(NodeType left, NodeType right)
		{
			return !left.Equals(right);
		}
	}
}
