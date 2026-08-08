using System;
using System.Collections.Generic;

public class LazySortedQueue<T>
{
	private bool needsSorting;

	private readonly List<T> list = new List<T>();

	private readonly Comparison<T> comparison;

	public int Count => list.Count;

	public LazySortedQueue(Comparison<T> comparison)
	{
		this.comparison = (T x, T y) => comparison(y, x);
	}

	public void Enqueue(T item)
	{
		list.Add(item);
		needsSorting = true;
	}

	public bool TryDequeue(out T item)
	{
		if (list.Count == 0)
		{
			item = default(T);
			return false;
		}
		if (needsSorting)
		{
			list.Sort(comparison);
			needsSorting = false;
		}
		item = list[list.Count - 1];
		list.RemoveAt(list.Count - 1);
		return true;
	}
}
