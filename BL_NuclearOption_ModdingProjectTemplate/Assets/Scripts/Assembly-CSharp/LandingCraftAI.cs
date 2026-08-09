using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LandingCraftAI : ShipAI
{
	[SerializeField]
	private AirCushion airCushion;

	[SerializeField]
	private UnitStorage unitStorage;

	[SerializeField]
	private UnitStorage homeDock;

	private bool landed;

	private bool returningToResupply;

	private Vector3 shoreDirection;

	private float launchTime;

	private float unloadingTime;

	protected override void Awake()
	{
		base.Awake();
		ship.OnLaunch += LandingCraftAI_OnLaunch;
	}

	protected override void Initialize()
	{
		if (GameManager.gameState != GameState.Editor && GameManager.gameState != GameState.Encyclopedia)
		{
			base.Initialize();
			IdentifyHomeDock().Forget();
		}
	}

	private void LandingCraftAI_OnLaunch()
	{
		launchTime = Time.timeSinceLevelLoad;
		ship.OnLaunch -= LandingCraftAI_OnLaunch;
		state = ShipAIState.launching;
	}

	private async UniTask IdentifyHomeDock()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		await UniTask.Delay(5000);
		if (!cancel.IsCancellationRequested && !ship.disabled && ship.NetworkHQ.TryGetNearestUnitStorage(ship, requireStoredUnits: false, out homeDock, out var nearestDistance))
		{
			Debug.Log($"[LandingCraftAI] found homeDock attached to {homeDock.GetUnit().unitName} at distance of {Mathf.Sqrt(nearestDistance)}");
		}
	}

	protected override void ChooseTarget()
	{
		if (Time.timeSinceLevelLoad - lastDestinationSelected < 30f)
		{
			return;
		}
		lastDestinationSelected = Time.timeSinceLevelLoad;
		if (!ship.holdPosition && !commandedDestination && !returningToResupply && !(ship.NetworkHQ == null) && MissionPosition.TryGetClosestObjectivePosition(ship, out var result))
		{
			GlobalPosition position = result.Position;
			float num = FastMath.Distance(ship.GlobalPosition(), lastDestination);
			if (FastMath.Distance(lastDestination, position) > num * 0.2f)
			{
				lastDestination = position;
				SetDestination(position);
			}
		}
	}

	protected override void SetDestination(GlobalPosition destination)
	{
		base.destination = destination;
		RaycastHit hitInfo;
		bool num = Physics.Linecast(destination.ToLocalPosition() + Vector3.up * 5000f, destination.ToLocalPosition() - Vector3.up * 5000f, out hitInfo, PhysicsLayers.StaticsMask) && hitInfo.point.y > Datum.LocalSeaY;
		FastMath.Distance(ship.GlobalPosition(), destination);
		if (num)
		{
			state = ShipAIState.landing;
			avoidShore = false;
			GlobalPosition nearestPoint = ship.GlobalPosition();
			NetworkSceneSingleton<LevelInfo>.i.seaLanes.TryGetNearestPoint(destination, out nearestPoint, out var _);
			if (NetworkSceneSingleton<LevelInfo>.i.roadNetwork.TryGetNearestPoint(nearestPoint, out var nearestPoint2, out var _))
			{
				destination = nearestPoint2;
				if (PlayerSettings.debugVis && SceneSingleton<CameraStateManager>.i.followingUnit == ship)
				{
					GameObject obj = Object.Instantiate(GameAssets.i.debugArrowFlat, Datum.origin);
					obj.transform.position = nearestPoint.ToLocalPosition();
					Vector3 forward = nearestPoint2 - nearestPoint;
					obj.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
					obj.transform.localScale = new Vector3(ship.definition.width, 1f, forward.magnitude);
					obj.transform.position += Vector3.up * 1f;
					Object.Destroy(obj, 10f);
				}
			}
			nearestPoint.y = 1f;
			destination.y = 1f;
			shoreDirection = destination - nearestPoint;
			if (Physics.Linecast(nearestPoint.ToLocalPosition(), destination.ToLocalPosition(), out var hitInfo2, PhysicsLayers.StaticsMask))
			{
				destination = hitInfo2.point.ToGlobalPosition();
				destination.y = 0f;
			}
		}
		else
		{
			avoidShore = true;
			if (state != ShipAIState.returning)
			{
				state = ShipAIState.navigating;
			}
		}
		base.destination = destination;
		pathfinder.Pathfind(NetworkSceneSingleton<LevelInfo>.i.seaLanes, destination, keel);
	}

	protected override void Steer()
	{
		if (Time.timeSinceLevelLoad - lastSteeringUpdate < 0.2f)
		{
			return;
		}
		lastSteeringUpdate = Time.timeSinceLevelLoad;
		SteeringInfo? shipSteerpoint = pathfinder.GetShipSteerpoint(ship.GlobalPosition(), base.transform.forward, ship.speed, stayOnRoad: false, keel);
		float num = FastMath.Distance(ship.GlobalPosition(), destination);
		inputs.throttle = 0f;
		inputs.steering = 0f;
		avoidShips = true;
		if (state == ShipAIState.docking)
		{
			avoidShips = false;
		}
		float num2 = 1f;
		Vector3 vector = shipSteerpoint?.steerVector.normalized ?? ship.transform.forward;
		if (num < ship.maxRadius + 100f)
		{
			StartHoldPosition();
			inputs.throttle = Mathf.Clamp(Vector3.Dot(ship.rb.velocity * 0.1f, -ship.transform.forward), -1f, 1f);
			return;
		}
		if (state == ShipAIState.landing && num < 300f)
		{
			vector = shoreDirection.normalized;
			num2 = ((ship.speed > 20f) ? 0f : 0.5f);
			if (airCushion != null && airCushion.Landed())
			{
				airCushion.Deflate();
				WaitDeployUnits().Forget();
				state = ShipAIState.unloading;
				return;
			}
		}
		if (state == ShipAIState.returning && airCushion != null && airCushion.Landed())
		{
			vector = -shoreDirection.normalized;
		}
		Vector3 zero = Vector3.zero;
		if (state == ShipAIState.unloading)
		{
			vector = shoreDirection.normalized;
			num2 = 0f;
		}
		foreach (Obstacle obstacle in obstacles)
		{
			if (!(obstacle.Transform == null))
			{
				float num3 = ship.maxRadius + obstacle.Radius + 50f;
				if (FastMath.InRange(obstacle.Transform.position, ship.transform.position, num3 * 8f))
				{
					Vector3 vector2 = obstacle.Transform.position - ship.transform.position;
					Vector3.Dot(vector2 - ship.rb.velocity * 5f, vector2);
					_ = 0f;
					float sqrMagnitude = vector2.sqrMagnitude;
					float num4 = 1f / (sqrMagnitude / (num3 * num3));
					zero -= vector2.normalized * num4;
				}
			}
		}
		if (PlayerSettings.debugVis)
		{
			GameObject gameObject = Object.Instantiate(GameAssets.i.debugArrowGreen, ship.transform);
			gameObject.transform.position = ship.transform.position + Vector3.up * 3f;
			gameObject.transform.rotation = Quaternion.LookRotation(vector);
			gameObject.transform.localScale = new Vector3(5f, 5f, vector.magnitude * 30f);
			Object.Destroy(gameObject, 0.2f);
			GameObject obj = Object.Instantiate(GameAssets.i.debugArrowFlat, ship.transform);
			Vector3 vector3 = destination - ship.transform.GlobalPosition();
			obj.transform.position = ship.transform.position + Vector3.up * 3f;
			obj.transform.rotation = Quaternion.LookRotation(vector);
			obj.transform.localScale = new Vector3(5f, 5f, vector3.magnitude);
			Object.Destroy(obj, 0.2f);
			if (zero != Vector3.zero)
			{
				GameObject obj2 = Object.Instantiate(GameAssets.i.debugArrow, ship.transform);
				obj2.transform.position = gameObject.transform.position + gameObject.transform.forward * gameObject.transform.localScale.z;
				obj2.transform.rotation = Quaternion.LookRotation(zero);
				obj2.transform.localScale = new Vector3(5f, 5f, zero.magnitude * 30f);
				Object.Destroy(obj2, 0.2f);
			}
		}
		vector = (zero + vector).normalized;
		if (state != ShipAIState.holding)
		{
			inputs.throttle = num2 * Mathf.Clamp(Vector3.Dot(vector, ship.transform.forward), -1f, 1f);
			inputs.steering = Mathf.Clamp(TargetCalc.GetAngleOnAxis(ship.transform.forward, vector, ship.transform.up) * -0.1f, -1f, 1f);
		}
	}

	private void ReturnToShip()
	{
		if (Time.timeSinceLevelLoad - lastDestinationSelected < 10f)
		{
			return;
		}
		lastDestinationSelected = Time.timeSinceLevelLoad;
		if ((homeDock == null || homeDock.GetUnit().disabled) && !ship.NetworkHQ.TryGetNearestUnitStorage(ship, requireStoredUnits: false, out homeDock, out var _))
		{
			Debug.Log("[LandingCraftAI] Couldn't find ship to return to");
			state = ShipAIState.holding;
			return;
		}
		airCushion.Inflate();
		Debug.Log("[LandingCraftAI] Returning to home dock " + homeDock.GetUnit().unitName);
		destination = homeDock.GetUnit().GlobalPosition();
		pathfinder.Pathfind(NetworkSceneSingleton<LevelInfo>.i.seaLanes, destination, keel);
		if (FastMath.InRange(destination, ship.GlobalPosition(), 1000f))
		{
			homeDock.RegisterIncoming(ship);
			state = ShipAIState.docking;
		}
	}

	private void Dock()
	{
		if (Time.timeSinceLevelLoad - lastSteeringUpdate < 0.2f)
		{
			return;
		}
		if (homeDock == null || homeDock.GetUnit().disabled || homeDock.GetApproachTransform() == null)
		{
			state = ShipAIState.holding;
			homeDock = null;
			return;
		}
		Vector3 other = homeDock.GetApproachTarget(ship) - ship.GlobalPosition();
		inputs.steering = Mathf.Clamp(TargetCalc.GetAngleOnAxis(ship.transform.forward, other, ship.transform.up) * -0.1f, -1f, 1f);
		inputs.throttle = Vector3.Dot(ship.transform.forward, other.normalized);
		if (FastMath.InRange(homeDock.GetDoorTransform().GlobalPosition(), ship.GlobalPosition(), 150f))
		{
			homeDock.OpenDoors();
			inputs.throttle *= 0.5f;
		}
		if (other.sqrMagnitude < ship.maxRadius * ship.maxRadius || ship.speed < 1f)
		{
			homeDock.Store(ship);
			homeDock.Transfer(unitStorage);
			state = ShipAIState.docked;
		}
	}

	private void Launch()
	{
		if (!(Time.timeSinceLevelLoad - lastSteeringUpdate < 0.2f))
		{
			inputs.steering = 0f;
			inputs.throttle = 0.5f;
			if (Time.timeSinceLevelLoad - launchTime > 10f)
			{
				state = ShipAIState.navigating;
			}
		}
	}

	private void NavigateToLand()
	{
	}

	private async UniTask WaitDeployUnits()
	{
		CancellationToken cancel = base.destroyCancellationToken;
		while (ship.speed > 1f)
		{
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested || ship.disabled)
			{
				return;
			}
		}
		homeDock = null;
		unitStorage.DeployUnits();
		while (!unitStorage.HasFinishedDeploying())
		{
			await UniTask.Delay(1000);
			if (cancel.IsCancellationRequested || ship.disabled)
			{
				return;
			}
		}
		float nearestDistance;
		while ((homeDock == null || homeDock.GetUnit().disabled || !homeDock.HasUnits()) && !ship.NetworkHQ.TryGetNearestUnitStorage(ship, requireStoredUnits: true, out homeDock, out nearestDistance))
		{
			await UniTask.Delay(10000);
			if (cancel.IsCancellationRequested || ship.disabled)
			{
				return;
			}
		}
		await UniTask.Delay(2000);
		if (!cancel.IsCancellationRequested && !ship.disabled)
		{
			state = ShipAIState.returning;
			lastDestinationSelected = -100f;
		}
	}

	protected override void Update()
	{
		if (state == ShipAIState.docked)
		{
			return;
		}
		if (state == ShipAIState.docking)
		{
			Dock();
			return;
		}
		if (state == ShipAIState.unloading)
		{
			unloadingTime += Time.deltaTime;
			if (unloadingTime > 60f)
			{
				state = ShipAIState.returning;
				lastDestinationSelected = -100f;
				unloadingTime = 0f;
			}
			Steer();
			return;
		}
		unloadingTime = 0f;
		if (state == ShipAIState.launching)
		{
			Launch();
			return;
		}
		if (state == ShipAIState.returning)
		{
			ReturnToShip();
			Steer();
			return;
		}
		if (!commandedDestination && !ship.holdPosition)
		{
			ChooseTarget();
		}
		Steer();
	}
}
