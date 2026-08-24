using System;

namespace NuclearOption.MissionEditorScripts
{
	public struct ItemWithButtonsWrapper
	{
		public readonly bool Enabled;

		public readonly string Text;

		public readonly int Index;

		public readonly int Count;

		public readonly Action<int> EditClicked;

		public readonly Action<int> DeleteClicked;

		public readonly MoveAction MoveClicked;

		public readonly string EditButtonText;

		public readonly string DeleteButtonText;

		public ItemWithButtonsWrapper(bool enabled, string text, int index, int count, Action<int> editClicked, Action<int> deleteClicked, MoveAction moveClicked, string editText, string deleteText)
		{
			Enabled = enabled;
			Text = text;
			Index = index;
			Count = count;
			EditClicked = editClicked;
			DeleteClicked = deleteClicked;
			MoveClicked = moveClicked;
			EditButtonText = editText;
			DeleteButtonText = deleteText;
		}
	}
}
