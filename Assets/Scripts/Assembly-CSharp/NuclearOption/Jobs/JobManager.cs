using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public class JobManager : SceneSingleton<JobManager>
	{
		private static readonly ProfilerMarker fixedUpdateEarlyMarker = new ProfilerMarker("JobManager FixedUpdateEarly");

		private static readonly ProfilerMarker fixedUpdateLateMarker = new ProfilerMarker("JobManager FixedUpdateLate");

		private static readonly ProfilerMarker scheduleAllMarker = new ProfilerMarker("JobManager Schedule");

		private static readonly ProfilerMarker scheduleAeroMarker = new ProfilerMarker("AeroJob Schedule");

		private static readonly ProfilerMarker pilotAeroInputsMarker = new ProfilerMarker("JobManager Pilot Inputs");

		private readonly JobUnitList<JobPart<AeroPart, AeroPartFields>> aeroParts = new JobUnitList<JobPart<AeroPart, AeroPartFields>>();

		private readonly JobUnitList<JobPart<ControlSurface, ControlSurfaceFields>> controlSurfaces = new JobUnitList<JobPart<ControlSurface, ControlSurfaceFields>>();

		private readonly List<Ship> ships = new List<Ship>();

		private readonly JobUnitList<JobPart<ShipPart, ShipPartFields>> shipParts = new JobUnitList<JobPart<ShipPart, ShipPartFields>>();

		private readonly JobUnitList<JobPart<GroundVehicle, GroundVehicleFields>> vehicles = new JobUnitList<JobPart<GroundVehicle, GroundVehicleFields>>();

		private readonly List<Pilot> pilots = new List<Pilot>();

		private readonly AeroJobSettings aeroJob;

		private readonly ControlJobSettings controlJob;

		private readonly WaterJobsSettings waterJobs;

		private readonly GroundVehicleJobSettings vehicleJob;

		public readonly DetectorManager detector;

		private readonly JobPerf jobPerf;

		private PtrAllocation<JobSharedFields> shared;

		private JobManager()
		{
			jobPerf = new JobPerf();
			controlJob = new ControlJobSettings(jobPerf);
			aeroJob = new AeroJobSettings(jobPerf, controlJob);
			waterJobs = new WaterJobsSettings(jobPerf);
			vehicleJob = new GroundVehicleJobSettings(jobPerf);
			detector = new DetectorManager(jobPerf);
		}

		public static void Add(JobPart<AeroPart, AeroPartFields> aeroPart)
		{
			SceneSingleton<JobManager>.ThrowIfInstanceNull();
			SceneSingleton<JobManager>.i.aeroParts.Add(aeroPart);
		}

		public static void Add(JobPart<ControlSurface, ControlSurfaceFields> controlSurface)
		{
			SceneSingleton<JobManager>.ThrowIfInstanceNull();
			SceneSingleton<JobManager>.i.controlSurfaces.Add(controlSurface);
		}

		public static void Add(Ship ship)
		{
			SceneSingleton<JobManager>.ThrowIfInstanceNull();
			SceneSingleton<JobManager>.i.ships.Add(ship);
			foreach (ShipPart part in ship.parts)
			{
				SceneSingleton<JobManager>.i.shipParts.Add(part.SetupJob());
			}
		}

		public static void Add(JobPart<ShipPart, ShipPartFields> shipPart)
		{
			SceneSingleton<JobManager>.ThrowIfInstanceNull();
			SceneSingleton<JobManager>.i.shipParts.Add(shipPart);
		}

		public static void Add(JobPart<GroundVehicle, GroundVehicleFields> vehicle)
		{
			SceneSingleton<JobManager>.ThrowIfInstanceNull();
			SceneSingleton<JobManager>.i.vehicles.Add(vehicle);
		}

		public static void Add(Pilot pilot)
		{
			SceneSingleton<JobManager>.ThrowIfInstanceNull();
			SceneSingleton<JobManager>.i.pilots.Add(pilot);
		}

		public static void Remove(ref JobPart<AeroPart, AeroPartFields> part)
		{
			if (part != null)
			{
				if (SceneSingleton<JobManager>.i != null)
				{
					SceneSingleton<JobManager>.i.aeroParts.Remove(part);
				}
				part = null;
			}
		}

		public static void Remove(ref JobPart<ControlSurface, ControlSurfaceFields> part)
		{
			if (part != null)
			{
				if (SceneSingleton<JobManager>.i != null)
				{
					SceneSingleton<JobManager>.i.controlSurfaces.Remove(part);
				}
				part = null;
			}
		}

		public static void Remove(Ship ship)
		{
			if (SceneSingleton<JobManager>.i != null)
			{
				SceneSingleton<JobManager>.i.ships.Remove(ship);
			}
		}

		public static void Remove(ref JobPart<ShipPart, ShipPartFields> part)
		{
			if (part != null)
			{
				if (SceneSingleton<JobManager>.i != null)
				{
					SceneSingleton<JobManager>.i.shipParts.Remove(part);
				}
				part = null;
			}
		}

		public static void Remove(ref JobPart<GroundVehicle, GroundVehicleFields> part)
		{
			if (part != null)
			{
				if (SceneSingleton<JobManager>.i != null)
				{
					SceneSingleton<JobManager>.i.vehicles.Remove(part);
				}
				part = null;
			}
		}

		public static void Remove(Pilot pilot)
		{
			if (SceneSingleton<JobManager>.i != null)
			{
				SceneSingleton<JobManager>.i.pilots.Remove(pilot);
			}
		}

		public static int IncreaseCapacity(int currentLength, int neededLength, int min)
		{
			int num = currentLength;
			if (num < min)
			{
				num = min;
			}
			while (num < neededLength)
			{
				num *= 2;
			}
			return num;
		}

		protected override void Awake()
		{
			JobsAllocator<JobSharedFields>.Allocate(ref shared, 1);
			jobPerf.Open();
			base.Awake();
			GenerateCharts();
			FixedUpdateEarlyTask().Forget();
		}

		private void OnDestroy()
		{
			if (shared.IsCreated)
			{
				shared.Ref().DebugMarkersArray.Dispose();
			}
			shared.Dispose();
			aeroParts.FullClear();
			controlSurfaces.FullClear();
			shipParts.FullClear();
			vehicles.FullClear();
			ships.Clear();
			pilots.Clear();
			NativeArrayExtensions.DelayDispose(ref aeroJob.liftCharts);
			NativeArrayExtensions.DelayDispose(ref aeroJob.dragCharts);
			aeroJob.DisposeAll();
			controlJob.DisposeAll();
			waterJobs.DisposeAll();
			vehicleJob.DisposeAll();
			detector.DisposeAll();
			jobPerf.Dispose();
		}

		private void GenerateCharts()
		{
			List<Airfoil> airfoilsList = new List<Airfoil>();
			foreach (AircraftDefinition item in Encyclopedia.i.aircraft)
			{
				item.aircraftParameters.AddAirfoils(ref airfoilsList);
			}
			NativeArray<float> nativeArray = new NativeArray<float>(airfoilsList.Count * 128, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			NativeArray<float> nativeArray2 = new NativeArray<float>(airfoilsList.Count * 128, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			for (int i = 0; i < airfoilsList.Count; i++)
			{
				airfoilsList[i].id = i;
				NativeArray<float>.Copy(airfoilsList[i].BuildLiftChart(), 0, nativeArray, i * 128, 128);
				NativeArray<float>.Copy(airfoilsList[i].BuildDragChart(), 0, nativeArray2, i * 128, 128);
			}
			aeroJob.liftCharts = nativeArray;
			aeroJob.dragCharts = nativeArray2;
		}

		private async UniTask FixedUpdateEarlyTask()
		{
			CancellationToken cancel = base.destroyCancellationToken;
			while (true)
			{
				await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
				if (cancel.IsCancellationRequested)
				{
					break;
				}
				try
				{
					FixedUpdateEarly();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
				try
				{
					FixedUpdateLate();
				}
				catch (Exception exception2)
				{
					Debug.LogException(exception2);
				}
			}
		}

		private void FixedUpdateEarly()
		{
			using (fixedUpdateEarlyMarker.Auto())
			{
				JobPerf.GetTimestamp();
				ScheduleJobs();
				JobPerf.GetTimestamp();
				PilotAeroInputs();
			}
		}

		private void PilotAeroInputs()
		{
			using (pilotAeroInputsMarker.Auto())
			{
				for (int num = pilots.Count - 1; num >= 0; num--)
				{
					try
					{
						if (pilots[num].Pilot_OnAeroInputsApplied() == PartResult.Remove)
						{
							pilots.RemoveAt(num);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
		}

		private void FixedUpdateLate()
		{
			using (fixedUpdateLateMarker.Auto())
			{
				FinishJobs();
				shared.Ref().LogAndReset(jobPerf);
				if (PlayerSettings.debugVis)
				{
					shared.Ref().DrawDebugMarkers();
				}
			}
		}

		private void ScheduleJobs()
		{
			using (scheduleAllMarker.Auto())
			{
				if (!LevelInfo.airDensityChart.IsCreated)
				{
					Debug.LogError("AeroJob could not run because LevelInfo had no AirDensityChart");
					return;
				}
				ref JobSharedFields reference = ref shared.Ref();
				reference.tickOffset++;
				reference.datum = Datum.GetBurstDatum();
				reference.fixedDeltaTime = Time.fixedDeltaTime;
				reference.timeSinceLevelLoad = Time.timeSinceLevelLoad;
				waterJobs.Schedule_1(shipParts, shared);
				vehicleJob.Schedule_1(vehicles, shared);
				controlJob.Schedule(controlSurfaces, shared);
				aeroJob.Schedule(aeroParts, shared);
				detector.Schedule();
				waterJobs.Schedule_2(shared);
				vehicleJob.Schedule_2(shared);
			}
		}

		private void FinishJobs()
		{
			JobPerf.GetTimestamp();
			controlJob.FinishJob();
			aeroJob.FinishJob();
			vehicleJob.FinishJob();
			JobPerf.GetTimestamp();
			waterJobs.FinishJob();
			JobPerf.GetTimestamp();
			try
			{
				foreach (Ship ship in ships)
				{
					ship.ApplyJobResults();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			detector.FinishJob();
		}
	}
}
