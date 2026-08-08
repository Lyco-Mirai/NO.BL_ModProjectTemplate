namespace NuclearOption
{
	public enum ReserveEvent : byte
	{
		Invalid = 0,
		accepted = 1,
		acceptedInQueue = 2,
		rejectedDuplicate = 3,
		rejectedRank = 4,
		rejectedAfford = 5,
		rejectedOwned = 6,
		rejectedPossessesReserved = 7,
		cancelledAfford = 8,
		cancelledOwned = 9,
		cancelledRank = 10,
		granted = 11
	}
}
