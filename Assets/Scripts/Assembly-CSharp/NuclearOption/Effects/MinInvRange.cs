using UnityEngine;

namespace NuclearOption.Effects
{
	public readonly struct MinInvRange
	{
		public readonly float Min;

		public readonly float InvRange;

		public MinInvRange(float min, float max)
		{
			Min = min;
			float num = max - min;
			InvRange = ((Mathf.Abs(num) > 0.0001f) ? (1f / num) : 0f);
		}

		public static implicit operator Vector4(MinInvRange r)
		{
			return new Vector4(r.Min, r.InvRange, 0f, 0f);
		}
	}
}
