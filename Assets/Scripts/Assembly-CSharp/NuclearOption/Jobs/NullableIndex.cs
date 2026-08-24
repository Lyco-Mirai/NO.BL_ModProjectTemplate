using System;
using System.Runtime.CompilerServices;

namespace NuclearOption.Jobs
{
	public readonly struct NullableIndex
	{
		private readonly uint raw;

		public bool HasValue
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (raw & 1) != 0;
			}
		}

		public int Index
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (int)(raw >> 1);
			}
		}

		public NullableIndex(int index)
		{
			if (index < 0)
			{
				throw new ArgumentException("Index can not be negative");
			}
			raw = (uint)((index << 1) | 1);
		}

		public override string ToString()
		{
			if (!HasValue)
			{
				return "NoIndex";
			}
			return $"{Index}";
		}
	}
}
