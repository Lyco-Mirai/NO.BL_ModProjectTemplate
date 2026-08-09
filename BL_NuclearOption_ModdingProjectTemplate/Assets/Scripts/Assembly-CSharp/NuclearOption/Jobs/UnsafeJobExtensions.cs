using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NuclearOption.Jobs
{
	public static class UnsafeJobExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ref JobTransformValues.ReadOnly GetReadOnlyRef(this NativeArray<JobTransformValues> array, int index)
		{
			LengthCheck(index, array.Length);
			JobTransformValues.ReadOnly* unsafeReadOnlyPtr = (JobTransformValues.ReadOnly*)array.GetUnsafeReadOnlyPtr();
			return ref unsafeReadOnlyPtr[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LengthCheck(int index, int length)
		{
			if (index < 0 || index >= length)
			{
				FailOutOfRangeError(index, length);
			}
		}

		public static void FailOutOfRangeError(int index, int length)
		{
			throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length.");
		}
	}
}
