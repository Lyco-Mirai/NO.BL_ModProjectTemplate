using JamesFrowen.Graphy;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class SendTransformBatcherExtrapolationTimeSource : GraphDataSource
	{
		public SendTransformBatcher SendTransformBatcher;

		public override float GetNewValue()
		{
			if (SendTransformBatcher == null)
			{
				SendTransformBatcher = Object.FindObjectOfType<SendTransformBatcher>();
				if (SendTransformBatcher == null)
				{
					return 0f;
				}
			}
			float num = (float)(SendTransformBatcher.Debug_extrapolationOffset * 1000.0);
			if (!(num >= 0f))
			{
				return 0f;
			}
			return num;
		}
	}
}
