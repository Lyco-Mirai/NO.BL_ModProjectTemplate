using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public class JobUnitList<T> where T : IHasIndexInJob
	{
		private readonly List<T> _items = new List<T>();

		public readonly List<T> PendingAdd = new List<T>();

		public readonly List<int> PendingRemove = new List<int>();

		private bool removeNeedsSorting;

		public int CountAfterPending => _items.Count + PendingAdd.Count - PendingRemove.Count;

		public bool PendingChangesAdd => PendingAdd.Count > 0;

		public bool PendingChangesRemove => PendingRemove.Count > 0;

		public void Add(T item)
		{
			PendingAdd.Add(item);
		}

		private bool Contains(T item)
		{
			foreach (T item2 in PendingAdd)
			{
				if (item2.Equals(item))
				{
					return true;
				}
			}
			foreach (T item3 in _items)
			{
				if (item3.Equals(item))
				{
					return true;
				}
			}
			return false;
		}

		public void Remove(T item)
		{
			NullableIndex indexInJob = item.IndexInJob;
			if (indexInJob.HasValue)
			{
				PendingRemove.Add(indexInJob.Index);
				removeNeedsSorting = true;
			}
			else if (!PendingAdd.Remove(item))
			{
				Debug.LogError("Item being removed was not in item or pending");
			}
		}

		public void FullClear()
		{
			foreach (T item in _items)
			{
				if (item != null && item is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			foreach (T item2 in PendingAdd)
			{
				if (item2 != null && item2 is IDisposable disposable2)
				{
					disposable2.Dispose();
				}
			}
			_items.Clear();
			PendingAdd.Clear();
			PendingRemove.Clear();
		}

		public bool ProcessNextAdded(out T add, out int addIndex)
		{
			if (PendingAdd.Count == 0)
			{
				add = default(T);
				addIndex = 0;
				return false;
			}
			int index = PendingAdd.Count - 1;
			add = PendingAdd[index];
			PendingAdd.RemoveAt(index);
			addIndex = _items.Count;
			_items.Add(add);
			NullableIndex indexInJob = new NullableIndex(addIndex);
			add.IndexInJob = indexInJob;
			return true;
		}

		public bool ProcessNextRemoved(out int removeIndex, out T removedItem)
		{
			if (PendingRemove.Count == 0)
			{
				removeIndex = 0;
				removedItem = default(T);
				return false;
			}
			if (removeNeedsSorting)
			{
				PendingRemove.Sort();
				removeNeedsSorting = false;
			}
			int index = PendingRemove.Count - 1;
			removeIndex = PendingRemove[index];
			PendingRemove.RemoveAt(index);
			RemoveAtSwap(removeIndex, out removedItem);
			return true;
		}

		private void RemoveAtSwap(int removeIndex, out T removedItem)
		{
			removedItem = _items[removeIndex];
			int num = _items.Count - 1;
			if (num != removeIndex)
			{
				T value = _items[num];
				_items[removeIndex] = value;
				value.IndexInJob = new NullableIndex(removeIndex);
			}
			_items.RemoveAt(num);
		}
	}
}
