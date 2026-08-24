using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NuclearOption.UI.HeaderSorting
{
	[Serializable]
	public class ListSortButton
	{
		public enum SortState
		{
			ascending = 0,
			descending = 1,
			None = 2
		}

		[SerializeField]
		private Button button;

		[SerializeField]
		private TextMeshProUGUI label;

		private string defaultLabelText;

		[NonSerialized]
		public int Mode;

		public SortState CurrentState { get; private set; } = SortState.None;

		public void Init(UnityAction onClick)
		{
			defaultLabelText = label.text;
			button.onClick.AddListener(onClick);
		}

		public void UpdateLabel(SortState newState, Color color, bool force = false)
		{
			if (force || CurrentState != newState)
			{
				CurrentState = newState;
				string text = "";
				switch (CurrentState)
				{
				case SortState.ascending:
					text = GoogleIconFont.FontString("\ue5c7").AddSize(0.8f);
					break;
				case SortState.descending:
					text = GoogleIconFont.FontString("\ue5c5").AddSize(0.8f);
					break;
				}
				label.text = defaultLabelText + text;
				label.color = color;
			}
		}
	}
}
