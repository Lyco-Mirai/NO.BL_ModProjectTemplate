using System;
using System.Collections.Generic;

namespace NuclearOption.SavedMission
{
	[Serializable]
	public struct Override<T> : IEquatable<Override<T>> where T : IEquatable<T>
	{
		public bool IsOverride;

		public T Value;

		public Override(bool isOverride, T value)
		{
			IsOverride = isOverride;
			Value = value;
		}

		public override bool Equals(object obj)
		{
			if (obj is Override<T> other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (IsOverride ? 1 : 0) | (EqualityComparer<T>.Default.GetHashCode(Value) << 1);
		}

		public bool Equals(Override<T> other)
		{
			if (IsOverride == other.IsOverride)
			{
				return EqualityComparer<T>.Default.Equals(Value, other.Value);
			}
			return false;
		}

		public static void SetAssert(ref Override<T> value, T newValue)
		{
			value = new Override<T>(isOverride: true, newValue);
		}

		public static Override<T> NewAssert(Override<T> value, T newValue)
		{
			value = new Override<T>(isOverride: true, newValue);
			return value;
		}
	}
}
