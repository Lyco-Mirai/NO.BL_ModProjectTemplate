using System;
using UnityEngine;

namespace NuclearOption.Effects
{
	[Serializable]
	public struct MinMax
	{
		public float Min;

		public float Max;

		public MinMax(float min, float max)
		{
			Min = min;
			Max = max;
		}

		public float GetRange()
		{
			return Max - Min;
		}

		public MinRange ToMinRange()
		{
			return new MinRange(Min, Max);
		}

		public MinInvRange ToMinInvRange()
		{
			return new MinInvRange(Min, Max);
		}

		public Vector4 ToVector4()
		{
			return new Vector4(Min, Max, 0f, 0f);
		}
	}
}
