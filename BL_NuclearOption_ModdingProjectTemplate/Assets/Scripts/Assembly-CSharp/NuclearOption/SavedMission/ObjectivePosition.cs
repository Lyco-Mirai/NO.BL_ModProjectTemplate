namespace NuclearOption.SavedMission
{
	public readonly struct ObjectivePosition
	{
		public readonly GlobalPosition Position;

		public readonly float? Range;

		public ObjectivePosition(GlobalPosition position, float? range)
		{
			Position = position;
			Range = range;
		}
	}
}
