using UnityEngine;

public class AssaultCarrierAI : ShipAI
{
	[SerializeField]
	private UnitStorage unitStorage;

	private bool inDeployRange;

	protected override void Initialize()
	{
		if (ship.LocalSim && GameManager.gameState != GameState.Editor && GameManager.gameState != GameState.Encyclopedia)
		{
			base.Initialize();
		}
	}

	private void ChooseDestination()
	{
		if (Time.timeSinceLevelLoad - lastDestinationSelected < 30f)
		{
			return;
		}
		lastDestinationSelected = Time.timeSinceLevelLoad;
		if (!ship.holdPosition && !commandedDestination && !(ship.NetworkHQ == null) && MissionPosition.TryGetClosestObjectivePosition(ship, out var result))
		{
			GlobalPosition position = result.Position;
			if (!FastMath.InRange(destination, position, 1000f))
			{
				destination = position;
				pathfinder.Pathfind(NetworkSceneSingleton<LevelInfo>.i.seaLanes, position, keel);
				state = ShipAIState.attacking;
			}
			inDeployRange = FastMath.InRange(ship.GlobalPosition(), result.Position, standoffDistance + 500f);
		}
	}

	private void DeployCargo()
	{
		if (inDeployRange)
		{
			unitStorage.DeployUnits();
			state = ShipAIState.unloading;
		}
		else if (unitStorage.HasFinishedDeploying())
		{
			state = ShipAIState.navigating;
		}
	}

	protected override void Update()
	{
		if (!commandedDestination && !ship.holdPosition)
		{
			ChooseDestination();
			if (unitStorage.HasUnits())
			{
				DeployCargo();
			}
		}
		Steer();
	}
}
