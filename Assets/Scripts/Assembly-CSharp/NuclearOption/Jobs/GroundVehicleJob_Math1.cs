using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct GroundVehicleJob_Math1 : IJobParallelFor
	{
		private static readonly ProfilerMarker executeMarker = new ProfilerMarker("GroundVehicleJob_Math1 Execute");

		private static readonly ProfilerMarker inputsMarker = new ProfilerMarker("GroundVehicleJob_Math1 Inputs");

		private static readonly ProfilerMarker avoidObstaclesMarker = new ProfilerMarker("GroundVehicleJob_Math1 AvoidObstacles");

		private static readonly QueryParameters queryParam = new QueryParameters(~(int)PhysicsLayers.ExclusionZonesMask);

		[ReadOnly]
		public NativeArray<JobTransformValues> transformValues;

		[ReadOnly]
		public NativeArray<Ptr<GroundVehicleFields>> fields;

		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public NativeArray<RaycastCommand> rayCommands;

		[NativeDisableUnsafePtrRestriction]
		public Ptr<JobSharedFields> shared;

		public void Execute(int i)
		{
			JobPerf.GetTimestampBurst();
			ref GroundVehicleFields reference = ref fields[i].Ref();
			ref JobTransformValues.ReadOnly readOnlyRef = ref transformValues.GetReadOnlyRef(i);
			Execute(i, ref reference, ref readOnlyRef);
			UpdateRayCommands(i, ref reference, ref readOnlyRef);
		}

		private void Execute(int i, ref GroundVehicleFields fields, ref JobTransformValues.ReadOnly transform)
		{
			if (!fields.monoBehaviourEnabled)
			{
				return;
			}
			fields.ResetForceResults();
			fields.speed = Vector3.Dot(fields.velocity, transform.Forward());
			if (fields.unitDisabled && !fields.unitWasDisabled)
			{
				fields.unitWasDisabled = true;
				fields.acceleration = 0f;
				fields.inputs.steering = 0f;
				fields.inputs.brake = 0f;
				fields.stationary = false;
				fields.stationaryTime = 0f;
			}
			else if (!fields.unitDisabled && fields.unitWasDisabled)
			{
				fields.unitWasDisabled = false;
			}
			if (Mathf.Abs(fields.speed) < 0.3f && fields.sampleGroundResult.didHit && !fields.sampleGroundResult.hasHitRB)
			{
				fields.stationaryTime += shared.Ref().fixedDeltaTime;
			}
			else
			{
				fields.stationaryTime = 0f;
			}
			fields.stationary = fields.stationaryTime > 5f && fields.inputs.throttle == 0f;
			if (GroundVehicleJobSettings.ShouldRunInputs(i, shared.Ref().tickOffset))
			{
				Inputs(ref fields, ref transform);
				if (!fields.disabled)
				{
					fields.inputs.throttle = Mathf.Clamp(fields.inputs.throttle, -1f, 1f);
					fields.inputs.brake = Mathf.Clamp01(1f - Mathf.Abs(fields.inputs.throttle));
				}
				else
				{
					float num = Mathf.Clamp(10f * Mathf.Sin(shared.Ref().timeSinceLevelLoad), -1f, 1f);
					num -= fields.angularVelocity.y * 0.2f;
					num *= Mathf.Clamp01(Mathf.Abs(fields.speed) * 0.05f);
					fields.inputs.steering = num * 0.5f;
				}
			}
			fields.engineOutput = fields.inputs.throttle * fields.acceleration;
		}

		private void UpdateRayCommands(int i, ref GroundVehicleFields fields, ref JobTransformValues.ReadOnly transform)
		{
			if (GroundVehicleJobSettings.ShouldRunSampleGround(i, shared.Ref().tickOffset))
			{
				if (fields.monoBehaviourEnabled)
				{
					Vector3 position = transform.Position;
					Vector3 direction = transform.Down();
					float distance = fields.suspensionTravel * 3f;
					rayCommands[GroundVehicleJobSettings.SampleGroundIndex(i)] = new RaycastCommand(position, direction, queryParam, distance);
				}
				else
				{
					rayCommands[GroundVehicleJobSettings.SampleGroundIndex(i)] = default(RaycastCommand);
				}
			}
		}

		private void Inputs(ref GroundVehicleFields fields, ref JobTransformValues.ReadOnly transform)
		{
			if (!fields.underwater && shared.Ref().datum.GlobalY(transform.Position) < -2f)
			{
				fields.underwater = true;
			}
			if (!fields.mobile)
			{
				return;
			}
			fields.inputs.throttle = 0f;
			float y = fields.angularVelocity.y;
			if (fields.disabled)
			{
				fields.inputs.brake += 0.004f;
			}
			else
			{
				if (!fields.steeringInfoNullable.HasValue)
				{
					return;
				}
				SteeringInfo value = fields.steeringInfoNullable.Value;
				float num = Mathf.Max((value.nextWaypointAngle - 10f) * 0.1f, 0.1f);
				Vector3 steerDirection = value.steerVector.normalized;
				float num2 = Mathf.Clamp(80f / num, 30f, fields.topSpeedOnroad);
				if (Vector3.Dot(steerDirection, transform.Forward()) < 0.5f)
				{
					num2 = 30f;
				}
				if (fields.speed > num2 * 0.277777f)
				{
					fields.inputs.brake += Mathf.Clamp01((fields.speed - num2 * 0.277f) * 0.1f);
					fields.inputs.throttle = 0f;
				}
				else
				{
					fields.inputs.throttle = 1f;
					if (fields.speed < -10f)
					{
						fields.inputs.throttle = 0f;
						fields.inputs.brake = 0.5f;
					}
				}
				AvoidObstacles(ref fields, ref transform, ref steerDirection, out var throttleInhibit);
				fields.bulldozeTimer += throttleInhibit * 0.2f;
				fields.bulldozeTimer -= 0.020000001f;
				fields.bulldozeTimer = Mathf.Clamp(fields.bulldozeTimer, 0f, 3f);
				if (fields.bulldozeTimer < 1f)
				{
					fields.inputs.throttle -= Mathf.Min(throttleInhibit, 0.5f);
				}
				if (fields.reverseTimer > 0f)
				{
					fields.reverseTimer -= 0.2f;
					fields.inputs.throttle = -1f;
					float num3 = Mathf.Clamp(10f * Mathf.Sin(shared.Ref().timeSinceLevelLoad), -1f, 1f);
					num3 *= Mathf.Clamp01(Mathf.Abs(fields.speed) * 0.05f);
					fields.inputs.steering = num3;
					fields.inputs.steering -= y * 0.2f;
				}
				if (Mathf.Abs(fields.speed) < 1f && fields.inputs.throttle > 0f)
				{
					fields.stuckTimer += 0.2f;
				}
				if (fields.stuckTimer > 2f)
				{
					fields.reverseTimer = 3f;
					fields.stuckTimer = 0f;
				}
				if (fields.reverseTimer <= 0f)
				{
					fields.inputs.steering = 0.05f * Mathf.Clamp(TargetCalc.GetAngleOnAxis(transform.Forward(), steerDirection, transform.Up()), -10f, 10f);
					fields.inputs.steering -= y * 0.2f;
				}
			}
		}

		private void AvoidObstacles(ref GroundVehicleFields fields, ref JobTransformValues.ReadOnly transform, ref Vector3 steerDirection, out float throttleInhibit)
		{
			throttleInhibit = 0f;
			int num = 0;
			Vector3 zero = Vector3.zero;
			Vector3 vector = Vector3.zero;
			for (int i = 0; i < fields.ObstaclesArray.Length; i++)
			{
				ObstaclePosition obstaclePosition = fields.ObstaclesArray[i];
				if (obstaclePosition.Radius == 0f)
				{
					continue;
				}
				Vector3 vector2 = FastMath.Direction(transform.Position, obstaclePosition.Position);
				float sqrMagnitude = vector2.sqrMagnitude;
				float num2 = fields.maxRadius + obstaclePosition.Radius;
				if (sqrMagnitude > (num2 + 50f) * (num2 + 50f) || Vector3.Dot(vector2, steerDirection) < -1f)
				{
					continue;
				}
				if (obstaclePosition.Radius < 8f)
				{
					vector = -vector2.normalized;
					float num3 = Mathf.Max(Vector3.Dot(-vector, transform.Forward()), 0f);
					vector *= Mathf.Min(100f * (1f + num3) / sqrMagnitude, 0.5f);
					throttleInhibit += num3 / Mathf.Max(sqrMagnitude * 0.1f, 2f);
					continue;
				}
				Vector3 vector3 = transform.Position + Vector3.Project(vector2, steerDirection);
				if (fields.DEBUG_VIS && shared.Ref().NextDebugIndex(out var marker))
				{
					marker.Ref() = new DebugVisJobMarker
					{
						Type = DebugVisJobMarkerType.DebugPoint,
						Position = vector3 + Vector3.up * 15f
					};
				}
				float num4 = obstaclePosition.Position.y + obstaclePosition.Top;
				if (vector3.y < num4 && FastMath.InRange(vector3, obstaclePosition.Position, num2))
				{
					Vector3 vector4 = obstaclePosition.Position + FastMath.NormalizedDirection(obstaclePosition.Position, vector3) * num2;
					zero += FastMath.NormalizedDirection(transform.Position, vector4);
					num++;
					if (fields.DEBUG_VIS && shared.Ref().NextDebugIndex(out var marker2))
					{
						marker2.Ref() = new DebugVisJobMarker
						{
							Type = DebugVisJobMarkerType.DebugArrowGreen,
							Position = vector3 + Vector3.up * 15f,
							Rotation = Quaternion.LookRotation(vector4 - vector3),
							Scale = new Vector3(2f, 2f, (vector4 - vector3).magnitude)
						};
					}
				}
			}
			if (num > 0)
			{
				steerDirection = zero;
			}
			steerDirection += vector;
		}
	}
}
