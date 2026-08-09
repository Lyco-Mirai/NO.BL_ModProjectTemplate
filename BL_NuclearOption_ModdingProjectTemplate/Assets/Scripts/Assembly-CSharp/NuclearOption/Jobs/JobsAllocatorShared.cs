using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public static class JobsAllocatorShared
	{
		private class IntPtrCompare : IComparer<IntPtr>
		{
			public static IntPtrCompare Instance = new IntPtrCompare();

			public int Compare(IntPtr x, IntPtr y)
			{
				long num = x.ToInt64();
				long value = y.ToInt64();
				return num.CompareTo(value);
			}
		}

		public static List<JobsAllocator> Allocators = new List<JobsAllocator>();

		private static int arrayCount;

		private static long arrayBytes;

		private static int listCount;

		private static long listBytes;

		public static long TotalBytes()
		{
			long num = 0L;
			foreach (JobsAllocator allocator in Allocators)
			{
				num += allocator.TotalBytes;
			}
			return num;
		}

		public static int TotalItems()
		{
			int num = 0;
			foreach (JobsAllocator allocator in Allocators)
			{
				num += allocator.TotalItems;
			}
			return num;
		}

		public static int AllocatedItems()
		{
			int num = 0;
			foreach (JobsAllocator allocator in Allocators)
			{
				num += allocator.AllocatedItems;
			}
			return num;
		}

		public static int ItemsInPool()
		{
			int num = 0;
			foreach (JobsAllocator allocator in Allocators)
			{
				int num2 = allocator.TotalItems - allocator.AllocatedItems;
				num += num2;
			}
			return num;
		}

		public static void SortAll()
		{
			if (GameManager.gameState != GameState.Encyclopedia)
			{
				if (arrayCount > 0)
				{
					Debug.LogError($"Leak warning: PtrArray still allocated when sorting: count={arrayCount} byte={arrayBytes}");
				}
				if (listCount > 0)
				{
					Debug.LogError($"Leak warning: PtrList still allocated when sorting: count={listCount} byte={listBytes}");
				}
				foreach (JobsAllocator allocator in Allocators)
				{
					if (allocator.AllocatedItems > 0)
					{
						Debug.LogError($"Leak warning: {allocator.GetType()} still allocated when sorting: {allocator.AllocatedItems} / {allocator.TotalItems}");
					}
				}
			}
			Debug.Log("Sorting all Allocations");
			foreach (JobsAllocator allocator2 in Allocators)
			{
				SortQueue(allocator2.freeItems);
			}
		}

		private static void DebugAssertSorted(Queue<IntPtr> queue)
		{
			IntPtr intPtr = default(IntPtr);
			foreach (IntPtr item in queue)
			{
				Debug.Log(item);
				_ = intPtr != IntPtr.Zero;
				intPtr = item;
			}
		}

		public static void SortQueue(Queue<IntPtr> queue)
		{
			int count = queue.Count;
			IntPtr[] array = new IntPtr[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = queue.Dequeue();
			}
			Array.Sort(array, IntPtrCompare.Instance);
			for (int j = 0; j < count; j++)
			{
				queue.Enqueue(array[j]);
			}
		}

		private unsafe static int Size<T>() where T : unmanaged
		{
			return sizeof(T);
		}

		public static void RecordAlloc<T>(PtrArray<T> array) where T : unmanaged
		{
			arrayCount++;
			arrayBytes += Size<T>() * array.Length;
		}

		public static void RecordFree<T>(PtrArray<T> array) where T : unmanaged
		{
			arrayCount--;
			arrayBytes -= Size<T>() * array.Length;
		}

		public static void RecordAlloc<T>(PtrList<T> _, int newCapacity) where T : unmanaged
		{
			listCount++;
			listBytes += Size<T>() * newCapacity;
		}

		public static void RecordReAlloc<T>(PtrList<T> list, int newCapacity) where T : unmanaged
		{
			listBytes += Size<T>() * (newCapacity - list.Capacity);
		}

		public static void RecordFree<T>(PtrList<T> list) where T : unmanaged
		{
			listCount--;
			listBytes -= Size<T>() * list.Capacity;
		}
	}
}
