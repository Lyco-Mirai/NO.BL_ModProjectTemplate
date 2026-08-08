namespace NuclearOption.UI.HeaderSorting
{
	public readonly struct SortOrder
	{
		public readonly int Mode;

		public readonly bool IsAscending;

		public SortOrder(int mode, bool isAscending)
		{
			Mode = mode;
			IsAscending = isAscending;
		}
	}
}
