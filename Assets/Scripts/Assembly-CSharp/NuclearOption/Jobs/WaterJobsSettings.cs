using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	public class WaterJobsSettings
	{
		private static readonly ProfilerMarker scheduleMarker = new ProfilerMarker("WaterJob Schedule");

		private static readonly ProfilerMarker finishMarker = new ProfilerMarker("WaterJob Finish");

		private static readonly ProfilerMarker setArgsMarker = new ProfilerMarker("WaterJob SetArgs");

		private static readonly ProfilerMarker setArgsFullMarker = new ProfilerMarker("WaterJob SetArgs_Full");

		public const int BATCH_SIZE = 32;

		private readonly JobPerf jobPerf;

		private ShipPart[] partsInJob;

		public TransformAccessArray transformAccess;

		public NativeArray<Ptr<ShipPartFields>> fields;

		public NativeArray<JobTransformValues> transformValues;

		private int countInJob;

		private JobHandle handleAccess;

		private JobHandle handleMath;

		public WaterJobsSettings(JobPerf jobPerf)
		{
			this.jobPerf = jobPerf;
		}

		private void CheckCapacity(int neededLength)
		{
			ShipPart[] array = partsInJob;
			int num = ((array != null) ? array.Length : 0);
			if (num < neededLength)
			{
				int num2 = JobManager.IncreaseCapacity(num, neededLength, 64);
				Array.Resize(ref partsInJob, num2);
				transformAccess.Resize(num2);
				NativeArrayExtensions.Resize(ref fields, num2, copyItems: true);
				NativeArrayExtensions.Resize(ref transformValues, num2, copyItems: false);
			}
		}

		public void DisposeAll()
		{
			partsInJob.SafeClear();
			transformAccess.DelayDispose();
			NativeArrayExtensions.DelayDispose(ref fields);
			NativeArrayExtensions.DelayDispose(ref transformValues);
			countInJob = 0;
		}

		public void SetArgs_Added(JobUnitList<JobPart<ShipPart, ShipPartFields>> list)
		{
			using (setArgsFullMarker.Auto())
			{
				JobPart<ShipPart, ShipPartFields> add;
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

		public void SetArgs_Removed(JobUnitList<JobPart<ShipPart, ShipPartFields>> list)
		{
			using (setArgsFullMarker.Auto())
			{
				int removeIndex;
				JobPart<ShipPart, ShipPartFields> removedItem;
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

		public void SetArgs_Update()
		{
			using (setArgsMarker.Auto())
			{
				for (int i = 0; i < countInJob; i++)
				{
					partsInJob[i].UpdateJobFields(ref transformValues.GetReadOnlyRef(i));
				}
			}
		}

		public void Schedule_1(JobUnitList<JobPart<ShipPart, ShipPartFields>> shipParts, Ptr<JobSharedFields> shared)
		{
			using (scheduleMarker.Auto())
			{
				if (shipParts.PendingChangesRemove)
				{
					JobPerf.GetTimestamp();
					SetArgs_Removed(shipParts);
				}
				if (shipParts.PendingChangesAdd)
				{
					JobPerf.GetTimestamp();
					CheckCapacity(shipParts.CountAfterPending);
					JobPerf.GetTimestamp();
					SetArgs_Added(shipParts);
				}
				if (countInJob != 0)
				{
					handleAccess = new ReadTransformJob(transformValues, shared.WaterAccessPtr()).ScheduleReadOnly(transformAccess, 32);
					JobHandle.ScheduleBatchedJobs();
				}
			}
		}

		public void Schedule_2(Ptr<JobSharedFields> shared)
		{
			if (countInJob != 0)
			{
				handleAccess.Complete();
				JobPerf.GetTimestamp();
				SetArgs_Update();
				JobPerf.GetTimestamp();
				WaterJob_Math jobData = new WaterJob_Math
				{
					fields = fields,
					transformValues = transformValues,
					shared = shared
				};
				handleMath = IJobParallelForExtensions.ScheduleByRef(ref jobData, countInJob, 32);
				JobHandle.ScheduleBatchedJobs();
			}
		}

		public void FinishJob()
		{
			if (countInJob == 0)
			{
				return;
			}
			using (finishMarker.Auto())
			{
				handleMath.Complete();
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
	}
}
