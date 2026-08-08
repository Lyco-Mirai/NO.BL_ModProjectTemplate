using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission.Objectives
{
	public class CompleteAllList<T> : IObjectiveList<T>
	{
		private readonly List<T> allItems;

		private readonly Action itemCompleted;

		private readonly bool[] complete;

		private readonly List<int> networkList = new List<int>();

		public CompleteAllList(List<T> allItems, Action itemCompleted)
		{
			this.allItems = allItems;
			this.itemCompleted = itemCompleted;
			complete = new bool[allItems.Count];
		}

		public float GetCompletePercent()
		{
			int num = 0;
			for (int i = 0; i < complete.Length; i++)
			{
				if (complete[i])
				{
					num++;
				}
			}
			return (float)num / (float)allItems.Count;
		}

		public List<int> UpdateNetworkList()
		{
			WriteNetworkData(networkList);
			return networkList;
		}

		public void WriteNetworkData(List<int> data)
		{
			data.Clear();
			for (int i = 0; i < complete.Length; i++)
			{
				int num = i % 32;
				if (num == 0)
				{
					data.Add(complete[i] ? 1 : 0);
				}
				else if (complete[i])
				{
					int index = i / 32;
					uint num2 = (uint)data[index];
					num2 |= (uint)(1 << num);
					data[index] = (int)num2;
				}
			}
		}

		public void ReadNetworkData(List<int> data)
		{
			for (int i = 0; i < data.Count; i++)
			{
				uint num = (uint)data[i];
				for (int j = 0; j < 32; j++)
				{
					int num2 = i * 32 + j;
					if (num2 >= complete.Length)
					{
						return;
					}
					uint num3 = (num >> j) & 1;
					complete[num2] = num3 == 1;
				}
			}
		}

		public bool UpdateAndCheck(CheckCallback<T> check)
		{
			if (allItems.Count == 0)
			{
				return true;
			}
			bool result = true;
			for (int i = 0; i < allItems.Count; i++)
			{
				T item = allItems[i];
				bool num = complete[i];
				bool flag = check(item);
				if (!flag)
				{
					result = false;
				}
				if (num != flag)
				{
					complete[i] = flag;
					itemCompleted?.Invoke();
				}
			}
			return result;
		}

		public void ForeachNotComplete(Action<T> callback)
		{
			for (int i = 0; i < complete.Length; i++)
			{
				if (!complete[i])
				{
					callback(allItems[i]);
				}
			}
		}
	}
}
