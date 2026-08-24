namespace NuclearOption.Jobs
{
	public static class JobPerfTimersExtensions
	{
		public unsafe static Ptr<long> ControlAccessPtr(this Ptr<JobSharedFields> timer)
		{
			return &timer.ptr->controlAccess;
		}

		public unsafe static Ptr<long> AeroAccessPtr(this Ptr<JobSharedFields> timer)
		{
			return &timer.ptr->aeroAccess;
		}

		public unsafe static Ptr<long> VehicleAccessPtr(this Ptr<JobSharedFields> timer)
		{
			return &timer.ptr->vehicleAccess;
		}

		public unsafe static Ptr<long> WaterAccessPtr(this Ptr<JobSharedFields> timer)
		{
			return &timer.ptr->waterAccess;
		}
	}
}
