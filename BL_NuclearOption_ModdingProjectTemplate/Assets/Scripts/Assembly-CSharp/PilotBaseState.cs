using UnityEngine;

public abstract class PilotBaseState
{
	protected class FuelChecker
	{
		private Aircraft aircraft;

		private bool enoughFuel;

		private float lastFuelCheck;

		public FuelChecker(Aircraft aircraft)
		{
			this.aircraft = aircraft;
			enoughFuel = true;
		}

		public bool HasEnoughFuel()
		{
			if (Time.timeSinceLevelLoad - lastFuelCheck > 5f)
			{
				lastFuelCheck = Time.timeSinceLevelLoad;
				enoughFuel = aircraft.GetFuelLevel() > 0.2f;
			}
			return enoughFuel;
		}
	}

	public string stateDisplayName;

	protected Pilot pilot;

	protected Aircraft aircraft;

	protected ControlInputs controlInputs;

	protected ThreatVector threatVector;

	protected FuelChecker fuelChecker;

	protected FriendlyAircraftProximity friendlyAircraftProximity;

	protected MissileWarning missileWarningSystem;

	protected Missile evadingMissile;

	protected Airbase nearestAirbase;

	protected GlobalPosition destination;

	public void Initialize(Pilot pilot)
	{
		aircraft = pilot.aircraft;
		controlInputs = aircraft.GetInputs();
		missileWarningSystem = aircraft.GetMissileWarningSystem();
		fuelChecker = new FuelChecker(aircraft);
	}

	public abstract void EnterState(Pilot pilot);

	public abstract void UpdateState(Pilot pilot);

	public abstract void FixedUpdateState(Pilot pilot);

	protected virtual void FindNearestAirbase()
	{
		RunwayQuery query = new RunwayQuery
		{
			RunwayType = RunwayQueryType.Vertical,
			MinSize = aircraft.maxRadius
		};
		nearestAirbase = aircraft.NetworkHQ.GetNearestAirbase(aircraft.transform.position, query);
	}

	public abstract void LeaveState();

	public string GetCurrentState()
	{
		return stateDisplayName;
	}
}
