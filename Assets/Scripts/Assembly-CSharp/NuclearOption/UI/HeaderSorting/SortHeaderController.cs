using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.UI.HeaderSorting
{
	public class SortHeaderController
	{
		private readonly List<ListSortButton> buttonList = new List<ListSortButton>();

		private readonly Color activeColor;

		private readonly Color inactiveColor;

		private readonly Action<SortOrder> onSortChanged;

		public SortHeaderController(Color activeColor, Color inactiveColor, Action<SortOrder> onSortChanged)
		{
			this.activeColor = activeColor;
			this.inactiveColor = inactiveColor;
			this.onSortChanged = onSortChanged;
		}

		public void Register(ListSortButton button, int mode)
		{
			buttonList.Add(button);
			button.Mode = mode;
			button.Init(delegate
			{
				OnButtonClicked(button);
			});
			button.UpdateLabel(ListSortButton.SortState.None, inactiveColor, force: true);
		}

		private void OnButtonClicked(ListSortButton button)
		{
			bool isAscending = button.CurrentState != ListSortButton.SortState.ascending;
			onSortChanged?.Invoke(new SortOrder(button.Mode, isAscending));
		}

		public void UpdateVisuals(SortOrder sortOrder)
		{
			foreach (ListSortButton button in buttonList)
			{
				if (button.Mode == sortOrder.Mode)
				{
					if (sortOrder.IsAscending)
					{
						button.UpdateLabel(ListSortButton.SortState.ascending, activeColor);
					}
					else
					{
						button.UpdateLabel(ListSortButton.SortState.descending, activeColor);
					}
				}
				else
				{
					button.UpdateLabel(ListSortButton.SortState.None, inactiveColor);
				}
			}
		}
	}
}
