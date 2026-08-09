using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NuclearOption.Jobs
{
	[BurstCompile]
	public struct AeroJob_Math : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<float> liftCharts;

		[ReadOnly]
		public NativeArray<float> dragCharts;

		[ReadOnly]
		public NativeArray<float> airDensityChart;

		public Vector3 windVelocity;

		public float windTurbulence;

		[ReadOnly]
		public NativeArray<Ptr<AeroPartFields>> fields;

		[ReadOnly]
		public NativeArray<JobTransformValues> transformValues;

		[ReadOnly]
		[NativeDisableUnsafePtrRestriction]
		public Ptr<JobSharedFields> shared;

		public void Execute(int i)
		{
			JobPerf.GetTimestampBurst();
			ref AeroPartFields reference = ref fields[i].Ref();
			if (IndexLink.BurstGetTransformIndex(reference.liftTransformIndex, out var index))
			{
				int index2 = 0;
				if (!(reference.airflowChanneling > 0f) || IndexLink.BurstGetTransformIndex(reference.otherTransformIndex, out index2))
				{
					Execute(ref reference, ref transformValues.GetReadOnlyRef(index), index2);
				}
			}
		}

		private void Execute(ref AeroPartFields fields, ref JobTransformValues.ReadOnly liftTransform, int otherTransformIndex)
		{
			Vector3 vector = fields.velocity;
			float num = fields.wingEffectiveness;
			float num2 = 0f;
			float num3 = fields.buoyancy;
			Vector3 vector2 = Vector3.zero;
			bool splashed = false;
			Vector3 globalPosition = shared.Ref().datum.ToGlobalPosition(liftTransform.Position);
			float y = globalPosition.y;
			float num4 = GetAirDensity(y);
			float num5 = Mathf.Max(fields.submergedAmount, 0f - (globalPosition.y - fields.collisionSize.y));
			float num6 = Mathf.Clamp01(num5 / (fields.collisionSize.y * 2f));
			if (num5 > 0f)
			{
				num2 = num6;
				Vector3 vector3 = new Vector3(fields.velocity.x, 0f, fields.velocity.z);
				if (fields.velocity.y > -4f)
				{
					vector2 += 3f * num6 * fields.mass * vector3.magnitude * Vector3.up;
				}
				num3 = Mathf.Max(num3 - 0.2f * num6 * shared.Ref().fixedDeltaTime, 0f);
				Vector3 vector4 = num6 * -fields.velocity * fields.mass * 5f;
				vector2 += new Vector3(vector4.x, vector4.y, vector4.z) + 9.81f * fields.buoyancy * fields.mass * num6 * Vector3.up;
				if (shared.Ref().timeSinceLevelLoad - fields.lastSplashTime > 2f && math.lengthsq(vector) > 400f)
				{
					splashed = true;
					fields.lastSplashTime = shared.Ref().timeSinceLevelLoad;
				}
				num = Mathf.Lerp(num, 0.01f, num6);
				num4 = Mathf.Lerp(num4, 1000f, num6);
			}
			Vector3 vector5 = CalcWind(globalPosition);
			vector -= new Vector3(vector5.x, vector5.y, vector5.z) * (1f - num6);
			float num7 = math.lengthsq(vector);
			if (fields.airflowChanneling > 0f)
			{
				Vector3 target = transformValues.GetReadOnlyRef(otherTransformIndex).Forward();
				vector = Vector3.RotateTowards(vector, target, fields.airflowChanneling, 0f);
			}
			Vector3 vector6 = Quaternion.Inverse(liftTransform.Rotation) * vector;
			Vector3 normalized = Vector3.Cross(vector, liftTransform.Right()).normalized;
			GetLiftDragCoef(alpha: Mathf.Atan2(vector6.y, vector6.z), airfoilID: fields.airfoilID, liftCoef: out var liftCoef, dragCoef: out var dragCoef);
			float num8 = (fields.dragArea + fields.wingArea * 0.1f) * (1f - num);
			float num9 = 0.5f * num4 * num7 * 0.5f * (fields.dragArea + num8);
			float num10 = dragCoef * num4 * num7 * 0.5f * fields.wingArea * num;
			float3 float5 = -math.normalize(vector) * (num10 + num9);
			float num11 = math.length(vector);
			float speedOfSound = LevelInfo.GetSpeedOfSound(globalPosition.y);
			if (num11 > speedOfSound * 0.8f && num11 < speedOfSound * 1.2f)
			{
				float num12 = 0.2f;
				float num13 = 0.15f;
				float num14 = Mathf.Min(Mathf.Abs((speedOfSound - num11) / speedOfSound), num12);
				float num15 = (num12 - num14) / num12;
				float5 *= 1f + num15 * num15 * num15 * num13;
			}
			Vector3 vector7 = -normalized * (liftCoef * num4 * num7 * 0.5f * fields.wingArea * num);
			vector2 += vector7 + new Vector3(float5.x, float5.y, float5.z);
			if (num5 > 0f)
			{
				vector2 = Vector3.ClampMagnitude(vector2, math.length(fields.velocity) * 0.5f * fields.mass * 60f);
			}
			if (globalPosition.y < -100f)
			{
				vector2 += Vector3.up * fields.mass * 20f;
			}
			if (!float.IsFinite(vector2.x) || !float.IsFinite(vector2.y) || !float.IsFinite(vector2.z))
			{
				vector2 = Vector3.zero;
			}
			JobForceType hasForce;
			if (vector2 != Vector3.zero)
			{
				fields.force = vector2;
				if (fields.centerOfLift != Vector3.zero)
				{
					hasForce = JobForceType.ForceAndTorque;
					Vector3 vector8 = liftTransform.Rotation * fields.centerOfLift;
					fields.torque = Vector3.Cross(vector2, -vector8);
				}
				else
				{
					hasForce = JobForceType.Force;
				}
			}
			else
			{
				hasForce = JobForceType.NoForce;
			}
			fields.hasForce = hasForce;
			fields.splashed = splashed;
			fields.buoyancy = num3;
			bool flag = fields.angularDrag != num2;
			if (flag)
			{
				fields.angularDrag = num2;
			}
			fields.angularDragChanged = flag;
		}

		private void GetLiftDragCoef(int airfoilID, float alpha, out float liftCoef, out float dragCoef)
		{
			if (airfoilID < 0)
			{
				liftCoef = 1.8f * Mathf.Sin(5f * alpha);
				dragCoef = 1.5f * (1f - Mathf.Cos(2f * alpha)) + 0.02f;
				return;
			}
			ReadOnlySpan<float> readOnlySpan = liftCharts.AsReadOnlySpan();
			ReadOnlySpan<float> chart = readOnlySpan.Slice(airfoilID * 128, 128);
			readOnlySpan = dragCharts.AsReadOnlySpan();
			ReadOnlySpan<float> chart2 = readOnlySpan.Slice(airfoilID * 128, 128);
			float index = alpha * 20.37185f + 64f;
			liftCoef = ChartHelper.SafeRead(index, chart);
			dragCoef = ChartHelper.SafeRead(index, chart2);
		}

		public float GetAirDensity(float altitude)
		{
			return ChartHelper.SafeRead(altitude * 0.0021f, airDensityChart.AsReadOnlySpan());
		}

		private Vector3 CalcWind(Vector3 globalPosition)
		{
			Vector3 vector = shared.Ref().timeSinceLevelLoad * Vector3.one * 10f;
			Vector3 normalized = windVelocity.normalized;
			globalPosition += vector;
			Vector3 vector2 = (-0.75f + Mathf.PerlinNoise1D(globalPosition.z * 0.02f) + 0.5f * Mathf.PerlinNoise1D(globalPosition.z * 0.1f)) * normalized + 0.5f * (-0.75f + Mathf.PerlinNoise1D(globalPosition.x * 0.02f) + 0.5f * Mathf.PerlinNoise1D(globalPosition.x * 0.1f)) * Vector3.Cross(normalized, Vector3.up) + 0.3f * (-0.75f + Mathf.PerlinNoise1D(globalPosition.y * 0.02f) + 0.5f * Mathf.PerlinNoise1D(globalPosition.y * 0.1f)) * Vector3.up;
			return windVelocity + windTurbulence * Mathf.Max(windVelocity.magnitude, 10f) * vector2;
		}
	}
}
