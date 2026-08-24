namespace NuclearOption.SavedMission.Objectives
{
	public class Waypoint
	{
		public readonly ValueWrapperGlobalPosition GlobalPosition = new ValueWrapperGlobalPosition();

		public readonly ValueWrapperFloat Range = new ValueWrapperFloat(50f);

		public Waypoint()
		{
		}

		public Waypoint(GlobalPosition globalPosition, float range = 50f)
		{
			GlobalPosition.SetValue(globalPosition, this);
			Range.SetValue(range, this);
		}

		public ObjectivePosition ToObjectivePosition()
		{
			return new ObjectivePosition(GlobalPosition, Range);
		}

		public override string ToString()
		{
			return $"[Pos {GlobalPosition}, Range {Range}]";
		}
	}
}
