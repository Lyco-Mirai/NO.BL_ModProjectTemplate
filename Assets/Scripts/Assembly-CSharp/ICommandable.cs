public interface ICommandable
{
	UnitCommand UnitCommand { get; }

	bool Disabled { get; }

	FactionHQ HQ { get; }
}
