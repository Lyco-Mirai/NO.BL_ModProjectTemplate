using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct ControlSurfaceJob_Math : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<Ptr<ControlSurfaceFields>> fields;

		[WriteOnly]
		public NativeArray<Quaternion> rotations;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Ptr<JobSharedFields> shared;

		private ref Quaternion GetRotation(int index)
		{
			return ref rotations.AsSpan()[index];
		}

		public void Execute(int i)
		{
			JobPerf.GetTimestampBurst();
			ref ControlSurfaceFields reference = ref fields[i].Ref();
			if (IndexLink.BurstGetTransformIndex(reference.visibleTransformLink, out var index))
			{
				int index2 = 0;
				int index3 = 0;
				if (!(reference.maxSplit > 0f) || (IndexLink.BurstGetTransformIndex(reference.upperTransformLink, out index2) && IndexLink.BurstGetTransformIndex(reference.lowerTransformLink, out index3)))
				{
					Execute(ref reference, ref GetRotation(index), index2, index3);
				}
			}
		}

		public void Execute(ref ControlSurfaceFields fields, ref Quaternion mainRotation, int upperIndex, int lowerIndex)
		{
			if (fields.IsDetached)
			{
				return;
			}
			float fixedDeltaTime = shared.Ref().fixedDeltaTime;
			ref ControlInputsBurst controlInputs = ref fields.controlInputs;
			if (!fields.flap)
			{
				float value = controlInputs.pitch * fields.pitchRange - fields.currentPitch;
				fields.currentPitch += Mathf.Clamp(value, (0f - fields.servoSpeed) * fixedDeltaTime, fields.servoSpeed * fixedDeltaTime);
				fields.currentPitch = Mathf.Clamp(fields.currentPitch, 0f - Mathf.Abs(fields.pitchRange), Mathf.Abs(fields.pitchRange));
				float value2 = controlInputs.roll * fields.rollRange - fields.currentRoll;
				fields.currentRoll += Mathf.Clamp(value2, (0f - fields.servoSpeed) * fixedDeltaTime, fields.servoSpeed * fixedDeltaTime);
				fields.currentRoll = Mathf.Clamp(fields.currentRoll, 0f - Mathf.Abs(fields.rollRange), Mathf.Abs(fields.rollRange));
				float value3 = controlInputs.yaw * fields.yawRange - fields.currentYaw;
				fields.currentYaw += Mathf.Clamp(value3, (0f - fields.servoSpeed) * fixedDeltaTime, fields.servoSpeed * fixedDeltaTime);
				fields.currentYaw = Mathf.Clamp(fields.currentYaw, 0f - Mathf.Abs(fields.yawRange), Mathf.Abs(fields.yawRange));
			}
			else
			{
				if (fields.gearState == LandingGear.GearState.Extending || fields.gearState == LandingGear.GearState.LockedExtended)
				{
					fields.currentPitch -= fields.servoSpeed * fixedDeltaTime;
				}
				else
				{
					fields.currentPitch += fields.servoSpeed * fixedDeltaTime;
				}
				fields.currentPitch = Mathf.Clamp(fields.currentPitch, 0f - fields.pitchRange, 0f);
			}
			float angle = fields.currentPitch + fields.currentRoll + fields.currentYaw;
			mainRotation = fields.restingRotation * Quaternion.AngleAxis(angle, Vector3.right);
			if (fields.maxSplit > 0f)
			{
				float num = ((controlInputs.throttle == 0f) ? fields.maxSplit : 0f);
				num = Mathf.Clamp(num + fields.yawSplitFactor * controlInputs.yaw * fields.maxSplit, 0f, fields.maxSplit);
				fields.splitAmount += Mathf.Clamp(num - fields.splitAmount, (0f - fields.servoSpeed) * fixedDeltaTime, fields.servoSpeed * fixedDeltaTime);
				GetRotation(upperIndex) = fields.restingSplitRotation * Quaternion.AngleAxis(fields.splitAmount, Vector3.right);
				GetRotation(lowerIndex) = fields.restingSplitRotation * Quaternion.AngleAxis(0f - fields.splitAmount, Vector3.right);
			}
		}
	}
}
