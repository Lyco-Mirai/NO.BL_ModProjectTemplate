using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace NuclearOption.Jobs
{
	public class ControlJobSettings
	{
		private static readonly ProfilerMarker scheduleMarker = new ProfilerMarker("ControlSurfaceJob Schedule");

		private static readonly ProfilerMarker updateArgsMarker = new ProfilerMarker("ControlSurfaceJob UpdateArgs");

		private static readonly ProfilerMarker updateArgsFullMarker = new ProfilerMarker("ControlSurfaceJob UpdateArgs_Full");

		private static readonly ProfilerMarker finishAeroMarker = new ProfilerMarker("ControlSurfaceJob Finish");

		private readonly List<int> removeTransformIndexCache = new List<int>();

		public const int BATCH_SIZE = 32;

		private readonly JobPerf jobPerf;

		public ControlSurface[] partsInJob;

		public TransformAccessArray transformAccess;

		public NativeArray<Ptr<ControlSurfaceFields>> fields;

		public NativeArray<Quaternion> rotations;

		public NativeArray<PtrRefCounter<IndexLink>> transformLinks;

		public int countInJob;

		private JobHandle handleMath;

		public JobHandle handleAccess;

		public ControlJobSettings(JobPerf jobPerf)
		{
			this.jobPerf = jobPerf;
		}

		public void CheckCapacity(int neededLength)
		{
			ControlSurface[] array = partsInJob;
			int num = ((array != null) ? array.Length : 0);
			if (num < neededLength)
			{
				int num2 = JobManager.IncreaseCapacity(num, neededLength, 64);
				NativeArrayExtensions.Resize(ref fields, num2, copyItems: true);
				NativeArrayExtensions.Resize(ref rotations, num2 * 3, copyItems: false);
				NativeArrayExtensions.Resize(ref transformLinks, num2 * 3, copyItems: true, NativeArrayOptions.ClearMemory);
				transformAccess.Resize(num2 * 3);
				Array.Resize(ref partsInJob, num2);
			}
		}

		public void DisposeAll()
		{
			partsInJob.SafeClear();
			NativeArrayExtensions.DelayDispose(ref fields);
			NativeArrayExtensions.DelayDispose(ref rotations);
			NativeArrayExtensions.ClearAndDispose(ref transformLinks, transformAccess.isCreated ? transformAccess.length : 0);
			transformAccess.DelayDispose();
			countInJob = 0;
		}

		public void SetArgs_Add(JobUnitList<JobPart<ControlSurface, ControlSurfaceFields>> list)
		{
			using (updateArgsFullMarker.Auto())
			{
				JobPart<ControlSurface, ControlSurfaceFields> add;
				int addIndex;
				while (list.ProcessNextAdded(out add, out addIndex))
				{
					Transform liftTransform;
					Transform upperTransform;
					Transform lowerTransform;
					bool jobTransforms = add.Part.GetJobTransforms(out liftTransform, out upperTransform, out lowerTransform);
					ref ControlSurfaceFields reference = ref add.Field.Ref();
					IndexLink.AddTransform(transformAccess, transformLinks, add.links, ref reference.visibleTransformLink, liftTransform);
					if (jobTransforms)
					{
						IndexLink.AddTransform(transformAccess, transformLinks, add.links, ref reference.upperTransformLink, upperTransform);
						IndexLink.AddTransform(transformAccess, transformLinks, add.links, ref reference.lowerTransformLink, lowerTransform);
					}
					partsInJob[addIndex] = add.Part;
					fields[addIndex] = add.Field;
					countInJob++;
				}
			}
		}

		public void SetArgs_Remove(JobUnitList<JobPart<ControlSurface, ControlSurfaceFields>> list)
		{
			using (updateArgsFullMarker.Auto())
			{
				removeTransformIndexCache.Clear();
				int removeIndex;
				JobPart<ControlSurface, ControlSurfaceFields> removedItem;
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
			using (updateArgsMarker.Auto())
			{
				for (int i = 0; i < countInJob; i++)
				{
					partsInJob[i].UpdateJobFields();
				}
			}
		}

		public void Schedule(JobUnitList<JobPart<ControlSurface, ControlSurfaceFields>> controlSurfaces, Ptr<JobSharedFields> shared)
		{
			using (scheduleMarker.Auto())
			{
				if (controlSurfaces.PendingChangesRemove)
				{
					JobPerf.GetTimestamp();
					SetArgs_Remove(controlSurfaces);
				}
				if (controlSurfaces.PendingChangesAdd)
				{
					JobPerf.GetTimestamp();
					CheckCapacity(controlSurfaces.CountAfterPending);
					JobPerf.GetTimestamp();
					SetArgs_Add(controlSurfaces);
				}
				if (countInJob != 0)
				{
					JobPerf.GetTimestamp();
					SetArgs_Update();
					ControlSurfaceJob_Math jobData = new ControlSurfaceJob_Math
					{
						fields = fields,
						rotations = rotations,
						shared = shared
					};
					handleMath = IJobParallelForExtensions.ScheduleByRef(ref jobData, countInJob, 32);
					handleAccess = new SetLocalRotationJob(rotations, shared.ControlAccessPtr()).Schedule(transformAccess, handleMath);
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
				handleMath.Complete();
				handleAccess.Complete();
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
