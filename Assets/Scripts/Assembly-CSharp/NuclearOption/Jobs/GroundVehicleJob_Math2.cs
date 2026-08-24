using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct GroundVehicleJob_Math2 : IJobParallelFor
	{
		private static readonly ProfilerMarker executeMarker = new ProfilerMarker("GroundVehicleJob_Math2 Execute");

		private static readonly ProfilerMarker processSampleGroundMarker = new ProfilerMarker("GroundVehicleJob_Math2 ProcessSampleGround");

		private static readonly ProfilerMarker suspensionPhysicsMarker = new ProfilerMarker("GroundVehicleJob_Math2 SuspensionPhysics");

		private const float NO_SURFACE_ALT = 100f;

		[ReadOnly]
		public NativeArray<JobTransformValues> transformValues;

		[ReadOnly]
		public NativeArray<Ptr<GroundVehicleFields>> fields;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Ptr<JobSharedFields> shared;

		internal NativeArray<RaycastHit> rayResults;

		public void Execute(int i)
		{
			JobPerf.GetTimestampBurst();
			Execute(i, ref fields[i].Ref(), ref transformValues.GetReadOnlyRef(i));
		}

		private void Execute(int i, ref GroundVehicleFields fields, ref JobTransformValues.ReadOnly transform)
		{
			if (fields.monoBehaviourEnabled)
			{
				if (GroundVehicleJobSettings.ShouldRunSampleGround(i, shared.Ref().tickOffset))
				{
					ProcessSampleGround(ref fields);
				}
				SuspensionPhysics(ref fields, ref transform);
			}
		}

		private void ProcessSampleGround(ref GroundVehicleFields fields)
		{
			if (fields.sampleGroundResult.didHit)
			{
				Vector3 hitNormal = fields.sampleGroundResult.hitNormal;
				Vector3 inPoint = shared.Ref().datum.ToGlobalPosition(fields.sampleGroundResult.hitPoint);
				fields.surfacePlane.SetNormalAndPosition(hitNormal, inPoint);
			}
			else
			{
				fields.surfacePlane.SetNormalAndPosition(Vector3.up, new Vector3(0f, -100f, 0f));
			}
		}

		private void SuspensionPhysics(ref GroundVehicleFields fields, ref JobTransformValues.ReadOnly transform)
		{
			Vector3 vector;
			if (fields.sampleGroundResult.hasHitRB)
			{
				vector = fields.velocity - fields.sampleGroundResult.hitPointVelocity;
				fields.surfacePlane.Translate(vector * shared.Ref().fixedDeltaTime);
				fields.speed = Vector3.Dot(vector, transform.Forward());
			}
			else
			{
				vector = fields.velocity;
			}
			if (fields.surfacePlane.Raycast(new Ray(shared.Ref().datum.ToGlobalPosition(transform.Position), transform.Down()), out var enter))
			{
				fields.radarAlt = Mathf.Max(enter, 0f);
			}
			else
			{
				fields.radarAlt = 100f;
			}
			if (!(fields.radarAlt > fields.suspensionTravel * 2f))
			{
				float num = fields.springRate * Mathf.Max(fields.suspensionTravel - fields.radarAlt, 0f);
				float num2 = ((num > 0f) ? ((0f - fields.dampingRate) * Mathf.Min(Vector3.Dot(fields.surfacePlane.normal, vector), 0f)) : 0f);
				Vector3 vector2 = fields.surfacePlane.normal * (num + num2);
				Vector3 vector3 = 1.5f * -Vector3.Cross(fields.surfacePlane.normal, transform.Up());
				vector3 -= 0.1f * fields.angularVelocity;
				Vector3 onNormal = Vector3.Cross(transform.Forward(), fields.surfacePlane.normal);
				Vector3 vector4 = new Vector3(0f - vector2.x, 0f, 0f - vector2.z) - vector * fields.mass * 10f;
				Vector3 vector5 = Vector3.Project(-vector * 10f + vector4, onNormal);
				Vector3 vector6 = (-vector * 10f + vector4) * fields.inputs.brake;
				float num3 = (fields.sampleGroundResult.onPaved ? fields.topSpeedOnroad : fields.topSpeedOffroad);
				Vector3 vector7;
				if (Mathf.Abs(fields.speed) < num3 * 0.277777f)
				{
					float num4 = Mathf.Clamp(fields.engineOutput * 40f / Mathf.Max(Mathf.Abs(fields.speed), 1f), (0f - fields.acceleration) * 5f, fields.acceleration * 5f);
					vector7 = transform.Forward() * fields.mass * num4;
				}
				else
				{
					vector7 = Vector3.zero;
				}
				Vector3 vector8 = new Vector3(Mathf.Sign(0f - vector.x) - vector.x * 0.05f, Mathf.Sign(0f - vector.y) - vector.y * 0.05f, Mathf.Sign(0f - vector.z) - vector.z * 0.05f) * (0.05f * (num + num2));
				Vector3 vector9 = Vector3.ClampMagnitude(vector5 + vector6 + vector8, (num + num2) * fields.frictionCoef);
				vector9 += vector7;
				fields.AddForce(vector2 + vector9);
				fields.AddTorque(fields.inputs.steering * 0.2f * transform.Up() + vector3);
			}
		}
	}
}
