using System;

namespace NuclearOption.UI.HeaderSorting
{
	public static class SortHelper
	{
		public static int CompareNullable<T>(T? a, T? b) where T : struct, IComparable
		{
			if (a.HasValue && b.HasValue)
			{
				return a.Value.CompareTo(b.Value);
			}
			if (a.HasValue)
			{
				return -1;
			}
			if (b.HasValue)
			{
				return 1;
			}
			return 0;
		}
	}
}
