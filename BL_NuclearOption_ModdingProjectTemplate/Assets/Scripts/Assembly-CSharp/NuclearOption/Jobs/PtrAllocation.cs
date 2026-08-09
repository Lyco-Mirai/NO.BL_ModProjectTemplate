using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct PtrAllocation<T> : IDisposable where T : unmanaged
	{
		public unsafe readonly T* ptr;

		public unsafe readonly bool IsCreated
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ptr != null;
			}
		}

		public unsafe PtrAllocation(T* ptr)
		{
			this.ptr = ptr;
		}

		public void Dispose()
		{
			if (IsCreated)
			{
				JobsAllocator<T>.Free(ref this);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe readonly ref T Ref()
		{
			if (ptr == null)
			{
				PtrNullException<PtrAllocation<T>>.Throw();
			}
			return ref *ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ref T RefCheckSafety()
		{
			if (Ptr<T>.JobRunningSafety)
			{
				Debug.LogError($"Getting Ptr<{typeof(T)}> but JobRunningSafety is true");
			}
			return ref Ref();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe readonly T Value()
		{
			if (ptr == null)
			{
				PtrNullException<Ptr<T>>.Throw();
			}
			return *ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe readonly Ptr<T> AsPtr()
		{
			return new Ptr<T>(ptr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Ptr<T>(PtrAllocation<T> ptr)
		{
			return ptr.AsPtr();
		}

		public override string ToString()
		{
			if (!IsCreated)
			{
				return "PtrAllocation(NULL)";
			}
			return "PtrAllocation(" + Ref().ToString() + ")";
		}
	}
}
