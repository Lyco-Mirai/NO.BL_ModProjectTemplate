using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	public class GroundVehicleJobSettings
	{
		private static readonly ProfilerMarker schedule1Marker = new ProfilerMarker("GroundVehicle Schedule1");

		private static readonly ProfilerMarker scheduleQueue1Marker = new ProfilerMarker("GroundVehicle Schedule Queue 1");

		private static readonly ProfilerMarker schedule2Marker = new ProfilerMarker("GroundVehicle Schedule2");

		private static readonly ProfilerMarker schedule2Raycasts = new ProfilerMarker("GroundVehicle ProcessRaycasts");

		private static readonly ProfilerMarker scheduleQueue2Marker = new ProfilerMarker("GroundVehicle Schedule Queue 1");

		private static readonly ProfilerMarker setArgsMarker = new ProfilerMarker("GroundVehicle SetArgs");

		private static readonly ProfilerMarker setArgsFieldsMarker = new ProfilerMarker("GroundVehicle SetArgs_fields");

		private static readonly ProfilerMarker setArgsPathfinderMarker = new ProfilerMarker("GroundVehicle SetArgs_pathfinder");

		private static readonly ProfilerMarker setArgsObstacleMarker = new ProfilerMarker("GroundVehicle SetArgs_obstacle");

		private static readonly ProfilerMarker setArgsFullMarker = new ProfilerMarker("GroundVehicle SetArgs_Full");

		private static readonly ProfilerMarker finishMarker = new ProfilerMarker("GroundVehicle Finish");

		public const float TICK_FREQUENCY = 60f;

		public const int INPUTS_UPDATE_FREQUENCY_TICKS = 12;

		public const int SAMPLE_GROUND_FREQUENCY_TICKS = 6;

		public const float INPUTS_UPDATE_FREQUENCY = 0.2f;

		public const float SAMPLE_GROUND_FREQUENCY = 0.1f;

		public const int BATCH_SIZE = 32;

		private readonly JobPerf jobPerf;

		public GroundVehicle[] partsInJob;

		public TransformAccessArray transformAccess;

		public NativeArray<Ptr<GroundVehicleFields>> fields;

		public NativeArray<JobTransformValues> transformValues;

		public NativeArray<RaycastHit> rayResults;

		public NativeArray<RaycastCommand> rayCommands;

		public int countInJob;

		private JobHandle handleRaycast;

		private JobHandle handle_math2;

		private JobHandle handleAccess;

		private JobHandle handleMath1;

		public GroundVehicleJobSettings(JobPerf jobPerf)
		{
			this.jobPerf = jobPerf;
		}

		public void CheckCapacity(int neededLength)
		{
			GroundVehicle[] array = partsInJob;
			int num = ((array != null) ? array.Length : 0);
			if (num < neededLength)
			{
				int num2 = JobManager.IncreaseCapacity(num, neededLength, 64);
				Array.Resize(ref partsInJob, num2);
				transformAccess.Resize(num2);
				NativeArrayExtensions.Resize(ref fields, num2, copyItems: true);
				NativeArrayExtensions.Resize(ref transformValues, num2, copyItems: false);
				int newLength = (num2 + 6 - 1) / 6;
				NativeArrayExtensions.Resize(ref rayResults, newLength, copyItems: false);
				NativeArrayExtensions.Resize(ref rayCommands, newLength, copyItems: false);
			}
		}

		public void DisposeAll()
		{
			partsInJob.SafeClear();
			transformAccess.DelayDispose();
			NativeArrayExtensions.DelayDispose(ref fields);
			NativeArrayExtensions.DelayDispose(ref transformValues);
			NativeArrayExtensions.DelayDispose(ref rayResults);
			NativeArrayExtensions.DelayDispose(ref rayCommands);
			countInJob = 0;
		}

		public void SetArgs_ProcessAdded(JobUnitList<JobPart<GroundVehicle, GroundVehicleFields>> list)
		{
			using (setArgsFullMarker.Auto())
			{
				JobPart<GroundVehicle, GroundVehicleFields> add;
				int addIndex;
				while (list.ProcessNextAdded(out add, out addIndex))
				{
					Transform jobTransforms = add.Part.GetJobTransforms();
					transformAccess.Add(jobTransforms);
					partsInJob[addIndex] = add.Part;
					fields[addIndex] = add.Field;
					countInJob++;
				}
			}
		}

		public void SetArgs_ProcessRemoved(JobUnitList<JobPart<GroundVehicle, GroundVehicleFields>> list)
		{
			using (setArgsFullMarker.Auto())
			{
				int removeIndex;
				JobPart<GroundVehicle, GroundVehicleFields> removedItem;
				while (list.ProcessNextRemoved(out removeIndex, out removedItem))
				{
					transformAccess.RemoveAtSwapBack(removeIndex);
					int num = countInJob - 1;
					if (num != removeIndex)
					{
						partsInJob[removeIndex] = partsInJob[num];
						fields[removeIndex] = fields[num];
					}
					countInJob--;
				}
			}
		}

		public void SetArgs_Update(Ptr<JobSharedFields> shared)
		{
			using (setArgsMarker.Auto())
			{
				using (setArgsFieldsMarker.Auto())
				{
					for (int i = 0; i < countInJob; i++)
					{
						partsInJob[i].UpdateJobFields();
					}
				}
				int tickOffset = shared.Ref().tickOffset;
				using (setArgsPathfinderMarker.Auto())
				{
					for (int j = 0; j < countInJob; j++)
					{
						if (ShouldRunInputs(j, tickOffset))
						{
							partsInJob[j].UpdateJobFields_Pathfinder();
						}
					}
				}
				using (setArgsObstacleMarker.Auto())
				{
					for (int k = 0; k < countInJob; k++)
					{
						if (ShouldRunInputs(k, tickOffset))
						{
							partsInJob[k].UpdateJobFields_Obstacles();
						}
					}
				}
			}
		}

		public void Schedule_1(JobUnitList<JobPart<GroundVehicle, GroundVehicleFields>> vehicles, Ptr<JobSharedFields> shared)
		{
			using (schedule1Marker.Auto())
			{
				Ptr<GroundVehicleFields>.JobRunningSafety = true;
				if (vehicles.PendingChangesRemove)
				{
					JobPerf.GetTimestamp();
					SetArgs_ProcessRemoved(vehicles);
				}
				if (vehicles.PendingChangesAdd)
				{
					JobPerf.GetTimestamp();
					CheckCapacity(vehicles.CountAfterPending);
					JobPerf.GetTimestamp();
					SetArgs_ProcessAdded(vehicles);
				}
				if (countInJob != 0)
				{
					JobPerf.GetTimestamp();
					SetArgs_Update(shared);
					Schedule_1_inner(shared);
				}
			}
		}

		private void Schedule_1_inner(Ptr<JobSharedFields> shared)
		{
			using (scheduleQueue1Marker.Auto())
			{
				handleAccess = new ReadTransformJob(transformValues, shared.VehicleAccessPtr()).ScheduleReadOnly(transformAccess, 32);
				GroundVehicleJob_Math1 jobData = new GroundVehicleJob_Math1
				{
					fields = fields,
					transformValues = transformValues,
					shared = shared,
					rayCommands = rayCommands
				};
				handleMath1 = IJobParallelForExtensions.ScheduleByRef(ref jobData, countInJob, 32, handleAccess);
				int length = SampleGroundRayCount(shared.Ref().tickOffset);
				handleRaycast = RaycastCommand.ScheduleBatch(rayCommands.GetSubArray(0, length), rayResults.GetSubArray(0, length), 32, 1, handleMath1);
				JobHandle.ScheduleBatchedJobs();
			}
		}

		public void Schedule_2(Ptr<JobSharedFields> shared)
		{
			if (countInJob == 0)
			{
				return;
			}
			using (schedule2Marker.Auto())
			{
				JobPerf.GetTimestamp();
				handleAccess.Complete();
				handleMath1.Complete();
				handleRaycast.Complete();
				JobPerf.GetTimestamp();
				ProcessRayCasts(shared.Ref().tickOffset);
				using (scheduleQueue2Marker.Auto())
				{
					GroundVehicleJob_Math2 jobData = new GroundVehicleJob_Math2
					{
						fields = fields,
						transformValues = transformValues,
						shared = shared,
						rayResults = rayResults
					};
					handle_math2 = IJobParallelForExtensions.ScheduleByRef(ref jobData, countInJob, 32, handleRaycast);
				}
				JobHandle.ScheduleBatchedJobs();
			}
		}

		private void ProcessRayCasts(int tickOffset)
		{
			using (schedule2Raycasts.Auto())
			{
				for (int i = 0; i < countInJob; i++)
				{
					ref GroundVehicleFields reference = ref fields[i].Ref();
					if (!reference.monoBehaviourEnabled)
					{
						continue;
					}
					GroundVehicle groundVehicle = partsInJob[i];
					ref SampleGroundResult sampleGroundResult = ref reference.sampleGroundResult;
					ref JobTransformValues.ReadOnly readOnlyRef = ref transformValues.GetReadOnlyRef(i);
					if (ShouldRunSampleGround(i, tickOffset))
					{
						RaycastHit raycastHit = rayResults[SampleGroundIndex(i)];
						sampleGroundResult.didHit = raycastHit.colliderInstanceID != 0;
						if (sampleGroundResult.didHit)
						{
							sampleGroundResult.hitNormal = raycastHit.normal;
							sampleGroundResult.hitPoint = raycastHit.point;
							if (raycastHit.collider.sharedMaterial == GameAssets.i.terrainMaterial)
							{
								if (GameManager.ShowEffects)
								{
									groundVehicle.ContactDustParticles(raycastHit.point);
								}
								sampleGroundResult.onPaved = false;
							}
							else
							{
								sampleGroundResult.onPaved = true;
							}
						}
						Rigidbody rigidbody = (sampleGroundResult.didHit ? raycastHit.collider.attachedRigidbody : null);
						if (rigidbody != null)
						{
							sampleGroundResult.hasHitRB = true;
							groundVehicle.hitRB = rigidbody;
						}
						else
						{
							sampleGroundResult.hasHitRB = false;
						}
					}
					if (sampleGroundResult.hasHitRB)
					{
						Rigidbody hitRB = groundVehicle.hitRB;
						if (hitRB == null)
						{
							sampleGroundResult.hasHitRB = false;
							continue;
						}
						Vector3 angularVelocity = hitRB.angularVelocity;
						groundVehicle.rb.angularVelocity = angularVelocity;
						reference.angularVelocity = angularVelocity;
						sampleGroundResult.hitPointVelocity = hitRB.GetPointVelocity(readOnlyRef.Position - readOnlyRef.Up() * 100f);
					}
				}
			}
		}

		public void FinishJob()
		{
			if (countInJob == 0)
			{
				return;
			}
			JobPerf.GetTimestamp();
			using (finishMarker.Auto())
			{
				handle_math2.Complete();
				Ptr<GroundVehicleFields>.JobRunningSafety = false;
				try
				{
					for (int i = 0; i < countInJob; i++)
					{
						partsInJob[i].ApplyJobFields();
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ShouldRunInputs(int i, int tickOffset)
		{
			return (i + tickOffset) % 12 == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ShouldRunSampleGround(int i, int tickOffset)
		{
			return (i + tickOffset) % 6 == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SampleGroundIndex(int i)
		{
			return i / 6;
		}

		public int SampleGroundRayCount(int tickOffset)
		{
			int num = (6 - tickOffset % 6) % 6;
			if (num >= countInJob)
			{
				return 0;
			}
			return (countInJob - 1 - num) / 6 + 1;
		}
	}
}
