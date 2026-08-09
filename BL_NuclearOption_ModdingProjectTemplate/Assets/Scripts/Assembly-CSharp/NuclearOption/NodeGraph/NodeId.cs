using System;
using UnityEngine;

namespace NuclearOption.NodeGraph
{
	[Serializable]
	public struct NodeId : IEquatable<NodeId>
	{
		[SerializeField]
		private string value;

		public string Value => value;

		public NodeId(string value)
		{
			this.value = value;
		}

		public bool Equals(NodeId other)
		{
			return value == other.value;
		}

		public override bool Equals(object obj)
		{
			if (obj is NodeId other)
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

		public static bool operator ==(NodeId left, NodeId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(NodeId left, NodeId right)
		{
			return !left.Equals(right);
		}
	}
}
