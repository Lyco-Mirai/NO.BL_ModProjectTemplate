namespace NuclearOption.SavedMission.Outcomes
{
	public class NoOutcome : Outcome
	{
		public NoOutcome(NoSavedOutcome savedOutcome)
			: base(savedOutcome)
		{
		}

		public override void Complete(Objective completedObjective)
		{
		}
	}
}
