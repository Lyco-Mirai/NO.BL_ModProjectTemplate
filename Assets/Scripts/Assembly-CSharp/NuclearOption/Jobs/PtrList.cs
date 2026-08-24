using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NuclearOption.Jobs
{
	public struct PtrList<T> : IDisposable where T : unmanaged
	{
		private unsafe T* ptr;

		public int Length;

		public int Capacity;

		public unsafe ref T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (ptr == null)
				{
					PtrNullException<PtrList<T>>.Throw();
				}
				UnsafeJobExtensions.LengthCheck(index, Length);
				return ref ptr[index];
			}
		}

		public unsafe void EnsureCapacity(int needLength)
		{
			int capacity = Capacity;
			if (capacity < needLength)
			{
				int num = JobManager.IncreaseCapacity(capacity, needLength, 4);
				int num2 = sizeof(T) * num;
				IntPtr intPtr;
				if (ptr == null)
				{
					JobsAllocatorShared.RecordAlloc(this, num);
					intPtr = Marshal.AllocHGlobal(num2);
				}
				else
				{
					JobsAllocatorShared.RecordReAlloc(this, num);
					intPtr = Marshal.ReAllocHGlobal(new IntPtr(ptr), (IntPtr)num2);
				}
				ptr = (T*)intPtr.ToPointer();
				Capacity = num;
			}
		}

		public void Dispose()
		{
			Dispose(ref this);
		}

		public unsafe static void Dispose(ref PtrList<T> list)
		{
			if (list.ptr != null)
			{
				JobsAllocatorShared.RecordFree(list);
				Marshal.FreeHGlobal(new IntPtr(list.ptr));
			}
			list = default(PtrList<T>);
		}

		public void Add(T item)
		{
			EnsureCapacity(Length + 1);
			this[Length] = item;
			Length++;
		}

		public unsafe Ptr<T> GetPtr(int index)
		{
			if (ptr == null)
			{
				PtrNullException<PtrList<T>>.Throw();
			}
			UnsafeJobExtensions.LengthCheck(index, Length);
			return new Ptr<T>(ptr + index);
		}
	}
}
