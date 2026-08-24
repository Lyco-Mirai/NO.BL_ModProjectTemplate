using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct SetLocalRotationJob : IJobParallelForTransform
	{
		[ReadOnly]
		public NativeArray<Quaternion> rotations;

		[NativeDisableUnsafePtrRestriction]
		public Ptr<long> timer;

		public SetLocalRotationJob(NativeArray<Quaternion> rotations, Ptr<long> timer)
		{
			this.rotations = rotations;
			this.timer = timer;
		}

		public void Execute(int i, TransformAccess access)
		{
			JobPerf.GetTimestampBurst();
			if (access.isValid)
			{
				access.localRotation = rotations[i];
			}
		}
	}
}
