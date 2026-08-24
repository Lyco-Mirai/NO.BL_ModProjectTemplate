using System;

namespace NuclearOption.NodeGraph
{
	public struct ContextMenuOptionId : IEquatable<ContextMenuOptionId>
	{
		public string category;

		public int id;

		public bool isTopLevel;

		public ContextMenuOptionId(string category, int id, bool isTopLevel = false)
		{
			this.category = (string.IsNullOrEmpty(category) ? "General" : category);
			this.id = id;
			this.isTopLevel = isTopLevel;
		}

		public bool Equals(ContextMenuOptionId other)
		{
			if (string.Equals(category, other.category, StringComparison.OrdinalIgnoreCase) && id == other.id)
			{
				return isTopLevel == other.isTopLevel;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ContextMenuOptionId other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((category != null) ? StringComparer.OrdinalIgnoreCase.GetHashCode(category) : 0) * 397) ^ id) * 397) ^ isTopLevel.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}{2}", category, id, isTopLevel ? ":Top" : "");
		}
	}
}
