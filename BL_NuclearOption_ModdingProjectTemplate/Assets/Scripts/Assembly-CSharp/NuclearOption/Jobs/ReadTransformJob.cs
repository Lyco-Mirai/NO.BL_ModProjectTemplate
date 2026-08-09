using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct ReadTransformJob : IJobParallelForTransform
	{
		[WriteOnly]
		public NativeArray<JobTransformValues> values;

		[NativeDisableUnsafePtrRestriction]
		public Ptr<long> timer;

		public ReadTransformJob(NativeArray<JobTransformValues> values, Ptr<long> timer)
		{
			this.values = values;
			this.timer = timer;
		}

		public void Execute(int i, TransformAccess access)
		{
			JobPerf.GetTimestampBurst();
			if (access.isValid)
			{
				ref JobTransformValues reference = ref values.AsSpan()[i];
				access.GetPositionAndRotation(out reference.Position, out reference.Rotation);
			}
		}
	}
}
