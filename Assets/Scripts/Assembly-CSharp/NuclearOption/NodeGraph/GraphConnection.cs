using System;

namespace NuclearOption.NodeGraph
{
	public readonly struct GraphConnection : IEquatable<GraphConnection>
	{
		public readonly GraphPin A;

		public readonly GraphPin B;

		public GraphPin OutputPin
		{
			get
			{
				if (A.Direction != PinDirection.Output)
				{
					return B;
				}
				return A;
			}
		}

		public GraphPin InputPin
		{
			get
			{
				if (A.Direction != PinDirection.Input)
				{
					return B;
				}
				return A;
			}
		}

		public bool Valid()
		{
			if (A != null)
			{
				return B != null;
			}
			return false;
		}

		public GraphConnection(GraphPin a, GraphPin b)
		{
			if (a == null)
			{
				throw new ArgumentNullException("a", "Pin A cannot be null");
			}
			if (b == null)
			{
				throw new ArgumentNullException("b", "Pin B cannot be null");
			}
			if (a.Direction == b.Direction)
			{
				throw new ArgumentException("Pins must have different directions");
			}
			A = a;
			B = b;
		}

		public bool Equals(GraphConnection other)
		{
			if (!(A == other.A) || !(B == other.B))
			{
				if (A == other.B)
				{
					return B == other.A;
				}
				return false;
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj is GraphConnection other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int num = ((A != null) ? A.GetHashCode() : 0);
			int num2 = ((B != null) ? B.GetHashCode() : 0);
			return num ^ num2;
		}

		public static bool operator ==(GraphConnection left, GraphConnection right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GraphConnection left, GraphConnection right)
		{
			return !left.Equals(right);
		}
	}
}
