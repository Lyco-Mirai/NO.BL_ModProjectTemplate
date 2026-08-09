namespace NuclearOption
{
	public struct ReserveNotice
	{
		public ReserveEvent outcome;

		public AircraftDefinition aircraftDefinition;

		public bool isReserving;

		public int queuePosition;

		public static readonly ReserveNotice Invalid = new ReserveNotice(ReserveEvent.Invalid, null, isReserving: false, 0);

		public ReserveNotice(ReserveEvent outcome, AircraftDefinition aircraftDefinition, bool isReserving, int queuePosition)
		{
			this.outcome = outcome;
			this.aircraftDefinition = aircraftDefinition;
			this.isReserving = isReserving;
			this.queuePosition = queuePosition;
		}
	}
}
