using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.SavedMission.Objectives
{
	public class CompleteOrderList<T> : IObjectiveList<T>
	{
		public readonly struct CheckItem : IEquatable<CheckItem>
		{
			public readonly int Index;

			public readonly T Item;

			public CheckItem(int index, T item)
			{
				Index = index;
				Item = item;
			}

			bool IEquatable<CheckItem>.Equals(CheckItem other)
			{
				return Index == other.Index;
			}
		}

		private readonly List<T> allItems;

		private readonly CompleteOrder completeOrder;

		private readonly Action itemCompleted;

		public readonly List<CheckItem> ToCheck = new List<CheckItem>();

		private readonly List<int> networkList = new List<int>();

		private readonly int requiredCount;

		public bool MarkedComplete { get; private set; }

		public CompleteOrderList(List<T> allItems, CompleteOrder completeOrder, float completeSomePercent, Action itemCompleted)
		{
			this.allItems = allItems;
			this.completeOrder = completeOrder;
			this.itemCompleted = itemCompleted;
			MarkedComplete = allItems.Count == 0;
			if (allItems.Count <= 0)
			{
				return;
			}
			switch (this.completeOrder)
			{
			case CompleteOrder.CompleteAny:
				requiredCount = 1;
				break;
			case CompleteOrder.CompleteAll:
				requiredCount = allItems.Count;
				break;
			case CompleteOrder.InOrder:
				requiredCount = 0;
				break;
			case CompleteOrder.CompleteSome:
				requiredCount = Mathf.Max(1, Mathf.CeilToInt((float)allItems.Count * completeSomePercent));
				break;
			}
			switch (this.completeOrder)
			{
			case CompleteOrder.InOrder:
				ToCheck.Add(new CheckItem(0, allItems[0]));
				return;
			}
			for (int i = 0; i < this.allItems.Count; i++)
			{
				ToCheck.Add(new CheckItem(i, allItems[i]));
			}
		}

		public float GetCompletePercent()
		{
			switch (completeOrder)
			{
			default:
				return 0f;
			case CompleteOrder.CompleteAll:
			case CompleteOrder.CompleteSome:
				if (requiredCount == 0)
				{
					return 0f;
				}
				return Mathf.Clamp01((float)(allItems.Count - ToCheck.Count) / (float)requiredCount);
			case CompleteOrder.InOrder:
				if (ToCheck.Count == 0)
				{
					return 1f;
				}
				return (float)ToCheck[0].Index / (float)allItems.Count;
			}
		}

		public List<int> UpdateNetworkList()
		{
			WriteNetworkData(networkList);
			return networkList;
		}

		public void WriteNetworkData(List<int> data)
		{
			data.Clear();
			foreach (CheckItem item in ToCheck)
			{
				data.Add(item.Index);
			}
		}

		public void ReadNetworkData(List<int> data)
		{
			ToCheck.Clear();
			foreach (int datum in data)
			{
				ToCheck.Add(new CheckItem(datum, allItems[datum]));
			}
		}

		public bool UpdateAndCheck(CheckCallback<T> check)
		{
			if (MarkedComplete)
			{
				return true;
			}
			foreach (CheckItem item in ToCheck)
			{
				if (check(item.Item))
				{
					MarkedComplete = MarkCompletedInternal(item);
					return MarkedComplete;
				}
			}
			return false;
		}

		public void MarkCompleted(CheckItem toCheck)
		{
			MarkedComplete = MarkCompletedInternal(toCheck);
		}

		private bool MarkCompletedInternal(CheckItem toCheck)
		{
			if (completeOrder == CompleteOrder.CompleteAny)
			{
				return true;
			}
			if (completeOrder == CompleteOrder.CompleteAll || completeOrder == CompleteOrder.CompleteSome)
			{
				ToCheck.Remove(toCheck);
				if (allItems.Count - ToCheck.Count >= requiredCount)
				{
					return true;
				}
			}
			if (completeOrder == CompleteOrder.InOrder)
			{
				int num = toCheck.Index + 1;
				if (num >= allItems.Count)
				{
					return true;
				}
				ToCheck[0] = new CheckItem(num, allItems[num]);
			}
			itemCompleted?.Invoke();
			return false;
		}

		public void ForeachNotComplete(Action<T> callback)
		{
			foreach (CheckItem item in ToCheck)
			{
				callback(item.Item);
			}
		}
	}
}
