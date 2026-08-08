public interface IRepairable
{
	float GetRepairPriority(GlobalPosition repairerPosition, float range);

	void Repair(Unit repairer, float strength);

	bool NeedsRepair();

	void OnRepairComplete();
}
