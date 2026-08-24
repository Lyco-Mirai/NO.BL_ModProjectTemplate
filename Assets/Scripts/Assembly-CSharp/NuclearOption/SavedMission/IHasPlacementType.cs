namespace NuclearOption.SavedMission
{
	public interface IHasPlacementType
	{
		PlacementType PlacementType { get; }

		bool CanBeAttached { get; }
	}
}
