using JamesFrowen.Graphy;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class SendTransformBatcherInterpolationDeltaTimeSource : GraphDataSource
	{
		public SendTransformBatcher SendTransformBatcher;

		private double previous;

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
			double serverSmoothTime = SendTransformBatcher.ServerSmoothTime;
			double num = serverSmoothTime - previous;
			previous = serverSmoothTime;
			float num2 = (float)(num * 1000.0);
			if (!(num2 >= 0f))
			{
				return 0f;
			}
			return num2;
		}
	}
}
