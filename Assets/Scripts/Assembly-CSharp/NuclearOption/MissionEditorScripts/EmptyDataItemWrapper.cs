using System;

namespace NuclearOption.MissionEditorScripts
{
	public readonly struct EmptyDataItemWrapper
	{
		public readonly EmptyDataList.DrawInnerData DrawContent;

		public readonly object Value;

		public readonly int Index;

		public readonly int Count;

		public readonly Action<int> EditClicked;

		public readonly Action<int> DeleteClicked;

		public readonly MoveAction MoveClicked;

		public EmptyDataItemWrapper(EmptyDataList.DrawInnerData drawContent, object value, int index, int count, Action<int> editClicked, Action<int> deleteClicked, MoveAction moveClicked)
		{
			DrawContent = drawContent;
			Value = value;
			Index = index;
			Count = count;
			EditClicked = editClicked;
			DeleteClicked = deleteClicked;
			MoveClicked = moveClicked;
		}
	}
}
