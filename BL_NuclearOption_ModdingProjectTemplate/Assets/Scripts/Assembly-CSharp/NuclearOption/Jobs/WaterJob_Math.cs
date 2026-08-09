using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct WaterJob_Math : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<Ptr<ShipPartFields>> fields;

		[ReadOnly]
		public NativeArray<JobTransformValues> transformValues;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Ptr<JobSharedFields> shared;

		public void Execute(int i)
		{
			JobPerf.GetTimestampBurst();
			Execute(ref fields[i].Ref(), ref transformValues.GetReadOnlyRef(i));
		}

		private void Execute(ref ShipPartFields fields, ref JobTransformValues.ReadOnly transform)
		{
			float num = shared.Ref().datum.GlobalY(transform.Position);
			float submergedAmount = Mathf.Clamp01((fields.partHeight * 0.5f - num) / fields.partHeight);
			Vector3 force = Buoyancy(submergedAmount, fields.displacement) * Vector3.up;
			force += Drag(submergedAmount, ref fields, ref transform);
			if (!float.IsFinite(force.x) || !float.IsFinite(force.y) || !float.IsFinite(force.z))
			{
				Debug.LogError("Non-finite force from WaterJob");
				force = Vector3.zero;
			}
			fields.forcePosition = transform.Position;
			fields.force = force;
			fields.submergedAmount = submergedAmount;
		}

		private float Buoyancy(float submergedAmount, float displacement)
		{
			return Mathf.Lerp(1.2f, 1000f, submergedAmount) * 9.81f * displacement;
		}

		private Vector3 Drag(float submergedAmount, ref ShipPartFields fields, ref JobTransformValues.ReadOnly transform)
		{
			Vector3 vector = transform.Forward();
			Vector3 vector2 = transform.Right();
			Vector3 vector3 = transform.Up();
			float num = Vector3.Dot(fields.velocity, vector);
			float num2 = Vector3.Dot(fields.velocity, vector2);
			float num3 = Vector3.Dot(fields.velocity, vector3);
			float num4 = num * Mathf.Abs(num) * fields.directionalDrag.z * fields.mass * submergedAmount;
			float num5 = num2 * Mathf.Abs(num2) * fields.directionalDrag.x * fields.mass * submergedAmount;
			float num6 = num3 * Mathf.Abs(num3) * fields.directionalDrag.y * fields.mass * submergedAmount;
			return Vector3.ClampMagnitude(vector * (0f - num4) + vector2 * (0f - num5) + vector3 * (0f - num6), fields.mass * 500f);
		}
	}
}
