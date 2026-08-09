using UnityEngine;

namespace NuclearOption.Effects
{
	public struct MinRange
	{
		public float Min;

		public float Range;

		public MinRange(float min, float max)
		{
			Min = min;
			Range = max - min;
		}

		public static implicit operator Vector4(MinRange r)
		{
			return new Vector4(r.Min, r.Range, 0f, 0f);
		}
	}
}
