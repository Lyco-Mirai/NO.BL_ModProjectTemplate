using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	public class AeroJobSettings
	{
		private static readonly ProfilerMarker scheduleMarker = new ProfilerMarker("AeroJob Schedule");

		private static readonly ProfilerMarker setArgsMarker = new ProfilerMarker("AeroJob SetArgs");

		private static readonly ProfilerMarker setArgsFullMarker = new ProfilerMarker("AeroJob SetArgs_Full");

		private static readonly ProfilerMarker finishAeroMarker = new ProfilerMarker("AeroJob Finish");

		private readonly List<int> removeTransformIndexCache = new List<int>();

		public const int BATCH_SIZE = 64;

		private readonly JobPerf jobPerf;

		private readonly ControlJobSettings controlJob;

		public NativeArray<float> liftCharts;

		public NativeArray<float> dragCharts;

		public AeroPart[] partsInJob;

		public TransformAccessArray transformAccess;

		public NativeArray<Ptr<AeroPartFields>> fields;

		public NativeArray<JobTransformValues> transformValues;

		public NativeArray<PtrRefCounter<IndexLink>> transformLinks;

		public int countInJob;

		private JobHandle handleAccess;

		private JobHandle handleMath;

		public AeroJobSettings(JobPerf jobPerf, ControlJobSettings controlJob)
		{
			this.jobPerf = jobPerf;
			this.controlJob = controlJob;
		}

		public void CheckCapacity(int neededLength)
		{
			AeroPart[] array = partsInJob;
			int num = ((array != null) ? array.Length : 0);
			if (num < neededLength)
			{
				int num2 = JobManager.IncreaseCapacity(num, neededLength, 128);
				NativeArrayExtensions.Resize(ref fields, num2, copyItems: true);
				NativeArrayExtensions.Resize(ref transformValues, num2 * 2, copyItems: false);
				NativeArrayExtensions.Resize(ref transformLinks, num2 * 2, copyItems: true, NativeArrayOptions.ClearMemory);
				transformAccess.Resize(num2 * 2);
				Array.Resize(ref partsInJob, num2);
			}
		}

		public void DisposeAll()
		{
			partsInJob.SafeClear();
			NativeArrayExtensions.DelayDispose(ref fields);
			NativeArrayExtensions.DelayDispose(ref transformValues);
			NativeArrayExtensions.ClearAndDispose(ref transformLinks, transformAccess.isCreated ? transformAccess.length : 0);
			transformAccess.DelayDispose();
			countInJob = 0;
		}

		public void SetArgs_Added(JobUnitList<JobPart<AeroPart, AeroPartFields>> list)
		{
			using (setArgsFullMarker.Auto())
			{
				JobPart<AeroPart, AeroPartFields> add;
				int addIndex;
				while (list.ProcessNextAdded(out add, out addIndex))
				{
					Transform liftTransform;
					Transform otherTransform;
					bool jobTransforms = add.Part.GetJobTransforms(out liftTransform, out otherTransform);
					ref AeroPartFields reference = ref add.Field.Ref();
					IndexLink.AddTransform(transformAccess, transformLinks, add.links, ref reference.liftTransformIndex, liftTransform);
					if (jobTransforms)
					{
						IndexLink.AddTransform(transformAccess, transformLinks, add.links, ref reference.otherTransformIndex, otherTransform);
					}
					partsInJob[addIndex] = add.Part;
					fields[addIndex] = add.Field;
					countInJob++;
				}
			}
		}

		public void SetArgs_Removed(JobUnitList<JobPart<AeroPart, AeroPartFields>> list)
		{
			using (setArgsFullMarker.Auto())
			{
				removeTransformIndexCache.Clear();
				int removeIndex;
				JobPart<AeroPart, AeroPartFields> removedItem;
				while (list.ProcessNextRemoved(out removeIndex, out removedItem))
				{
					IndexLink.QueueToRemove(removeTransformIndexCache, transformLinks, removedItem.links);
					int num = countInJob - 1;
					if (num != removeIndex)
					{
						partsInJob[removeIndex] = partsInJob[num];
						fields[removeIndex] = fields[num];
					}
					countInJob--;
				}
				IndexLink.RemoveLinks(transformAccess, transformLinks, removeTransformIndexCache);
			}
		}

		public void SetArgs_Update()
		{
			using (setArgsMarker.Auto())
			{
				for (int i = 0; i < countInJob; i++)
				{
					partsInJob[i].UpdateJobFields();
				}
			}
		}

		public void Schedule(JobUnitList<JobPart<AeroPart, AeroPartFields>> aeroParts, Ptr<JobSharedFields> shared)
		{
			using (scheduleMarker.Auto())
			{
				if (aeroParts.PendingChangesRemove)
				{
					JobPerf.GetTimestamp();
					SetArgs_Removed(aeroParts);
				}
				if (aeroParts.PendingChangesAdd)
				{
					JobPerf.GetTimestamp();
					CheckCapacity(aeroParts.CountAfterPending);
					JobPerf.GetTimestamp();
					SetArgs_Added(aeroParts);
				}
				if (countInJob != 0)
				{
					handleAccess = new ReadTransformJob(transformValues, shared.AeroAccessPtr()).ScheduleReadOnly(transformAccess, 64, controlJob.handleAccess);
					JobPerf.GetTimestamp();
					SetArgs_Update();
					AeroJob_Math jobData = new AeroJob_Math
					{
						liftCharts = liftCharts,
						dragCharts = dragCharts,
						airDensityChart = LevelInfo.airDensityChart,
						windVelocity = NetworkSceneSingleton<LevelInfo>.i.GetWind(),
						windTurbulence = NetworkSceneSingleton<LevelInfo>.i.GetTurbulence(),
						fields = fields,
						transformValues = transformValues,
						shared = shared
					};
					handleMath = IJobParallelForExtensions.ScheduleByRef(ref jobData, countInJob, 64, handleAccess);
					JobHandle.ScheduleBatchedJobs();
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
			using (finishAeroMarker.Auto())
			{
				handleAccess.Complete();
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
