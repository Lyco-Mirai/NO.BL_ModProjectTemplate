public class PilotParkedState : PilotBaseState
{
	public override void EnterState(Pilot pilot)
	{
		stateDisplayName = "parked";
		controlInputs = pilot.aircraft.GetInputs();
		if (pilot.aircraft.radarAlt < 1f)
		{
			controlInputs.throttle = 0f;
			controlInputs.brake = 1f;
		}
	}

	public override void LeaveState()
	{
	}

	public override void UpdateState(Pilot pilot)
	{
	}

	public override void FixedUpdateState(Pilot pilot)
	{
	}
}
