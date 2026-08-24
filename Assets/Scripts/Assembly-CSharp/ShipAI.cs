using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;

public class ShipAI : MonoBehaviour
{
	public enum ShipAIState
	{
		launching = 0,
		holding = 1,
		navigating = 2,
		attacking = 3,
		landing = 4,
		unloading = 5,
		returning = 6,
		docking = 7,
		docked = 8
	}

	public ShipAIState state;

	protected float lastDestinationSelected;

	protected GlobalPosition lastDestination;

	protected PathfindingAgent pathfinder;

	protected List<GlobalPosition> waypoints = new List<GlobalPosition>();

	protected bool commandedDestination;

	[SerializeField]
	protected Ship ship;

	[SerializeField]
	protected float standoffDistance;

	protected ShipInputs inputs;

	protected float lastTargetAssessTime;

	protected float currentTargetPriority;

	protected float lastSteeringUpdate;

	protected Unit currentTarget;

	protected GlobalPosition destination;

	protected List<Unit> unitsInObstacleRange = new List<Unit>();

	protected List<Obstacle> obstacles = new List<Obstacle>();

	protected Obstacle shoreObstacle;

	protected bool avoidShips;

	protected bool avoidShore;

	protected Transform avoidTransform;

	protected float holdTime = 120f;

	[SerializeField]
	protected Transform keel;

	protected virtual void Awake()
	{
		if (!NetworkManagerNuclearOption.i.Server.Active || GameManager.gameState == GameState.Encyclopedia)
		{
			base.enabled = false;
			return;
		}
		state = ShipAIState.holding;
		avoidShips = true;
		avoidShore = true;
		pathfinder = new PathfindingAgent(ship);
		Transform transform = new GameObject("shoreObstacle").transform;
		transform.SetParent(Datum.origin);
		transform.position = Vector3.forward * 200000f;
		shoreObstacle = new Obstacle(transform, 400f, float.MaxValue);
		inputs = ship.GetInputs();
		ship.onInitialize += Initialize;
		ship.UnitCommand.ProcessSetDestination += ShipAI_ProcessSetDestination;
		lastDestinationSelected = Time.timeSinceLevelLoad - 25f;
		this.StartSlowUpdateDelayed(5f, CheckObstacles);
	}

	protected virtual void Initialize()
	{
		destination = ship.GlobalPosition();
		lastDestination = destination;
		ship.onInitialize -= Initialize;
	}

	protected virtual void ChooseTarget()
	{
		if (Time.timeSinceLevelLoad - lastDestinationSelected < 30f)
		{
			return;
		}
		lastDestinationSelected = Time.timeSinceLevelLoad;
		if (!ship.holdPosition && !commandedDestination && !(ship.NetworkHQ == null))
		{
			AssessHQTargets();
			GlobalPosition b = destination;
			float num = float.MaxValue;
			bool flag = false;
			if (currentTarget != null && ship.NetworkHQ.TryGetKnownPosition(currentTarget, out var knownPosition))
			{
				b = knownPosition;
				num = FastMath.Distance(ship.GlobalPosition(), b);
			}
			if (MissionPosition.TryGetClosestObjectivePosition(ship, out var result) && FastMath.Distance(ship.GlobalPosition(), result.Position) < num - standoffDistance)
			{
				b = result.Position;
				flag = true;
			}
			if (!FastMath.InRange(destination, b, 1000f))
			{
				SetDestination(b);
				state = (flag ? ShipAIState.navigating : ShipAIState.attacking);
			}
		}
	}

	public void ArriveAtCommandedDestination()
	{
		state = ShipAIState.holding;
		ship.holdPosition = true;
		WaitHoldPosition(holdTime).Forget();
	}

	protected void StartHoldPosition()
	{
		WaitHoldPosition(holdTime).Forget();
	}

	protected async UniTask WaitHoldPosition(float holdTime)
	{
		CancellationToken cancel = base.destroyCancellationToken;
		state = ShipAIState.holding;
		await UniTask.Delay((int)(holdTime * 1000f));
		if (!cancel.IsCancellationRequested)
		{
			commandedDestination = false;
		}
	}

	protected void HoldPosition()
	{
		inputs.throttle = Vector3.Dot(ship.rb.velocity.normalized, -ship.transform.forward);
		inputs.steering = 0f;
	}

	protected void ShipAI_ProcessSetDestination(ref UnitCommand.Command command)
	{
		commandedDestination = true;
		state = ShipAIState.navigating;
		SetDestination(command.position);
	}

	protected virtual void SetDestination(GlobalPosition newDestination)
	{
		lastDestination = destination;
		pathfinder.Pathfind(NetworkSceneSingleton<LevelInfo>.i.seaLanes, newDestination, keel);
		destination = newDestination;
		lastDestinationSelected = Time.timeSinceLevelLoad;
	}

	protected void CheckObstacles()
	{
		if (ship.speed > 1f)
		{
			UpdateObstacles();
		}
	}

