using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public class DetectorManager
	{
		private static readonly ProfilerMarker scheduleMarker = new ProfilerMarker("DetectorManager Schedule");

		private static readonly ProfilerMarker finishMarker = new ProfilerMarker("DetectorManager Finish");

		private static readonly ProfilerMarker requestRadarMarker = new ProfilerMarker("Request RadarCheck");

		private static readonly ProfilerMarker requestLoSMarker = new ProfilerMarker("Request LoSCheck");

		public const int BATCH_SIZE = 16;

		public const float EARTH_RADIUS = 6371000f;

		private readonly JobPerf jobPerf;

		private List<DetectionRequest> LoSRequests = new List<DetectionRequest>();

		private NativeArray<RaycastHit> results;

		private NativeArray<RaycastCommand> commands;

		private int countInJob;

		private JobHandle handle;

		public DetectorManager(JobPerf jobPerf)
		{
			this.jobPerf = jobPerf;
		}

		public static void RequestRadarCheck(TargetDetector detector, Unit target, IRadarReturn radarReturn)
		{
			using (requestRadarMarker.Auto())
			{
				SceneSingleton<JobManager>.ThrowIfInstanceNull();
				GlobalPosition globalPosition = detector.GetScanPoint().GlobalPosition();
				GlobalPosition globalPosition2 = target.GlobalPosition();
				Vector3 vector = globalPosition2 - globalPosition;
				vector.y = 0f;
				float num = FastMath.Distance(globalPosition, globalPosition2);
				float magnitude = vector.magnitude;
				float num2 = Mathf.Sqrt(12742000f * globalPosition.y);
				float num3 = Mathf.Sqrt(12742000f * globalPosition2.y);
				if (!(num2 + num3 < magnitude))
				{
					float num4 = 0f;
					if (magnitude < num2 && globalPosition2.y < globalPosition.y * (1f - magnitude / num2))
					{
						float num5 = num * target.radarAlt / (globalPosition.y - globalPosition2.y);
						num4 += Mathf.Min(num, 1000f) / num5;
					}
					num4 += target.maxRadius * target.maxRadius * 2f / (target.radarAlt * target.radarAlt);
					SceneSingleton<JobManager>.i.detector.LoSRequests.Add(new DetectionRequest(detector, target, radarReturn, num, num4));
				}
			}
		}

		public static void RequestLoSCheck(TargetDetector detector, Unit target)
		{
			using (requestLoSMarker.Auto())
			{
				SceneSingleton<JobManager>.ThrowIfInstanceNull();
				if (detector.InVisualRange(target))
				{
					SceneSingleton<JobManager>.i.detector.LoSRequests.Add(new DetectionRequest(detector, target, null, 0f, 0f));
				}
			}
		}

		private void DisposeNative(bool delayed)
		{
			NativeArrayExtensions.DelayDispose(ref results, delayed);
			NativeArrayExtensions.DelayDispose(ref commands, delayed);
		}

		public void DisposeAll()
		{
			LoSRequests.Clear();
			DisposeNative(delayed: true);
		}

		private void CheckCapacity(int max)
		{
			if (results.Length < max)
			{
				int length = Mathf.Max(results.Length * 2, max) + 10;
				DisposeNative(delayed: false);
				results = new NativeArray<RaycastHit>(length, Allocator.Persistent);
				commands = new NativeArray<RaycastCommand>(length, Allocator.Persistent);
			}
		}

		public void Schedule()
		{
			using (scheduleMarker.Auto())
			{
				try
				{
					for (int num = LoSRequests.Count - 1; num >= 0; num--)
					{
						if (LoSRequests[num].target == null || LoSRequests[num].Detector == null)
						{
							LoSRequests.RemoveAt(num);
						}
					}
					countInJob = LoSRequests.Count;
					if (countInJob != 0)
					{
						CheckCapacity(countInJob);
						for (int i = 0; i < countInJob; i++)
						{
							commands[i] = LoSRequests[i].GetRaycastCommand();
						}
						NativeArray<RaycastCommand> subArray = commands.GetSubArray(0, countInJob);
						NativeArray<RaycastHit> subArray2 = results.GetSubArray(0, countInJob);
						handle = RaycastCommand.ScheduleBatch(subArray, subArray2, 16, 1);
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					LoSRequests.Clear();
					countInJob = 0;
				}
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
				handle.Complete();
				try
				{
					for (int i = 0; i < countInJob; i++)
					{
						LoSRequests[i].ProcessResult(results[i]);
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
				LoSRequests.RemoveRange(0, countInJob);
				countInJob = 0;
			}
		}
	}
}
