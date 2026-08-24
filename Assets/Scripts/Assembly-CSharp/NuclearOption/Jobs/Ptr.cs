using System.Runtime.CompilerServices;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public struct Ptr<T> where T : unmanaged
	{
		public static bool JobRunningSafety;

		public unsafe T* ptr;

		public unsafe readonly bool IsCreated => ptr != null;

		public unsafe Ptr(T* ptr)
		{
			this.ptr = ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe readonly ref T Ref()
		{
			if (ptr == null)
			{
				PtrNullException<Ptr<T>>.Throw();
			}
			return ref *ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ref T RefCheckSafety()
		{
			if (JobRunningSafety)
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

		public unsafe static implicit operator Ptr<T>(T* ptr)
		{
			return new Ptr<T>(ptr);
		}

		public unsafe static implicit operator T*(Ptr<T> ptr)
		{
			return ptr.ptr;
		}

		public unsafe static bool PtrEqual(Ptr<T> a, Ptr<T> b)
		{
			return a.ptr == b.ptr;
		}

		public override string ToString()
		{
			if (!IsCreated)
			{
				return "Ptr(NULL)";
			}
			return "Ptr(" + Ref().ToString() + ")";
		}
	}
}
