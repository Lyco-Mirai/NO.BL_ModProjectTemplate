using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct PtrRefCounter<T> : IDisposable where T : unmanaged
	{
		public readonly PtrAllocation<T> field;

		public readonly PtrAllocation<int> refCounter;

		public unsafe readonly bool IsCreated
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return field.ptr != null;
			}
		}

		public PtrRefCounter(PtrAllocation<T> field, PtrAllocation<int> refCounter)
		{
			this.field = field;
			this.refCounter = refCounter;
		}

		public readonly void AddRef()
		{
			refCounter.Ref()++;
		}

		public unsafe void RemoveRef()
		{
			if (!refCounter.IsCreated)
			{
				Debug.LogWarning("RemoveRef called when PtrRefCounter was not created");
				return;
			}
			if (!MainMenu.ApplicationIsQuitting)
			{
				int num = 0;
				try
				{
					ref int reference = ref refCounter.Ref();
					reference--;
					num = reference;
				}
				catch (Exception arg)
				{
					Debug.LogWarning($"Exception Removing Ref field:{(ulong)refCounter.ptr:X} {arg}");
					return;
				}
				if (num < 0)
				{
					Debug.LogWarning($"RemoveRef set count to {num} field:{(ulong)refCounter.ptr:X}");
				}
				if (num <= 0)
				{
					Dispose();
				}
			}
			this = default(PtrRefCounter<T>);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly int GetRefCount()
		{
			return refCounter.Value();
		}

		public void Dispose()
		{
			if (IsCreated)
			{
				JobsAllocator<T>.Free(ref this);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ref T Ref()
		{
			return ref field.Ref();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ref T RefCheckSafety()
		{
			return ref field.RefCheckSafety();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T Value()
		{
			return field.Value();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe readonly Ptr<T> AsPtr()
		{
			return new Ptr<T>(field.ptr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Ptr<T>(PtrRefCounter<T> ptr)
		{
			return ptr.AsPtr();
		}

		public override string ToString()
		{
			if (!IsCreated)
			{
				return "PtrRefCounter(NULL)";
			}
			return "PtrRefCounter(" + Ref().ToString() + ")";
		}
	}
}
