using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public class JobsAllocator : IDisposable
	{
		public long TotalBytes;

		public int TotalItems;

		public int AllocatedItems;

		public readonly List<IntPtr> chunks = new List<IntPtr>();

		public readonly Queue<IntPtr> freeItems = new Queue<IntPtr>();

		~JobsAllocator()
		{
			Dispose();
		}

		public void Dispose()
		{
			List<IntPtr> list = chunks;
			if (list == null || list.Count <= 0)
			{
				return;
			}
			foreach (IntPtr chunk in chunks)
			{
				if (chunk != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(chunk);
				}
			}
			chunks.Clear();
		}
	}
	public class JobsAllocator<T> : JobsAllocator where T : unmanaged
	{
		private const int DEFAULT_CHUNK_SIZE = 256;

		private static readonly JobsAllocator<T> i = new JobsAllocator<T>();

		private JobsAllocator()
		{
			JobsAllocatorShared.Allocators.Add(this);
		}

		private unsafe T* Allocate(int newChunkSize)
		{
			if (freeItems.Count == 0)
			{
				AllocateNewChunk(newChunkSize);
			}
			T* ptr = (T*)freeItems.Dequeue().ToPointer();
			*ptr = default(T);
			AllocatedItems++;
			return ptr;
		}

		private unsafe void AllocateNewChunk(int newChunkSize)
		{
			int cb = sizeof(T) * newChunkSize;
			RecordChunk(newChunkSize);
			IntPtr intPtr = Marshal.AllocHGlobal(cb);
			chunks.Add(intPtr);
			for (int i = 0; i < newChunkSize; i++)
			{
				IntPtr item = intPtr + i * sizeof(T);
				freeItems.Enqueue(item);
			}
		}

		private unsafe void RecordChunk(int items)
		{
			int num = sizeof(T) * items;
			TotalBytes += num;
			TotalItems += items;
			long num2 = TotalBytes / 1024;
			ColorLog<T>.Info($"Allocating new Chunk for {typeof(T).Name}, total KB:{num2}");
		}

		public unsafe static void Allocate(ref PtrAllocation<T> ptr, int newChunkSize = 256)
		{
			if (ptr.ptr != null)
			{
				Debug.LogError("JobsAllocHelper.Allocate was given a pointer that already had a value");
			}
			else
			{
				ptr = new PtrAllocation<T>(i.Allocate(newChunkSize));
			}
		}

		public unsafe static void AllocateRefCounter(ref PtrRefCounter<T> ptrRefCounter, int newChunkSize = 256)
		{
			if (ptrRefCounter.field.ptr != null)
			{
				Debug.LogError("JobsAllocHelper.Allocate was given a pointer that already had a value");
				return;
			}
			PtrAllocation<T> field = new PtrAllocation<T>(i.Allocate(newChunkSize));
			PtrAllocation<int> refCounter = new PtrAllocation<int>(JobsAllocator<int>.i.Allocate(newChunkSize));
			refCounter.Ref() = 1;
			ptrRefCounter = new PtrRefCounter<T>(field, refCounter);
		}

		public unsafe static void Free(ref PtrAllocation<T> ptr)
		{
			if (ptr.ptr == null)
			{
				Debug.LogError("JobsAllocHelper.Free was given a null pointer");
				return;
			}
			if (!MainMenu.ApplicationIsQuitting)
			{
				*ptr.ptr = default(T);
			}
			i.freeItems.Enqueue(new IntPtr(ptr.ptr));
			ptr = default(PtrAllocation<T>);
			i.AllocatedItems--;
		}

		public static void Free(ref PtrRefCounter<T> ptrRefCounter)
		{
			PtrAllocation<T> ptr = ptrRefCounter.field;
			PtrAllocation<int> ptr2 = ptrRefCounter.refCounter;
			Free(ref ptr);
			JobsAllocator<int>.Free(ref ptr2);
			ptrRefCounter = default(PtrRefCounter<T>);
		}
	}
}
