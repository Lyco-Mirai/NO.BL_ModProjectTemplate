using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NuclearOption.Jobs
{
	public struct PtrArray<T> : IDisposable where T : unmanaged
	{
		private unsafe readonly T* ptr;

		public readonly int Length;

		public unsafe ref T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (ptr == null)
				{
					PtrNullException<PtrArray<T>>.Throw();
				}
				UnsafeJobExtensions.LengthCheck(index, Length);
				return ref ptr[index];
			}
		}

		public unsafe PtrArray(int length)
		{
			int cb = sizeof(T) * length;
			ptr = (T*)Marshal.AllocHGlobal(cb).ToPointer();
			Length = length;
			for (int i = 0; i < length; i++)
			{
				ptr[i] = default(T);
			}
			JobsAllocatorShared.RecordAlloc(this);
		}

		public void Dispose()
		{
			Dispose(ref this);
		}

		public unsafe static void Dispose(ref PtrArray<T> array)
		{
			if (array.ptr != null)
			{
				JobsAllocatorShared.RecordFree(array);
				Marshal.FreeHGlobal(new IntPtr(array.ptr));
			}
			array = default(PtrArray<T>);
		}

		public unsafe Ptr<T> GetPtr(int index)
		{
			if (ptr == null)
			{
				PtrNullException<PtrArray<T>>.Throw();
			}
			UnsafeJobExtensions.LengthCheck(index, Length);
			return new Ptr<T>(ptr + index);
		}
	}
}
