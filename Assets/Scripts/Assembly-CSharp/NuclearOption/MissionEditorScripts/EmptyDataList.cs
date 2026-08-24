using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class EmptyDataList : ListControllerWithButtonsBase<EmptyDataItemWrapper>
	{
		public delegate void DrawInnerData(int index, object value, RectTransform parent);

		public delegate void DrawInnerData<T>(int index, T value, RectTransform parent);

		public bool AllowSwapItems = true;

		private DrawInnerData drawContent;

		private IList dataList;

		protected override IList GetDataList()
		{
			return dataList;
		}

		public void UpdateList<T>(List<T> list, DrawInnerData<T> drawContent)
		{
			dataList = list;
			this.drawContent = delegate(int i, object v, RectTransform c)
			{
				drawContent(i, (T)v, c);
			};
			RefreshList();
		}

		public override void RefreshList()
		{
			wrappers.Clear();
			for (int i = 0; i < dataList.Count; i++)
			{
				wrappers.Add(new EmptyDataItemWrapper(drawContent, dataList[i], i, dataList.Count, null, deleteClicked, AllowSwapItems ? new MoveAction(SwapItems) : null));
			}
			UpdateListFromWrapper();
		}
	}
}