	protected void UpdateObstacles()
	{
		obstacles.Clear();
		if (!BattlefieldGrid.TryGetGridSquare(base.transform.GlobalPosition(), out var _))
		{
			return;
		}
		if (avoidShips)
		{
			foreach (Unit item in BattlefieldGrid.GetUnitsInRangeEnumerable(ship.GlobalPosition(), 1000f))
			{
				if (item != null && item != ship && item.radarAlt < 10f && (item.rb == null || item.rb.mass > ship.rb.mass * 0.5f))
				{
					obstacles.Add(new Obstacle(item.transform, item.maxRadius, float.MaxValue));
				}
			}
		}
		Vector3 forward = ship.transform.forward;
		forward.y = 0f;
		Vector3 velocity = ship.rb.velocity;
		velocity.y = 0f;
		if (!avoidShore)
		{
			return;
		}
		if (Physics.Linecast(keel.position, keel.position + forward * 400f + velocity * 5f, out var hitInfo, PhysicsLayers.StaticsMask))
		{
			GlobalPosition globalPosition = ship.GlobalPosition();
			NetworkSceneSingleton<LevelInfo>.i.roadNetwork.TryGetNearestPoint(globalPosition, out var nearestPoint, out var _);
			NetworkSceneSingleton<LevelInfo>.i.seaLanes.TryGetNearestPoint(globalPosition, out var nearestPoint2, out var _);
			Vector3 a = nearestPoint2 - globalPosition;
			Vector3 vector = nearestPoint - globalPosition;
			Vector3 normalized = Vector3.Lerp(a, -vector, 0.5f).normalized;
			normalized.y = 0f;
			shoreObstacle.Transform.position = hitInfo.point - normalized * hitInfo.distance * 0.5f;
			if (PlayerSettings.debugVis && SceneSingleton<CameraStateManager>.i.followingUnit == ship)
			{
				GameObject obj = Object.Instantiate(GameAssets.i.debugArrowFlat, Datum.origin);
				obj.transform.position = hitInfo.point + Vector3.up * 10f;
				obj.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
				obj.transform.localScale = new Vector3(ship.definition.width, 1f, 200f);
				Object.Destroy(obj, 10f);
			}
		}
		obstacles.Add(shoreObstacle);
	}

	protected virtual void Steer()
	{
		if (Time.timeSinceLevelLoad - lastSteeringUpdate < 0.2f)
		{
			return;
		}
		lastSteeringUpdate = Time.timeSinceLevelLoad;
		inputs.throttle = 0f;
		inputs.steering = 0f;
		SteeringInfo? shipSteerpoint = pathfinder.GetShipSteerpoint(ship.GlobalPosition(), base.transform.forward, ship.speed, stayOnRoad: false, keel);
		float num = FastMath.Distance(ship.GlobalPosition(), destination);
		if (state == ShipAIState.attacking && num < standoffDistance)
		{
			StartHoldPosition();
			return;
		}
		if (num < ship.maxRadius + 100f)
		{
			StartHoldPosition();
			inputs.throttle = Mathf.Clamp(Vector3.Dot(ship.rb.velocity * 0.1f, -ship.transform.forward), -1f, 1f);
			return;
		}
		Vector3 vector = shipSteerpoint?.steerVector.normalized ?? ship.transform.forward;
		Vector3 zero = Vector3.zero;
		foreach (Obstacle obstacle in obstacles)
		{
			if (!(obstacle.Transform == null))
			{
				float num2 = ship.maxRadius + obstacle.Radius + 50f;
				if (FastMath.InRange(obstacle.Transform.position, ship.transform.position, num2 * 8f))
				{
					Vector3 vector2 = obstacle.Transform.position - ship.transform.position;
					Vector3.Dot(vector2 - ship.rb.velocity * 5f, vector2);
					_ = 0f;
					float sqrMagnitude = vector2.sqrMagnitude;
					float num3 = 1f / (sqrMagnitude / (num2 * num2));
					zero -= vector2.normalized * num3;
				}
			}
		}
		vector = (zero + vector).normalized;
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
		if (state != ShipAIState.holding)
		{
			inputs.throttle = Vector3.Dot(vector, ship.transform.forward);
			if (inputs.throttle < 0f && Vector3.Dot(ship.transform.forward, ship.rb.velocity) < 0f)
			{
				inputs.throttle = 0f;
			}
			inputs.steering = Mathf.Clamp(TargetCalc.GetAngleOnAxis(ship.transform.forward, vector, ship.transform.up) * -0.1f, -1f, 1f);
		}
	}

	protected void AssessHQTargets()
	{
		currentTargetPriority = 0f;
		currentTarget = null;
		foreach (KeyValuePair<PersistentID, TrackingInfo> item in ship.NetworkHQ.trackingDatabase)
		{
			TrackingInfo value = item.Value;
			if (!value.TryGetUnit(out var unit) || unit.disabled || unit.NetworkHQ == null || unit.NetworkHQ == ship.NetworkHQ || unit.radarAlt > 10f || unit.speed > 100f)
			{
				continue;
			}
			float targetDistance = FastMath.Distance(ship.GlobalPosition(), value.GetPosition());
			foreach (WeaponStation weaponStation in ship.weaponStations)
			{
				if (weaponStation.Ammo > 0)
				{
					OpportunityThreat opportunityThreat = CombatAI.AnalyzeTarget(weaponStation, ship, value, 0f, targetDistance, 100f);
					float num = opportunityThreat.opportunity + opportunityThreat.threat;
					num *= (float)((!weaponStation.WeaponInfo.overHorizon) ? 1 : 100);
					if (num > currentTargetPriority)
					{
						currentTarget = unit;
						currentTargetPriority = num;
					}
				}
			}
		}
	}

	protected void OnDestroy()
	{
		if (avoidTransform != null)
		{
			Object.Destroy(avoidTransform.gameObject);
		}
	}

	protected virtual void Update()
	{
		if (!commandedDestination && !ship.holdPosition)
		{
			ChooseTarget();
		}
		Steer();
	}
}
