public interface IRefuelable
{
	void Refuel(Unit refueler);

	bool CanRefuel();
}
